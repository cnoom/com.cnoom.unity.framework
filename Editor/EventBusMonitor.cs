using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CnoomFramework.Core;
using CnoomFramework.Core.EventBuss.Interfaces;

namespace CnoomFramework.Editor
{
    /// <summary>
    /// 事件总线监视器
    /// </summary>
    public class EventBusMonitor
    {
        private Vector2 _scrollPosition;
        private bool _enableEventLogging = false;
        private List<EventLogEntry> _eventLog = new List<EventLogEntry>();
        private int _maxLogEntries = 1000;
        private string _eventFilter = "";
        private bool _showBroadcastEvents = true;
        private bool _showCommandEvents = true;
        private bool _showQueryEvents = true;

        private class EventLogEntry
        {
            public DateTime Timestamp;
            public string EventType;
            public string EventName;
            public string Data;
            public EventCategory Category;
        }

        private enum EventCategory
        {
            Broadcast,
            Command,
            Query
        }

        public void OnGUI()
        {
            DrawHeader();
            DrawControls();
            DrawEventLog();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("事件总线监视器", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            var frameworkManager = FrameworkManager.Instance;
            if (frameworkManager == null || !frameworkManager.IsInitialized)
            {
                EditorGUILayout.HelpBox("框架未初始化，无法监控事件总线。", MessageType.Info);
                return;
            }

            // 事件总线状态
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("事件总线状态", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("状态: 运行中");
            EditorGUILayout.LabelField($"日志条目: {_eventLog.Count}/{_maxLogEntries}");
            EditorGUILayout.EndVertical();
        }

        private void DrawControls()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("控制面板", EditorStyles.boldLabel);

            // 事件日志控制
            EditorGUILayout.BeginHorizontal();
            _enableEventLogging = EditorGUILayout.Toggle("启用事件日志", _enableEventLogging);
            
            if (GUILayout.Button("清空日志", GUILayout.Width(80)))
            {
                _eventLog.Clear();
            }
            
            EditorGUILayout.LabelField("最大条目:", GUILayout.Width(60));
            _maxLogEntries = EditorGUILayout.IntField(_maxLogEntries, GUILayout.Width(80));
            _maxLogEntries = Mathf.Clamp(_maxLogEntries, 100, 10000);
            
            EditorGUILayout.EndHorizontal();

            // 过滤器
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("事件过滤:", GUILayout.Width(60));
            _eventFilter = EditorGUILayout.TextField(_eventFilter);
            EditorGUILayout.EndHorizontal();

            // 事件类型过滤
            EditorGUILayout.BeginHorizontal();
            _showBroadcastEvents = EditorGUILayout.Toggle("广播事件", _showBroadcastEvents);
            _showCommandEvents = EditorGUILayout.Toggle("命令事件", _showCommandEvents);
            _showQueryEvents = EditorGUILayout.Toggle("查询事件", _showQueryEvents);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // 测试事件发送
            DrawTestEventSender();
        }

        private void DrawTestEventSender()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("测试事件发送", EditorStyles.boldLabel);

            var frameworkManager = FrameworkManager.Instance;
            if (frameworkManager != null && frameworkManager.IsInitialized)
            {
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("发送测试广播"))
                {
                    var testEvent = new TestBroadcastEvent { Message = "来自编辑器的测试消息", Timestamp = DateTime.Now };
                    frameworkManager.EventBus.Broadcast(testEvent);
                    LogEvent("TestBroadcastEvent", "Broadcast", testEvent.Message);
                }
                
                if (GUILayout.Button("发送测试命令"))
                {
                    var testCommand = new TestCommand { Action = "EditorTest", Value = UnityEngine.Random.Range(1, 100) };
                    frameworkManager.EventBus.SendCommand(testCommand);
                    LogEvent("TestCommand", "Command", $"Action: {testCommand.Action}, Value: {testCommand.Value}");
                }
                
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("框架未初始化，无法发送测试事件。", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawEventLog()
        {
            EditorGUILayout.LabelField("事件日志", EditorStyles.boldLabel);

            if (_eventLog.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无事件日志。启用事件日志记录或发送测试事件来查看日志。", MessageType.Info);
                return;
            }

            var filteredEvents = FilterEvents();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));

            foreach (var entry in filteredEvents.Take(100)) // 限制显示数量以提高性能
            {
                DrawEventLogEntry(entry);
            }

            EditorGUILayout.EndScrollView();

            if (filteredEvents.Count > 100)
            {
                EditorGUILayout.HelpBox($"显示前100条，共{filteredEvents.Count}条记录。", MessageType.Info);
            }
        }

        private List<EventLogEntry> FilterEvents()
        {
            var filtered = _eventLog.AsEnumerable();

            // 按事件名称过滤
            if (!string.IsNullOrEmpty(_eventFilter))
            {
                filtered = filtered.Where(e => e.EventName.ToLower().Contains(_eventFilter.ToLower()));
            }

            // 按事件类型过滤
            filtered = filtered.Where(e =>
                (e.Category == EventCategory.Broadcast && _showBroadcastEvents) ||
                (e.Category == EventCategory.Command && _showCommandEvents) ||
                (e.Category == EventCategory.Query && _showQueryEvents));

            return filtered.OrderByDescending(e => e.Timestamp).ToList();
        }

        private void DrawEventLogEntry(EventLogEntry entry)
        {
            EditorGUILayout.BeginHorizontal("box");

            // 时间戳
            EditorGUILayout.LabelField(entry.Timestamp.ToString("HH:mm:ss.fff"), GUILayout.Width(80));

            // 事件类型标识
            var categoryColor = GetCategoryColor(entry.Category);
            var originalColor = GUI.color;
            GUI.color = categoryColor;
            GUILayout.Label("●", GUILayout.Width(15));
            GUI.color = originalColor;

            // 事件名称
            EditorGUILayout.LabelField(entry.EventName, GUILayout.Width(150));

            // 事件数据
            EditorGUILayout.LabelField(entry.Data);

            EditorGUILayout.EndHorizontal();
        }

        private Color GetCategoryColor(EventCategory category)
        {
            switch (category)
            {
                case EventCategory.Broadcast:
                    return Color.blue;
                case EventCategory.Command:
                    return Color.green;
                case EventCategory.Query:
                    return Color.yellow;
                default:
                    return Color.white;
            }
        }

        private void LogEvent(string eventName, string eventType, string data)
        {
            if (!_enableEventLogging) return;

            var category = EventCategory.Broadcast;
            if (eventType == "Command") category = EventCategory.Command;
            else if (eventType == "Query") category = EventCategory.Query;

            var entry = new EventLogEntry
            {
                Timestamp = DateTime.Now,
                EventType = eventType,
                EventName = eventName,
                Data = data,
                Category = category
            };

            _eventLog.Add(entry);

            // 限制日志条目数量
            while (_eventLog.Count > _maxLogEntries)
            {
                _eventLog.RemoveAt(0);
            }
        }
    }

    // 测试事件类
    public class TestBroadcastEvent
    {
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class TestCommand
    {
        public string Action { get; set; }
        public int Value { get; set; }
    }
}