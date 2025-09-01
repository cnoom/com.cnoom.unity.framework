using System;

namespace CnoomFramework.Core.Attributes
{
    /// <summary>
    /// 标记为 **查询‑响应** 处理方法。
    /// 必须是 TResponse Method(TQuery query)。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class QueryHandlerAttribute : Attribute
    {
    }
}