using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace CnoomFramework.Core.EventBuss
{
    /// <summary>
    /// 高性能单播事件总线 - 专为模块间命令通信优化
    /// </summary>
    public class OptimizedUnicastBus
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
        public void Subscribe<T>(Action<T> handler) where T : notnull
        {
            if (handler == null) return;
            
            _handlers[typeof(T)] = handler;
            
#if UNITY_EDITOR
            Debug.Log($"Registered unicast handler for: {typeof(T).Name}");
#endif
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