using System;
using System.Collections.Generic;
using CnoomFramework.Core.EventBuss.Core;
using CnoomFramework.Core.EventBuss.Interfaces;
using UnityEngine;

namespace CnoomFramework.Core.EventBuss
{
    /// <summary>
    /// 高性能请求-响应总线 - 专为模块间数据查询优化
    /// </summary>
    internal class RequestBus : BaseEventBusCore, IRequestEventBus
    {
        // ✅ 使用基类的 _requestHandlers 字段替代静态类
        public TResponse Request<TRequest, TResponse>(TRequest request)
        {
            var key = (typeof(TRequest), typeof(TResponse));
            if (_requestHandlers.TryGetValue(key, out var handler))
            {
                return ((Func<TRequest, TResponse>)handler)(request);
            }

            return default;
        }

        public void RegisterHandler<TRequest, TResponse>(Func<TRequest, TResponse> handler)
        {
            if (handler == null) return;
            var key = (typeof(TRequest), typeof(TResponse));
            _requestHandlers[key] = handler;
#if UNITY_EDITOR
            Debug.Log($"Registered request handler for: {typeof(TRequest).Name}->{typeof(TResponse).Name}");
#endif
        }

        /// <summary>
        /// 取消注册请求处理器
        /// </summary>
        public void UnregisterHandler<TRequest, TResponse>()
        {
            var key = (typeof(TRequest), typeof(TResponse));
            _requestHandlers.Remove(key, out object o);
        }
    }
}