using System;
using System.Collections.Generic;
using CnoomFramework.Utils;

namespace CnoomFramework.Core.Contracts
{
    /// <summary>
    /// 轻量级契约验证器 - 为个人开发者设计的简化版本
    /// </summary>
    public class LightweightContractValidator : IContractValidator
    {
        private readonly HashSet<Type> _registeredEventTypes = new();
        private bool _isEnabled = false; // 默认禁用，个人项目通常不需要

        public void RegisterEventContract<T>() where T : class
        {
            if (!_isEnabled) return;
            _registeredEventTypes.Add(typeof(T));
        }

        public void RegisterEventContract(Type eventType)
        {
            if (!_isEnabled) return;
            _registeredEventTypes.Add(eventType);
        }

        public void RegisterRequestContract<TRequest, TResponse>()
            where TRequest : class
            where TResponse : class
        {
            // 轻量版本不支持请求-响应验证
        }

        public void RegisterRequestContract(Type requestType, Type responseType)
        {
            // 轻量版本不支持请求-响应验证
        }

        public ContractValidationResult ValidateEvent<T>(T eventData) where T : notnull
        {
            if (!_isEnabled || eventData == null)
                return ContractValidationResult.Valid;

            var eventType = typeof(T);

            // 简化的验证：只检查是否注册，不进行复杂的属性验证
            if (_registeredEventTypes.Count > 0 && !IsEventTypeRegistered(eventType))
            {
                return ContractValidationResult.Invalid($"未注册的事件类型: {eventType.Name}");
            }

            return ContractValidationResult.Valid;
        }

        public ContractValidationResult ValidateRequest<TRequest, TResponse>(TRequest request)
        {
            // 轻量版本不支持请求验证
            return ContractValidationResult.Valid;
        }

        public ContractValidationResult ValidateResponse<TRequest, TResponse>(TResponse response)
        {
            // 轻量版本不支持响应验证
            return ContractValidationResult.Valid;
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            FrameworkLogger.LogInfo($"轻量级契约验证已{(_isEnabled ? "启用" : "禁用")}");
        }

        public bool IsEnabled()
        {
            return _isEnabled;
        }

        public void ClearContracts()
        {
            _registeredEventTypes.Clear();
        }

        private bool IsEventTypeRegistered(Type eventType)
        {
            return _registeredEventTypes.Contains(eventType);
        }
    }
}