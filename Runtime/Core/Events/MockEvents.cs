using System;

namespace CnoomFramework.Core.Mock
{
    /// <summary>
    ///     模块被Mock事件
    /// </summary>
    public class ModuleMockedEvent : IFrameworkEvent
    {
        public ModuleMockedEvent(Type moduleType, IModule mockImplementation)
        {
            ModuleType = moduleType;
            MockImplementation = mockImplementation;
            Timestamp = DateTime.Now;
        }

        /// <summary>
        ///     被Mock的模块接口类型
        /// </summary>
        public Type ModuleType { get; }

        /// <summary>
        ///     Mock实现实例
        /// </summary>
        public IModule MockImplementation { get; }

        /// <summary>
        ///     事件时间戳
        /// </summary>
        public DateTime Timestamp { get; }
    }

    /// <summary>
    ///     模块Mock被移除事件
    /// </summary>
    public class ModuleUnmockedEvent : IFrameworkEvent
    {
        public ModuleUnmockedEvent(Type moduleType, IModule originalImplementation)
        {
            ModuleType = moduleType;
            OriginalImplementation = originalImplementation;
            Timestamp = DateTime.Now;
        }

        /// <summary>
        ///     被恢复的模块接口类型
        /// </summary>
        public Type ModuleType { get; }

        /// <summary>
        ///     原始实现实例
        /// </summary>
        public IModule OriginalImplementation { get; }

        /// <summary>
        ///     事件时间戳
        /// </summary>
        public DateTime Timestamp { get; }
    }
}