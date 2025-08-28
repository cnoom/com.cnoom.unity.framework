using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CnoomFramework.Core.Contracts;
using CnoomFramework.Core.EventBuss.Handlers;
using CnoomFramework.Core.Events;
using CnoomFramework.Core.Exceptions;
using CnoomFramework.Core.Performance;
using UnityEngine;
using EventHandler = CnoomFramework.Core.EventBuss.Handlers.EventHandler;

namespace CnoomFramework.Core.EventBuss.Core
{
    /// <summary>
    /// 所有总线共享的核心实现（数据结构、公共工具、性能监控、合约校验）。
    /// 子类只负责自己的业务逻辑。
    /// </summary>
    internal abstract class BaseEventBusCore
    {
        // ------------ 共享的字段（子类直接访问） ------------
        protected internal readonly ConcurrentDictionary<Type, List<EventHandler>> _eventHandlers = new();
        protected internal readonly ConcurrentDictionary<(Type, Type), object> _requestHandlers = new();
        protected internal readonly Queue<CachedEvent> _cachedEvents = new();
        protected internal readonly Queue<PendingExecution> _pendingExecutions = new();
        protected internal readonly object _lockObject = new();

        protected internal bool IsProcessingEvents;
        protected internal int MaxAsyncHandlersPerFrame = 64;
        protected internal int MaxCachedEvents = 1000;
        protected internal bool EnableInheritanceDispatch { get; set; } = true;

        // ------------ 公共工具 ------------

        #region Invoke / Enqueue / Match / Cache / ProcessCached

        protected internal void EnqueuePending(EventHandler handler, Type eventType, object eventData)
        {
            lock (_lockObject) _pendingExecutions.Enqueue(new PendingExecution(handler, eventType, eventData));
        }

        protected internal void InvokeHandler(EventHandler handler, Type eventType, object eventData)
        {
            string name = handler switch
            {
                ReflectionEventHandler r => $"EH.{eventType.Name}.{r.Target.GetType().Name}.{r.Method.Name}",
                GenericEventHandler g => $"EH.{eventType.Name}.{g.DebugName}",
                _ => $"EH.{eventType.Name}.{handler.GetType().Name}"
            };
            using (PerformanceUtils.SampleScope(name))
            {
                handler.Invoke(eventData);
            }
        }

        protected internal List<EventHandler> GetMatchingHandlers(Type eventType)
        {
            var result = new List<EventHandler>();
            lock (_lockObject)
            {
                if (_eventHandlers.TryGetValue(eventType, out var direct)) result.AddRange(direct);

                if (EnableInheritanceDispatch)
                {
                    foreach (var kv in _eventHandlers)
                    {
                        var sub = kv.Key;
                        if (sub != eventType && sub.IsAssignableFrom(eventType))
                            result.AddRange(kv.Value);
                    }
                }
            }

            return result.OrderBy(h => h.Priority).ToList();
        }

        protected internal void CacheEvent(Type eventType, object eventData)
        {
            lock (_lockObject)
            {
                if (_cachedEvents.Count >= MaxCachedEvents) _cachedEvents.Dequeue();
                _cachedEvents.Enqueue(new CachedEvent(eventType, eventData, DateTime.Now));
            }
        }

        protected internal void ProcessCachedEvents(Type subscriberEventType, Action<CachedEvent> publishCached)
        {
            if (IsProcessingEvents) return;
            IsProcessingEvents = true;
            try
            {
                lock (_lockObject)
                {
                    var toPublish = new List<CachedEvent>();
                    var remain = new Queue<CachedEvent>();

                    while (_cachedEvents.Count > 0)
                    {
                        var ce = _cachedEvents.Dequeue();
                        if (subscriberEventType.IsAssignableFrom(ce.EventType))
                            toPublish.Add(ce);
                        else
                            remain.Enqueue(ce);
                    }

                    while (remain.Count > 0) _cachedEvents.Enqueue(remain.Dequeue());

                    foreach (var ce in toPublish) publishCached(ce);
                }
            }
            finally
            {
                IsProcessingEvents = false;
            }
        }

        #endregion

        #region 合约校验 & 日志（保持原有行为）

        protected internal void ValidateEventContract<T>(T ev) where T : notnull
        {
            try
            {
                var module = FrameworkManager.Instance?.GetModule("ContractValidation") as ContractValidationModule;
                module?.ValidateEvent(ev);
            }
            catch (ContractValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Event contract error: {ex}");
            }
        }

        protected internal void ValidateRequestContract<TReq, TResp>(TReq req)
        {
            try
            {
                var module = FrameworkManager.Instance?.GetModule("ContractValidation") as ContractValidationModule;
                module?.ValidateRequest<TReq, TResp>(req);
            }
            catch (ContractValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Request contract error: {ex}");
            }
        }

        protected internal void ValidateResponseContract<TReq, TResp>(TResp resp)
        {
            try
            {
                var module = FrameworkManager.Instance?.GetModule("ContractValidation") as ContractValidationModule;
                module?.ValidateResponse<TReq, TResp>(resp);
            }
            catch (ContractValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Response contract error: {ex}");
            }
        }

        protected internal void LogEvent(string action, Type evType, object evData)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (evData is IFrameworkEvent fe && IFrameworkEvent.ShowInfo)
                Debug.Log($"[EventBus] {action}: {evType.Name} @ {DateTime.Now:HH:mm:ss.fff}");
            if (evData is IShowInfoEvent si && IShowInfoEvent.IsShow)
                Debug.Log($"[EventBus] {action}: {evType.Name} @ {DateTime.Now:HH:mm:ss.fff}");
#endif
        }

        #endregion

        #region 异步执行统一入口

        public void ProcessPending(int maxHandlersToProcess = int.MaxValue)
        {
            var budget = Math.Min(MaxAsyncHandlersPerFrame, maxHandlersToProcess);
            while (budget-- > 0)
            {
                PendingExecution exec = null;
                lock (_lockObject)
                {
                    if (_pendingExecutions.Count > 0) exec = _pendingExecutions.Dequeue();
                }

                if (exec == null) break;

                try
                {
                    InvokeHandler(exec.Handler, exec.EventType, exec.EventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Async handler error: {ex}");
                }
            }
        }

        #endregion
    }
}