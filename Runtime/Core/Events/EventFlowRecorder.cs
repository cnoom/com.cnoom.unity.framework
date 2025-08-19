using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CnoomFramework.Core.Events
{
    /// <summary>
    ///     事件流记录器，用于记录和分析事件流
    /// </summary>
    public class EventFlowRecorder
    {
        private static EventFlowRecorder _instance;

        /// <summary>
        ///     事件发生记录
        /// </summary>
        private readonly ConcurrentQueue<EventOccurrence> _eventOccurrences = new();

        /// <summary>
        ///     事件订阅者信息字典
        /// </summary>
        private readonly ConcurrentDictionary<Type, List<EventSubscriberInfo>> _eventSubscribers = new();

        /// <summary>
        ///     事件类型信息字典
        /// </summary>
        private readonly ConcurrentDictionary<Type, EventTypeInfo> _eventTypeInfos = new();

        /// <summary>
        ///     单例实例
        /// </summary>
        public static EventFlowRecorder Instance
        {
            get
            {
                if (_instance == null) _instance = new EventFlowRecorder();
                return _instance;
            }
        }

        /// <summary>
        ///     是否启用事件流记录
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///     最大记录事件数量
        /// </summary>
        public int MaxEventCount { get; set; } = 100;

        /// <summary>
        ///     记录事件发布
        /// </summary>
        public void RecordEventPublish(Type eventType, object eventData)
        {
            if (!Enabled) return;

            // 确保事件类型信息存在
            var eventTypeInfo = _eventTypeInfos.GetOrAdd(eventType, type => new EventTypeInfo(type));

            // 增加事件发生次数
            eventTypeInfo.TotalOccurrences++;
            eventTypeInfo.LastOccurrenceTime = DateTime.Now;

            // 记录事件发生
            var occurrence = new EventOccurrence(eventType, eventData, DateTime.Now);
            _eventOccurrences.Enqueue(occurrence);

            // 限制事件记录数量
            while (_eventOccurrences.Count > MaxEventCount) _eventOccurrences.TryDequeue(out _);
        }

        /// <summary>
        ///     记录事件处理
        /// </summary>
        public void RecordEventHandling(Type eventType, object handler, object target, string methodName)
        {
            if (!Enabled) return;

            // 查找最近的事件发生记录
            var recentOccurrence = _eventOccurrences
                .Where(o => o.EventType == eventType)
                .OrderByDescending(o => o.Timestamp)
                .FirstOrDefault();

            if (recentOccurrence != null)
            {
                var handlerName = target != null ? target.GetType().Name : handler.GetType().Name;
                recentOccurrence.AddHandler(handlerName, methodName);
            }
        }

        /// <summary>
        ///     记录事件订阅
        /// </summary>
        public void RecordEventSubscription(Type eventType, object handler, object target, string methodName)
        {
            if (!Enabled) return;

            // 确保事件类型信息存在
            _eventTypeInfos.GetOrAdd(eventType, type => new EventTypeInfo(type));

            // 添加订阅者信息
            if (!_eventSubscribers.TryGetValue(eventType, out var subscribers))
            {
                subscribers = new List<EventSubscriberInfo>();
                _eventSubscribers[eventType] = subscribers;
            }

            var subscriberName = target != null ? target.GetType().Name : handler.GetType().Name;
            var subscriberInfo = new EventSubscriberInfo(subscriberName, methodName, target);

            lock (subscribers)
            {
                // 避免重复添加
                if (!subscribers.Any(s => s.SubscriberName == subscriberName && s.MethodName == methodName))
                    subscribers.Add(subscriberInfo);
            }
        }

        /// <summary>
        ///     记录事件取消订阅
        /// </summary>
        public void RecordEventUnsubscription(Type eventType, object handler, object target)
        {
            if (!Enabled) return;

            if (_eventSubscribers.TryGetValue(eventType, out var subscribers))
            {
                var subscriberName = target != null ? target.GetType().Name : handler.GetType().Name;

                lock (subscribers)
                {
                    subscribers.RemoveAll(s => s.SubscriberName == subscriberName);
                }
            }
        }

        /// <summary>
        ///     获取所有事件类型信息
        /// </summary>
        public List<EventTypeInfo> GetAllEventTypeInfos()
        {
            return _eventTypeInfos.Values.ToList();
        }

        /// <summary>
        ///     获取事件订阅者信息
        /// </summary>
        public List<EventSubscriberInfo> GetEventSubscribers(Type eventType)
        {
            if (_eventSubscribers.TryGetValue(eventType, out var subscribers)) return subscribers.ToList();
            return new List<EventSubscriberInfo>();
        }

        /// <summary>
        ///     获取事件发生记录
        /// </summary>
        public List<EventOccurrence> GetEventOccurrences(Type eventType, int maxCount = 10)
        {
            return _eventOccurrences
                .Where(o => o.EventType == eventType)
                .OrderByDescending(o => o.Timestamp)
                .Take(maxCount)
                .ToList();
        }

        /// <summary>
        ///     获取所有事件发生记录
        /// </summary>
        public List<EventOccurrence> GetAllEventOccurrences(int maxCount = 100)
        {
            return _eventOccurrences
                .OrderByDescending(o => o.Timestamp)
                .Take(maxCount)
                .ToList();
        }

        /// <summary>
        ///     清除所有记录
        /// </summary>
        public void Clear()
        {
            _eventTypeInfos.Clear();
            _eventSubscribers.Clear();
            while (_eventOccurrences.TryDequeue(out _))
            {
            }
        }

        /// <summary>
        ///     事件类型信息
        /// </summary>
        public class EventTypeInfo
        {
            public EventTypeInfo(Type eventType)
            {
                EventType = eventType;
                EventTypeName = eventType.Name;
                TotalOccurrences = 0;
                LastOccurrenceTime = DateTime.MinValue;
            }

            /// <summary>
            ///     事件类型
            /// </summary>
            public Type EventType { get; }

            /// <summary>
            ///     事件类型名称
            /// </summary>
            public string EventTypeName { get; }

            /// <summary>
            ///     事件总发生次数
            /// </summary>
            public int TotalOccurrences { get; set; }

            /// <summary>
            ///     最后发生时间
            /// </summary>
            public DateTime LastOccurrenceTime { get; set; }
        }

        /// <summary>
        ///     事件订阅者信息
        /// </summary>
        public class EventSubscriberInfo
        {
            public EventSubscriberInfo(string subscriberName, string methodName, object target)
            {
                SubscriberName = subscriberName;
                MethodName = methodName;
                Target = target;
            }

            /// <summary>
            ///     订阅者名称
            /// </summary>
            public string SubscriberName { get; }

            /// <summary>
            ///     方法名称
            /// </summary>
            public string MethodName { get; }

            /// <summary>
            ///     目标对象
            /// </summary>
            public object Target { get; }

            /// <summary>
            ///     是否活跃
            /// </summary>
            public bool IsActive
            {
                get
                {
                    if (Target is IModule module)
                        return module.State == ModuleState.Started || module.State == ModuleState.Initialized;
                    return Target != null;
                }
            }
        }

        /// <summary>
        ///     事件发生记录
        /// </summary>
        public class EventOccurrence
        {
            public EventOccurrence(Type eventType, object eventData, DateTime timestamp)
            {
                EventType = eventType;
                EventData = eventData;
                Timestamp = timestamp;
            }

            /// <summary>
            ///     事件类型
            /// </summary>
            public Type EventType { get; }

            /// <summary>
            ///     事件数据
            /// </summary>
            public object EventData { get; }

            /// <summary>
            ///     发生时间
            /// </summary>
            public DateTime Timestamp { get; }

            /// <summary>
            ///     处理者列表
            /// </summary>
            public List<HandlerInfo> Handlers { get; } = new();

            /// <summary>
            ///     添加处理者
            /// </summary>
            public void AddHandler(string handlerName, string methodName)
            {
                lock (Handlers)
                {
                    Handlers.Add(new HandlerInfo(handlerName, methodName, DateTime.Now));
                }
            }

            /// <summary>
            ///     处理者信息
            /// </summary>
            public class HandlerInfo
            {
                public HandlerInfo(string handlerName, string methodName, DateTime processTime)
                {
                    HandlerName = handlerName;
                    MethodName = methodName;
                    ProcessTime = processTime;
                }

                /// <summary>
                ///     处理者名称
                /// </summary>
                public string HandlerName { get; }

                /// <summary>
                ///     方法名称
                /// </summary>
                public string MethodName { get; }

                /// <summary>
                ///     处理时间
                /// </summary>
                public DateTime ProcessTime { get; }
            }
        }
    }
}