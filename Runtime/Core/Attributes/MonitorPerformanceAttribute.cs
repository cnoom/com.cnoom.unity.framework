using System;

namespace CnoomFramework.Core.Attributes
{
    /// <summary>
    ///     标记需要监控性能的方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class MonitorPerformanceAttribute : Attribute
    {
        /// <summary>
        ///     创建性能监控特性
        /// </summary>
        /// <param name="operationName">操作名称，如果为空则使用方法名</param>
        /// <param name="recordToGlobalStats">是否记录到全局性能统计</param>
        public MonitorPerformanceAttribute(string operationName = null, bool recordToGlobalStats = true)
        {
            OperationName = operationName;
            RecordToGlobalStats = recordToGlobalStats;
        }

        /// <summary>
        ///     性能监控的操作名称，如果为空则使用方法名
        /// </summary>
        public string OperationName { get; }

        /// <summary>
        ///     是否记录到全局性能统计
        /// </summary>
        public bool RecordToGlobalStats { get; }
    }
}