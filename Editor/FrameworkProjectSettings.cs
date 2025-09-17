using UnityEditor;
using UnityEngine;
using CnoomFramework.Core;

namespace CnoomFramework.Editor
{
    /// <summary>
    /// Cnoom Framework 项目设置
    /// </summary>
    public class FrameworkProjectSettings : ScriptableObject
    {
        [Header("框架设置")]
        [Tooltip("是否在启动时自动初始化框架")]
        public bool autoInitializeOnStart = true;
        
        [Tooltip("是否启用调试日志")]
        public bool enableDebugLog = true;
        
        [Tooltip("最大缓存事件数量")]
        [Range(100, 10000)]
        public int maxCachedEvents = 1000;

        [Header("性能监控")]
        [Tooltip("是否启用性能监控")]
        public bool enablePerformanceMonitoring = true;
        
        [Tooltip("性能数据更新间隔（秒）")]
        [Range(0.1f, 5.0f)]
        public float performanceUpdateInterval = 1.0f;
        
        [Tooltip("最大性能历史数据点")]
        [Range(50, 1000)]
        public int maxPerformanceHistoryPoints = 300;

        [Header("错误处理")]
        [Tooltip("最大错误历史记录数")]
        [Range(50, 1000)]
        public int maxErrorHistoryCount = 100;
        
        [Tooltip("是否自动恢复模块错误")]
        public bool autoRecoverModuleErrors = true;

        [Header("事件总线")]
        [Tooltip("每帧最大异步处理器数")]
        [Range(1, 200)]
        public int maxAsyncHandlersPerFrame = 64;
        
        [Tooltip("是否启用继承分发")]
        public bool enableInheritanceDispatch = true;

        private static FrameworkProjectSettings _instance;
        
        public static FrameworkProjectSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<FrameworkProjectSettings>("FrameworkProjectSettings");
                    if (_instance == null)
                    {
                        _instance = CreateInstance<FrameworkProjectSettings>();
                        
                        // 创建Resources目录（如果不存在）
                        var resourcesPath = "Assets/Resources";
                        if (!AssetDatabase.IsValidFolder(resourcesPath))
                        {
                            AssetDatabase.CreateFolder("Assets", "Resources");
                        }
                        
                        // 保存设置文件
                        AssetDatabase.CreateAsset(_instance, "Assets/Resources/FrameworkProjectSettings.asset");
                        AssetDatabase.SaveAssets();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 应用设置到框架
        /// </summary>
        public void ApplyToFramework()
        {
            var frameworkManager = FrameworkManager.Instance;
            if (frameworkManager == null) return;

            // 应用配置到ConfigManager
            if (frameworkManager.ConfigManager != null)
            {
                frameworkManager.ConfigManager.SetValue("Framework.AutoInitialize", autoInitializeOnStart);
                frameworkManager.ConfigManager.SetValue("Framework.EnableDebugLog", enableDebugLog);
                frameworkManager.ConfigManager.SetValue("Framework.MaxCachedEvents", maxCachedEvents);
                frameworkManager.ConfigManager.SetValue("Performance.EnableMonitoring", enablePerformanceMonitoring);
                frameworkManager.ConfigManager.SetValue("Performance.UpdateInterval", performanceUpdateInterval);
                frameworkManager.ConfigManager.SetValue("Performance.MaxHistoryPoints", maxPerformanceHistoryPoints);
                frameworkManager.ConfigManager.SetValue("ErrorHandling.MaxHistoryCount", maxErrorHistoryCount);
                frameworkManager.ConfigManager.SetValue("ErrorHandling.AutoRecover", autoRecoverModuleErrors);
                frameworkManager.ConfigManager.SetValue("EventBus.MaxAsyncPerFrame", maxAsyncHandlersPerFrame);
                frameworkManager.ConfigManager.SetValue("EventBus.EnableInheritance", enableInheritanceDispatch);
            }

            Debug.Log("Framework settings applied successfully.");
        }

        /// <summary>
        /// 从框架加载设置
        /// </summary>
        public void LoadFromFramework()
        {
            var frameworkManager = FrameworkManager.Instance;
            if (frameworkManager?.ConfigManager == null) return;

            var config = frameworkManager.ConfigManager;
            autoInitializeOnStart = config.GetValue("Framework.AutoInitialize", autoInitializeOnStart);
            enableDebugLog = config.GetValue("Framework.EnableDebugLog", enableDebugLog);
            maxCachedEvents = config.GetValue("Framework.MaxCachedEvents", maxCachedEvents);
            enablePerformanceMonitoring = config.GetValue("Performance.EnableMonitoring", enablePerformanceMonitoring);
            performanceUpdateInterval = config.GetValue("Performance.UpdateInterval", performanceUpdateInterval);
            maxPerformanceHistoryPoints = config.GetValue("Performance.MaxHistoryPoints", maxPerformanceHistoryPoints);
            maxErrorHistoryCount = config.GetValue("ErrorHandling.MaxHistoryCount", maxErrorHistoryCount);
            autoRecoverModuleErrors = config.GetValue("ErrorHandling.AutoRecover", autoRecoverModuleErrors);
            maxAsyncHandlersPerFrame = config.GetValue("EventBus.MaxAsyncPerFrame", maxAsyncHandlersPerFrame);
            enableInheritanceDispatch = config.GetValue("EventBus.EnableInheritance", enableInheritanceDispatch);

            EditorUtility.SetDirty(this);
        }
    }

