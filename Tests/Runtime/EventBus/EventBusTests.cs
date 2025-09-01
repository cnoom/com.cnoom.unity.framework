using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using CnoomFramework.Core;
using CnoomFramework.Core.EventBuss.Core;
using CnoomFramework.Core.EventBuss.Interfaces;

namespace CnoomFramework.Tests.EventBus
{
    /// <summary>
    /// 事件总线核心功能测试
    /// </summary>
    public class EventBusTests
    {
        private IEventBus _eventBus;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new CnoomFramework.Core.EventBuss.Core.EventBus();
        }

        [TearDown]
        public void TearDown()
        {
            _eventBus?.Clear();
            _eventBus = null;
        }

        #region 广播测试

        [Test]
        public void Broadcast_WithValidEvent_ShouldInvokeSubscriber()
        {
            // Arrange
            var testEvent = new TestEvent { Message = "Test Message" };
            var receivedEvent = default(TestEvent);
            var invoked = false;

            _eventBus.SubscribeBroadcast<TestEvent>(evt =>
            {
                receivedEvent = evt;
                invoked = true;
            });

            // Act
            _eventBus.Broadcast(testEvent);

            // Assert
            Assert.IsTrue(invoked, "事件处理器应该被调用");
            Assert.AreEqual(testEvent.Message, receivedEvent.Message, "接收到的事件数据应该匹配");
        }

