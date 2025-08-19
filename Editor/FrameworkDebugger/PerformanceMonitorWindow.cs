using System;
using System.Collections.Generic;
using System.Linq;
using CnoomFramework.Core;
using CnoomFramework.Core.Events;
using CnoomFramework.Core.Performance;
using CnoomFramework.Editor.Editor;
using UnityEditor;
using UnityEngine;

namespace CnoomFramework.Editor
{
    /// <summary>
    /// 性能监控窗口，用于可视化展示框架性能数据
    /// </summary>
    public class PerformanceMonitorWindow : EditorWindow
    {
        private const string MenuPath = FrameworkEditorConfig.MenuPath + "性能监视器";
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
        private float _refreshInterval = 1.0f;
        private float _lastRefreshTime;
        private bool _showInactiveOperations = false;
        private bool _showDetails = true;

        // 性能数据
        private Dictionary<string, PerformanceStats> _performanceStats = new Dictionary<string, PerformanceStats>();
        private Dictionary<string, bool> _operationFoldouts = new Dictionary<string, bool>();
        private Dictionary<string, Color> _operationColors = new Dictionary<string, Color>();
        private List<string> _categories = new List<string>();
        private Dictionary<string, bool> _categoryFoldouts = new Dictionary<string, bool>();
        private Dictionary<string, List<string>> _categoryOperations = new Dictionary<string, List<string>>();

        // 图表数据
        private Dictionary<string, List<float>> _timeSeriesData = new Dictionary<string, List<float>>();
        private int _maxTimeSeriesPoints = 100;
        private bool _showCharts = true;
        private string _selectedOperation = null;
        private int _chartHeight = 100;

        // 排序选项
        private enum SortOption
        {
            Name,
            TotalTime,
            AverageTime,
            CallCount,
            MaxTime,
            LastTime
        }

        private SortOption _sortOption = SortOption.TotalTime;
        private bool _sortAscending = false;

        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            var window = GetWindow<PerformanceMonitorWindow>("Performance Monitor");
            window.minSize = new Vector2(800, 500);
            window.Show();
        }

