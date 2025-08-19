using System;
using System.Collections.Generic;
using System.Linq;
using CnoomFramework.Core;
using CnoomFramework.Core.Events;
using CnoomFramework.Editor.Editor;
using UnityEditor;
using UnityEngine;

namespace CnoomFramework.Editor
{
    /// <summary>
    /// 事件流可视化窗口
    /// </summary>
    public class EventFlowVisualizerWindow : EditorWindow
    {
        private const string MenuPath = FrameworkEditorConfig.MenuPath + "事件流程可视化器";

        private Vector2 _scrollPosition;
        private bool _isFrameworkInitialized = false;
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _boldLabelStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _eventNodeStyle;
        private GUIStyle _subscriberNodeStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _searchFieldStyle;
        private GUIStyle _searchCancelButtonStyle;
        private GUIStyle _eventTitle;
        private string _searchText = "";
        private bool _autoRefresh = true;
        private float _refreshInterval = 1.0f;
        private float _lastRefreshTime;
        private bool _showInactiveEvents = false;
        private bool _showEventDetails = true;
        private bool _showSubscriberDetails = true;
        private bool _showEventStatistics = true;
        private bool _showEventTimeline = true;
        private bool _groupByModule = false;
        private bool _showEventFlowGraph = false;
        private string _selectedEventType = "";
        private int _maxOccurrencesToShow = 10;
        
        // 事件流数据
        private List<EventFlowData> _eventFlowData = new List<EventFlowData>();
        private Dictionary<string, bool> _eventFoldouts = new Dictionary<string, bool>();
        private Dictionary<string, Color> _eventTypeColors = new Dictionary<string, Color>();
        private int _selectedEventIndex = -1;
        
        // 事件流数据类
        [Serializable]
        private class EventFlowData
        {
            public string EventType;
            public List<EventSubscriberData> Subscribers = new List<EventSubscriberData>();
            public List<EventOccurrenceData> RecentOccurrences = new List<EventOccurrenceData>();
            public int TotalOccurrences;
            public DateTime LastOccurrence;
            public DateTime FirstOccurrence;
            public float AverageTimeBetweenOccurrences;
            public List<float> RecentDurations = new List<float>();
        }
        
        [Serializable]
        private class EventSubscriberData
        {
            public string SubscriberName;
            public string MethodName;
            public bool IsActive;
        }
        
