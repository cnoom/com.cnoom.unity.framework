using NUnit.Framework;
using CnoomFramework.Core;
using CnoomFramework.Core.Performance;
using UnityEngine;

namespace CnoomFramework.Tests.Performance
{
    /// <summary>
    /// 测试PerformanceMonitor模块是否正确注册
    /// </summary>
    [TestFixture]
    public class PerformanceModuleRegistrationTest
    {
        private FrameworkManager _frameworkManager;

        [SetUp]
        public void Setup()
        {
            // 确保框架管理器是干净的状态
            if (FrameworkManager.HasInstance)
            {
                FrameworkManager.Instance.Shutdown();
            }
            
            _frameworkManager = FrameworkManager.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            if (_frameworkManager != null)
            {
                _frameworkManager.Shutdown();
            }
        }

        [Test]
        public void PerformanceMonitor_ShouldBeAutoRegistered()
        {
            // Arrange & Act
            _frameworkManager.Initialize();

            // Assert
            Assert.IsTrue(_frameworkManager.HasModule<PerformanceMonitor>(), 
                "PerformanceMonitor应该被自动注册");
            
            var performanceMonitor = _frameworkManager.GetModule<PerformanceMonitor>();
            Assert.IsNotNull(performanceMonitor, "应该能够获取PerformanceMonitor实例");
            
            // 验证单例访问也能工作
            var singletonInstance = PerformanceMonitor.Instance;
            Assert.IsNotNull(singletonInstance, "单例实例应该可用");
            Assert.AreSame(performanceMonitor, singletonInstance, 
                "框架实例和单例实例应该是同一个对象");

            Debug.Log("[PerformanceModuleRegistrationTest] PerformanceMonitor模块注册测试通过");
        }

        [Test]
        public void PerformanceMonitor_ShouldHaveCorrectName()
        {
            // Arrange & Act
            _frameworkManager.Initialize();
            var performanceMonitor = _frameworkManager.GetModule<PerformanceMonitor>();

            // Assert
            Assert.AreEqual("PerformanceMonitor", performanceMonitor.Name, 
                "模块名称应该是PerformanceMonitor");
        }

        [Test]
        public void PerformanceMonitor_ShouldBeEnabledByDefault()
        {
            // Arrange & Act
            _frameworkManager.Initialize();
            var performanceMonitor = _frameworkManager.GetModule<PerformanceMonitor>();

            // Assert
            Assert.IsTrue(performanceMonitor.IsEnabled, 
                "PerformanceMonitor应该默认启用");
        }

        [Test]
        public void PerformanceMonitor_ShouldWorkAfterFrameworkInitialization()
        {
            // Arrange & Act
            _frameworkManager.Initialize();
            var performanceMonitor = PerformanceMonitor.Instance;

            // 测试基本功能
            string sampleId = performanceMonitor.BeginSample("TestOperation");
            Assert.IsNotNull(sampleId, "BeginSample应该返回有效的样本ID");
            Assert.IsNotEmpty(sampleId, "样本ID不应该为空");

            performanceMonitor.EndSample(sampleId);
            
            var stats = performanceMonitor.GetStats("TestOperation");
            Assert.IsNotNull(stats, "应该能够获取性能统计数据");
            Assert.AreEqual(1, stats.CallCount, "调用次数应该为1");

            Debug.Log("[PerformanceModuleRegistrationTest] PerformanceMonitor功能测试通过");
        }
    }
}