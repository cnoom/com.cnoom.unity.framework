using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CnoomFramework.Core;

namespace CnoomFramework.Editor
{
    /// <summary>
    /// 模块状态查看器
    /// </summary>
    public class ModuleStatusViewer
    {
        private Vector2 _scrollPosition;
        private string _searchFilter = "";
        private ModuleState _stateFilter = (ModuleState)(-1); // -1 表示显示所有状态
        private bool _showDetails = true;
        private Dictionary<string, bool> _moduleExpanded = new Dictionary<string, bool>();

        public void OnGUI()
        {
            DrawHeader();
            DrawFilters();
            DrawModuleList();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("模块状态监视器", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            var frameworkManager = FrameworkManager.Instance;
            if (frameworkManager == null || !frameworkManager.IsInitialized)
            {
                EditorGUILayout.HelpBox("框架未初始化，无法显示模块信息。", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"已注册模块数量: {frameworkManager.ModuleCount}");
        }

        private void DrawFilters()
        {
            EditorGUILayout.BeginHorizontal();
            
            // 搜索过滤器
            EditorGUILayout.LabelField("搜索:", GUILayout.Width(50));
            _searchFilter = EditorGUILayout.TextField(_searchFilter);
            
            // 状态过滤器
            EditorGUILayout.LabelField("状态:", GUILayout.Width(50));
            var stateNames = new[] { "全部" }.Concat(Enum.GetNames(typeof(ModuleState))).ToArray();
            var stateValues = new[] { -1 }.Concat(Enum.GetValues(typeof(ModuleState)).Cast<int>()).ToArray();
            var currentIndex = Array.IndexOf(stateValues, (int)_stateFilter);
            var newIndex = EditorGUILayout.Popup(currentIndex, stateNames, GUILayout.Width(100));
            _stateFilter = (ModuleState)stateValues[newIndex];
            
            // 详细信息开关
            _showDetails = EditorGUILayout.Toggle("显示详情", _showDetails);
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void DrawModuleList()
        {
            var frameworkManager = FrameworkManager.Instance;
            if (frameworkManager == null || !frameworkManager.IsInitialized)
                return;

            var modules = frameworkManager.Modules;
            var filteredModules = FilterModules(modules);

            if (filteredModules.Count == 0)
            {
                EditorGUILayout.HelpBox("没有找到匹配的模块。", MessageType.Info);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var module in filteredModules)
            {
                DrawModuleItem(module);
            }

            EditorGUILayout.EndScrollView();
        }

        private List<IModule> FilterModules(IReadOnlyList<IModule> modules)
        {
            var filtered = modules.AsEnumerable();

            // 按名称过滤
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                filtered = filtered.Where(m => m.Name.ToLower().Contains(_searchFilter.ToLower()));
            }

            // 按状态过滤
            if (_stateFilter != (ModuleState)(-1))
            {
                filtered = filtered.Where(m => m.State == _stateFilter);
            }

            return filtered.OrderBy(m => m.Priority).ThenBy(m => m.Name).ToList();
        }

        private void DrawModuleItem(IModule module)
        {
            var isExpanded = _moduleExpanded.GetValueOrDefault(module.Name, false);
            
            EditorGUILayout.BeginVertical("box");
            
            // 模块头部信息
            EditorGUILayout.BeginHorizontal();
            
            // 展开/折叠按钮
            if (_showDetails)
            {
                var newExpanded = EditorGUILayout.Foldout(isExpanded, "", true);
                if (newExpanded != isExpanded)
                {
                    _moduleExpanded[module.Name] = newExpanded;
                }
            }
            
            // 状态指示器
            var statusColor = GetStatusColor(module.State);
            var originalColor = GUI.color;
            GUI.color = statusColor;
            GUILayout.Label("●", GUILayout.Width(15));
            GUI.color = originalColor;
            
            // 模块名称
            EditorGUILayout.LabelField(module.Name, EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            // 状态文本
            EditorGUILayout.LabelField(module.State.ToString(), GUILayout.Width(100));
            
            // 优先级
            EditorGUILayout.LabelField($"优先级: {module.Priority}", GUILayout.Width(80));
            
            EditorGUILayout.EndHorizontal();

            // 详细信息
            if (_showDetails && _moduleExpanded.GetValueOrDefault(module.Name, false))
            {
                DrawModuleDetails(module);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawModuleDetails(IModule module)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.BeginVertical("box");
            
            // 基本信息
            EditorGUILayout.LabelField("详细信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"类型: {module.GetType().Name}");
            EditorGUILayout.LabelField($"命名空间: {module.GetType().Namespace}");
            EditorGUILayout.LabelField($"程序集: {module.GetType().Assembly.GetName().Name}");
            
            // 依赖信息
            var dependsOnAttributes = module.GetType().GetCustomAttributes(typeof(CnoomFramework.Core.Attributes.DependsOnAttribute), true);
            if (dependsOnAttributes.Length > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("依赖模块:", EditorStyles.boldLabel);
                foreach (CnoomFramework.Core.Attributes.DependsOnAttribute attr in dependsOnAttributes)
                {
                    EditorGUILayout.LabelField($"  • {attr.ModuleType.Name}");
                }
            }

            // 操作按钮
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            
            if (module.State == ModuleState.Uninitialized)
            {
                if (GUILayout.Button("初始化", GUILayout.Width(80)))
                {
                    try
                    {
                        module.Init();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"初始化模块 {module.Name} 失败: {ex.Message}");
                    }
                }
            }
            
            if (module.State == ModuleState.Initialized)
            {
                if (GUILayout.Button("启动", GUILayout.Width(80)))
                {
                    try
                    {
                        module.Start();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"启动模块 {module.Name} 失败: {ex.Message}");
                    }
                }
            }
            
            if (module.State == ModuleState.Started)
            {
                if (GUILayout.Button("关闭", GUILayout.Width(80)))
                {
                    try
                    {
                        module.Shutdown();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"关闭模块 {module.Name} 失败: {ex.Message}");
                    }
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            EditorGUI.indentLevel--;
        }

        private Color GetStatusColor(ModuleState state)
        {
            switch (state)
            {
                case ModuleState.Uninitialized:
                    return Color.gray;
                case ModuleState.Initialized:
                    return Color.yellow;
                case ModuleState.Started:
                    return Color.green;
                case ModuleState.Shutdown:
                    return Color.red;
                default:
                    return Color.white;
            }
        }
    }
}