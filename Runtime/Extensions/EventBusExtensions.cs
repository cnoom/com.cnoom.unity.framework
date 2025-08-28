using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using CnoomFramework.Core.Attributes;
using CnoomFramework.Core.EventBuss.Core;
using CnoomFramework.Core.EventBuss.Interfaces;
using UnityEngine;

namespace CnoomFramework.Extensions
{
    namespace CnoomFramework.Core
    {
        /// <summary>
        /// 为任意对象提供 “一次性把所有标记好的方法注册到 EventBus / 一次性注销” 的扩展。
        /// </summary>
        public static class EventBusExtensions
        {
            // 为每个对象记录它在 EventBus 中的所有注册信息（用于后续注销）
            private static readonly ConditionalWeakTable<object, List<RegInfo>> _registry = new();

            #region ==== 注册 ====

            public static void RegisterHandlers(this IEventBus bus, object target)
            {
                if (bus == null) throw new ArgumentNullException(nameof(bus));
                if (target == null) throw new ArgumentNullException(nameof(target));

                var type = target.GetType();
                var methods = type.GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.FlattenHierarchy);

                var regList = new List<RegInfo>();

                foreach (var method in methods)
                {
                    // ---------- 1. Broadcast ----------
                    var bcAttr = method.GetCustomAttribute<BroadcastHandlerAttribute>(true);
                    if (bcAttr != null)
                    {
                        RegisterBroadcast(bus, target, method, bcAttr, regList);
                        continue;
                    }

                    // ---------- 2. Unicast ----------
                    var ucAttr = method.GetCustomAttribute<UnicastHandlerAttribute>(true);
                    if (ucAttr != null)
                    {
                        RegisterUnicast(bus, target, method, ucAttr, regList);
                        continue;
                    }

                    // ---------- 3. Request ----------
                    var rqAttr = method.GetCustomAttribute<RequestHandlerAttribute>(true);
                    if (rqAttr != null)
                    {
                        RegisterRequest(bus, target, method, regList);
                    }
                }

                // 把本对象的注册信息保存起来，供 Unregister 使用
                if (regList.Count > 0) _registry.Add(target, regList);
            }

            private static void RegisterBroadcast(IEventBus bus, object target, MethodInfo method,
                BroadcastHandlerAttribute attr, List<RegInfo> regList)
            {
                // 参数检查：必须只有一个参数且返回 void
                var ps = method.GetParameters();
                if (ps.Length != 1 || method.ReturnType != typeof(void))
                    throw new InvalidOperationException(
                        $"[BroadcastHandler] 方法 {method.DeclaringType?.FullName}.{method.Name} 必须是 void 方法且仅有一个参数。");

                var evType = ps[0].ParameterType;
                var delegateType = typeof(Action<>).MakeGenericType(evType);
                var handler = Delegate.CreateDelegate(delegateType, target, method, throwOnBindFailure: true);

                // 调用 EventBus.Broadcast.Subscribe<T>(handler, priority, isAsync)
                Type type = typeof(IEventBus);
                var methodBus = type.GetMethod(nameof(EventBus.SubscribeBroadcast));
                var subscribe = methodBus?.MakeGenericMethod(evType);
                subscribe?.Invoke(bus, new object[] { handler, attr.Priority, attr.IsAsync });

                // 记录用于后面注销
                var unsubscribe = typeof(EventBus).GetMethod(nameof(EventBus.UnsubscribeBroadcast),
                        new[] { typeof(Action<>).MakeGenericType(evType) })
                    ?.MakeGenericMethod(evType);
                regList.Add(new RegInfo { UnsubscribeMethod = unsubscribe, Handler = handler });
            }

