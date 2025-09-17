using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using CnoomFramework.Core.Attributes;
using CnoomFramework.Core.Config;
using CnoomFramework.Core.EventBuss.Interfaces;
using CnoomFramework.Core.Events;
using CnoomFramework.Utils;
using UnityEngine;

namespace CnoomFramework.Core.Performance
{
    /// <summary>
    /// 轻量级性能监控器 - 支持方法级性能指标采集
    /// 特点：无侵入性、可配置监控粒度、实时数据收集
    /// </summary>
    public class PerformanceMonitor : BaseModule
    {
        #region 配置常量
        private const string CONFIG_ENABLE_MONITORING = "Performance.EnableMonitoring";
        private const string CONFIG_SAMPLING_INTERVAL = "Performance.SamplingInterval";
        private const string CONFIG_MAX_HISTORY_COUNT = "Performance.MaxHistoryCount";
        private const string CONFIG_ENABLE_MEMORY_TRACKING = "Performance.EnableMemoryTracking";
        private const string CONFIG_ENABLE_MODULE_STATS = "Performance.EnableModuleStats";
        #endregion

        #region 私有字段
        private bool _isEnabled = true;
        private float _samplingInterval = 1.0f;
        private int _maxHistoryCount = 1000;
        private bool _enableMemoryTracking = true;
        private bool _enableModuleStats = true;

        // 性能数据存储
        private readonly ConcurrentQueue<PerformanceMetrics> _performanceHistory = new();
        private readonly ConcurrentDictionary<string, PerformanceStatistics> _moduleStats = new();
        private readonly ConcurrentDictionary<string, PerformanceStatistics> _methodStats = new();
        
        // 实时监控数据
        private readonly ConcurrentDictionary<string, MethodExecutionContext> _activeExecutions = new();
        
        // 配置和事件总线引用
        private IConfigManager _configManager;
        private IEventBus _eventBus;
        
        // 采样控制
        private float _lastSampleTime;
        private readonly object _lockObject = new object();
        #endregion

        #region 公共属性
        public override string Name => "PerformanceMonitor";
        public override int Priority => -100; // 高优先级，早期初始化

        /// <summary>
        /// 是否启用性能监控
        /// </summary>
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// 当前性能历史记录数量
        /// </summary>
        public int HistoryCount => _performanceHistory.Count;

        /// <summary>
        /// 获取所有模块的性能统计
        /// </summary>
        public IReadOnlyDictionary<string, PerformanceStatistics> ModuleStatistics => 
            new Dictionary<string, PerformanceStatistics>(_moduleStats);

        /// <summary>
        /// 获取所有方法的性能统计
        /// </summary>
        public IReadOnlyDictionary<string, PerformanceStatistics> MethodStatistics => 
            new Dictionary<string, PerformanceStatistics>(_methodStats);
        #endregion

        #region 生命周期方法
        protected override void OnInit()
        {
            try
            {
                // 获取框架服务引用
                var frameworkManager = FrameworkManager.Instance;
                _configManager = frameworkManager.ConfigManager;
                _eventBus = frameworkManager.EventBus;

                // 加载配置
                LoadConfiguration();

                // 注册配置变化监听
                RegisterConfigurationListeners();

                FrameworkLogger.LogInfo($"[{Name}] 性能监控器初始化完成 - 启用状态: {_isEnabled}");
            }
            catch (Exception ex)
            {
                FrameworkLogger.LogError($"[{Name}] 初始化失败: {ex.Message}");
                throw;
            }
        }

        protected override void OnStart()
        {
            if (_isEnabled)
            {
                FrameworkLogger.LogInfo($"[{Name}] 性能监控已启动 - 采样间隔: {_samplingInterval}s");
                
                // 发布监控启动事件
                _eventBus?.Broadcast(new PerformanceMonitoringStartedEvent());
            }
        }

        protected override void OnShutdown()
        {
            _isEnabled = false;
            
            // 清理数据
            _activeExecutions.Clear();
            
            FrameworkLogger.LogInfo($"[{Name}] 性能监控器已关闭");
            
            // 发布监控停止事件
            _eventBus?.Broadcast(new PerformanceMonitoringStoppedEvent());
        }
        #endregion

