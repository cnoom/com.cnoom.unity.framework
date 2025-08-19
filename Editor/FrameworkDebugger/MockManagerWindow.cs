using System;
using System.Collections.Generic;
using System.Linq;
using CnoomFramework.Core;
using CnoomFramework.Core.Mock;
using UnityEditor;
using UnityEngine;

namespace CnoomFramework.Editor
{
    /// <summary>
    /// Mock管理器窗口，用于在编辑器中管理Mock模块
    /// </summary>
    public class MockManagerWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _isFrameworkInitialized = false;
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _buttonStyle;
        private Dictionary<Type, bool> _moduleFoldouts = new Dictionary<Type, bool>();

        [MenuItem("Window/CnoomFramework/Mock Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<MockManagerWindow>("Mock Manager");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            // 检查框架是否已初始化
            EditorApplication.update += CheckFrameworkStatus;
        }

        private void OnDisable()
        {
            // 取消检查框架状态
            EditorApplication.update -= CheckFrameworkStatus;
        }

        private void CheckFrameworkStatus()
        {
            try
            {
                if (Application.isPlaying)
                {
                    // 安全地检查FrameworkManager是否存在和初始化
                    if (FrameworkManager.Instance != null && FrameworkManager.Instance.IsInitialized)
                    {
                        _isFrameworkInitialized = true;
                        Repaint();
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
            }
            catch (Exception ex)
            {
                // 捕获任何异常，确保窗口不会崩溃
                _isFrameworkInitialized = false;
                Debug.LogError($"Mock管理器窗口检查框架状态时出错: {ex.Message}");
            }
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

            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    margin = new RectOffset(5, 5, 5, 5),
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }

            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    margin = new RectOffset(2, 2, 2, 2)
                };
            }
        }

        private void OnGUI()
        {
            InitializeStyles();

            EditorGUILayout.BeginVertical();
            GUILayout.Label("Mock管理器", _headerStyle);

            if (!_isFrameworkInitialized)
            {
                EditorGUILayout.HelpBox("框架未初始化，请在运行时使用Mock管理器。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            try
            {
                var mockManager = FrameworkManager.Instance.MockManager;
                if (mockManager == null)
                {
                    EditorGUILayout.HelpBox("Mock管理器未初始化。", MessageType.Error);
                    EditorGUILayout.EndVertical();
                    return;
                }

                DrawMockControls(mockManager);
            }
            catch (Exception ex)
            {
                EditorGUILayout.HelpBox($"访问Mock管理器时出错: {ex.Message}", MessageType.Error);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMockControls(IMockManager mockManager)
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("已注册的模块", _subHeaderStyle);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // 获取所有已注册的模块
            var modules = FrameworkManager.Instance.Modules;
            var mockedModules = mockManager.GetMockedModules();

            if (modules.Count == 0)
            {
                EditorGUILayout.HelpBox("没有已注册的模块。", MessageType.Info);
            }
            else
            {
                // 按模块类型分组显示
                var modulesByType = new Dictionary<string, List<IModule>>();
                foreach (var module in modules)
                {
                    var typeName = module.GetType().Namespace ?? "其他";
                    if (!modulesByType.ContainsKey(typeName))
                    {
                        modulesByType[typeName] = new List<IModule>();
                    }
                    modulesByType[typeName].Add(module);
                }

                foreach (var typeGroup in modulesByType)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Label($"{typeGroup.Key}", EditorStyles.boldLabel);

                    foreach (var module in typeGroup.Value)
                    {
                        DrawModuleItem(module, mockManager, mockedModules);
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // 清除所有Mock按钮
            if (mockedModules.Count > 0)
            {
                if (GUILayout.Button("清除所有Mock", GUILayout.Height(30)))
                {
                    mockManager.ClearAllMocks();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawModuleItem(IModule module, IMockManager mockManager, IReadOnlyList<Type> mockedModules)
        {
            // 获取模块实现的所有接口
            var interfaces = module.GetType().GetInterfaces()
                .Where(i => typeof(IModule).IsAssignableFrom(i) && i != typeof(IModule))
                .ToList();

            if (interfaces.Count == 0)
            {
                return; // 跳过没有实现特定接口的模块
            }

            // 确保每个模块都有折叠状态
            foreach (var interfaceType in interfaces)
            {
                if (!_moduleFoldouts.ContainsKey(interfaceType))
                {
                    _moduleFoldouts[interfaceType] = false;
                }
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 模块名称和状态
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{module.Name} ({module.State})", EditorStyles.boldLabel);
            
            // 显示是否被Mock
            bool isMocked = interfaces.Any(i => mockedModules.Contains(i));
            if (isMocked)
            {
                GUILayout.Label("已Mock", EditorStyles.boldLabel, GUILayout.Width(60));
            }
            
            EditorGUILayout.EndHorizontal();

            // 模块接口列表
            foreach (var interfaceType in interfaces)
            {
                bool isInterfaceMocked = mockedModules.Contains(interfaceType);
                
                EditorGUILayout.BeginHorizontal();
                
                // 接口名称和折叠控制
                _moduleFoldouts[interfaceType] = EditorGUILayout.Foldout(_moduleFoldouts[interfaceType], 
                    $"接口: {interfaceType.Name}", true);
                
                // Mock/取消Mock按钮
                if (isInterfaceMocked)
                {
                    if (GUILayout.Button("取消Mock", _buttonStyle, GUILayout.Width(80)))
                    {
                        mockManager.RemoveMock(interfaceType);
                    }
                }
                
                EditorGUILayout.EndHorizontal();

                // 如果展开，显示详情
                if (_moduleFoldouts[interfaceType])
                {
                    EditorGUI.indentLevel++;
                    
                    // 显示接口详情
                    EditorGUILayout.LabelField("实现类型:", module.GetType().FullName);
                    
                    // 如果模块实现了IStatefulModule，显示状态信息
                    if (module is IStatefulModule statefulModule)
                    {
                        EditorGUILayout.LabelField("支持状态转移: 是");
                        
                        // 显示状态信息
                        var state = statefulModule.ExportState();
                        if (state != null && state.Count > 0)
                        {
                            EditorGUILayout.LabelField("当前状态:");
                            EditorGUI.indentLevel++;
                            foreach (var item in state)
                            {
                                EditorGUILayout.LabelField($"{item.Key}: {item.Value}");
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("支持状态转移: 否");
                    }
                    
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}