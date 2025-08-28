using System;
using CnoomFramework.Core.EventBuss.Interfaces;
using UnityEngine;

namespace CnoomFramework.Core.EventBuss.Core
{
    /// <summary>
    /// 请求‑响应实现
    /// </summary>
    internal sealed class RequestFacade : BaseEventBusCore, IRequestEventBus
    {
        public TResponse Request<TRequest, TResponse>(TRequest request)
        {
            if (request == null) return default;
            ValidateRequestContract<TRequest, TResponse>(request);
            var key = (typeof(TRequest), typeof(TResponse));

            if (_requestHandlers.TryGetValue(key, out var obj) && obj is Func<TRequest, TResponse> handler)
            {
                try
                {
                    var resp = handler(request);
                    ValidateResponseContract<TRequest, TResponse>(resp);
                    return resp;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Request handler error: {ex}");
                    return default;
                }
            }

            Debug.LogWarning($"No request handler for {typeof(TRequest).Name}->{typeof(TResponse).Name}");
            return default;
        }

        public void RegisterHandler<TRequest, TResponse>(Func<TRequest, TResponse> handler)
        {
            if (handler == null) return;
            var key = (typeof(TRequest), typeof(TResponse));
            _requestHandlers[key] = handler;
        }

        public void UnregisterHandler<TRequest, TResponse>()
        {
            var key = (typeof(TRequest), typeof(TResponse));
            _requestHandlers.TryRemove(key, out _);
        }
    }
}