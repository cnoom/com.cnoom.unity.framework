using System;
using System.Collections.Generic;
using System.Reflection;
using CnoomFramework.Core.Attributes;
using CnoomFramework.Utils;

namespace CnoomFramework.Core
{
    /// <summary>
    ///     自动注册管理器，负责处理事件订阅和请求处理器的自动注册
    /// </summary>
    public static class AutoRegistrationManager
    {
        private static readonly Dictionary<object, List<EventSubscriptionInfo>> _eventSubscriptions = new();

        private static readonly Dictionary<object, List<RequestHandlerInfo>> _requestHandlers = new();

        /// <summary>
        ///     为目标对象自动注册事件订阅和请求处理器
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="eventBus">事件总线</param>
        public static void RegisterTarget(object target, IEventBus eventBus)
        {
            if (target == null || eventBus == null) return;

            try
            {
                RegisterEventHandlers(target, eventBus);
                RegisterRequestHandlers(target, eventBus);

                FrameworkLogger.LogDebug($"Auto-registration completed for {target.GetType().Name}");
            }
            catch (Exception ex)
            {
                FrameworkLogger.LogError($"Failed to auto-register {target.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        ///     为目标对象取消注册所有自动注册的内容
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="eventBus">事件总线</param>
        public static void UnregisterTarget(object target, IEventBus eventBus)
        {
            if (target == null || eventBus == null) return;

            try
            {
                UnregisterEventHandlers(target, eventBus);
                UnregisterRequestHandlers(target, eventBus);

                FrameworkLogger.LogDebug($"Auto-unregistration completed for {target.GetType().Name}");
            }
            catch (Exception ex)
            {
                FrameworkLogger.LogError($"Failed to auto-unregister {target.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        ///     注册事件处理器
        /// </summary>
        private static void RegisterEventHandlers(object target, IEventBus eventBus)
        {
            var methods = target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var subscriptions = new List<EventSubscriptionInfo>();

            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes<SubscribeEventAttribute>();
                foreach (var attribute in attributes)
                {
                    var subscription = RegisterEventHandler(target, method, attribute, eventBus);
                    if (subscription != null) subscriptions.Add(subscription);
                }
            }

            if (subscriptions.Count > 0) _eventSubscriptions[target] = subscriptions;
        }

        /// <summary>
        ///     注册单个事件处理器
        /// </summary>
        private static EventSubscriptionInfo RegisterEventHandler(object target, MethodInfo method,
            SubscribeEventAttribute attribute, IEventBus eventBus)
        {
            var parameters = method.GetParameters();
            if (parameters.Length != 1)
            {
                FrameworkLogger.LogError(
                    $"Event handler method {method.Name} in {target.GetType().Name} must have exactly one parameter.");
                return null;
            }

            var eventType = attribute.EventType ?? parameters[0].ParameterType;

            // 通过EventBus的反射订阅方法注册事件处理器
            if (eventBus is EventBus concreteEventBus)
            {
                concreteEventBus.SubscribeByReflection(target, method, eventType, attribute.Priority,
                    attribute.IsAsync);

                var subscription = new EventSubscriptionInfo(target, method, eventType, attribute.Priority);
                FrameworkLogger.LogDebug(
                    $"Registered event handler: {target.GetType().Name}.{method.Name} for {eventType.Name}");
                return subscription;
            }

            return null;
        }

        /// <summary>
        ///     注册请求处理器
        /// </summary>
        private static void RegisterRequestHandlers(object target, IEventBus eventBus)
        {
            var methods = target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var handlers = new List<RequestHandlerInfo>();

            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<RequestHandlerAttribute>();
                if (attribute != null)
                {
                    var handler = RegisterRequestHandler(target, method, attribute, eventBus);
                    if (handler != null) handlers.Add(handler);
                }
            }

            if (handlers.Count > 0) _requestHandlers[target] = handlers;
        }

        /// <summary>
        ///     注册单个请求处理器
        /// </summary>
        private static RequestHandlerInfo RegisterRequestHandler(object target, MethodInfo method,
            RequestHandlerAttribute attribute, IEventBus eventBus)
        {
            var parameters = method.GetParameters();
            if (parameters.Length != 1)
            {
                FrameworkLogger.LogError(
                    $"Request handler method {method.Name} in {target.GetType().Name} must have exactly one parameter.");
                return null;
            }

            var requestType = attribute.RequestType ?? parameters[0].ParameterType;
            var responseType = attribute.ResponseType ?? method.ReturnType;

            if (responseType == typeof(void))
            {
                FrameworkLogger.LogError(
                    $"Request handler method {method.Name} in {target.GetType().Name} must have a return type.");
                return null;
            }

            try
            {
                // 创建委托并注册
                var delegateType = typeof(Func<,>).MakeGenericType(requestType, responseType);
                var handlerDelegate = Delegate.CreateDelegate(delegateType, target, method);

                // 通过反射调用EventBus的RegisterRequestHandler方法
                var registerMethod = eventBus.GetType().GetMethod("RegisterRequestHandler")
                    ?.MakeGenericMethod(requestType, responseType);

                if (registerMethod != null)
                {
                    registerMethod.Invoke(eventBus, new object[] { handlerDelegate });

                    var handler = new RequestHandlerInfo(target, method, requestType, responseType);
                    FrameworkLogger.LogDebug(
                        $"Registered request handler: {target.GetType().Name}.{method.Name} for {requestType.Name} -> {responseType.Name}");
                    return handler;
                }
            }
            catch (Exception ex)
            {
                FrameworkLogger.LogError($"Failed to register request handler {method.Name}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        ///     取消注册事件处理器
        /// </summary>
        private static void UnregisterEventHandlers(object target, IEventBus eventBus)
        {
            if (_eventSubscriptions.TryGetValue(target, out var subscriptions))
            {
                if (eventBus is EventBus concreteEventBus)
                    foreach (var subscription in subscriptions)
                        concreteEventBus.UnsubscribeByReflection(target, subscription.EventType);

                _eventSubscriptions.Remove(target);
                FrameworkLogger.LogDebug(
                    $"Unregistered {subscriptions.Count} event handlers for {target.GetType().Name}");
            }
        }

        /// <summary>
        ///     取消注册请求处理器
        /// </summary>
        private static void UnregisterRequestHandlers(object target, IEventBus eventBus)
        {
            if (_requestHandlers.TryGetValue(target, out var handlers))
            {
                foreach (var handler in handlers)
                    try
                    {
                        // 通过反射调用EventBus的UnregisterRequestHandler方法
                        var unregisterMethod = eventBus.GetType().GetMethod("UnregisterRequestHandler")
                            ?.MakeGenericMethod(handler.RequestType, handler.ResponseType);

                        unregisterMethod?.Invoke(eventBus, null);
                    }
                    catch (Exception ex)
                    {
                        FrameworkLogger.LogError(
                            $"Failed to unregister request handler {handler.Method.Name}: {ex.Message}");
                    }

                _requestHandlers.Remove(target);
                FrameworkLogger.LogDebug($"Unregistered {handlers.Count} request handlers for {target.GetType().Name}");
            }
        }

        /// <summary>
        ///     获取目标对象的事件订阅信息
        /// </summary>
        public static IReadOnlyList<EventSubscriptionInfo> GetEventSubscriptions(object target)
        {
            return _eventSubscriptions.TryGetValue(target, out var subscriptions)
                ? subscriptions.AsReadOnly()
                : new List<EventSubscriptionInfo>().AsReadOnly();
        }

        /// <summary>
        ///     获取目标对象的请求处理器信息
        /// </summary>
        public static IReadOnlyList<RequestHandlerInfo> GetRequestHandlers(object target)
        {
            return _requestHandlers.TryGetValue(target, out var handlers)
                ? handlers.AsReadOnly()
                : new List<RequestHandlerInfo>().AsReadOnly();
        }

        /// <summary>
        ///     清理所有注册信息
        /// </summary>
        public static void Clear()
        {
            _eventSubscriptions.Clear();
            _requestHandlers.Clear();
        }
    }

    /// <summary>
    ///     事件订阅信息
    /// </summary>
    public class EventSubscriptionInfo
    {
        public EventSubscriptionInfo(object target, MethodInfo method, Type eventType, int priority)
        {
            Target = target;
            Method = method;
            EventType = eventType;
            Priority = priority;
        }

        public object Target { get; }
        public MethodInfo Method { get; }
        public Type EventType { get; }
        public int Priority { get; }
    }

    /// <summary>
    ///     请求处理器信息
    /// </summary>
    public class RequestHandlerInfo
    {
        public RequestHandlerInfo(object target, MethodInfo method, Type requestType, Type responseType)
        {
            Target = target;
            Method = method;
            RequestType = requestType;
            ResponseType = responseType;
        }

        public object Target { get; }
        public MethodInfo Method { get; }
        public Type RequestType { get; }
        public Type ResponseType { get; }
    }
}