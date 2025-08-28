using System;

namespace CnoomFramework.Core.EventBuss.Handlers
{
    /// <summary>
    /// 异步执行（每帧后处理）的待处理单元
    /// </summary>
    internal sealed class PendingExecution
    {
        public PendingExecution(EventHandler handler, Type eventType, object eventData)
        {
            Handler = handler;
            EventType = eventType;
            EventData = eventData;
        }

        public EventHandler Handler { get; }
        public Type EventType { get; }
        public object EventData { get; }
    }
}