using System;
using System.Collections.Generic;
using CnoomFramework.Core.EventBuss.Handlers;
using CnoomFramework.Core.EventBuss.Interfaces;
using UnityEngine;
using EventHandler = CnoomFramework.Core.EventBuss.Handlers.EventHandler;

namespace CnoomFramework.Core.EventBuss.Core
{
    /// <summary>
    /// 广播（多对多）实现
    /// </summary>
    internal sealed class BroadcastFacade : BaseEventBusCore, IBroadcastEventBus
    {
        public void Publish<T>(T eventData) where T : notnull
        {
            if (eventData == null) return;
            var evType = typeof(T);
            ValidateEventContract(eventData);
            LogEvent("Publish", evType, eventData);

            var handlers = GetMatchingHandlers(evType);
            if (handlers.Count == 0)
            {
                CacheEvent(evType, eventData);
                return;
            }

            foreach (var h in handlers)
            {
                try
                {
                    if (h.IsAsync) EnqueuePending(h, evType, eventData);
                    else InvokeHandler(h, evType, eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Broadcast handling error: {ex}");
                }
            }
        }

        public void Subscribe<T>(Action<T> handler, int priority = 0, bool isAsync = false) where T : notnull
        {
            var evType = typeof(T);
            var eh = new GenericEventHandler(handler, priority, isAsync);
            lock (_lockObject)
            {
                if (!_eventHandlers.ContainsKey(evType)) _eventHandlers[evType] = new List<EventHandler>();
                _eventHandlers[evType].Add(eh);
            }

            ProcessCachedEvents(evType, ce => PublishCached(ce));
        }

        public void Unsubscribe<T>(Action<T> handler) where T : notnull
        {
            var evType = typeof(T);
            lock (_lockObject)
            {
                if (_eventHandlers.TryGetValue(evType, out var list))
                {
                    list.RemoveAll(h => h is GenericEventHandler g && g.EqualsDelegate(handler));
                    if (list.Count == 0) _eventHandlers.TryRemove(evType, out _);
                }
            }
        }

        private void PublishCached(CachedEvent ce)
        {
            var handlers = GetMatchingHandlers(ce.EventType);
            foreach (var h in handlers)
            {
                if (h.IsAsync) EnqueuePending(h, ce.EventType, ce.EventData);
                else InvokeHandler(h, ce.EventType, ce.EventData);
            }
        }
    }
}