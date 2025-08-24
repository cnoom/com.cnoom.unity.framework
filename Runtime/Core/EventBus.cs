using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CnoomFramework.Core.Contracts;
using CnoomFramework.Core.Events;
using CnoomFramework.Core.Exceptions;
using CnoomFramework.Core.Performance;
using UnityEngine;

namespace CnoomFramework.Core
{
    /// <summary>
    ///     事件总线实现
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly Queue<CachedEvent> _cachedEvents = new();
        private readonly ConcurrentDictionary<Type, List<EventHandler>> _eventHandlers = new();
        private readonly object _lockObject = new();
        private readonly Queue<PendingExecution> _pendingExecutions = new();
        private readonly ConcurrentDictionary<(Type, Type), object> _requestHandlers = new();

        private bool _isProcessingEvents;
        private int _maxAsyncHandlersPerFrame = 64;
        private int _maxCachedEvents = 1000;

        /// <summary>
        ///     最大事件缓存条目数
        /// </summary>
        public int MaxCachedEvents
        {
            get => _maxCachedEvents;
            set => _maxCachedEvents = Math.Max(0, value);
        }

        /// <summary>
        ///     每帧最多处理的异步处理器数量
        /// </summary>
        public int MaxAsyncHandlersPerFrame
        {
            get => _maxAsyncHandlersPerFrame;
            set => _maxAsyncHandlersPerFrame = Math.Max(1, value);
        }

        /// <summary>
        ///     是否启用基类/接口继承分发
        /// </summary>
        public bool EnableInheritanceDispatch { get; set; } = true;

        /// <summary>
        ///     发布事件
        /// </summary>
        public void Publish<T>(T eventData) where T : notnull
        {
            if (eventData == null) return;

            var eventType = typeof(T);
            var operationName = $"EventBus.Publish.{eventType.Name}";

            using (PerformanceUtils.SampleScope(operationName))
            {
                // 验证事件契约
                ValidateEventContract(eventData);

                // 记录事件日志
                LogEvent("Publish", eventType, eventData);

                // 记录事件流
                EventFlowRecorder.Instance.RecordEventPublish(eventType, eventData);

                var matchingHandlers = GetMatchingHandlers(eventType);

                if (matchingHandlers.Count == 0)
                {
                    CacheEvent(eventType, eventData);
                    return;
                }

                foreach (var handler in matchingHandlers)
                    try
                    {
                        // 记录事件处理
                        if (handler is ReflectionEventHandler reflectionHandler)
                            EventFlowRecorder.Instance.RecordEventHandling(eventType, handler, reflectionHandler.Target,
                                reflectionHandler.Method.Name);
                        else if (handler is GenericEventHandler genericHandler)
                            EventFlowRecorder.Instance.RecordEventHandling(eventType, handler, null,
                                genericHandler.DebugName);

                        if (handler.IsAsync)
                            EnqueuePending(handler, eventType, eventData);
                        else
                            InvokeHandler(handler, eventType, eventData);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error handling event {eventType.Name}: {ex.Message}");
                    }
            }
        }

        /// <summary>
        ///     订阅事件
        /// </summary>
        public void Subscribe<T>(Action<T> handler, int priority) where T : notnull
        {
            if (handler == null) return;

            var eventType = typeof(T);
            var eventHandler = new GenericEventHandler(handler, priority, false);

            lock (_lockObject)
            {
                if (!_eventHandlers.ContainsKey(eventType)) _eventHandlers[eventType] = new List<EventHandler>();
                _eventHandlers[eventType].Add(eventHandler);

                // 记录事件订阅
                EventFlowRecorder.Instance.RecordEventSubscription(eventType, handler, handler.Target,
                    handler.Method.Name);
            }

            // 处理缓存的事件
            ProcessCachedEventsForSubscriberType(eventType);
        }

        /// <summary>
        ///     取消订阅事件
        /// </summary>
        public void Unsubscribe<T>(Action<T> handler) where T : notnull
        {
            if (handler == null) return;

            var eventType = typeof(T);

            lock (_lockObject)
            {
                if (_eventHandlers.TryGetValue(eventType, out var handlers))
                {
                    handlers.RemoveAll(h => h is GenericEventHandler generic && generic.EqualsDelegate(handler));

                    // 记录事件取消订阅
                    EventFlowRecorder.Instance.RecordEventUnsubscription(eventType, handler, handler.Target);

                    if (handlers.Count == 0) _eventHandlers.TryRemove(eventType, out _);
                }
            }
        }

