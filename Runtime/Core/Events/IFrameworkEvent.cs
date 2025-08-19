using System;

namespace CnoomFramework.Core
{
    /// <summary>
    ///     框架事件基接口
    /// </summary>
    public interface IFrameworkEvent
    {
        /// <summary>
        ///     事件时间戳
        /// </summary>
        DateTime Timestamp { get; }
    }
}