        #region 配置管理
        private void LoadConfiguration()
        {
            if (_configManager == null) return;

            _isEnabled = _configManager.GetValue(CONFIG_ENABLE_MONITORING, _isEnabled);
            _samplingInterval = _configManager.GetValue(CONFIG_SAMPLING_INTERVAL, _samplingInterval);
            _maxHistoryCount = _configManager.GetValue(CONFIG_MAX_HISTORY_COUNT, _maxHistoryCount);
            _enableMemoryTracking = _configManager.GetValue(CONFIG_ENABLE_MEMORY_TRACKING, _enableMemoryTracking);
            _enableModuleStats = _configManager.GetValue(CONFIG_ENABLE_MODULE_STATS, _enableModuleStats);
        }

        private void RegisterConfigurationListeners()
        {
            if (_configManager == null) return;

            _configManager.RegisterChangeListener<bool>(CONFIG_ENABLE_MONITORING, OnMonitoringEnabledChanged);
            _configManager.RegisterChangeListener<float>(CONFIG_SAMPLING_INTERVAL, OnSamplingIntervalChanged);
            _configManager.RegisterChangeListener<int>(CONFIG_MAX_HISTORY_COUNT, OnMaxHistoryCountChanged);
        }

        private void OnMonitoringEnabledChanged(string key, bool enabled)
        {
            _isEnabled = enabled;
            FrameworkLogger.LogInfo($"[{Name}] 监控状态变更: {enabled}");
            
            if (enabled)
                _eventBus?.Broadcast(new PerformanceMonitoringStartedEvent());
            else
                _eventBus?.Broadcast(new PerformanceMonitoringStoppedEvent());
        }

        private void OnSamplingIntervalChanged(string key, float interval)
        {
            _samplingInterval = Mathf.Max(0.1f, interval);
            FrameworkLogger.LogInfo($"[{Name}] 采样间隔变更: {_samplingInterval}s");
        }

        private void OnMaxHistoryCountChanged(string key, int count)
        {
            _maxHistoryCount = Mathf.Max(100, count);
            TrimHistoryIfNeeded();
        }
        #endregion

        #region 核心监控功能
        /// <summary>
        /// 开始监控方法执行
        /// </summary>
        /// <param name="methodName">方法名称</param>
        /// <param name="moduleName">模块名称</param>
        /// <param name="operationName">操作名称</param>
        /// <returns>执行上下文ID</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string BeginMethodMonitoring(string methodName, string moduleName = null, string operationName = null)
        {
            if (!_isEnabled) return null;

            var contextId = Guid.NewGuid().ToString("N");
            var context = new MethodExecutionContext
            {
                ContextId = contextId,
                MethodName = methodName,
                ModuleName = moduleName ?? "Unknown",
                OperationName = operationName ?? methodName,
                StartTime = DateTime.UtcNow,
                StartTimestamp = Stopwatch.GetTimestamp(),
                StartMemory = _enableMemoryTracking ? GC.GetTotalMemory(false) : 0
            };

            _activeExecutions.TryAdd(contextId, context);
            return contextId;
        }

        /// <summary>
        /// 结束方法监控并记录性能数据
        /// </summary>
        /// <param name="contextId">执行上下文ID</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndMethodMonitoring(string contextId)
        {
            if (!_isEnabled || string.IsNullOrEmpty(contextId)) return;

            if (!_activeExecutions.TryRemove(contextId, out var context)) return;

            var endTimestamp = Stopwatch.GetTimestamp();
            var endMemory = _enableMemoryTracking ? GC.GetTotalMemory(false) : 0;
            
            var executionTime = TimeSpan.FromTicks((endTimestamp - context.StartTimestamp) * TimeSpan.TicksPerSecond / Stopwatch.Frequency);

            var metrics = new PerformanceMetrics
            {
                ContextId = contextId,
                OperationName = context.OperationName,
                MethodName = context.MethodName,
                ModuleName = context.ModuleName,
                ExecutionTime = executionTime,
                MemoryBefore = context.StartMemory,
                MemoryAfter = endMemory,
                MemoryDelta = endMemory - context.StartMemory,
                Timestamp = context.StartTime,
                ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId
            };

            RecordPerformanceMetrics(metrics);
        }

