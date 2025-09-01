using System;
using System.Collections.Generic;
using System.Linq;
using CnoomFramework.Core.ErrorHandling;
using CnoomFramework.Core.EventBuss.Interfaces;
using CnoomFramework.Core.Events;
using UnityEngine;

namespace CnoomFramework.Core.Config
{
    /// <summary>
    ///     配置管理器实现，支持多配置源、配置变更通知和持久化
    /// </summary>
    public class ConfigManager : IConfigManager
    {
        private readonly Dictionary<string, List<Action<string, object>>> _changeListeners = new();
        private readonly List<IConfigSource> _configSources = new();
        private readonly IEventBus _eventBus;

        /// <summary>
        ///     创建配置管理器实例
        /// </summary>
        /// <param name="eventBus">事件总线</param>
        public ConfigManager(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        /// <summary>
        ///     添加配置源
        /// </summary>
        /// <param name="configSource">配置源</param>
        public void AddConfigSource(IConfigSource configSource)
        {
            if (configSource == null) throw new ArgumentNullException(nameof(configSource));

            // 按优先级降序排列
            var index = 0;
            while (index < _configSources.Count && _configSources[index].Priority >= configSource.Priority) index++;

            _configSources.Insert(index, configSource);
            Debug.Log($"Added config source: {configSource.Name} with priority {configSource.Priority}");
        }

        /// <summary>
        ///     移除配置源
        /// </summary>
        /// <param name="sourceName">配置源名称</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveConfigSource(string sourceName)
        {
            var source = _configSources.FirstOrDefault(s => s.Name == sourceName);
            if (source != null)
            {
                _configSources.Remove(source);
                Debug.Log($"Removed config source: {sourceName}");
                return true;
            }

            return false;
        }

        /// <summary>
        ///     获取配置源
        /// </summary>
        /// <param name="sourceName">配置源名称</param>
        /// <returns>配置源或null</returns>
        public IConfigSource GetConfigSource(string sourceName)
        {
            return _configSources.FirstOrDefault(s => s.Name == sourceName);
        }

        /// <summary>
        ///     获取所有配置源
        /// </summary>
        /// <returns>配置源集合</returns>
        public IReadOnlyList<IConfigSource> GetAllConfigSources()
        {
            return _configSources.AsReadOnly();
        }

        /// <summary>
        ///     通知配置值变更
        /// </summary>
        /// <param name="key">配置键</param>
        /// <param name="newValue">新值</param>
        private void NotifyValueChanged(string key, object newValue)
        {
            // 触发监听器
            if (_changeListeners.TryGetValue(key, out var listeners))
                foreach (var listener in listeners.ToArray()) // 创建副本以防在回调中修改集合
                    try
                    {
                        listener(key, newValue);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error in config change listener for key {key}: {ex.Message}");
                    }

            // 发布配置变更事件
            _eventBus?.Broadcast(new ConfigChangedEvent(key, newValue));
        }

        #region IConfigManager Implementation

        /// <inheritdoc />
        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            // 按优先级查找配置值
            foreach (var source in _configSources)
                try
                {
                    if (source.HasValue(key)) return source.GetValue(key, defaultValue);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error getting value for key {key} from source {source.Name}: {ex.Message}");
                }

            return defaultValue;
        }

        /// <inheritdoc />
        public void SetValue<T>(string key, T value, bool persistent = true)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be empty", nameof(key));

            // 获取第一个支持持久化的配置源（如果需要持久化）
            IConfigSource targetSource = null;

            if (persistent) targetSource = _configSources.FirstOrDefault(s => s.SupportsPersistence);

            // 如果没有找到支持持久化的配置源或不需要持久化，则使用优先级最高的配置源
            if (targetSource == null) targetSource = _configSources.FirstOrDefault();

            if (targetSource == null) throw new InvalidOperationException("No config source available");

            // 获取旧值用于比较
            var oldValue = HasValue(key) ? GetValue<T>(key) : default;

            // 设置新值
            targetSource.SetValue(key, value);

            // 如果值发生变化，触发变更事件
            if (!EqualityComparer<T>.Default.Equals(oldValue, value)) NotifyValueChanged(key, value);

