using System;

namespace CnoomFramework.Core.Attributes
{
    /// <summary>
    ///     事件订阅特性，用于自动注册事件处理方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class SubscribeEventAttribute : Attribute
    {
        /// <summary>
        ///     构造函数
        /// </summary>
        /// <param name="eventType">事件类型</param>
        public SubscribeEventAttribute(Type eventType)
        {
            EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        }

        /// <summary>
        ///     构造函数，通过泛型指定事件类型
        /// </summary>
        public SubscribeEventAttribute()
        {
            EventType = null; // 将在运行时通过方法参数推断
        }

        /// <summary>
        ///     事件类型
        /// </summary>
        public Type EventType { get; }

        /// <summary>
        ///     优先级，数值越小优先级越高
        /// </summary>
        public int Priority { get; set; } = 1;

        /// <summary>
        ///     是否异步处理
        /// </summary>
        public bool IsAsync { get; set; } = false;
    }
}