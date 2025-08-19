namespace CnoomFramework.Core.Events
{
    /// <summary>
    ///     配置变更事件
    /// </summary>
    public class ConfigChangedEvent
    {
        /// <summary>
        ///     创建配置变更事件
        /// </summary>
        /// <param name="key">配置键</param>
        /// <param name="newValue">新值</param>
        public ConfigChangedEvent(string key, object newValue)
        {
            Key = key;
            NewValue = newValue;
        }

        /// <summary>
        ///     配置键
        /// </summary>
        public string Key { get; }

        /// <summary>
        ///     新值
        /// </summary>
        public object NewValue { get; }
    }

    /// <summary>
    ///     配置保存事件
    /// </summary>
    public class ConfigSavedEvent
    {
    }

    /// <summary>
    ///     配置加载事件
    /// </summary>
    public class ConfigLoadedEvent
    {
    }

    /// <summary>
    ///     配置清除事件
    /// </summary>
    public class ConfigClearedEvent
    {
    }
}