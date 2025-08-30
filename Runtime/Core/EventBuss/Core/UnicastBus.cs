using System;
using System.Collections.Concurrent;
using CnoomFramework.Core.EventBuss.Interfaces;
using UnityEngine;

namespace CnoomFramework.Core.EventBuss
{
    /// <summary>
    /// 高性能单播事件总线 - 专为模块间命令通信优化
    /// </summary>
    public class UnicastBus : IUnicastEventBus
    {
        private readonly ConcurrentDictionary<Type, Delegate> _handlers = new();

        /// <summary>
        /// 发布单播事件
        /// </summary>
        public void Publish<T>(T data) where T : notnull
        {
            if (data == null) return;

            if (_handlers.TryGetValue(typeof(T), out var handler))
            {
                try
                {
                    ((Action<T>)handler)?.Invoke(data);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Unicast handler error: {ex}");
                }
            }
#if UNITY_EDITOR
            else
            {
                Debug.LogWarning($"No handler registered for unicast event: {typeof(T).Name}");
            }
#endif
        }

        /// <summary>
        /// 订阅单播事件
        /// </summary>
        public void Subscribe<T>(Action<T> handler, bool replaceIfExists = true) where T : notnull
        {
            if (handler == null) return;
            if (replaceIfExists || !_handlers.ContainsKey(typeof(T)))
            {
                _handlers[typeof(T)] = handler;
#if UNITY_EDITOR
                Debug.Log($"Registered unicast handler for: {typeof(T).Name}");
#endif
            }
            else
            {
                Debug.Log($"存在处理者!跳过本次注册!");
            }
        }

        /// <summary>
        /// 取消订阅单播事件
        /// </summary>
        public void Unsubscribe<T>() where T : notnull
        {
            _handlers.TryRemove(typeof(T), out _);
        }

        /// <summary>
        /// 清空所有订阅
        /// </summary>
        public void Clear()
        {
            _handlers.Clear();
        }
    }
}