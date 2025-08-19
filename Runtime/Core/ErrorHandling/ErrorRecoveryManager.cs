using System;
using System.Collections.Generic;
using CnoomFramework.Core.Exceptions;
using CnoomFramework.Utils;

namespace CnoomFramework.Core.ErrorHandling
{
    /// <summary>
    ///     错误恢复管理器
    /// </summary>
    public class ErrorRecoveryManager
    {
        private readonly List<ErrorRecord> _errorHistory = new();
        private readonly int _maxErrorHistoryCount = 100;

        private readonly Dictionary<Type, IErrorRecoveryStrategy> _recoveryStrategies = new();

        /// <summary>
        ///     错误历史记录
        /// </summary>
        public IReadOnlyList<ErrorRecord> ErrorHistory => _errorHistory.AsReadOnly();

        /// <summary>
        ///     注册错误恢复策略
        /// </summary>
        public void RegisterRecoveryStrategy<T>(IErrorRecoveryStrategy strategy) where T : Exception
        {
            _recoveryStrategies[typeof(T)] = strategy;
            FrameworkLogger.LogDebug($"Registered recovery strategy for {typeof(T).Name}");
        }

        /// <summary>
        ///     取消注册错误恢复策略
        /// </summary>
        public void UnregisterRecoveryStrategy<T>() where T : Exception
        {
            if (_recoveryStrategies.Remove(typeof(T)))
                FrameworkLogger.LogDebug($"Unregistered recovery strategy for {typeof(T).Name}");
        }

        /// <summary>
        ///     处理异常
        /// </summary>
        public bool HandleException(Exception exception, object context = null)
        {
            if (exception == null) return false;

            try
            {
                // 记录错误
                RecordError(exception, context);

                // 查找恢复策略
                var strategy = FindRecoveryStrategy(exception.GetType());
                if (strategy != null)
                {
                    FrameworkLogger.LogInfo($"Attempting recovery for {exception.GetType().Name}");

                    var result = strategy.TryRecover(exception, context);
                    if (result.IsSuccess)
                    {
                        FrameworkLogger.LogInfo(
                            $"Successfully recovered from {exception.GetType().Name}: {result.Message}");
                        return true;
                    }

                    FrameworkLogger.LogWarning($"Failed to recover from {exception.GetType().Name}: {result.Message}");
                }
                else
                {
                    FrameworkLogger.LogWarning($"No recovery strategy found for {exception.GetType().Name}");
                }

                return false;
            }
            catch (Exception recoveryException)
            {
                FrameworkLogger.LogError($"Error during exception handling: {recoveryException.Message}");
                return false;
            }
        }

        /// <summary>
        ///     查找恢复策略
        /// </summary>
        private IErrorRecoveryStrategy FindRecoveryStrategy(Type exceptionType)
        {
            // 首先查找精确匹配
            if (_recoveryStrategies.TryGetValue(exceptionType, out var strategy)) return strategy;

            // 查找基类匹配
            var currentType = exceptionType.BaseType;
            while (currentType != null && currentType != typeof(object))
            {
                if (_recoveryStrategies.TryGetValue(currentType, out strategy)) return strategy;
                currentType = currentType.BaseType;
            }

            return null;
        }

        /// <summary>
        ///     记录错误
        /// </summary>
        private void RecordError(Exception exception, object context)
        {
            var errorRecord = new ErrorRecord
            {
                Exception = exception,
                Context = context,
                Timestamp = DateTime.Now,
                Severity = GetErrorSeverity(exception)
            };

            _errorHistory.Add(errorRecord);

            // 限制历史记录数量
            if (_errorHistory.Count > _maxErrorHistoryCount) _errorHistory.RemoveAt(0);

            // 根据严重级别记录日志
            switch (errorRecord.Severity)
            {
                case ErrorSeverity.Low:
                    FrameworkLogger.LogDebug($"Low severity error: {exception.Message}");
                    break;
                case ErrorSeverity.Medium:
                    FrameworkLogger.LogWarning($"Medium severity error: {exception.Message}");
                    break;
                case ErrorSeverity.High:
                    FrameworkLogger.LogError($"High severity error: {exception.Message}");
                    break;
                case ErrorSeverity.Critical:
                    FrameworkLogger.LogError($"CRITICAL ERROR: {exception.Message}");
                    break;
            }
        }

        /// <summary>
        ///     获取错误严重级别
        /// </summary>
        private ErrorSeverity GetErrorSeverity(Exception exception)
        {
            if (exception is FrameworkException frameworkException) return frameworkException.Severity;

            // 根据异常类型判断严重级别
            switch (exception)
            {
                case OutOfMemoryException _:
                case StackOverflowException _:
                    return ErrorSeverity.Critical;

                case ArgumentNullException _:
                case ArgumentException _:
                case InvalidOperationException _:
                    return ErrorSeverity.High;

                case NotImplementedException _:
                case NotSupportedException _:
                    return ErrorSeverity.Medium;

                default:
                    return ErrorSeverity.Medium;
            }
        }

        /// <summary>
        ///     清理错误历史
        /// </summary>
        public void ClearErrorHistory()
        {
            _errorHistory.Clear();
            FrameworkLogger.LogDebug("Error history cleared");
        }

        /// <summary>
        ///     获取错误统计
        /// </summary>
        public ErrorStatistics GetErrorStatistics()
        {
            var stats = new ErrorStatistics();

            foreach (var error in _errorHistory)
            {
                stats.TotalErrors++;

                switch (error.Severity)
                {
                    case ErrorSeverity.Low:
                        stats.LowSeverityCount++;
                        break;
                    case ErrorSeverity.Medium:
                        stats.MediumSeverityCount++;
                        break;
                    case ErrorSeverity.High:
                        stats.HighSeverityCount++;
                        break;
                    case ErrorSeverity.Critical:
                        stats.CriticalSeverityCount++;
                        break;
                }
            }

            return stats;
        }
    }

    /// <summary>
    ///     错误记录
    /// </summary>
    public class ErrorRecord
    {
        public Exception Exception { get; set; }
        public object Context { get; set; }
        public DateTime Timestamp { get; set; }
        public ErrorSeverity Severity { get; set; }
    }

    /// <summary>
    ///     错误统计
    /// </summary>
    public class ErrorStatistics
    {
        public int TotalErrors { get; set; }
        public int LowSeverityCount { get; set; }
        public int MediumSeverityCount { get; set; }
        public int HighSeverityCount { get; set; }
        public int CriticalSeverityCount { get; set; }
    }
}