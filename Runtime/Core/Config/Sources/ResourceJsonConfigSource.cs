using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace CnoomFramework.Core.Config.Sources
{
    /// <summary>
    ///     资源JSON配置源，通过Unity资源系统加载JSON配置
    /// </summary>
    public class ResourceJsonConfigSource : IConfigSource
    {
        private readonly string _resourcePath;
        private readonly JsonSerializerSettings _serializerSettings;
        private readonly Dictionary<string, object> _values = new();

        /// <summary>
        ///     创建资源JSON配置源
        /// </summary>
        /// <param name="resourcePath">资源路径，相对于Resources文件夹</param>
        /// <param name="priority">优先级</param>
        public ResourceJsonConfigSource(string resourcePath, int priority = 20)
        {
            if (string.IsNullOrEmpty(resourcePath)) throw new ArgumentNullException(nameof(resourcePath));

            _resourcePath = resourcePath;
            Priority = priority;

            // 配置JSON序列化设置
            _serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };

            Load();
        }

        /// <summary>
        ///     是否有未保存的更改
        /// </summary>
        public bool IsDirty { get; private set; }

        /// <inheritdoc />
        public string Name => "ResourceJson";

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

                    // 尝试使用JSON序列化进行类型转换
                    var json = JsonConvert.SerializeObject(value, _serializerSettings);
                    return JsonConvert.DeserializeObject<T>(json, _serializerSettings);
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

            IsDirty = true;
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

            var result = _values.Remove(key);
            if (result) IsDirty = true;
            return result;
        }

        /// <inheritdoc />
        public void Clear()
        {
            _values.Clear();
            IsDirty = true;
        }

        /// <inheritdoc />
        public void Save()
        {
            // 资源配置源不支持保存回资源文件
            Debug.LogWarning("ResourceJsonConfigSource does not support saving back to resource files.");

            // 如果需要，可以在这里添加保存到PlayerPrefs或其他位置的逻辑
        }

        /// <inheritdoc />
        public void Load()
        {
            _values.Clear();

            try
            {
                // 从Resources加载TextAsset
                var textAsset = Resources.Load<TextAsset>(_resourcePath);
                if (textAsset == null)
                {
                    Debug.LogWarning($"Resource not found at path: {_resourcePath}");
                    return;
                }

                // 解析JSON
                var configData =
                    JsonConvert.DeserializeObject<Dictionary<string, object>>(textAsset.text, _serializerSettings);
                if (configData != null)
                    foreach (var pair in configData)
                        _values[pair.Key] = pair.Value;

                IsDirty = false;
                Debug.Log($"Config loaded from resource {_resourcePath} with {_values.Count} items");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load config from resource {_resourcePath}: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAllKeys()
        {
            return _values.Keys.ToList();
        }

        /// <summary>
        ///     重新加载配置
        /// </summary>
        public void Reload()
        {
            if (IsDirty) Debug.LogWarning("Reloading will discard all unsaved changes.");

            Load();
        }

        /// <summary>
        ///     获取自定义资源加载器
        /// </summary>
        /// <param name="customLoader">自定义加载函数</param>
        /// <returns>配置源实例</returns>
        public static ResourceJsonConfigSource WithCustomLoader(Func<string, string> customLoader, string resourcePath,
            int priority = 20)
        {
            var source = new ResourceJsonConfigSource(resourcePath, priority);
            source.SetCustomLoader(customLoader);
            return source;
        }

        /// <summary>
        ///     设置自定义资源加载器
        /// </summary>
        /// <param name="customLoader">自定义加载函数</param>
        public void SetCustomLoader(Func<string, string> customLoader)
        {
            if (customLoader == null) throw new ArgumentNullException(nameof(customLoader));

            try
            {
                // 使用自定义加载器加载JSON文本
                var jsonText = customLoader(_resourcePath);
                if (string.IsNullOrEmpty(jsonText))
                {
                    Debug.LogWarning($"Custom loader returned empty text for path: {_resourcePath}");
                    return;
                }

                // 解析JSON
                _values.Clear();
                var configData =
                    JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonText, _serializerSettings);
                if (configData != null)
                    foreach (var pair in configData)
                        _values[pair.Key] = pair.Value;

                IsDirty = false;
                Debug.Log($"Config loaded using custom loader for {_resourcePath} with {_values.Count} items");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load config using custom loader for {_resourcePath}: {ex.Message}");
            }
        }
    }
}