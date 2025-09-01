using System;
using NUnit.Framework;
using CnoomFramework.Core.Config;
using CnoomFramework.Core.Config.Sources;
using CnoomFramework.Core.EventBuss.Interfaces;
using UnityEngine;

namespace CnoomFramework.Tests.Config
{
    /// <summary>
    /// 配置管理系统测试
    /// </summary>
    public class ConfigManagerTests
    {
        private ConfigManager _configManager;
        private IConfigSource _testConfigSource;
        private int _testCounter = 0; // 非静态测试计数器

        [SetUp]
        public void SetUp()
        {
            _testCounter++;
            
            try
            {
                _testConfigSource = new MemoryConfigSource();
                var mockEventBus = new MockEventBus();
                _configManager = new ConfigManager(mockEventBus);
                _configManager.AddConfigSource(_testConfigSource);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ConfigManagerTests] 第{_testCounter}次SetUp失败: {ex.Message}");
                throw;
            }
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                _configManager?.Clear();
                _configManager = null;
                _testConfigSource = null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ConfigManagerTests] 第{_testCounter}次TearDown失败: {ex.Message}");
                throw;
            }
        }

        #region 基本读写测试

        [Test]
        [Timeout(5000)] // 5秒超时保护
        public void SetValue_WithValidKeyValue_ShouldStoreValue()
        {
            try
            {
                // Arrange
                const string key = "test.string";
                const string value = "test value";

                // Act
                _configManager.SetValue(key, value);

                // Assert
                var retrievedValue = _configManager.GetValue<string>(key);
                Assert.AreEqual(value, retrievedValue, "应该能够正确存储和获取字符串值");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ConfigManagerTests] SetValue_WithValidKeyValue_ShouldStoreValue 测试失败: {ex.Message}");
                throw;
            }
        }

        [Test]
        [Timeout(5000)] // 5秒超时保护
        public void GetValue_WithNonExistentKey_ShouldReturnDefault()
        {
            try
            {
                // Act
                var result = _configManager.GetValue<string>("non.existent.key");

                // Assert
                Assert.IsNull(result, "不存在的键应该返回默认值");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ConfigManagerTests] GetValue_WithNonExistentKey_ShouldReturnDefault 测试失败: {ex.Message}");
                throw;
            }
        }

        [Test]
        public void GetValue_WithDefaultValue_ShouldReturnDefault()
        {
            // Arrange
            const string defaultValue = "default";

            // Act
            var result = _configManager.GetValue("non.existent.key", defaultValue);

            // Assert
            Assert.AreEqual(defaultValue, result, "应该返回指定的默认值");
        }

        [Test]
        public void HasValue_WithExistingKey_ShouldReturnTrue()
        {
            // Arrange
            const string key = "test.key";
            _configManager.SetValue(key, "value");

            // Act
            var exists = _configManager.HasValue(key);

            // Assert
            Assert.IsTrue(exists, "存在的键应该返回 true");
        }

        [Test]
        public void HasValue_WithNonExistentKey_ShouldReturnFalse()
        {
            // Act
            var exists = _configManager.HasValue("non.existent.key");

            // Assert
            Assert.IsFalse(exists, "不存在的键应该返回 false");
        }

        #endregion

        #region 数据类型测试

        [Test]
        public void ConfigManager_ShouldSupportIntValues()
        {
            // Arrange
            const string key = "test.int";
            const int value = 42;

            // Act
            _configManager.SetValue(key, value);
            var retrievedValue = _configManager.GetValue<int>(key);

            // Assert
            Assert.AreEqual(value, retrievedValue, "应该正确处理整数类型");
        }

        [Test]
        public void ConfigManager_ShouldSupportFloatValues()
        {
            // Arrange
            const string key = "test.float";
            const float value = 3.14f;

            // Act
            _configManager.SetValue(key, value);
            var retrievedValue = _configManager.GetValue<float>(key);

            // Assert
            Assert.AreEqual(value, retrievedValue, 0.001f, "应该正确处理浮点数类型");
        }

        [Test]
        public void ConfigManager_ShouldSupportBoolValues()
        {
            // Arrange
            const string key = "test.bool";
            const bool value = true;

            // Act
            _configManager.SetValue(key, value);
            var retrievedValue = _configManager.GetValue<bool>(key);

            // Assert
            Assert.AreEqual(value, retrievedValue, "应该正确处理布尔类型");
        }

        [Test]
        public void ConfigManager_ShouldSupportComplexObjects()
        {
            // Arrange
            const string key = "test.object";
            var value = new TestConfigObject { Name = "Test", Value = 123 };

            // Act
            _configManager.SetValue(key, value);
            var retrievedValue = _configManager.GetValue<TestConfigObject>(key);

            // Assert
            Assert.IsNotNull(retrievedValue, "应该能够存储复杂对象");
            Assert.AreEqual(value.Name, retrievedValue.Name, "对象属性应该正确保存");
            Assert.AreEqual(value.Value, retrievedValue.Value, "对象属性应该正确保存");
        }

        #endregion

        #region 多配置源测试

        [Test]
        public void MultipleConfigSources_ShouldUseCorrectPriority()
        {
            // Arrange
            var lowPrioritySource = new MemoryConfigSource(priority: 1);
            var highPrioritySource = new MemoryConfigSource(priority: 2);

            _configManager.AddConfigSource(lowPrioritySource);
            _configManager.AddConfigSource(highPrioritySource);

            const string key = "priority.test";
            lowPrioritySource.SetValue(key, "low");
            highPrioritySource.SetValue(key, "high");

            // Act
            var result = _configManager.GetValue<string>(key);

            // Assert
            Assert.AreEqual("high", result, "应该返回高优先级配置源的值");
        }

        [Test]
        public void RemoveConfigSource_ShouldFallbackToNextSource()
        {
            // Arrange
            var source1 = new MockConfigSource { Priority = 2, Name = "Source1" }; // 高优先级
            var source2 = new MockConfigSource { Priority = 1, Name = "Source2" }; // 低优先级

            _configManager.AddConfigSource(source1);
            _configManager.AddConfigSource(source2);

            const string key = "fallback.test";
            source1.SetValue(key, "source1");
            source2.SetValue(key, "source2");

            // Act
            _configManager.RemoveConfigSource(source1.Name);
            var result = _configManager.GetValue<string>(key);

            // Assert
            Assert.AreEqual("source2", result, "移除高优先级源后应该回退到低优先级源");
        }

        #endregion

        #region 持久化测试

        [Test]
        public void SetValue_WithPersistence_ShouldCallSave()
        {
            // Arrange
            var mockSource = new MockConfigSource();
            _configManager.AddConfigSource(mockSource);

            // Act
            _configManager.SetValue("persist.test", "value", persistent: true);

            // Assert
            Assert.IsTrue(mockSource.SaveCalled, "启用持久化时应该调用 Save");
        }

        [Test]
        public void SetValue_WithoutPersistence_ShouldNotCallSave()
        {
            // Arrange
            var mockSource = new MockConfigSource();
            _configManager.AddConfigSource(mockSource);

            // Act
            _configManager.SetValue("temp.test", "value", persistent: false);

            // Assert
            Assert.IsFalse(mockSource.SaveCalled, "禁用持久化时不应该调用 Save");
        }

        #endregion

        #region 异常处理测试

        [Test]
        public void SetValue_WithNullKey_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _configManager.SetValue(null, "value"),
                "null 键应该抛出异常");
        }

        [Test]
        public void SetValue_WithEmptyKey_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _configManager.SetValue("", "value"),
                "空键应该抛出异常");
        }

        [Test]
        public void GetValue_WithNullKey_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _configManager.GetValue<string>(null),
                "null 键应该抛出异常");
        }

        #endregion

        #region 性能测试

        [Test]
        public void ConfigManager_PerformanceTest_ShouldHandleMultipleOperations()
        {
            // Arrange
            const int operationCount = 100; // 减少操作数量以适应测试环境
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < operationCount; i++)
            {
                var key = $"perf.test.{i}";
                _configManager.SetValue(key, i, persistent: false); // 禁用持久化提高性能
                var value = _configManager.GetValue<int>(key);
                Assert.AreEqual(i, value, $"值 {i} 应该正确存储和获取");
            }

            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 5000, 
                $"{operationCount} 次配置操作应该在5秒内完成");
            
            UnityEngine.Debug.Log($"配置管理器性能测试: {operationCount} 次操作耗时 {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion
    }

    #region 测试辅助类

    [Serializable]
    public class TestConfigObject
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }

    public class MockConfigSource : IConfigSource
    {
        private readonly System.Collections.Generic.Dictionary<string, object> _data = new();
        public bool SaveCalled { get; private set; }
        public bool LoadCalled { get; private set; }

        public string Name { get; set; } = "MockConfigSource";
        public int Priority { get; set; } = 1;
        public bool SupportsPersistence => true;

        public T GetValue<T>(string key, T defaultValue = default)
        {
            return _data.TryGetValue(key, out var value) ? (T)value : defaultValue;
        }

        public void SetValue<T>(string key, T value)
        {
            _data[key] = value;
        }

        public bool HasValue(string key)
        {
            return _data.ContainsKey(key);
        }

        public bool RemoveValue(string key)
        {
            return _data.Remove(key);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public void Save()
        {
            SaveCalled = true;
        }

        public void Load()
        {
            LoadCalled = true;
        }

        public System.Collections.Generic.IEnumerable<string> GetAllKeys()
        {
            return _data.Keys;
        }
    }

    /// <summary>
    /// Mock EventBus 实现，用于测试
    /// </summary>
    public class MockEventBus : IEventBus
    {
        public void Broadcast<T>(T eventData) where T : notnull
        {
            // Mock 实现，不做任何操作
        }

        public void SubscribeBroadcast<T>(System.Action<T> handler, int priority = 1, bool isAsync = false) where T : notnull
        {
            // Mock 实现，不做任何操作
        }

        public void UnsubscribeBroadcast<T>(System.Action<T> handler) where T : notnull
        {
            // Mock 实现，不做任何操作
        }

        public void SendCommand<T>(T command) where T : notnull
        {
            // Mock 实现，不做任何操作
        }

        public TResponse Query<TQuery, TResponse>(TQuery query) where TQuery : notnull
        {
            // Mock 实现，返回默认值
            return default(TResponse);
        }

        public void RegisterCommandHandler<T>(System.Action<T> handler, bool replaceIfExists = true) where T : notnull
        {
            // Mock 实现，不做任何操作
        }

        public void RegisterQueryHandler<TQuery, TResponse>(System.Func<TQuery, TResponse> handler) where TQuery : notnull
        {
            // Mock 实现，不做任何操作
        }

        public void UnregisterCommandHandler<T>() where T : notnull
        {
            // Mock 实现，不做任何操作
        }

        public void UnregisterQueryHandler<TQuery, TResponse>() where TQuery : notnull
        {
            // Mock 实现，不做任何操作
        }

        public void Clear()
        {
            // Mock 实现，不做任何操作
        }
    }

    #endregion
}