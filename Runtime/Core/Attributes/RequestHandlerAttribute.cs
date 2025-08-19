using System;

namespace CnoomFramework.Core.Attributes
{
    /// <summary>
    ///     请求处理器特性，用于自动注册请求处理方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RequestHandlerAttribute : Attribute
    {
        /// <summary>
        ///     构造函数
        /// </summary>
        /// <param name="requestType">请求类型</param>
        /// <param name="responseType">响应类型</param>
        public RequestHandlerAttribute(Type requestType, Type responseType)
        {
            RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
            ResponseType = responseType ?? throw new ArgumentNullException(nameof(responseType));
        }

        /// <summary>
        ///     构造函数，通过泛型指定类型
        /// </summary>
        public RequestHandlerAttribute()
        {
            RequestType = null; // 将在运行时通过方法参数推断
            ResponseType = null;
        }

        /// <summary>
        ///     请求类型
        /// </summary>
        public Type RequestType { get; }

        /// <summary>
        ///     响应类型
        /// </summary>
        public Type ResponseType { get; }
    }
}