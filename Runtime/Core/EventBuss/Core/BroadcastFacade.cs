using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CnoomFramework.Core.EventBuss.Handlers;
using CnoomFramework.Core.EventBuss.Interfaces;
using UnityEngine;
using EventHandler = CnoomFramework.Core.EventBuss.Handlers.EventHandler;

namespace CnoomFramework.Core.EventBuss.Core
{
    /// <summary>
    /// 高性能广播实现（一对多）
    /// 优化点：无锁设计、委托缓存、对象池
    /// </summary>
    internal sealed class BroadcastFacade : IBroadcastEventBus
    {
        // 使用线程安全的字典，避免锁竞争
        private readonly ConcurrentDictionary<Type, List<EventHandler>> _eventHandlers = new();
        
        // 事件缓存（限制大小避免内存泄漏）
        private readonly ConcurrentQueue<CachedEvent> _cachedEvents = new();
        private const int MaxCachedEvents = 100;

        // 对象池用于重用 GenericEventHandler
        private static readonly ConcurrentBag<GenericEventHandler> HandlerPool = new();

        public void Publish<T>(T eventData) where T : notnull
        {
            if (eventData == null) return;
            
            var evType = typeof(T);
            LogEvent("Publish", evType, eventData);

            // 获取处理器（无锁读取）
            if (_eventHandlers.TryGetValue(evType, out var handlers) && handlers.Count > 0)
            {
                InvokeHandlers(handlers, evType, eventData);
            }
            else
            {
                CacheEvent(evType, eventData);
            }
        }

        public void Subscribe<T>(Action<T> handler, int priority = 0, bool isAsync = false) where T : notnull
        {
            var evType = typeof(T);
            var eventHandler = GetOrCreateHandler(handler, priority, isAsync);

            // 使用原子操作添加处理器
            var handlers = _eventHandlers.GetOrAdd(evType, _ => new List<EventHandler>());
            lock (handlers) // 只锁单个类型的处理器列表
            {
                handlers.Add(eventHandler);
                handlers.Sort((a, b) => b.Priority.CompareTo(a.Priority)); // 按优先级排序
            }

            // 处理缓存的事件
            ProcessCachedEvents(evType);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : notnull
        {
            var evType = typeof(T);
            if (_eventHandlers.TryGetValue(evType, out var handlers))
            {
                lock (handlers)
                {
                    handlers.RemoveAll(h => 
                        h is GenericEventHandler genericHandler && 
                        genericHandler.EqualsDelegate(handler));
                    
                    if (handlers.Count == 0)
                    {
                        _eventHandlers.TryRemove(evType, out _);
                    }
                }
            }
        }

        private void InvokeHandlers(List<EventHandler> handlers, Type eventType, object eventData)
        {
            // 复制处理器列表以避免并发修改问题
            EventHandler[] handlersCopy;
            lock (handlers)
            {
                handlersCopy = handlers.ToArray();
            }

            foreach (var handler in handlersCopy)
            {
                try
                {
                    if (handler.IsAsync)
                    {
                        EnqueueAsyncHandler(handler, eventType, eventData);
                    }
                    else
                    {
                        handler.Invoke(eventData);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Broadcast handling error: {ex}");
                }
            }
        }

        private void EnqueueAsyncHandler(EventHandler handler, Type eventType, object eventData)
        {
            // 使用线程池或专用异步处理器
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    handler.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Async broadcast handling error: {ex}");
                }
            });
        }

        private void CacheEvent(Type eventType, object eventData)
        {
            // 限制缓存大小
            while (_cachedEvents.Count >= MaxCachedEvents)
            {
                _cachedEvents.TryDequeue(out _);
            }
            
            _cachedEvents.Enqueue(new CachedEvent(eventType, eventData,DateTime.Now));
        }

        private void ProcessCachedEvents(Type eventType)
        {
            // 处理特定类型的所有缓存事件
            var eventsToProcess = new List<CachedEvent>();
            
            foreach (var cachedEvent in _cachedEvents)
            {
                if (cachedEvent.EventType == eventType)
                {
                    eventsToProcess.Add(cachedEvent);
                }
            }

            foreach (var cachedEvent in eventsToProcess)
            {
                if (_eventHandlers.TryGetValue(eventType, out var handlers))
                {
                    InvokeHandlers(handlers, cachedEvent.EventType, cachedEvent.EventData);
                }
            }
        }

        private GenericEventHandler GetOrCreateHandler<T>(Action<T> handler, int priority, bool isAsync)
        {
            // 尝试从对象池获取
            if (HandlerPool.TryTake(out GenericEventHandler pooledHandler))
            {
                pooledHandler.SetHandler(handler, priority, isAsync);
                return pooledHandler;
            }
            
            // 创建新的处理器
            return new GenericEventHandler(handler, priority, isAsync);
        }

        private void ReturnHandlerToPool(GenericEventHandler handler)
        {
            HandlerPool.Add(handler);
        }

        private void LogEvent(string operation, Type eventType, object eventData)
        {
            #if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[EventBus] {operation}: {eventType.Name} - {eventData}");
            #endif
        }
    }
}