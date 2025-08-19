using System;
using CnoomFramework.Core.Performance;

namespace CnoomFramework.Core.Events
{
    /// <summary>
    ///     性能监控事件接口
    /// </summary>
    public interface IPerformanceEvent : IFrameworkEvent
    {
    }

    /// <summary>
    ///     性能数据更新事件
    /// </summary>
    public class PerformanceDataUpdatedEvent : IPerformanceEvent
    {
        /// <summary>
        ///     创建性能数据更新事件
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <param name="stats">性能统计数据</param>
        public PerformanceDataUpdatedEvent(string operationName, PerformanceStats stats)
        {
            Timestamp = DateTime.Now;
            OperationName = operationName;
            Stats = stats;
        }

        /// <summary>
        ///     操作名称
        /// </summary>
        public string OperationName { get; }

        /// <summary>
        ///     性能统计数据
        /// </summary>
        public PerformanceStats Stats { get; }

        /// <summary>
        ///     事件时间戳
        /// </summary>
        public DateTime Timestamp { get; }
    }

    /// <summary>
    ///     性能监控状态变更事件
    /// </summary>
    public class PerformanceMonitorStatusChangedEvent : IPerformanceEvent
    {
        /// <summary>
        ///     创建性能监控状态变更事件
        /// </summary>
        /// <param name="isEnabled">性能监控是否启用</param>
        public PerformanceMonitorStatusChangedEvent(bool isEnabled)
        {
            Timestamp = DateTime.Now;
            IsEnabled = isEnabled;
        }

        /// <summary>
        ///     性能监控是否启用
        /// </summary>
        public bool IsEnabled { get; }

        /// <summary>
        ///     事件时间戳
        /// </summary>
        public DateTime Timestamp { get; }
    }

    /// <summary>
    ///     性能统计重置事件
    /// </summary>
    public class PerformanceStatsResetEvent : IPerformanceEvent
    {
        /// <summary>
        ///     创建性能统计重置事件
        /// </summary>
        /// <param name="operationName">重置的操作名称，如果为null则表示重置所有统计数据</param>
        public PerformanceStatsResetEvent(string operationName = null)
        {
            Timestamp = DateTime.Now;
            OperationName = operationName;
        }

        /// <summary>
        ///     重置的操作名称，如果为null则表示重置所有统计数据
        /// </summary>
        public string OperationName { get; }

        /// <summary>
        ///     事件时间戳
        /// </summary>
        public DateTime Timestamp { get; }
    }
}