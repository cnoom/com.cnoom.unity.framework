using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CnoomFramework.Core;
using CnoomFramework.Core.ErrorHandling;
using CnoomFramework.Core.Exceptions;

namespace CnoomFramework.Editor
{
    /// <summary>
    /// 错误日志查看器
    /// </summary>
    public class ErrorLogViewer
    {
        private Vector2 _scrollPosition;
        private string _searchFilter = "";
        private ErrorSeverity _severityFilter = (ErrorSeverity)(-1); // -1 表示显示所有级别
        private bool _showStackTrace = false;
        private Dictionary<int, bool> _expandedErrors = new Dictionary<int, bool>();

        public void OnGUI()
        {
            DrawHeader();
            DrawFilters();
            DrawErrorList();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("错误日志查看器", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            var frameworkManager = FrameworkManager.Instance;
            if (frameworkManager == null || !frameworkManager.IsInitialized)
            {
                EditorGUILayout.HelpBox("框架未初始化，无法访问错误恢复管理器。", MessageType.Info);
                return;
            }

            var errorManager = frameworkManager.ErrorRecoveryManager;
            if (errorManager == null)
            {
                EditorGUILayout.HelpBox("错误恢复管理器未初始化。", MessageType.Warning);
                return;
            }

            // 错误统计
            var errorStats = errorManager.GetErrorStatistics();
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("错误统计", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"总错误数: {errorStats.TotalErrors}", GUILayout.Width(100));
            
            if (errorStats.CriticalSeverityCount > 0)
            {
                var originalColor = GUI.color;
                GUI.color = Color.red;
                EditorGUILayout.LabelField($"严重: {errorStats.CriticalSeverityCount}", GUILayout.Width(80));
                GUI.color = originalColor;
            }
            
            if (errorStats.HighSeverityCount > 0)
            {
                var originalColor = GUI.color;
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField($"高级: {errorStats.HighSeverityCount}", GUILayout.Width(80));
                GUI.color = originalColor;
            }
            
            EditorGUILayout.LabelField($"中级: {errorStats.MediumSeverityCount}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"低级: {errorStats.LowSeverityCount}", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            
            // 操作按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清空错误历史", GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("确认", "确定要清空所有错误历史记录吗？", "确定", "取消"))
                {
                    errorManager.ClearErrorHistory();
                    _expandedErrors.Clear();
                    Debug.Log("错误历史已清空");
                }
            }
            
            if (GUILayout.Button("导出错误报告", GUILayout.Width(100)))
            {
                ExportErrorReport(errorManager);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawFilters()
        {
            EditorGUILayout.BeginHorizontal();
            
            // 搜索过滤器
            EditorGUILayout.LabelField("搜索:", GUILayout.Width(50));
            _searchFilter = EditorGUILayout.TextField(_searchFilter);
            
            // 严重级别过滤器
            EditorGUILayout.LabelField("级别:", GUILayout.Width(50));
            var severityNames = new[] { "全部" }.Concat(Enum.GetNames(typeof(ErrorSeverity))).ToArray();
            var severityValues = new[] { -1 }.Concat(Enum.GetValues(typeof(ErrorSeverity)).Cast<int>()).ToArray();
            var currentIndex = Array.IndexOf(severityValues, (int)_severityFilter);
            var newIndex = EditorGUILayout.Popup(currentIndex, severityNames, GUILayout.Width(100));
            _severityFilter = (ErrorSeverity)severityValues[newIndex];
            
            // 显示堆栈跟踪
            _showStackTrace = EditorGUILayout.Toggle("显示堆栈", _showStackTrace);
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void DrawErrorList()
        {
            var frameworkManager = FrameworkManager.Instance;
            if (frameworkManager?.ErrorRecoveryManager == null) return;

            var errorHistory = frameworkManager.ErrorRecoveryManager.ErrorHistory;
            var filteredErrors = FilterErrors(errorHistory);

            if (filteredErrors.Count == 0)
            {
                EditorGUILayout.HelpBox("没有找到匹配的错误记录。", MessageType.Info);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            for (int i = 0; i < filteredErrors.Count; i++)
            {
                DrawErrorItem(filteredErrors[i], i);
            }

            EditorGUILayout.EndScrollView();
        }

        private List<ErrorRecord> FilterErrors(IReadOnlyList<ErrorRecord> errors)
        {
            var filtered = errors.AsEnumerable();

            // 按异常消息过滤
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                filtered = filtered.Where(e => 
                    e.Exception.Message.ToLower().Contains(_searchFilter.ToLower()) ||
                    e.Exception.GetType().Name.ToLower().Contains(_searchFilter.ToLower()));
            }

            // 按严重级别过滤
            if (_severityFilter != (ErrorSeverity)(-1))
            {
                filtered = filtered.Where(e => e.Severity == _severityFilter);
            }

            return filtered.OrderByDescending(e => e.Timestamp).ToList();
        }

        private void DrawErrorItem(ErrorRecord errorRecord, int index)
        {
            var isExpanded = _expandedErrors.GetValueOrDefault(index, false);
            
            EditorGUILayout.BeginVertical("box");
            
            // 错误头部信息
            EditorGUILayout.BeginHorizontal();
            
            // 展开/折叠按钮
            var newExpanded = EditorGUILayout.Foldout(isExpanded, "", true);
            if (newExpanded != isExpanded)
            {
                _expandedErrors[index] = newExpanded;
            }
            
            // 严重级别指示器
            var severityColor = GetSeverityColor(errorRecord.Severity);
            var originalColor = GUI.color;
            GUI.color = severityColor;
            GUILayout.Label("●", GUILayout.Width(15));
            GUI.color = originalColor;
            
            // 时间戳
            EditorGUILayout.LabelField(errorRecord.Timestamp.ToString("HH:mm:ss"), GUILayout.Width(80));
            
            // 异常类型
            EditorGUILayout.LabelField(errorRecord.Exception.GetType().Name, GUILayout.Width(150));
            
            // 异常消息（截断显示）
            var message = errorRecord.Exception.Message;
            if (message.Length > 50)
                message = message.Substring(0, 50) + "...";
            EditorGUILayout.LabelField(message);
            
            // 严重级别
            EditorGUILayout.LabelField(errorRecord.Severity.ToString(), GUILayout.Width(80));
            
            EditorGUILayout.EndHorizontal();

            // 详细信息
            if (_expandedErrors.GetValueOrDefault(index, false))
            {
                DrawErrorDetails(errorRecord);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawErrorDetails(ErrorRecord errorRecord)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.BeginVertical("box");
            
            // 基本信息
            EditorGUILayout.LabelField("详细信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"异常类型: {errorRecord.Exception.GetType().FullName}");
            EditorGUILayout.LabelField($"时间戳: {errorRecord.Timestamp:yyyy-MM-dd HH:mm:ss.fff}");
            EditorGUILayout.LabelField($"严重级别: {errorRecord.Severity}");
            
            // 上下文信息
            if (errorRecord.Context != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("上下文:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"类型: {errorRecord.Context.GetType().Name}");
                EditorGUILayout.LabelField($"信息: {errorRecord.Context.ToString()}");
            }
            
            // 异常消息
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("异常消息:", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(errorRecord.Exception.Message, EditorStyles.textArea, GUILayout.Height(40));
            
            // 堆栈跟踪
            if (_showStackTrace && !string.IsNullOrEmpty(errorRecord.Exception.StackTrace))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("堆栈跟踪:", EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(errorRecord.Exception.StackTrace, EditorStyles.textArea, GUILayout.Height(100));
            }
            
            // 内部异常
            if (errorRecord.Exception.InnerException != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("内部异常:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"类型: {errorRecord.Exception.InnerException.GetType().Name}");
                EditorGUILayout.SelectableLabel(errorRecord.Exception.InnerException.Message, EditorStyles.textArea, GUILayout.Height(30));
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUI.indentLevel--;
        }

        private Color GetSeverityColor(ErrorSeverity severity)
        {
            switch (severity)
            {
                case ErrorSeverity.Low:
                    return Color.green;
                case ErrorSeverity.Medium:
                    return Color.yellow;
                case ErrorSeverity.High:
                    return new Color(1f, 0.5f, 0f); // 橙色
                case ErrorSeverity.Critical:
                    return Color.red;
                default:
                    return Color.white;
            }
        }

        private void ExportErrorReport(ErrorRecoveryManager errorManager)
        {
            var path = EditorUtility.SaveFilePanel("导出错误报告", "", "ErrorReport", "txt");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                var errorHistory = errorManager.ErrorHistory;
                var errorStats = errorManager.GetErrorStatistics();
                
                var report = new System.Text.StringBuilder();
                report.AppendLine("Cnoom Framework 错误报告");
                report.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                report.AppendLine();
                
                // 统计信息
                report.AppendLine("=== 错误统计 ===");
                report.AppendLine($"总错误数: {errorStats.TotalErrors}");
                report.AppendLine($"严重错误: {errorStats.CriticalSeverityCount}");
                report.AppendLine($"高级错误: {errorStats.HighSeverityCount}");
                report.AppendLine($"中级错误: {errorStats.MediumSeverityCount}");
                report.AppendLine($"低级错误: {errorStats.LowSeverityCount}");
                report.AppendLine();
                
                // 详细错误列表
                report.AppendLine("=== 错误详情 ===");
                foreach (var error in errorHistory.OrderByDescending(e => e.Timestamp))
                {
                    report.AppendLine($"[{error.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {error.Severity} - {error.Exception.GetType().Name}");
                    report.AppendLine($"消息: {error.Exception.Message}");
                    if (error.Context != null)
                        report.AppendLine($"上下文: {error.Context}");
                    if (!string.IsNullOrEmpty(error.Exception.StackTrace))
                        report.AppendLine($"堆栈: {error.Exception.StackTrace}");
                    report.AppendLine();
                }
                
                System.IO.File.WriteAllText(path, report.ToString());
                Debug.Log($"错误报告已导出到: {path}");
                
                EditorUtility.DisplayDialog("导出成功", $"错误报告已导出到:\n{path}", "确定");
            }
            catch (Exception ex)
            {
                Debug.LogError($"导出错误报告失败: {ex.Message}");
                EditorUtility.DisplayDialog("导出失败", $"导出错误报告时发生错误:\n{ex.Message}", "确定");
            }
        }
    }
}