using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace CnoomFramework.Core.Config.Sources
{
    /// <summary>
    ///     PlayerPrefs配置源，支持持久化
    /// </summary>
    public class PlayerPrefsConfigSource : IConfigSource
    {
        private readonly string _keyPrefix;
        private readonly HashSet<string> _keys = new();

        /// <summary>
        ///     创建PlayerPrefs配置源
        /// </summary>
        /// <param name="keyPrefix">键前缀</param>
        /// <param name="priority">优先级</param>
        public PlayerPrefsConfigSource(string keyPrefix = "", int priority = 50)
        {
            _keyPrefix = keyPrefix ?? "";
            Priority = priority;
            Load();
        }

        /// <inheritdoc />
        public string Name => "PlayerPrefs";

        /// <inheritdoc />
        public int Priority { get; }

        /// <inheritdoc />
        public bool SupportsPersistence => true;

        /// <inheritdoc />
        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            var prefKey = GetPrefKey(key);

            if (!PlayerPrefs.HasKey(prefKey)) return defaultValue;

            try
            {
                if (typeof(T) == typeof(string)) return (T)(object)PlayerPrefs.GetString(prefKey);

                if (typeof(T) == typeof(int)) return (T)(object)PlayerPrefs.GetInt(prefKey);

                if (typeof(T) == typeof(float)) return (T)(object)PlayerPrefs.GetFloat(prefKey);

                if (typeof(T) == typeof(bool)) return (T)(object)(PlayerPrefs.GetInt(prefKey) != 0);

                if (typeof(T) == typeof(object))
                {
                    // 对于object类型，先获取字符串值，然后尝试推断类型
                    var stringValue = PlayerPrefs.GetString(prefKey);

                    // 尝试解析为基本类型
                    if (int.TryParse(stringValue, out var intResult)) return (T)(object)intResult;
                    if (float.TryParse(stringValue, out var floatResult)) return (T)(object)floatResult;
                    if (bool.TryParse(stringValue, out var boolResult)) return (T)(object)boolResult;

                    // 尝试解析为JSON对象
                    try
                    {
                        var jsonObj = JsonConvert.DeserializeObject(stringValue);
                        return (T)jsonObj;
                    }
                    catch
                    {
                        // 如果解析失败，则返回字符串值
                        return (T)(object)stringValue;
                    }
                }

                // 对于其他类型，尝试从JSON字符串反序列化
                var json = PlayerPrefs.GetString(prefKey);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get value for key {key} from PlayerPrefs: {ex.Message}");
                return defaultValue;
            }
        }

        /// <inheritdoc />
        public void SetValue<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            var prefKey = GetPrefKey(key);

            try
            {
                if (value is string stringValue)
                {
                    PlayerPrefs.SetString(prefKey, stringValue);
                }
                else if (value is int intValue)
                {
                    PlayerPrefs.SetInt(prefKey, intValue);
                }
                else if (value is float floatValue)
                {
                    PlayerPrefs.SetFloat(prefKey, floatValue);
                }
                else if (value is bool boolValue)
                {
                    PlayerPrefs.SetInt(prefKey, boolValue ? 1 : 0);
                }
                else if (value != null)
                {
                    // 对于其他类型，尝试序列化为JSON字符串
                    var json = JsonConvert.SerializeObject(value);
                    PlayerPrefs.SetString(prefKey, json);
                }
                else
                {
                    // 如果值为null，则移除该键
                    PlayerPrefs.DeleteKey(prefKey);
                    _keys.Remove(key);
                    return;
                }

                _keys.Add(key);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set value for key {key} to PlayerPrefs: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public bool HasValue(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            return PlayerPrefs.HasKey(GetPrefKey(key));
        }

        /// <inheritdoc />
        public bool RemoveValue(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            var prefKey = GetPrefKey(key);

            if (PlayerPrefs.HasKey(prefKey))
            {
                PlayerPrefs.DeleteKey(prefKey);
                _keys.Remove(key);
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public void Clear()
        {
            // 只清除带有前缀的键
            foreach (var key in _keys.ToList()) PlayerPrefs.DeleteKey(GetPrefKey(key));

            _keys.Clear();
        }

        /// <inheritdoc />
        public void Save()
        {
            PlayerPrefs.Save();
        }

        /// <inheritdoc />
        public void Load()
        {
            // PlayerPrefs不提供获取所有键的方法，因此我们需要维护一个键集合
            // 这里我们可以尝试从一个特殊的键中加载键列表
            var keysKey = GetPrefKey("__keys__");

            if (PlayerPrefs.HasKey(keysKey))
            {
                var keysJson = PlayerPrefs.GetString(keysKey);
                try
                {
                    var keysList = JsonConvert.DeserializeObject<KeysList>(keysJson);
                    if (keysList != null && keysList.keys != null)
                    {
                        _keys.Clear();
                        foreach (var key in keysList.keys) _keys.Add(key);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to load keys from PlayerPrefs: {ex.Message}");
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAllKeys()
        {
            return _keys.ToList();
        }

        /// <summary>
        ///     获取PlayerPrefs中使用的键
        /// </summary>
        /// <param name="key">原始键</param>
        /// <returns>带前缀的键</returns>
        private string GetPrefKey(string key)
        {
            return string.IsNullOrEmpty(_keyPrefix) ? key : $"{_keyPrefix}.{key}";
        }

        /// <summary>
        ///     保存键列表
        /// </summary>
        private void SaveKeysList()
        {
            var keysList = new KeysList { keys = _keys.ToArray() };
            var keysJson = JsonConvert.SerializeObject(keysList);
            PlayerPrefs.SetString(GetPrefKey("__keys__"), keysJson);
        }

        /// <summary>
        ///     用于序列化键列表的辅助类
        /// </summary>
        [Serializable]
        private class KeysList
        {
            public string[] keys;
        }
    }
}