using System;
using CnoomFramework.Core.Exceptions;
using CnoomFramework.Utils;
using UnityEngine;

namespace CnoomFramework.Core.ErrorHandling
{
    /// <summary>
    ///     模块异常恢复策略
    /// </summary>
    public class ModuleExceptionRecoveryStrategy : IErrorRecoveryStrategy
    {
        public RecoveryResult TryRecover(Exception exception, object context)
        {
            if (!(exception is ModuleException moduleException))
                return RecoveryResult.Failure("Not a module exception");

            try
            {
                var frameworkManager = FrameworkManager.Instance;
                var module = frameworkManager.GetModule(moduleException.ModuleName);

                if (module != null)
                {
                    FrameworkLogger.LogWarning($"Attempting to restart module: {moduleException.ModuleName}");

                    // 尝试重启模块
                    module.Shutdown();
                    module.Init();
                    module.Start();

                    return RecoveryResult.Success($"Successfully restarted module: {moduleException.ModuleName}");
                }

                return RecoveryResult.Failure($"Module not found: {moduleException.ModuleName}");
            }
            catch (Exception recoveryException)
            {
                return RecoveryResult.Failure($"Failed to restart module: {recoveryException.Message}");
            }
        }
    }

    /// <summary>
    ///     事件总线异常恢复策略
    /// </summary>
    public class EventBusExceptionRecoveryStrategy : IErrorRecoveryStrategy
    {
        public RecoveryResult TryRecover(Exception exception, object context)
        {
            if (!(exception is EventBusException eventBusException))
                return RecoveryResult.Failure("Not an event bus exception");

            try
            {
                FrameworkLogger.LogWarning($"Event bus error for event type: {eventBusException.EventType?.Name}");

                // 对于事件总线异常，通常只需要记录日志，不需要特殊恢复
                // 可以考虑清理相关的事件订阅或重新初始化事件总线

                return RecoveryResult.Success("Event bus error logged, continuing operation");
            }
            catch (Exception recoveryException)
            {
                return RecoveryResult.Failure($"Failed to handle event bus exception: {recoveryException.Message}");
            }
        }
    }

    /// <summary>
    ///     依赖异常恢复策略
    /// </summary>
    public class DependencyExceptionRecoveryStrategy : IErrorRecoveryStrategy
    {
        public RecoveryResult TryRecover(Exception exception, object context)
        {
            if (!(exception is DependencyException dependencyException))
                return RecoveryResult.Failure("Not a dependency exception");

            try
            {
                FrameworkLogger.LogError(
                    $"Critical dependency error for module type: {dependencyException.ModuleType?.Name}");

                // 依赖异常通常是严重的，可能需要重新初始化整个框架
                // 这里只记录错误，不尝试自动恢复，因为可能导致更严重的问题

                return RecoveryResult.Failure("Dependency errors require manual intervention");
            }
            catch (Exception recoveryException)
            {
                return RecoveryResult.Failure($"Failed to handle dependency exception: {recoveryException.Message}");
            }
        }
    }

    /// <summary>
    ///     通用异常恢复策略
    /// </summary>
    public class GenericExceptionRecoveryStrategy : IErrorRecoveryStrategy
    {
        public RecoveryResult TryRecover(Exception exception, object context)
        {
            try
            {
                // 对于一般异常，记录详细信息并尝试继续运行
                FrameworkLogger.LogError($"Generic exception: {exception.GetType().Name} - {exception.Message}");

                if (exception.InnerException != null)
                    FrameworkLogger.LogError($"Inner exception: {exception.InnerException.Message}");

                // 记录堆栈跟踪（仅在开发模式下）
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                FrameworkLogger.LogError($"Stack trace: {exception.StackTrace}");
#endif

                return RecoveryResult.Success("Exception logged, attempting to continue");
            }
            catch (Exception recoveryException)
            {
                return RecoveryResult.Failure($"Failed to handle generic exception: {recoveryException.Message}");
            }
        }
    }

    /// <summary>
    ///     内存不足异常恢复策略
    /// </summary>
    public class OutOfMemoryRecoveryStrategy : IErrorRecoveryStrategy
    {
        public RecoveryResult TryRecover(Exception exception, object context)
        {
            if (!(exception is OutOfMemoryException)) return RecoveryResult.Failure("Not an out of memory exception");

            try
            {
                FrameworkLogger.LogError("CRITICAL: Out of memory exception detected!");

                // 尝试强制垃圾回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // 在Unity中，也可以尝试卸载未使用的资源
                Resources.UnloadUnusedAssets();

                FrameworkLogger.LogInfo("Attempted memory cleanup");

                return RecoveryResult.Success("Memory cleanup performed");
            }
            catch (Exception recoveryException)
            {
                return RecoveryResult.Failure($"Failed to handle out of memory exception: {recoveryException.Message}");
            }
        }
    }
}