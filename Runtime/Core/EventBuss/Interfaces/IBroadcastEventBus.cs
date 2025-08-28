using System;

namespace CnoomFramework.Core.EventBuss.Interfaces
{
    /// <summary>
    /// 一对多（广播）事件总线
    /// </summary>
    public interface IBroadcastEventBus
    {
        void Publish<T>(T eventData) where T : notnull;
        void Subscribe<T>(Action<T> handler, int priority = 0, bool isAsync = false) where T : notnull;
        void Unsubscribe<T>(Action<T> handler) where T : notnull;
    }
}