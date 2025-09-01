using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CnoomFramework.Core;
using CnoomFramework.Core.Events;

namespace CnoomFramework.Tests.Module
{
    /// <summary>
    /// 模块系统测试
    /// </summary>
    public class ModuleSystemTests
    {
        private FrameworkManager _frameworkManager;

        [SetUp]
        public void SetUp()
        {
            // 确保清理之前的实例
            try
            {
                CnoomFrameWork.Singleton.MonoSingleton<FrameworkManager>.DestroyInstance();
            }
            catch
            {
                // 忽略清理错误
            }
            
            // 获取或创建新的实例
            _frameworkManager = FrameworkManager.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            if (_frameworkManager != null)
            {
                // 先记录可能的警告/错误日志
                LogAssert.ignoreFailingMessages = true;
                
                try
                {
                    _frameworkManager.Shutdown();
                }
                catch
                {
                    // 忽略 Shutdown 过程中的错误
                }
                
                // 清理单例实例
                try
                {
                    CnoomFrameWork.Singleton.MonoSingleton<FrameworkManager>.DestroyInstance();
                }
                catch
                {
                    // 忽略清理错误
                }
                
                LogAssert.ignoreFailingMessages = false;
            }
        }

        #region 模块生命周期测试

        [Test]
        public void Module_Lifecycle_ShouldExecuteInCorrectOrder()
        {
            // Arrange
            var testModule = new TestLifecycleModule();
            var executionOrder = new List<string>();
            testModule.OnExecutionStep = step => executionOrder.Add(step);

            // 先初始化框架，再测试模块
            _frameworkManager.Initialize();

            // Act
            testModule.Init();
            testModule.Start();
            testModule.Shutdown();

            // Assert
            Assert.AreEqual(3, executionOrder.Count, "应该执行3个生命周期步骤");
            Assert.AreEqual("Init", executionOrder[0], "Init 应该最先执行");
            Assert.AreEqual("Start", executionOrder[1], "Start 应该第二执行");
            Assert.AreEqual("Shutdown", executionOrder[2], "Shutdown 应该最后执行");
        }

        [Test]
        public void Module_StateTransition_ShouldBeCorrect()
        {
            // Arrange
            var testModule = new TestLifecycleModule();

            // 先初始化框架，再测试模块
            _frameworkManager.Initialize();

            // Act & Assert
            Assert.AreEqual(ModuleState.Uninitialized, testModule.State, "初始状态应该是 Uninitialized");

            testModule.Init();
            Assert.AreEqual(ModuleState.Initialized, testModule.State, "Init 后状态应该是 Initialized");

            testModule.Start();
            Assert.AreEqual(ModuleState.Started, testModule.State, "Start 后状态应该是 Started");

            testModule.Shutdown();
            Assert.AreEqual(ModuleState.Shutdown, testModule.State, "Shutdown 后状态应该是 Shutdown");
        }

        [Test]
        public void Module_InitTwice_ShouldIgnoreSecondCall()
        {
            // Arrange
            var testModule = new TestLifecycleModule();
            var initCallCount = 0;
            testModule.OnExecutionStep = step => { if (step == "Init") initCallCount++; };

            // 先初始化框架，再测试模块
            _frameworkManager.Initialize();

            // Act
            testModule.Init();
            testModule.Init(); // 第二次调用

            // Assert
            Assert.AreEqual(1, initCallCount, "Init 只应该被调用一次");
            Assert.AreEqual(ModuleState.Initialized, testModule.State, "状态应该保持 Initialized");
        }

        [Test]
        public void Module_StartWithoutInit_ShouldIgnore()
        {
            // Arrange
            var testModule = new TestLifecycleModule();
            var startCallCount = 0;
            testModule.OnExecutionStep = step => { if (step == "Start") startCallCount++; };

            // 先初始化框架（但不初始化模块）
            _frameworkManager.Initialize();

            // Act
            testModule.Start(); // 没有先调用 Init

            // Assert
            Assert.AreEqual(0, startCallCount, "Start 不应该被调用");
            Assert.AreEqual(ModuleState.Uninitialized, testModule.State, "状态应该保持 Uninitialized");
        }

