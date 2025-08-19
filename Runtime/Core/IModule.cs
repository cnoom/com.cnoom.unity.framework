namespace CnoomFramework.Core
{
    /// <summary>
    ///     模块状态枚举
    /// </summary>
    public enum ModuleState
    {
        /// <summary>未初始化</summary>
        Uninitialized,

        /// <summary>已初始化</summary>
        Initialized,

        /// <summary>已启动</summary>
        Started,

        /// <summary>已关闭</summary>
        Shutdown
    }

    /// <summary>
    ///     模块接口，所有框架模块必须实现此接口
    /// </summary>
    public interface IModule
    {
        /// <summary>
        ///     模块名称
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     模块当前状态
        /// </summary>
        ModuleState State { get; }

        /// <summary>
        ///     模块优先级，数值越小优先级越高
        /// </summary>
        int Priority { get; }

        /// <summary>
        ///     初始化模块
        ///     在此阶段注册事件、准备资源
        /// </summary>
        void Init();

        /// <summary>
        ///     启动模块
        ///     在此阶段可以安全发送事件，所有依赖模块已初始化完成
        /// </summary>
        void Start();

        /// <summary>
        ///     关闭模块
        ///     清理资源、自动取消事件订阅
        /// </summary>
        void Shutdown();
    }
}