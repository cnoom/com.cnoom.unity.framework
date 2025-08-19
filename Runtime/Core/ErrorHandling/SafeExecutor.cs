using System;
using CnoomFramework.Utils;

namespace CnoomFramework.Core.ErrorHandling
{
    /// <summary>
    ///     安全执行器，提供异常安全的方法执行
    /// </summary>
    public static class SafeExecutor
    {
        private static ErrorRecoveryManager _errorRecoveryManager;

        /// <summary>
        ///     设置错误恢复管理器
        /// </summary>
        public static void SetErrorRecoveryManager(ErrorRecoveryManager errorRecoveryManager)
        {
            _errorRecoveryManager = errorRecoveryManager;
        }

        /// <summary>
        ///     安全执行操作
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <param name="context">上下文信息</param>
        /// <param name="onError">错误回调</param>
        /// <returns>是否成功执行</returns>
        public static bool Execute(Action action, object context = null, Action<Exception> onError = null)
        {
            if (action == null) return false;

            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                HandleException(ex, context, onError);
                return false;
            }
        }

        /// <summary>
        ///     安全执行带返回值的操作
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">要执行的函数</param>
        /// <param name="defaultValue">默认返回值</param>
        /// <param name="context">上下文信息</param>
        /// <param name="onError">错误回调</param>
        /// <returns>执行结果或默认值</returns>
        public static T Execute<T>(Func<T> func, T defaultValue = default, object context = null,
            Action<Exception> onError = null)
        {
            if (func == null) return defaultValue;

            try
            {
                return func();
            }
            catch (Exception ex)
            {
                HandleException(ex, context, onError);
                return defaultValue;
            }
        }

        /// <summary>
        ///     安全执行操作并返回结果
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <param name="context">上下文信息</param>
        /// <returns>执行结果</returns>
        public static ExecutionResult ExecuteWithResult(Action action, object context = null)
        {
            if (action == null) return ExecutionResult.Failure("Action is null");

            try
            {
                action();
                return ExecutionResult.Success();
            }
            catch (Exception ex)
            {
                HandleException(ex, context);
                return ExecutionResult.Failure(ex.Message, ex);
            }
        }

        /// <summary>
        ///     安全执行带返回值的操作并返回结果
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">要执行的函数</param>
        /// <param name="context">上下文信息</param>
        /// <returns>执行结果</returns>
        public static ExecutionResult<T> ExecuteWithResult<T>(Func<T> func, object context = null)
        {
            if (func == null) return ExecutionResult<T>.Failure("Function is null");

            try
            {
                var result = func();
                return ExecutionResult<T>.Success(result);
            }
            catch (Exception ex)
            {
                HandleException(ex, context);
                return ExecutionResult<T>.Failure(ex.Message, ex);
            }
        }

        /// <summary>
        ///     处理异常
        /// </summary>
        private static void HandleException(Exception exception, object context = null,
            Action<Exception> onError = null)
        {
            // 调用错误回调
            onError?.Invoke(exception);

            // 尝试使用错误恢复管理器处理异常
            if (_errorRecoveryManager != null)
            {
                _errorRecoveryManager.HandleException(exception, context);
            }
            else
            {
                // 如果没有错误恢复管理器，只记录日志
                FrameworkLogger.LogError($"Unhandled exception: {exception.Message}");
                FrameworkLogger.LogException(exception);
            }
        }
    }

    /// <summary>
    ///     执行结果
    /// </summary>
    public class ExecutionResult
    {
        protected ExecutionResult()
        {
        }

        /// <summary>
        ///     是否成功
        /// </summary>
        public bool IsSuccess { get; protected set; }

        /// <summary>
        ///     错误消息
        /// </summary>
        public string ErrorMessage { get; protected set; }

        /// <summary>
        ///     异常信息
        /// </summary>
        public Exception Exception { get; protected set; }

        /// <summary>
        ///     创建成功结果
        /// </summary>
        public static ExecutionResult Success()
        {
            return new ExecutionResult
            {
                IsSuccess = true
            };
        }

        /// <summary>
        ///     创建失败结果
        /// </summary>
        public static ExecutionResult Failure(string errorMessage, Exception exception = null)
        {
            return new ExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
    }

    /// <summary>
    ///     带返回值的执行结果
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    public class ExecutionResult<T> : ExecutionResult
    {
        private ExecutionResult()
        {
        }

        /// <summary>
        ///     返回值
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        ///     创建成功结果
        /// </summary>
        public static ExecutionResult<T> Success(T value)
        {
            return new ExecutionResult<T>
            {
                IsSuccess = true,
                Value = value
            };
        }

        /// <summary>
        ///     创建失败结果
        /// </summary>
        public new static ExecutionResult<T> Failure(string errorMessage, Exception exception = null)
        {
            return new ExecutionResult<T>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Exception = exception,
                Value = default
            };
        }
    }
}