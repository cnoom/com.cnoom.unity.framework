using CnoomFramework.Editor.Editor;
using UnityEditor;
using UnityEngine;

namespace CnoomFramework.Editor.ExamplesEditor
{
    public class ExamplesWindow : EditorWindow
    {
        public const string MenuPath = FrameworkEditorConfig.MenuPath + "/示例/打开示例窗口";

        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            GetWindow<ExamplesWindow>("Cnoom 示例");
        }

        private void OnGUI()
        {
            GUILayout.Label("CnoomFramework 示例生成器", EditorStyles.boldLabel);
            if (GUILayout.Button("生成 ConfigTestModule 脚本"))
                CreateExampleScript("ConfigTestModule");
            if (GUILayout.Button("生成 ContractTestModule 脚本"))
                CreateExampleScript("ContractTestModule");
            if (GUILayout.Button("生成 DebuggerTestModule 脚本"))
                CreateExampleScript("DebuggerTestModule");
            if (GUILayout.Button("生成 ErrorHandlingTestModule 脚本"))
                CreateExampleScript("ErrorHandlingTestModule");
            if (GUILayout.Button("生成 ExampleModule 脚本"))
                CreateExampleScript("ExampleModule");
            if (GUILayout.Button("生成 MockTestModule 脚本"))
                CreateExampleScript("MockTestModule");
            if (GUILayout.Button("生成 MockTestScript 脚本"))
                CreateExampleScript("MockTestScript");
            if (GUILayout.Button("生成 PerformanceTestModule 脚本"))
                CreateExampleScript("PerformanceTestModule");
            if (GUILayout.Button("生成 TestFrameworkScript 脚本"))
                CreateExampleScript("TestFrameworkScript");
        }

        private void CreateExampleScript(string scriptName)
        {
            string editorPath = "Assets/Editor/Examples/";
            if (!AssetDatabase.IsValidFolder("Assets/Editor"))
                AssetDatabase.CreateFolder("Assets", "Editor");
            if (!AssetDatabase.IsValidFolder(editorPath.TrimEnd('/')))
                AssetDatabase.CreateFolder("Assets/Editor", "Examples");
            string templatePath = $"Packages/com.cnoom.unity.framework/Editor/ExamplesEditor/{scriptName}.cs";
            string targetPath = editorPath + scriptName + ".cs";
            if (System.IO.File.Exists(templatePath))
            {
                System.IO.File.Copy(templatePath, targetPath, true);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("生成成功", $"已生成 {scriptName}.cs 到 {editorPath}", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("生成失败", $"未找到模板文件: {templatePath}", "OK");
            }
        }
    }
}