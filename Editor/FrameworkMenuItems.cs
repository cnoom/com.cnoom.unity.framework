using UnityEditor;
using UnityEngine;
using CnoomFramework.Core;

namespace CnoomFramework.Editor
{
    /// <summary>
    /// Cnoom Framework 菜单项
    /// </summary>
    public static class FrameworkMenuItems
    {
        public const string MENU_ROOT = "Cnoom Framework/";

        [MenuItem(MENU_ROOT + "主调试器窗口", false, 1)]
        public static void OpenFrameworkDebugger()
        {
            FrameworkDebuggerWindow.ShowWindow();
        }

        [MenuItem(MENU_ROOT + "便捷操作/初始化运行时框架", false, 100)]
        public static void InitializeFramework()
        {
            var frameworkManager = FrameworkManager.Instance;
            if (!frameworkManager.IsInitialized)
            {
                frameworkManager.Initialize();
                Debug.Log("Framework initialized successfully.");
            }
            else
            {
                Debug.LogWarning("Framework is already initialized.");
            }
        }

        [MenuItem(MENU_ROOT + "便捷操作/初始化运行时框架", true)]
        public static bool ValidateInitializeFramework()
        {
            var frameworkManager = FrameworkManager.Instance;
            return frameworkManager != null && !frameworkManager.IsInitialized;
        }

        [MenuItem(MENU_ROOT + "便捷操作/关闭框架", false, 101)]
        public static void ShutdownFramework()
        {
            var frameworkManager = FrameworkManager.Instance;
            if (frameworkManager.IsInitialized)
            {
                frameworkManager.Shutdown();
                Debug.Log("Framework shutdown successfully.");
            }
            else
            {
                Debug.LogWarning("Framework is not initialized.");
            }
        }

        [MenuItem(MENU_ROOT + "便捷操作/关闭框架", true)]
        public static bool ValidateShutdownFramework()
        {
            var frameworkManager = FrameworkManager.Instance;
            return frameworkManager != null && frameworkManager.IsInitialized;
        }

        [MenuItem(MENU_ROOT + "便捷操作/重置框架", false, 102)]
        public static void RestartFramework()
        {
            var frameworkManager = FrameworkManager.Instance;
            if (frameworkManager.IsInitialized)
            {
                frameworkManager.Shutdown();
            }

            frameworkManager.Initialize();
            Debug.Log("Framework restarted successfully.");
        }

        [MenuItem(MENU_ROOT + "便捷操作/重置框架", true)]
        public static bool ValidateRestartFramework()
        {
            return FrameworkManager.Instance != null;
        }

        [MenuItem(MENU_ROOT + "工具/移除所有配置", false, 200)]
        public static void ClearAllConfigs()
        {
            if (EditorUtility.DisplayDialog("移除所有配置",
                    "Are you sure you want to clear all framework configurations? This action cannot be undone.",
                    "Clear", "Cancel"))
            {
                var frameworkManager = FrameworkManager.Instance;
                if (frameworkManager?.ConfigManager != null)
                {
                    frameworkManager.ConfigManager.Clear();
                    Debug.Log("All configurations cleared.");
                }
                else
                {
                    Debug.LogWarning("ConfigManager is not available.");
                }
            }
        }

        [MenuItem(MENU_ROOT + "工具/移除所有配置", true)]
        public static bool ValidateClearAllConfigs()
        {
            var frameworkManager = FrameworkManager.Instance;
            return frameworkManager?.ConfigManager != null;
        }

        [MenuItem(MENU_ROOT + "工具/清除错误历史记录", false, 201)]
        public static void ClearErrorHistory()
        {
            if (EditorUtility.DisplayDialog("清除错误历史记录",
                    "您确定要清除所有错误历史记录吗？此操作无法撤销。",
                    "清除", "取消"))
            {
                var frameworkManager = FrameworkManager.Instance;
                if (frameworkManager?.ErrorRecoveryManager != null)
                {
                    frameworkManager.ErrorRecoveryManager.ClearErrorHistory();
                    Debug.Log("清除错误历史记录。");
                }
                else
                {
                    Debug.LogWarning("ErrorRecoveryManager 不可用。");
                }
            }
        }

        [MenuItem(MENU_ROOT + "工具/清除错误历史记录", true)]
        public static bool ValidateClearErrorHistory()
        {
            var frameworkManager = FrameworkManager.Instance;
            return frameworkManager?.ErrorRecoveryManager != null;
        }

        [MenuItem(MENU_ROOT + "工具/生成模块模板", false, 300)]
        public static void GenerateModuleTemplate()
        {
            ModuleTemplateGenerator.ShowWindow();
        }

        // [MenuItem(MENU_ROOT + "Help/Framework Documentation", false, 400)]
        // public static void OpenDocumentation()
        // {
        //     Application.OpenURL("https://github.com/cnoom/unity-framework");
        // }

        // [MenuItem(MENU_ROOT + "Help/Report Issue", false, 401)]
        // public static void ReportIssue()
        // {
        //     Application.OpenURL("https://github.com/cnoom/unity-framework/issues");
        // }

        [MenuItem(MENU_ROOT + "Help/About Framework", false, 402)]
        public static void ShowAbout()
        {
            EditorUtility.DisplayDialog("About Cnoom Framework",
                "Cnoom Unity Framework v1.0.3\n\n" +
                "A lightweight Unity framework focused on event systems and modular architecture.\n\n" +
                "© 2024 Cnoom\n" +
                "Licensed under MIT License",
                "OK");
        }
    }
}