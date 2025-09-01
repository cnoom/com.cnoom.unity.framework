using System;
using NUnit.Framework;
using CnoomFramework.Core.Config;
using CnoomFramework.Core.Config.Sources;
using CnoomFramework.Core.EventBuss.Interfaces;
using UnityEngine;

namespace CnoomFramework.Tests.Config
{
    /// <summary>
    /// 简化的配置管理测试类，用于排查测试环境问题
    /// </summary>
    public class SimpleConfigTests
    {
        private ConfigManager _configManager;
        private int _testCounter = 0;

        [SetUp]
        public void SetUp()
        {
            _testCounter++;
            Debug.Log($"[SimpleConfigTests] 开始第{_testCounter}次SetUp");
            
            try
            {
                var mockEventBus = new SimpleMockEventBus();
                _configManager = new ConfigManager(mockEventBus);
                _configManager.AddConfigSource(new MemoryConfigSource());
                
                Debug.Log($"[SimpleConfigTests] 第{_testCounter}次SetUp完成");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SimpleConfigTests] 第{_testCounter}次SetUp失败: {ex.Message}");
                throw;
            }
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Log($"[SimpleConfigTests] 开始第{_testCounter}次TearDown");
            
            try
            {
                _configManager?.Clear();
                _configManager = null;
                
                Debug.Log($"[SimpleConfigTests] 第{_testCounter}次TearDown完成");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SimpleConfigTests] 第{_testCounter}次TearDown失败: {ex.Message}");
                throw;
            }
        }

        [Test]
        [Timeout(3000)] // 3秒超时保护
        public void SimpleTest1_ShouldPass()
        {
            Debug.Log("[SimpleConfigTests] 开始 SimpleTest1_ShouldPass 测试");
            
            try
            {
                _configManager.SetValue("test.key", "test.value");
                var result = _configManager.GetValue<string>("test.key");
                Assert.AreEqual("test.value", result, "值应该相等");
                
                Debug.Log("[SimpleConfigTests] SimpleTest1_ShouldPass 测试完成");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SimpleConfigTests] SimpleTest1_ShouldPass 测试失败: {ex.Message}");
                throw;
            }
        }

        [Test]
        [Timeout(3000)] // 3秒超时保护
        public void SimpleTest2_ShouldPass()
        {
            Debug.Log("[SimpleConfigTests] 开始 SimpleTest2_ShouldPass 测试");
            
            try
            {
                var result = _configManager.GetValue<string>("non.existent.key");
                Assert.IsNull(result, "不存在的键应该返回null");
                
                Debug.Log("[SimpleConfigTests] SimpleTest2_ShouldPass 测试完成");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SimpleConfigTests] SimpleTest2_ShouldPass 测试失败: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// 极简的Mock EventBus实现
    /// </summary>
    public class SimpleMockEventBus : IEventBus
    {
        public void Broadcast<T>(T eventData) where T : notnull
        {
            // 极简实现，不做任何操作
        }

        public void SubscribeBroadcast<T>(System.Action<T> handler, int priority = 1, bool isAsync = false) where T : notnull
        {
            // 极简实现，不做任何操作
        }

        public void UnsubscribeBroadcast<T>(System.Action<T> handler) where T : notnull
        {
            // 极简实现，不做任何操作
        }

        public void SendCommand<T>(T command) where T : notnull
        {
            // 极简实现，不做任何操作
        }

        public TResponse Query<TQuery, TResponse>(TQuery query) where TQuery : notnull
        {
            // 极简实现，返回默认值
            return default(TResponse);
        }

        public void RegisterCommandHandler<T>(System.Action<T> handler, bool replaceIfExists = true) where T : notnull
        {
            // 极简实现，不做任何操作
        }

        public void RegisterQueryHandler<TQuery, TResponse>(System.Func<TQuery, TResponse> handler) where TQuery : notnull
        {
            // 极简实现，不做任何操作
        }

        public void UnregisterCommandHandler<T>() where T : notnull
        {
            // 极简实现，不做任何操作
        }

        public void UnregisterQueryHandler<TQuery, TResponse>() where TQuery : notnull
        {
            // 极简实现，不做任何操作
        }

        public void Clear()
        {
            // 极简实现，不做任何操作
        }
    }
}