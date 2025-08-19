using System;
using System.Collections.Generic;

namespace CnoomFramework.Core.Config
{
    /// <summary>
    ///     配置管理器接口，提供配置的读取、设置和监听功能
    /// </summary>
    public interface IConfigManager
    {
        /// <summary>
        ///     获取配置值，如果不存在则返回默认值
        /// </summary>
        /// <typeparam name="T">配置值类型</typeparam>
        /// <param name="key">配置键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>配置值或默认值</returns>
        T GetValue<T>(string key, T defaultValue = default);

        /// <summary>
        ///     设置配置值
        /// </summary>
        /// <typeparam name="T">配置值类型</typeparam>
        /// <param name="key">配置键</param>
        /// <param name="value">配置值</param>
        /// <param name="persistent">是否持久化</param>
        void SetValue<T>(string key, T value, bool persistent = true);

        /// <summary>
        ///     检查配置是否存在
        /// </summary>
        /// <param name="key">配置键</param>
        /// <returns>是否存在</returns>
        bool HasValue(string key);

        /// <summary>
        ///     移除配置
        /// </summary>
        /// <param name="key">配置键</param>
        /// <returns>是否成功移除</returns>
        bool RemoveValue(string key);

        /// <summary>
        ///     清除所有配置
        /// </summary>
        void Clear();

        /// <summary>
        ///     保存配置到持久化存储
        /// </summary>
        void Save();

        /// <summary>
        ///     从持久化存储加载配置
        /// </summary>
        void Load();

        /// <summary>
        ///     注册配置变更监听器
        /// </summary>
        /// <param name="key">配置键</param>
        /// <param name="listener">监听器回调</param>
        void RegisterChangeListener(string key, Action<string, object> listener);

        /// <summary>
        ///     注册配置变更监听器（泛型版本）
        /// </summary>
        /// <typeparam name="T">配置值类型</typeparam>
        /// <param name="key">配置键</param>
        /// <param name="listener">监听器回调</param>
        void RegisterChangeListener<T>(string key, Action<string, T> listener);

        /// <summary>
        ///     取消注册配置变更监听器
        /// </summary>
        /// <param name="key">配置键</param>
        /// <param name="listener">监听器回调</param>
        void UnregisterChangeListener(string key, Action<string, object> listener);

        /// <summary>
        ///     取消注册配置变更监听器（泛型版本）
        /// </summary>
        /// <typeparam name="T">配置值类型</typeparam>
        /// <param name="key">配置键</param>
        /// <param name="listener">监听器回调</param>
        void UnregisterChangeListener<T>(string key, Action<string, T> listener);

        /// <summary>
        ///     获取所有配置键
        /// </summary>
        /// <returns>配置键集合</returns>
        IEnumerable<string> GetAllKeys();

        /// <summary>
        ///     获取指定前缀的所有配置键
        /// </summary>
        /// <param name="prefix">前缀</param>
        /// <returns>配置键集合</returns>
        IEnumerable<string> GetKeysByPrefix(string prefix);

        /// <summary>
        ///     添加配置源
        /// </summary>
        /// <param name="configSource">配置源</param>
        void AddConfigSource(IConfigSource configSource);

        /// <summary>
        ///     移除配置源
        /// </summary>
        /// <param name="sourceName">配置源名称</param>
        /// <returns>是否成功移除</returns>
        bool RemoveConfigSource(string sourceName);

        /// <summary>
        ///     获取配置源
        /// </summary>
        /// <param name="sourceName">配置源名称</param>
        /// <returns>配置源或null</returns>
        IConfigSource GetConfigSource(string sourceName);

        /// <summary>
        ///     获取所有配置源
        /// </summary>
        /// <returns>配置源集合</returns>
        IReadOnlyList<IConfigSource> GetAllConfigSources();
    }
}