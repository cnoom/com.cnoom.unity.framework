using System;
using CnoomFramework.Core.EventBuss.Interfaces;

namespace CnoomFramework.Core.EventBuss.Core
{
    /// <summary>
    /// 旧的门面类，保持原有 IEventBus 接口不变。
    /// 内部把广播、单播、请求‑响应三个 Facade 组合起来，并共享同一个 Core 实例。
    /// </summary>
    public sealed class EventBus : IEventBus
    {
        // 共享的核心（只创建一次）
        private readonly BaseEventBusCore _sharedCore;

        internal BaseEventBusCore Core => _sharedCore;

        // 三个子系统的 Facade（内部直接使用共享 core 的字段）
        private readonly IBroadcastEventBus _broadcast;
        private readonly IUnicastEventBus _unicast;
        private readonly IRequestEventBus _request;

        public EventBus()
        {
            // 统一创建 core（实际是 BaseEventBusCore 的具体实例，后面会把字段拷贝进子类）
            _sharedCore = new SharedCore(); // 此行仅为了拿到一个对象，后面不会再使用

            // 创建每个 Facade
            _broadcast = new BroadcastFacade();
            _unicast = new UnicastFacade();
            _request = new RequestFacade();

            // 把共享的字段拷贝到子类（一次性操作，后面所有子类都指向同一批数据）
            CopySharedFields(_broadcast);
            CopySharedFields(_unicast);
            CopySharedFields(_request);
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
        public void Publish<T>(T data) where T : notnull => _broadcast.Publish(data);

        public void Subscribe<T>(Action<T> handler, int priority = 1) where T : notnull
        {
            _broadcast.Subscribe(handler, priority);
        }

        public void Subscribe<T>(Action<T> h, int p = 0, bool isAsync = false) where T : notnull =>
            _broadcast.Subscribe(h, p, isAsync);

        public void Unsubscribe<T>(Action<T> h) where T : notnull => _broadcast.Unsubscribe(h);

        // ----------------- Ⅱ️⃣ 单播 API -----------------
        public void PublishUnicast<T>(T data) where T : notnull => _unicast.Publish(data);

        public void SubscribeUnicast<T>(Action<T> h, bool replace = true) where T : notnull =>
            _unicast.Subscribe(h, replace);

        public void UnsubscribeUnicast<T>() => _unicast.Unsubscribe<T>();

        // ----------------- Ⅲ️⃣ 请求‑响应 API -----------------
        public TResponse Request<TRequest, TResponse>(TRequest req) => _request.Request<TRequest, TResponse>(req);

        public void RegisterRequestHandler<TRequest, TResponse>(Func<TRequest, TResponse> h) =>
            _request.RegisterHandler(h);

        public void UnregisterRequestHandler<TRequest, TResponse>() =>
            _request.UnregisterHandler<TRequest, TResponse>();

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
    }
}