        #endregion

        #region 模块注册测试

        [Test]
        public void RegisterModule_WithValidModule_ShouldSucceed()
        {
            // Arrange
            var testModule = new TestLifecycleModule();

            // 先初始化框架，再注册模块
            _frameworkManager.Initialize();
            
            // 记录初始化后的基础模块数量（PerformanceMonitor 和 LightweightContractValidationModule）
            var baseModuleCount = _frameworkManager.ModuleCount;

            // Act
            _frameworkManager.RegisterModule(testModule);

            // Assert
            Assert.AreEqual(baseModuleCount + 1, _frameworkManager.ModuleCount, "应该在基础模块之上增加一个注册的模块");
            Assert.IsTrue(_frameworkManager.HasModule<TestLifecycleModule>(), "应该包含注册的模块类型");
            Assert.AreEqual(testModule, _frameworkManager.GetModule<TestLifecycleModule>(), "应该返回相同的模块实例");
        }

        [Test]
        public void RegisterModule_WithNullModule_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _frameworkManager.RegisterModule<TestLifecycleModule>(null),
                "注册 null 模块应该抛出异常");
        }

        [Test]
        public void RegisterModule_WithDuplicateType_ShouldThrow()
        {
            // Arrange
            var module1 = new TestLifecycleModule();
            var module2 = new TestLifecycleModule();

            // 先初始化框架，再注册模块
            _frameworkManager.Initialize();
            _frameworkManager.RegisterModule(module1);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _frameworkManager.RegisterModule(module2),
                "注册重复类型的模块应该抛出异常");
        }

        [Test]
        public void UnregisterModule_WithExistingModule_ShouldSucceed()
        {
            // Arrange
            var testModule = new TestLifecycleModule();
            
            // 先初始化框架，再注册模块
            _frameworkManager.Initialize();
            
            // 记录初始化后的基础模块数量（PerformanceMonitor 和 LightweightContractValidationModule）
            var baseModuleCount = _frameworkManager.ModuleCount;
            
            _frameworkManager.RegisterModule(testModule);

            // Act
            _frameworkManager.UnregisterModule<TestLifecycleModule>();

            // Assert
            Assert.AreEqual(baseModuleCount, _frameworkManager.ModuleCount, "模块应该被移除，只保留基础模块");
            Assert.IsFalse(_frameworkManager.HasModule<TestLifecycleModule>(), "不应该包含已移除的模块");
            Assert.AreEqual(ModuleState.Shutdown, testModule.State, "模块应该被正确关闭");
        }

        #endregion

        #region 模块依赖测试

        [Test]
        public void ModuleDependency_ShouldInitializeInCorrectOrder()
        {
            // Arrange
            var initOrder = new List<string>();
            var moduleA = new TestDependentModule { ModuleName = "A", InitOrder = initOrder };
            var moduleB = new TestDependencyModule { ModuleName = "B", InitOrder = initOrder };

            // 先初始化框架，然后批量注册模块以正确处理依赖关系
            _frameworkManager.Initialize();
            
            // 使用批量注册方法，它会正确处理依赖关系
            _frameworkManager.RegisterModules(moduleA, moduleB); // A 依赖 B

            // Act & Assert
            // 依赖解析应该确保 B 在 A 之前初始化
            Assert.AreEqual(2, initOrder.Count, "两个模块都应该被初始化");
            Assert.AreEqual("B", initOrder[0], "被依赖的模块 B 应该先初始化");
            Assert.AreEqual("A", initOrder[1], "依赖模块 A 应该后初始化");
        }

        #endregion

        #region 异常处理测试

        [Test]
        public void Module_InitException_ShouldNotStopOtherModules()
        {
            // 预期模块初始化失败的错误日志
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, 
                new System.Text.RegularExpressions.Regex("Failed to initialize module TestExceptionModule.*Test initialization exception"));
            
            // 预期错误恢复过程中的日志
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, 
                new System.Text.RegularExpressions.Regex("\\[CnoomFramework\\] Generic exception: Exception - Test initialization exception"));
            
            // 预期堆栈跟踪日志
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, 
                new System.Text.RegularExpressions.Regex("\\[CnoomFramework\\] Stack trace:.*at CnoomFramework\\.Tests\\.Module\\.TestExceptionModule\\.OnInit"));
            
            // 预期高严重性错误日志
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, 
                new System.Text.RegularExpressions.Regex("\\[CnoomFramework\\] High severity error: 模块 \\[TestExceptionModule\\] 初始化失败: Test initialization exception"));
            
            // 预期模块重启尝试时的重复错误日志
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, 
                new System.Text.RegularExpressions.Regex("Failed to initialize module TestExceptionModule.*Test initialization exception"));
            
            // 预期最终的严重错误日志
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, 
                new System.Text.RegularExpressions.Regex("严重: 模块 \\[TestExceptionModule\\] 初始化失败且恢复失败"));
            
            // Arrange
            var normalModule = new TestLifecycleModule();
            var exceptionModule = new TestExceptionModule();
            var executionSteps = new List<string>();
            
            normalModule.OnExecutionStep = step => executionSteps.Add($"Normal-{step}");
            exceptionModule.OnExecutionStep = step => executionSteps.Add($"Exception-{step}");

            // 先初始化框架，再注册模块
            _frameworkManager.Initialize();
            _frameworkManager.RegisterModule(normalModule);
            
            // 注册异常模块时不应该抛出异常，应该被框架处理
            Assert.DoesNotThrow(() => _frameworkManager.RegisterModule(exceptionModule),
                "模块初始化异常不应该中断框架操作");

            // Act & Assert
            Assert.Contains("Normal-Init", executionSteps, "正常模块应该被初始化");
            Assert.Contains("Exception-Init", executionSteps, "异常模块也应该尝试初始化");
            
            // 验证正常模块正常工作，异常模块被正确处理
            Assert.AreEqual(ModuleState.Started, normalModule.State, "正常模块应该正常启动");
            
            // 验证两个模块都被注册了
            Assert.IsTrue(_frameworkManager.HasModule<TestLifecycleModule>(), "正常模块应该被注册");
            Assert.IsTrue(_frameworkManager.HasModule<TestExceptionModule>(), "异常模块也应该被注册（即使初始化失败）");
        }

        #endregion
    }

    #region 测试模块类

    public class TestLifecycleModule : BaseModule
    {
        public System.Action<string> OnExecutionStep;

        protected override void OnInit()
        {
            OnExecutionStep?.Invoke("Init");
        }

        protected override void OnStart()
        {
            OnExecutionStep?.Invoke("Start");
        }

        protected override void OnShutdown()
        {
            OnExecutionStep?.Invoke("Shutdown");
        }
    }

    [CnoomFramework.Core.Attributes.DependsOn(typeof(TestDependencyModule))]
    public class TestDependentModule : BaseModule
    {
        public string ModuleName { get; set; }
        public List<string> InitOrder { get; set; }

        protected override void OnInit()
        {
            InitOrder?.Add(ModuleName);
        }
    }

    public class TestDependencyModule : BaseModule
    {
        public string ModuleName { get; set; }
        public List<string> InitOrder { get; set; }

        protected override void OnInit()
        {
            InitOrder?.Add(ModuleName);
        }
    }

    public class TestExceptionModule : BaseModule
    {
        public System.Action<string> OnExecutionStep;

        protected override void OnInit()
        {
            OnExecutionStep?.Invoke("Init");
            throw new System.Exception("Test initialization exception");
        }
    }

    #endregion
}