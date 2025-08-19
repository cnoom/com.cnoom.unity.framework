using System;
using System.Collections.Generic;
using System.Linq;
using CnoomFramework.Core;
using CnoomFramework.Core.Performance;
using CnoomFramework.Editor.Editor;
using UnityEditor;
using UnityEngine;

namespace CnoomFramework.Editor
{
    /// <summary>
    /// 增强的性能监控窗口
    /// </summary>
    public class EnhancedPerformanceMonitorWindow : EditorWindow
    {
        private const string MenuPath = FrameworkEditorConfig.MenuPath + "/" +"性能监控器2";
        
        private Vector2 _scrollPosition;
        private bool _isFrameworkInitialized = false;
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _boldLabelStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _searchFieldStyle;
        private GUIStyle _searchCancelButtonStyle;
        private string _searchText = "";
        private bool _autoRefresh = true;
        private float _refreshInterval = 2.0f;
        private float _lastRefreshTime;
        private bool _showPerformanceGraphs = true;
        private bool _showPerformanceWarnings = true;
        private bool _groupByModule = false;
        private bool _showHistoricalData = false;
        private float _warningThreshold = 100f; // 100ms警告阈值
        private float _criticalThreshold = 500f; // 500ms严重阈值

        // 性能数据
        private Dictionary<string, PerformanceStats> _performanceStats = new Dictionary<string, PerformanceStats>();
        private Dictionary<string, List<float>> _historicalData = new Dictionary<string, List<float>>();
        private Dictionary<string, bool> _operationFoldouts = new Dictionary<string, bool>();
        private string _selectedOperation = "";
        private int _maxHistoricalPoints = 100;

        // 性能警告
        private List<PerformanceWarning> _performanceWarnings = new List<PerformanceWarning>();

        [Serializable]
        private class PerformanceWarning
        {
            public string OperationName;
            public float Duration;
            public string WarningType; // "Warning" or "Critical"
            public DateTime Timestamp;
            public string Message;
        }

        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            var window = GetWindow<EnhancedPerformanceMonitorWindow>("Enhanced Performance Monitor");
            window.minSize = new Vector2(900, 600);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += CheckFrameworkStatus;
            _lastRefreshTime = Time.realtimeSinceStartup;
        }

        private void OnDisable()
        {
            EditorApplication.update -= CheckFrameworkStatus;
        }

