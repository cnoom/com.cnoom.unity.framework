using System;
using System.Threading.Tasks;

namespace CnoomFramework.Core.Performance
{
    /// <summary>
    ///     性能监控工具类，提供简化的性能监控API
    /// </summary>
    public static class PerformanceUtils
    {
        /// <summary>
        ///     使用指定名称开始性能采样
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <returns>性能采样标识，用于结束采样</returns>
        public static string BeginSample(string operationName)
        {
            if (FrameworkManager.Instance == null || !FrameworkManager.Instance.IsInitialized) return string.Empty;

            var performanceModule =
                FrameworkManager.Instance.GetModule("PerformanceMonitor") as PerformanceMonitorModule;
            if (performanceModule == null || !performanceModule.PerformanceMonitor.IsEnabled) return string.Empty;

            return performanceModule.PerformanceMonitor.BeginSample(operationName);
        }

        /// <summary>
        ///     结束性能采样
        /// </summary>
        /// <param name="sampleId">由BeginSample返回的性能采样标识</param>
        public static void EndSample(string sampleId)
        {
            if (string.IsNullOrEmpty(sampleId) || FrameworkManager.Instance == null ||
                !FrameworkManager.Instance.IsInitialized) return;

            var performanceModule =
                FrameworkManager.Instance.GetModule("PerformanceMonitor") as PerformanceMonitorModule;
            if (performanceModule == null || !performanceModule.PerformanceMonitor.IsEnabled) return;

            performanceModule.PerformanceMonitor.EndSample(sampleId);
        }

        /// <summary>
        ///     使用using语句进行性能采样
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <returns>可释放的性能采样对象</returns>
        public static IDisposable SampleScope(string operationName)
        {
            return new PerformanceSampleScope(operationName);
        }

        /// <summary>
        ///     记录单次操作的执行时间
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <param name="milliseconds">执行时间（毫秒）</param>
        public static void RecordOperation(string operationName, float milliseconds)
        {
            if (FrameworkManager.Instance == null || !FrameworkManager.Instance.IsInitialized) return;

            var performanceModule =
                FrameworkManager.Instance.GetModule("PerformanceMonitor") as PerformanceMonitorModule;
            if (performanceModule == null || !performanceModule.PerformanceMonitor.IsEnabled) return;

            performanceModule.PerformanceMonitor.RecordOperation(operationName, milliseconds);
        }

        /// <summary>
        ///     测量操作执行时间
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <param name="action">要执行的操作</param>
        public static void Measure(string operationName, Action action)
        {
            if (action == null) return;

            var sampleId = BeginSample(operationName);

            try
            {
                action();
            }
            finally
            {
                EndSample(sampleId);
            }
        }

        /// <summary>
        ///     测量函数执行时间并返回结果
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="operationName">操作名称</param>
        /// <param name="func">要执行的函数</param>
        /// <returns>函数的返回值</returns>
        public static T Measure<T>(string operationName, Func<T> func)
        {
            if (func == null) return default;

            var sampleId = BeginSample(operationName);

            try
            {
                return func();
            }
            finally
            {
                EndSample(sampleId);
            }
        }

        /// <summary>
        ///     测量异步函数执行时间并返回结果
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="operationName">操作名称</param>
        /// <param name="func">要执行的异步函数</param>
        /// <returns>异步函数的返回值</returns>
        public static async Task<T> MeasureAsync<T>(string operationName, Func<Task<T>> func)
        {
            if (func == null) return default;

            var sampleId = BeginSample(operationName);

            try
            {
                return await func();
            }
            finally
            {
                EndSample(sampleId);
            }
        }
    }

    /// <summary>
    ///     性能采样作用域，用于using语句
    /// </summary>
    public class PerformanceSampleScope : IDisposable
    {
        private readonly string _sampleId;

        /// <summary>
        ///     创建性能采样作用域
        /// </summary>
        /// <param name="operationName">操作名称</param>
        public PerformanceSampleScope(string operationName)
        {
            _sampleId = PerformanceUtils.BeginSample(operationName);
        }

        /// <summary>
        ///     释放资源，结束性能采样
        /// </summary>
        public void Dispose()
        {
            PerformanceUtils.EndSample(_sampleId);
        }
    }
}