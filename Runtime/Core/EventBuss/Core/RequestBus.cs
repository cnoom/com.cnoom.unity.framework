using System;
using CnoomFramework.Core.EventBuss.Interfaces;
using UnityEngine;

namespace CnoomFramework.Core.EventBuss
{
    internal static class RequestHandlers<TRequest, TResponse>
    {
        public static Func<TRequest, TResponse> Handler;
    }

    /// <summary>
    /// 高性能请求-响应总线 - 专为模块间数据查询优化
    /// </summary>
    public class RequestBus : IRequestEventBus
    {
        /// <summary>
        /// 发送请求并获取响应
        /// </summary>
        public TResponse Request<TRequest, TResponse>(TRequest request)
        {
            if (request == null) return default;

            var handler = RequestHandlers<TRequest, TResponse>.Handler;
            if (handler != null)
            {
                try
                {
                    return handler(request);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Request handler error: {ex}");
                    return default;
                }
            }

#if UNITY_EDITOR
            Debug.LogWarning($"No handler registered for request: {typeof(TRequest).Name}->{typeof(TResponse).Name}");
#endif
            return default;
        }

        /// <summary>
        /// 注册请求处理器
        /// </summary>
        public void RegisterHandler<TRequest, TResponse>(Func<TRequest, TResponse> handler)
        {
            if (handler == null) return;

            RequestHandlers<TRequest, TResponse>.Handler = handler;

#if UNITY_EDITOR
            Debug.Log($"Registered request handler for: {typeof(TRequest).Name}->{typeof(TResponse).Name}");
#endif
        }

        /// <summary>
        /// 取消注册请求处理器
        /// </summary>
        public void UnregisterHandler<TRequest, TResponse>()
        {
            RequestHandlers<TRequest, TResponse>.Handler = null;
        }
    }
}