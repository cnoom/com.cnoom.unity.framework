using System;
using System.Collections.Generic;
using System.Linq;
using CnoomFramework.Core;
using CnoomFramework.Core.Attributes;
using CnoomFramework.Core.Config;
using CnoomFramework.Core.Events;
using CnoomFramework.Core.Performance;
using CnoomFramework.Editor.Editor;
using UnityEditor;
using UnityEngine;

namespace CnoomFramework.Editor
{
    /// <summary>
    /// CnoomFramework调试器窗口
    /// </summary>
    public class FrameworkDebuggerWindow : EditorWindow
    {
        private enum TabType
        {
            Overview,
            Modules,
            Events,
            Config,
            Console,
            Dependencies,
            Performance
        }

        private const string MenuPath = FrameworkEditorConfig.MenuPath + "/" + "框架调试器";

        private TabType _selectedTab = TabType.Overview;
        private Vector2 _scrollPosition;
        private bool _isFrameworkInitialized = false;
        private string _searchText = "";
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _boldLabelStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _tabStyle;
        private GUIStyle _activeTabStyle;
        private GUIStyle _moduleBoxStyle;
        private GUIStyle _eventBoxStyle;
        private GUIStyle _configBoxStyle;
        private GUIStyle _consoleBoxStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _searchFieldStyle;
        private GUIStyle _searchCancelButtonStyle;
        private Texture2D _logoTexture;

        // 模块相关
        private int _selectedModuleIndex = -1;
        private List<IModule> _modules = new List<IModule>();
        private Dictionary<string, bool> _moduleFoldouts = new Dictionary<string, bool>();
        private bool _showModuleDetails = true;
        private bool _showModuleDependencies = true;
        private bool _showModuleEvents = true;

        // 事件相关
        private List<EventLogEntry> _eventLogs = new List<EventLogEntry>();
        private bool _autoScrollEvents = true;
        private bool _pauseEventLogging = false;
        private int _maxEventLogs = 100;
        private string _eventTypeFilter = "";
        private bool _showEventDetails = true;
        private bool _groupEventsByType = false;

        // 配置相关
        private Dictionary<string, object> _configValues = new Dictionary<string, object>();
        private string _configKeyFilter = "";
        private string _newConfigKey = "";
        private string _newConfigValue = "";
        private bool _showAddConfigPanel = false;
        private bool _showConfigHistory = false;

        // 控制台相关
        private List<LogEntry> _consoleLogs = new List<LogEntry>();
        private bool _showInfoLogs = true;
        private bool _showWarningLogs = true;
        private bool _showErrorLogs = true;
        private bool _autoScrollConsole = true;
        private int _maxConsoleLogs = 100;

        // 依赖关系相关
        private Dictionary<string, List<string>> _moduleDependencies = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> _moduleDependents = new Dictionary<string, List<string>>();

        private Dictionary<string, DependencyAnalyzer.DependencyInfo> _dependencyInfos =
            new Dictionary<string, DependencyAnalyzer.DependencyInfo>();

        private List<DependencyAnalyzer.CircularDependencyInfo> _circularDependencies =
            new List<DependencyAnalyzer.CircularDependencyInfo>();

        private bool _showDependencyGraph = true;
        private bool _showCircularDependencies = true;
        private bool _showInitializationOrder = true;

        // 性能相关
        private Dictionary<string, PerformanceStats> _performanceStats = new Dictionary<string, PerformanceStats>();
        private bool _showPerformanceDetails = true;
        private bool _autoRefreshPerformance = true;
        private float _performanceRefreshInterval = 2.0f;
        private float _lastPerformanceRefreshTime;

        // 日志条目类
        [Serializable]
        private class LogEntry
        {
            public DateTime Timestamp;
            public string Message;
            public string StackTrace;
            public LogType LogType;
            public bool ShowStackTrace;
        }

        // 事件日志条目类
        [Serializable]
        private class EventLogEntry
        {
            public DateTime Timestamp;
            public string EventType;
            public object EventData;
        }

        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            var window = GetWindow<FrameworkDebuggerWindow>("Framework Debugger");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            // 订阅Unity日志回调
            Application.logMessageReceived += OnLogMessageReceived;

            // 加载资源
            _logoTexture = EditorGUIUtility.Load("Icons/d_UnityEditor.ConsoleWindow@2x") as Texture2D;

            // 检查框架是否已初始化
            EditorApplication.update += CheckFrameworkStatus;
        }

        private void OnDisable()
        {
            // 取消订阅Unity日志回调
            Application.logMessageReceived -= OnLogMessageReceived;

            // 取消检查框架状态
            EditorApplication.update -= CheckFrameworkStatus;
        }

        private void CheckFrameworkStatus()
        {
            if (Application.isPlaying)
            {
                var frameworkManager = FrameworkManager.Instance;
                if (frameworkManager != null && frameworkManager.IsInitialized)
                {
                    if (!_isFrameworkInitialized)
                    {
                        _isFrameworkInitialized = true;
                        SubscribeToFrameworkEvents();
                        RefreshModulesList();
                        RefreshConfigValues();
                        RefreshPerformanceData();
                    }

                    // 自动刷新性能数据
                    if (_autoRefreshPerformance && Time.realtimeSinceStartup - _lastPerformanceRefreshTime >
                        _performanceRefreshInterval)
                    {
                        RefreshPerformanceData();
                        _lastPerformanceRefreshTime = Time.realtimeSinceStartup;
                    }
                }
                else
                {
                    _isFrameworkInitialized = false;
                }
            }
            else
            {
                _isFrameworkInitialized = false;
            }

            Repaint();
        }

        private void RefreshPerformanceData()
        {
            if (!_isFrameworkInitialized) return;

            try
            {
                var performanceModule = FrameworkManager.Instance.GetModule<PerformanceMonitorModule>();
                if (performanceModule != null)
                {
                    var stats = performanceModule.PerformanceMonitor.GetAllStats();
                    _performanceStats = new Dictionary<string, PerformanceStats>(stats);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"刷新性能数据时出错: {ex.Message}");
            }
        }

