using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CnoomFramework.Core;

namespace CnoomFramework.Editor
{
    /// <summary>
    /// 性能监控面板
    /// </summary>
    public class PerformancePanel
    {
        private Vector2 _scrollPosition;
        private bool _enablePerformanceMonitoring = true;
        private List<PerformanceDataPoint> _performanceHistory = new List<PerformanceDataPoint>();
        private int _maxHistoryPoints = 300; // 5分钟的数据（假设1秒更新一次）
        private float _updateInterval = 1.0f;
        private double _lastUpdateTime;

        private class PerformanceDataPoint
        {
            public DateTime Timestamp;
            public float FrameTime;
            public float FPS;
            public long MemoryUsage;
            public int EventCount;
            public int ModuleCount;
        }

        public void OnGUI()
        {
            UpdatePerformanceData();
            DrawHeader();
            DrawControls();
            DrawPerformanceCharts();
            DrawDetailedMetrics();
        }

        private void UpdatePerformanceData()
        {
            if (!_enablePerformanceMonitoring) return;

            if (EditorApplication.timeSinceStartup - _lastUpdateTime > _updateInterval)
            {
                _lastUpdateTime = EditorApplication.timeSinceStartup;
                CollectPerformanceData();
            }
        }

        private void CollectPerformanceData()
        {
            var frameworkManager = FrameworkManager.Instance;
            
            var dataPoint = new PerformanceDataPoint
            {
                Timestamp = DateTime.Now,
                FrameTime = Time.unscaledDeltaTime * 1000f, // 转换为毫秒
                FPS = 1.0f / Time.unscaledDeltaTime,
                MemoryUsage = GC.GetTotalMemory(false),
                EventCount = 0, // 这里需要从EventBus获取，暂时设为0
                ModuleCount = frameworkManager?.ModuleCount ?? 0
            };

            _performanceHistory.Add(dataPoint);

            // 限制历史数据点数量
            while (_performanceHistory.Count > _maxHistoryPoints)
            {
                _performanceHistory.RemoveAt(0);
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("性能监控面板", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 实时性能指标
            if (_performanceHistory.Count > 0)
            {
                var latest = _performanceHistory.Last();
                
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("实时性能指标", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"FPS: {latest.FPS:F1}", GUILayout.Width(100));
                EditorGUILayout.LabelField($"帧时间: {latest.FrameTime:F2}ms", GUILayout.Width(120));
                EditorGUILayout.LabelField($"内存: {FormatBytes(latest.MemoryUsage)}", GUILayout.Width(120));
                EditorGUILayout.LabelField($"模块数: {latest.ModuleCount}", GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawControls()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("监控控制", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _enablePerformanceMonitoring = EditorGUILayout.Toggle("启用性能监控", _enablePerformanceMonitoring);
            
            if (GUILayout.Button("清空历史数据", GUILayout.Width(100)))
            {
                _performanceHistory.Clear();
            }
            
            EditorGUILayout.LabelField("更新间隔:", GUILayout.Width(60));
            _updateInterval = EditorGUILayout.Slider(_updateInterval, 0.1f, 5.0f, GUILayout.Width(100));
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("历史数据点:", GUILayout.Width(80));
            _maxHistoryPoints = EditorGUILayout.IntSlider(_maxHistoryPoints, 50, 1000, GUILayout.Width(200));
            EditorGUILayout.LabelField($"({_performanceHistory.Count}/{_maxHistoryPoints})", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawPerformanceCharts()
        {
            if (_performanceHistory.Count < 2)
            {
                EditorGUILayout.HelpBox("需要更多数据点来显示性能图表。", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("性能图表", EditorStyles.boldLabel);

            // FPS图表
            DrawChart("FPS", _performanceHistory.Select(p => p.FPS).ToArray(), Color.green, 0, 120);
            
            EditorGUILayout.Space();
            
            // 帧时间图表
            DrawChart("帧时间 (ms)", _performanceHistory.Select(p => p.FrameTime).ToArray(), Color.red, 0, 50);
            
            EditorGUILayout.Space();
            
            // 内存使用图表
            var memoryMB = _performanceHistory.Select(p => p.MemoryUsage / (1024f * 1024f)).ToArray();
            DrawChart("内存使用 (MB)", memoryMB, Color.blue, 0, memoryMB.Max() * 1.1f);
        }

        private void DrawChart(string title, float[] values, Color color, float minValue, float maxValue)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            
            var rect = GUILayoutUtility.GetRect(0, 100, GUILayout.ExpandWidth(true));
            
            // 绘制背景
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
            
            if (values.Length < 2) return;

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
            
            for (int i = 0; i < values.Length; i++)
            {
                float x = rect.x + rect.width * i / (values.Length - 1);
                float normalizedValue = Mathf.InverseLerp(minValue, maxValue, values[i]);
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
            
            GUI.Label(new Rect(rect.x + 5, rect.y + 5, 100, 20), 
                $"当前: {values.Last():F1}", labelStyle);
            GUI.Label(new Rect(rect.x + 5, rect.yMax - 20, 100, 20), 
                $"范围: {minValue:F1} - {maxValue:F1}", labelStyle);
        }

        private void DrawDetailedMetrics()
        {
            EditorGUILayout.LabelField("详细指标", EditorStyles.boldLabel);

            if (_performanceHistory.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无性能数据。", MessageType.Info);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));

            EditorGUILayout.BeginVertical("box");

            // 统计信息
            var fpsValues = _performanceHistory.Select(p => p.FPS).ToArray();
            var frameTimeValues = _performanceHistory.Select(p => p.FrameTime).ToArray();
            var memoryValues = _performanceHistory.Select(p => p.MemoryUsage).ToArray();

            EditorGUILayout.LabelField("统计信息 (基于历史数据)", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("FPS:");
            EditorGUILayout.LabelField($"  平均: {fpsValues.Average():F1}");
            EditorGUILayout.LabelField($"  最小: {fpsValues.Min():F1}");
            EditorGUILayout.LabelField($"  最大: {fpsValues.Max():F1}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("帧时间 (ms):");
            EditorGUILayout.LabelField($"  平均: {frameTimeValues.Average():F2}");
            EditorGUILayout.LabelField($"  最小: {frameTimeValues.Min():F2}");
            EditorGUILayout.LabelField($"  最大: {frameTimeValues.Max():F2}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("内存使用:");
            EditorGUILayout.LabelField($"  平均: {FormatBytes((long)memoryValues.Average())}");
            EditorGUILayout.LabelField($"  最小: {FormatBytes(memoryValues.Min())}");
            EditorGUILayout.LabelField($"  最大: {FormatBytes(memoryValues.Max())}");
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // 性能警告
            DrawPerformanceWarnings();

            EditorGUILayout.EndScrollView();
        }

        private void DrawPerformanceWarnings()
        {
            if (_performanceHistory.Count == 0) return;

            var latest = _performanceHistory.Last();
            var warnings = new List<string>();

            if (latest.FPS < 30)
                warnings.Add($"FPS过低: {latest.FPS:F1} (建议 > 30)");
            
            if (latest.FrameTime > 33.33f)
                warnings.Add($"帧时间过长: {latest.FrameTime:F2}ms (建议 < 33.33ms)");
            
            if (latest.MemoryUsage > 500 * 1024 * 1024) // 500MB
                warnings.Add($"内存使用过高: {FormatBytes(latest.MemoryUsage)} (建议 < 500MB)");

            if (warnings.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("性能警告", EditorStyles.boldLabel);
                foreach (var warning in warnings)
                {
                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int suffixIndex = 0;
            double size = bytes;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:F2} {suffixes[suffixIndex]}";
        }
    }
}