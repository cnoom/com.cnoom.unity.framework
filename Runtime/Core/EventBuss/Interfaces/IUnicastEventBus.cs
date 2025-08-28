using System;

namespace CnoomFramework.Core.EventBuss.Interfaces
{
    /// <summary>
    /// 一对一（单播）事件总线
    /// </summary>
    public interface IUnicastEventBus
    {
        void Publish<T>(T eventData) where T : notnull;
        void Subscribe<T>(Action<T> handler, bool replaceIfExists = true) where T : notnull;
        void Unsubscribe<T>();
    }
}