        private void SubscribeToFrameworkEvents()
        {
            var eventBus = FrameworkManager.Instance.EventBus;

            // 订阅框架事件
            eventBus.Subscribe<ModuleRegisteredEvent>(OnModuleRegistered);
            eventBus.Subscribe<ModuleUnregisteredEvent>(OnModuleUnregistered);
            eventBus.Subscribe<ModuleStateChangedEvent>(OnModuleStateChanged);
            eventBus.Subscribe<ConfigChangedEvent>(OnConfigChanged);
            eventBus.Subscribe<ConfigSavedEvent>(OnConfigSaved);
            eventBus.Subscribe<ConfigLoadedEvent>(OnConfigLoaded);
            eventBus.Subscribe<PerformanceDataUpdatedEvent>(OnPerformanceDataUpdated);
            eventBus.Subscribe<PerformanceMonitorStatusChangedEvent>(OnPerformanceMonitorStatusChanged);
        }

        private void OnModuleRegistered(ModuleRegisteredEvent evt)
        {
            AddEventLog("ModuleRegistered", evt);
            RefreshModulesList();
        }

        private void OnModuleUnregistered(ModuleUnregisteredEvent evt)
        {
            AddEventLog("ModuleUnregistered", evt);
            RefreshModulesList();
        }

        private void OnModuleStateChanged(ModuleStateChangedEvent evt)
        {
            AddEventLog("ModuleStateChanged", evt);
            RefreshModulesList();
        }

        private void OnConfigChanged(ConfigChangedEvent evt)
        {
            AddEventLog("ConfigChanged", evt);
            RefreshConfigValues();
        }

        private void OnConfigSaved(ConfigSavedEvent evt)
        {
            AddEventLog("ConfigSaved", evt);
        }

        private void OnConfigLoaded(ConfigLoadedEvent evt)
        {
            AddEventLog("ConfigLoaded", evt);
            RefreshConfigValues();
        }

        private void OnPerformanceDataUpdated(PerformanceDataUpdatedEvent evt)
        {
            _performanceStats[evt.OperationName] = evt.Stats;
            Repaint();
        }

        private void OnPerformanceMonitorStatusChanged(PerformanceMonitorStatusChangedEvent evt)
        {
            Repaint();
        }

        private void RefreshModulesList()
        {
            if (!_isFrameworkInitialized) return;
            
            _modules.Clear();
            _modules.AddRange(FrameworkManager.Instance.Modules);

            // 更新依赖关系
            UpdateModuleDependencies();
        }

        private void UpdateModuleDependencies()
        {
            _moduleDependencies.Clear();
            _moduleDependents.Clear();

            foreach (var module in _modules)
            {
                var moduleType = module.GetType();
                var dependsOnAttributes = moduleType.GetCustomAttributes(typeof(DependsOnAttribute), true);

                var dependencies = new List<string>();
                foreach (DependsOnAttribute dependsOn in dependsOnAttributes)
                {
                    dependencies.Add(dependsOn.ModuleType.Name);

                    // 更新被依赖的模块
                    if (!_moduleDependents.ContainsKey(dependsOn.ModuleType.Name))
                    {
                        _moduleDependents[dependsOn.ModuleType.Name] = new List<string>();
                    }

                    _moduleDependents[dependsOn.ModuleType.Name].Add(module.Name);
                }

                _moduleDependencies[module.Name] = dependencies;
            }

            // 使用依赖分析器进行详细分析
            _dependencyInfos = DependencyAnalyzer.AnalyzeDependencies(_modules);
            _circularDependencies = DependencyAnalyzer.GetCircularDependencies(_dependencyInfos);
        }

        private void RefreshConfigValues()
        {
            if (!_isFrameworkInitialized) return;

            _configValues.Clear();
            var configManager = FrameworkManager.Instance.ConfigManager;
            var keys = configManager.GetAllKeys();

            foreach (var key in keys)
            {
                _configValues[key] = configManager.GetValue<object>(key);
            }
        }

        private void AddEventLog(string eventType, object eventData)
        {
            if (_pauseEventLogging) return;

            _eventLogs.Add(new EventLogEntry
            {
                Timestamp = DateTime.Now,
                EventType = eventType,
                EventData = eventData
            });

            // 限制日志数量
            while (_eventLogs.Count > _maxEventLogs)
            {
                _eventLogs.RemoveAt(0);
            }

            // 自动滚动
            if (_autoScrollEvents && _selectedTab == TabType.Events)
            {
                _scrollPosition.y = float.MaxValue;
            }

            Repaint();
        }

        private void OnLogMessageReceived(string condition, string stacktrace, LogType type)
        {
            if (!condition.Contains("[CnoomFramework]")) return;

            _consoleLogs.Add(new LogEntry
            {
                Timestamp = DateTime.Now,
                Message = condition,
                StackTrace = stacktrace,
                LogType = type
            });

            // 限制日志数量
            while (_consoleLogs.Count > _maxConsoleLogs)
            {
                _consoleLogs.RemoveAt(0);
            }

            // 自动滚动
            if (_autoScrollConsole && _selectedTab == TabType.Console)
            {
                _scrollPosition.y = float.MaxValue;
            }

            Repaint();
        }

        private void OnGUI()
        {
            InitializeStyles();

            DrawToolbar();

            EditorGUILayout.Space();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            switch (_selectedTab)
            {
                case TabType.Overview:
                    DrawOverviewTab();
                    break;
                case TabType.Modules:
                    DrawModulesTab();
                    break;
                case TabType.Events:
                    DrawEventsTab();
                    break;
                case TabType.Config:
                    DrawConfigTab();
                    break;
                case TabType.Console:
                    DrawConsoleTab();
                    break;
                case TabType.Dependencies:
                    DrawDependenciesTab();
                    break;
                case TabType.Performance:
                    DrawPerformanceTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void InitializeStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleLeft,
                    margin = new RectOffset(10, 10, 10, 10)
                };
            }

