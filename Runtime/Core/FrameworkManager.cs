using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CnoomFramework.Core.Attributes;
using CnoomFramework.Core.Config;
using CnoomFramework.Core.Config.Sources;
using CnoomFramework.Core.Contracts;
using CnoomFramework.Core.ErrorHandling;
using CnoomFramework.Core.EventBuss.Core;
using CnoomFramework.Core.EventBuss.Interfaces;
using CnoomFramework.Core.Events;
using CnoomFramework.Core.Exceptions;
using CnoomFramework.Core.Mock;
using CnoomFramework.Core.Performance;
using CnoomFrameWork.Singleton;
using UnityEngine;
using UnityEngine.Serialization;

namespace CnoomFramework.Core
{
    /// <summary>
    ///     框架管理器，核心单例类
    /// </summary>
    public class FrameworkManager : PersistentMonoSingleton<FrameworkManager>
    {
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool enableDebugLog = true;
        [SerializeField] private int maxCachedEvents = 1000;

        private readonly Dictionary<Type, IModule> _moduleDict = new();
        private readonly Dictionary<string, IModule> _moduleDictByName = new();
        private readonly List<IModule> _sortedModules = new();
        private readonly List<IModule> _modules = new();
        private readonly List<IUpdateModule> _updateModules = new();
        private ConfigManager _configManager;

        private bool _eventBusEnableInheritance = true;
        private int _eventBusMaxAsyncPerFrame = 64;
        private int _eventBusMaxCached = 1000;
        private bool _isShuttingDown;
        private bool _isFrameworkInitialized;

        /// <summary>
        ///     事件总线
        /// </summary>
        public IEventBus EventBus { get; private set; }

        /// <summary>
        ///     错误恢复管理器
        /// </summary>
        public ErrorRecoveryManager ErrorRecoveryManager { get; private set; }

        /// <summary>
        ///     配置管理器
        /// </summary>
        public IConfigManager ConfigManager => _configManager;

        /// <summary>
        ///     Mock管理器
        /// </summary>
        public IMockManager MockManager { get; private set; }

        /// <summary>
        ///     已注册的模块数量
        /// </summary>
        public int ModuleCount => _modules.Count;

        /// <summary>
        ///     获取所有模块
        /// </summary>
        public IReadOnlyList<IModule> Modules => _modules.AsReadOnly();

        /// <summary>
        ///     框架是否已初始化
        /// </summary>
        public new bool IsInitialized => _isFrameworkInitialized;

        protected override void OnInitialized()
        {
            Debug.Log("[FrameworkManager] OnInitialized 被调用");

            if (autoInitialize)
            {
                Debug.Log("[FrameworkManager] 执行自动初始化");
                Initialize();
            }
            else
            {
                Debug.Log("[FrameworkManager] 自动初始化已禁用");
            }
        }

        private void Update()
        {
            // 跳过非运行状态或正在关闭的情况
            if (!Application.isPlaying || _isShuttingDown)
                return;

            // 调度事件总线异步队列
            if (EventBus != null && EventBus is EventBus concreteEventBus)
                concreteEventBus.ProcessPending();

            foreach (IUpdateModule module in _updateModules)
            {
                module.Update();
            }
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                OnApplicationResumed();
            else
                OnApplicationPaused();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                OnApplicationPaused();
            else
                OnApplicationResumed();
        }

        /// <summary>
        ///     初始化框架
        /// </summary>
        public void Initialize()
        {
            if (_isFrameworkInitialized)
            {
                Debug.LogWarning("FrameworkManager 已经初始化。");
                return;
            }

            try
            {
                Debug.Log("正在初始化 CnoomFramework...");

                // 初始化事件总线
                var eventBus = new EventBus();
                EventBus = eventBus;

                // 初始化错误恢复管理器
                InitializeErrorRecoveryManager();

                // 初始化配置管理器
                InitializeConfigManager();

                // 初始化Mock管理器
                InitializeMockManager();

                // 从配置设置事件总线参数
                ApplyEventBusConfig();

                // 自动发现并注册模块
                AutoDiscoverModules();

                // 解析依赖关系并排序模块
                ResolveDependencies();

                // 标记框架核心组件初始化完成，允许模块初始化
                _isFrameworkInitialized = true;

                // 初始化所有模块
                InitializeModules();

                // 启动所有模块
                StartModules();

                // 清除排序集合
                _sortedModules.Clear();

                // 发布框架初始化完成事件
                EventBus.Broadcast(new FrameworkInitializedEvent(_moduleDict.Count));

                Debug.Log($"CnoomFramework 初始化成功，共 {_moduleDict.Count} 个模块。");
            }
            catch (Exception ex)
            {
                Debug.LogError($"CnoomFramework 初始化失败: {ex.Message}");
                Debug.LogError($"详细异常信息: {ex}");
                throw;
            }
        }

