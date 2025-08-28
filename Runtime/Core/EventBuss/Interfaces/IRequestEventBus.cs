namespace CnoomFramework.Core.EventBuss.Interfaces
{
    /// <summary>
    /// 请求‑响应（Request‑Response）总线
    /// </summary>
    public interface IRequestEventBus
    {
        TResponse Request<TRequest, TResponse>(TRequest request);
        void RegisterHandler<TRequest, TResponse>(System.Func<TRequest, TResponse> handler);
        void UnregisterHandler<TRequest, TResponse>();
    }
}