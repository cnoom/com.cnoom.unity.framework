using System;

namespace CnoomFramework.Core.EventBuss.Handlers
{
    /// <summary>
    /// 事件缓存的单元
    /// </summary>
    internal sealed class CachedEvent
    {
        public CachedEvent(Type type, object data, DateTime ts)
        {
            EventType = type;
            EventData = data;
            Timestamp = ts;
        }

        public Type EventType { get; }
        public object EventData { get; }
        public DateTime Timestamp { get; }
    }
}