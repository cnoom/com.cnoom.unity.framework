using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using CnoomFramework.Core.Attributes;
using CnoomFramework.Utils;

namespace CnoomFramework.Core.Performance
{
    /// <summary>
    /// 性能监控拦截器 - 提供无侵入性的方法性能监控
    /// 通过编译时织入或运行时代理实现自动性能监控
    /// </summary>
    public static class PerformanceInterceptor
    {
        private static PerformanceMonitor _monitor;
        private static bool _isInitialized = false;

        /// <summary>
        /// 初始化性能拦截器
        /// </summary>
        /// <param name="monitor">性能监控器实例</param>
        public static void Initialize(PerformanceMonitor monitor)
        {
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _isInitialized = true;
            FrameworkLogger.LogInfo("[PerformanceInterceptor] 性能拦截器已初始化");
        }

        /// <summary>
        /// 方法执行前拦截 - 开始性能监控
        /// </summary>
        /// <param name="method">方法信息</param>
        /// <param name="target">目标对象</param>
        /// <param name="args">方法参数</param>
        /// <returns>监控上下文ID</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string BeforeMethodExecution(MethodInfo method, object target, object[] args)
        {
            if (!_isInitialized || _monitor == null || !_monitor.IsEnabled)
                return null;

            try
            {
                // 检查方法是否有性能监控特性
                var monitorAttribute = method.GetCustomAttribute<MonitorPerformanceAttribute>();
                if (monitorAttribute == null)
                    return null;

                // 获取模块名称
                string moduleName = GetModuleName(target);
                string operationName = monitorAttribute.OperationName ?? method.Name;
                string methodName = $"{method.DeclaringType?.Name}.{method.Name}";

                // 开始监控
                return _monitor.BeginMethodMonitoring(methodName, moduleName, operationName);
            }
            catch (Exception ex)
            {
                FrameworkLogger.LogError($"[PerformanceInterceptor] 方法执行前拦截失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 方法执行后拦截 - 结束性能监控
        /// </summary>
        /// <param name="contextId">监控上下文ID</param>
        /// <param name="result">方法返回值</param>
        /// <param name="exception">执行异常（如果有）</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AfterMethodExecution(string contextId, object result, Exception exception)
        {
            if (!_isInitialized || _monitor == null || string.IsNullOrEmpty(contextId))
                return;

            try
            {
                _monitor.EndMethodMonitoring(contextId);
            }
            catch (Exception ex)
            {
                FrameworkLogger.LogError($"[PerformanceInterceptor] 方法执行后拦截失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取模块名称
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <returns>模块名称</returns>
        private static string GetModuleName(object target)
        {
            if (target == null)
                return "Unknown";

            // 如果是模块对象，直接获取模块名称
            if (target is IModule module)
                return module.Name;

            // 否则使用类型名称
            return target.GetType().Name;
        }

        /// <summary>
        /// 手动监控代码块执行
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <param name="action">要执行的操作</param>
        /// <param name="moduleName">模块名称</param>
        public static void MonitorOperation(string operationName, Action action, string moduleName = null)
        {
            if (!_isInitialized || _monitor == null || !_monitor.IsEnabled)
            {
                action?.Invoke();
                return;
            }

            string contextId = null;
            try
            {
                contextId = _monitor.BeginMethodMonitoring(operationName, moduleName, operationName);
                action?.Invoke();
            }
            catch (Exception ex)
            {
                FrameworkLogger.LogError($"[PerformanceInterceptor] 监控操作执行失败: {ex.Message}");
                throw;
            }
            finally
            {
                if (!string.IsNullOrEmpty(contextId))
                {
                    _monitor.EndMethodMonitoring(contextId);
                }
            }
        }

        /// <summary>
        /// 手动监控函数执行
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="operationName">操作名称</param>
        /// <param name="func">要执行的函数</param>
        /// <param name="moduleName">模块名称</param>
        /// <returns>函数返回值</returns>
        public static T MonitorFunction<T>(string operationName, Func<T> func, string moduleName = null)
        {
            if (!_isInitialized || _monitor == null || !_monitor.IsEnabled)
            {
                return func != null ? func() : default(T);
            }

            string contextId = null;
            try
            {
                contextId = _monitor.BeginMethodMonitoring(operationName, moduleName, operationName);
                return func != null ? func() : default(T);
            }
            catch (Exception ex)
            {
                FrameworkLogger.LogError($"[PerformanceInterceptor] 监控函数执行失败: {ex.Message}");
                throw;
            }
            finally
            {
                if (!string.IsNullOrEmpty(contextId))
                {
                    _monitor.EndMethodMonitoring(contextId);
                }
            }
        }

        /// <summary>
        /// 创建性能监控作用域
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <param name="moduleName">模块名称</param>
        /// <returns>性能监控作用域</returns>
        public static PerformanceScope CreateScope(string operationName, string moduleName = null)
        {
            return new PerformanceScope(_monitor, operationName, moduleName);
        }
    }

    /// <summary>
    /// 性能监控作用域 - 使用using语句自动管理监控生命周期
    /// </summary>
    public class PerformanceScope : IDisposable
    {
        private readonly PerformanceMonitor _monitor;
        private readonly string _contextId;
        private bool _disposed = false;

        internal PerformanceScope(PerformanceMonitor monitor, string operationName, string moduleName)
        {
            _monitor = monitor;
            if (_monitor != null && _monitor.IsEnabled)
            {
                _contextId = _monitor.BeginMethodMonitoring(operationName, moduleName, operationName);
            }
        }

        public void Dispose()
        {
            if (!_disposed && _monitor != null && !string.IsNullOrEmpty(_contextId))
            {
                _monitor.EndMethodMonitoring(_contextId);
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 性能监控扩展方法
    /// </summary>
    public static class PerformanceExtensions
    {
        /// <summary>
        /// 为Action添加性能监控
        /// </summary>
        /// <param name="action">要监控的操作</param>
        /// <param name="operationName">操作名称</param>
        /// <param name="moduleName">模块名称</param>
        /// <returns>带监控的Action</returns>
        public static Action WithPerformanceMonitoring(this Action action, string operationName, string moduleName = null)
        {
            return () => PerformanceInterceptor.MonitorOperation(operationName, action, moduleName);
        }

        /// <summary>
        /// 为Func添加性能监控
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">要监控的函数</param>
        /// <param name="operationName">操作名称</param>
        /// <param name="moduleName">模块名称</param>
        /// <returns>带监控的Func</returns>
        public static Func<T> WithPerformanceMonitoring<T>(this Func<T> func, string operationName, string moduleName = null)
        {
            return () => PerformanceInterceptor.MonitorFunction(operationName, func, moduleName);
        }
    }
}