using System;
using System.Collections.Generic;
using CnoomFramework.Utils;

namespace CnoomFramework.Core.Contracts
{
    /// <summary>
    ///     契约验证器实现，用于验证事件和请求是否符合预定义的契约
    /// </summary>
    public class ContractValidator : IContractValidator
    {
        private readonly HashSet<Type> _registeredEventTypes = new();
        private readonly Dictionary<Type, Type> _registeredRequestTypes = new();
        private bool _isEnabled = true;

        /// <inheritdoc />
        public void RegisterEventContract<T>() where T : class
        {
            RegisterEventContract(typeof(T));
        }

        /// <inheritdoc />
        public void RegisterEventContract(Type eventType)
        {
            if (eventType == null)
                throw new ArgumentNullException(nameof(eventType));

            _registeredEventTypes.Add(eventType);
            FrameworkLogger.LogDebug($"已注册事件契约: {eventType.Name}");
        }

        /// <inheritdoc />
        public void RegisterRequestContract<TRequest, TResponse>()
            where TRequest : class
            where TResponse : class
        {
            RegisterRequestContract(typeof(TRequest), typeof(TResponse));
        }

        /// <inheritdoc />
        public void RegisterRequestContract(Type requestType, Type responseType)
        {
            if (requestType == null)
                throw new ArgumentNullException(nameof(requestType));

            if (responseType == null)
                throw new ArgumentNullException(nameof(responseType));

            _registeredRequestTypes[requestType] = responseType;
            FrameworkLogger.LogDebug($"已注册请求-响应契约: {requestType.Name} -> {responseType.Name}");
        }

        /// <inheritdoc />
        public ContractValidationResult ValidateEvent<T>(T eventData) where T : class
        {
            if (!_isEnabled)
                return ContractValidationResult.Valid;

            if (eventData == null)
                return ContractValidationResult.Invalid("事件数据不能为空");

            var eventType = typeof(T);

            // 检查事件类型是否已注册
            if (!IsEventTypeRegistered(eventType))
                return ContractValidationResult.Invalid($"未注册的事件类型: {eventType.Name}");

            // 验证事件数据的属性（可以根据需要扩展）
            return ValidateObject(eventData);
        }

        /// <inheritdoc />
        public ContractValidationResult ValidateRequest<TRequest, TResponse>(TRequest request)
            where TRequest : class
            where TResponse : class
        {
            if (!_isEnabled)
                return ContractValidationResult.Valid;

            if (request == null)
                return ContractValidationResult.Invalid("请求数据不能为空");

            var requestType = typeof(TRequest);
            var responseType = typeof(TResponse);

            // 检查请求-响应类型是否已注册
            if (!IsRequestTypeRegistered(requestType, responseType))
                return ContractValidationResult.Invalid($"未注册的请求-响应类型: {requestType.Name} -> {responseType.Name}");

            // 验证请求数据的属性（可以根据需要扩展）
            return ValidateObject(request);
        }

        /// <inheritdoc />
        public ContractValidationResult ValidateResponse<TRequest, TResponse>(TResponse response)
            where TRequest : class
            where TResponse : class
        {
            if (!_isEnabled)
                return ContractValidationResult.Valid;

            if (response == null)
                return ContractValidationResult.Invalid("响应数据不能为空");

            var requestType = typeof(TRequest);
            var responseType = typeof(TResponse);

            // 检查请求-响应类型是否已注册
            if (!IsRequestTypeRegistered(requestType, responseType))
                return ContractValidationResult.Invalid($"未注册的请求-响应类型: {requestType.Name} -> {responseType.Name}");

            // 验证响应数据的属性（可以根据需要扩展）
            return ValidateObject(response);
        }

        /// <inheritdoc />
        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            FrameworkLogger.LogInfo($"契约验证已{(_isEnabled ? "启用" : "禁用")}");
        }

        /// <inheritdoc />
        public bool IsEnabled()
        {
            return _isEnabled;
        }

        /// <inheritdoc />
        public void ClearContracts()
        {
            _registeredEventTypes.Clear();
            _registeredRequestTypes.Clear();
            FrameworkLogger.LogInfo("已清除所有契约");
        }

        /// <summary>
        ///     检查事件类型是否已注册
        /// </summary>
        private bool IsEventTypeRegistered(Type eventType)
        {
            // 直接匹配
            if (_registeredEventTypes.Contains(eventType))
                return true;

            // 检查接口实现
            foreach (var registeredType in _registeredEventTypes)
                if (registeredType.IsAssignableFrom(eventType))
                    return true;

            return false;
        }

        /// <summary>
        ///     检查请求-响应类型是否已注册
        /// </summary>
        private bool IsRequestTypeRegistered(Type requestType, Type responseType)
        {
            // 直接匹配
            if (_registeredRequestTypes.TryGetValue(requestType, out var registeredResponseType))
                return registeredResponseType == responseType || registeredResponseType.IsAssignableFrom(responseType);

            // 检查接口实现
            foreach (var pair in _registeredRequestTypes)
                if (pair.Key.IsAssignableFrom(requestType) &&
                    (pair.Value == responseType || pair.Value.IsAssignableFrom(responseType)))
                    return true;

            return false;
        }

        /// <summary>
        ///     验证对象的属性
        /// </summary>
        private ContractValidationResult ValidateObject(object obj)
        {
            // 这里可以添加更复杂的验证逻辑，例如使用数据注解、反射等
            // 目前只是简单地检查对象是否为空
            return obj != null
                ? ContractValidationResult.Valid
                : ContractValidationResult.Invalid("对象不能为空");
        }
    }
}