using System;

namespace CnoomFramework.Core.Exceptions
{
    /// <summary>
    ///     契约验证异常
    /// </summary>
    public class ContractValidationException : FrameworkException
    {
        /// <summary>
        ///     创建契约验证异常
        /// </summary>
        /// <param name="message">错误消息</param>
        public ContractValidationException(string message)
            : base("CONTRACT_VALIDATION_FAILED", ErrorSeverity.Medium, message)
        {
        }

        /// <summary>
        ///     创建契约验证异常
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public ContractValidationException(string message, Exception innerException)
            : base("CONTRACT_VALIDATION_FAILED", ErrorSeverity.Medium, message, innerException)
        {
        }
    }

    /// <summary>
    ///     事件契约验证异常
    /// </summary>
    public class EventContractValidationException : ContractValidationException
    {
        /// <summary>
        ///     创建事件契约验证异常
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <param name="message">错误消息</param>
        public EventContractValidationException(Type eventType, string message)
            : base($"事件契约验证失败 [{eventType.Name}]: {message}")
        {
            EventType = eventType;
        }

        /// <summary>
        ///     事件类型
        /// </summary>
        public Type EventType { get; }
    }

    /// <summary>
    ///     请求契约验证异常
    /// </summary>
    public class RequestContractValidationException : ContractValidationException
    {
        /// <summary>
        ///     创建请求契约验证异常
        /// </summary>
        /// <param name="requestType">请求类型</param>
        /// <param name="responseType">响应类型</param>
        /// <param name="message">错误消息</param>
        public RequestContractValidationException(Type requestType, Type responseType, string message)
            : base($"请求契约验证失败 [{requestType.Name} -> {responseType.Name}]: {message}")
        {
            RequestType = requestType;
            ResponseType = responseType;
        }

        /// <summary>
        ///     请求类型
        /// </summary>
        public Type RequestType { get; }

        /// <summary>
        ///     响应类型
        /// </summary>
        public Type ResponseType { get; }
    }

    /// <summary>
    ///     响应契约验证异常
    /// </summary>
    public class ResponseContractValidationException : ContractValidationException
    {
        /// <summary>
        ///     创建响应契约验证异常
        /// </summary>
        /// <param name="requestType">请求类型</param>
        /// <param name="responseType">响应类型</param>
        /// <param name="message">错误消息</param>
        public ResponseContractValidationException(Type requestType, Type responseType, string message)
            : base($"响应契约验证失败 [{requestType.Name} -> {responseType.Name}]: {message}")
        {
            RequestType = requestType;
            ResponseType = responseType;
        }

        /// <summary>
        ///     请求类型
        /// </summary>
        public Type RequestType { get; }

        /// <summary>
        ///     响应类型
        /// </summary>
        public Type ResponseType { get; }
    }
}