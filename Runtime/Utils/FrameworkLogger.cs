using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CnoomFramework.Utils
{
    /// <summary>
    ///     框架日志管理器
    /// </summary>
    public static class FrameworkLogger
    {
        /// <summary>
        ///     启用调试日志
        /// </summary>
        public static bool EnableDebugLog { get; set; } = true;

        /// <summary>
        ///     启用信息日志
        /// </summary>
        public static bool EnableInfoLog { get; set; } = true;

        /// <summary>
        ///     启用警告日志
        /// </summary>
        public static bool EnableWarningLog { get; set; } = true;

        /// <summary>
        ///     启用错误日志
        /// </summary>
        public static bool EnableErrorLog { get; set; } = true;

        /// <summary>
        ///     记录调试日志
        /// </summary>
        public static void LogDebug(string message, Object context = null)
        {
            if (EnableDebugLog) Debug.Log($"[CnoomFramework] {message}", context);
        }

        /// <summary>
        ///     记录信息日志
        /// </summary>
        public static void LogInfo(string message, Object context = null)
        {
            if (EnableInfoLog) Debug.Log($"[CnoomFramework] {message}", context);
        }

        /// <summary>
        ///     记录警告日志
        /// </summary>
        public static void LogWarning(string message, Object context = null)
        {
            if (EnableWarningLog) Debug.LogWarning($"[CnoomFramework] {message}", context);
        }

        /// <summary>
        ///     记录错误日志
        /// </summary>
        public static void LogError(string message, Object context = null)
        {
            if (EnableErrorLog) Debug.LogError($"[CnoomFramework] {message}", context);
        }

        /// <summary>
        ///     记录异常日志
        /// </summary>
        public static void LogException(Exception exception, Object context = null)
        {
            if (EnableErrorLog) Debug.LogException(exception, context);
        }

        /// <summary>
        ///     格式化日志消息
        /// </summary>
        public static string FormatMessage(string category, string message)
        {
            return $"[CnoomFramework:{category}] {DateTime.Now:HH:mm:ss.fff} - {message}";
        }

        /// <summary>
        ///     记录模块日志
        /// </summary>
        public static void LogModule(string moduleName, string message, LogType logType = LogType.Log)
        {
            var formattedMessage = FormatMessage($"Module:{moduleName}", message);

            switch (logType)
            {
                case LogType.Log:
                    LogInfo(formattedMessage);
                    break;
                case LogType.Warning:
                    LogWarning(formattedMessage);
                    break;
                case LogType.Error:
                    LogError(formattedMessage);
                    break;
            }
        }

        /// <summary>
        ///     记录事件日志
        /// </summary>
        public static void LogEvent(string eventType, string message, LogType logType = LogType.Log)
        {
            var formattedMessage = FormatMessage($"Event:{eventType}", message);

            switch (logType)
            {
                case LogType.Log:
                    LogDebug(formattedMessage);
                    break;
                case LogType.Warning:
                    LogWarning(formattedMessage);
                    break;
                case LogType.Error:
                    LogError(formattedMessage);
                    break;
            }
        }
    }
}