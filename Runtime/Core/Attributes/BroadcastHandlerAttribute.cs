using System;

namespace CnoomFramework.Core.Attributes
{
    /// <summary>
    /// 标记为 **广播**（一对多）事件处理方法。
    /// 必须是 void Method(TEvent ev)。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class BroadcastHandlerAttribute : Attribute
    {
        public int Priority { get; }
        public bool IsAsync { get; }

        public BroadcastHandlerAttribute(int priority = 0, bool isAsync = false)
        {
            Priority = priority;
            IsAsync = isAsync;
        }
    }
}