        [Test]
        public void Broadcast_WithNullEvent_ShouldNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => _eventBus.Broadcast<TestEvent>(null), 
                "广播 null 事件不应该抛出异常");
        }

        [Test]
        public void SubscribeBroadcast_WithPriority_ShouldExecuteInOrder()
        {
            // Arrange
            var executionOrder = new System.Collections.Generic.List<int>();
            var testEvent = new TestEvent();

            _eventBus.SubscribeBroadcast<TestEvent>(evt => executionOrder.Add(3), priority: 3);
            _eventBus.SubscribeBroadcast<TestEvent>(evt => executionOrder.Add(1), priority: 1);
            _eventBus.SubscribeBroadcast<TestEvent>(evt => executionOrder.Add(2), priority: 2);

            // Act
            _eventBus.Broadcast(testEvent);

            // Assert
            Assert.AreEqual(3, executionOrder.Count, "所有处理器都应该被执行");
            Assert.AreEqual(1, executionOrder[0], "优先级 1 应该最先执行");
            Assert.AreEqual(2, executionOrder[1], "优先级 2 应该第二执行");
            Assert.AreEqual(3, executionOrder[2], "优先级 3 应该最后执行");
        }

        [Test]
        public void UnsubscribeBroadcast_ShouldRemoveHandler()
        {
            // Arrange
            var invoked = false;
            System.Action<TestEvent> handler = evt => invoked = true;

            _eventBus.SubscribeBroadcast(handler);
            _eventBus.UnsubscribeBroadcast(handler);

            // Act
            _eventBus.Broadcast(new TestEvent());

            // Assert
            Assert.IsFalse(invoked, "取消订阅后处理器不应该被调用");
        }

        #endregion

        #region 单播测试

        [Test]
        public void SendCommand_WithValidCommand_ShouldInvokeHandler()
        {
            // Arrange
            var testCommand = new TestCommand { Value = 42 };
            var receivedCommand = default(TestCommand);
            var invoked = false;

            _eventBus.RegisterCommandHandler<TestCommand>(cmd =>
            {
                receivedCommand = cmd;
                invoked = true;
            });

            // Act
            _eventBus.SendCommand(testCommand);

            // Assert
            Assert.IsTrue(invoked, "命令处理器应该被调用");
            Assert.AreEqual(testCommand.Value, receivedCommand.Value, "接收到的命令数据应该匹配");
        }

        [Test]
        public void RegisterCommandHandler_WithReplacement_ShouldReplaceHandler()
        {
            // Arrange
            var firstHandlerInvoked = false;
            var secondHandlerInvoked = false;

            _eventBus.RegisterCommandHandler<TestCommand>(cmd => firstHandlerInvoked = true);
            _eventBus.RegisterCommandHandler<TestCommand>(cmd => secondHandlerInvoked = true, replaceIfExists: true);

            // Act
            _eventBus.SendCommand(new TestCommand());

            // Assert
            Assert.IsFalse(firstHandlerInvoked, "第一个处理器应该被替换");
            Assert.IsTrue(secondHandlerInvoked, "第二个处理器应该被调用");
        }

        [Test]
        public void UnregisterCommandHandler_ShouldRemoveHandler()
        {
            // Arrange
            var invoked = false;
            _eventBus.RegisterCommandHandler<TestCommand>(cmd => invoked = true);
            _eventBus.UnregisterCommandHandler<TestCommand>();

            // Act
            _eventBus.SendCommand(new TestCommand());

            // Assert
            Assert.IsFalse(invoked, "取消注册后命令处理器不应该被调用");
        }

        #endregion

        #region 请求-响应测试

        [Test]
        public void Query_WithValidQuery_ShouldReturnResponse()
        {
            // Arrange
            var expectedResponse = new TestResponse { Result = "Success" };
            _eventBus.RegisterQueryHandler<TestQuery, TestResponse>(query => expectedResponse);

            // Act
            var actualResponse = _eventBus.Query<TestQuery, TestResponse>(new TestQuery());

            // Assert
            Assert.IsNotNull(actualResponse, "响应不应该为 null");
            Assert.AreEqual(expectedResponse.Result, actualResponse.Result, "响应数据应该匹配");
        }

        [Test]
        public void Query_WithoutHandler_ShouldReturnDefault()
        {
            // Act
            var response = _eventBus.Query<TestQuery, TestResponse>(new TestQuery());

            // Assert
            Assert.IsNull(response, "没有注册处理器时应该返回默认值");
        }

        [Test]
        public void UnregisterQueryHandler_ShouldRemoveHandler()
        {
            // Arrange
            _eventBus.RegisterQueryHandler<TestQuery, TestResponse>(query => new TestResponse());
            _eventBus.UnregisterQueryHandler<TestQuery, TestResponse>();

            // Act
            var response = _eventBus.Query<TestQuery, TestResponse>(new TestQuery());

            // Assert
            Assert.IsNull(response, "取消注册后查询处理器不应该被调用");
        }

        #endregion

        #region 性能测试

        [Test]
        public void Broadcast_PerformanceTest_ShouldHandleMultipleEvents()
        {
            // Arrange
            const int eventCount = 1000;
            var processedCount = 0;

            _eventBus.SubscribeBroadcast<TestEvent>(evt => processedCount++);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < eventCount; i++)
            {
                _eventBus.Broadcast(new TestEvent { Message = $"Event {i}" });
            }

            stopwatch.Stop();

            // Assert
            Assert.AreEqual(eventCount, processedCount, "所有事件都应该被处理");
            Assert.Less(stopwatch.ElapsedMilliseconds, 1000, "1000个事件处理应该在1秒内完成");
            
            Debug.Log($"处理 {eventCount} 个事件耗时: {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region 异步测试

        [UnityTest]
        public IEnumerator Broadcast_AsyncHandler_ShouldExecuteAsynchronously()
        {
            // Arrange
            var syncHandlerExecuted = false;
            var asyncHandlerExecuted = false;

            _eventBus.SubscribeBroadcast<TestEvent>(evt => syncHandlerExecuted = true, isAsync: false);
            _eventBus.SubscribeBroadcast<TestEvent>(evt => asyncHandlerExecuted = true, isAsync: true);

            // Act
            _eventBus.Broadcast(new TestEvent());

            // Assert
            Assert.IsTrue(syncHandlerExecuted, "同步处理器应该立即执行");
            
            // 等待异步处理器执行
            yield return new WaitForSeconds(0.1f);
            
            Assert.IsTrue(asyncHandlerExecuted, "异步处理器应该在等待后执行");
        }

        #endregion

        #region 错误处理测试

        [Test]
        public void Broadcast_WithExceptionInHandler_ShouldNotStopOtherHandlers()
        {
            // 预期错误日志 - 事件处理器中的异常会被记录
            LogAssert.Expect(LogType.Error, new Regex("Broadcast handling error: System.Reflection.TargetInvocationException.*Test exception.*"));
            
            // Arrange
            var firstHandlerExecuted = false;
            var thirdHandlerExecuted = false;

            _eventBus.SubscribeBroadcast<TestEvent>(evt => firstHandlerExecuted = true);
            _eventBus.SubscribeBroadcast<TestEvent>(evt => throw new System.Exception("Test exception"));
            _eventBus.SubscribeBroadcast<TestEvent>(evt => thirdHandlerExecuted = true);

            // Act & Assert
            Assert.DoesNotThrow(() => _eventBus.Broadcast(new TestEvent()), 
                "处理器中的异常不应该中断事件分发");
            
            Assert.IsTrue(firstHandlerExecuted, "第一个处理器应该执行");
            Assert.IsTrue(thirdHandlerExecuted, "第三个处理器应该执行");
        }

        #endregion
    }

    #region 测试数据类

    public class TestEvent
    {
        public string Message { get; set; }
    }

    public class TestCommand
    {
        public int Value { get; set; }
    }

    public class TestQuery
    {
        public string QueryType { get; set; }
    }

    public class TestResponse
    {
        public string Result { get; set; }
    }

    #endregion
}