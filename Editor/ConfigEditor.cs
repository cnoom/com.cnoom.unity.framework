using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CnoomFramework.Core;
using CnoomFramework.Core.Config;

namespace CnoomFramework.Editor
{
    /// <summary>
    /// 配置编辑器
    /// </summary>
    public class ConfigEditor
    {
        private Vector2 _scrollPosition;
        private string _searchFilter = "";
        private string _newConfigKey = "";
        private string _newConfigValue = "";
        private Dictionary<string, bool> _expandedGroups = new Dictionary<string, bool>();
        private Dictionary<string, string> _editingValues = new Dictionary<string, string>();

        public void OnGUI()
        {
            DrawHeader();
            DrawConfigSources();
            DrawConfigEditor();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("配置管理器", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            var frameworkManager = FrameworkManager.Instance;
            if (frameworkManager == null || !frameworkManager.IsInitialized)
            {
                EditorGUILayout.HelpBox("框架未初始化，无法访问配置管理器。", MessageType.Info);
                return;
            }

            var configManager = frameworkManager.ConfigManager;
            if (configManager == null)
            {
                EditorGUILayout.HelpBox("配置管理器未初始化。", MessageType.Warning);
                return;
            }

            // 配置管理器状态
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("配置管理器状态", EditorStyles.boldLabel);
            
            var allKeys = configManager.GetAllKeys().ToList();
            EditorGUILayout.LabelField($"配置项数量: {allKeys.Count}");
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("保存所有配置", GUILayout.Width(100)))
            {
                configManager.Save();
                Debug.Log("配置已保存");
            }
            
            if (GUILayout.Button("重新加载配置", GUILayout.Width(100)))
            {
                configManager.Load();
                Debug.Log("配置已重新加载");
            }
            
            if (GUILayout.Button("清空所有配置", GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("确认", "确定要清空所有配置吗？此操作不可撤销。", "确定", "取消"))
                {
                    configManager.Clear();
                    Debug.Log("所有配置已清空");
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawConfigSources()
        {
            var frameworkManager = FrameworkManager.Instance;
            if (frameworkManager?.ConfigManager == null) return;

            var configManager = frameworkManager.ConfigManager;
            var sources = configManager.GetAllConfigSources();

            EditorGUILayout.LabelField("配置源", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            foreach (var source in sources)
            {
                EditorGUILayout.BeginHorizontal();
                
                // 优先级指示器
                EditorGUILayout.LabelField($"优先级 {source.Priority}", GUILayout.Width(80));
                
                // 配置源名称
                EditorGUILayout.LabelField(source.Name, EditorStyles.boldLabel);
                
                // 持久化支持
                var persistenceText = source.SupportsPersistence ? "支持持久化" : "仅内存";
                EditorGUILayout.LabelField(persistenceText, GUILayout.Width(80));
                
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
        }

        private void DrawConfigEditor()
        {
            var frameworkManager = FrameworkManager.Instance;
            if (frameworkManager?.ConfigManager == null) return;

            var configManager = frameworkManager.ConfigManager;

            EditorGUILayout.LabelField("配置编辑器", EditorStyles.boldLabel);

            // 搜索过滤器
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("搜索:", GUILayout.Width(50));
            _searchFilter = EditorGUILayout.TextField(_searchFilter);
            EditorGUILayout.EndHorizontal();

            // 添加新配置
            DrawAddNewConfig(configManager);

            EditorGUILayout.Space();

            // 配置列表
            DrawConfigList(configManager);
        }

        private void DrawAddNewConfig(IConfigManager configManager)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("添加新配置", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("键:", GUILayout.Width(30));
            _newConfigKey = EditorGUILayout.TextField(_newConfigKey);
            EditorGUILayout.LabelField("值:", GUILayout.Width(30));
            _newConfigValue = EditorGUILayout.TextField(_newConfigValue);
            
            GUI.enabled = !string.IsNullOrEmpty(_newConfigKey);
            if (GUILayout.Button("添加", GUILayout.Width(60)))
            {
                configManager.SetValue(_newConfigKey, _newConfigValue, true);
                _newConfigKey = "";
                _newConfigValue = "";
                Debug.Log($"已添加配置: {_newConfigKey} = {_newConfigValue}");
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawConfigList(IConfigManager configManager)
        {
            var allKeys = configManager.GetAllKeys().ToList();
            var filteredKeys = FilterKeys(allKeys);
            var groupedKeys = GroupKeys(filteredKeys);

            if (groupedKeys.Count == 0)
            {
                EditorGUILayout.HelpBox("没有找到匹配的配置项。", MessageType.Info);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var group in groupedKeys.OrderBy(g => g.Key))
            {
                DrawConfigGroup(group.Key, group.Value, configManager);
            }

            EditorGUILayout.EndScrollView();
        }

        private List<string> FilterKeys(List<string> keys)
        {
            if (string.IsNullOrEmpty(_searchFilter))
                return keys;

            return keys.Where(key => key.ToLower().Contains(_searchFilter.ToLower())).ToList();
        }

        private Dictionary<string, List<string>> GroupKeys(List<string> keys)
        {
            var groups = new Dictionary<string, List<string>>();

            foreach (var key in keys)
            {
                var parts = key.Split('.');
                var groupName = parts.Length > 1 ? parts[0] : "其他";

                if (!groups.ContainsKey(groupName))
                    groups[groupName] = new List<string>();

                groups[groupName].Add(key);
            }

            return groups;
        }

        private void DrawConfigGroup(string groupName, List<string> keys, IConfigManager configManager)
        {
            var isExpanded = _expandedGroups.GetValueOrDefault(groupName, true);
            
            EditorGUILayout.BeginVertical("box");
            
            // 组头部
            EditorGUILayout.BeginHorizontal();
            var newExpanded = EditorGUILayout.Foldout(isExpanded, $"{groupName} ({keys.Count})", true);
            if (newExpanded != isExpanded)
            {
                _expandedGroups[groupName] = newExpanded;
            }
            EditorGUILayout.EndHorizontal();

            // 组内容
            if (_expandedGroups.GetValueOrDefault(groupName, true))
            {
                EditorGUI.indentLevel++;
                
                foreach (var key in keys.OrderBy(k => k))
                {
                    DrawConfigItem(key, configManager);
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawConfigItem(string key, IConfigManager configManager)
        {
            EditorGUILayout.BeginHorizontal();

            // 配置键
            EditorGUILayout.LabelField(key, GUILayout.Width(200));

            // 配置值编辑
            var currentValue = configManager.GetValue<string>(key, "");
            var editingKey = $"editing_{key}";
            
            if (!_editingValues.ContainsKey(editingKey))
            {
                _editingValues[editingKey] = currentValue;
            }

            var newValue = EditorGUILayout.TextField(_editingValues[editingKey]);
            if (newValue != _editingValues[editingKey])
            {
                _editingValues[editingKey] = newValue;
            }

            // 操作按钮
            if (GUILayout.Button("保存", GUILayout.Width(50)))
            {
                configManager.SetValue(key, _editingValues[editingKey], true);
                Debug.Log($"已更新配置: {key} = {_editingValues[editingKey]}");
            }

            if (GUILayout.Button("重置", GUILayout.Width(50)))
            {
                _editingValues[editingKey] = currentValue;
            }

            if (GUILayout.Button("删除", GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("确认删除", $"确定要删除配置项 '{key}' 吗？", "删除", "取消"))
                {
                    configManager.RemoveValue(key);
                    _editingValues.Remove(editingKey);
                    Debug.Log($"已删除配置: {key}");
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}