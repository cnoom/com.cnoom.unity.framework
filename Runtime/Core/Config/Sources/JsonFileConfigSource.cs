using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace CnoomFramework.Core.Config.Sources
{
    /// <summary>
    ///     JSON文件配置源，支持持久化到本地文件
    /// </summary>
    public class JsonFileConfigSource : IConfigSource
    {
        private readonly string _filePath;
        private readonly Dictionary<string, object> _values = new();

        /// <summary>
        ///     创建JSON文件配置源
        /// </summary>
        /// <param name="filePath">配置文件路径，相对于Application.persistentDataPath</param>
        /// <param name="priority">优先级</param>
        public JsonFileConfigSource(string filePath, int priority = 10)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));

            _filePath = Path.Combine(Application.persistentDataPath, filePath);
            Priority = priority;
            Load();
        }

        /// <inheritdoc />
        public string Name => "JsonFile";

        /// <inheritdoc />
        public int Priority { get; }

        /// <inheritdoc />
        public bool SupportsPersistence => true;

        /// <inheritdoc />
        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            if (_values.TryGetValue(key, out var value))
                try
                {
                    if (value is T typedValue) return typedValue;

                    // 处理特殊类型
                    if (value is Dictionary<string, object> dictValue && typeof(T).IsClass && !typeof(T).IsPrimitive &&
                        typeof(T) != typeof(string))
                    {
                        // 将字典转换为JSON字符串，然后反序列化为目标类型
                        var json = JsonConvert.SerializeObject(dictValue);
                        return JsonConvert.DeserializeObject<T>(json);
                    }

                    // 尝试基本类型转换
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

            if (value == null)
                _values.Remove(key);
            else
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
            try
            {
                // 确保目录存在
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                // 将配置数据序列化为JSON
                var configData = new ConfigData
                {
                    values = _values.Select(kv => new ConfigItem
                    {
                        key = kv.Key,
                        value = kv.Value,
                        type = kv.Value?.GetType().AssemblyQualifiedName
                    }).ToList()
                };

                var json = JsonConvert.SerializeObject(configData, Formatting.Indented);
                File.WriteAllText(_filePath, json);

                Debug.Log($"Config saved to {_filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save config to {_filePath}: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public void Load()
        {
            _values.Clear();

            if (!File.Exists(_filePath))
            {
                Debug.Log($"Config file {_filePath} does not exist, using empty config");
                return;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                var configData = JsonConvert.DeserializeObject<ConfigData>(json);

                if (configData != null && configData.values != null)
                    foreach (var item in configData.values)
                    {
                        if (string.IsNullOrEmpty(item.key) || item.value == null) continue;

                        _values[item.key] = item.value;
                    }

                Debug.Log($"Config loaded from {_filePath} with {_values.Count} items");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load config from {_filePath}: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAllKeys()
        {
            return _values.Keys.ToList();
        }

        /// <summary>
        ///     配置数据类，用于JSON序列化
        /// </summary>
        [Serializable]
        private class ConfigData
        {
            public List<ConfigItem> values;
        }

        /// <summary>
        ///     配置项，用于JSON序列化
        /// </summary>
        [Serializable]
        private class ConfigItem
        {
            public string key;
            public string type;
            public object value;
        }

        /// <summary>
        ///     字典包装器，用于JSON序列化
        /// </summary>
        [Serializable]
        private class DictionaryWrapper
        {
            public Dictionary<string, object> dictionary;
        }
    }
}