using System;

namespace CnoomFramework.Core.EventBuss.Interfaces
{
    /// <summary>
    ///     事件总线接口
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        ///     广播
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        void Broadcast<T>(T eventData) where T : notnull;

        /// <summary>
        /// 订阅广播
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">处理事件的委托</param>
        /// <param name="priority">事件处理优先级，默认为1，值越小优先级越高</param>
        /// <param name="isAsync">是否异步</param>
        void SubscribeBroadcast<T>(Action<T> handler, int priority = 1, bool isAsync = false) where T : notnull;

        /// <summary>
        ///     取消订阅广播事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        void UnsubscribeBroadcast<T>(Action<T> handler) where T : notnull;

        /// <summary>
        /// 单播
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="data">事件数据</param>
        void Unicast<T>(T data) where T : notnull;

        /// <summary>
        /// 订阅单播事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="h">处理事件的委托</param>
        /// <param name="replace">是否替换已存在的处理器，默认为true，表示如果已有相同的处理器则替换</param>
        public void SubscribeUnicast<T>(Action<T> h, bool replace = true) where T : notnull;

        /// <summary>
        /// 取消订阅单播事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        public void UnsubscribeUnicast<T>();

        /// <summary>
        ///     请求-响应模式
        /// </summary>
        /// <typeparam name="TRequest">请求类型</typeparam>
        /// <typeparam name="TResponse">响应类型</typeparam>
        /// <param name="request">请求数据</param>
        /// <returns>响应数据</returns>
        TResponse Request<TRequest, TResponse>(TRequest request);

        /// <summary>
        ///     注册请求处理器
        /// </summary>
        /// <typeparam name="TRequest">请求类型</typeparam>
        /// <typeparam name="TResponse">响应类型</typeparam>
        /// <param name="handler">请求处理器</param>
        void RegisterRequestHandler<TRequest, TResponse>(Func<TRequest, TResponse> handler);

        /// <summary>
        ///     取消注册请求处理器
        /// </summary>
        /// <typeparam name="TRequest">请求类型</typeparam>
        /// <typeparam name="TResponse">响应类型</typeparam>
        void UnregisterRequestHandler<TRequest, TResponse>();

        /// <summary>
        ///     清空所有事件订阅
        /// </summary>
        void Clear();
    }
}