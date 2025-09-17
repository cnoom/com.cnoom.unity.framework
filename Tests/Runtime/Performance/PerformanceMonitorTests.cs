using System;
using System.Threading;
using NUnit.Framework;
using CnoomFramework.Core.Performance;

namespace CnoomFramework.Tests.Performance
{
    /// <summary>
    /// 性能监控器测试类
    /// </summary>
    [TestFixture]
    public class PerformanceMonitorTests
    {
        private PerformanceMonitor _monitor;

        [SetUp]
        public void Setup()
        {
            _monitor = PerformanceMonitor.Instance;
            _monitor.SetEnabled(true);
            _monitor.ClearStats();
        }

        [TearDown]
        public void TearDown()
        {
            _monitor?.ClearStats();
        }

        [Test]
        public void BeginSample_ShouldReturnValidSampleId()
        {
            // Arrange
            string operationName = "TestOperation";

            // Act
            string sampleId = _monitor.BeginSample(operationName);

            // Assert
            Assert.IsNotNull(sampleId);
            Assert.IsNotEmpty(sampleId);
        }

        [Test]
        public void EndSample_ShouldRecordPerformanceStats()
        {
            // Arrange
            string operationName = "TestOperation";
            string sampleId = _monitor.BeginSample(operationName);

            // 模拟一些工作
            Thread.Sleep(10);

            // Act
            _monitor.EndSample(sampleId);

            // Assert
            var stats = _monitor.GetStats(operationName);
            Assert.IsNotNull(stats);
            Assert.AreEqual(1, stats.CallCount);
            Assert.Greater(stats.TotalTimeMs, 0);
        }

        [Test]
        public void RecordSample_ShouldAddToStatistics()
        {
            // Arrange
            string operationName = "TestOperation";
            var elapsedTime = TimeSpan.FromMilliseconds(25.5);

            // Act
            _monitor.RecordSample(operationName, elapsedTime);

            // Assert
            var stats = _monitor.GetStats(operationName);
            Assert.IsNotNull(stats);
            Assert.AreEqual(1, stats.CallCount);
            Assert.AreEqual(25.5, stats.TotalTimeMs, 0.1);
            Assert.AreEqual(25.5, stats.AverageTimeMs, 0.1);
        }

        [Test]
        public void GetAllStats_ShouldReturnAllRecordedStats()
        {
            // Arrange
            _monitor.RecordSample("Operation1", TimeSpan.FromMilliseconds(10));
            _monitor.RecordSample("Operation2", TimeSpan.FromMilliseconds(20));
            _monitor.RecordSample("Operation3", TimeSpan.FromMilliseconds(30));

            // Act
            var allStats = _monitor.GetAllStats();

            // Assert
            Assert.AreEqual(3, allStats.Count);
            Assert.IsTrue(allStats.ContainsKey("Operation1"));
            Assert.IsTrue(allStats.ContainsKey("Operation2"));
            Assert.IsTrue(allStats.ContainsKey("Operation3"));
        }

        [Test]
        public void ClearStats_ShouldRemoveAllStatistics()
        {
            // Arrange
            _monitor.RecordSample("Operation1", TimeSpan.FromMilliseconds(10));
            _monitor.RecordSample("Operation2", TimeSpan.FromMilliseconds(20));

            // Act
            _monitor.ClearStats();

            // Assert
            var allStats = _monitor.GetAllStats();
            Assert.AreEqual(0, allStats.Count);
        }

        [Test]
        public void SetEnabled_False_ShouldDisableMonitoring()
        {
            // Arrange
            _monitor.SetEnabled(false);

            // Act
            string sampleId = _monitor.BeginSample("TestOperation");

            // Assert
            Assert.IsNull(sampleId);
        }

        [Test]
        public void MultipleRecords_ShouldCalculateCorrectStatistics()
        {
            // Arrange
            string operationName = "TestOperation";

            // Act
            _monitor.RecordSample(operationName, TimeSpan.FromMilliseconds(10));
            _monitor.RecordSample(operationName, TimeSpan.FromMilliseconds(20));
            _monitor.RecordSample(operationName, TimeSpan.FromMilliseconds(30));

            // Assert
            var stats = _monitor.GetStats(operationName);
            Assert.IsNotNull(stats);
            Assert.AreEqual(3, stats.CallCount);
            Assert.AreEqual(60, stats.TotalTimeMs);
            Assert.AreEqual(20, stats.AverageTimeMs);
            Assert.AreEqual(10, stats.MinTimeMs);
            Assert.AreEqual(30, stats.MaxTimeMs);
        }
    }
}