        /// <summary>
        ///     关闭框架
        /// </summary>
        public void Shutdown()
        {
            if (_isShuttingDown || !_isFrameworkInitialized) return;

            _isShuttingDown = true;

            try
            {
                Debug.Log("关闭 CnoomFramework...");

                // 保存配置
                _configManager?.Save();

                // 发布框架关闭事件
                EventBus?.Broadcast(new FrameworkShutdownEvent());

                // 反向关闭所有模块（使用_modules而不是_sortedModules，因为_sortedModules在初始化后被清空了）
                for (var i = _modules.Count - 1; i >= 0; i--)
                {
                    var module = _modules[i];
                    var result = SafeExecutor.ExecuteWithResult(() => module.Shutdown(), module);
                    if (!result.IsSuccess)
                    {
                        var moduleException = new ModuleException(module.Name, "SHUTDOWN_FAILED",
                            $"模块 [{module.Name}] 关闭失败: {result.ErrorMessage}", result.Exception);

                        // 在关闭过程中，即使恢复失败也要继续关闭其他模块
                        ErrorRecoveryManager?.HandleException(moduleException, module);
                    }
                }

                // 清理事件总线
                EventBus?.Clear();

                // 清理模块集合
                _moduleDict.Clear();
                _moduleDictByName.Clear();
                _sortedModules.Clear();
                _modules.Clear();

                // 重置框架初始化标志
                _isFrameworkInitialized = false;

                Debug.Log("CnoomFramework 关闭完成。");

                // 根据运行环境选择正确的销毁方法
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    // 在Editor模式下使用DestroyImmediate
                    DestroyImmediate(gameObject);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"框架关闭过程中出错: {ex.Message}");
            }
            finally
            {
                _isShuttingDown = false;
            }
        }

        /// <summary>
        ///     初始化Mock管理器
        /// </summary>
        private void InitializeMockManager()
        {
            try
            {
                MockManager = new MockManager(this);
                Debug.Log("Mock管理器已初始化。");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Mock管理器初始化失败: {ex.Message}");

                // 创建一个安全的备用实现
                MockManager = null;
                Debug.LogWarning("Mock管理器初始化失败，相关功能将不可用");
            }
        }

        /// <summary>
        ///     注册单个模块
        /// </summary>
        public void RegisterModule<T>(T module) where T : class, IModule
        {
            if (module == null) throw new ArgumentNullException(nameof(module));

            var moduleType = typeof(T);
            RegisterModuleWithoutProcess(module, moduleType);

            // 如果框架已初始化，简单初始化该模块（不考虑依赖关系）
            if (!_isFrameworkInitialized) return;
            try
            {
                InitializeModule(module);
                StartModule(module);
            }
            catch (ModuleException ex)
            {
                Debug.LogWarning($"模块 [{module.Name}] 初始化失败： {ex.Message}");
            }
        }

        /// <summary>
        ///     批量注册模块并正确处理依赖关系
        /// </summary>
        public void RegisterModules(params IModule[] modules)
        {
            if (modules == null || modules.Length == 0)
                return;

            // 检查是否有重复的模块类型
            var moduleTypes = new HashSet<Type>();
            foreach (var module in modules)
            {
                if (module != null)
                {
                    var moduleType = module.GetType();
                    if (moduleTypes.Contains(moduleType))
                    {
                        throw new InvalidOperationException($"批量注册中包含重复的模块类型: {moduleType.Name}");
                    }

                    if (_moduleDict.ContainsKey(moduleType))
                    {
                        throw new InvalidOperationException($"模块类型 [{moduleType.Name}] 已经注册，不能重复注册。");
                    }

                    moduleTypes.Add(moduleType);
                }
            }

            // 首先注册所有模块（不立即初始化）
            foreach (var module in modules)
            {
                if (module != null)
                {
                    RegisterModuleWithoutProcess(module, module.GetType());
                }
            }

            // 如果框架已初始化，统一解析依赖关系并初始化
            if (_isFrameworkInitialized)
            {
                // 重新解析依赖关系
                ResolveDependencies();

                // 按正确顺序初始化和启动所有未初始化的模块
                var uninitializedModules = _sortedModules.Where(m => m.State == ModuleState.Uninitialized).ToList();
                foreach (var uninitializedModule in uninitializedModules)
                {
                    try
                    {
                        InitializeModule(uninitializedModule);
                        StartModule(uninitializedModule);
                    }
                    catch (ModuleException ex)
                    {
                        Debug.LogWarning($"模块 [{uninitializedModule.Name}] 初始化失败： {ex.Message}");
                    }
                }
            }
        }

