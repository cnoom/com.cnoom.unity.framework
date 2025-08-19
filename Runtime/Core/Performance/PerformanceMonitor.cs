using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CnoomFramework.Core.Performance
{
    /// <summary>
    ///     性能监控器实现，用于收集和分析框架性能数据
    /// </summary>
    public class PerformanceMonitor : IPerformanceMonitor
    {
        private static PerformanceMonitor _instance;

        // 活动的性能采样字典
        private readonly Dictionary<string, Stopwatch> _activeSamples = new();

        // 性能统计数据字典
        private readonly Dictionary<string, PerformanceStats> _statsDict = new();

        // 是否启用性能监控

        /// <summary>
        ///     私有构造函数，确保单例模式
        /// </summary>
        private PerformanceMonitor()
        {
        }

        /// <summary>
        ///     单例实例
        /// </summary>
        public static PerformanceMonitor Instance
        {
            get
            {
                if (_instance == null) _instance = new PerformanceMonitor();
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
                var milliseconds = stopwatch.ElapsedMilliseconds + stopwatch.ElapsedTicks %
                    TimeSpan.TicksPerMillisecond / (float)TimeSpan.TicksPerMillisecond;

                RecordOperation(operationName, milliseconds);
                _activeSamples.Remove(sampleId);
            }
        }

        /// <summary>
        ///     记录单次操作的执行时间
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <param name="milliseconds">执行时间（毫秒）</param>
        public void RecordOperation(string operationName, float milliseconds)
        {
            if (!IsEnabled) return;

            if (!_statsDict.TryGetValue(operationName, out var stats))
            {
                stats = new PerformanceStats(operationName);
                _statsDict[operationName] = stats;
            }

            stats.RecordExecution(milliseconds);
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
        ///     重置所有性能统计数据
        /// </summary>
        public void ResetAllStats()
        {
            foreach (var stats in _statsDict.Values) stats.Reset();
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