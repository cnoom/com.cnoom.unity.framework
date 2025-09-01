using System;
using NUnit.Framework;
using UnityEngine;
using CnoomFramework.Core;
using CnoomFramework.Tests.Module;

namespace CnoomFramework.Tests
{
    /// <summary>
    /// 验证模块系统修复的测试
    /// </summary>
    public class VerifyModuleSystemFix
    {
        private FrameworkManager _frameworkManager;

        [SetUp]
        public void SetUp()
        {
            // 清理之前的实例
            try
            {
                CnoomFrameWork.Singleton.MonoSingleton<FrameworkManager>.DestroyInstance();
            }
            catch
            {
                // 如果清理失败，忽略错误
            }

            // 创建新的实例
            var go = new GameObject("TestFrameworkManager");
            _frameworkManager = go.AddComponent<FrameworkManager>();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                _frameworkManager?.Shutdown();
                CnoomFrameWork.Singleton.MonoSingleton<FrameworkManager>.DestroyInstance();
            }
            catch
            {
                // 如果清理失败，忽略错误
            }
            
            _frameworkManager = null;
        }

        [Test]
        public void VerifyAutoModulesAreRegistered()
        {
            Debug.Log("[VerifyModuleSystemFix] 开始验证自动模块注册");

            // Act
            _frameworkManager.Initialize();

            // Assert
            // 验证自动注册的模块数量（应该有PerformanceMonitor和LightweightContractValidationModule）
            Assert.GreaterOrEqual(_frameworkManager.ModuleCount, 2, "应该至少注册了PerformanceMonitor和LightweightContractValidationModule");
            
            // 验证具体的自动注册模块
            Assert.IsTrue(_frameworkManager.HasModule("PerformanceMonitor"), "应该自动注册PerformanceMonitor模块");
            Assert.IsTrue(_frameworkManager.HasModule("LightweightContractValidationModule"), "应该自动注册LightweightContractValidationModule模块");

            Debug.Log($"[VerifyModuleSystemFix] 框架已注册 {_frameworkManager.ModuleCount} 个模块");
            Debug.Log("[VerifyModuleSystemFix] 自动模块注册验证完成");
        }

        [Test]
        public void VerifyManualModuleRegistrationWithAutoModules()
        {
            Debug.Log("[VerifyModuleSystemFix] 开始验证手动模块注册");

            // Arrange
            var testModule = new TestLifecycleModule();
            
            // Act
            _frameworkManager.Initialize();
            var baseModuleCount = _frameworkManager.ModuleCount;
            
            _frameworkManager.RegisterModule(testModule);

            // Assert
            Assert.AreEqual(baseModuleCount + 1, _frameworkManager.ModuleCount, "应该在基础模块之上增加一个手动注册的模块");
            Assert.IsTrue(_frameworkManager.HasModule<TestLifecycleModule>(), "应该包含手动注册的模块");
            Assert.AreEqual(testModule, _frameworkManager.GetModule<TestLifecycleModule>(), "应该返回相同的模块实例");

            Debug.Log($"[VerifyModuleSystemFix] 基础模块数量: {baseModuleCount}, 总模块数量: {_frameworkManager.ModuleCount}");
            Debug.Log("[VerifyModuleSystemFix] 手动模块注册验证完成");
        }

        [Test]
        public void VerifyModuleUnregistrationWithAutoModules()
        {
            Debug.Log("[VerifyModuleSystemFix] 开始验证模块注销");

            // Arrange
            var testModule = new TestLifecycleModule();
            
            // Act
            _frameworkManager.Initialize();
            var baseModuleCount = _frameworkManager.ModuleCount;
            
            _frameworkManager.RegisterModule(testModule);
            _frameworkManager.UnregisterModule<TestLifecycleModule>();

            // Assert
            Assert.AreEqual(baseModuleCount, _frameworkManager.ModuleCount, "注销后应该回到基础模块数量");
            Assert.IsFalse(_frameworkManager.HasModule<TestLifecycleModule>(), "不应该包含已注销的模块");

            Debug.Log($"[VerifyModuleSystemFix] 注销后模块数量: {_frameworkManager.ModuleCount}");
            Debug.Log("[VerifyModuleSystemFix] 模块注销验证完成");
        }
    }
}