        /// <summary>
        /// 记录性能指标
        /// </summary>
        private void RecordPerformanceMetrics(PerformanceMetrics metrics)
        {
            // 添加到历史记录
            _performanceHistory.Enqueue(metrics);
            TrimHistoryIfNeeded();

            // 更新统计信息
            if (_enableModuleStats)
            {
                UpdateModuleStatistics(metrics);
                UpdateMethodStatistics(metrics);
            }

            // 检查是否需要发布性能事件
            CheckAndPublishPerformanceEvents(metrics);
        }

        /// <summary>
        /// 更新模块统计信息
        /// </summary>
        private void UpdateModuleStatistics(PerformanceMetrics metrics)
        {
            var key = metrics.ModuleName;
            _moduleStats.AddOrUpdate(key, 
                new PerformanceStatistics(key), 
                (k, existing) => {
                    existing.AddSample(metrics.ExecutionTime, metrics.MemoryDelta);
                    return existing;
                });
        }

        /// <summary>
        /// 更新方法统计信息
        /// </summary>
        private void UpdateMethodStatistics(PerformanceMetrics metrics)
        {
            var key = $"{metrics.ModuleName}.{metrics.MethodName}";
            _methodStats.AddOrUpdate(key,
                new PerformanceStatistics(key),
                (k, existing) => {
                    existing.AddSample(metrics.ExecutionTime, metrics.MemoryDelta);
                    return existing;
                });
        }

        /// <summary>
        /// 检查并发布性能事件
        /// </summary>
        private void CheckAndPublishPerformanceEvents(PerformanceMetrics metrics)
        {
            // 检查是否有性能问题
            if (metrics.ExecutionTime.TotalMilliseconds > 100) // 超过100ms
            {
                _eventBus?.Broadcast(new PerformanceWarningEvent(
                    metrics.OperationName, 
                    metrics.ExecutionTime, 
                    "执行时间过长"));
            }

            if (_enableMemoryTracking && metrics.MemoryDelta > 10 * 1024 * 1024) // 超过10MB
            {
                _eventBus?.Broadcast(new PerformanceWarningEvent(
                    metrics.OperationName, 
                    metrics.ExecutionTime, 
                    "内存分配过多"));
            }

            // 定期发布性能报告
            if (Time.time - _lastSampleTime >= _samplingInterval)
            {
                _lastSampleTime = Time.time;
                PublishPerformanceReport();
            }
        }

        /// <summary>
        /// 发布性能报告
        /// </summary>
        private void PublishPerformanceReport()
        {
            var report = GeneratePerformanceReport();
            _eventBus?.Broadcast(new PerformanceReportEvent(report));
        }
        #endregion

        #region 数据查询和分析
        /// <summary>
        /// 获取性能历史记录
        /// </summary>
        /// <param name="count">获取数量，-1表示全部</param>
        /// <returns>性能指标列表</returns>
        public List<PerformanceMetrics> GetPerformanceHistory(int count = -1)
        {
            var history = _performanceHistory.ToArray();
            if (count <= 0 || count >= history.Length)
                return history.ToList();
            
            return history.Skip(history.Length - count).ToList();
        }

        /// <summary>
        /// 获取指定模块的性能统计
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <returns>性能统计信息</returns>
        public PerformanceStatistics GetModuleStatistics(string moduleName)
        {
            return _moduleStats.TryGetValue(moduleName, out var stats) ? stats : null;
        }

        /// <summary>
        /// 获取指定方法的性能统计
        /// </summary>
        /// <param name="methodKey">方法键值（格式：ModuleName.MethodName）</param>
        /// <returns>性能统计信息</returns>
        public PerformanceStatistics GetMethodStatistics(string methodKey)
        {
            return _methodStats.TryGetValue(methodKey, out var stats) ? stats : null;
        }

