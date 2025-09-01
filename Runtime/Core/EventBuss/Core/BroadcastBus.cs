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
    internal sealed class BroadcastBus : BaseEventBusCore,IBroadcastEventBus
    {
        
        // 对象池用于重用 GenericEventHandler
        private static readonly ConcurrentBag<GenericEventHandler> HandlerPool = new();

        public void Publish<T>(T eventData) where T : notnull
        {
            if (eventData == null) return;

            var evType = typeof(T);
            LogEvent("Publish", evType, eventData);

            // 使用 GetMatchingHandlers 获取正确排序的处理器，保证优先级正确
            var handlers = GetMatchingHandlers(evType);
            if (handlers.Count > 0)
            {
                InvokeHandlers(handlers, evType, eventData);
            }
            else
            {
                CacheEvent(evType, eventData);
            }
        }
        
        // 建议使用基类的 ProcessCachedEvents 方法
        public void Subscribe<T>(Action<T> handler, int priority = 0, bool isAsync = false) where T : notnull
        {
            var evType = typeof(T);
            var eventHandler = GetOrCreateHandler(handler, priority, isAsync);
    
            var handlers = _eventHandlers.GetOrAdd(evType, _ => new List<EventHandler>());
            lock (handlers)
            {
                handlers.Add(eventHandler);
                // 使用升序排列，与 BaseEventBusCore.GetMatchingHandlers 保持一致（值越小优先级越高）
                handlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }
    
            ProcessCachedEvents(evType, cachedEvent => 
                InvokeHandlers(GetMatchingHandlers(cachedEvent.EventType), 
                    cachedEvent.EventType, cachedEvent.EventData));
        }

        public void Unsubscribe<T>(Action<T> handler) where T : notnull
        {
            var evType = typeof(T);
            if (_eventHandlers.TryGetValue(evType, out var handlers))
            {
                lock (handlers)
                {
                    handlers.RemoveAll(h =>
                        {
                            if (h is not GenericEventHandler genericHandler || !genericHandler.EqualsDelegate(handler))
                                return false;
                            ReturnHandlerToPool(genericHandler);
                            return true;

                        }
                    );
                    if (handlers.Count == 0)
                    {
                        _eventHandlers.TryRemove(evType, out _);
                    }
                }
            }
        }

        private void InvokeHandlers(List<EventHandler> handlers, Type eventType, object eventData)
        {
            // handlers 已经是从 GetMatchingHandlers 返回的正确排序列表，直接使用
            foreach (var handler in handlers)
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
    }
}