        /// <summary>
        ///     请求-响应模式
        /// </summary>
        public TResponse Request<TRequest, TResponse>(TRequest request)
        {
            if (request == null) return default;

            var requestType = typeof(TRequest);
            var responseType = typeof(TResponse);
            var operationName = $"EventBus.Request.{requestType.Name}";

            return PerformanceUtils.Measure(operationName, () =>
            {
                // 验证请求契约
                ValidateRequestContract<TRequest, TResponse>(request);

                if (_requestHandlers.TryGetValue((requestType, responseType), out var handler))
                    try
                    {
                        if (handler is Func<TRequest, TResponse> typedHandler)
                        {
                            var response = typedHandler(request);

                            // 验证响应契约
                            ValidateResponseContract<TRequest, TResponse>(response);

                            return response;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error handling request {requestType.Name}: {ex.Message}");
                    }

                Debug.LogWarning($"No handler registered for request type {requestType.Name}");
                return default;
            });
        }

        /// <summary>
        ///     注册请求处理器
        /// </summary>
        public void RegisterRequestHandler<TRequest, TResponse>(Func<TRequest, TResponse> handler)
        {
            if (handler == null) return;

            var requestType = typeof(TRequest);
            var responseType = typeof(TResponse);
            _requestHandlers[(requestType, responseType)] = handler;
        }

        /// <summary>
        ///     取消注册请求处理器
        /// </summary>
        public void UnregisterRequestHandler<TRequest, TResponse>()
        {
            var requestType = typeof(TRequest);
            var responseType = typeof(TResponse);

            _requestHandlers.TryRemove((requestType, responseType), out _);
        }

        /// <summary>
        ///     清空所有事件订阅
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                _eventHandlers.Clear();
                _requestHandlers.Clear();
                _cachedEvents.Clear();
            }
        }

        /// <summary>
        ///     订阅事件（带优先级与异步）
        /// </summary>
        public void Subscribe<T>(Action<T> handler, int priority, bool isAsync) where T : class
        {
            if (handler == null) return;

            var eventType = typeof(T);
            var eventHandler = new GenericEventHandler(handler, priority, isAsync);

            lock (_lockObject)
            {
                if (!_eventHandlers.ContainsKey(eventType)) _eventHandlers[eventType] = new List<EventHandler>();
                _eventHandlers[eventType].Add(eventHandler);

                EventFlowRecorder.Instance.RecordEventSubscription(eventType, handler, handler.Target,
                    handler.Method.Name);
            }

            ProcessCachedEventsForSubscriberType(eventType);
        }

        /// <summary>
        ///     通过反射订阅事件（用于特性自动订阅）
        /// </summary>
        public void SubscribeByReflection(object target, MethodInfo method, Type eventType, int priority = 0,
            bool isAsync = false)
        {
            if (target == null || method == null || eventType == null) return;

            var eventHandler = new ReflectionEventHandler(target, method, priority, isAsync);

            lock (_lockObject)
            {
                if (!_eventHandlers.ContainsKey(eventType)) _eventHandlers[eventType] = new List<EventHandler>();
                _eventHandlers[eventType].Add(eventHandler);

                // 记录事件订阅
                EventFlowRecorder.Instance.RecordEventSubscription(eventType, eventHandler, target, method.Name);
            }

            // 处理缓存的事件
            ProcessCachedEventsForSubscriberType(eventType);
        }

        /// <summary>
        ///     通过反射取消订阅事件
        /// </summary>
        public void UnsubscribeByReflection(object target, Type eventType)
        {
            if (target == null || eventType == null) return;

            lock (_lockObject)
            {
                if (_eventHandlers.TryGetValue(eventType, out var handlers))
                {
                    handlers.RemoveAll(h => h is ReflectionEventHandler reflectionHandler &&
                                            reflectionHandler.Target == target);

                    // 记录事件取消订阅
                    EventFlowRecorder.Instance.RecordEventUnsubscription(eventType, null, target);

                    if (handlers.Count == 0) _eventHandlers.TryRemove(eventType, out _);
                }
            }
        }

        /// <summary>
        ///     缓存事件
        /// </summary>
        private void CacheEvent(Type eventType, object eventData)
        {
            lock (_lockObject)
            {
                if (_cachedEvents.Count >= _maxCachedEvents) _cachedEvents.Dequeue(); // 移除最旧的事件

                _cachedEvents.Enqueue(new CachedEvent(eventType, eventData, DateTime.Now));
            }
        }

        /// <summary>
        ///     处理缓存的事件：当某个订阅类型注册时，回放与该类型兼容的缓存事件
        /// </summary>
        private void ProcessCachedEventsForSubscriberType(Type subscriberEventType)
        {
            if (_isProcessingEvents) return;

            _isProcessingEvents = true;

            try
            {
                lock (_lockObject)
                {
                    var eventsToProcess = new List<CachedEvent>();
                    var remainingEvents = new Queue<CachedEvent>();

                    while (_cachedEvents.Count > 0)
                    {
                        var cachedEvent = _cachedEvents.Dequeue();
                        // 若订阅类型是事件类型的基类/接口或同类型，则匹配
                        if (subscriberEventType.IsAssignableFrom(cachedEvent.EventType))
                            eventsToProcess.Add(cachedEvent);
                        else
                            remainingEvents.Enqueue(cachedEvent);
                    }

                    // 恢复未处理的事件
                    while (remainingEvents.Count > 0) _cachedEvents.Enqueue(remainingEvents.Dequeue());

                    // 处理匹配的事件
                    foreach (var eventToProcess in eventsToProcess) PublishCachedEvent(eventToProcess);
                }
            }
            finally
            {
                _isProcessingEvents = false;
            }
        }

        /// <summary>
        ///     发布缓存的事件
        /// </summary>
        private void PublishCachedEvent(CachedEvent cachedEvent)
        {
            var operationName = $"EventBus.PublishCached.{cachedEvent.EventType.Name}";

            using (PerformanceUtils.SampleScope(operationName))
            {
                var matchingHandlers = GetMatchingHandlers(cachedEvent.EventType);
                if (matchingHandlers.Count == 0) return;

                // 记录事件流（缓存事件发布）
                EventFlowRecorder.Instance.RecordEventPublish(cachedEvent.EventType, cachedEvent.EventData);

                foreach (var handler in matchingHandlers)
                    try
                    {
                        if (handler is ReflectionEventHandler reflectionHandler)
                            EventFlowRecorder.Instance.RecordEventHandling(cachedEvent.EventType, handler,
                                reflectionHandler.Target, reflectionHandler.Method.Name);
                        else if (handler is GenericEventHandler genericHandler)
                            EventFlowRecorder.Instance.RecordEventHandling(cachedEvent.EventType, handler, null,
                                genericHandler.DebugName);

                        if (handler.IsAsync)
                            EnqueuePending(handler, cachedEvent.EventType, cachedEvent.EventData);
                        else
                            InvokeHandler(handler, cachedEvent.EventType, cachedEvent.EventData);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error handling cached event {cachedEvent.EventType.Name}: {ex.Message}");
                    }
            }
        }

        /// <summary>
        ///     记录事件日志
        /// </summary>
        private void LogEvent(string action, Type eventType, object eventData)
        {
            // 这里可以实现详细的事件日志记录
            // 为了性能考虑，可以通过配置开关控制
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (eventData is IFrameworkEvent && IFrameworkEvent.ShowInfo)
            {
                Debug.Log($"[EventBus] {action}: {eventType.Name} at {DateTime.Now:HH:mm:ss.fff}");
            }

            if (eventData is IShowInfoEvent && IShowInfoEvent.IsShow)
            {
                Debug.Log($"[EventBus] {action}: {eventType.Name} at {DateTime.Now:HH:mm:ss.fff}");
            }
#endif
        }

        /// <summary>
        ///     验证事件契约
        /// </summary>
        private void ValidateEventContract<T>(T eventData) where T : notnull
        {
            try
            {
                var contractModule =
                    FrameworkManager.Instance?.GetModule("ContractValidation") as ContractValidationModule;
                if (contractModule != null) contractModule.ValidateEvent(eventData);
            }
            catch (ContractValidationException ex)
            {
                // 契约验证异常会被抛出，由调用者处理
                throw;
            }
            catch (Exception ex)
            {
                // 其他异常记录但不抛出，避免影响事件分发
                Debug.LogError($"Error validating event contract: {ex.Message}");
            }
        }

        /// <summary>
        ///     验证请求契约
        /// </summary>
        private void ValidateRequestContract<TRequest, TResponse>(TRequest request)
        {
            try
            {
                var contractModule =
                    FrameworkManager.Instance?.GetModule("ContractValidation") as ContractValidationModule;
                if (contractModule != null) contractModule.ValidateRequest<TRequest, TResponse>(request);
            }
            catch (ContractValidationException ex)
            {
                // 契约验证异常会被抛出，由调用者处理
                throw;
            }
            catch (Exception ex)
            {
                // 其他异常记录但不抛出，避免影响请求处理
                Debug.LogError($"Error validating request contract: {ex.Message}");
            }
        }

        /// <summary>
        ///     验证响应契约
        /// </summary>
        private void ValidateResponseContract<TRequest, TResponse>(TResponse response)
        {
            try
            {
                var contractModule =
                    FrameworkManager.Instance?.GetModule("ContractValidation") as ContractValidationModule;
                if (contractModule != null) contractModule.ValidateResponse<TRequest, TResponse>(response);
            }
            catch (ContractValidationException ex)
            {
                // 契约验证异常会被抛出，由调用者处理
                throw;
            }
            catch (Exception ex)
            {
                // 其他异常记录但不抛出，避免影响响应处理
                Debug.LogError($"Error validating response contract: {ex.Message}");
            }
        }

        private void EnqueuePending(EventHandler handler, Type eventType, object eventData)
        {
            lock (_lockObject)
            {
                _pendingExecutions.Enqueue(new PendingExecution(handler, eventType, eventData));
            }
        }

        private void InvokeHandler(EventHandler handler, Type eventType, object eventData)
        {
            // 性能监控事件处理
            string handlerName;
            if (handler is ReflectionEventHandler reh)
                handlerName = $"EventHandler.{eventType.Name}.{reh.Target.GetType().Name}.{reh.Method.Name}";
            else if (handler is GenericEventHandler geh)
                handlerName = $"EventHandler.{eventType.Name}.{geh.DebugName}";
            else
                handlerName = $"EventHandler.{eventType.Name}.{handler.GetType().Name}";

            using (PerformanceUtils.SampleScope(handlerName))
            {
                handler.Invoke(eventData);
            }
        }

        /// <summary>
        ///     获取与事件类型匹配的所有处理器（支持继承/接口分发），按优先级排序
        /// </summary>
        private List<EventHandler> GetMatchingHandlers(Type eventType)
        {
            var handlers = new List<EventHandler>();
            lock (_lockObject)
            {
                // 精确类型
                if (_eventHandlers.TryGetValue(eventType, out var directHandlers)) handlers.AddRange(directHandlers);

                if (EnableInheritanceDispatch)
                    // 基类与接口类型
                    foreach (var kv in _eventHandlers)
                    {
                        var subscribedType = kv.Key;
                        if (subscribedType == eventType) continue;
                        if (subscribedType.IsAssignableFrom(eventType)) handlers.AddRange(kv.Value);
                    }
            }

            return handlers.OrderBy(h => h.Priority).ToList();
        }

        /// <summary>
        ///     每帧处理挂起的异步事件处理器
        /// </summary>
        public void ProcessPending(int maxHandlersToProcess = int.MaxValue)
        {
            var budget = Math.Min(_maxAsyncHandlersPerFrame, maxHandlersToProcess);
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
                    Debug.LogError($"Error processing async handler for event {exec.EventType.Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        ///     事件处理器基类
        /// </summary>
        private abstract class EventHandler
        {
            protected EventHandler(int priority, bool isAsync)
            {
                Priority = priority;
                IsAsync = isAsync;
            }

            public int Priority { get; }
            public bool IsAsync { get; }

            public abstract void Invoke(object eventData);
        }

        /// <summary>
        ///     泛型事件处理器
        /// </summary>
        private class GenericEventHandler : EventHandler
        {
            private readonly Delegate _handler;

            public GenericEventHandler(Delegate handler, int priority, bool isAsync) : base(priority, isAsync)
            {
                _handler = handler;
            }

            public string DebugName => _handler?.Method?.Name ?? "UnknownHandler";

            public override void Invoke(object eventData)
            {
                _handler?.DynamicInvoke(eventData);
            }

            public bool EqualsDelegate<T>(Action<T> action) where T : notnull
            {
                return _handler != null && _handler.Equals(action);
            }
        }

        /// <summary>
        ///     反射事件处理器
        /// </summary>
        private class ReflectionEventHandler : EventHandler
        {
            public ReflectionEventHandler(object target, MethodInfo method, int priority, bool isAsync) : base(priority,
                isAsync)
            {
                Target = target;
                Method = method;
            }

            public object Target { get; }
            public MethodInfo Method { get; }

            public override void Invoke(object eventData)
            {
                try
                {
                    Method.Invoke(Target, new[] { eventData });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error invoking reflection event handler: {ex.Message}");
                }
            }
        }

        /// <summary>
        ///     缓存事件数据
        /// </summary>
        private class CachedEvent
        {
            public CachedEvent(Type eventType, object eventData, DateTime timestamp)
            {
                EventType = eventType;
                EventData = eventData;
                Timestamp = timestamp;
            }

            public Type EventType { get; }
            public object EventData { get; }
            public DateTime Timestamp { get; }
        }

        /// <summary>
        ///     待处理的异步执行单元
        /// </summary>
        private class PendingExecution
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
}