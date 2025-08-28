using System;
using System.Collections.Generic;
using CnoomFramework.Core.EventBuss.Handlers;
using CnoomFramework.Core.EventBuss.Interfaces;
using UnityEngine;
using EventHandler = CnoomFramework.Core.EventBuss.Handlers.EventHandler;

namespace CnoomFramework.Core.EventBuss.Core
{
    /// <summary>
    /// 单播（只能有唯一处理器）实现
    /// </summary>
    internal sealed class UnicastFacade : BaseEventBusCore, IUnicastEventBus
    {
        public void Publish<T>(T eventData) where T : notnull
        {
            if (eventData == null) return;
            var evType = typeof(T);
            ValidateEventContract(eventData);
            LogEvent("Publish", evType, eventData);

            var handler = GetSingleHandler(evType);
            if (handler == null)
            {
                CacheEvent(evType, eventData);
                return;
            }

            try
            {
                if (handler.IsAsync) EnqueuePending(handler, evType, eventData);
                else InvokeHandler(handler, evType, eventData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unicast handling error: {ex}");
            }
        }

        public void Subscribe<T>(Action<T> handler, bool replaceIfExists = true) where T : notnull
        {
            var evType = typeof(T);
            var eh = new GenericEventHandler(handler, priority: 0, isAsync: false);
            lock (_lockObject)
            {
                if (_eventHandlers.TryGetValue(evType, out var list) && list.Count > 0)
                {
                    if (!replaceIfExists)
                        throw new InvalidOperationException($"Unicast handler for {evType.Name} already exists.");
                    list.Clear();
                }
                else
                {
                    _eventHandlers[evType] = new List<EventHandler>();
                }

                _eventHandlers[evType].Add(eh);
            }

            ProcessCachedEvents(evType, ce => PublishCached(ce));
        }

        public void Unsubscribe<T>()
        {
            var evType = typeof(T);
            lock (_lockObject) _eventHandlers.TryRemove(evType, out _);
        }

        // ---------- 私有帮助 ----------
        private EventHandler GetSingleHandler(Type evType)
        {
            lock (_lockObject)
            {
                if (_eventHandlers.TryGetValue(evType, out var list) && list.Count > 0)
                    return list[0];
            }

            return null;
        }

        private void PublishCached(CachedEvent ce)
        {
            var handler = GetSingleHandler(ce.EventType);
            if (handler == null) return;
            if (handler.IsAsync) EnqueuePending(handler, ce.EventType, ce.EventData);
            else InvokeHandler(handler, ce.EventType, ce.EventData);
        }
    }
}