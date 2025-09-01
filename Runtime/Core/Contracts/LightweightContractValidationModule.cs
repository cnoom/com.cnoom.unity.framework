using System;
using CnoomFramework.Core.Attributes;
using CnoomFramework.Core.Events;
using CnoomFramework.Core.Exceptions;
using CnoomFramework.Utils;
using UnityEngine;

namespace CnoomFramework.Core.Contracts
{
    /// <summary>
    /// 轻量级契约验证模块 - 为个人开发者设计的简化版本
    /// </summary>
    [AutoRegisterModule]
    public class LightweightContractValidationModule : BaseModule
    {
        /// <summary>
        /// 契约验证器
        /// </summary>
        public IContractValidator ContractValidator { get; private set; }

        /// <summary>
        /// 是否在验证失败时记录警告（默认不抛出异常）
        /// </summary>
        public bool LogWarningsOnFailure { get; set; } = true;

        protected override void OnInit()
        {
            try
            {
                // 创建轻量级验证器
                ContractValidator = new LightweightContractValidator();
                
                // 默认禁用，开发者可以手动启用
                ContractValidator.SetEnabled(false);

                FrameworkLogger.LogInfo("轻量级契约验证模块初始化完成");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[契约验证] 初始化失败，将禁用功能: {ex.Message}");
                ContractValidator = null;
            }
        }

        protected override void OnStart()
        {
            FrameworkLogger.LogInfo("轻量级契约验证模块启动");
        }

        protected override void OnShutdown()
        {
            FrameworkLogger.LogInfo("轻量级契约验证模块关闭");
        }

        /// <summary>
        /// 简化的事件验证
        /// </summary>
        public bool ValidateEvent<T>(T eventData) where T : notnull
        {
            if (!ContractValidator.IsEnabled() || eventData == null)
                return true;

            var result = ContractValidator.ValidateEvent(eventData);
            
            if (!result.IsValid && LogWarningsOnFailure)
            {
                FrameworkLogger.LogWarning($"契约验证警告: {result.ErrorMessage}");
            }

            return result.IsValid;
        }

        /// <summary>
        /// 启用或禁用契约验证
        /// </summary>
        public void SetValidationEnabled(bool enabled)
        {
            ContractValidator.SetEnabled(enabled);
        }

        /// <summary>
        /// 快速注册事件契约（简化API）
        /// </summary>
        public void RegisterEvent<T>() where T : class
        {
            ContractValidator.RegisterEventContract<T>();
        }
    }
}