using System;

namespace CnoomFramework.Core.Performance
{
    /// <summary>
    /// 性能统计数据
    /// </summary>
    [Serializable]
    public class PerformanceStats
    {
                /// <summary>
        /// 操作名称
        /// </summary>
        public string OperationName { get; set; }

        /// <summary>
        /// 总调用次数
        /// </summary>
        public int CallCount { get; set; }

        /// <summary>
        /// 总执行时间（毫秒）
        /// </summary>
        public double TotalTimeMs { get; set; }

        /// <summary>
        /// 平均执行时间（毫秒）
        /// </summary>
        public double AverageTimeMs => CallCount > 0 ? TotalTimeMs / CallCount : 0;

        /// <summary>
        /// 最小执行时间（毫秒）
        /// </summary>
        public double MinTimeMs { get; set; } = double.MaxValue;

        /// <summary>
        /// 最大执行时间（毫秒）
        /// </summary>
        public double MaxTimeMs { get; set; }

        /// <summary>
        /// 最后一次调用时间
        /// </summary>
        public DateTime LastCallTime { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="operationName">操作名称</param>
        public PerformanceStats(string operationName)
        {
            OperationName = operationName;
            CallCount = 0;
            TotalTimeMs = 0;
            MinTimeMs = double.MaxValue;
            MaxTimeMs = 0;
            LastCallTime = DateTime.MinValue;
        }

        /// <summary>
        /// 添加一次性能采样数据
        /// </summary>
        /// <param name="elapsedTimeMs">执行时间（毫秒）</param>
        public void AddSample(double elapsedTimeMs)
        {
            CallCount++;
            TotalTimeMs += elapsedTimeMs;
            
            if (elapsedTimeMs < MinTimeMs)
                MinTimeMs = elapsedTimeMs;
            
            if (elapsedTimeMs > MaxTimeMs)
                MaxTimeMs = elapsedTimeMs;
            
            LastCallTime = DateTime.Now;
        }

        /// <summary>
        /// 重置统计数据
        /// </summary>
        public void Reset()
        {
            CallCount = 0;
            TotalTimeMs = 0;
            MinTimeMs = double.MaxValue;
            MaxTimeMs = 0;
            LastCallTime = DateTime.MinValue;
        }

        /// <summary>
        /// 获取格式化的统计信息
        /// </summary>
        /// <returns>格式化的统计信息字符串</returns>
        public override string ToString()
        {
            return $"{OperationName}: 调用{CallCount}次, 平均{AverageTimeMs:F2}ms, 最小{MinTimeMs:F2}ms, 最大{MaxTimeMs:F2}ms";
        }
    }
}