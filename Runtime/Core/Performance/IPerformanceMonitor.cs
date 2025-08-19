using System;
using System.Collections.Generic;

namespace CnoomFramework.Core.Performance
{
    /// <summary>
    ///     性能监控接口，提供性能数据收集和分析功能
    /// </summary>
    public interface IPerformanceMonitor
    {
        /// <summary>
        ///     性能监控是否启用
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        ///     开始测量指定操作的性能
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <returns>性能测量标识，用于结束测量</returns>
        string BeginSample(string operationName);

        /// <summary>
        ///     结束指定操作的性能测量
        /// </summary>
        /// <param name="sampleId">由BeginSample返回的性能测量标识</param>
        void EndSample(string sampleId);

        /// <summary>
        ///     记录单次操作的执行时间
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <param name="milliseconds">执行时间（毫秒）</param>
        void RecordOperation(string operationName, float milliseconds);

        /// <summary>
        ///     获取指定操作的性能统计数据
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <returns>性能统计数据</returns>
        PerformanceStats GetStats(string operationName);

        /// <summary>
        ///     获取所有操作的性能统计数据
        /// </summary>
        /// <returns>所有操作的性能统计数据字典</returns>
        Dictionary<string, PerformanceStats> GetAllStats();

        /// <summary>
        ///     重置所有性能统计数据
        /// </summary>
        void ResetAllStats();

        /// <summary>
        ///     重置指定操作的性能统计数据
        /// </summary>
        /// <param name="operationName">操作名称</param>
        void ResetStats(string operationName);

        /// <summary>
        ///     启用或禁用性能监控
        /// </summary>
        /// <param name="enabled">是否启用</param>
        void SetEnabled(bool enabled);
    }

    /// <summary>
    ///     性能统计数据
    /// </summary>
    [Serializable]
    public class PerformanceStats
    {
        /// <summary>
        ///     创建新的性能统计数据
        /// </summary>
        /// <param name="operationName">操作名称</param>
        public PerformanceStats(string operationName)
        {
            OperationName = operationName;
            CallCount = 0;
            TotalTime = 0;
            MinTime = float.MaxValue;
            MaxTime = 0;
            LastTime = 0;
            LastExecutionTime = DateTime.Now;
        }

        /// <summary>
        ///     操作名称
        /// </summary>
        public string OperationName { get; set; }

        /// <summary>
        ///     调用次数
        /// </summary>
        public int CallCount { get; set; }

        /// <summary>
        ///     总执行时间（毫秒）
        /// </summary>
        public float TotalTime { get; set; }

        /// <summary>
        ///     最小执行时间（毫秒）
        /// </summary>
        public float MinTime { get; set; }

        /// <summary>
        ///     最大执行时间（毫秒）
        /// </summary>
        public float MaxTime { get; set; }

        /// <summary>
        ///     平均执行时间（毫秒）
        /// </summary>
        public float AverageTime => CallCount > 0 ? TotalTime / CallCount : 0;

        /// <summary>
        ///     最后一次执行时间（毫秒）
        /// </summary>
        public float LastTime { get; set; }

        /// <summary>
        ///     最后执行时间戳
        /// </summary>
        public DateTime LastExecutionTime { get; set; }

        /// <summary>
        ///     记录一次操作执行
        /// </summary>
        /// <param name="milliseconds">执行时间（毫秒）</param>
        public void RecordExecution(float milliseconds)
        {
            CallCount++;
            TotalTime += milliseconds;
            MinTime = Math.Min(MinTime, milliseconds);
            MaxTime = Math.Max(MaxTime, milliseconds);
            LastTime = milliseconds;
            LastExecutionTime = DateTime.Now;
        }

        /// <summary>
        ///     重置统计数据
        /// </summary>
        public void Reset()
        {
            CallCount = 0;
            TotalTime = 0;
            MinTime = float.MaxValue;
            MaxTime = 0;
            LastTime = 0;
        }
    }
}