            // 如果需要持久化，保存配置
            if (persistent && targetSource.SupportsPersistence) SafeExecutor.Execute(() => targetSource.Save());
        }

        /// <inheritdoc />
        public bool HasValue(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            return _configSources.Any(source => source.HasValue(key));
        }

        /// <inheritdoc />
        public bool RemoveValue(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            var removed = false;

            // 从所有配置源中移除
            foreach (var source in _configSources)
                if (source.HasValue(key))
                    if (source.RemoveValue(key))
                        removed = true;

            // 如果成功移除，触发变更事件
            if (removed) NotifyValueChanged(key, null);

            return removed;
        }

        /// <inheritdoc />
        public void Clear()
        {
            // 清除所有配置源
            foreach (var source in _configSources) source.Clear();

            // 触发清除事件
            _eventBus?.Broadcast(new ConfigClearedEvent());
        }

        /// <inheritdoc />
        public void Save()
        {
            // 保存所有支持持久化的配置源
            foreach (var source in _configSources.Where(s => s.SupportsPersistence))
                try
                {
                    source.Save();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error saving config source {source.Name}: {ex.Message}");
                }

            // 触发保存事件
            _eventBus?.Broadcast(new ConfigSavedEvent());
        }

        /// <inheritdoc />
        public void Load()
        {
            // 加载所有支持持久化的配置源
            foreach (var source in _configSources.Where(s => s.SupportsPersistence))
                try
                {
                    source.Load();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error loading config source {source.Name}: {ex.Message}");
                }

            // 触发加载事件
            _eventBus?.Broadcast(new ConfigLoadedEvent());
        }

        /// <inheritdoc />
        public void RegisterChangeListener(string key, Action<string, object> listener)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            if (listener == null) throw new ArgumentNullException(nameof(listener));

            if (!_changeListeners.TryGetValue(key, out var listeners))
            {
                listeners = new List<Action<string, object>>();
                _changeListeners[key] = listeners;
            }

            if (!listeners.Contains(listener)) listeners.Add(listener);
        }

        /// <inheritdoc />
        public void RegisterChangeListener<T>(string key, Action<string, T> listener)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));

            // 包装泛型监听器为非泛型监听器
            Action<string, object> wrapperListener = (k, v) =>
            {
                if (v == null)
                    listener(k, default);
                else if (v is T typedValue)
                    listener(k, typedValue);
                else
                    try
                    {
                        // 尝试转换类型
                        var convertedValue = (T)Convert.ChangeType(v, typeof(T));
                        listener(k, convertedValue);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error converting value for key {k} to type {typeof(T).Name}: {ex.Message}");
                        listener(k, default);
                    }
            };

            RegisterChangeListener(key, wrapperListener);
        }

        /// <inheritdoc />
        public void UnregisterChangeListener(string key, Action<string, object> listener)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            if (listener == null) throw new ArgumentNullException(nameof(listener));

            if (_changeListeners.TryGetValue(key, out var listeners))
            {
                listeners.Remove(listener);

                if (listeners.Count == 0) _changeListeners.Remove(key);
            }
        }

        /// <inheritdoc />
        public void UnregisterChangeListener<T>(string key, Action<string, T> listener)
        {
            // 由于我们无法直接获取包装后的监听器，这里需要移除所有匹配的监听器
            // 这是一个简化的实现，实际应用中可能需要更精确的匹配
            if (_changeListeners.TryGetValue(key, out var listeners))
            {
                var listenersToRemove = listeners
                    .Where(l => l.Target == listener.Target && l.Method.Name == listener.Method.Name)
                    .ToList();

                foreach (var l in listenersToRemove) listeners.Remove(l);

                if (listeners.Count == 0) _changeListeners.Remove(key);
            }
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAllKeys()
        {
            // 合并所有配置源的键，去重
            return _configSources
                .SelectMany(source => source.GetAllKeys())
                .Distinct();
        }

        /// <inheritdoc />
        public IEnumerable<string> GetKeysByPrefix(string prefix)
        {
            if (prefix == null) throw new ArgumentNullException(nameof(prefix));

            // 筛选指定前缀的键
            return GetAllKeys().Where(key => key.StartsWith(prefix));
        }

        #endregion
    }
}