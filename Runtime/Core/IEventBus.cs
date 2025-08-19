using System;

namespace CnoomFramework.Core
{
    /// <summary>
    ///     框架事件基接口
    /// </summary>
    public interface IFrameworkEvent
    {
        /// <summary>
        ///     事件时间戳
        /// </summary>
        DateTime Timestamp { get; }
    }

    /// <summary>
    ///     事件总线接口
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        ///     发布事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        void Publish<T>(T eventData) where T : class;

        /// <summary>
        ///     订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        void Subscribe<T>(Action<T> handler) where T : class;

        /// <summary>
        ///     取消订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        void Unsubscribe<T>(Action<T> handler) where T : class;

        /// <summary>
        ///     请求-响应模式
        /// </summary>
        /// <typeparam name="TRequest">请求类型</typeparam>
        /// <typeparam name="TResponse">响应类型</typeparam>
        /// <param name="request">请求数据</param>
        /// <returns>响应数据</returns>
        TResponse Request<TRequest, TResponse>(TRequest request)
            where TRequest : class
            where TResponse : class;

        /// <summary>
        ///     注册请求处理器
        /// </summary>
        /// <typeparam name="TRequest">请求类型</typeparam>
        /// <typeparam name="TResponse">响应类型</typeparam>
        /// <param name="handler">请求处理器</param>
        void RegisterRequestHandler<TRequest, TResponse>(Func<TRequest, TResponse> handler)
            where TRequest : class
            where TResponse : class;

        /// <summary>
        ///     取消注册请求处理器
        /// </summary>
        /// <typeparam name="TRequest">请求类型</typeparam>
        /// <typeparam name="TResponse">响应类型</typeparam>
        void UnregisterRequestHandler<TRequest, TResponse>()
            where TRequest : class
            where TResponse : class;

        /// <summary>
        ///     清空所有事件订阅
        /// </summary>
        void Clear();
    }
}