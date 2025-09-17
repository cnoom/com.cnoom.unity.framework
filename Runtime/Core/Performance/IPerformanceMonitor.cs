using System;
using System.Collections.Generic;

namespace CnoomFramework.Core.Performance
{
    /// <summary>
    /// 性能监控器接口
    /// </summary>
    public interface IPerformanceMonitor
    {
        /// <summary>
        /// 性能监控是否启用
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// 开始测量指定操作的性能
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <returns>性能测量标识，用于结束测量</returns>
        string BeginSample(string operationName);

        /// <summary>
        /// 结束性能测量
        /// </summary>
        /// <param name="sampleId">性能测量标识</param>
        void EndSample(string sampleId);

        /// <summary>
        /// 获取指定操作的性能统计
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <returns>性能统计数据</returns>
        PerformanceStats GetStats(string operationName);

        /// <summary>
        /// 获取所有性能统计数据
        /// </summary>
        /// <returns>所有性能统计数据</returns>
        Dictionary<string, PerformanceStats> GetAllStats();

        /// <summary>
        /// 清空所有性能统计数据
        /// </summary>
        void ClearStats();

        /// <summary>
        /// 启用或禁用性能监控
        /// </summary>
        /// <param name="enabled">是否启用</param>
        void SetEnabled(bool enabled);

        /// <summary>
        /// 记录单次性能数据
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <param name="elapsedTime">执行时间</param>
        void RecordSample(string operationName, TimeSpan elapsedTime);
    }
}