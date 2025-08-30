using System;
using System.Reflection;
using UnityEngine;

namespace CnoomFramework.Core.EventBuss.Handlers
{
    /// <summary>
    /// 事件处理器抽象基类
    /// </summary>
    internal abstract class EventHandler
    {
        protected EventHandler(int priority, bool isAsync)
        {
            Priority = priority;
            IsAsync = isAsync;
        }

        public int Priority { get; protected set; }
        public bool IsAsync { get; protected set; }

        public abstract void Invoke(object eventData);
    }

    /// <summary>
    /// 使用委托的泛型处理器（Subscribe/Unsubscribe 时创建）
    /// </summary>
    internal sealed class GenericEventHandler : EventHandler
    {
        private Delegate _handler;

        public GenericEventHandler(Delegate handler, int priority, bool isAsync)
            : base(priority, isAsync) => _handler = handler;

        public string DebugName => _handler?.Method?.Name ?? "??";

        public override void Invoke(object ev) => _handler?.DynamicInvoke(ev);

        public bool EqualsDelegate<T>(Action<T> a) where T : notnull => _handler != null && _handler.Equals(a);


        public void SetHandler<T>(Action<T> handler, int priority, bool isAsync)
        {
            _handler = handler;
            Priority = priority;
            IsAsync = isAsync;
        }
    }

    /// <summary>
    /// 通过反射订阅的处理器（特性自动订阅时使用）
    /// </summary>
    internal sealed class ReflectionEventHandler : EventHandler
    {
        public ReflectionEventHandler(object target, MethodInfo method, int priority, bool isAsync)
            : base(priority, isAsync)
        {
            Target = target;
            Method = method;
        }

        public object Target { get; }
        public MethodInfo Method { get; }

        public override void Invoke(object ev)
        {
            try
            {
                Method.Invoke(Target, new[] { ev });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Reflection handler error: {ex}");
            }
        }
    }
}