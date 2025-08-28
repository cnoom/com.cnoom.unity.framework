using System;

namespace CnoomFramework.Core.Attributes
{
    /// <summary>
    /// 标记为 **单播**（一对一）事件处理方法。
    /// 必须是 void Method(TEvent ev)。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class UnicastHandlerAttribute : Attribute
    {
        /// <summary>
        /// 如果已经有处理器，是否用本次的覆盖（true）还是保持原有的（false）。
        /// </summary>
        public bool ReplaceIfExists { get; }

        public UnicastHandlerAttribute(bool replaceIfExists = true) => ReplaceIfExists = replaceIfExists;
    }
}