        private void RegisterModuleWithoutProcess(IModule module, Type moduleType)
        {
            if (_moduleDict.ContainsKey(moduleType))
            {
                throw new InvalidOperationException($"模块类型 [{moduleType.Name}] 已经注册，不能重复注册。");
            }

            _moduleDict[moduleType] = module;
            _moduleDictByName[module.Name] = module;
            _modules.Add(module);

            if (module is IUpdateModule updateModule)
            {
                _updateModules.Add(updateModule);
            }

            // 发布模块注册事件
            EventBus?.Broadcast(new ModuleRegisteredEvent(module.Name, moduleType));

            Debug.Log($"模块 [{module.Name}] 注册成功。");
        }

        /// <summary>
        ///     取消注册模块
        /// </summary>
        public void UnregisterModule<T>() where T : class, IModule
        {
            var moduleType = typeof(T);

            if (_moduleDict.TryGetValue(moduleType, out var module))
            {
                // 关闭模块
                module.Shutdown();

                // 从集合中移除
                _moduleDict.Remove(moduleType);
                _moduleDictByName.Remove(module.Name);
                _sortedModules.Remove(module);
                _modules.Remove(module);
                if (module is IUpdateModule updateModule)
                {
                    _updateModules.Remove(updateModule);
                }

                // 发布模块注销事件
                EventBus?.Broadcast(new ModuleUnregisteredEvent(module.Name, moduleType));

                Debug.Log($"模块 [{module.Name}] 已成功注销。");
            }
        }

        /// <summary>
        ///     获取模块
        /// </summary>
        public T GetModule<T>() where T : class, IModule
        {
            var moduleType = typeof(T);
            return _moduleDict.TryGetValue(moduleType, out var module) ? module as T : null;
        }

        /// <summary>
        ///     获取模块（通过类型）
        /// </summary>
        public IModule GetModule(Type moduleType)
        {
            return _moduleDict.TryGetValue(moduleType, out var module) ? module : null;
        }

        /// <summary>
        ///     根据名称获取模块
        /// </summary>
        public IModule GetModule(string moduleName)
        {
            return _moduleDictByName.TryGetValue(moduleName, out var module) ? module : null;
        }

        /// <summary>
        ///     检查模块是否已注册
        /// </summary>
        public bool HasModule<T>() where T : class, IModule
        {
            return _moduleDict.ContainsKey(typeof(T));
        }

        /// <summary>
        ///     检查模块是否已注册
        /// </summary>
        public bool HasModule(string moduleName)
        {
            return _moduleDictByName.ContainsKey(moduleName);
        }

