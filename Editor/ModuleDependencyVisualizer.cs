using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CnoomFramework.Core;

namespace CnoomFramework.Editor
{
    /// <summary>
    /// 模块依赖关系可视化器
    /// </summary>
    public class ModuleDependencyVisualizer : EditorWindow
    {
        private Vector2 _scrollPosition;
        private Dictionary<Type, Vector2> _nodePositions = new Dictionary<Type, Vector2>();
        private Dictionary<Type, Rect> _nodeRects = new Dictionary<Type, Rect>();
        private List<Type> _moduleTypes = new List<Type>();
        private Dictionary<Type, List<Type>> _dependencyGraph = new Dictionary<Type, List<Type>>();
        private Type _selectedModule;
        private bool _showOnlyRegisteredModules = true;
        private bool _autoLayout = true;

        private const float NODE_WIDTH = 150f;
        private const float NODE_HEIGHT = 60f;
        private const float NODE_SPACING = 200f;

        [MenuItem("Tools/Cnoom Framework/Module Dependency Visualizer")]
        public static void ShowWindow()
        {
            var window = GetWindow<ModuleDependencyVisualizer>("Module Dependencies");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshModuleData();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawVisualization();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton))
            {
                RefreshModuleData();
            }

            _showOnlyRegisteredModules = GUILayout.Toggle(_showOnlyRegisteredModules, "仅显示已注册模块", EditorStyles.toolbarButton);

            _autoLayout = GUILayout.Toggle(_autoLayout, "自动布局", EditorStyles.toolbarButton);

            if (GUILayout.Button("重置布局", EditorStyles.toolbarButton))
            {
                ResetLayout();
            }

            GUILayout.FlexibleSpace();

            // 显示统计信息
            GUILayout.Label($"模块数: {_moduleTypes.Count}", EditorStyles.toolbarButton);

            var circularDeps = FrameworkEditorUtilities.DetectCircularDependencies(_dependencyGraph);
            if (circularDeps.Count > 0)
            {
                var originalColor = GUI.color;
                GUI.color = Color.red;
                GUILayout.Label($"循环依赖: {circularDeps.Count}", EditorStyles.toolbarButton);
                GUI.color = originalColor;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawVisualization()
        {
            var rect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            
            // 绘制背景
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.3f));

            // 开始滚动视图
            _scrollPosition = GUI.BeginScrollView(rect, _scrollPosition, GetContentRect());

            // 绘制连接线
            DrawConnections();

            // 绘制节点
            DrawNodes();

            // 处理事件
            HandleEvents();

            GUI.EndScrollView();

