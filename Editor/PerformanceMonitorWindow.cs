using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CnoomFramework.Core;
using CnoomFramework.Core.Performance;

namespace CnoomFramework.Editor
{
    /// <summary>
    /// 性能监控窗口 - 提供实时Unity界面监控仪表盘功能
    /// </summary>
    public class PerformanceMonitorWindow : EditorWindow
    {
        #region 窗口管理
        [MenuItem("Cnoom Framework/Performance Monitor", priority = 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<PerformanceMonitorWindow>("性能监控");
            window.minSize = new Vector2(1000, 700);
            window.Show();
        }
        #endregion

        #region 私有字段
        private enum MonitorTab
        {
            Overview,      // 概览
            Realtime,      // 实时监控
            History,       // 历史数据
            Statistics,    // 统计分析
            Settings       // 设置
        }

        private MonitorTab _currentTab = MonitorTab.Overview;
        private Vector2 _scrollPosition;
        private bool _autoRefresh = true;
        private float _refreshInterval = 0.5f;
        private double _lastRefreshTime;

        // 性能监控器引用
        private PerformanceMonitor _performanceMonitor;
        
        // 图表数据
        private List<float> _fpsHistory = new List<float>();
        private List<float> _frameTimeHistory = new List<float>();
        private List<float> _memoryHistory = new List<float>();
        private List<float> _executionTimeHistory = new List<float>();
        
        // 显示设置
        private bool _showFPSChart = true;
        private bool _showFrameTimeChart = true;
        private bool _showMemoryChart = true;
        private bool _showExecutionTimeChart = true;
        private int _maxChartPoints = 300;
        
        // 过滤设置
        private string _moduleFilter = "";
        private string _methodFilter = "";
        private float _minExecutionTimeFilter = 0f;
        
        // 颜色主题
        private readonly Color _primaryColor = new Color(0.2f, 0.6f, 1f);
        private readonly Color _secondaryColor = new Color(0f, 0.8f, 0.8f);
        private readonly Color _warningColor = new Color(1f, 0.6f, 0f);
        private readonly Color _errorColor = new Color(1f, 0.3f, 0.3f);
        #endregion

        #region Unity生命周期
        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            InitializeMonitor();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > _refreshInterval)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                UpdateChartData();
                Repaint();
            }
        }
        #endregion

        #region 初始化
        private void InitializeMonitor()
        {
            var frameworkManager = FrameworkManager.Instance;
            if (frameworkManager != null && frameworkManager.IsInitialized)
            {
                _performanceMonitor = frameworkManager.GetModule<PerformanceMonitor>();
            }
        }
        #endregion

        #region GUI绘制
        private void OnGUI()
        {
            if (_performanceMonitor == null)
            {
                DrawNoMonitorMessage();
                return;
            }

            DrawHeader();
            DrawTabButtons();
            DrawTabContent();
        }

        private void DrawNoMonitorMessage()
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox(
                "性能监控器未找到或框架未初始化。\n" +
                "请确保：\n" +
                "1. FrameworkManager已初始化\n" +
                "2. PerformanceMonitor模块已注册", 
                MessageType.Warning);
            
            if (GUILayout.Button("尝试重新初始化"))
            {
                InitializeMonitor();
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // 状态指示器
            var isEnabled = _performanceMonitor.IsEnabled;
            var statusColor = isEnabled ? Color.green : Color.red;
            var statusText = isEnabled ? "监控中" : "已停止";
            
            var originalColor = GUI.color;
            GUI.color = statusColor;
            GUILayout.Label($"状态: {statusText}", EditorStyles.toolbarButton, GUILayout.Width(80));
            GUI.color = originalColor;

            // 实时数据显示
            if (isEnabled)
            {
                var snapshot = _performanceMonitor.GetRealtimeSnapshot();
                GUILayout.Label($"FPS: {snapshot.FrameRate:F1}", EditorStyles.toolbarButton, GUILayout.Width(70));
                GUILayout.Label($"帧时间: {snapshot.FrameTime:F1}ms", EditorStyles.toolbarButton, GUILayout.Width(90));
                GUILayout.Label($"内存: {FormatBytes(snapshot.TotalMemoryUsage)}", EditorStyles.toolbarButton, GUILayout.Width(80));
                GUILayout.Label($"活跃: {snapshot.ActiveExecutions}", EditorStyles.toolbarButton, GUILayout.Width(60));
            }

            GUILayout.FlexibleSpace();

            // 控制按钮
            if (GUILayout.Button(isEnabled ? "停止监控" : "开始监控", EditorStyles.toolbarButton))
            {
                _performanceMonitor.SetEnabled(!isEnabled);
            }

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "自动刷新", EditorStyles.toolbarButton);
            
            if (GUILayout.Button("手动刷新", EditorStyles.toolbarButton))
            {
                UpdateChartData();
                Repaint();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTabButtons()
        {
            EditorGUILayout.BeginHorizontal();
            var tabNames = new[] { "概览", "实时监控", "历史数据", "统计分析", "设置" };
            
            for (int i = 0; i < tabNames.Length; i++)
            {
                var isSelected = (int)_currentTab == i;
                var style = isSelected ? EditorStyles.toolbarButton : EditorStyles.toolbarButton;
                
                if (isSelected)
                {
                    GUI.backgroundColor = _primaryColor;
                }
                
                if (GUILayout.Button(tabNames[i], style))
                {
                    _currentTab = (MonitorTab)i;
                }
                
                if (isSelected)
                {
                    GUI.backgroundColor = Color.white;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTabContent()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            switch (_currentTab)
            {
                case MonitorTab.Overview:
                    DrawOverviewTab();
                    break;
                case MonitorTab.Realtime:
                    DrawRealtimeTab();
                    break;
                case MonitorTab.History:
                    DrawHistoryTab();
                    break;
                case MonitorTab.Statistics:
                    DrawStatisticsTab();
                    break;
                case MonitorTab.Settings:
                    DrawSettingsTab();
                    break;
            }
            
            EditorGUILayout.EndScrollView();
        }
        #endregion

        #region 标签页内容
        private void DrawOverviewTab()
        {
            EditorGUILayout.LabelField("性能监控概览", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 基本信息
            DrawInfoBox("基本信息", () =>
            {
                var report = _performanceMonitor.GeneratePerformanceReport();
                EditorGUILayout.LabelField($"总采样数: {report.TotalSamples:N0}");
                EditorGUILayout.LabelField($"活跃执行: {report.ActiveExecutions}");
                EditorGUILayout.LabelField($"监控模块: {report.ModuleCount}");
                EditorGUILayout.LabelField($"监控方法: {report.MethodCount}");
                EditorGUILayout.LabelField($"平均执行时间: {report.AverageExecutionTime.TotalMilliseconds:F2}ms");
                EditorGUILayout.LabelField($"最大执行时间: {report.MaxExecutionTime.TotalMilliseconds:F2}ms");
                EditorGUILayout.LabelField($"总内存分配: {FormatBytes(report.TotalMemoryAllocated)}");
            });

            EditorGUILayout.Space();

            // 性能最差的方法
            DrawInfoBox("性能最差的方法 (Top 5)", () =>
            {
                var report = _performanceMonitor.GeneratePerformanceReport();
                if (report.SlowestMethods.Count == 0)
                {
                    EditorGUILayout.LabelField("暂无数据");
                    return;
                }

                foreach (var method in report.SlowestMethods)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(method.MethodName, GUILayout.Width(200));
                    EditorGUILayout.LabelField($"{method.AverageTime.TotalMilliseconds:F2}ms", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"({method.CallCount} 次调用)", GUILayout.Width(80));
                    EditorGUILayout.EndHorizontal();
                }
            });

            EditorGUILayout.Space();

            // 快速操作
            DrawInfoBox("快速操作", () =>
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("清空历史数据"))
                {
                    _performanceMonitor.ClearHistory();
                    _fpsHistory.Clear();
                    _frameTimeHistory.Clear();
                    _memoryHistory.Clear();
                    _executionTimeHistory.Clear();
                }
                
                if (GUILayout.Button("生成性能报告"))
                {
                    var report = _performanceMonitor.GeneratePerformanceReport();
                    Debug.Log($"性能报告已生成: {report.TotalSamples} 个采样点");
                }
                
                if (GUILayout.Button("导出数据"))
                {
                    ExportPerformanceData();
                }
                EditorGUILayout.EndHorizontal();
            });
        }

        private void DrawRealtimeTab()
        {
            EditorGUILayout.LabelField("实时性能监控", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 图表显示选项
            EditorGUILayout.BeginHorizontal();
            _showFPSChart = EditorGUILayout.Toggle("FPS", _showFPSChart, GUILayout.Width(60));
            _showFrameTimeChart = EditorGUILayout.Toggle("帧时间", _showFrameTimeChart, GUILayout.Width(80));
            _showMemoryChart = EditorGUILayout.Toggle("内存", _showMemoryChart, GUILayout.Width(60));
            _showExecutionTimeChart = EditorGUILayout.Toggle("执行时间", _showExecutionTimeChart, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 绘制图表
            if (_showFPSChart)
            {
                DrawChart("FPS", _fpsHistory, Color.green, 0, 120, "帧/秒");
                EditorGUILayout.Space();
            }

            if (_showFrameTimeChart)
            {
                DrawChart("帧时间", _frameTimeHistory, Color.red, 0, 50, "毫秒");
                EditorGUILayout.Space();
            }

            if (_showMemoryChart)
            {
                DrawChart("内存使用", _memoryHistory, Color.blue, 0, _memoryHistory.Count > 0 ? _memoryHistory.Max() * 1.1f : 100, "MB");
                EditorGUILayout.Space();
            }

            if (_showExecutionTimeChart)
            {
                DrawChart("平均执行时间", _executionTimeHistory, _warningColor, 0, _executionTimeHistory.Count > 0 ? _executionTimeHistory.Max() * 1.1f : 10, "毫秒");
            }
        }

        private void DrawHistoryTab()
        {
            EditorGUILayout.LabelField("历史性能数据", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 过滤器
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("过滤器:", GUILayout.Width(50));
            _moduleFilter = EditorGUILayout.TextField("模块:", _moduleFilter, GUILayout.Width(150));
            _methodFilter = EditorGUILayout.TextField("方法:", _methodFilter, GUILayout.Width(150));
            _minExecutionTimeFilter = EditorGUILayout.FloatField("最小执行时间(ms):", _minExecutionTimeFilter, GUILayout.Width(150));
            if (GUILayout.Button("清空过滤器", GUILayout.Width(80)))
            {
                _moduleFilter = "";
                _methodFilter = "";
                _minExecutionTimeFilter = 0f;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 历史数据表格
            var history = _performanceMonitor.GetPerformanceHistory(100);
            var filteredHistory = FilterHistoryData(history);

            if (filteredHistory.Count == 0)
            {
                EditorGUILayout.HelpBox("没有符合条件的历史数据", MessageType.Info);
                return;
            }

            // 表格标题
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("时间", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("模块", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField("方法", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField("执行时间", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("内存变化", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("线程ID", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 表格数据
            foreach (var metrics in filteredHistory.Take(50)) // 限制显示数量
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(metrics.Timestamp.ToString("HH:mm:ss"), GUILayout.Width(80));
                EditorGUILayout.LabelField(metrics.ModuleName, GUILayout.Width(100));
                EditorGUILayout.LabelField(metrics.MethodName, GUILayout.Width(150));
                
                // 执行时间着色
                var executionTimeMs = metrics.ExecutionTime.TotalMilliseconds;
                var timeColor = executionTimeMs > 10 ? _errorColor : (executionTimeMs > 5 ? _warningColor : Color.white);
                var originalColor = GUI.color;
                GUI.color = timeColor;
                EditorGUILayout.LabelField($"{executionTimeMs:F2}ms", GUILayout.Width(80));
                GUI.color = originalColor;
                
                EditorGUILayout.LabelField(FormatBytes(metrics.MemoryDelta), GUILayout.Width(80));
                EditorGUILayout.LabelField(metrics.ThreadId.ToString(), GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawStatisticsTab()
        {
            EditorGUILayout.LabelField("统计分析", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 模块统计
            DrawInfoBox("模块性能统计", () =>
            {
                var moduleStats = _performanceMonitor.ModuleStatistics;
                if (moduleStats.Count == 0)
                {
                    EditorGUILayout.LabelField("暂无模块统计数据");
                    return;
                }

                // 表格标题
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("模块名", EditorStyles.boldLabel, GUILayout.Width(120));
                EditorGUILayout.LabelField("调用次数", EditorStyles.boldLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField("平均时间", EditorStyles.boldLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField("最大时间", EditorStyles.boldLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField("总时间", EditorStyles.boldLabel, GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();

                foreach (var kvp in moduleStats.OrderByDescending(x => x.Value.AverageExecutionTime.TotalMilliseconds))
                {
                    var stats = kvp.Value;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(stats.Name, GUILayout.Width(120));
                    EditorGUILayout.LabelField(stats.SampleCount.ToString(), GUILayout.Width(80));
                    EditorGUILayout.LabelField($"{stats.AverageExecutionTime.TotalMilliseconds:F2}ms", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"{stats.MaxExecutionTime.TotalMilliseconds:F2}ms", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"{stats.TotalExecutionTime.TotalMilliseconds:F2}ms", GUILayout.Width(80));
                    EditorGUILayout.EndHorizontal();
                }
            });

            EditorGUILayout.Space();

            // 方法统计
            DrawInfoBox("方法性能统计 (Top 10)", () =>
            {
                var methodStats = _performanceMonitor.MethodStatistics;
                if (methodStats.Count == 0)
                {
                    EditorGUILayout.LabelField("暂无方法统计数据");
                    return;
                }

                // 表格标题
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("方法名", EditorStyles.boldLabel, GUILayout.Width(200));
                EditorGUILayout.LabelField("调用次数", EditorStyles.boldLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField("平均时间", EditorStyles.boldLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField("最大时间", EditorStyles.boldLabel, GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();

                foreach (var kvp in methodStats.OrderByDescending(x => x.Value.AverageExecutionTime.TotalMilliseconds).Take(10))
                {
                    var stats = kvp.Value;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(stats.Name, GUILayout.Width(200));
                    EditorGUILayout.LabelField(stats.SampleCount.ToString(), GUILayout.Width(80));
                    EditorGUILayout.LabelField($"{stats.AverageExecutionTime.TotalMilliseconds:F2}ms", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"{stats.MaxExecutionTime.TotalMilliseconds:F2}ms", GUILayout.Width(80));
                    EditorGUILayout.EndHorizontal();
                }
            });
        }

        private void DrawSettingsTab()
        {
            EditorGUILayout.LabelField("性能监控设置", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 监控设置
            DrawInfoBox("监控设置", () =>
            {
                var isEnabled = _performanceMonitor.IsEnabled;
                var newEnabled = EditorGUILayout.Toggle("启用性能监控", isEnabled);
                if (newEnabled != isEnabled)
                {
                    _performanceMonitor.SetEnabled(newEnabled);
                }

                EditorGUILayout.Space();

                var currentInterval = _performanceMonitor.GetType()
                    .GetField("_samplingInterval", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(_performanceMonitor) as float? ?? 1.0f;
                
                var newInterval = EditorGUILayout.Slider("采样间隔 (秒)", currentInterval, 0.1f, 5.0f);
                if (Math.Abs(newInterval - currentInterval) > 0.01f)
                {
                    _performanceMonitor.SetSamplingInterval(newInterval);
                }
            });

            EditorGUILayout.Space();

            // 显示设置
            DrawInfoBox("显示设置", () =>
            {
                _refreshInterval = EditorGUILayout.Slider("刷新间隔 (秒)", _refreshInterval, 0.1f, 2.0f);
                _maxChartPoints = EditorGUILayout.IntSlider("图表最大点数", _maxChartPoints, 50, 1000);
                
                EditorGUILayout.Space();
                
                EditorGUILayout.LabelField("图表显示:", EditorStyles.boldLabel);
                _showFPSChart = EditorGUILayout.Toggle("显示FPS图表", _showFPSChart);
                _showFrameTimeChart = EditorGUILayout.Toggle("显示帧时间图表", _showFrameTimeChart);
                _showMemoryChart = EditorGUILayout.Toggle("显示内存图表", _showMemoryChart);
                _showExecutionTimeChart = EditorGUILayout.Toggle("显示执行时间图表", _showExecutionTimeChart);
            });

            EditorGUILayout.Space();

            // 数据管理
            DrawInfoBox("数据管理", () =>
            {
                EditorGUILayout.LabelField($"历史记录数量: {_performanceMonitor.HistoryCount:N0}");
                EditorGUILayout.LabelField($"模块统计数量: {_performanceMonitor.ModuleStatistics.Count}");
                EditorGUILayout.LabelField($"方法统计数量: {_performanceMonitor.MethodStatistics.Count}");
                
                EditorGUILayout.Space();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("清空所有数据"))
                {
                    if (EditorUtility.DisplayDialog("确认", "确定要清空所有性能数据吗？", "确定", "取消"))
                    {
                        _performanceMonitor.ClearHistory();
                        _fpsHistory.Clear();
                        _frameTimeHistory.Clear();
                        _memoryHistory.Clear();
                        _executionTimeHistory.Clear();
                    }
                }
                
                if (GUILayout.Button("导出数据"))
                {
                    ExportPerformanceData();
                }
                EditorGUILayout.EndHorizontal();
            });
        }
        #endregion

        #region 辅助方法
        private void DrawInfoBox(string title, System.Action content)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            content?.Invoke();
            EditorGUILayout.EndVertical();
        }

        private void DrawChart(string title, List<float> data, Color color, float minValue, float maxValue, string unit)
        {
            EditorGUILayout.LabelField($"{title} ({unit})", EditorStyles.boldLabel);
            
            var rect = GUILayoutUtility.GetRect(0, 120, GUILayout.ExpandWidth(true));
            
            // 绘制背景
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 0.3f));
            
            if (data.Count < 2) return;

            // 绘制网格线
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            for (int i = 1; i < 5; i++)
            {
                float y = rect.y + rect.height * i / 5f;
                Handles.DrawLine(new Vector3(rect.x, y), new Vector3(rect.xMax, y));
            }

            // 绘制数据线
            Handles.color = color;
            var points = new List<Vector3>();
            
            var dataToShow = data.Count > _maxChartPoints ? 
                data.Skip(data.Count - _maxChartPoints).ToList() : data;
            
            for (int i = 0; i < dataToShow.Count; i++)
            {
                float x = rect.x + rect.width * i / (dataToShow.Count - 1);
                float normalizedValue = Mathf.InverseLerp(minValue, maxValue, dataToShow[i]);
                float y = rect.yMax - rect.height * normalizedValue;
                points.Add(new Vector3(x, y, 0));
            }

            if (points.Count > 1)
            {
                Handles.DrawAAPolyLine(2f, points.ToArray());
            }

            // 绘制当前值和范围标签
            var labelStyle = new GUIStyle(EditorStyles.miniLabel);
            labelStyle.normal.textColor = Color.white;
            
            if (dataToShow.Count > 0)
            {
                GUI.Label(new Rect(rect.x + 5, rect.y + 5, 150, 20), 
                    $"当前: {dataToShow.Last():F1} {unit}", labelStyle);
                GUI.Label(new Rect(rect.x + 5, rect.y + 25, 150, 20), 
                    $"平均: {dataToShow.Average():F1} {unit}", labelStyle);
                GUI.Label(new Rect(rect.x + 5, rect.yMax - 40, 150, 20), 
                    $"范围: {minValue:F1} - {maxValue:F1} {unit}", labelStyle);
                GUI.Label(new Rect(rect.x + 5, rect.yMax - 20, 150, 20), 
                    $"点数: {dataToShow.Count}", labelStyle);
            }
        }

        private void UpdateChartData()
        {
            if (_performanceMonitor == null || !_performanceMonitor.IsEnabled)
                return;

            var snapshot = _performanceMonitor.GetRealtimeSnapshot();
            
            // 更新FPS数据
            _fpsHistory.Add(snapshot.FrameRate);
            if (_fpsHistory.Count > _maxChartPoints)
                _fpsHistory.RemoveAt(0);

            // 更新帧时间数据
            _frameTimeHistory.Add(snapshot.FrameTime);
            if (_frameTimeHistory.Count > _maxChartPoints)
                _frameTimeHistory.RemoveAt(0);

            // 更新内存数据
            _memoryHistory.Add(snapshot.TotalMemoryUsage / (1024f * 1024f)); // 转换为MB
            if (_memoryHistory.Count > _maxChartPoints)
                _memoryHistory.RemoveAt(0);

            // 更新执行时间数据
            _executionTimeHistory.Add((float)snapshot.RecentAverageExecutionTime.TotalMilliseconds);
            if (_executionTimeHistory.Count > _maxChartPoints)
                _executionTimeHistory.RemoveAt(0);
        }

        private List<PerformanceMetrics> FilterHistoryData(List<PerformanceMetrics> history)
        {
            return history.Where(m =>
                (string.IsNullOrEmpty(_moduleFilter) || m.ModuleName.Contains(_moduleFilter)) &&
                (string.IsNullOrEmpty(_methodFilter) || m.MethodName.Contains(_methodFilter)) &&
                (m.ExecutionTime.TotalMilliseconds >= _minExecutionTimeFilter)
            ).ToList();
        }

        private void ExportPerformanceData()
        {
            var path = EditorUtility.SaveFilePanel("导出性能数据", "", "performance_data", "csv");
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                var history = _performanceMonitor.GetPerformanceHistory();
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Timestamp,Module,Method,ExecutionTime(ms),MemoryDelta(bytes),ThreadId");
                
                foreach (var metrics in history)
                {
                    csv.AppendLine($"{metrics.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
                                  $"{metrics.ModuleName}," +
                                  $"{metrics.MethodName}," +
                                  $"{metrics.ExecutionTime.TotalMilliseconds:F3}," +
                                  $"{metrics.MemoryDelta}," +
                                  $"{metrics.ThreadId}");
                }
                
                System.IO.File.WriteAllText(path, csv.ToString());
                EditorUtility.DisplayDialog("导出成功", $"性能数据已导出到:\n{path}", "确定");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("导出失败", $"导出性能数据时发生错误:\n{ex.Message}", "确定");
            }
        }

        private string FormatBytes(long bytes)
        {
            if (bytes == 0) return "0 B";
            
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int suffixIndex = 0;
            double size = Math.Abs(bytes);

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            var sign = bytes < 0 ? "-" : "";
            return $"{sign}{size:F2} {suffixes[suffixIndex]}";
        }
        #endregion
    }
}