    /// <summary>
    /// 框架项目设置提供器
    /// </summary>
    public class FrameworkProjectSettingsProvider : SettingsProvider
    {
        private SerializedObject _serializedSettings;

        public FrameworkProjectSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) { }

        public static bool IsSettingsAvailable()
        {
            return FrameworkProjectSettings.Instance != null;
        }

        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
        {
            _serializedSettings = new SerializedObject(FrameworkProjectSettings.Instance);
        }

        public override void OnGUI(string searchContext)
        {
            if (_serializedSettings == null)
                return;

            EditorGUILayout.LabelField("Cnoom Framework 项目设置", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _serializedSettings.Update();

            // 绘制所有属性
            var iterator = _serializedSettings.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                if (iterator.propertyPath == "m_Script")
                    continue;

                EditorGUILayout.PropertyField(iterator, true);
                enterChildren = false;
            }

            _serializedSettings.ApplyModifiedProperties();

            EditorGUILayout.Space();

            // 操作按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("应用到框架"))
            {
                FrameworkProjectSettings.Instance.ApplyToFramework();
            }

            if (GUILayout.Button("从框架加载"))
            {
                FrameworkProjectSettings.Instance.LoadFromFramework();
            }

            if (GUILayout.Button("重置为默认"))
            {
                if (EditorUtility.DisplayDialog("重置设置", "确定要重置所有设置为默认值吗？", "重置", "取消"))
                {
                    var defaultSettings = ScriptableObject.CreateInstance<FrameworkProjectSettings>();
                    EditorUtility.CopySerialized(defaultSettings, FrameworkProjectSettings.Instance);
                    ScriptableObject.DestroyImmediate(defaultSettings);
                }
            }
            EditorGUILayout.EndHorizontal();

            // 帮助信息
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "这些设置控制 Cnoom Framework 的行为。修改后点击'应用到框架'使设置生效。", 
                MessageType.Info);
        }

        [SettingsProvider]
        public static SettingsProvider CreateFrameworkProjectSettingsProvider()
        {
            if (IsSettingsAvailable())
            {
                var provider = new FrameworkProjectSettingsProvider("Project/Cnoom Framework", SettingsScope.Project);
                provider.keywords = new[] { "Cnoom", "Framework", "Module", "Event", "Performance" };
                return provider;
            }

            return null;
        }
    }
}