using System.IO;
using UnityEditor;
using UnityEngine;

namespace CnoomFramework.Editor
{
    /// <summary>
    /// 模块模板生成器
    /// </summary>
    public class ModuleTemplateGenerator : EditorWindow
    {
        private string _moduleName = "MyModule";
        private string _moduleNamespace = "MyProject.Modules";
        private bool _includeEventHandlers = true;
        private bool _includeCommandHandlers = true;
        private bool _includeQueryHandlers = false;
        private bool _autoRegister = true;
        private int _priority = 0;
        private string _outputPath = "Assets/Scripts/Modules";

        public static void ShowWindow()
        {
            var window = GetWindow<ModuleTemplateGenerator>("Module Template Generator");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("模块模板生成器", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawBasicSettings();
            EditorGUILayout.Space();
            DrawFeatureSettings();
            EditorGUILayout.Space();
            DrawOutputSettings();
            EditorGUILayout.Space();
            DrawGenerateButton();
        }

        private void DrawBasicSettings()
        {
            EditorGUILayout.LabelField("基本设置", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            _moduleName = EditorGUILayout.TextField("模块名称", _moduleName);
            _moduleNamespace = EditorGUILayout.TextField("命名空间", _moduleNamespace);
            _priority = EditorGUILayout.IntField("优先级", _priority);
            _autoRegister = EditorGUILayout.Toggle("自动注册", _autoRegister);
            
            EditorGUILayout.EndVertical();
        }

        private void DrawFeatureSettings()
        {
            EditorGUILayout.LabelField("功能设置", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            _includeEventHandlers = EditorGUILayout.Toggle("包含事件处理器", _includeEventHandlers);
            _includeCommandHandlers = EditorGUILayout.Toggle("包含命令处理器", _includeCommandHandlers);
            _includeQueryHandlers = EditorGUILayout.Toggle("包含查询处理器", _includeQueryHandlers);
            
            EditorGUILayout.EndVertical();
        }

        private void DrawOutputSettings()
        {
            EditorGUILayout.LabelField("输出设置", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            _outputPath = EditorGUILayout.TextField("输出路径", _outputPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                var selectedPath = EditorUtility.OpenFolderPanel("选择输出目录", _outputPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    _outputPath = FileUtil.GetProjectRelativePath(selectedPath);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawGenerateButton()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUI.enabled = !string.IsNullOrEmpty(_moduleName) && !string.IsNullOrEmpty(_outputPath);
            if (GUILayout.Button("生成模块", GUILayout.Width(100), GUILayout.Height(30)))
            {
                GenerateModule();
            }
            GUI.enabled = true;
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void GenerateModule()
        {
            try
            {
                var template = GenerateModuleTemplate();
                var fileName = $"{_moduleName}.cs";
                var fullPath = Path.Combine(_outputPath, fileName);
                
                // 确保目录存在
                Directory.CreateDirectory(_outputPath);
                
                // 写入文件
                File.WriteAllText(fullPath, template);
                
                // 刷新资源数据库
                AssetDatabase.Refresh();
                
                // 选中生成的文件
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(fullPath);
                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
                
                Debug.Log($"模块模板已生成: {fullPath}");
                EditorUtility.DisplayDialog("生成成功", $"模块模板已生成到:\n{fullPath}", "确定");
                
                Close();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"生成模块模板失败: {ex.Message}");
                EditorUtility.DisplayDialog("生成失败", $"生成模块模板时发生错误:\n{ex.Message}", "确定");
            }
        }

        private string GenerateModuleTemplate()
        {
            var template = new System.Text.StringBuilder();
            
            // 添加using语句
            template.AppendLine("using System;");
            template.AppendLine("using UnityEngine;");
            template.AppendLine("using CnoomFramework.Core;");
            
            if (_includeEventHandlers || _includeCommandHandlers || _includeQueryHandlers)
            {
                template.AppendLine("using CnoomFramework.Core.Attributes;");
            }
            
            template.AppendLine();
            
            // 添加命名空间
            if (!string.IsNullOrEmpty(_moduleNamespace))
            {
                template.AppendLine($"namespace {_moduleNamespace}");
                template.AppendLine("{");
            }
            
            // 添加类注释
            template.AppendLine("    /// <summary>");
            template.AppendLine($"    /// {_moduleName} - 自动生成的模块模板");
            template.AppendLine("    /// </summary>");
            
            // 添加自动注册属性
            if (_autoRegister)
            {
                template.AppendLine($"    [AutoRegisterModule(Priority = {_priority})]");
            }
            
            // 添加类定义
            template.AppendLine($"    public class {_moduleName} : BaseModule");
            template.AppendLine("    {");
            
            // 添加模块名称重写
            template.AppendLine($"        public override string Name => \"{_moduleName}\";");
            template.AppendLine();
            
            // 添加优先级重写
            if (_priority != 0)
            {
                template.AppendLine($"        public override int Priority => {_priority};");
                template.AppendLine();
            }
            
            // 添加初始化方法
            template.AppendLine("        protected override void OnInit()");
            template.AppendLine("        {");
            template.AppendLine("            // 模块初始化逻辑");
            template.AppendLine("            Debug.Log($\"[{Name}] Module initialized\");");
            template.AppendLine("        }");
            template.AppendLine();
            
            // 添加启动方法
            template.AppendLine("        protected override void OnStart()");
            template.AppendLine("        {");
            template.AppendLine("            // 模块启动逻辑");
            template.AppendLine("            Debug.Log($\"[{Name}] Module started\");");
            template.AppendLine("        }");
            template.AppendLine();
            
            // 添加关闭方法
            template.AppendLine("        protected override void OnShutdown()");
            template.AppendLine("        {");
            template.AppendLine("            // 模块关闭逻辑");
            template.AppendLine("            Debug.Log($\"[{Name}] Module shutdown\");");
            template.AppendLine("        }");
            
            // 添加事件处理器示例
            if (_includeEventHandlers)
            {
                template.AppendLine();
                template.AppendLine("        // 事件处理器示例");
                template.AppendLine("        [BroadcastHandler]");
                template.AppendLine("        private void OnExampleEvent(ExampleEvent evt)");
                template.AppendLine("        {");
                template.AppendLine("            // 处理广播事件");
                template.AppendLine("            Debug.Log($\"[{Name}] Received event: {evt.Message}\");");
                template.AppendLine("        }");
            }
            
            // 添加命令处理器示例
            if (_includeCommandHandlers)
            {
                template.AppendLine();
                template.AppendLine("        // 命令处理器示例");
                template.AppendLine("        [CommandHandler]");
                template.AppendLine("        private void OnExampleCommand(ExampleCommand cmd)");
                template.AppendLine("        {");
                template.AppendLine("            // 处理命令");
                template.AppendLine("            Debug.Log($\"[{Name}] Executing command: {cmd.Action}\");");
                template.AppendLine("        }");
            }
            
            // 添加查询处理器示例
            if (_includeQueryHandlers)
            {
                template.AppendLine();
                template.AppendLine("        // 查询处理器示例");
                template.AppendLine("        [QueryHandler]");
                template.AppendLine("        private ExampleResponse OnExampleQuery(ExampleQuery query)");
                template.AppendLine("        {");
                template.AppendLine("            // 处理查询并返回结果");
                template.AppendLine("            return new ExampleResponse { Data = \"Example data\" };");
                template.AppendLine("        }");
            }
            
            template.AppendLine("    }");
            
            // 添加示例事件、命令和查询类
            if (_includeEventHandlers || _includeCommandHandlers || _includeQueryHandlers)
            {
                template.AppendLine();
                template.AppendLine("    // 示例事件、命令和查询类");
                
                if (_includeEventHandlers)
                {
                    template.AppendLine("    public class ExampleEvent");
                    template.AppendLine("    {");
                    template.AppendLine("        public string Message { get; set; }");
                    template.AppendLine("    }");
                    template.AppendLine();
                }
                
                if (_includeCommandHandlers)
                {
                    template.AppendLine("    public class ExampleCommand");
                    template.AppendLine("    {");
                    template.AppendLine("        public string Action { get; set; }");
                    template.AppendLine("    }");
                    template.AppendLine();
                }
                
                if (_includeQueryHandlers)
                {
                    template.AppendLine("    public class ExampleQuery");
                    template.AppendLine("    {");
                    template.AppendLine("        public string QueryType { get; set; }");
                    template.AppendLine("    }");
                    template.AppendLine();
                    
                    template.AppendLine("    public class ExampleResponse");
                    template.AppendLine("    {");
                    template.AppendLine("        public string Data { get; set; }");
                    template.AppendLine("    }");
                }
            }
            
            // 关闭命名空间
            if (!string.IsNullOrEmpty(_moduleNamespace))
            {
                template.AppendLine("}");
            }
            
            return template.ToString();
        }
    }
}