            private static void RegisterUnicast(IEventBus bus, object target, MethodInfo method,
                UnicastHandlerAttribute attr, List<RegInfo> regList)
            {
                var ps = method.GetParameters();
                if (ps.Length != 1 || method.ReturnType != typeof(void))
                    throw new InvalidOperationException(
                        $"[UnicastHandler] 方法 {method.DeclaringType?.FullName}.{method.Name} 必须是 void 方法且仅有一个参数。");

                var evType = ps[0].ParameterType;
                var delegateType = typeof(Action<>).MakeGenericType(evType);
                var handler = Delegate.CreateDelegate(delegateType, target, method, throwOnBindFailure: true);

                // EventBus.SubscribeUnicast<T>(handler, replaceIfExists)
                var methodBus = typeof(EventBus).GetMethod(nameof(EventBus.SubscribeUnicast));

                var subscribe = methodBus?.MakeGenericMethod(evType);
                subscribe?.Invoke(bus, new object[] { handler, attr.ReplaceIfExists });

                // 注销：EventBus.UnsubscribeUnicast<T>()
                var unsubscribe = typeof(EventBus).GetMethod(nameof(EventBus.UnsubscribeUnicast),
                        Type.EmptyTypes)
                    ?.MakeGenericMethod(evType);
                regList.Add(new RegInfo { UnsubscribeMethod = unsubscribe, Handler = null });
            }

            private static void RegisterRequest(IEventBus bus, object target, MethodInfo method,
                List<RegInfo> regList)
            {
                // 必须是 TResponse Method(TRequest req)
                var ps = method.GetParameters();
                if (ps.Length != 1)
                    throw new InvalidOperationException(
                        $"[RequestHandler] 方法 {method.DeclaringType?.FullName}.{method.Name} 必须恰好有一个参数。");

                var requestType = ps[0].ParameterType;
                var responseType = method.ReturnType;
                if (responseType == typeof(void))
                    throw new InvalidOperationException(
                        $"[RequestHandler] 方法 {method.DeclaringType?.FullName}.{method.Name} 必须有返回值（请求的响应）。");

                var delegateType = typeof(Func<,>).MakeGenericType(requestType, responseType);
                var handler = Delegate.CreateDelegate(delegateType, target, method, throwOnBindFailure: true);

                // EventBus.RegisterRequestHandler<TReq,TResp>(handler)
                var methodBus = typeof(EventBus).GetMethod(nameof(EventBus.RegisterRequestHandler));
                var register = methodBus?.MakeGenericMethod(requestType, responseType);
                register?.Invoke(bus, new object[] { handler });

                // 注销：EventBus.UnregisterRequestHandler<TReq,TResp>()
                var unregister = typeof(EventBus).GetMethod(nameof(EventBus.UnregisterRequestHandler),
                        Type.EmptyTypes)?
                    .MakeGenericMethod(requestType, responseType);
                regList.Add(new RegInfo { UnsubscribeMethod = unregister, Handler = null });
            }

            #endregion

            #region ==== 注销 ====

            /// <summary>
            /// 依据 RegisterHandlers 时保存的登记信息，一次性把目标对象的所有
            /// 事件/请求 处理器全部注销。
            /// </summary>
            public static void UnregisterHandlers(this IEventBus bus, object target)
            {
                if (bus == null) throw new ArgumentNullException(nameof(bus));
                if (target == null) throw new ArgumentNullException(nameof(target));

                if (!_registry.TryGetValue(target, out var list)) return;

                foreach (var info in list)
                {
                    try
                    {
                        // 对于 Broadcast / Unicast 的 Unsubscribe，需要把 delegate 传进去；
                        // 对于 Request 的 Unregister，只需要调用方法本身（参数为 null）。
                        if (info.Handler != null)
                            info.UnsubscribeMethod.Invoke(bus, new object[] { info.Handler });
                        else
                            info.UnsubscribeMethod.Invoke(bus, null);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"EventBus auto‑unregister error: {ex}");
                    }
                }

                _registry.Remove(target);
            }

            #endregion

            // -----------------------------------------------------------------
            // 记录每一次注册时需要的撤销信息
            // -----------------------------------------------------------------
            private class RegInfo
            {
                public MethodInfo UnsubscribeMethod; // EventBus 中对应的 Unsubscribe 方法
                public Delegate Handler; // 对应的 Delegate（Broadcast/Unicast 时需要）
            }
        }
    }
}