            if (_subHeaderStyle == null)
            {
                _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleLeft,
                    margin = new RectOffset(5, 5, 5, 5)
                };
            }

            if (_boldLabelStyle == null)
            {
                _boldLabelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12
                };
            }

            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    margin = new RectOffset(5, 5, 5, 5),
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }

            if (_tabStyle == null)
            {
                _tabStyle = new GUIStyle(EditorStyles.toolbarButton)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter,
                    fixedHeight = 30,
                    margin = new RectOffset(0, 0, 0, 0),
                    padding = new RectOffset(10, 10, 5, 5)
                };
            }

            if (_activeTabStyle == null)
            {
                _activeTabStyle = new GUIStyle(_tabStyle)
                {
                    normal = { background = EditorGUIUtility.Load("IN BigTitle") as Texture2D },
                    fontStyle = FontStyle.Bold
                };
            }

            if (_moduleBoxStyle == null)
            {
                _moduleBoxStyle = new GUIStyle(_boxStyle)
                {
                    normal = { background = EditorGUIUtility.Load("CN EntryBackEven") as Texture2D }
                };
            }

            if (_eventBoxStyle == null)
            {
                _eventBoxStyle = new GUIStyle(_boxStyle)
                {
                    normal = { background = EditorGUIUtility.Load("CN EntryBackOdd") as Texture2D }
                };
            }

            if (_configBoxStyle == null)
            {
                _configBoxStyle = new GUIStyle(_boxStyle)
                {
                    normal = { background = EditorGUIUtility.Load("CN EntryBackEven") as Texture2D }
                };
            }

            if (_consoleBoxStyle == null)
            {
                _consoleBoxStyle = new GUIStyle(_boxStyle);
            }

            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    margin = new RectOffset(2, 2, 2, 2)
                };
            }

            if (_searchFieldStyle == null)
            {
                _searchFieldStyle = new GUIStyle(EditorStyles.toolbarSearchField)
                {
                    margin = new RectOffset(5, 5, 5, 5),
                    fixedWidth = 200
                };
            }

            if (_searchCancelButtonStyle == null)
            {
                _searchCancelButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
                {
                    fixedWidth = 20,
                    margin = new RectOffset(0, 5, 5, 5)
                };
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Toggle(_selectedTab == TabType.Overview, "概览",
                    _selectedTab == TabType.Overview ? _activeTabStyle : _tabStyle))
            {
                _selectedTab = TabType.Overview;
            }

            if (GUILayout.Toggle(_selectedTab == TabType.Modules, "模块",
                    _selectedTab == TabType.Modules ? _activeTabStyle : _tabStyle))
            {
                _selectedTab = TabType.Modules;
            }

            if (GUILayout.Toggle(_selectedTab == TabType.Events, "事件",
                    _selectedTab == TabType.Events ? _activeTabStyle : _tabStyle))
            {
                _selectedTab = TabType.Events;
            }

            if (GUILayout.Toggle(_selectedTab == TabType.Config, "配置",
                    _selectedTab == TabType.Config ? _activeTabStyle : _tabStyle))
            {
                _selectedTab = TabType.Config;
            }

            if (GUILayout.Toggle(_selectedTab == TabType.Console, "控制台",
                    _selectedTab == TabType.Console ? _activeTabStyle : _tabStyle))
            {
                _selectedTab = TabType.Console;
            }

            if (GUILayout.Toggle(_selectedTab == TabType.Dependencies, "依赖关系",
                    _selectedTab == TabType.Dependencies ? _activeTabStyle : _tabStyle))
            {
                _selectedTab = TabType.Dependencies;
            }

            if (GUILayout.Toggle(_selectedTab == TabType.Performance, "性能",
                    _selectedTab == TabType.Performance ? _activeTabStyle : _tabStyle))
            {
                _selectedTab = TabType.Performance;
            }

            GUILayout.FlexibleSpace();

            _searchText = EditorGUILayout.TextField(_searchText, _searchFieldStyle);

            if (GUILayout.Button("×", _searchCancelButtonStyle) && !string.IsNullOrEmpty(_searchText))
            {
                _searchText = "";
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawOverviewTab()
        {
            EditorGUILayout.BeginVertical();

            // 标题
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(_logoTexture, GUILayout.Width(32), GUILayout.Height(32));
            GUILayout.Label("CnoomFramework 调试器", _headerStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 框架状态
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("框架状态", _subHeaderStyle);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("运行状态:", _boldLabelStyle, GUILayout.Width(100));

            if (Application.isPlaying)
            {
                if (_isFrameworkInitialized)
                {
                    EditorGUILayout.LabelField("已初始化", EditorStyles.boldLabel);
                    GUI.color = Color.green;
                    GUILayout.Box("●", GUILayout.Width(20));
                    GUI.color = Color.white;
                }
                else
                {
                    EditorGUILayout.LabelField("未初始化", EditorStyles.boldLabel);
                    GUI.color = Color.yellow;
                    GUILayout.Box("●", GUILayout.Width(20));
                    GUI.color = Color.white;
                }
            }
            else
            {
                EditorGUILayout.LabelField("编辑器模式", EditorStyles.boldLabel);
                GUI.color = Color.gray;
                GUILayout.Box("●", GUILayout.Width(20));
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            if (_isFrameworkInitialized)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("模块数量:", _boldLabelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField(_modules.Count.ToString());
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("事件日志:", _boldLabelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField(_eventLogs.Count.ToString());
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("配置项数:", _boldLabelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField(_configValues.Count.ToString());
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("性能操作:", _boldLabelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField(_performanceStats.Count.ToString());
                EditorGUILayout.EndHorizontal();

                // 模块状态统计
                var startedModules = _modules.Count(m => m.State == ModuleState.Started);
                var initializedModules = _modules.Count(m => m.State == ModuleState.Initialized);
                var uninitializedModules = _modules.Count(m => m.State == ModuleState.Uninitialized);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("已启动:", _boldLabelStyle, GUILayout.Width(100));
                GUI.color = Color.green;
                EditorGUILayout.LabelField(startedModules.ToString());
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("已初始化:", _boldLabelStyle, GUILayout.Width(100));
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField(initializedModules.ToString());
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();

                if (uninitializedModules > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("未初始化:", _boldLabelStyle, GUILayout.Width(100));
                    GUI.color = Color.red;
                    EditorGUILayout.LabelField(uninitializedModules.ToString());
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 快速操作
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("快速操作", _subHeaderStyle);

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = Application.isPlaying && _isFrameworkInitialized;

            if (GUILayout.Button("刷新模块列表", _buttonStyle))
            {
                RefreshModulesList();
            }

            if (GUILayout.Button("刷新配置", _buttonStyle))
            {
                RefreshConfigValues();
            }

            if (GUILayout.Button("刷新性能数据", _buttonStyle))
            {
                RefreshPerformanceData();
            }

            if (GUILayout.Button("保存配置", _buttonStyle))
            {
                if (_isFrameworkInitialized)
                {
                    FrameworkManager.Instance.ConfigManager.Save();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("清除事件日志", _buttonStyle))
            {
                _eventLogs.Clear();
            }

            if (GUILayout.Button("清除控制台", _buttonStyle))
            {
                _consoleLogs.Clear();
            }

            if (GUILayout.Button("重置性能统计", _buttonStyle))
            {
                if (_isFrameworkInitialized)
                {
                    var performanceModule = FrameworkManager.Instance.GetModule<PerformanceMonitorModule>();
                    if (performanceModule != null)
                    {
                        performanceModule.ResetAllStats();
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("打开事件流可视化", _buttonStyle))
            {
                EventFlowVisualizerWindow.ShowWindow();
            }

            if (GUILayout.Button("打开性能监控", _buttonStyle))
            {
                PerformanceMonitorWindow.ShowWindow();
            }

            if (GUILayout.Button("打开增强性能监控", _buttonStyle))
            {
                EnhancedPerformanceMonitorWindow.ShowWindow();
            }

            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 最近事件
            if (_isFrameworkInitialized && _eventLogs.Count > 0)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                GUILayout.Label("最近事件", _subHeaderStyle);

                var recentEvents = _eventLogs.Skip(Math.Max(0, _eventLogs.Count - 5)).Take(5).Reverse();

                foreach (var eventLog in recentEvents)
                {
                    EditorGUILayout.BeginHorizontal(_eventBoxStyle);

                    EditorGUILayout.LabelField(eventLog.Timestamp.ToString("HH:mm:ss"), GUILayout.Width(70));
                    EditorGUILayout.LabelField(eventLog.EventType, _boldLabelStyle, GUILayout.Width(150));

                    string eventDataString = "无数据";
                    if (eventLog.EventData != null)
                    {
                        if (eventLog.EventData is ModuleRegisteredEvent moduleRegEvent)
                        {
                            eventDataString = $"模块: {moduleRegEvent.ModuleName}";
                        }
                        else if (eventLog.EventData is ModuleStateChangedEvent moduleStateEvent)
                        {
                            eventDataString = $"模块: {moduleStateEvent.ModuleName}, 状态: {moduleStateEvent.NewState}";
                        }
                        else if (eventLog.EventData is ConfigChangedEvent configEvent)
                        {
                            eventDataString = $"键: {configEvent.Key}, 值: {configEvent.NewValue}";
                        }
                        else
                        {
                            eventDataString = eventLog.EventData.ToString();
                        }
                    }

                    EditorGUILayout.LabelField(eventDataString);

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawModulesTab()
        {
            EditorGUILayout.BeginVertical();

            GUILayout.Label("模块管理", _headerStyle);

            if (!_isFrameworkInitialized)
            {
                EditorGUILayout.HelpBox("框架未初始化，请在运行时查看模块信息。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // 模块列表
            EditorGUILayout.BeginHorizontal();

            // 左侧模块列表
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            GUILayout.Label("已注册模块", _subHeaderStyle);

            for (int i = 0; i < _modules.Count; i++)
            {
                var module = _modules[i];

                // 如果有搜索文本，过滤模块
                if (!string.IsNullOrEmpty(_searchText) &&
                    !module.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal(_moduleBoxStyle);

                // 状态指示器
                switch (module.State)
                {
                    case ModuleState.Uninitialized:
                        GUI.color = Color.gray;
                        GUILayout.Box("●", GUILayout.Width(20));
                        break;
                    case ModuleState.Initialized:
                        GUI.color = Color.yellow;
                        GUILayout.Box("●", GUILayout.Width(20));
                        break;
                    case ModuleState.Started:
                        GUI.color = Color.green;
                        GUILayout.Box("●", GUILayout.Width(20));
                        break;
                    case ModuleState.Shutdown:
                        GUI.color = Color.red;
                        GUILayout.Box("●", GUILayout.Width(20));
                        break;
                }

                GUI.color = Color.white;

                if (GUILayout.Toggle(_selectedModuleIndex == i, module.Name, EditorStyles.radioButton))
                {
                    _selectedModuleIndex = i;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            // 右侧模块详情
            EditorGUILayout.BeginVertical();

            if (_selectedModuleIndex >= 0 && _selectedModuleIndex < _modules.Count)
            {
                var selectedModule = _modules[_selectedModuleIndex];

                EditorGUILayout.BeginVertical(_boxStyle);
                GUILayout.Label($"模块详情: {selectedModule.Name}", _subHeaderStyle);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("名称:", _boldLabelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField(selectedModule.Name);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("类型:", _boldLabelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField(selectedModule.GetType().FullName);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("优先级:", _boldLabelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField(selectedModule.Priority.ToString());
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("状态:", _boldLabelStyle, GUILayout.Width(100));
                switch (selectedModule.State)
                {
                    case ModuleState.Uninitialized:
                        GUI.color = Color.gray;
                        EditorGUILayout.LabelField("未初始化");
                        break;
                    case ModuleState.Initialized:
                        GUI.color = Color.yellow;
                        EditorGUILayout.LabelField("已初始化");
                        break;
                    case ModuleState.Started:
                        GUI.color = Color.green;
                        EditorGUILayout.LabelField("已启动");
                        break;
                    case ModuleState.Shutdown:
                        GUI.color = Color.red;
                        EditorGUILayout.LabelField("已关闭");
                        break;
                }

                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                // 模块操作
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("卸载模块", _buttonStyle))
                {
                    if (EditorUtility.DisplayDialog("卸载模块", $"确定要卸载模块 {selectedModule.Name} 吗？", "确定", "取消"))
                    {
                        var moduleType = selectedModule.GetType();
                        var unregisterMethod = typeof(FrameworkManager).GetMethod("UnregisterModule")
                            .MakeGenericMethod(moduleType);
                        unregisterMethod.Invoke(FrameworkManager.Instance, null);
                    }
                }

                if (GUILayout.Button("查看性能", _buttonStyle))
                {
                    _selectedTab = TabType.Performance;
                    _searchText = selectedModule.Name;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                // 模块依赖
                EditorGUILayout.BeginVertical(_boxStyle);
                GUILayout.Label("模块依赖", _subHeaderStyle);

                var dependsOnAttributes =
                    selectedModule.GetType().GetCustomAttributes(typeof(DependsOnAttribute), true);

                if (dependsOnAttributes.Length > 0)
                {
                    foreach (DependsOnAttribute dependsOn in dependsOnAttributes)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("依赖:", _boldLabelStyle, GUILayout.Width(100));
                        EditorGUILayout.LabelField(dependsOn.ModuleType.Name);
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("无依赖");
                }

                EditorGUILayout.EndVertical();

                // 事件订阅
                EditorGUILayout.BeginVertical(_boxStyle);
                GUILayout.Label("事件订阅", _subHeaderStyle);

                var subscribeAttributes = selectedModule.GetType().GetMethods()
                    .SelectMany(m => m.GetCustomAttributes(typeof(SubscribeEventAttribute), true)
                        .Cast<SubscribeEventAttribute>()
                        .Select(attr => new { Method = m, Attribute = attr }))
                    .ToList();

                if (subscribeAttributes.Count > 0)
                {
                    foreach (var subscription in subscribeAttributes)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("订阅:", _boldLabelStyle, GUILayout.Width(100));
                        EditorGUILayout.LabelField(
                            $"{subscription.Attribute.EventType.Name} -> {subscription.Method.Name}");
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("无事件订阅");
                }

                EditorGUILayout.EndVertical();

                // 请求处理
                EditorGUILayout.BeginVertical(_boxStyle);
                GUILayout.Label("请求处理", _subHeaderStyle);

                var requestHandlerAttributes = selectedModule.GetType().GetMethods()
                    .SelectMany(m => m.GetCustomAttributes(typeof(RequestHandlerAttribute), true)
                        .Cast<RequestHandlerAttribute>()
                        .Select(attr => new { Method = m, Attribute = attr }))
                    .ToList();

                if (requestHandlerAttributes.Count > 0)
                {
                    foreach (var handler in requestHandlerAttributes)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("处理:", _boldLabelStyle, GUILayout.Width(100));
                        EditorGUILayout.LabelField($"{handler.Attribute.RequestType.Name} -> {handler.Method.Name}");
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("无请求处理");
                }

                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("请选择一个模块查看详情。", MessageType.Info);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawEventsTab()
        {
            EditorGUILayout.BeginVertical();

            GUILayout.Label("事件监控", _headerStyle);

            if (!_isFrameworkInitialized)
            {
                EditorGUILayout.HelpBox("框架未初始化，请在运行时查看事件信息。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // 事件控制面板
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            _pauseEventLogging = GUILayout.Toggle(_pauseEventLogging, _pauseEventLogging ? "继续" : "暂停",
                EditorStyles.toolbarButton);

            if (GUILayout.Button("清除", EditorStyles.toolbarButton))
            {
                _eventLogs.Clear();
            }

            GUILayout.FlexibleSpace();

            GUILayout.Label("事件类型:", GUILayout.Width(60));
            _eventTypeFilter =
                EditorGUILayout.TextField(_eventTypeFilter, EditorStyles.toolbarTextField, GUILayout.Width(100));

            _autoScrollEvents = GUILayout.Toggle(_autoScrollEvents, "自动滚动", EditorStyles.toolbarButton);

            EditorGUILayout.EndHorizontal();

            // 事件日志列表
            EditorGUILayout.BeginVertical(_boxStyle);

            if (_eventLogs.Count == 0)
            {
                EditorGUILayout.LabelField("暂无事件日志");
            }
            else
            {
                // 表头
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.Label("时间", EditorStyles.toolbarButton, GUILayout.Width(70));
                GUILayout.Label("事件类型", EditorStyles.toolbarButton, GUILayout.Width(150));
                GUILayout.Label("事件数据", EditorStyles.toolbarButton);
                EditorGUILayout.EndHorizontal();

                // 事件列表
                var filteredEvents = _eventLogs
                    .Where(e => string.IsNullOrEmpty(_eventTypeFilter) ||
                                e.EventType.Contains(_eventTypeFilter, StringComparison.OrdinalIgnoreCase) ||
                                (e.EventData != null && e.EventData.ToString()
                                    .Contains(_eventTypeFilter, StringComparison.OrdinalIgnoreCase)))
                    .Where(e => string.IsNullOrEmpty(_searchText) ||
                                e.EventType.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                                (e.EventData != null && e.EventData.ToString()
                                    .Contains(_searchText, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                foreach (var eventLog in filteredEvents)
                {
                    EditorGUILayout.BeginHorizontal(_eventBoxStyle);

                    EditorGUILayout.LabelField(eventLog.Timestamp.ToString("HH:mm:ss"), GUILayout.Width(70));
                    EditorGUILayout.LabelField(eventLog.EventType, _boldLabelStyle, GUILayout.Width(150));

                    string eventDataString = "无数据";
                    if (eventLog.EventData != null)
                    {
                        if (eventLog.EventData is ModuleRegisteredEvent moduleRegEvent)
                        {
                            eventDataString = $"模块: {moduleRegEvent.ModuleName}";
                        }
                        else if (eventLog.EventData is ModuleStateChangedEvent moduleStateEvent)
                        {
                            eventDataString = $"模块: {moduleStateEvent.ModuleName}, 状态: {moduleStateEvent.NewState}";
                        }
                        else if (eventLog.EventData is ConfigChangedEvent configEvent)
                        {
                            eventDataString = $"键: {configEvent.Key}, 值: {configEvent.NewValue}";
                        }
                        else
                        {
                            eventDataString = eventLog.EventData.ToString();
                        }
                    }

                    EditorGUILayout.LabelField(eventDataString);

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();

            // 循环依赖警告
            if (_showCircularDependencies && _circularDependencies.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(_boxStyle);
                GUILayout.Label("循环依赖警告", _subHeaderStyle);

                GUI.color = Color.red;
                EditorGUILayout.HelpBox($"发现 {_circularDependencies.Count} 个循环依赖，这可能导致模块初始化失败！", MessageType.Error);
                GUI.color = Color.white;

                foreach (var circular in _circularDependencies)
                {
                    EditorGUILayout.BeginVertical(_eventBoxStyle);
                    EditorGUILayout.LabelField("循环路径:", circular.Description);
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndVertical();
            }

            // 初始化顺序建议
            if (_showInitializationOrder)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(_boxStyle);
                GUILayout.Label("建议的初始化顺序", _subHeaderStyle);

                var initializationOrder = DependencyAnalyzer.GetInitializationOrder(_dependencyInfos);

                if (initializationOrder.Count > 0)
                {
                    for (int i = 0; i < initializationOrder.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal(_moduleBoxStyle);
                        EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(30));
                        EditorGUILayout.LabelField(initializationOrder[i]);

                        // 显示依赖深度
                        if (_dependencyInfos.TryGetValue(initializationOrder[i], out var info))
                        {
                            EditorGUILayout.LabelField($"深度: {info.DependencyDepth}", GUILayout.Width(80));
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("无法确定初始化顺序，可能存在循环依赖。", MessageType.Warning);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawConfigTab()
        {
            EditorGUILayout.BeginVertical();

            GUILayout.Label("配置管理", _headerStyle);

            if (!_isFrameworkInitialized)
            {
                EditorGUILayout.HelpBox("框架未初始化，请在运行时查看配置信息。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // 配置控制面板
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton))
            {
                RefreshConfigValues();
            }

            if (GUILayout.Button("保存", EditorStyles.toolbarButton))
            {
                FrameworkManager.Instance.ConfigManager.Save();
            }

            GUILayout.FlexibleSpace();

            GUILayout.Label("键:", GUILayout.Width(30));
            _configKeyFilter =
                EditorGUILayout.TextField(_configKeyFilter, EditorStyles.toolbarTextField, GUILayout.Width(100));

            _showAddConfigPanel = GUILayout.Toggle(_showAddConfigPanel, "添加配置", EditorStyles.toolbarButton);

            EditorGUILayout.EndHorizontal();

            // 添加配置面板
            if (_showAddConfigPanel)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                GUILayout.Label("添加新配置", _subHeaderStyle);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("键:", GUILayout.Width(50));
                _newConfigKey = EditorGUILayout.TextField(_newConfigKey);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("值:", GUILayout.Width(50));
                _newConfigValue = EditorGUILayout.TextField(_newConfigValue);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("添加", _buttonStyle))
                {
                    if (!string.IsNullOrEmpty(_newConfigKey))
                    {
                        // 尝试解析值
                        object value = _newConfigValue;

                        // 尝试解析为数字
                        if (int.TryParse(_newConfigValue, out int intValue))
                        {
                            value = intValue;
                        }
                        else if (float.TryParse(_newConfigValue, out float floatValue))
                        {
                            value = floatValue;
                        }
                        // 尝试解析为布尔值
                        else if (bool.TryParse(_newConfigValue, out bool boolValue))
                        {
                            value = boolValue;
                        }

                        FrameworkManager.Instance.ConfigManager.SetValue(_newConfigKey, value);
                        RefreshConfigValues();

                        _newConfigKey = "";
                        _newConfigValue = "";
                        _showAddConfigPanel = false;
                    }
                }

                if (GUILayout.Button("取消", _buttonStyle))
                {
                    _newConfigKey = "";
                    _newConfigValue = "";
                    _showAddConfigPanel = false;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();
            }

            // 配置列表
            EditorGUILayout.BeginVertical(_boxStyle);

            if (_configValues.Count == 0)
            {
                EditorGUILayout.LabelField("暂无配置项");
            }
            else
            {
                // 表头
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.Label("键", EditorStyles.toolbarButton, GUILayout.Width(200));
                GUILayout.Label("值", EditorStyles.toolbarButton, GUILayout.Width(200));
                GUILayout.Label("类型", EditorStyles.toolbarButton, GUILayout.Width(100));
                GUILayout.Label("操作", EditorStyles.toolbarButton);
                EditorGUILayout.EndHorizontal();

                // 配置项
                var filteredConfigs = _configValues
                    .Where(kv => string.IsNullOrEmpty(_configKeyFilter) ||
                                 kv.Key.Contains(_configKeyFilter, StringComparison.OrdinalIgnoreCase))
                    .Where(kv => string.IsNullOrEmpty(_searchText) ||
                                 kv.Key.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                                 (kv.Value != null && kv.Value.ToString()
                                     .Contains(_searchText, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                foreach (var config in filteredConfigs)
                {
                    EditorGUILayout.BeginHorizontal(_configBoxStyle);

                    EditorGUILayout.LabelField(config.Key, GUILayout.Width(200));

                    string valueString = config.Value?.ToString() ?? "null";
                    string typeName = config.Value?.GetType().Name ?? "null";

                    // 根据类型显示不同的编辑控件
                    object newValue = config.Value;

                    if (config.Value is bool boolValue)
                    {
                        bool newBool = EditorGUILayout.Toggle(boolValue, GUILayout.Width(200));
                        if (newBool != boolValue)
                        {
                            newValue = newBool;
                            FrameworkManager.Instance.ConfigManager.SetValue(config.Key, newValue);
                        }
                    }
                    else if (config.Value is int intValue)
                    {
                        int newInt = EditorGUILayout.IntField(intValue, GUILayout.Width(200));
                        if (newInt != intValue)
                        {
                            newValue = newInt;
                            FrameworkManager.Instance.ConfigManager.SetValue(config.Key, newValue);
                        }
                    }
                    else if (config.Value is float floatValue)
                    {
                        float newFloat = EditorGUILayout.FloatField(floatValue, GUILayout.Width(200));
                        if (Math.Abs(newFloat - floatValue) > 0.0001f)
                        {
                            newValue = newFloat;
                            FrameworkManager.Instance.ConfigManager.SetValue(config.Key, newValue);
                        }
                    }
                    else if (config.Value is string stringValue)
                    {
                        string newString = EditorGUILayout.TextField(stringValue, GUILayout.Width(200));
                        if (newString != stringValue)
                        {
                            newValue = newString;
                            FrameworkManager.Instance.ConfigManager.SetValue(config.Key, newValue);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField(valueString, GUILayout.Width(200));
                    }

                    EditorGUILayout.LabelField(typeName, GUILayout.Width(100));

                    if (GUILayout.Button("删除", _buttonStyle))
                    {
                        if (EditorUtility.DisplayDialog("删除配置", $"确定要删除配置项 {config.Key} 吗？", "确定", "取消"))
                        {
                            FrameworkManager.Instance.ConfigManager.RemoveValue(config.Key);
                            RefreshConfigValues();
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
        }

        private void DrawConsoleTab()
        {
            EditorGUILayout.BeginVertical();

            GUILayout.Label("框架控制台", _headerStyle);

            // 控制台控制面板
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("清除", EditorStyles.toolbarButton))
            {
                _consoleLogs.Clear();
            }

            GUILayout.FlexibleSpace();

            _showInfoLogs = GUILayout.Toggle(_showInfoLogs, "信息", EditorStyles.toolbarButton);
            _showWarningLogs = GUILayout.Toggle(_showWarningLogs, "警告", EditorStyles.toolbarButton);
            _showErrorLogs = GUILayout.Toggle(_showErrorLogs, "错误", EditorStyles.toolbarButton);

            _autoScrollConsole = GUILayout.Toggle(_autoScrollConsole, "自动滚动", EditorStyles.toolbarButton);

            EditorGUILayout.EndHorizontal();

            // 日志列表
            EditorGUILayout.BeginVertical(_boxStyle);

            if (_consoleLogs.Count == 0)
            {
                EditorGUILayout.LabelField("暂无日志");
            }
            else
            {
                // 过滤日志
                var filteredLogs = _consoleLogs
                    .Where(log =>
                        (log.LogType == LogType.Log && _showInfoLogs) ||
                        (log.LogType == LogType.Warning && _showWarningLogs) ||
                        (log.LogType == LogType.Error && _showErrorLogs) ||
                        (log.LogType == LogType.Exception && _showErrorLogs))
                    .Where(log => string.IsNullOrEmpty(_searchText) ||
                                  log.Message.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var log in filteredLogs)
                {
                    GUIStyle logStyle = _consoleBoxStyle;
                    Color textColor = Color.white;

                    switch (log.LogType)
                    {
                        case LogType.Warning:
                            textColor = Color.yellow;
                            break;
                        case LogType.Error:
                        case LogType.Exception:
                            textColor = Color.red;
                            break;
                    }

                    EditorGUILayout.BeginVertical(logStyle);

                    EditorGUILayout.BeginHorizontal();

                    GUI.color = textColor;
                    EditorGUILayout.LabelField(log.Timestamp.ToString("HH:mm:ss"), GUILayout.Width(70));

                    string logTypeString = log.LogType.ToString();
                    EditorGUILayout.LabelField(logTypeString, _boldLabelStyle, GUILayout.Width(70));

                    EditorGUILayout.LabelField(log.Message);
                    GUI.color = Color.white;

                    EditorGUILayout.EndHorizontal();

                    // 显示堆栈跟踪（可折叠）
                    if (!string.IsNullOrEmpty(log.StackTrace))
                    {
                        log.ShowStackTrace = EditorGUILayout.Foldout(log.ShowStackTrace, "堆栈跟踪");

                        if (log.ShowStackTrace)
                        {
                            EditorGUILayout.TextArea(log.StackTrace, GUILayout.Height(100));
                        }
                    }

                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
        }

        private void DrawDependenciesTab()
        {
            EditorGUILayout.BeginVertical();

            GUILayout.Label("模块依赖关系", _headerStyle);

            if (!_isFrameworkInitialized)
            {
                EditorGUILayout.HelpBox("框架未初始化，请在运行时查看依赖关系。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // 依赖关系控制面板
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton))
            {
                UpdateModuleDependencies();
            }

            _showDependencyGraph = GUILayout.Toggle(_showDependencyGraph, "显示依赖图", EditorStyles.toolbarButton);
            _showCircularDependencies =
                GUILayout.Toggle(_showCircularDependencies, "显示循环依赖", EditorStyles.toolbarButton);
            _showInitializationOrder =
                GUILayout.Toggle(_showInitializationOrder, "显示初始化顺序", EditorStyles.toolbarButton);

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 依赖关系概览
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("依赖关系概览", _subHeaderStyle);

            int totalModules = _modules.Count;
            int modulesWithDependencies = _moduleDependencies.Count(kv => kv.Value.Count > 0);
            int totalDependencies = _moduleDependencies.Values.Sum(deps => deps.Count);
            int circularDependencyCount = _circularDependencies.Count;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("模块总数:", totalModules.ToString());
            EditorGUILayout.LabelField("有依赖的模块:", modulesWithDependencies.ToString());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("总依赖数:", totalDependencies.ToString());
            EditorGUILayout.LabelField("平均依赖数:",
                totalModules > 0 ? (totalDependencies / (float)totalModules).ToString("F1") : "0");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("循环依赖:", circularDependencyCount.ToString());
            if (circularDependencyCount > 0)
            {
                GUI.color = Color.red;
                EditorGUILayout.LabelField("⚠️ 发现循环依赖!");
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("✓ 无循环依赖");
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 模块依赖列表
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("模块依赖详情", _subHeaderStyle);

            foreach (var module in _modules)
            {
                if (!string.IsNullOrEmpty(_searchText) &&
                    !module.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                EditorGUILayout.BeginVertical(_moduleBoxStyle);

                // 模块名称和状态
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(module.Name, _boldLabelStyle, GUILayout.Width(200));

                // 状态指示器
                switch (module.State)
                {
                    case ModuleState.Uninitialized:
                        GUI.color = Color.gray;
                        GUILayout.Box("未初始化", GUILayout.Width(60));
                        break;
                    case ModuleState.Initialized:
                        GUI.color = Color.yellow;
                        GUILayout.Box("已初始化", GUILayout.Width(60));
                        break;
                    case ModuleState.Started:
                        GUI.color = Color.green;
                        GUILayout.Box("已启动", GUILayout.Width(60));
                        break;
                    case ModuleState.Shutdown:
                        GUI.color = Color.red;
                        GUILayout.Box("已关闭", GUILayout.Width(60));
                        break;
                }

                GUI.color = Color.white;

                EditorGUILayout.EndHorizontal();

                // 依赖信息
                if (_moduleDependencies.TryGetValue(module.Name, out var dependencies))
                {
                    if (dependencies.Count > 0)
                    {
                        EditorGUILayout.LabelField("依赖模块:", string.Join(", ", dependencies));
                    }
                    else
                    {
                        EditorGUILayout.LabelField("依赖模块:", "无");
                    }
                }

                // 被依赖信息
                if (_moduleDependents.TryGetValue(module.Name, out var dependents))
                {
                    if (dependents.Count > 0)
                    {
                        EditorGUILayout.LabelField("被依赖模块:", string.Join(", ", dependents));
                    }
                    else
                    {
                        EditorGUILayout.LabelField("被依赖模块:", "无");
                    }
                }

                // 依赖深度信息
                if (_dependencyInfos.TryGetValue(module.Name, out var dependencyInfo))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("依赖深度:", dependencyInfo.DependencyDepth.ToString());

                    if (dependencyInfo.HasCircularDependency)
                    {
                        GUI.color = Color.red;
                        EditorGUILayout.LabelField("⚠️ 存在循环依赖");
                        GUI.color = Color.white;
                    }
                    else
                    {
                        GUI.color = Color.green;
                        EditorGUILayout.LabelField("✓ 无循环依赖");
                        GUI.color = Color.white;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
        }

        private void DrawPerformanceTab()
        {
            EditorGUILayout.BeginVertical();

            GUILayout.Label("性能监控", _headerStyle);

            if (!_isFrameworkInitialized)
            {
                EditorGUILayout.HelpBox("框架未初始化，请在运行时查看性能数据。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // 性能控制面板
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton))
            {
                RefreshPerformanceData();
            }

            _autoRefreshPerformance = GUILayout.Toggle(_autoRefreshPerformance, "自动刷新", EditorStyles.toolbarButton);

            if (_autoRefreshPerformance)
            {
                GUILayout.Label("刷新间隔:", GUILayout.Width(60));
                _performanceRefreshInterval =
                    EditorGUILayout.Slider(_performanceRefreshInterval, 0.5f, 10.0f, GUILayout.Width(100));
            }

            _showPerformanceDetails = GUILayout.Toggle(_showPerformanceDetails, "显示详情", EditorStyles.toolbarButton);

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 性能概览
            if (_performanceStats.Count > 0)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                GUILayout.Label("性能概览", _subHeaderStyle);

                int totalOperations = _performanceStats.Count;
                int totalCalls = _performanceStats.Values.Sum(s => s.CallCount);
                float totalTime = _performanceStats.Values.Sum(s => s.TotalTime);
                float maxTime = _performanceStats.Values.Max(s => s.MaxTime);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("操作总数:", totalOperations.ToString());
                EditorGUILayout.LabelField("调用总次数:", totalCalls.ToString());
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("总执行时间:", $"{totalTime:F2} ms");
                EditorGUILayout.LabelField("最大执行时间:", $"{maxTime:F2} ms");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();

                // 性能数据列表
                EditorGUILayout.BeginVertical(_boxStyle);
                GUILayout.Label("性能数据", _subHeaderStyle);

                // 表头
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.Label("操作名称", EditorStyles.toolbarButton, GUILayout.Width(200));
                GUILayout.Label("调用次数", EditorStyles.toolbarButton, GUILayout.Width(80));
                GUILayout.Label("总时间 (ms)", EditorStyles.toolbarButton, GUILayout.Width(100));
                GUILayout.Label("平均时间 (ms)", EditorStyles.toolbarButton, GUILayout.Width(100));
                GUILayout.Label("最大时间 (ms)", EditorStyles.toolbarButton, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();

                // 按总时间排序
                var sortedStats = _performanceStats.OrderByDescending(kv => kv.Value.TotalTime).ToList();

                foreach (var kvp in sortedStats)
                {
                    var operationName = kvp.Key;
                    var stats = kvp.Value;

                    if (!string.IsNullOrEmpty(_searchText) &&
                        !operationName.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    EditorGUILayout.BeginHorizontal(_eventBoxStyle);

                    EditorGUILayout.LabelField(operationName, GUILayout.Width(200));
                    EditorGUILayout.LabelField(stats.CallCount.ToString(), GUILayout.Width(80));
                    EditorGUILayout.LabelField($"{stats.TotalTime:F2}", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"{stats.AverageTime:F2}", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"{stats.MaxTime:F2}", GUILayout.Width(100));

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("暂无性能数据。", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }
    }
}