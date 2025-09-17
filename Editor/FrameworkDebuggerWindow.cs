using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CnoomFramework.Core;
using CnoomFramework.Core.ErrorHandling;

namespace CnoomFramework.Editor
{
    /// <summary>
    /// Cnoom Framework 主调试器窗口
    /// </summary>
    public class FrameworkDebuggerWindow : EditorWindow
    {
        private enum DebuggerTab
        {
            Overview,
            Modules,
            EventBus,
            Performance,
            Config,
            ErrorLog
        }

        private DebuggerTab _currentTab = DebuggerTab.Overview;
        private Vector2 _scrollPosition;
        private bool _autoRefresh = true;
        private float _refreshInterval = 1.0f;
        private double _lastRefreshTime;

        // 子窗口引用
        private ModuleStatusViewer _moduleViewer;
        private EventBusMonitor _eventMonitor;
        private PerformancePanel _performancePanel;
        private ConfigEditor _configEditor;
        private ErrorLogViewer _errorViewer;

        public static void ShowWindow()
        {
            var window = GetWindow<FrameworkDebuggerWindow>("Framework Debugger");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeSubWindows();
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void InitializeSubWindows()
        {
            _moduleViewer = new ModuleStatusViewer();
            _eventMonitor = new EventBusMonitor();
            _performancePanel = new PerformancePanel();
            _configEditor = new ConfigEditor();
            _errorViewer = new ErrorLogViewer();
        }

        private void OnEditorUpdate()
        {
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > _refreshInterval)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawTabContent();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 框架状态指示器
            var frameworkManager = FrameworkManager.Instance;
            bool isInitialized = frameworkManager != null && frameworkManager.IsInitialized;
            
            var statusColor = isInitialized ? Color.green : Color.red;
            var statusText = isInitialized ? "运行中" : "未初始化";
            
            var originalColor = GUI.color;
            GUI.color = statusColor;
            GUILayout.Label($"框架状态: {statusText}", EditorStyles.toolbarButton, GUILayout.Width(100));
            GUI.color = originalColor;

            GUILayout.FlexibleSpace();

            // 自动刷新控制
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "自动刷新", EditorStyles.toolbarButton);
            
            if (GUILayout.Button("手动刷新", EditorStyles.toolbarButton))
            {
                Repaint();
            }

            // 刷新间隔设置
            GUILayout.Label("间隔:", EditorStyles.miniLabel);
            _refreshInterval = EditorGUILayout.Slider(_refreshInterval, 0.1f, 5.0f, GUILayout.Width(100));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTabContent()
        {
            // 标签页选择
            EditorGUILayout.BeginHorizontal();
            var tabNames = Enum.GetNames(typeof(DebuggerTab));
            var tabDisplayNames = new[] { "概览", "模块", "事件总线", "性能", "配置", "错误日志" };
            
            for (int i = 0; i < tabNames.Length; i++)
            {
                var isSelected = (int)_currentTab == i;
                var style = isSelected ? EditorStyles.toolbarButton : EditorStyles.toolbarButton;
                
                if (isSelected)
                {
                    GUI.backgroundColor = Color.cyan;
                }
                
                if (GUILayout.Button(tabDisplayNames[i], style))
                {
                    _currentTab = (DebuggerTab)i;
                }
                
                if (isSelected)
                {
                    GUI.backgroundColor = Color.white;
                }
            }
            EditorGUILayout.EndHorizontal();

            // 内容区域
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            switch (_currentTab)
            {
                case DebuggerTab.Overview:
                    DrawOverviewTab();
                    break;
                case DebuggerTab.Modules:
                    _moduleViewer?.OnGUI();
                    break;
                case DebuggerTab.EventBus:
                    _eventMonitor?.OnGUI();
                    break;
                case DebuggerTab.Performance:
                    _performancePanel?.OnGUI();
                    break;
                case DebuggerTab.Config:
                    _configEditor?.OnGUI();
                    break;
                case DebuggerTab.ErrorLog:
                    _errorViewer?.OnGUI();
                    break;
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawOverviewTab()
        {
            EditorGUILayout.LabelField("Cnoom Framework 概览", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            var frameworkManager = FrameworkManager.Instance;
            if (frameworkManager == null)
            {
                EditorGUILayout.HelpBox("FrameworkManager 未找到。请确保框架已正确初始化。", MessageType.Warning);
                
                if (GUILayout.Button("尝试获取 FrameworkManager"))
                {
                    frameworkManager = FrameworkManager.Instance;
                }
                return;
            }

            // 基本信息
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("基本信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"框架版本: 1.0.3");
            EditorGUILayout.LabelField($"初始化状态: {(frameworkManager.IsInitialized ? "已初始化" : "未初始化")}");
            EditorGUILayout.LabelField($"注册模块数: {frameworkManager.ModuleCount}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 快速操作
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("快速操作", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("初始化框架"))
            {
                if (!frameworkManager.IsInitialized)
                {
                    frameworkManager.Initialize();
                }
            }
            
            if (GUILayout.Button("关闭框架"))
            {
                if (frameworkManager.IsInitialized)
                {
                    frameworkManager.Shutdown();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 系统状态概览
            if (frameworkManager.IsInitialized)
            {
                DrawSystemStatusOverview(frameworkManager);
            }
        }

        private void DrawSystemStatusOverview(FrameworkManager frameworkManager)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("系统状态", EditorStyles.boldLabel);

            // 模块状态统计
            var modules = frameworkManager.Modules;
            var moduleStats = new Dictionary<ModuleState, int>();
            
            foreach (ModuleState state in Enum.GetValues(typeof(ModuleState)))
            {
                moduleStats[state] = 0;
            }
            
            foreach (var module in modules)
            {
                moduleStats[module.State]++;
            }

            EditorGUILayout.LabelField("模块状态分布:");
            EditorGUI.indentLevel++;
            foreach (var kvp in moduleStats)
            {
                if (kvp.Value > 0)
                {
                    EditorGUILayout.LabelField($"{kvp.Key}: {kvp.Value}");
                }
            }
            EditorGUI.indentLevel--;

            // 错误统计
            if (frameworkManager.ErrorRecoveryManager != null)
            {
                var errorStats = frameworkManager.ErrorRecoveryManager.GetErrorStatistics();
                if (errorStats.TotalErrors > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("错误统计:");
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField($"总错误数: {errorStats.TotalErrors}");
                    EditorGUILayout.LabelField($"严重错误: {errorStats.CriticalSeverityCount}");
                    EditorGUILayout.LabelField($"高级错误: {errorStats.HighSeverityCount}");
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}