using System;
using CnoomFramework.Core.Attributes;
using CnoomFramework.Core.Events;
using UnityEngine;

namespace CnoomFramework.Core.Performance
{
    /// <summary>
    ///     性能监控模块，负责管理框架性能监控功能
    /// </summary>
    [AutoRegisterModule]
    public class PerformanceMonitorModule : BaseModule
    {
        private bool _autoPublishUpdates = true;
        private float _lastUpdateTime;
        private float _updateInterval = 1.0f;

        /// <summary>
        ///     获取性能监控器实例
        /// </summary>
        public IPerformanceMonitor PerformanceMonitor { get; private set; }

        /// <summary>
        ///     模块名称
        /// </summary>
        public override string Name => "PerformanceMonitor";

        /// <summary>
        ///     初始化模块
        /// </summary>
        protected override void OnInit()
        {
            PerformanceMonitor = Performance.PerformanceMonitor.Instance;
            _lastUpdateTime = Time.realtimeSinceStartup;

            // 延迟读取配置以避免循环依赖
            try
            {
                // 从配置中读取设置
                if (FrameworkManager.Instance?.ConfigManager != null)
                {
                    _updateInterval = FrameworkManager.Instance.ConfigManager.GetValue("Performance.UpdateInterval", 1.0f);
                    _autoPublishUpdates =
                        FrameworkManager.Instance.ConfigManager.GetValue("Performance.AutoPublishUpdates", true);
                    var enableMonitoring =
                        FrameworkManager.Instance.ConfigManager.GetValue("Performance.EnableMonitoring", true);
                    PerformanceMonitor.SetEnabled(enableMonitoring);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PerformanceMonitor] 配置读取失败，使用默认配置: {ex.Message}");
                // 使用默认配置
                _updateInterval = 1.0f;
                _autoPublishUpdates = true;
                PerformanceMonitor.SetEnabled(true);
            }

            // 延迟订阅配置变更事件
            try
            {
                SubscribeToConfigChanges();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PerformanceMonitor] 事件订阅失败: {ex.Message}");
            }
        }

        /// <summary>
        ///     启动模块
        /// </summary>
        protected override void OnStart()
        {
            // 在编辑器测试环境下跳过GameObject创建
            if (!Application.isPlaying)
            {
                Debug.Log("[PerformanceMonitor] 在编辑器环境下跳过GameObject创建");
                PublishStatusChangedEvent();
                return;
            }
            
            // 创建性能监控组件
            var gameObject = new GameObject("PerformanceMonitorComponent");
            var component = gameObject.AddComponent<PerformanceMonitorComponent>();
            component.Initialize(this);
            
            // 只在播放模式下使用 DontDestroyOnLoad
            if (Application.isPlaying)
            {
                GameObject.DontDestroyOnLoad(gameObject);
            }

            // 发布性能监控状态事件
            PublishStatusChangedEvent();
        }

        /// <summary>
        ///     关闭模块
        /// </summary>
        protected override void OnShutdown()
        {
            // 清理资源
        }

        /// <summary>
        ///     更新模块
        /// </summary>
        public void Update()
        {
            // 在测试环境下跳过更新以避免潜在的无限循环
            if (!Application.isPlaying || State != ModuleState.Started)
                return;
                
            if (_autoPublishUpdates && PerformanceMonitor.IsEnabled)
            {
                var currentTime = Time.realtimeSinceStartup;
                if (currentTime - _lastUpdateTime >= _updateInterval)
                {
                    PublishPerformanceUpdates();
                    _lastUpdateTime = currentTime;
                }
            }
        }

        /// <summary>
        ///     订阅配置变更事件
        /// </summary>
        private void SubscribeToConfigChanges()
        {
            if (FrameworkManager.Instance.EventBus != null)
                FrameworkManager.Instance.EventBus.SubscribeBroadcast<ConfigChangedEvent>(OnConfigChanged);
        }

        /// <summary>
        ///     处理配置变更事件
        /// </summary>
        /// <param name="evt">配置变更事件</param>
        [UnicastHandler]
        private void OnConfigChanged(ConfigChangedEvent evt)
        {
            switch (evt.Key)
            {
                case "Performance.UpdateInterval":
                    if (float.TryParse(evt.NewValue?.ToString(), out var interval)) _updateInterval = interval;

                    break;

                case "Performance.AutoPublishUpdates":
                    if (bool.TryParse(evt.NewValue?.ToString(), out var autoPublish)) _autoPublishUpdates = autoPublish;

                    break;

                case "Performance.EnableMonitoring":
                    if (bool.TryParse(evt.NewValue?.ToString(), out var enableMonitoring))
                    {
                        PerformanceMonitor.SetEnabled(enableMonitoring);
                        PublishStatusChangedEvent();
                    }

                    break;
            }
        }

        /// <summary>
        ///     发布性能数据更新事件
        /// </summary>
        private void PublishPerformanceUpdates()
        {
            var stats = PerformanceMonitor.GetAllStats();
            var eventBus = FrameworkManager.Instance.EventBus;

            foreach (var pair in stats) eventBus.Broadcast(new PerformanceDataUpdatedEvent(pair.Key, pair.Value));
        }

        /// <summary>
        ///     发布性能监控状态变更事件
        /// </summary>
        private void PublishStatusChangedEvent()
        {
            var eventBus = FrameworkManager.Instance.EventBus;
            eventBus.Broadcast(new PerformanceMonitorStatusChangedEvent(PerformanceMonitor.IsEnabled));
        }

        /// <summary>
        ///     重置所有性能统计数据
        /// </summary>
        public void ResetAllStats()
        {
            PerformanceMonitor.ResetAllStats();
            FrameworkManager.Instance.EventBus.Broadcast(new PerformanceStatsResetEvent());
        }

        /// <summary>
        ///     重置指定操作的性能统计数据
        /// </summary>
        /// <param name="operationName">操作名称</param>
        public void ResetStats(string operationName)
        {
            PerformanceMonitor.ResetStats(operationName);
            FrameworkManager.Instance.EventBus.Broadcast(new PerformanceStatsResetEvent(operationName));
        }

        /// <summary>
        ///     设置性能监控是否启用
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetMonitoringEnabled(bool enabled)
        {
            PerformanceMonitor.SetEnabled(enabled);
            PublishStatusChangedEvent();

            // 更新配置
            if (FrameworkManager.Instance.ConfigManager != null)
                FrameworkManager.Instance.ConfigManager.SetValue("Performance.EnableMonitoring", enabled);
        }
    }
}