using System;

namespace CnoomFramework.Core.Exceptions
{
    /// <summary>
    ///     框架异常基类
    /// </summary>
    public abstract class FrameworkException : Exception
    {
        protected FrameworkException(string errorCode, ErrorSeverity severity, string message)
            : base(message)
        {
            ErrorCode = errorCode;
            Severity = severity;
        }

        protected FrameworkException(string errorCode, ErrorSeverity severity, string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            Severity = severity;
        }

        /// <summary>
        ///     异常代码
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        ///     异常严重级别
        /// </summary>
        public ErrorSeverity Severity { get; }
    }

    /// <summary>
    ///     异常严重级别
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>低级别，可忽略</summary>
        Low,

        /// <summary>中等级别，需要注意</summary>
        Medium,

        /// <summary>高级别，需要处理</summary>
        High,

        /// <summary>严重级别，可能导致系统不稳定</summary>
        Critical
    }

    /// <summary>
    ///     模块异常
    /// </summary>
    public class ModuleException : FrameworkException
    {
        public ModuleException(string moduleName, string errorCode, string message)
            : base(errorCode, ErrorSeverity.High, message)
        {
            ModuleName = moduleName;
        }

        public ModuleException(string moduleName, string errorCode, string message, Exception innerException)
            : base(errorCode, ErrorSeverity.High, message, innerException)
        {
            ModuleName = moduleName;
        }

        public string ModuleName { get; }
    }

    /// <summary>
    ///     事件总线异常
    /// </summary>
    public class EventBusException : FrameworkException
    {
        public EventBusException(Type eventType, string errorCode, string message)
            : base(errorCode, ErrorSeverity.Medium, message)
        {
            EventType = eventType;
        }

        public EventBusException(Type eventType, string errorCode, string message, Exception innerException)
            : base(errorCode, ErrorSeverity.Medium, message, innerException)
        {
            EventType = eventType;
        }

        public Type EventType { get; }
    }

    /// <summary>
    ///     依赖解析异常
    /// </summary>
    public class DependencyException : FrameworkException
    {
        public DependencyException(Type moduleType, string errorCode, string message)
            : base(errorCode, ErrorSeverity.Critical, message)
        {
            ModuleType = moduleType;
        }

        public DependencyException(Type moduleType, string errorCode, string message, Exception innerException)
            : base(errorCode, ErrorSeverity.Critical, message, innerException)
        {
            ModuleType = moduleType;
        }

        public Type ModuleType { get; }
    }

    /// <summary>
    ///     配置异常
    /// </summary>
    public class ConfigurationException : FrameworkException
    {
        public ConfigurationException(string configKey, string errorCode, string message)
            : base(errorCode, ErrorSeverity.Medium, message)
        {
            ConfigKey = configKey;
        }

        public ConfigurationException(string configKey, string errorCode, string message, Exception innerException)
            : base(errorCode, ErrorSeverity.Medium, message, innerException)
        {
            ConfigKey = configKey;
        }

        public string ConfigKey { get; }
    }
}