        private void OnEnable()
        {
            // 检查框架是否已初始化
            EditorApplication.update += CheckFrameworkStatus;
            _lastRefreshTime = Time.realtimeSinceStartup;
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
                        if (!_isFrameworkInitialized)
                        {
                            _isFrameworkInitialized = true;
                            SubscribeToFrameworkEvents();
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
                    // 编辑模式下，清除性能数据
                    if (_isFrameworkInitialized)
                    {
                        _isFrameworkInitialized = false;
                        _performanceStats.Clear();
                        _timeSeriesData.Clear();
                        UpdateCategories();
                    }
                }
            }
            catch (Exception ex)
            {
                // 捕获任何异常，确保窗口不会崩溃
                _isFrameworkInitialized = false;
                Debug.LogError($"性能监控窗口检查框架状态时出错: {ex.Message}");
            }
        }

        private void SubscribeToFrameworkEvents()
        {
            try
            {
                // 订阅性能相关事件
                var eventBus = FrameworkManager.Instance.EventBus;
                eventBus.Subscribe<PerformanceDataUpdatedEvent>(OnPerformanceDataUpdated);
                eventBus.Subscribe<PerformanceMonitorStatusChangedEvent>(OnPerformanceMonitorStatusChanged);
                eventBus.Subscribe<PerformanceStatsResetEvent>(OnPerformanceStatsReset);
            }
            catch (Exception ex)
            {
                Debug.LogError($"订阅框架事件时出错: {ex.Message}");
            }
        }

        private void OnPerformanceDataUpdated(PerformanceDataUpdatedEvent evt)
        {
            _performanceStats[evt.OperationName] = evt.Stats;

            // 更新时间序列数据
            if (!_timeSeriesData.ContainsKey(evt.OperationName))
            {
                _timeSeriesData[evt.OperationName] = new List<float>();
            }

            var timeSeriesList = _timeSeriesData[evt.OperationName];
            timeSeriesList.Add(evt.Stats.LastTime);

            // 限制时间序列数据点数量
            if (timeSeriesList.Count > _maxTimeSeriesPoints)
            {
                timeSeriesList.RemoveAt(0);
            }

            UpdateCategories();
            Repaint();
        }

        private void OnPerformanceMonitorStatusChanged(PerformanceMonitorStatusChangedEvent evt)
        {
            Repaint();
        }

        private void OnPerformanceStatsReset(PerformanceStatsResetEvent evt)
        {
            if (string.IsNullOrEmpty(evt.OperationName))
            {
                // 重置所有统计数据
                _performanceStats.Clear();
                _timeSeriesData.Clear();
            }
            else
            {
                // 重置指定操作的统计数据
                _performanceStats.Remove(evt.OperationName);
                _timeSeriesData.Remove(evt.OperationName);
            }

            UpdateCategories();
            Repaint();
        }

        private void RefreshPerformanceData()
        {
            if (!_isFrameworkInitialized) return;

            try
            {
                // 获取性能监控模块
                var performanceModule =
                    FrameworkManager.Instance.GetModule("PerformanceMonitor") as PerformanceMonitorModule;
                if (performanceModule == null) return;

                // 获取性能数据
                var stats = performanceModule.PerformanceMonitor.GetAllStats();
                _performanceStats = new Dictionary<string, PerformanceStats>(stats);

                // 为每个操作分配颜色
                foreach (var operationName in _performanceStats.Keys)
                {
                    if (!_operationColors.ContainsKey(operationName))
                    {
                        _operationColors[operationName] = GetRandomColor();
                    }
                }

                UpdateCategories();
            }
            catch (Exception ex)
            {
                Debug.LogError($"刷新性能数据时出错: {ex.Message}");
            }
        }

        private void UpdateCategories()
        {
            _categories.Clear();
            _categoryOperations.Clear();

            foreach (var operationName in _performanceStats.Keys)
            {
                string category = "其他";

                // 尝试从操作名称中提取类别
                int dotIndex = operationName.IndexOf('.');
                if (dotIndex > 0)
                {
                    category = operationName.Substring(0, dotIndex);
                }

                if (!_categories.Contains(category))
                {
                    _categories.Add(category);
                    _categoryOperations[category] = new List<string>();
                }

                _categoryOperations[category].Add(operationName);
            }

            // 确保每个类别都有折叠状态
            foreach (var category in _categories)
            {
                if (!_categoryFoldouts.ContainsKey(category))
                {
                    _categoryFoldouts[category] = true;
                }
            }

            // 排序类别
            _categories.Sort();
        }

        private Color GetRandomColor()
        {
            return new Color(
                UnityEngine.Random.Range(0.3f, 0.9f),
                UnityEngine.Random.Range(0.3f, 0.9f),
                UnityEngine.Random.Range(0.3f, 0.9f)
            );
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
                DrawPerformanceData();
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
            // 安全检查，确保在编辑模式下不会尝试访问FrameworkManager
            bool canAccessFramework = Application.isPlaying && _isFrameworkInitialized;
            
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton))
            {
                if (canAccessFramework)
                {
                    RefreshPerformanceData();
                }
            }

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "自动刷新", EditorStyles.toolbarButton);

            if (_autoRefresh)
            {
                GUILayout.Label("刷新间隔:", GUILayout.Width(60));
                _refreshInterval = EditorGUILayout.Slider(_refreshInterval, 0.1f, 5.0f, GUILayout.Width(100));
            }

            _showInactiveOperations = GUILayout.Toggle(_showInactiveOperations, "显示非活跃操作", EditorStyles.toolbarButton);
            _showDetails = GUILayout.Toggle(_showDetails, "显示详情", EditorStyles.toolbarButton);
            _showCharts = GUILayout.Toggle(_showCharts, "显示图表", EditorStyles.toolbarButton);

            GUILayout.FlexibleSpace();

            // 排序选项
            GUILayout.Label("排序:", GUILayout.Width(40));
            var sortOptions = Enum.GetNames(typeof(SortOption));
            var sortOptionNames = new string[] { "名称", "总时间", "平均时间", "调用次数", "最大时间", "最近时间" };
            int sortIndex = (int)_sortOption;
            int newSortIndex = EditorGUILayout.Popup(sortIndex, sortOptionNames, GUILayout.Width(80));
            if (newSortIndex != sortIndex)
            {
                _sortOption = (SortOption)newSortIndex;
            }

            _sortAscending = GUILayout.Toggle(_sortAscending, _sortAscending ? "↑" : "↓", EditorStyles.toolbarButton,
                GUILayout.Width(25));

            _searchText = EditorGUILayout.TextField(_searchText, _searchFieldStyle);

            if (GUILayout.Button("×", _searchCancelButtonStyle) && !string.IsNullOrEmpty(_searchText))
            {
                _searchText = "";
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();

            // 只有在运行模式下才显示第二个工具栏
            if (canAccessFramework)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

                try
                {
                    // 性能监控控制按钮
                    var performanceModule =
                        FrameworkManager.Instance.GetModule("PerformanceMonitor") as PerformanceMonitorModule;
                    if (performanceModule != null)
                    {
                        bool isEnabled = performanceModule.PerformanceMonitor.IsEnabled;

                        if (GUILayout.Button(isEnabled ? "禁用监控" : "启用监控", EditorStyles.toolbarButton))
                        {
                            performanceModule.SetMonitoringEnabled(!isEnabled);
                        }

                        if (GUILayout.Button("重置所有统计", EditorStyles.toolbarButton))
                        {
                            performanceModule.ResetAllStats();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"访问性能监控模块时出错: {ex.Message}");
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawPerformanceData()
        {
            EditorGUILayout.BeginVertical();

            GUILayout.Label("性能监控", _headerStyle);

            if (_performanceStats.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无性能数据。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // 绘制性能概览
            DrawPerformanceOverview();

            EditorGUILayout.Space();

            // 按类别绘制性能数据
            foreach (var category in _categories)
            {
                DrawCategoryData(category);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPerformanceOverview()
        {
            EditorGUILayout.BeginVertical(_boxStyle);

            GUILayout.Label("性能概览", _subHeaderStyle);

            // 计算总体统计数据
            int totalOperations = _performanceStats.Count;
            int totalCalls = _performanceStats.Values.Sum(s => s.CallCount);
            float totalTime = _performanceStats.Values.Sum(s => s.TotalTime);
            float maxTime = _performanceStats.Values.Max(s => s.MaxTime);

            // 找出最耗时的操作
            string mostExpensiveOperation = "";
            float mostExpensiveTime = 0;

            foreach (var pair in _performanceStats)
            {
                if (pair.Value.TotalTime > mostExpensiveTime)
                {
                    mostExpensiveTime = pair.Value.TotalTime;
                    mostExpensiveOperation = pair.Key;
                }
            }

            // 绘制统计信息
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("操作总数:", totalOperations.ToString());
            EditorGUILayout.LabelField("调用总次数:", totalCalls.ToString());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("总执行时间:", $"{totalTime:F2} ms");
            EditorGUILayout.LabelField("最大执行时间:", $"{maxTime:F2} ms");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("最耗时操作:", mostExpensiveOperation);
            EditorGUILayout.LabelField("耗时:", $"{mostExpensiveTime:F2} ms");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawCategoryData(string category)
        {
            if (!_categoryOperations.ContainsKey(category) || _categoryOperations[category].Count == 0)
            {
                return;
            }

            EditorGUILayout.BeginVertical(_boxStyle);

            // 类别标题和折叠控制
            _categoryFoldouts[category] = EditorGUILayout.Foldout(_categoryFoldouts[category],
                $"{category} ({_categoryOperations[category].Count} 操作)", true, _boldLabelStyle);

            if (_categoryFoldouts[category])
            {
                // 获取并排序该类别下的操作
                var operations = _categoryOperations[category];
                var sortedOperations = SortOperations(operations);

                // 获取列宽度
                float[] columnWidths = GetColumnWidths();

                // 表头行
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                
                // 为颜色标记留出空间
                GUILayout.Space(16);
                
                // 列标题
                GUILayout.Label("操作名称", EditorStyles.toolbarButton, GUILayout.Width(columnWidths[0]));
                GUILayout.Label("调用次数", EditorStyles.toolbarButton, GUILayout.Width(columnWidths[1]));
                GUILayout.Label("总时间 (ms)", EditorStyles.toolbarButton, GUILayout.Width(columnWidths[2]));
                GUILayout.Label("平均时间 (ms)", EditorStyles.toolbarButton, GUILayout.Width(columnWidths[3]));
                GUILayout.Label("最小时间 (ms)", EditorStyles.toolbarButton, GUILayout.Width(columnWidths[4]));
                GUILayout.Label("最大时间 (ms)", EditorStyles.toolbarButton, GUILayout.Width(columnWidths[5]));
                GUILayout.Label("最近时间 (ms)", EditorStyles.toolbarButton, GUILayout.Width(columnWidths[6]));
                GUILayout.Label("操作", EditorStyles.toolbarButton, GUILayout.Width(columnWidths[7] + columnWidths[8]));
                
                EditorGUILayout.EndHorizontal();

                // 绘制操作数据
                foreach (var operationName in sortedOperations)
                {
                    if (!string.IsNullOrEmpty(_searchText) &&
                        !operationName.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    DrawOperationData(operationName);
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
        }

        private List<string> SortOperations(List<string> operations)
        {
            var sortedOperations = new List<string>(operations);

            switch (_sortOption)
            {
                case SortOption.Name:
                    sortedOperations.Sort((a, b) =>
                        _sortAscending
                            ? string.Compare(a, b, StringComparison.Ordinal)
                            : string.Compare(b, a, StringComparison.Ordinal));
                    break;

                case SortOption.TotalTime:
                    sortedOperations.Sort((a, b) =>
                        _sortAscending
                            ? _performanceStats[a].TotalTime.CompareTo(_performanceStats[b].TotalTime)
                            : _performanceStats[b].TotalTime.CompareTo(_performanceStats[a].TotalTime));
                    break;

                case SortOption.AverageTime:
                    sortedOperations.Sort((a, b) =>
                        _sortAscending
                            ? _performanceStats[a].AverageTime.CompareTo(_performanceStats[b].AverageTime)
                            : _performanceStats[b].AverageTime.CompareTo(_performanceStats[a].AverageTime));
                    break;

                case SortOption.CallCount:
                    sortedOperations.Sort((a, b) =>
                        _sortAscending
                            ? _performanceStats[a].CallCount.CompareTo(_performanceStats[b].CallCount)
                            : _performanceStats[b].CallCount.CompareTo(_performanceStats[a].CallCount));
                    break;

                case SortOption.MaxTime:
                    sortedOperations.Sort((a, b) =>
                        _sortAscending
                            ? _performanceStats[a].MaxTime.CompareTo(_performanceStats[b].MaxTime)
                            : _performanceStats[b].MaxTime.CompareTo(_performanceStats[a].MaxTime));
                    break;

                case SortOption.LastTime:
                    sortedOperations.Sort((a, b) =>
                        _sortAscending
                            ? _performanceStats[a].LastTime.CompareTo(_performanceStats[b].LastTime)
                            : _performanceStats[b].LastTime.CompareTo(_performanceStats[a].LastTime));
                    break;
            }

            return sortedOperations;
        }

        private void DrawOperationData(string operationName)
        {
            if (!_performanceStats.TryGetValue(operationName, out var stats))
            {
                return;
            }

            // 确保操作折叠状态字典中有此操作
            if (!_operationFoldouts.ContainsKey(operationName))
            {
                _operationFoldouts[operationName] = false;
            }

            // 获取操作颜色
            Color operationColor = _operationColors.ContainsKey(operationName)
                ? _operationColors[operationName]
                : Color.white;

            // 获取列宽度
            float[] columnWidths = GetColumnWidths();

            // 操作名称（去掉类别前缀）
            string displayName = operationName;
            int dotIndex = operationName.IndexOf('.');
            if (dotIndex > 0)
            {
                displayName = operationName.Substring(dotIndex + 1);
            }
            
            // 绘制操作数据行
            EditorGUILayout.BeginHorizontal();

            // 绘制颜色标记
            GUI.color = operationColor;
            GUILayout.Box("", GUILayout.Width(16), GUILayout.Height(16));
            GUI.color = Color.white;

            // 操作名称和折叠控制
            if (_showDetails)
            {
                EditorGUILayout.BeginHorizontal(GUILayout.Width(columnWidths[0]));
                _operationFoldouts[operationName] = EditorGUILayout.Foldout(_operationFoldouts[operationName],
                    displayName, true);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                var style = new GUIStyle(EditorStyles.label);
                style.clipping = TextClipping.Overflow;
                GUILayout.Label(displayName, style, GUILayout.Width(columnWidths[0]));
            }

            // 统计数据
            GUILayout.Label(stats.CallCount.ToString(), GUILayout.Width(columnWidths[1]));
            GUILayout.Label($"{stats.TotalTime:F2}", GUILayout.Width(columnWidths[2]));
            GUILayout.Label($"{stats.AverageTime:F2}", GUILayout.Width(columnWidths[3]));
            GUILayout.Label($"{stats.MinTime:F2}", GUILayout.Width(columnWidths[4]));
            GUILayout.Label($"{stats.MaxTime:F2}", GUILayout.Width(columnWidths[5]));
            GUILayout.Label($"{stats.LastTime:F2}", GUILayout.Width(columnWidths[6]));

            // 按钮区域
            EditorGUILayout.BeginHorizontal(GUILayout.Width(columnWidths[7] + columnWidths[8]));
            
            // 安全检查，确保在编辑模式下不会尝试访问FrameworkManager
            bool canAccessFramework = Application.isPlaying && _isFrameworkInitialized;
            
            // 重置按钮
            if (GUILayout.Button("重置", _buttonStyle, GUILayout.Width(columnWidths[7])))
            {
                if (canAccessFramework)
                {
                    try
                    {
                        var performanceModule =
                            FrameworkManager.Instance.GetModule("PerformanceMonitor") as PerformanceMonitorModule;
                        if (performanceModule != null)
                        {
                            performanceModule.ResetStats(operationName);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"重置性能统计时出错: {ex.Message}");
                    }
                }
            }

            // 查看图表按钮
            if (_showCharts && GUILayout.Button("图表", _buttonStyle, GUILayout.Width(columnWidths[8])))
            {
                _selectedOperation = operationName == _selectedOperation ? null : operationName;
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();

            // 如果展开，显示详情
            if (_showDetails && _operationFoldouts[operationName])
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField("完整操作名称:", operationName);
                EditorGUILayout.LabelField("最后执行时间:", stats.LastExecutionTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                // 绘制时间序列图表
                if (_showCharts && _timeSeriesData.ContainsKey(operationName) &&
                    _timeSeriesData[operationName].Count > 0)
                {
                    DrawTimeSeriesChart(operationName);
                }

                EditorGUILayout.EndVertical();
            }

            // 如果是选中的操作，绘制大图表
            if (_showCharts && _selectedOperation == operationName)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"'{displayName}' 执行时间图表", _boldLabelStyle);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("关闭", _buttonStyle, GUILayout.Width(50)))
                {
                    _selectedOperation = null;
                }

                EditorGUILayout.EndHorizontal();

                // 绘制大图表
                if (_timeSeriesData.ContainsKey(operationName) && _timeSeriesData[operationName].Count > 0)
                {
                    _chartHeight = 200;
                    DrawTimeSeriesChart(operationName);
                }
                else
                {
                    EditorGUILayout.HelpBox("暂无图表数据。", MessageType.Info);
                }

                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// 获取列宽度数组
        /// </summary>
        /// <returns>列宽度数组</returns>
        private float[] GetColumnWidths()
        {
            // 基础列宽度
            float[] baseColumnWidths = new float[] { 300, 80, 100, 100, 100, 100, 100, 50, 50 };

            // 计算可用宽度
            float availableWidth = EditorGUIUtility.currentViewWidth - 100; // 100是边距和其他元素的宽度
            float totalFixedWidth = 0;

            // 计算除第一列外的总固定宽度
            for (int i = 1; i < baseColumnWidths.Length; i++)
            {
                totalFixedWidth += baseColumnWidths[i];
            }

            // 添加颜色标记的宽度
            totalFixedWidth += 16;

            // 计算第一列的宽度
            float firstColumnWidth = Mathf.Max(baseColumnWidths[0], availableWidth - totalFixedWidth);

            // 创建最终的列宽度数组
            float[] columnWidths = new float[baseColumnWidths.Length];
            columnWidths[0] = firstColumnWidth;
            
            // 复制其他列的宽度
            for (int i = 1; i < baseColumnWidths.Length; i++)
            {
                columnWidths[i] = baseColumnWidths[i];
            }
            
            return columnWidths;
        }
        
        private void DrawTimeSeriesChart(string operationName)
        {
            if (!_timeSeriesData.ContainsKey(operationName) || _timeSeriesData[operationName].Count == 0)
            {
                return;
            }

            var timeSeriesData = _timeSeriesData[operationName];
            var rect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - 40, _chartHeight);
            
            // 计算最大值和最小值
            float maxValue = timeSeriesData.Max();
            float minValue = timeSeriesData.Min();
            
            // 确保有一定的范围
            if (Mathf.Approximately(maxValue, minValue))
            {
                maxValue = minValue + 1.0f;
            }
            
            // 绘制背景
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));
            
            // 绘制网格线
            int gridLines = 4;
            for (int i = 1; i < gridLines; i++)
            {
                float y = rect.y + rect.height * (1.0f - (float)i / gridLines);
                Handles.color = new Color(1, 1, 1, 0.2f);
                Handles.DrawLine(new Vector3(rect.x, y), new Vector3(rect.x + rect.width, y));
                
                // 绘制网格值
                float value = minValue + (maxValue - minValue) * ((float)i / gridLines);
                GUI.color = Color.white;
                GUI.Label(new Rect(rect.x, y - 8, 50, 16), $"{value:F2}");
            }
            
            // 绘制数据线
            if (timeSeriesData.Count > 1)
            {
                Color lineColor = _operationColors.ContainsKey(operationName) ? _operationColors[operationName] : Color.cyan;
                Handles.color = lineColor;
                
                for (int i = 0; i < timeSeriesData.Count - 1; i++)
                {
                    float x1 = rect.x + rect.width * ((float)i / (timeSeriesData.Count - 1));
                    float y1 = rect.y + rect.height * (1.0f - (timeSeriesData[i] - minValue) / (maxValue - minValue));
                    
                    float x2 = rect.x + rect.width * ((float)(i + 1) / (timeSeriesData.Count - 1));
                    float y2 = rect.y + rect.height * (1.0f - (timeSeriesData[i + 1] - minValue) / (maxValue - minValue));
                    
                    Handles.DrawLine(new Vector3(x1, y1), new Vector3(x2, y2));
                }
            }
            
            // 绘制统计信息
            GUI.color = Color.white;
            string statsText = $"最小: {minValue:F2} ms  最大: {maxValue:F2} ms  平均: {timeSeriesData.Average():F2} ms  样本数: {timeSeriesData.Count}";
            GUI.Label(new Rect(rect.x, rect.y + rect.height - 16, rect.width, 16), statsText);
            
            // 重置颜色
            GUI.color = Color.white;
            Handles.color = Color.white;
        }
        
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
