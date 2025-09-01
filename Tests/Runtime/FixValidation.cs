using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CnoomFramework.Core.Performance;
using CnoomFramework.Core;
using System.Text.RegularExpressions;

namespace CnoomFramework.Tests.FixValidation
{
    /// <summary>
    /// 验证修复的测试类
    /// </summary>
    public class FixValidationTests
    {
        [Test]
        public void PerformanceMonitor_ResetAllStats_ShouldClearDictionary()
        {
            // Arrange
            var monitor = PerformanceMonitor.Instance;
            
            // 记录一些性能数据
            var sampleId1 = monitor.BeginSample("Test1");
            monitor.EndSample(sampleId1);
            var sampleId2 = monitor.BeginSample("Test2");
            monitor.EndSample(sampleId2);
            
            // 验证数据存在
            Assert.AreEqual(2, monitor.GetAllStats().Count, "应该有2个统计数据");
            
            // Act
            monitor.ResetAllStats();
            
            // Assert
            Assert.AreEqual(0, monitor.GetAllStats().Count, "重置后应该没有任何统计数据");
            Assert.IsNull(monitor.GetStats("Test1"), "Test1统计数据应该被清除");
            Assert.IsNull(monitor.GetStats("Test2"), "Test2统计数据应该被清除");
        }
        
        [Test]
        public void LogAssert_Expect_ValidatesRegexPatterns()
        {
            // 验证我们的日志匹配模式是否正确
            var testMessage1 = "Failed to initialize module DataModule: Cannot initialize module DataModule: FrameworkManager is not initialized. Please call FrameworkManager.Instance.Initialize() before registering modules.";
            var pattern1 = new Regex(@"Failed to initialize module DataModule.*FrameworkManager is not initialized");
            Assert.IsTrue(pattern1.IsMatch(testMessage1), "模式1应该匹配");
            
            var testMessage2 = "[Error] [CnoomFramework] High severity error: Cannot initialize module DataModule: FrameworkManager is not initialized. Please call FrameworkManager.Instance.Initialize() before registering modules.";
            var pattern2 = new Regex(@"\[Error\] \[CnoomFramework\] High severity error: Cannot initialize module DataModule.*FrameworkManager is not initialized");
            Assert.IsTrue(pattern2.IsMatch(testMessage2), "模式2应该匹配");
            
            var testMessage3 = "[Error] [CnoomFramework] High severity error: 模块 [DataModule] 初始化失败: Cannot initialize module DataModule: FrameworkManager is not initialized. Please call FrameworkManager.Instance.Initialize() before registering modules.";
            var pattern3 = new Regex(@"\[Error\] \[CnoomFramework\] High severity error: 模块.*DataModule.*初始化失败.*FrameworkManager is not initialized");
            Assert.IsTrue(pattern3.IsMatch(testMessage3), "模式3应该匹配");
        }
        
        [Test]
        public void Framework_LogAssert_ShouldHandleExpectedErrors()
        {
            // 这个测试验证LogAssert.Expect能正确预期错误日志
            // 预期一个简单的错误日志
            LogAssert.Expect(LogType.Error, new Regex("Test error message"));
            
            // 生成预期的错误日志
            Debug.LogError("Test error message");
            
            // 如果没有未处理的日志，测试通过
            Assert.Pass("LogAssert.Expect正常工作");
        }
    }
}