        private void CheckFrameworkStatus()
        {
            if (Application.isPlaying)
            {
                var frameworkManager = FrameworkManager.Instance;
                if (frameworkManager != null && frameworkManager.IsInitialized)
                {
                    if (!_isFrameworkInitialized)
                    {
                        _isFrameworkInitialized = true;
                        RefreshPerformanceData();
                    }

                    // 自动刷新
                    if (_autoRefresh && Time.realtimeSinceStartup - _lastRefreshTime > _refreshInterval)
                    {
                        RefreshPerformanceData();
                        _lastRefreshTime = Time.realtimeSinceStartup;
                        Repaint();
                    }
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

        private void RefreshPerformanceData()
        {
            if (!_isFrameworkInitialized) return;

            try
            {
                var performanceModule = FrameworkManager.Instance.GetModule<PerformanceMonitorModule>();
                if (performanceModule != null)
                {
                    var stats = performanceModule.PerformanceMonitor.GetAllStats();
                    _performanceStats = new Dictionary<string, PerformanceStats>(stats);

                    // 更新历史数据
                    if (_showHistoricalData)
                    {
                        UpdateHistoricalData();
                    }

                    // 检查性能警告
                    if (_showPerformanceWarnings)
                    {
                        CheckPerformanceWarnings();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"刷新性能数据时出错: {ex.Message}");
            }
        }

        private void UpdateHistoricalData()
        {
            foreach (var kvp in _performanceStats)
            {
                var operationName = kvp.Key;
                var stats = kvp.Value;

                if (!_historicalData.ContainsKey(operationName))
                {
                    _historicalData[operationName] = new List<float>();
                }

                // 添加最新的平均时间
                _historicalData[operationName].Add(stats.AverageTime);

                // 限制历史数据点数量
                if (_historicalData[operationName].Count > _maxHistoricalPoints)
                {
                    _historicalData[operationName].RemoveAt(0);
                }
            }
        }

        private void CheckPerformanceWarnings()
        {
            _performanceWarnings.Clear();

            foreach (var kvp in _performanceStats)
            {
                var operationName = kvp.Key;
                var stats = kvp.Value;

                // 检查平均时间
                if (stats.AverageTime > _criticalThreshold)
                {
                    _performanceWarnings.Add(new PerformanceWarning
                    {
                        OperationName = operationName,
                        Duration = stats.AverageTime,
                        WarningType = "Critical",
                        Timestamp = DateTime.Now,
                        Message = $"操作 {operationName} 的平均执行时间 ({stats.AverageTime:F1}ms) 超过严重阈值 ({_criticalThreshold}ms)"
                    });
                }
                else if (stats.AverageTime > _warningThreshold)
                {
                    _performanceWarnings.Add(new PerformanceWarning
                    {
                        OperationName = operationName,
                        Duration = stats.AverageTime,
                        WarningType = "Warning",
                        Timestamp = DateTime.Now,
                        Message = $"操作 {operationName} 的平均执行时间 ({stats.AverageTime:F1}ms) 超过警告阈值 ({_warningThreshold}ms)"
                    });
                }

                // 检查最大时间
                if (stats.MaxTime > _criticalThreshold * 2)
                {
                    _performanceWarnings.Add(new PerformanceWarning
                    {
                        OperationName = operationName,
                        Duration = stats.MaxTime,
                        WarningType = "Critical",
                        Timestamp = DateTime.Now,
                        Message = $"操作 {operationName} 的最大执行时间 ({stats.MaxTime:F1}ms) 异常高"
                    });
                }
            }
        }

        private void OnGUI()
        {
            InitializeStyles();

            DrawToolbar();

            EditorGUILayout.Space();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (!_isFrameworkInitialized)
            {
                EditorGUILayout.HelpBox("框架未初始化，请在运行时查看性能数据。", MessageType.Info);
            }
            else
            {
                DrawPerformanceVisualization();
            }

            EditorGUILayout.EndScrollView();
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

            if (_boldLabelStyle == null)
            {
                _boldLabelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12
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

            if (_searchFieldStyle == null)
            {
                _searchFieldStyle = new GUIStyle(EditorStyles.toolbarSearchField)
                {
                    margin = new RectOffset(5, 5, 5, 5),
                    fixedWidth = 200
                };
            }

            if (_searchCancelButtonStyle == null)
            {
                _searchCancelButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
                {
                    fixedWidth = 20,
                    margin = new RectOffset(0, 5, 5, 5)
                };
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton))
            {
                RefreshPerformanceData();
            }

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "自动刷新", EditorStyles.toolbarButton);

            if (_autoRefresh)
            {
                GUILayout.Label("刷新间隔:", GUILayout.Width(60));
                _refreshInterval = EditorGUILayout.Slider(_refreshInterval, 0.5f, 10.0f, GUILayout.Width(100));
            }

            _showPerformanceGraphs = GUILayout.Toggle(_showPerformanceGraphs, "显示图表", EditorStyles.toolbarButton);
            _showPerformanceWarnings = GUILayout.Toggle(_showPerformanceWarnings, "显示警告", EditorStyles.toolbarButton);
            _groupByModule = GUILayout.Toggle(_groupByModule, "按模块分组", EditorStyles.toolbarButton);
            _showHistoricalData = GUILayout.Toggle(_showHistoricalData, "历史数据", EditorStyles.toolbarButton);

            GUILayout.FlexibleSpace();

            _searchText = EditorGUILayout.TextField(_searchText, _searchFieldStyle);

            if (GUILayout.Button("×", _searchCancelButtonStyle) && !string.IsNullOrEmpty(_searchText))
            {
                _searchText = "";
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPerformanceVisualization()
        {
            EditorGUILayout.BeginVertical();

            GUILayout.Label("增强性能监控", _headerStyle);

            if (_performanceStats.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无性能数据。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // 显示性能警告
            if (_showPerformanceWarnings && _performanceWarnings.Count > 0)
            {
                DrawPerformanceWarnings();
            }

            // 显示总体统计
            DrawOverallStatistics();

            // 显示性能图表
            if (_showPerformanceGraphs)
            {
                DrawPerformanceGraphs();
            }

            // 显示详细性能数据
            DrawDetailedPerformanceData();

            EditorGUILayout.EndVertical();
        }

        private void DrawPerformanceWarnings()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("性能警告", _subHeaderStyle);

            var criticalWarnings = _performanceWarnings.Where(w => w.WarningType == "Critical").ToList();
            var warningWarnings = _performanceWarnings.Where(w => w.WarningType == "Warning").ToList();

            if (criticalWarnings.Count > 0)
            {
                GUI.color = Color.red;
                GUILayout.Label($"严重警告 ({criticalWarnings.Count}):", _boldLabelStyle);
                GUI.color = Color.white;

                foreach (var warning in criticalWarnings.Take(3))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(warning.Timestamp.ToString("HH:mm:ss"), GUILayout.Width(80));
                    EditorGUILayout.LabelField(warning.Message);
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (warningWarnings.Count > 0)
            {
                GUI.color = Color.yellow;
                GUILayout.Label($"警告 ({warningWarnings.Count}):", _boldLabelStyle);
                GUI.color = Color.white;

                foreach (var warning in warningWarnings.Take(3))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(warning.Timestamp.ToString("HH:mm:ss"), GUILayout.Width(80));
                    EditorGUILayout.LabelField(warning.Message);
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawOverallStatistics()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("总体统计", _subHeaderStyle);

            int totalOperations = _performanceStats.Count;
            int totalCalls = _performanceStats.Values.Sum(s => s.CallCount);
            float totalTime = _performanceStats.Values.Sum(s => s.TotalTime);
            float maxTime = _performanceStats.Values.Max(s => s.MaxTime);
            float avgTime = totalCalls > 0 ? totalTime / totalCalls : 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("操作总数:", totalOperations.ToString());
            EditorGUILayout.LabelField("调用总次数:", totalCalls.ToString());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("总执行时间:", $"{totalTime:F2} ms");
            EditorGUILayout.LabelField("平均执行时间:", $"{avgTime:F2} ms");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("最大执行时间:", $"{maxTime:F2} ms");
            EditorGUILayout.LabelField("警告阈值:", $"{_warningThreshold} ms");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawPerformanceGraphs()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("性能图表", _subHeaderStyle);

            // 简单的性能图表
            var topOperations = _performanceStats
                .OrderByDescending(kv => kv.Value.TotalTime)
                .Take(5)
                .ToList();

            float maxTotalTime = topOperations.Count > 0 ? topOperations[0].Value.TotalTime : 1f;

            foreach (var kvp in topOperations)
            {
                var operationName = kvp.Key;
                var stats = kvp.Value;

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(operationName, GUILayout.Width(200));
                EditorGUILayout.LabelField($"{stats.TotalTime:F1}ms", GUILayout.Width(80));

                // 绘制简单的条形图
                float barWidth = (stats.TotalTime / maxTotalTime) * 200f;
                EditorGUILayout.BeginVertical(GUILayout.Width(barWidth));
                GUI.color = stats.AverageTime > _warningThreshold ? Color.red : Color.green;
                GUILayout.Box("", GUILayout.Width(barWidth), GUILayout.Height(20));
                GUI.color = Color.white;
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawDetailedPerformanceData()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("详细性能数据", _subHeaderStyle);

            // 过滤操作
            var filteredOperations = _performanceStats;
            if (!string.IsNullOrEmpty(_searchText))
            {
                filteredOperations = _performanceStats
                    .Where(kv => kv.Key.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
            }

            // 按模块分组
            if (_groupByModule)
            {
                DrawOperationsGroupedByModule(filteredOperations);
            }
            else
            {
                DrawOperationsList(filteredOperations);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawOperationsList(Dictionary<string, PerformanceStats> operations)
        {
            // 表头
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("操作名称", EditorStyles.toolbarButton, GUILayout.Width(250));
            GUILayout.Label("调用次数", EditorStyles.toolbarButton, GUILayout.Width(80));
            GUILayout.Label("总时间 (ms)", EditorStyles.toolbarButton, GUILayout.Width(100));
            GUILayout.Label("平均时间 (ms)", EditorStyles.toolbarButton, GUILayout.Width(100));
            GUILayout.Label("最大时间 (ms)", EditorStyles.toolbarButton, GUILayout.Width(100));
            GUILayout.Label("最小时间 (ms)", EditorStyles.toolbarButton, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            // 按总时间排序
            var sortedOperations = operations.OrderByDescending(kv => kv.Value.TotalTime).ToList();

            foreach (var kvp in sortedOperations)
            {
                var operationName = kvp.Key;
                var stats = kvp.Value;

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(operationName, GUILayout.Width(250));
                EditorGUILayout.LabelField(stats.CallCount.ToString(), GUILayout.Width(80));
                EditorGUILayout.LabelField($"{stats.TotalTime:F2}", GUILayout.Width(100));

                // 根据平均时间设置颜色
                if (stats.AverageTime > _criticalThreshold)
                {
                    GUI.color = Color.red;
                }
                else if (stats.AverageTime > _warningThreshold)
                {
                    GUI.color = Color.yellow;
                }
                else
                {
                    GUI.color = Color.green;
                }

                EditorGUILayout.LabelField($"{stats.AverageTime:F2}", GUILayout.Width(100));
                EditorGUILayout.LabelField($"{stats.MaxTime:F2}", GUILayout.Width(100));
                EditorGUILayout.LabelField($"{stats.MinTime:F2}", GUILayout.Width(100));

                GUI.color = Color.white;

                EditorGUILayout.EndHorizontal();

                // 显示历史数据图表
                if (_showHistoricalData && _historicalData.ContainsKey(operationName))
                {
                    DrawHistoricalDataChart(operationName);
                }
            }
        }

        private void DrawOperationsGroupedByModule(Dictionary<string, PerformanceStats> operations)
        {
            // 按模块分组
            var moduleGroups = operations
                .GroupBy(kv => GetModuleNameFromOperation(kv.Key))
                .OrderBy(g => g.Key);

            foreach (var moduleGroup in moduleGroups)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                GUILayout.Label($"模块: {moduleGroup.Key}", _subHeaderStyle);

                var moduleOperations = moduleGroup
                    .OrderByDescending(kv => kv.Value.TotalTime)
                    .ToList();

                foreach (var kvp in moduleOperations)
                {
                    var operationName = kvp.Key;
                    var stats = kvp.Value;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(operationName, GUILayout.Width(200));
                    EditorGUILayout.LabelField($"{stats.AverageTime:F2}ms", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"({stats.CallCount} 次)", GUILayout.Width(60));
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        private void DrawHistoricalDataChart(string operationName)
        {
            if (!_historicalData.ContainsKey(operationName)) return;

            var data = _historicalData[operationName];
            if (data.Count < 2) return;

            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label($"历史数据: {operationName}", _boldLabelStyle);

            // 简单的折线图
            float maxValue = data.Max();
            float minValue = data.Min();
            float range = maxValue - minValue;
            if (range == 0) range = 1;

            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < data.Count; i++)
            {
                float normalizedValue = (data[i] - minValue) / range;
                float barHeight = normalizedValue * 50f;

                EditorGUILayout.BeginVertical(GUILayout.Width(4));
                GUI.color = data[i] > _warningThreshold ? Color.red : Color.green;
                GUILayout.Box("", GUILayout.Width(4), GUILayout.Height(barHeight));
                GUI.color = Color.white;
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"范围: {minValue:F1}ms - {maxValue:F1}ms");
            EditorGUILayout.EndVertical();
        }

        private string GetModuleNameFromOperation(string operationName)
        {
            if (string.IsNullOrEmpty(operationName)) return "未知模块";

            // 尝试从操作名称中提取模块名
            var parts = operationName.Split('.');
            foreach (var part in parts)
            {
                if (part.Contains("Module"))
                {
                    return part;
                }
            }

            return parts.Length > 0 ? parts[0] : "其他";
        }
    }
}

