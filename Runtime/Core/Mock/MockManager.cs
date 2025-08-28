using System;
using System.Collections.Generic;
using System.Linq;
using CnoomFramework.Utils;

namespace CnoomFramework.Core.Mock
{
    /// <summary>
    ///     Mock管理器实现，负责管理模块的Mock替换和热替换
    /// </summary>
    public class MockManager : IMockManager
    {
        private readonly FrameworkManager _frameworkManager;
        private readonly Dictionary<Type, IModule> _mockModules = new();
        private readonly Dictionary<Type, IModule> _originalModules = new();

        public MockManager(FrameworkManager frameworkManager)
        {
            _frameworkManager = frameworkManager ?? throw new ArgumentNullException(nameof(frameworkManager));
        }

        /// <inheritdoc />
        public bool RegisterMock<TInterface>(IModule mockImplementation, bool preserveState = false)
            where TInterface : class, IModule
        {
            return RegisterMock(typeof(TInterface), mockImplementation, preserveState);
        }

        /// <inheritdoc />
        public bool RegisterMock(Type interfaceType, IModule mockImplementation, bool preserveState = false)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));

            if (mockImplementation == null)
                throw new ArgumentNullException(nameof(mockImplementation));

            if (!interfaceType.IsInterface)
                throw new ArgumentException("指定的类型必须是接口", nameof(interfaceType));

            if (!typeof(IModule).IsAssignableFrom(interfaceType))
                throw new ArgumentException("指定的接口必须继承自IModule", nameof(interfaceType));

            if (!interfaceType.IsInstanceOfType(mockImplementation))
                throw new ArgumentException($"Mock实现必须实现接口 {interfaceType.Name}", nameof(mockImplementation));

            // 检查模块是否已注册
            var originalModule = _frameworkManager.GetModule(interfaceType);
            if (originalModule == null)
            {
                FrameworkLogger.LogWarning($"无法替换模块 {interfaceType.Name}，因为它尚未注册");
                return false;
            }

            // 保存原始模块
            if (!_originalModules.ContainsKey(interfaceType)) _originalModules[interfaceType] = originalModule;

            // 如果模块已启动，先关闭它
            if (originalModule.State == ModuleState.Started) _frameworkManager.ShutdownModule(originalModule);

            // 替换模块
            _mockModules[interfaceType] = mockImplementation;
            _frameworkManager.ReplaceModule(interfaceType, mockImplementation);

            // 如果需要保留状态，可以在这里实现状态转移逻辑
            if (preserveState) TransferState(originalModule, mockImplementation);

            // 初始化并启动Mock模块
            _frameworkManager.InitializeModule(mockImplementation);
            _frameworkManager.StartModule(mockImplementation);

            // 发布模块Mock事件
            _frameworkManager.EventBus.Broadcast(new ModuleMockedEvent(interfaceType, mockImplementation));

            FrameworkLogger.LogInfo($"成功替换模块 {interfaceType.Name} 为Mock实现");
            return true;
        }

        /// <inheritdoc />
        public bool RemoveMock<TInterface>() where TInterface : class, IModule
        {
            return RemoveMock(typeof(TInterface));
        }

        /// <inheritdoc />
        public bool RemoveMock(Type interfaceType)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));

            // 检查模块是否被Mock
            if (!_originalModules.TryGetValue(interfaceType, out var originalModule))
            {
                FrameworkLogger.LogWarning($"模块 {interfaceType.Name} 未被Mock，无法恢复");
                return false;
            }

            // 获取当前Mock模块
            var mockModule = _frameworkManager.GetModule(interfaceType);
            if (mockModule == null)
            {
                FrameworkLogger.LogWarning($"无法找到当前的Mock模块 {interfaceType.Name}");
                return false;
            }

            // 如果模块已启动，先关闭它
            if (mockModule.State == ModuleState.Started) _frameworkManager.ShutdownModule(mockModule);

            // 恢复原始模块
            _frameworkManager.ReplaceModule(interfaceType, originalModule);

            // 如果原始模块之前是启动状态，重新启动它
            if (originalModule.State != ModuleState.Started)
            {
                _frameworkManager.InitializeModule(originalModule);
                _frameworkManager.StartModule(originalModule);
            }

            // 清理记录
            _originalModules.Remove(interfaceType);
            _mockModules.Remove(interfaceType);

            // 发布模块恢复事件
            _frameworkManager.EventBus.Broadcast(new ModuleUnmockedEvent(interfaceType, originalModule));

            FrameworkLogger.LogInfo($"成功恢复模块 {interfaceType.Name} 的原始实现");
            return true;
        }

        /// <inheritdoc />
        public IReadOnlyList<Type> GetMockedModules()
        {
            return _originalModules.Keys.ToList();
        }

        /// <inheritdoc />
        public bool IsMocked<TInterface>() where TInterface : class, IModule
        {
            return IsMocked(typeof(TInterface));
        }

        /// <inheritdoc />
        public bool IsMocked(Type interfaceType)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));

            return _originalModules.ContainsKey(interfaceType);
        }

        /// <inheritdoc />
        public void ClearAllMocks()
        {
            // 创建一个临时列表，避免在迭代过程中修改集合
            var mockedTypes = new List<Type>(_originalModules.Keys);

            foreach (var interfaceType in mockedTypes) RemoveMock(interfaceType);

            _originalModules.Clear();
            _mockModules.Clear();

            FrameworkLogger.LogInfo("已清除所有Mock实现");
        }

        /// <summary>
        ///     在模块之间转移状态（可根据具体需求实现）
        /// </summary>
        private void TransferState(IModule source, IModule target)
        {
            // 这里可以实现状态转移逻辑
            // 例如，通过反射复制公共属性值，或调用特定的状态转移方法

            // 简单示例：如果模块实现了IStatefulModule接口，则调用其状态转移方法
            if (source is IStatefulModule sourceStateful && target is IStatefulModule targetStateful)
                targetStateful.ImportState(sourceStateful.ExportState());
        }
    }
}