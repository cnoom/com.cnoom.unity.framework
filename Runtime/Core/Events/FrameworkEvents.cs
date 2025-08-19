using System;

namespace CnoomFramework.Core.Events
{
    /// <summary>
    ///     模块注册事件
    /// </summary>
    public class ModuleRegisteredEvent : IFrameworkEvent
    {
        public ModuleRegisteredEvent(string moduleName, Type moduleType)
        {
            Timestamp = DateTime.Now;
            ModuleName = moduleName;
            ModuleType = moduleType;
        }

        public string ModuleName { get; }
        public Type ModuleType { get; }
        public DateTime Timestamp { get; }
    }

    /// <summary>
    ///     模块注销事件
    /// </summary>
    public class ModuleUnregisteredEvent : IFrameworkEvent
    {
        public ModuleUnregisteredEvent(string moduleName, Type moduleType)
        {
            Timestamp = DateTime.Now;
            ModuleName = moduleName;
            ModuleType = moduleType;
        }

        public string ModuleName { get; }
        public Type ModuleType { get; }
        public DateTime Timestamp { get; }
    }

    /// <summary>
    ///     模块状态变更事件
    /// </summary>
    public class ModuleStateChangedEvent : IFrameworkEvent
    {
        public ModuleStateChangedEvent(string moduleName, ModuleState oldState, ModuleState newState)
        {
            Timestamp = DateTime.Now;
            ModuleName = moduleName;
            OldState = oldState;
            NewState = newState;
        }

        public string ModuleName { get; }
        public ModuleState OldState { get; }
        public ModuleState NewState { get; }
        public DateTime Timestamp { get; }
    }

    /// <summary>
    ///     框架初始化完成事件
    /// </summary>
    public class FrameworkInitializedEvent : IFrameworkEvent
    {
        public FrameworkInitializedEvent(int moduleCount)
        {
            Timestamp = DateTime.Now;
            ModuleCount = moduleCount;
        }

        public int ModuleCount { get; }
        public DateTime Timestamp { get; }
    }

    /// <summary>
    ///     框架关闭事件
    /// </summary>
    public class FrameworkShutdownEvent : IFrameworkEvent
    {
        public FrameworkShutdownEvent()
        {
            Timestamp = DateTime.Now;
        }

        public DateTime Timestamp { get; }
    }
}