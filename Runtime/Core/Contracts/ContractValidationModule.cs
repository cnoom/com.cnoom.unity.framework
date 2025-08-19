using System;
using CnoomFramework.Core.Attributes;
using CnoomFramework.Core.Events;
using CnoomFramework.Core.Exceptions;
using CnoomFramework.Core.Mock;
using CnoomFramework.Utils;

namespace CnoomFramework.Core.Contracts
{
    /// <summary>
    ///     契约验证模块，负责管理和执行契约验证
    /// </summary>
    [AutoRegisterModule]
    public class ContractValidationModule : BaseModule
    {
        /// <summary>
        ///     创建契约验证模块
        /// </summary>
        public ContractValidationModule()
        {
            ContractValidator = new ContractValidator();
        }

        /// <summary>
        ///     契约验证器
        /// </summary>
        public IContractValidator ContractValidator { get; }

        /// <summary>
        ///     是否在验证失败时抛出异常
        /// </summary>
        public bool ThrowOnValidationFailure { get; set; } = true;

        /// <summary>
        ///     初始化模块
        /// </summary>
        protected override void OnInit()
        {
            FrameworkLogger.LogInfo("契约验证模块初始化");

            // 从配置中加载设置
            LoadSettings();

            // 注册框架事件契约
            RegisterFrameworkContracts();
        }

        /// <summary>
        ///     启动模块
        /// </summary>
        protected override void OnStart()
        {
            FrameworkLogger.LogInfo("契约验证模块启动");

            // 发布模块就绪事件
            EventBus.Publish(new ContractValidationModuleReadyEvent());
        }

        /// <summary>
        ///     关闭模块
        /// </summary>
        protected override void OnShutdown()
        {
            FrameworkLogger.LogInfo("契约验证模块关闭");

            // 清除所有契约
            ContractValidator.ClearContracts();
        }

        /// <summary>
        ///     从配置中加载设置
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                var configManager = FrameworkManager.Instance.ConfigManager;
                if (configManager != null)
                {
                    ThrowOnValidationFailure =
                        configManager.GetValue("ContractValidation.ThrowOnFailure", ThrowOnValidationFailure);
                    var enableValidation = configManager.GetValue("ContractValidation.Enabled", true);
                    ContractValidator.SetEnabled(enableValidation);
                }
            }
            catch (Exception ex)
            {
                FrameworkLogger.LogWarning($"加载契约验证设置时出错: {ex.Message}");
            }
        }

        /// <summary>
        ///     注册框架事件契约
        /// </summary>
        private void RegisterFrameworkContracts()
        {
            // 注册框架事件
            ContractValidator.RegisterEventContract<FrameworkInitializedEvent>();
            ContractValidator.RegisterEventContract<FrameworkShutdownEvent>();
            ContractValidator.RegisterEventContract<ModuleRegisteredEvent>();
            ContractValidator.RegisterEventContract<ModuleUnregisteredEvent>();
            ContractValidator.RegisterEventContract<ModuleStateChangedEvent>();

            // 注册配置事件
            ContractValidator.RegisterEventContract<ConfigLoadedEvent>();
            ContractValidator.RegisterEventContract<ConfigSavedEvent>();
            ContractValidator.RegisterEventContract<ConfigChangedEvent>();

            // 注册性能事件
            ContractValidator.RegisterEventContract<PerformanceDataUpdatedEvent>();
            ContractValidator.RegisterEventContract<PerformanceMonitorStatusChangedEvent>();
            ContractValidator.RegisterEventContract<PerformanceStatsResetEvent>();

            // 注册Mock事件
            ContractValidator.RegisterEventContract<ModuleMockedEvent>();
            ContractValidator.RegisterEventContract<ModuleUnmockedEvent>();

            // 注册契约验证事件
            ContractValidator.RegisterEventContract<ContractValidationModuleReadyEvent>();
            ContractValidator.RegisterEventContract<ContractValidationFailedEvent>();
        }

        /// <summary>
        ///     验证事件
        /// </summary>
        public bool ValidateEvent<T>(T eventData) where T : notnull
        {
            var result = ContractValidator.ValidateEvent(eventData);
            if (!result.IsValid)
            {
                // 发布验证失败事件
                EventBus.Publish(new ContractValidationFailedEvent(typeof(T), result.ErrorMessage));

                // 如果配置为抛出异常，则抛出
                if (ThrowOnValidationFailure)
                    throw new EventContractValidationException(typeof(T), result.ErrorMessage);

                return false;
            }

            return true;
        }

        /// <summary>
        ///     验证请求
        /// </summary>
        public bool ValidateRequest<TRequest, TResponse>(TRequest request)
        {
            var result = ContractValidator.ValidateRequest<TRequest, TResponse>(request);
            if (!result.IsValid)
            {
                // 发布验证失败事件
                EventBus.Publish(new ContractValidationFailedEvent(typeof(TRequest), result.ErrorMessage));

                // 如果配置为抛出异常，则抛出
                if (ThrowOnValidationFailure)
                    throw new RequestContractValidationException(typeof(TRequest), typeof(TResponse),
                        result.ErrorMessage);

                return false;
            }

            return true;
        }

        /// <summary>
        ///     验证响应
        /// </summary>
        public bool ValidateResponse<TRequest, TResponse>(TResponse response)
        {
            var result = ContractValidator.ValidateResponse<TRequest, TResponse>(response);
            if (!result.IsValid)
            {
                // 发布验证失败事件
                EventBus.Publish(new ContractValidationFailedEvent(typeof(TResponse), result.ErrorMessage));

                // 如果配置为抛出异常，则抛出
                if (ThrowOnValidationFailure)
                    throw new ResponseContractValidationException(typeof(TRequest), typeof(TResponse),
                        result.ErrorMessage);

                return false;
            }

            return true;
        }
    }
}