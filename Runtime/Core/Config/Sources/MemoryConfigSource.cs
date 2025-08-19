using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CnoomFramework.Core.Config.Sources
{
    /// <summary>
    ///     内存配置源，不支持持久化
    /// </summary>
    public class MemoryConfigSource : IConfigSource
    {
        private readonly Dictionary<string, object> _values = new();

        /// <summary>
        ///     创建内存配置源
        /// </summary>
        /// <param name="priority">优先级</param>
        public MemoryConfigSource(int priority = 100)
        {
            Priority = priority;
        }

        /// <inheritdoc />
        public string Name => "Memory";

        /// <inheritdoc />
        public int Priority { get; }

        /// <inheritdoc />
        public bool SupportsPersistence => false;

        /// <inheritdoc />
        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            if (_values.TryGetValue(key, out var value))
                try
                {
                    if (value is T typedValue) return typedValue;

                    // 尝试转换类型
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to convert value for key {key} to type {typeof(T).Name}: {ex.Message}");
                }

            return defaultValue;
        }

        /// <inheritdoc />
        public void SetValue<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            _values[key] = value;
        }

        /// <inheritdoc />
        public bool HasValue(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            return _values.ContainsKey(key);
        }

        /// <inheritdoc />
        public bool RemoveValue(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            return _values.Remove(key);
        }

        /// <inheritdoc />
        public void Clear()
        {
            _values.Clear();
        }

        /// <inheritdoc />
        public void Save()
        {
            // 内存配置源不支持持久化，无需实现
        }

        /// <inheritdoc />
        public void Load()
        {
            // 内存配置源不支持持久化，无需实现
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAllKeys()
        {
            return _values.Keys.ToList();
        }
    }
}