        /// <summary>
        /// 生成性能报告
        /// </summary>
        /// <returns>性能报告</returns>
        public PerformanceReport GeneratePerformanceReport()
        {
            var report = new PerformanceReport
            {
                GeneratedAt = DateTime.UtcNow,
                TotalSamples = _performanceHistory.Count,
                ActiveExecutions = _activeExecutions.Count,
                ModuleCount = _moduleStats.Count,
                MethodCount = _methodStats.Count
            };

            // 获取最近的性能数据
            var recentMetrics = GetPerformanceHistory(100);
            if (recentMetrics.Count > 0)
            {
                report.AverageExecutionTime = TimeSpan.FromMilliseconds(
                    recentMetrics.Average(m => m.ExecutionTime.TotalMilliseconds));
                report.MaxExecutionTime = TimeSpan.FromMilliseconds(
                    recentMetrics.Max(m => m.ExecutionTime.TotalMilliseconds));
                report.TotalMemoryAllocated = recentMetrics.Sum(m => Math.Max(0, m.MemoryDelta));
            }

            // 获取性能最差的方法
            report.SlowestMethods = _methodStats.Values
                .OrderByDescending(s => s.AverageExecutionTime.TotalMilliseconds)
                .Take(5)
                .Select(s => new MethodPerformanceInfo
                {
                    MethodName = s.Name,
                    AverageTime = s.AverageExecutionTime,
                    MaxTime = s.MaxExecutionTime,
                    CallCount = s.SampleCount
                })
                .ToList();

            return report;
        }

        /// <summary>
        /// 清空性能历史数据
        /// </summary>
        public void ClearHistory()
        {
            lock (_lockObject)
            {
                while (_performanceHistory.TryDequeue(out _)) { }
                _moduleStats.Clear();
                _methodStats.Clear();
            }
            
            FrameworkLogger.LogInfo($"[{Name}] 性能历史数据已清空");
        }

        /// <summary>
        /// 获取实时性能快照
        /// </summary>
        /// <returns>实时性能数据</returns>
        public RealtimePerformanceSnapshot GetRealtimeSnapshot()
        {
            var snapshot = new RealtimePerformanceSnapshot
            {
                Timestamp = DateTime.UtcNow,
                ActiveExecutions = _activeExecutions.Count,
                TotalMemoryUsage = GC.GetTotalMemory(false),
                FrameRate = 1.0f / Time.unscaledDeltaTime,
                FrameTime = Time.unscaledDeltaTime * 1000f // 转换为毫秒
            };

            // 获取最近1秒的性能数据
            var recentMetrics = _performanceHistory
                .Where(m => (DateTime.UtcNow - m.Timestamp).TotalSeconds <= 1)
                .ToList();

            if (recentMetrics.Count > 0)
            {
                snapshot.RecentAverageExecutionTime = TimeSpan.FromMilliseconds(
                    recentMetrics.Average(m => m.ExecutionTime.TotalMilliseconds));
                snapshot.RecentMethodCalls = recentMetrics.Count;
            }

            return snapshot;
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 修剪历史记录以保持在限制范围内
        /// </summary>
        private void TrimHistoryIfNeeded()
        {
            while (_performanceHistory.Count > _maxHistoryCount)
            {
                _performanceHistory.TryDequeue(out _);
            }
        }

        /// <summary>
        /// 设置监控启用状态
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetEnabled(bool enabled)
        {
            if (_isEnabled != enabled)
            {
                _isEnabled = enabled;
                _configManager?.SetValue(CONFIG_ENABLE_MONITORING, enabled, persistent: true);
            }
        }

        /// <summary>
        /// 设置采样间隔
        /// </summary>
        /// <param name="interval">采样间隔（秒）</param>
        public void SetSamplingInterval(float interval)
        {
            var newInterval = Mathf.Max(0.1f, interval);
            if (Math.Abs(_samplingInterval - newInterval) > 0.01f)
            {
                _samplingInterval = newInterval;
                _configManager?.SetValue(CONFIG_SAMPLING_INTERVAL, newInterval, persistent: true);
            }
        }
        #endregion
    }

    #region 数据结构定义
    /// <summary>
    /// 方法执行上下文
    /// </summary>
    internal class MethodExecutionContext
    {
        public string ContextId { get; set; }
        public string MethodName { get; set; }
        public string ModuleName { get; set; }
        public string OperationName { get; set; }
        public DateTime StartTime { get; set; }
        public long StartTimestamp { get; set; }
        public long StartMemory { get; set; }
    }