        [Serializable]
        private class EventOccurrenceData
        {
            public DateTime Timestamp;
            public object EventData;
            public List<string> ProcessedBy = new List<string>();
            public float Duration;
            public bool IsAsync;
        }

        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            var window = GetWindow<EventFlowVisualizerWindow>("Event Flow Visualizer");
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
            if (Application.isPlaying)
            {
                var frameworkManager = FrameworkManager.Instance;
                if (frameworkManager != null && frameworkManager.IsInitialized)
                {
                    if (!_isFrameworkInitialized)
                    {
                        _isFrameworkInitialized = true;
                        SubscribeToFrameworkEvents();
                        RefreshEventFlowData();
                    }
                    
                    // 自动刷新
                    if (_autoRefresh && Time.realtimeSinceStartup - _lastRefreshTime > _refreshInterval)
                    {
                        RefreshEventFlowData();
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

        private void SubscribeToFrameworkEvents()
        {
            // 这里可以订阅一些框架事件来更新事件流数据
            var eventBus = FrameworkManager.Instance.EventBus;
            
            // 使用反射获取EventBus的私有字段和方法，以便我们可以获取订阅信息
            var eventBusType = eventBus.GetType();
            
            // 订阅框架事件
            eventBus.Subscribe<ModuleRegisteredEvent>(OnModuleRegistered);
            eventBus.Subscribe<ModuleUnregisteredEvent>(OnModuleUnregistered);
            eventBus.Subscribe<ModuleStateChangedEvent>(OnModuleStateChanged);
        }
        
        private void OnModuleRegistered(ModuleRegisteredEvent evt)
        {
            RefreshEventFlowData();
        }
        
        private void OnModuleUnregistered(ModuleUnregisteredEvent evt)
        {
            RefreshEventFlowData();
        }
        
        private void OnModuleStateChanged(ModuleStateChangedEvent evt)
        {
            RefreshEventFlowData();
        }

        private void RefreshEventFlowData()
        {
            if (!_isFrameworkInitialized) return;
            
            // 使用EventFlowRecorder获取事件流数据
            var recorder = EventFlowRecorder.Instance;
            
            // 清除旧数据，但保留事件折叠状态
            _eventFlowData.Clear();
            
            // 获取所有事件类型信息
            var eventTypeInfos = recorder.GetAllEventTypeInfos();
            
            // 为每种事件类型分配一个颜色
            foreach (var eventTypeInfo in eventTypeInfos)
            {
                var eventTypeName = eventTypeInfo.EventTypeName;
                if (!_eventTypeColors.ContainsKey(eventTypeName))
                {
                    _eventTypeColors[eventTypeName] = GetRandomColor();
                }
            }
            
            // 遍历所有事件类型和订阅者
            foreach (var eventTypeInfo in eventTypeInfos)
            {
                var eventType = eventTypeInfo.EventType;
                var eventTypeName = eventTypeInfo.EventTypeName;
                
                var eventData = new EventFlowData
                {
                    EventType = eventTypeName,
                    TotalOccurrences = eventTypeInfo.TotalOccurrences,
                    LastOccurrence = eventTypeInfo.LastOccurrenceTime
                };
                
                // 获取订阅者信息
                var subscribers = recorder.GetEventSubscribers(eventType);
                foreach (var subscriber in subscribers)
                {
                    eventData.Subscribers.Add(new EventSubscriberData
                    {
                        SubscriberName = subscriber.SubscriberName,
                        MethodName = subscriber.MethodName,
                        IsActive = subscriber.IsActive
                    });
                }
                
                // 获取最近事件发生记录
                var occurrences = recorder.GetEventOccurrences(eventType, 5);
                foreach (var occurrence in occurrences)
                {
                    var occurrenceData = new EventOccurrenceData
                    {
                        Timestamp = occurrence.Timestamp,
                        EventData = occurrence.EventData
                    };
                    
                    // 添加处理者信息
                    foreach (var handler in occurrence.Handlers)
                    {
                        occurrenceData.ProcessedBy.Add($"{handler.HandlerName}.{handler.MethodName}");
                    }
                    
                    eventData.RecentOccurrences.Add(occurrenceData);
                }
                
                _eventFlowData.Add(eventData);
            }
            
            // 按事件类型名称排序
            _eventFlowData = _eventFlowData.OrderBy(e => e.EventType).ToList();
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
                EditorGUILayout.HelpBox("框架未初始化，请在运行时查看事件流信息。", MessageType.Info);
            }
            else
            {
                DrawEventFlowVisualization();
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

            if (_eventTitle == null)
            {
                _eventTitle = new GUIStyle(_boldLabelStyle)
                {
                    clipping = TextClipping.Overflow 
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
            
            if (_eventNodeStyle == null)
            {
                _eventNodeStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    margin = new RectOffset(5, 5, 5, 5),
                    padding = new RectOffset(10, 10, 10, 10),
                    normal = { background = EditorGUIUtility.Load("CN EntryBackEven") as Texture2D }
                };
            }
            
            if (_subscriberNodeStyle == null)
            {
                _subscriberNodeStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    margin = new RectOffset(20, 5, 2, 2),
                    padding = new RectOffset(10, 10, 5, 5),
                    normal = { background = EditorGUIUtility.Load("CN EntryBackOdd") as Texture2D }
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
                RefreshEventFlowData();
            }
            
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "自动刷新", EditorStyles.toolbarButton);
            
            if (_autoRefresh)
            {
                GUILayout.Label("刷新间隔:", GUILayout.Width(60));
                _refreshInterval = EditorGUILayout.Slider(_refreshInterval, 0.1f, 5.0f, GUILayout.Width(100));
            }
            
            _showInactiveEvents = GUILayout.Toggle(_showInactiveEvents, "显示非活跃事件", EditorStyles.toolbarButton);
            _showEventDetails = GUILayout.Toggle(_showEventDetails, "显示事件详情", EditorStyles.toolbarButton);
            _showSubscriberDetails = GUILayout.Toggle(_showSubscriberDetails, "显示订阅者详情", EditorStyles.toolbarButton);
            _showEventStatistics = GUILayout.Toggle(_showEventStatistics, "显示统计信息", EditorStyles.toolbarButton);
            _showEventTimeline = GUILayout.Toggle(_showEventTimeline, "显示时间线", EditorStyles.toolbarButton);
            _groupByModule = GUILayout.Toggle(_groupByModule, "按模块分组", EditorStyles.toolbarButton);
            _showEventFlowGraph = GUILayout.Toggle(_showEventFlowGraph, "显示流程图", EditorStyles.toolbarButton);
            
            GUILayout.FlexibleSpace();
            
            _searchText = EditorGUILayout.TextField(_searchText, _searchFieldStyle);
            
            if (GUILayout.Button("×", _searchCancelButtonStyle) && !string.IsNullOrEmpty(_searchText))
            {
                _searchText = "";
                GUI.FocusControl(null);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEventFlowVisualization()
        {
            EditorGUILayout.BeginVertical();
            
            GUILayout.Label("事件流可视化", _headerStyle);
            
            if (_eventFlowData.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无事件数据。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // 显示总体统计信息
            if (_showEventStatistics)
            {
                DrawOverallStatistics();
            }

            // 显示事件流程图
            if (_showEventFlowGraph)
            {
                DrawEventFlowGraph();
            }
            
            // 过滤事件
            var filteredEvents = _eventFlowData;
            if (!string.IsNullOrEmpty(_searchText))
            {
                filteredEvents = _eventFlowData
                    .Where(e => e.EventType.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                                e.Subscribers.Any(s => s.SubscriberName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                                                      s.MethodName.Contains(_searchText, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            // 按模块分组显示
            if (_groupByModule)
            {
                DrawEventsGroupedByModule(filteredEvents);
            }
            else
            {
                // 绘制事件节点
                foreach (var eventData in filteredEvents)
                {
                    DrawEventNode(eventData);
                }
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawEventNode(EventFlowData eventData)
        {
            // 确保事件折叠状态字典中有此事件类型
            if (!_eventFoldouts.ContainsKey(eventData.EventType))
            {
                _eventFoldouts[eventData.EventType] = false;
            }
            
            // 获取事件颜色
            Color eventColor = _eventTypeColors.ContainsKey(eventData.EventType) 
                ? _eventTypeColors[eventData.EventType] 
                : Color.white;
            
            // 绘制事件节点
            EditorGUILayout.BeginVertical(_eventNodeStyle);
            
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            
            // 绘制颜色标记
            GUI.color = eventColor;
            GUILayout.Box("", GUILayout.Width(16), GUILayout.Height(16));
            GUI.color = Color.white;
            
            // 事件类型和折叠控制
            GUIContent content = new GUIContent($"{eventData.EventType} ({eventData.Subscribers.Count} 订阅者)");
            _eventFoldouts[eventData.EventType] = EditorGUILayout.Foldout(_eventFoldouts[eventData.EventType], 
                content, true, _eventTitle);
            
            // 事件统计信息
            if (eventData.TotalOccurrences > 0)
            {
                GUILayout.Label($"触发次数: {eventData.TotalOccurrences}", GUILayout.Width(100));
                GUILayout.Label($"最近: {eventData.LastOccurrence.ToString("HH:mm:ss")}", GUILayout.Width(100));
                
                if (_showEventStatistics && eventData.TotalOccurrences > 1)
                {
                    var timeSpan = eventData.LastOccurrence - eventData.FirstOccurrence;
                    var avgInterval = timeSpan.TotalSeconds / (eventData.TotalOccurrences - 1);
                    GUILayout.Label($"平均间隔: {avgInterval:F1}s", GUILayout.Width(120));
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 如果展开，显示订阅者
            if (_eventFoldouts[eventData.EventType])
            {
                // 事件详情
                if (_showEventDetails)
                {
                    EditorGUILayout.BeginVertical(_boxStyle);
                    GUILayout.Label("事件详情", _subHeaderStyle);
                    
                    EditorGUILayout.LabelField("完整类型名称:", eventData.EventType);
                    
                    // 这里可以显示更多事件类型的详细信息，如事件参数类型等
                    
                    EditorGUILayout.EndVertical();
                    
                    EditorGUILayout.Space();
                }
                
                // 订阅者列表
                GUILayout.Label("订阅者", _subHeaderStyle);
                
                if (eventData.Subscribers.Count == 0)
                {
                    EditorGUILayout.HelpBox("此事件没有订阅者。", MessageType.Info);
                }
                else
                {
                    foreach (var subscriber in eventData.Subscribers)
                    {
                        DrawSubscriberNode(subscriber);
                    }
                }
                
                // 最近事件发生记录
                if (eventData.RecentOccurrences.Count > 0)
                {
                    EditorGUILayout.Space();
                    GUILayout.Label("最近事件记录", _subHeaderStyle);
                    
                    foreach (var occurrence in eventData.RecentOccurrences)
                    {
                        EditorGUILayout.BeginVertical(_boxStyle);
                        
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(occurrence.Timestamp.ToString("HH:mm:ss.fff"), GUILayout.Width(100));
                        
                        if (occurrence.EventData != null)
                        {
                            string eventDataString = "无数据";
                            
                            // 尝试获取更友好的事件数据显示
                            if (occurrence.EventData is ModuleRegisteredEvent moduleRegEvent)
                            {
                                eventDataString = $"模块注册: {moduleRegEvent.ModuleName}";
                            }
                            else if (occurrence.EventData is ModuleUnregisteredEvent moduleUnregEvent)
                            {
                                eventDataString = $"模块注销: {moduleUnregEvent.ModuleName}";
                            }
                            else if (occurrence.EventData is ModuleStateChangedEvent moduleStateEvent)
                            {
                                eventDataString = $"模块状态变更: {moduleStateEvent.ModuleName}, 状态: {moduleStateEvent.NewState}";
                            }
                            else if (occurrence.EventData is ConfigChangedEvent configEvent)
                            {
                                eventDataString = $"配置变更: {configEvent.Key} = {configEvent.NewValue}";
                            }
                            else
                            {
                                eventDataString = occurrence.EventData.ToString();
                            }
                            
                            EditorGUILayout.LabelField(eventDataString);
                        }
                        else
                        {
                            EditorGUILayout.LabelField("无数据");
                        }
                        
                        EditorGUILayout.EndHorizontal();
                        
                        if (occurrence.ProcessedBy.Count > 0)
                        {
                            EditorGUILayout.LabelField("处理者:", _boldLabelStyle);
                            foreach (var processor in occurrence.ProcessedBy)
                            {
                                EditorGUILayout.LabelField("- " + processor);
                            }
                        }
                        
                        EditorGUILayout.EndVertical();
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
        }

        private void DrawOverallStatistics()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("总体统计", _subHeaderStyle);

            int totalEvents = _eventFlowData.Count;
            int totalSubscribers = _eventFlowData.Sum(e => e.Subscribers.Count);
            int totalOccurrences = _eventFlowData.Sum(e => e.TotalOccurrences);
            int activeSubscribers = _eventFlowData.Sum(e => e.Subscribers.Count(s => s.IsActive));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("事件类型总数:", totalEvents.ToString());
            EditorGUILayout.LabelField("订阅者总数:", totalSubscribers.ToString());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("活跃订阅者:", activeSubscribers.ToString());
            EditorGUILayout.LabelField("总触发次数:", totalOccurrences.ToString());
            EditorGUILayout.EndHorizontal();

            // 最活跃的事件
            var mostActiveEvent = _eventFlowData.OrderByDescending(e => e.TotalOccurrences).FirstOrDefault();
            if (mostActiveEvent != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("最活跃事件:", mostActiveEvent.EventType);
                EditorGUILayout.LabelField("触发次数:", mostActiveEvent.TotalOccurrences.ToString());
                EditorGUILayout.EndHorizontal();
            }

            // 最多订阅者的事件
            var mostSubscribedEvent = _eventFlowData.OrderByDescending(e => e.Subscribers.Count).FirstOrDefault();
            if (mostSubscribedEvent != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("最多订阅者:", mostSubscribedEvent.EventType);
                EditorGUILayout.LabelField("订阅者数:", mostSubscribedEvent.Subscribers.Count.ToString());
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawEventFlowGraph()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("事件流程图", _subHeaderStyle);

            // 简单的流程图显示
            EditorGUILayout.BeginHorizontal();

            // 事件发布者
            EditorGUILayout.BeginVertical(GUILayout.Width(150));
            GUILayout.Label("发布者", _boldLabelStyle);
            
            var publishers = _eventFlowData
                .SelectMany(e => e.RecentOccurrences)
                .Select(o => GetEventPublisher(o.EventData))
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct()
                .Take(5);

            foreach (var publisher in publishers)
            {
                EditorGUILayout.LabelField(publisher);
            }
            EditorGUILayout.EndVertical();

            // 箭头
            EditorGUILayout.BeginVertical(GUILayout.Width(50));
            GUILayout.Label("→", _boldLabelStyle);
            EditorGUILayout.EndVertical();

            // 事件类型
            EditorGUILayout.BeginVertical(GUILayout.Width(150));
            GUILayout.Label("事件类型", _boldLabelStyle);
            
            var eventTypes = _eventFlowData
                .OrderByDescending(e => e.TotalOccurrences)
                .Take(5)
                .Select(e => e.EventType);

            foreach (var eventType in eventTypes)
            {
                EditorGUILayout.LabelField(eventType);
            }
            EditorGUILayout.EndVertical();

            // 箭头
            EditorGUILayout.BeginVertical(GUILayout.Width(50));
            GUILayout.Label("→", _boldLabelStyle);
            EditorGUILayout.EndVertical();

            // 订阅者
            EditorGUILayout.BeginVertical(GUILayout.Width(150));
            GUILayout.Label("订阅者", _boldLabelStyle);
            
            var subscribers = _eventFlowData
                .SelectMany(e => e.Subscribers)
                .Where(s => s.IsActive)
                .Select(s => s.SubscriberName)
                .Distinct()
                .Take(5);

            foreach (var subscriber in subscribers)
            {
                EditorGUILayout.LabelField(subscriber);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawEventsGroupedByModule(List<EventFlowData> events)
        {
            // 按模块分组事件
            var moduleGroups = events
                .SelectMany(e => e.Subscribers.Select(s => new { Event = e, Subscriber = s }))
                .GroupBy(x => GetModuleNameFromSubscriber(x.Subscriber.SubscriberName))
                .OrderBy(g => g.Key);

            foreach (var moduleGroup in moduleGroups)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                GUILayout.Label($"模块: {moduleGroup.Key}", _subHeaderStyle);

                var moduleEvents = moduleGroup
                    .Select(x => x.Event)
                    .Distinct()
                    .OrderBy(e => e.EventType);

                foreach (var eventData in moduleEvents)
                {
                    DrawEventNode(eventData);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        private void DrawSubscriberNode(EventSubscriberData subscriber)
        {
            EditorGUILayout.BeginVertical(_subscriberNodeStyle);
            
            EditorGUILayout.BeginHorizontal();
            
            // 活跃状态指示器
            if (subscriber.IsActive)
            {
                GUI.color = Color.green;
                GUILayout.Box("●", GUILayout.Width(20));
            }
            else
            {
                GUI.color = Color.gray;
                GUILayout.Box("●", GUILayout.Width(20));
            }
            GUI.color = Color.white;
            
            // 订阅者名称
            EditorGUILayout.LabelField(subscriber.SubscriberName, _boldLabelStyle);
            
            // 方法名称
            if (_showSubscriberDetails)
            {
                EditorGUILayout.LabelField("方法: " + subscriber.MethodName);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 订阅者详情
            if (_showSubscriberDetails)
            {
                // 这里可以显示更多订阅者的详细信息
            }
            
            EditorGUILayout.EndVertical();
        }

        private string GetEventPublisher(object eventData)
        {
            if (eventData == null) return "未知";

            // 根据事件类型推断发布者
            if (eventData is ModuleRegisteredEvent || 
                eventData is ModuleUnregisteredEvent || 
                eventData is ModuleStateChangedEvent)
            {
                return "FrameworkManager";
            }
            else if (eventData is ConfigChangedEvent || 
                     eventData is ConfigSavedEvent || 
                     eventData is ConfigLoadedEvent)
            {
                return "ConfigManager";
            }
            else if (eventData is PerformanceDataUpdatedEvent || 
                     eventData is PerformanceMonitorStatusChangedEvent)
            {
                return "PerformanceMonitor";
            }

            return "其他";
        }

        private string GetModuleNameFromSubscriber(string subscriberName)
        {
            if (string.IsNullOrEmpty(subscriberName)) return "未知模块";

            // 尝试从订阅者名称中提取模块名
            if (subscriberName.Contains("Module"))
            {
                var parts = subscriberName.Split('.');
                foreach (var part in parts)
                {
                    if (part.Contains("Module"))
                    {
                        return part;
                    }
                }
            }

            return subscriberName;
        }
    }
}