using System;
using System.Collections.Generic;
using System.Diagnostics;
using CnoomFramework.Core;
using CnoomFramework.Core.Attributes;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CnoomFramework.Core.Performance
{
    /// <summary>
    ///     性能监控器实现，用于收集和分析框架性能数据
    /// </summary>
    [AutoRegisterModule]
    public class PerformanceMonitor : BaseModule, IPerformanceMonitor
    {
        private static PerformanceMonitor _instance;

        // 活动的性能采样字典
        private readonly Dictionary<string, Stopwatch> _activeSamples = new();

        // 性能统计数据字典
        private readonly Dictionary<string, PerformanceStats> _statsDict = new();

        /// <summary>
        ///     构造函数
        /// </summary>
        public PerformanceMonitor()
        {
            // 设置单例实例
            _instance = this;
        }

        /// <summary>
        ///     单例实例访问器
        /// </summary>
        public static PerformanceMonitor Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 如果框架已初始化，尝试从框架获取实例
                    var frameworkManager = FrameworkManager.Instance;
                    if (frameworkManager != null && frameworkManager.HasModule<PerformanceMonitor>())
                    {
                        _instance = frameworkManager.GetModule<PerformanceMonitor>();
                    }
                    else
                    {
                        // 否则创建新实例
                        _instance = new PerformanceMonitor();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        ///     性能监控是否启用
        /// </summary>
        public bool IsEnabled { get; private set; } = true;

        /// <summary>
        ///     开始测量指定操作的性能
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <returns>性能测量标识，用于结束测量</returns>
        public string BeginSample(string operationName)
        {
            if (!IsEnabled) return string.Empty;

            var sampleId = $"{operationName}_{Guid.NewGuid().ToString()}";
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            _activeSamples[sampleId] = stopwatch;

            return sampleId;
        }

        /// <summary>
        ///     结束指定操作的性能测量
        /// </summary>
        /// <param name="sampleId">由BeginSample返回的性能测量标识</param>
        public void EndSample(string sampleId)
        {
            if (!IsEnabled || string.IsNullOrEmpty(sampleId)) return;

            if (_activeSamples.TryGetValue(sampleId, out var stopwatch))
            {
                stopwatch.Stop();
                var operationName = sampleId.Substring(0, sampleId.LastIndexOf('_'));
                var milliseconds = stopwatch.ElapsedMilliseconds + (stopwatch.ElapsedTicks %
                    TimeSpan.TicksPerMillisecond) / (double)TimeSpan.TicksPerMillisecond;

                RecordOperation(operationName, milliseconds);
                _activeSamples.Remove(sampleId);
            }
        }

        /// <summary>
        ///     记录单次操作的执行时间
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <param name="milliseconds">执行时间（毫秒）</param>
        public void RecordOperation(string operationName, double milliseconds)
        {
            if (!IsEnabled) return;

            if (!_statsDict.TryGetValue(operationName, out var stats))
            {
                stats = new PerformanceStats(operationName);
                _statsDict[operationName] = stats;
            }

            stats.AddSample(milliseconds);
        }

        /// <summary>
        ///     获取指定操作的性能统计数据
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <returns>性能统计数据</returns>
        public PerformanceStats GetStats(string operationName)
        {
            if (_statsDict.TryGetValue(operationName, out var stats)) return stats;

            return null;
        }

        /// <summary>
        ///     获取所有操作的性能统计数据
        /// </summary>
        /// <returns>所有操作的性能统计数据字典</returns>
        public Dictionary<string, PerformanceStats> GetAllStats()
        {
            return new Dictionary<string, PerformanceStats>(_statsDict);
        }

        /// <summary>
        ///     清空所有性能统计数据
        /// </summary>
        public void ClearStats()
        {
            _statsDict.Clear();
        }

        /// <summary>
        ///     记录单次性能数据
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <param name="elapsedTime">执行时间</param>
        public void RecordSample(string operationName, TimeSpan elapsedTime)
        {
            if (!IsEnabled) return;

            if (!_statsDict.TryGetValue(operationName, out var stats))
            {
                stats = new PerformanceStats(operationName);
                _statsDict[operationName] = stats;
            }

            stats.AddSample(elapsedTime.TotalMilliseconds);
        }

        /// <summary>
        ///     重置指定操作的性能统计数据
        /// </summary>
        /// <param name="operationName">操作名称</param>
        public void ResetStats(string operationName)
        {
            if (_statsDict.TryGetValue(operationName, out var stats)) stats.Reset();
        }

        /// <summary>
        ///     模块初始化
        /// </summary>
        protected override void OnStart()
        {
            base.OnStart();
            // 性能监控器启动时的初始化逻辑
            IsEnabled = true;
            Debug.Log("[PerformanceMonitor] 性能监控模块已启动");
        }

        /// <summary>
        ///     模块关闭
        /// </summary>
        protected override void OnShutdown()
        {
            base.OnShutdown();
            // 清理资源
            _activeSamples.Clear();
            Debug.Log("[PerformanceMonitor] 性能监控模块已关闭");
        }

        /// <summary>
        ///     启用或禁用性能监控
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
                // 清除所有活动采样
                _activeSamples.Clear();
        }
    }
}