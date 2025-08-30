using CnoomFramework.Core;
using UnityEditor;
using UnityEngine;
using CnoomFramework.Core.Contracts;
using CnoomFramework.Core.Events;

namespace CnoomFramework.Editor
{
    /// <summary>
    /// 契约验证编辑器工具 - 提供简单的配置界面
    /// </summary>
    public static class ContractValidationEditor
    {
        private const string MenuPath = "Tools/CnoomFramework/契约验证/";
        
        [MenuItem(MenuPath + "启用验证", false, 100)]
        public static void EnableValidation()
        {
            var settings = ContractValidationSettings.Instance;
            settings.enableContractValidation = true;
            Debug.Log("契约验证已启用");
        }
        
        [MenuItem(MenuPath + "启用验证", true)]
        public static bool EnableValidationValidate()
        {
            var settings = ContractValidationSettings.Instance;
            return !settings.enableContractValidation;
        }
        
        [MenuItem(MenuPath + "禁用验证", false, 101)]
        public static void DisableValidation()
        {
            var settings = ContractValidationSettings.Instance;
            settings.enableContractValidation = false;
            Debug.Log("契约验证已禁用");
        }
        
        [MenuItem(MenuPath + "禁用验证", true)]
        public static bool DisableValidationValidate()
        {
            var settings = ContractValidationSettings.Instance;
            return settings.enableContractValidation;
        }
        
        [MenuItem(MenuPath + "打开设置", false, 200)]
        public static void OpenSettings()
        {
            var settings = ContractValidationSettings.Instance;
            Selection.activeObject = settings;
        }
        
        [MenuItem(MenuPath + "快速注册框架事件", false, 300)]
        public static void RegisterFrameworkEvents()
        {
            var module = FrameworkManager.Instance?.GetModule<LightweightContractValidationModule>();
            if (module != null)
            {
                module.RegisterEvent<FrameworkInitializedEvent>();
                module.RegisterEvent<FrameworkShutdownEvent>();
                module.RegisterEvent<ModuleRegisteredEvent>();
                module.RegisterEvent<ModuleUnregisteredEvent>();
                Debug.Log("已快速注册框架事件契约");
            }
            else
            {
                Debug.LogWarning("找不到轻量级契约验证模块");
            }
        }
    }

    /// <summary>
    /// 契约验证设置的自定义编辑器
    /// </summary>
    [CustomEditor(typeof(ContractValidationSettings))]
    public class ContractValidationSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("契约验证设置", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "这是为个人开发者设计的简化契约验证系统。\n" +
                "建议在开发阶段启用，发布版本中禁用以提升性能。", 
                MessageType.Info);
            
            EditorGUILayout.Space();
            
            // 绘制默认属性
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            
            // 快速操作按钮
            if (GUILayout.Button("应用当前设置到运行时"))
            {
                ApplySettingsToRuntime();
            }
            
            if (GUILayout.Button("重置为默认设置"))
            {
                ResetToDefaultSettings();
            }
        }
        
        private void ApplySettingsToRuntime()
        {
            var settings = (ContractValidationSettings)target;
            var module = FrameworkManager.Instance?.GetModule<LightweightContractValidationModule>();
            
            if (module != null)
            {
                module.SetValidationEnabled(settings.enableContractValidation);
                module.LogWarningsOnFailure = settings.logWarningsOnFailure;
                Debug.Log("设置已应用到运行时");
            }
            else
            {
                Debug.LogWarning("找不到运行时模块，设置将在下次运行时生效");
            }
        }
        
        private void ResetToDefaultSettings()
        {
            var settings = (ContractValidationSettings)target;
            settings.enableContractValidation = false;
            settings.logWarningsOnFailure = true;
            settings.throwOnValidationFailure = false;
            settings.disableInReleaseBuilds = true;
            settings.disableInEditor = false;
            
            Debug.Log("已重置为默认设置");
        }
    }
}