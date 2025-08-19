using System;
using System.Collections.Generic;

namespace CnoomFramework.Core.Mock
{
    /// <summary>
    ///     Mock管理器接口，负责管理模块的Mock替换和热替换
    /// </summary>
    public interface IMockManager
    {
        /// <summary>
        ///     注册一个模块的Mock实现
        /// </summary>
        /// <typeparam name="TInterface">模块接口类型</typeparam>
        /// <param name="mockImplementation">Mock实现实例</param>
        /// <param name="preserveState">是否保留原模块状态</param>
        /// <returns>是否注册成功</returns>
        bool RegisterMock<TInterface>(IModule mockImplementation, bool preserveState = false)
            where TInterface : class, IModule;

        /// <summary>
        ///     注册一个模块的Mock实现
        /// </summary>
        /// <param name="interfaceType">模块接口类型</param>
        /// <param name="mockImplementation">Mock实现实例</param>
        /// <param name="preserveState">是否保留原模块状态</param>
        /// <returns>是否注册成功</returns>
        bool RegisterMock(Type interfaceType, IModule mockImplementation, bool preserveState = false);

        /// <summary>
        ///     移除一个模块的Mock实现，恢复原始实现
        /// </summary>
        /// <typeparam name="TInterface">模块接口类型</typeparam>
        /// <returns>是否移除成功</returns>
        bool RemoveMock<TInterface>() where TInterface : class, IModule;

        /// <summary>
        ///     移除一个模块的Mock实现，恢复原始实现
        /// </summary>
        /// <param name="interfaceType">模块接口类型</param>
        /// <returns>是否移除成功</returns>
        bool RemoveMock(Type interfaceType);

        /// <summary>
        ///     获取当前所有被Mock的模块类型
        /// </summary>
        /// <returns>被Mock的模块类型列表</returns>
        IReadOnlyList<Type> GetMockedModules();

        /// <summary>
        ///     检查指定模块是否被Mock
        /// </summary>
        /// <typeparam name="TInterface">模块接口类型</typeparam>
        /// <returns>是否被Mock</returns>
        bool IsMocked<TInterface>() where TInterface : class, IModule;

        /// <summary>
        ///     检查指定模块是否被Mock
        /// </summary>
        /// <param name="interfaceType">模块接口类型</param>
        /// <returns>是否被Mock</returns>
        bool IsMocked(Type interfaceType);

        /// <summary>
        ///     清除所有Mock实现，恢复原始实现
        /// </summary>
        void ClearAllMocks();
    }
}