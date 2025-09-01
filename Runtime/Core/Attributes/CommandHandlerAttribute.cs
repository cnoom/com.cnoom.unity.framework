﻿﻿﻿using System;

namespace CnoomFramework.Core.Attributes
{
    /// <summary>
    /// 标记为 **命令** 处理方法。
    /// 必须是 void Method(TCommand cmd)。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CommandHandlerAttribute : Attribute
    {
        /// <summary>
        /// 如果已经有处理器，是否用本次的覆盖（true）还是保持原有的（false）。
        /// </summary>
        public bool ReplaceIfExists { get; }

        public CommandHandlerAttribute(bool replaceIfExists = true) => ReplaceIfExists = replaceIfExists;
    }
}