            // 绘制选中模块的详细信息
            DrawSelectedModuleInfo();
        }

        private Rect GetContentRect()
        {
            if (_nodePositions.Count == 0)
                return new Rect(0, 0, 1000, 1000);

            var minX = _nodePositions.Values.Min(p => p.x) - NODE_WIDTH;
            var maxX = _nodePositions.Values.Max(p => p.x) + NODE_WIDTH * 2;
            var minY = _nodePositions.Values.Min(p => p.y) - NODE_HEIGHT;
            var maxY = _nodePositions.Values.Max(p => p.y) + NODE_HEIGHT * 2;

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        private void DrawNodes()
        {
            foreach (var moduleType in _moduleTypes)
            {
                DrawNode(moduleType);
            }
        }

        private void DrawNode(Type moduleType)
        {
            var position = _nodePositions.GetValueOrDefault(moduleType, Vector2.zero);
            var rect = new Rect(position.x, position.y, NODE_WIDTH, NODE_HEIGHT);
            _nodeRects[moduleType] = rect;

            // 确定节点颜色
            var nodeColor = GetNodeColor(moduleType);
            var isSelected = _selectedModule == moduleType;

            if (isSelected)
            {
                // 绘制选中边框
                var borderRect = new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4);
                EditorGUI.DrawRect(borderRect, Color.yellow);
            }

            // 绘制节点背景
            EditorGUI.DrawRect(rect, nodeColor);

            // 绘制节点边框
            var borderColor = isSelected ? Color.yellow : Color.gray;
            DrawRectBorder(rect, borderColor, 1f);

            // 绘制节点文本
            var labelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                normal = { textColor = Color.white }
            };

            var moduleName = moduleType.Name;
            if (moduleName.Length > 15)
                moduleName = moduleName.Substring(0, 12) + "...";

            GUI.Label(rect, moduleName, labelStyle);

            // 绘制状态指示器
            DrawNodeStatusIndicator(rect, moduleType);
        }

        private void DrawNodeStatusIndicator(Rect nodeRect, Type moduleType)
        {
            var frameworkManager = FrameworkManager.Instance;
            if (frameworkManager == null || !frameworkManager.IsInitialized)
                return;

            var module = frameworkManager.GetModule(moduleType);
            if (module == null)
                return;

            var statusColor = GetModuleStatusColor(module.State);
            var statusRect = new Rect(nodeRect.xMax - 15, nodeRect.y + 5, 10, 10);
            EditorGUI.DrawRect(statusRect, statusColor);
        }

        private void DrawConnections()
        {
            foreach (var kvp in _dependencyGraph)
            {
                var fromType = kvp.Key;
                var dependencies = kvp.Value;

                if (!_nodePositions.ContainsKey(fromType))
                    continue;

                var fromPos = _nodePositions[fromType];
                var fromCenter = new Vector2(fromPos.x + NODE_WIDTH / 2, fromPos.y + NODE_HEIGHT / 2);

                foreach (var dependency in dependencies)
                {
                    if (!_nodePositions.ContainsKey(dependency))
                        continue;

                    var toPos = _nodePositions[dependency];
                    var toCenter = new Vector2(toPos.x + NODE_WIDTH / 2, toPos.y + NODE_HEIGHT / 2);

                    // 绘制箭头
                    DrawArrow(fromCenter, toCenter, Color.white);
                }
            }
        }

        private void DrawArrow(Vector2 from, Vector2 to, Color color)
        {
            Handles.color = color;
            Handles.DrawLine(from, to);

            // 绘制箭头头部
            var direction = (to - from).normalized;
            var arrowSize = 10f;
            var arrowAngle = 30f * Mathf.Deg2Rad;

            var arrowPoint1 = to - direction * arrowSize;
            var perpendicular = new Vector2(-direction.y, direction.x);
            
            var arrowLeft = arrowPoint1 + perpendicular * Mathf.Sin(arrowAngle) * arrowSize;
            var arrowRight = arrowPoint1 - perpendicular * Mathf.Sin(arrowAngle) * arrowSize;

            Handles.DrawLine(to, arrowLeft);
            Handles.DrawLine(to, arrowRight);
        }

        private void DrawRectBorder(Rect rect, Color color, float width)
        {
            // 绘制矩形边框
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, width), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - width, rect.width, width), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, width, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - width, rect.y, width, rect.height), color);
        }

        private void HandleEvents()
        {
            var currentEvent = Event.current;

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                // 检查是否点击了节点
                foreach (var kvp in _nodeRects)
                {
                    if (kvp.Value.Contains(currentEvent.mousePosition))
                    {
                        _selectedModule = kvp.Key;
                        currentEvent.Use();
                        Repaint();
                        break;
                    }
                }
            }
        }

        private void DrawSelectedModuleInfo()
        {
            if (_selectedModule == null)
                return;

            var rect = new Rect(10, position.height - 150, 300, 140);
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 0.8f));

            var labelRect = new Rect(rect.x + 10, rect.y + 10, rect.width - 20, 20);
            EditorGUI.LabelField(labelRect, $"模块: {_selectedModule.Name}", EditorStyles.boldLabel);

            labelRect.y += 25;
            var autoRegisterAttr = FrameworkEditorUtilities.GetAutoRegisterAttribute(_selectedModule);
            if (autoRegisterAttr != null)
            {
                EditorGUI.LabelField(labelRect, $"自动注册: 是 (优先级: {autoRegisterAttr.Priority})");
            }
            else
            {
                EditorGUI.LabelField(labelRect, "自动注册: 否");
            }

            labelRect.y += 20;
            var dependencies = FrameworkEditorUtilities.GetModuleDependencies(_selectedModule);
            EditorGUI.LabelField(labelRect, $"依赖数量: {dependencies.Count}");

            labelRect.y += 20;
            var eventHandlers = FrameworkEditorUtilities.GetEventHandlerMethods(_selectedModule);
            EditorGUI.LabelField(labelRect, $"事件处理器: {eventHandlers.Count}");

            // 显示模块状态（如果已注册）
            var frameworkManager = FrameworkManager.Instance;
            if (frameworkManager?.IsInitialized == true)
            {
                var module = frameworkManager.GetModule(_selectedModule);
                if (module != null)
                {
                    labelRect.y += 20;
                    EditorGUI.LabelField(labelRect, $"状态: {module.State}");
                }
            }
        }

        private void RefreshModuleData()
        {
            // 获取模块类型
            if (_showOnlyRegisteredModules)
            {
                var frameworkManager = FrameworkManager.Instance;
                if (frameworkManager?.IsInitialized == true)
                {
                    _moduleTypes = frameworkManager.Modules.Select(m => m.GetType()).ToList();
                }
                else
                {
                    _moduleTypes.Clear();
                }
            }
            else
            {
                _moduleTypes = FrameworkEditorUtilities.GetAllModuleTypes();
            }

            // 创建依赖关系图
            _dependencyGraph = FrameworkEditorUtilities.CreateDependencyGraph(_moduleTypes);

            // 自动布局
            if (_autoLayout)
            {
                AutoLayoutNodes();
            }
            else
            {
                InitializeNodePositions();
            }

            Repaint();
        }

        private void InitializeNodePositions()
        {
            for (int i = 0; i < _moduleTypes.Count; i++)
            {
                var moduleType = _moduleTypes[i];
                if (!_nodePositions.ContainsKey(moduleType))
                {
                    var x = (i % 5) * NODE_SPACING + 50;
                    var y = (i / 5) * NODE_SPACING + 50;
                    _nodePositions[moduleType] = new Vector2(x, y);
                }
            }
        }

        private void AutoLayoutNodes()
        {
            _nodePositions.Clear();

            // 简单的层次布局算法
            var layers = new List<List<Type>>();
            var visited = new HashSet<Type>();
            var inDegree = new Dictionary<Type, int>();

            // 计算入度
            foreach (var moduleType in _moduleTypes)
            {
                inDegree[moduleType] = 0;
            }

            foreach (var kvp in _dependencyGraph)
            {
                foreach (var dependency in kvp.Value)
                {
                    if (inDegree.ContainsKey(dependency))
                    {
                        inDegree[dependency]++;
                    }
                }
            }

            // 拓扑排序分层
            var queue = new Queue<Type>(_moduleTypes.Where(t => inDegree[t] == 0));
            
            while (queue.Count > 0)
            {
                var currentLayer = new List<Type>();
                var layerSize = queue.Count;

                for (int i = 0; i < layerSize; i++)
                {
                    var current = queue.Dequeue();
                    currentLayer.Add(current);
                    visited.Add(current);

                    // 更新依赖此模块的其他模块的入度
                    foreach (var kvp in _dependencyGraph)
                    {
                        if (kvp.Value.Contains(current))
                        {
                            inDegree[kvp.Key]--;
                            if (inDegree[kvp.Key] == 0 && !visited.Contains(kvp.Key))
                            {
                                queue.Enqueue(kvp.Key);
                            }
                        }
                    }
                }

                if (currentLayer.Count > 0)
                {
                    layers.Add(currentLayer);
                }
            }

            // 处理剩余的模块（可能存在循环依赖）
            var remaining = _moduleTypes.Where(t => !visited.Contains(t)).ToList();
            if (remaining.Count > 0)
            {
                layers.Add(remaining);
            }

            // 设置节点位置
            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                var layer = layers[layerIndex];
                var layerY = layerIndex * NODE_SPACING + 50;

                for (int nodeIndex = 0; nodeIndex < layer.Count; nodeIndex++)
                {
                    var nodeX = nodeIndex * NODE_SPACING + 50;
                    _nodePositions[layer[nodeIndex]] = new Vector2(nodeX, layerY);
                }
            }
        }

        private void ResetLayout()
        {
            _nodePositions.Clear();
            if (_autoLayout)
            {
                AutoLayoutNodes();
            }
            else
            {
                InitializeNodePositions();
            }
            Repaint();
        }

        private Color GetNodeColor(Type moduleType)
        {
            var frameworkManager = FrameworkManager.Instance;
            if (frameworkManager?.IsInitialized == true)
            {
                var module = frameworkManager.GetModule(moduleType);
                if (module != null)
                {
                    return GetModuleStatusColor(module.State);
                }
            }

            // 检查是否有自动注册属性
            if (FrameworkEditorUtilities.HasAutoRegisterAttribute(moduleType))
            {
                return new Color(0.3f, 0.5f, 0.8f); // 蓝色表示自动注册
            }

            return new Color(0.5f, 0.5f, 0.5f); // 灰色表示未注册
        }

        private Color GetModuleStatusColor(ModuleState state)
        {
            switch (state)
            {
                case ModuleState.Uninitialized:
                    return new Color(0.6f, 0.6f, 0.6f);
                case ModuleState.Initialized:
                    return new Color(0.8f, 0.8f, 0.3f);
                case ModuleState.Started:
                    return new Color(0.3f, 0.8f, 0.3f);
                case ModuleState.Shutdown:
                    return new Color(0.8f, 0.3f, 0.3f);
                default:
                    return Color.white;
            }
        }
    }
}