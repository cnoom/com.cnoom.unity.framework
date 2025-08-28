using System;
using CnoomFramework.Core.EventBuss.Interfaces;
using CnoomFramework.Core.Events;
using CnoomFramework.Extensions.CnoomFramework.Core;
using UnityEngine;

namespace CnoomFramework.Core
{
    /// <summary>
    ///     模块基类，提供通用的模块实现
    /// </summary>
    public abstract class BaseModule : IModule
    {
        /// <summary>
        ///     事件总线引用
        /// </summary>
        protected IEventBus EventBus { get; private set; }

        /// <summary>
        ///     模块名称
        /// </summary>
        public virtual string Name => GetType().Name;

        /// <summary>
        ///     模块当前状态
        /// </summary>
        public ModuleState State { get; private set; } = ModuleState.Uninitialized;

        /// <summary>
        ///     模块优先级，数值越小优先级越高
        /// </summary>
        public virtual int Priority => 0;

        /// <summary>
        ///     初始化模块
        /// </summary>
        public void Init()
        {
            if (State != ModuleState.Uninitialized)
            {
                Debug.LogWarning($"Module {Name} is already initialized. Current state: {State}");
                return;
            }

            try
            {
                EventBus = FrameworkManager.Instance.EventBus;
                RegisterEventHandlers();
                OnInit();
                SetState(ModuleState.Initialized);
                Debug.Log($"Module {Name} initialized successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize module {Name}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        ///     启动模块
        /// </summary>
        public void Start()
        {
            if (State != ModuleState.Initialized)
            {
                Debug.LogWarning($"Module {Name} is not in initialized state. Current state: {State}");
                return;
            }

            try
            {
                OnStart();
                SetState(ModuleState.Started);
                Debug.Log($"Module {Name} started successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start module {Name}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        ///     关闭模块
        /// </summary>
        public void Shutdown()
        {
            if (State == ModuleState.Shutdown || State == ModuleState.Uninitialized) return;

            try
            {
                OnShutdown();
                UnregisterEventHandlers();
                SetState(ModuleState.Shutdown);
                Debug.Log($"Module {Name} shutdown successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during module {Name} shutdown: {ex.Message}");
            }
        }

        /// <summary>
        ///     子类重写此方法实现初始化逻辑
        /// </summary>
        protected virtual void OnInit()
        {
        }

        /// <summary>
        ///     子类重写此方法实现启动逻辑
        /// </summary>
        protected virtual void OnStart()
        {
        }

        /// <summary>
        ///     子类重写此方法实现关闭逻辑
        /// </summary>
        protected virtual void OnShutdown()
        {
        }

        /// <summary>
        ///     设置模块状态
        /// </summary>
        private void SetState(ModuleState newState)
        {
            var oldState = State;
            State = newState;

            // 发布状态变更事件
            EventBus?.Broadcast(new ModuleStateChangedEvent(Name, oldState, newState));
        }

        /// <summary>
        ///     自动注册事件处理器和请求处理器
        /// </summary>
        private void RegisterEventHandlers()
        {
            EventBus.RegisterHandlers(this);
        }

        /// <summary>
        ///     取消注册所有事件处理器和请求处理器
        /// </summary>
        private void UnregisterEventHandlers()
        {
            EventBus.UnregisterHandlers(this);
        }
    }
}