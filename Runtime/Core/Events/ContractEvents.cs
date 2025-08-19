using System;

namespace CnoomFramework.Core.Events
{
    /// <summary>
    ///     契约验证模块就绪事件
    /// </summary>
    public class ContractValidationModuleReadyEvent : IFrameworkEvent
    {
        public ContractValidationModuleReadyEvent()
        {
            Timestamp = DateTime.Now;
        }

        /// <summary>
        ///     事件时间戳
        /// </summary>
        public DateTime Timestamp { get; }
    }

    /// <summary>
    ///     契约验证失败事件
    /// </summary>
    public class ContractValidationFailedEvent : IFrameworkEvent
    {
        public ContractValidationFailedEvent(Type failedType, string errorMessage)
        {
            FailedType = failedType;
            ErrorMessage = errorMessage;
            Timestamp = DateTime.Now;
        }

        /// <summary>
        ///     验证失败的类型
        /// </summary>
        public Type FailedType { get; }

        /// <summary>
        ///     错误消息
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        ///     事件时间戳
        /// </summary>
        public DateTime Timestamp { get; }
    }
}