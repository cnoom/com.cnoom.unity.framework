using System.Linq;
using UnityEditor;
using UnityEngine;
using CnoomFramework.Core;

namespace CnoomFramework.Editor
{
    /// <summary>
    /// FrameworkManager 自定义Inspector
    /// </summary>
    [CustomEditor(typeof(FrameworkManager))]
    public class FrameworkManagerInspector : UnityEditor.Editor
    {
        private bool _showModules = true;
        private bool _showSystemInfo = true;
        private bool _showDebugControls = true;

        public override void OnInspectorGUI()
        {
            var frameworkManager = (FrameworkManager)target;
            
            EditorGUILayout.LabelField("Cnoom Framework Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 绘制默认属性
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            
            // 框架状态信息
            DrawFrameworkStatus(frameworkManager);
            
            EditorGUILayout.Space();
            
            // 系统信息
            DrawSystemInfo(frameworkManager);
            
            EditorGUILayout.Space();
            
            // 模块信息
            DrawModulesInfo(frameworkManager);
            
            EditorGUILayout.Space();
            
            // 调试控制
            DrawDebugControls(frameworkManager);
        }

        private void DrawFrameworkStatus(FrameworkManager frameworkManager)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("框架状态", EditorStyles.boldLabel);
            
            var isInitialized = frameworkManager.IsInitialized;
            var statusColor = isInitialized ? Color.green : Color.red;
            var statusText = isInitialized ? "已初始化" : "未初始化";
            
            var originalColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField($"状态: {statusText}");
            GUI.color = originalColor;
            
            if (isInitialized)
            {
                EditorGUILayout.LabelField($"模块数量: {frameworkManager.ModuleCount}");
                
                if (frameworkManager.ErrorRecoveryManager != null)
                {
                    var errorStats = frameworkManager.ErrorRecoveryManager.GetErrorStatistics();
                    if (errorStats.TotalErrors > 0)
                    {
                        EditorGUILayout.LabelField($"错误数量: {errorStats.TotalErrors}");
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawSystemInfo(FrameworkManager frameworkManager)
        {
            _showSystemInfo = EditorGUILayout.Foldout(_showSystemInfo, "系统信息", true);
            if (!_showSystemInfo) return;
            
            EditorGUILayout.BeginVertical("box");
            
            if (frameworkManager.IsInitialized)
            {
                // EventBus信息
                if (frameworkManager.EventBus != null)
                {
                    EditorGUILayout.LabelField("事件总线: 已初始化");
                }
                
                // ConfigManager信息
                if (frameworkManager.ConfigManager != null)
                {
                    var configCount = frameworkManager.ConfigManager.GetAllKeys().Count();
                    EditorGUILayout.LabelField($"配置管理器: {configCount} 个配置项");
                }
                
                // ErrorRecoveryManager信息
                if (frameworkManager.ErrorRecoveryManager != null)
                {
                    EditorGUILayout.LabelField("错误恢复管理器: 已初始化");
                }
                
                // MockManager信息
                if (frameworkManager.MockManager != null)
                {
                    EditorGUILayout.LabelField("Mock管理器: 已初始化");
                }
            }
            else
            {
                EditorGUILayout.LabelField("系统组件未初始化");
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawModulesInfo(FrameworkManager frameworkManager)
        {
            _showModules = EditorGUILayout.Foldout(_showModules, "模块信息", true);
            if (!_showModules) return;
            
            EditorGUILayout.BeginVertical("box");
            
            if (frameworkManager.IsInitialized && frameworkManager.ModuleCount > 0)
            {
                var modules = frameworkManager.Modules;
                
                foreach (var module in modules)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    // 状态指示器
                    var statusColor = GetModuleStatusColor(module.State);
                    var originalColor = GUI.color;
                    GUI.color = statusColor;
                    GUILayout.Label("●", GUILayout.Width(15));
                    GUI.color = originalColor;
                    
                    // 模块信息
                    EditorGUILayout.LabelField($"{module.Name} ({module.State})", GUILayout.Width(200));
                    EditorGUILayout.LabelField($"优先级: {module.Priority}", GUILayout.Width(80));
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("暂无模块");
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawDebugControls(FrameworkManager frameworkManager)
        {
            _showDebugControls = EditorGUILayout.Foldout(_showDebugControls, "调试控制", true);
            if (!_showDebugControls) return;
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            
            // 初始化/关闭按钮
            if (!frameworkManager.IsInitialized)
            {
                if (GUILayout.Button("初始化框架"))
                {
                    frameworkManager.Initialize();
                }
            }
            else
            {
                if (GUILayout.Button("关闭框架"))
                {
                    frameworkManager.Shutdown();
                }
                
                if (GUILayout.Button("重启框架"))
                {
                    frameworkManager.Shutdown();
                    frameworkManager.Initialize();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            // 工具窗口按钮
            if (GUILayout.Button("打开调试器"))
            {
                FrameworkDebuggerWindow.ShowWindow();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 快速操作
            if (frameworkManager.IsInitialized)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("快速操作", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("保存配置"))
                {
                    frameworkManager.ConfigManager?.Save();
                    Debug.Log("配置已保存");
                }
                
                if (GUILayout.Button("清空错误历史"))
                {
                    frameworkManager.ErrorRecoveryManager?.ClearErrorHistory();
                    Debug.Log("错误历史已清空");
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }

        private Color GetModuleStatusColor(ModuleState state)
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