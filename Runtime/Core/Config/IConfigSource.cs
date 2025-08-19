using System.Collections.Generic;

namespace CnoomFramework.Core.Config
{
    /// <summary>
    ///     配置源接口，定义配置的存储和读取方式
    /// </summary>
    public interface IConfigSource
    {
        /// <summary>
        ///     配置源名称
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     配置源优先级，数值越大优先级越高
        /// </summary>
        int Priority { get; }

        /// <summary>
        ///     是否支持持久化
        /// </summary>
        bool SupportsPersistence { get; }

        /// <summary>
        ///     获取配置值
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
        void SetValue<T>(string key, T value);

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
        ///     获取所有配置键
        /// </summary>
        /// <returns>配置键集合</returns>
        IEnumerable<string> GetAllKeys();
    }
}