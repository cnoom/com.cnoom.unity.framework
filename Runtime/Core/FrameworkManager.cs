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
        private ConfigManager _configManager;

        private bool _eventBusEnableInheritance = true;
        private int _eventBusMaxAsyncPerFrame = 64;
        private int _eventBusMaxCached = 1000;
        private bool _isShuttingDown;

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
        ///     是否已初始化
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        ///     已注册的模块数量
        /// </summary>
        public int ModuleCount => _modules.Count;

        /// <summary>
        ///     获取所有模块
        /// </summary>
        public IReadOnlyList<IModule> Modules => _modules.AsReadOnly();

        protected override void OnInitialized()
        {
            if (autoInitialize) Initialize();
        }

        private void Update()
        {
            // 调度事件总线异步队列
            if (EventBus != null && EventBus is EventBus concreteEventBus) concreteEventBus.ProcessPending();
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
            if (IsInitialized)
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

                // 初始化所有模块
                InitializeModules();

                // 启动所有模块
                StartModules();

                // 清除排序集合
                _sortedModules.Clear();

                IsInitialized = true;

                // 发布框架初始化完成事件
                EventBus.Broadcast(new FrameworkInitializedEvent(_moduleDict.Count));

                Debug.Log($"CnoomFramework 初始化成功，共 {_moduleDict.Count} 个模块。");
            }
            catch (Exception ex)
            {
                Debug.LogError($"CnoomFramework 初始化失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        ///     关闭框架
        /// </summary>
        public void Shutdown()
        {
            if (_isShuttingDown || !IsInitialized) return;

            _isShuttingDown = true;

            try
            {
                Debug.Log("关闭 CnoomFramework...");

                // 保存配置
                _configManager?.Save();

                // 发布框架关闭事件
                EventBus?.Broadcast(new FrameworkShutdownEvent());

                // 反向关闭所有模块
                for (var i = _sortedModules.Count - 1; i >= 0; i--)
                {
                    var module = _sortedModules[i];
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

                IsInitialized = false;

                Debug.Log("CnoomFramework 关闭完成。");
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
            MockManager = new MockManager(this);
            Debug.Log("Mock管理器已初始化。");
        }

        /// <summary>
        ///     注册模块并执行初始化和开始方法
        /// </summary>
        public void RegisterModule<T>(T module) where T : class, IModule
        {
            if (module == null) throw new ArgumentNullException(nameof(module));

            var moduleType = typeof(T);

            RegisterModuleWithoutProcess(module, moduleType);
            module.Init();
            module.Start();
        }

        private void RegisterModuleWithoutProcess(IModule module, Type moduleType)
        {
            if (_moduleDict.ContainsKey(moduleType))
            {
                Debug.LogWarning($"模块 [{moduleType.Name}] 已经注册。");
                return;
            }

            _moduleDict[moduleType] = module;
            _moduleDictByName[module.Name] = module;
            _modules.Add(module);
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
            ErrorRecoveryManager = new ErrorRecoveryManager();

            // 注册默认的恢复策略
            ErrorRecoveryManager.RegisterRecoveryStrategy<ModuleException>(new ModuleExceptionRecoveryStrategy());
            ErrorRecoveryManager.RegisterRecoveryStrategy<EventBusException>(new EventBusExceptionRecoveryStrategy());
            ErrorRecoveryManager.RegisterRecoveryStrategy<DependencyException>(
                new DependencyExceptionRecoveryStrategy());
            ErrorRecoveryManager.RegisterRecoveryStrategy<OutOfMemoryException>(new OutOfMemoryRecoveryStrategy());
            ErrorRecoveryManager.RegisterRecoveryStrategy<Exception>(new GenericExceptionRecoveryStrategy());

            // 设置SafeExecutor的错误恢复管理器
            SafeExecutor.SetErrorRecoveryManager(ErrorRecoveryManager);

            Debug.Log("使用默认策略初始化错误恢复管理器。");
        }

        /// <summary>
        ///     初始化配置管理器
        /// </summary>
        private void InitializeConfigManager()
        {
            // 创建配置管理器
            _configManager = new ConfigManager(EventBus);

            // 添加内存配置源（优先级最高，但不持久化）
            _configManager.AddConfigSource(new MemoryConfigSource());

            // 添加PlayerPrefs配置源（中等优先级，持久化）
            _configManager.AddConfigSource(new PlayerPrefsConfigSource("CnoomFramework"));

            // 添加JSON文件配置源（优先级最低，持久化）
            _configManager.AddConfigSource(new JsonFileConfigSource("CnoomFramework/config.json"));

            // 加载配置
            _configManager.Load();

            Debug.Log("配置管理器使用默认源初始化。");
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
            foreach (var module in _sortedModules)
            {
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
            }
        }

        /// <summary>
        ///     启动所有模块
        /// </summary>
        private void StartModules()
        {
            foreach (var module in _sortedModules)
            {
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