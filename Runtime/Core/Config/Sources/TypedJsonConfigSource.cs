using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace CnoomFramework.Core.Config.Sources
{
    /// <summary>
    ///     强类型JSON配置源，支持精确的类型处理和类型安全的配置访问
    /// </summary>
    public class TypedJsonConfigSource : IConfigSource
    {
        private readonly string _filePath;
        private readonly JsonSerializerSettings _serializerSettings;
        private readonly Dictionary<string, TypedConfigValue> _values = new();

        /// <summary>
        ///     创建强类型JSON配置源
        /// </summary>
        /// <param name="filePath">配置文件路径，相对于Application.persistentDataPath</param>
        /// <param name="priority">优先级</param>
        public TypedJsonConfigSource(string filePath, int priority = 15)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));

            _filePath = Path.Combine(Application.persistentDataPath, filePath);
            Priority = priority;

            // 配置JSON序列化设置
            _serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            Load();
        }

        /// <inheritdoc />
        public string Name => "TypedJson";

        /// <inheritdoc />
        public int Priority { get; }

        /// <inheritdoc />
        public bool SupportsPersistence => true;

        /// <inheritdoc />
        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            if (_values.TryGetValue(key, out var typedValue))
            {
                if (typedValue.Value is T value) return value;

                try
                {
                    // 尝试使用JSON序列化进行类型转换
                    var json = JsonConvert.SerializeObject(typedValue.Value, _serializerSettings);
                    return JsonConvert.DeserializeObject<T>(json, _serializerSettings);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to convert value for key {key} to type {typeof(T).Name}: {ex.Message}");
                }
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
                _values[key] = new TypedConfigValue
                {
                    Value = value,
                    Type = typeof(T).AssemblyQualifiedName
                };
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
                var json = JsonConvert.SerializeObject(_values, _serializerSettings);
                File.WriteAllText(_filePath, json);

                Debug.Log($"Typed config saved to {_filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save typed config to {_filePath}: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public void Load()
        {
            _values.Clear();

            if (!File.Exists(_filePath))
            {
                Debug.Log($"Typed config file {_filePath} does not exist, using empty config");
                return;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                var values =
                    JsonConvert.DeserializeObject<Dictionary<string, TypedConfigValue>>(json, _serializerSettings);

                if (values != null)
                    foreach (var pair in values)
                        _values[pair.Key] = pair.Value;

                Debug.Log($"Typed config loaded from {_filePath} with {_values.Count} items");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load typed config from {_filePath}: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAllKeys()
        {
            return _values.Keys.ToList();
        }

        /// <summary>
        ///     获取指定键的值类型
        /// </summary>
        /// <param name="key">配置键</param>
        /// <returns>值类型或null</returns>
        public Type GetValueType(string key)
        {
            if (_values.TryGetValue(key, out var typedValue) && !string.IsNullOrEmpty(typedValue.Type))
                try
                {
                    return Type.GetType(typedValue.Type);
                }
                catch
                {
                    return null;
                }

            return null;
        }

        /// <summary>
        ///     强类型配置值
        /// </summary>
        [Serializable]
        private class TypedConfigValue
        {
            public object Value { get; set; }
            public string Type { get; set; }
        }
    }
}