    /// <summary>
    /// 性能指标数据
    /// </summary>
    public class PerformanceMetrics
    {
        public string ContextId { get; set; }
        public string OperationName { get; set; }
        public string MethodName { get; set; }
        public string ModuleName { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public long MemoryBefore { get; set; }
        public long MemoryAfter { get; set; }
        public long MemoryDelta { get; set; }
        public DateTime Timestamp { get; set; }
        public int ThreadId { get; set; }
    }

    /// <summary>
    /// 性能统计信息
    /// </summary>
    public class PerformanceStatistics
    {
        public string Name { get; }
        public int SampleCount { get; private set; }
        public TimeSpan TotalExecutionTime { get; private set; }
        public TimeSpan AverageExecutionTime => SampleCount > 0 ? 
            TimeSpan.FromTicks(TotalExecutionTime.Ticks / SampleCount) : TimeSpan.Zero;
        public TimeSpan MinExecutionTime { get; private set; } = TimeSpan.MaxValue;
        public TimeSpan MaxExecutionTime { get; private set; }
        public long TotalMemoryDelta { get; private set; }
        public long AverageMemoryDelta => SampleCount > 0 ? TotalMemoryDelta / SampleCount : 0;
        public DateTime LastUpdated { get; private set; }

        public PerformanceStatistics(string name)
        {
            Name = name;
        }

        public void AddSample(TimeSpan executionTime, long memoryDelta)
        {
            SampleCount++;
            TotalExecutionTime = TotalExecutionTime.Add(executionTime);
            TotalMemoryDelta += memoryDelta;
            
            if (executionTime < MinExecutionTime)
                MinExecutionTime = executionTime;
            if (executionTime > MaxExecutionTime)
                MaxExecutionTime = executionTime;
                
            LastUpdated = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 性能报告
    /// </summary>
    public class PerformanceReport
    {
        public DateTime GeneratedAt { get; set; }
        public int TotalSamples { get; set; }
        public int ActiveExecutions { get; set; }
        public int ModuleCount { get; set; }
        public int MethodCount { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public TimeSpan MaxExecutionTime { get; set; }
        public long TotalMemoryAllocated { get; set; }
        public List<MethodPerformanceInfo> SlowestMethods { get; set; } = new();
    }

    /// <summary>
    /// 方法性能信息
    /// </summary>
    public class MethodPerformanceInfo
    {
        public string MethodName { get; set; }
        public TimeSpan AverageTime { get; set; }
        public TimeSpan MaxTime { get; set; }
        public int CallCount { get; set; }
    }

    /// <summary>
    /// 实时性能快照
    /// </summary>
    public class RealtimePerformanceSnapshot
    {
        public DateTime Timestamp { get; set; }
        public int ActiveExecutions { get; set; }
        public long TotalMemoryUsage { get; set; }
        public float FrameRate { get; set; }
        public float FrameTime { get; set; }
        public TimeSpan RecentAverageExecutionTime { get; set; }
        public int RecentMethodCalls { get; set; }
    }
    #endregion

    #region 性能监控事件
    /// <summary>
    /// 性能监控启动事件
    /// </summary>
    public class PerformanceMonitoringStartedEvent : IFrameworkEvent
    {
        public DateTime Timestamp { get; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 性能监控停止事件
    /// </summary>
    public class PerformanceMonitoringStoppedEvent : IFrameworkEvent
    {
        public DateTime Timestamp { get; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 性能警告事件
    /// </summary>
    public class PerformanceWarningEvent : IFrameworkEvent
    {
        public string OperationName { get; }
        public TimeSpan ExecutionTime { get; }
        public string Warning { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;

        public PerformanceWarningEvent(string operationName, TimeSpan executionTime, string warning)
        {
            OperationName = operationName;
            ExecutionTime = executionTime;
            Warning = warning;
        }
    }

    /// <summary>
    /// 性能报告事件
    /// </summary>
    public class PerformanceReportEvent : IFrameworkEvent
    {
        public PerformanceReport Report { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;

        public PerformanceReportEvent(PerformanceReport report)
        {
            Report = report;
        }
    }
    #endregion
}