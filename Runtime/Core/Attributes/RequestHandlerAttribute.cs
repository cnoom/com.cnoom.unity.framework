using System;

namespace CnoomFramework.Core.Attributes
{
    /// <summary>
    /// 标记为 **请求‑响应** 处理方法。
    /// 必须是 TResponse Method(TRequest req)。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RequestHandlerAttribute : Attribute
    {
    }
}