        /// <summary>
        ///     替换模块实现
        /// </summary>
        /// <param name="interfaceType">模块接口类型</param>
        /// <param name="newImplementation">新的模块实现</param>
        /// <returns>是否替换成功</returns>
        internal bool ReplaceModule(Type interfaceType, IModule newImplementation)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));

            if (newImplementation == null)
                throw new ArgumentNullException(nameof(newImplementation));

            if (!_moduleDict.ContainsKey(interfaceType))
            {
                Debug.LogWarning($"无法替换模块 [{interfaceType.Name}]，因为它未注册。");
                return false;
            }

            // 获取旧模块
            var oldModule = _moduleDict[interfaceType];

            // 从集合中移除旧模块
            _moduleDictByName.Remove(oldModule.Name);

            // 添加新模块
            _moduleDict[interfaceType] = newImplementation;
            _moduleDictByName[newImplementation.Name] = newImplementation;

            // 更新排序列表
            var index = _sortedModules.IndexOf(oldModule);
            if (index >= 0)
            {
                _sortedModules[index] = newImplementation;
            }
            else
            {
                _sortedModules.Add(newImplementation);
                ResolveDependencies(); // 重新排序
            }

            Debug.Log($"模块 [{interfaceType.Name}] 的实现已成功替换。");
            return true;
        }

        /// <summary>
        ///     初始化指定模块
        /// </summary>
        /// <param name="module">要初始化的模块</param>
        internal void InitializeModule(IModule module)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module));

            if (module.State != ModuleState.Uninitialized)
            {
                Debug.LogWarning($"模块 [{module.Name}] 已经初始化。");
                return;
            }

            var result = SafeExecutor.ExecuteWithResult(() => module.Init(), module);
            if (!result.IsSuccess)
            {
                var moduleException = new ModuleException(module.Name, "INIT_FAILED",
                    $"模块 [{module.Name}] 初始化失败: {result.ErrorMessage}", result.Exception);

                if (!ErrorRecoveryManager.HandleException(moduleException, module))
                {
                    Debug.LogError($"严重: 模块 [{module.Name}] 初始化失败且恢复失败");
                    throw moduleException;
                }
            }
        }

        /// <summary>
        ///     启动指定模块
        /// </summary>
        /// <param name="module">要启动的模块</param>
        internal void StartModule(IModule module)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module));

            if (module.State != ModuleState.Initialized)
            {
                Debug.LogWarning($"模块 [{module.Name}] 未初始化或已启动。");
                return;
            }

            var result = SafeExecutor.ExecuteWithResult(() => module.Start(), module);
            if (!result.IsSuccess)
            {
                var moduleException = new ModuleException(module.Name, "START_FAILED",
                    $"模块 [{module.Name}] 启动失败: {result.ErrorMessage}", result.Exception);

                if (!ErrorRecoveryManager.HandleException(moduleException, module))
                {
                    Debug.LogError($"严重: 模块 [{module.Name}] 启动失败且恢复失败");
                    throw moduleException;
                }
            }
        }

        /// <summary>
        ///     关闭指定模块
        /// </summary>
        /// <param name="module">要关闭的模块</param>
        internal void ShutdownModule(IModule module)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module));

            if (module.State != ModuleState.Started)
            {
                Debug.LogWarning($"模块 [{module.Name}] 未启动。");
                return;
            }

            var result = SafeExecutor.ExecuteWithResult(() => module.Shutdown(), module);
            if (!result.IsSuccess)
            {
                var moduleException = new ModuleException(module.Name, "SHUTDOWN_FAILED",
                    $"模块 [{module.Name}] 关闭失败: {result.ErrorMessage}", result.Exception);

                ErrorRecoveryManager?.HandleException(moduleException, module);
            }
        }

        /// <summary>
        ///     自动发现模块
        /// </summary>
        private void AutoDiscoverModules()
        {
            // 在非运行时状态下跳过自动发现
            if (!Application.isPlaying)
            {
                Debug.Log("[FrameworkManager] 非运行时状态下跳过自动发现模块");
                return;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            List<(Type, AutoRegisterModuleAttribute)> list = new List<(Type, AutoRegisterModuleAttribute)>();
            foreach (var assembly in assemblies)
            {
                try
                {
                    var moduleTypes = assembly.GetTypes()
                        .Where(t => typeof(IModule).IsAssignableFrom(t) &&
                                    !t.IsInterface &&
                                    !t.IsAbstract &&
                                    t.GetConstructor(Type.EmptyTypes) != null)
                        .ToList();

                    foreach (var moduleType in moduleTypes)
                    {
                        AutoRegisterModuleAttribute attr =
                            moduleType.GetCustomAttribute<AutoRegisterModuleAttribute>();
                        if (attr == null) continue;
                        list.Add((moduleType, attr));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"在程序集 [{assembly.FullName}] 中发现模块时出错: {ex.Message}");
                }
            }

            list.Sort((i1, i2) => i1.Item2.Priority.CompareTo(i2.Item2.Priority));
            foreach (var valueTuple in list)
            {
                try
                {
                    Type moduleType = valueTuple.Item1;
                    AutoRegisterModuleAttribute attr = valueTuple.Item2;
                    if (Activator.CreateInstance(moduleType) is IModule module)
                    {
                        RegisterModuleWithoutProcess(module, attr.InterfaceType ?? module.GetType());
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"创建模块 [{valueTuple.Item1.Name}] 的实例失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        ///     解析依赖关系并排序模块
        /// </summary>
        private void ResolveDependencies()
        {
            var sortedModules = new List<IModule>();
            var visited = new HashSet<Type>();
            var visiting = new HashSet<Type>();

            foreach (var module in _modules)
                if (!visited.Contains(module.GetType()))
                    VisitModule(module, visited, visiting, sortedModules);

            _sortedModules.Clear();
            _sortedModules.AddRange(sortedModules.OrderBy(m => m.Priority));
        }

        /// <summary>
        ///     访问模块（深度优先搜索）
        /// </summary>
        private void VisitModule(IModule module, HashSet<Type> visited, HashSet<Type> visiting,
            List<IModule> sortedModules)
        {
            var moduleType = module.GetType();

            if (visiting.Contains(moduleType)) throw new InvalidOperationException($"检测到涉及模块的循环依赖关系 [{module.Name}]");

            if (visited.Contains(moduleType)) return;

            visiting.Add(moduleType);

            // 获取依赖的模块
            var dependsOnAttributes = moduleType.GetCustomAttributes<DependsOnAttribute>();
            foreach (var dependsOn in dependsOnAttributes)
                if (_moduleDict.TryGetValue(dependsOn.ModuleType, out var dependencyModule))
                    VisitModule(dependencyModule, visited, visiting, sortedModules);
                else
                    Debug.LogWarning(
                        $"模块 [{module.Name}] 依赖于 [{dependsOn.ModuleType.Name}], 但没有注册.");

            visiting.Remove(moduleType);
            visited.Add(moduleType);
            sortedModules.Add(module);
        }

        /// <summary>
        ///     初始化错误恢复管理器
        /// </summary>
        private void InitializeErrorRecoveryManager()
        {
            try
            {
                Debug.Log("[FrameworkManager] 开始初始化错误恢复管理器");

                ErrorRecoveryManager = new ErrorRecoveryManager();
                Debug.Log("[FrameworkManager] ErrorRecoveryManager 实例创建成功");

                // 注册默认的恢复策略
                ErrorRecoveryManager.RegisterRecoveryStrategy<ModuleException>(new ModuleExceptionRecoveryStrategy());
                ErrorRecoveryManager.RegisterRecoveryStrategy<EventBusException>(
                    new EventBusExceptionRecoveryStrategy());
                ErrorRecoveryManager.RegisterRecoveryStrategy<DependencyException>(
                    new DependencyExceptionRecoveryStrategy());
                ErrorRecoveryManager.RegisterRecoveryStrategy<OutOfMemoryException>(new OutOfMemoryRecoveryStrategy());
                ErrorRecoveryManager.RegisterRecoveryStrategy<Exception>(new GenericExceptionRecoveryStrategy());
                Debug.Log("[FrameworkManager] 错误恢复策略注册完成");

                // 设置SafeExecutor的错误恢复管理器
                SafeExecutor.SetErrorRecoveryManager(ErrorRecoveryManager);
                Debug.Log("[FrameworkManager] SafeExecutor 配置完成");

                Debug.Log("使用默认策略初始化错误恢复管理器。");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FrameworkManager] 错误恢复管理器初始化失败: {ex.Message}");
                Debug.LogError($"详细异常: {ex}");

                // 创建一个简化版本
                ErrorRecoveryManager = new ErrorRecoveryManager();
                ErrorRecoveryManager.RegisterRecoveryStrategy<Exception>(new GenericExceptionRecoveryStrategy());
                Debug.LogWarning("[FrameworkManager] 使用简化版错误恢复管理器");
            }
        }

        /// <summary>
        ///     初始化配置管理器
        /// </summary>
        private void InitializeConfigManager()
        {
            try
            {
                Debug.Log("[FrameworkManager] 开始初始化配置管理器");

                // 创建配置管理器
                _configManager = new ConfigManager(EventBus);
                Debug.Log("[FrameworkManager] ConfigManager 实例创建成功");

                // 添加内存配置源（优先级最高，但不持久化）
                _configManager.AddConfigSource(new MemoryConfigSource());
                Debug.Log("[FrameworkManager] 内存配置源添加成功");

                // 添加PlayerPrefs配置源（中等优先级，持久化）
                _configManager.AddConfigSource(new PlayerPrefsConfigSource("CnoomFramework"));
                Debug.Log("[FrameworkManager] PlayerPrefs配置源添加成功");

                // 添加JSON文件配置源（优先级最低，持久化）
                _configManager.AddConfigSource(new JsonFileConfigSource("CnoomFramework/config.json"));
                Debug.Log("[FrameworkManager] JSON文件配置源添加成功");

                // 加载配置
                _configManager.Load();
                Debug.Log("[FrameworkManager] 配置加载成功");

                Debug.Log("配置管理器使用默认源初始化。");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FrameworkManager] 配置管理器初始化失败: {ex.Message}");
                Debug.LogError($"详细异常: {ex}");

                // 创建一个最简单的配置管理器作为备用
                _configManager = new ConfigManager(EventBus);
                _configManager.AddConfigSource(new MemoryConfigSource());
                Debug.LogWarning("[FrameworkManager] 使用简化版配置管理器");
            }
        }

        /// <summary>
        ///     应用事件总线相关配置
        /// </summary>
        private void ApplyEventBusConfig()
        {
            if (_configManager == null || EventBus == null) return;

            _eventBusMaxCached = _configManager.GetValue("EventBus.MaxCachedEvents", _eventBusMaxCached);
            _eventBusMaxAsyncPerFrame =
                _configManager.GetValue("EventBus.MaxAsyncHandlersPerFrame", _eventBusMaxAsyncPerFrame);
            _eventBusEnableInheritance =
                _configManager.GetValue("EventBus.EnableInheritanceDispatch", _eventBusEnableInheritance);

            if (EventBus is EventBus concrete)
            {
                concrete.Core.MaxCachedEvents = _eventBusMaxCached;
                concrete.Core.MaxAsyncHandlersPerFrame = _eventBusMaxAsyncPerFrame;
                concrete.Core.EnableInheritanceDispatch = _eventBusEnableInheritance;
            }
        }

        /// <summary>
        ///     初始化所有模块
        /// </summary>
        private void InitializeModules()
        {
            Debug.Log($"[FrameworkManager] 开始初始化 {_sortedModules.Count} 个模块");

            for (int i = 0; i < _sortedModules.Count; i++)
            {
                var module = _sortedModules[i];
                Debug.Log($"[FrameworkManager] 初始化模块 [{i + 1}/{_sortedModules.Count}]: {module.Name}");

                var result = SafeExecutor.ExecuteWithResult(() => module.Init(), module);
                if (!result.IsSuccess)
                {
                    var moduleException = new ModuleException(module.Name, "INIT_FAILED",
                        $"初始化模块失败 [{module.Name}]: {result.ErrorMessage}", result.Exception);

                    if (!ErrorRecoveryManager.HandleException(moduleException, module))
                    {
                        Debug.LogError($"危急： 初始化模块失败 [{module.Name}] 并且恢复失败");
                        throw moduleException;
                    }
                }
                else
                {
                    Debug.Log($"[FrameworkManager] 模块 {module.Name} 初始化成功");
                }
            }

            Debug.Log("[FrameworkManager] 所有模块初始化完成");
        }

        /// <summary>
        ///     启动所有模块
        /// </summary>
        private void StartModules()
        {
            Debug.Log($"[FrameworkManager] 开始启动 {_sortedModules.Count} 个模块");

            for (int i = 0; i < _sortedModules.Count; i++)
            {
                var module = _sortedModules[i];
                Debug.Log($"[FrameworkManager] 启动模块 [{i + 1}/{_sortedModules.Count}]: {module.Name}");

                var result = SafeExecutor.ExecuteWithResult(() => module.Start(), module);
                if (!result.IsSuccess)
                {
                    var moduleException = new ModuleException(module.Name, "START_FAILED",
                        $"模块 [{module.Name}] 启动失败: {result.ErrorMessage}", result.Exception);

                    if (!ErrorRecoveryManager.HandleException(moduleException, module))
                    {
                        Debug.LogError($"严重: 模块 [{module.Name}] 启动失败且恢复失败");
                        throw moduleException;
                    }
                }
                else
                {
                    Debug.Log($"[FrameworkManager] 模块 {module.Name} 启动成功");
                }
            }

            Debug.Log("[FrameworkManager] 所有模块启动完成");
        }

        /// <summary>
        ///     应用程序暂停时调用
        /// </summary>
        private void OnApplicationPaused()
        {
            // 可以在这里添加暂停时的逻辑
        }

        /// <summary>
        ///     应用程序恢复时调用
        /// </summary>
        private void OnApplicationResumed()
        {
            // 可以在这里添加恢复时的逻辑
        }
    }
}