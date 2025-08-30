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
        /// 发送命令到目标模块
        /// </summary>
        /// <typeparam name="T">命令类型</typeparam>
        /// <param name="command">命令数据</param>
        void SendCommand<T>(T command) where T : notnull;

        /// <summary>
        /// 查询模块数据
        /// </summary>
        /// <typeparam name="TQuery">查询类型</typeparam>
        /// <typeparam name="TResponse">响应类型</typeparam>
        /// <param name="query">查询数据</param>
        /// <returns>响应数据</returns>
        TResponse Query<TQuery, TResponse>(TQuery query) where TQuery : notnull;

        /// <summary>
        /// 注册命令处理器
        /// </summary>
        /// <typeparam name="T">命令类型</typeparam>
        /// <param name="handler">命令处理函数</param>
        /// <param name="replaceIfExists">存在时是否替换</param>
        void RegisterCommandHandler<T>(Action<T> handler, bool replaceIfExists = true) where T : notnull;

        /// <summary>
        /// 注册查询处理器
        /// </summary>
        /// <typeparam name="TQuery">查询类型</typeparam>
        /// <typeparam name="TResponse">响应类型</typeparam>
        /// <param name="handler">查询处理函数</param>
        void RegisterQueryHandler<TQuery, TResponse>(Func<TQuery, TResponse> handler) where TQuery : notnull;

        /// <summary>
        /// 取消注册命令处理器
        /// </summary>
        /// <typeparam name="T">命令类型</typeparam>
        void UnregisterCommandHandler<T>() where T : notnull;

        /// <summary>
        /// 取消注册查询处理器
        /// </summary>
        /// <typeparam name="TQuery">查询类型</typeparam>
        /// <typeparam name="TResponse">响应类型</typeparam>
        void UnregisterQueryHandler<TQuery, TResponse>() where TQuery : notnull;

        /// <summary>
        ///     清空所有事件订阅
        /// </summary>
        void Clear();
    }
}