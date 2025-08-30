using System;
using CnoomFramework.Core.EventBuss.Interfaces;

namespace CnoomFramework.Core.EventBuss.Core
{
    /// <summary>
    /// 事件总线适配器 - 保持原有接口兼容性，内部使用优化实现
    /// </summary>
    public sealed class EventBus : IEventBus
    {
        // 使用优化的事件总线实现
        private readonly UnicastBus _unicastBus = new UnicastBus();
        private readonly RequestBus _requestBus = new RequestBus();

        // 广播仍然使用原有实现（保持不变）
        private readonly IBroadcastEventBus _broadcast;
        private readonly BaseEventBusCore _sharedCore;
        internal BaseEventBusCore Core => _sharedCore;

        public EventBus()
        {
            _sharedCore = new SharedCore();
            _broadcast = new BroadcastFacade();

            // 复制共享字段到广播facade
            CopySharedFields(_broadcast);
        }

        // ------------- 复制共享字段（仅在构造时执行一次） -------------
        private void CopySharedFields(object target)
        {
            var srcFields = typeof(BaseEventBusCore).GetFields(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);

            foreach (var f in srcFields)
            {
                var value = f.GetValue(_sharedCore);
                f.SetValue(target, value);
            }
        }

        // ----------------- Ⅰ️⃣ 广播 API -----------------
        public void Broadcast<T>(T data) where T : notnull => _broadcast.Publish(data);

        public void SubscribeBroadcast<T>(Action<T> h, int p = 0, bool isAsync = false) where T : notnull =>
            _broadcast.Subscribe(h, p, isAsync);

        public void UnsubscribeBroadcast<T>(Action<T> h) where T : notnull => _broadcast.Unsubscribe(h);


        // ----------------- Ⅳ️⃣ 其它 -----------------
        public void Clear()
        {
            lock (_sharedCore._lockObject)
            {
                _sharedCore._eventHandlers.Clear();
                _sharedCore._requestHandlers.Clear();
                _sharedCore._cachedEvents.Clear();
                _sharedCore._pendingExecutions.Clear();
            }
        }

        public void ProcessPending(int max = int.MaxValue) => _sharedCore.ProcessPending(max);

        // ----------------- Ⅴ️⃣ 模块通信扩展 API -----------------

        /// <summary>
        /// 发送命令到目标模块
        /// </summary>
        public void SendCommand<T>(T command) where T : notnull
        {
            _unicastBus.Publish(command);
        }

        /// <summary>
        /// 查询模块数据
        /// </summary>
        public TResponse Query<TQuery, TResponse>(TQuery query) where TQuery : notnull
        {
            return _requestBus.Request<TQuery, TResponse>(query);
        }

        /// <summary>
        /// 注册命令处理器
        /// </summary>
        public void RegisterCommandHandler<T>(Action<T> handler, bool replaceIfExists = true) where T : notnull
        {
            _unicastBus.Subscribe(handler, replaceIfExists);
        }

        /// <summary>
        /// 注册查询处理器
        /// </summary>
        public void RegisterQueryHandler<TQuery, TResponse>(Func<TQuery, TResponse> handler)
            where TQuery : notnull
        {
            _requestBus.RegisterHandler(handler);
        }

        /// <summary>
        /// 取消注册命令处理器
        /// </summary>
        public void UnregisterCommandHandler<T>() where T : notnull
        {
            _unicastBus.Unsubscribe<T>();
        }

        /// <summary>
        /// 取消注册查询处理器
        /// </summary>
        public void UnregisterQueryHandler<TQuery, TResponse>() where TQuery : notnull
        {
            _requestBus.UnregisterHandler<TQuery, TResponse>();
        }
    }
}