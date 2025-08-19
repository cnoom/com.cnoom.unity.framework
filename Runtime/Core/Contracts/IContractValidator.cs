using System;

namespace CnoomFramework.Core.Contracts
{
    /// <summary>
    ///     契约验证器接口，用于验证事件和请求是否符合预定义的契约
    /// </summary>
    public interface IContractValidator
    {
        /// <summary>
        ///     注册事件契约
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        void RegisterEventContract<T>() where T : class;

        /// <summary>
        ///     注册事件契约
        /// </summary>
        /// <param name="eventType">事件类型</param>
        void RegisterEventContract(Type eventType);

        /// <summary>
        ///     注册请求-响应契约
        /// </summary>
        /// <typeparam name="TRequest">请求类型</typeparam>
        /// <typeparam name="TResponse">响应类型</typeparam>
        void RegisterRequestContract<TRequest, TResponse>()
            where TRequest : class
            where TResponse : class;

        /// <summary>
        ///     注册请求-响应契约
        /// </summary>
        /// <param name="requestType">请求类型</param>
        /// <param name="responseType">响应类型</param>
        void RegisterRequestContract(Type requestType, Type responseType);

        /// <summary>
        ///     验证事件是否符合契约
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        /// <returns>验证结果</returns>
        ContractValidationResult ValidateEvent<T>(T eventData) where T : notnull;

        /// <summary>
        ///     验证请求是否符合契约
        /// </summary>
        /// <typeparam name="TRequest">请求类型</typeparam>
        /// <typeparam name="TResponse">响应类型</typeparam>
        /// <param name="request">请求数据</param>
        /// <returns>验证结果</returns>
        ContractValidationResult ValidateRequest<TRequest, TResponse>(TRequest request);

        /// <summary>
        ///     验证响应是否符合契约
        /// </summary>
        /// <typeparam name="TRequest">请求类型</typeparam>
        /// <typeparam name="TResponse">响应类型</typeparam>
        /// <param name="response">响应数据</param>
        /// <returns>验证结果</returns>
        ContractValidationResult ValidateResponse<TRequest, TResponse>(TResponse response);

        /// <summary>
        ///     启用或禁用契约验证
        /// </summary>
        /// <param name="enabled">是否启用</param>
        void SetEnabled(bool enabled);

        /// <summary>
        ///     获取契约验证是否启用
        /// </summary>
        /// <returns>是否启用</returns>
        bool IsEnabled();

        /// <summary>
        ///     清除所有注册的契约
        /// </summary>
        void ClearContracts();
    }

    /// <summary>
    ///     契约验证结果
    /// </summary>
    public class ContractValidationResult
    {
        private ContractValidationResult(bool isValid, string errorMessage)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        ///     是否验证通过
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        ///     错误消息
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        ///     创建验证通过的结果
        /// </summary>
        public static ContractValidationResult Valid => new(true, null);

        /// <summary>
        ///     创建验证失败的结果
        /// </summary>
        /// <param name="errorMessage">错误消息</param>
        public static ContractValidationResult Invalid(string errorMessage)
        {
            return new ContractValidationResult(false, errorMessage);
        }
    }
}