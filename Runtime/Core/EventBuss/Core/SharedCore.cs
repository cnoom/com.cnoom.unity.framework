namespace CnoomFramework.Core.EventBuss.Core
{
    /// <summary>
    /// 真正的运行时实例。它不再添加任何新逻辑，只是把 BaseEventBusCore 的所有成员暴露出来，
    /// 供广播 / 单播 / 请求‑响应三个 Facade 共享同一份数据。
    /// </summary>
    internal sealed class SharedCore : BaseEventBusCore
    {
        // 这里不需要额外代码，一切都继承自基类。
    }
}