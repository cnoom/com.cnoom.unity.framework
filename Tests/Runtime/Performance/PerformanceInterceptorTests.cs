using System;
using NUnit.Framework;
using CnoomFramework.Core.Performance;
using CnoomFramework.Core.Attributes;

namespace CnoomFramework.Tests.Performance
{
    /// <summary>
    /// 性能拦截器测试类
    /// </summary>
    [TestFixture]
    public class PerformanceInterceptorTests
    {
        private PerformanceMonitor _monitor;

        [SetUp]
        public void Setup()
        {
            _monitor = PerformanceMonitor.Instance;
            _monitor.SetEnabled(true);
            _monitor.ClearStats();
            PerformanceInterceptor.Initialize(_monitor);
        }

        [TearDown]
        public void TearDown()
        {
            _monitor?.ClearStats();
        }

        [Test]
        public void MonitorOperation_ShouldRecordPerformanceData()
        {
            // Arrange
            string operationName = "TestOperation";
            bool operationExecuted = false;

            // Act
            PerformanceInterceptor.MonitorOperation(operationName, () =>
            {
                operationExecuted = true;
                System.Threading.Thread.Sleep(10);
            });

            // Assert
            Assert.IsTrue(operationExecuted);
            var stats = _monitor.GetStats(operationName);
            Assert.IsNotNull(stats);
            Assert.AreEqual(1, stats.CallCount);
            Assert.Greater(stats.TotalTimeMs, 0);
        }

        [Test]
        public void MonitorFunction_ShouldReturnValueAndRecordPerformance()
        {
            // Arrange
            string operationName = "TestFunction";
            int expectedResult = 42;

            // Act
            int result = PerformanceInterceptor.MonitorFunction(operationName, () =>
            {
                System.Threading.Thread.Sleep(5);
                return expectedResult;
            });

            // Assert
            Assert.AreEqual(expectedResult, result);
            var stats = _monitor.GetStats(operationName);
            Assert.IsNotNull(stats);
            Assert.AreEqual(1, stats.CallCount);
        }

        [Test]
        public void CreateScope_ShouldRecordPerformanceWhenDisposed()
        {
            // Arrange
            string operationName = "ScopeTest";

            // Act
            using (PerformanceInterceptor.CreateScope(operationName))
            {
                System.Threading.Thread.Sleep(5);
            }

            // Assert
            var stats = _monitor.GetStats(operationName);
            Assert.IsNotNull(stats);
            Assert.AreEqual(1, stats.CallCount);
            Assert.Greater(stats.TotalTimeMs, 0);
        }

        [Test]
        public void WithPerformanceMonitoring_Action_ShouldWork()
        {
            // Arrange
            string operationName = "ExtensionTest";
            bool executed = false;
            Action testAction = () => { executed = true; };

            // Act
            var monitoredAction = testAction.WithPerformanceMonitoring(operationName);
            monitoredAction();

            // Assert
            Assert.IsTrue(executed);
            var stats = _monitor.GetStats(operationName);
            Assert.IsNotNull(stats);
            Assert.AreEqual(1, stats.CallCount);
        }

        [Test]
        public void WithPerformanceMonitoring_Func_ShouldWork()
        {
            // Arrange
            string operationName = "FuncExtensionTest";
            Func<string> testFunc = () => "test result";

            // Act
            var monitoredFunc = testFunc.WithPerformanceMonitoring(operationName);
            string result = monitoredFunc();

            // Assert
            Assert.AreEqual("test result", result);
            var stats = _monitor.GetStats(operationName);
            Assert.IsNotNull(stats);
            Assert.AreEqual(1, stats.CallCount);
        }

        [Test]
        public void MonitorOperation_WithException_ShouldStillRecordStats()
        {
            // Arrange
            string operationName = "ExceptionTest";

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                PerformanceInterceptor.MonitorOperation(operationName, () =>
                {
                    throw new InvalidOperationException("Test exception");
                });
            });

            // 验证即使有异常，性能数据仍然被记录
            var stats = _monitor.GetStats(operationName);
            Assert.IsNotNull(stats);
            Assert.AreEqual(1, stats.CallCount);
        }
    }

    /// <summary>
    /// 测试用的示例类，演示如何使用MonitorPerformance特性
    /// </summary>
    public class TestClass
    {
        [MonitorPerformance("测试方法")]
        public void TestMethod()
        {
            System.Threading.Thread.Sleep(1);
        }

        [MonitorPerformance("计算方法", true)]
        public int Calculate(int a, int b)
        {
            System.Threading.Thread.Sleep(2);
            return a + b;
        }
    }
}