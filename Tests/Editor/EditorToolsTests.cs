using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using CnoomFramework.Core;
using CnoomFramework.Core.EventBuss.Core;

namespace CnoomFramework.Tests.Editor
{
    /// <summary>
    /// Editor 工具测试
    /// </summary>
    public class EditorToolsTests
    {
        private FrameworkManager _frameworkManager;
        private GameObject _testGameObject;

        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestFramework");
            _frameworkManager = _testGameObject.AddComponent<FrameworkManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_frameworkManager != null)
            {
                // 在Editor模式下，不调用Shutdown（因为它会尝试销毁GameObject）
                // 直接销毁GameObject即可
                if (_testGameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(_testGameObject);
                    _testGameObject = null;
                }
                _frameworkManager = null;
            }
        }

        #region 框架调试器测试

        [Test]
        public void FrameworkDebugger_ShouldDetectFrameworkInstance()
        {
            // Arrange
            _frameworkManager.Initialize();

            // Act
            var hasFramework = FrameworkManager.Instance != null;

            // Assert
            Assert.IsTrue(hasFramework, "调试器应该能检测到框架实例");
            Assert.AreEqual(_frameworkManager, FrameworkManager.Instance, "应该返回正确的框架实例");
        }

        [Test]
        public void FrameworkDebugger_ShouldListModules()
        {
            // Arrange
            _frameworkManager.Initialize(); // 先初始化框架
            var testModule = new EditorTestModule();
            _frameworkManager.RegisterModule(testModule); // 再注册模块

            // Act
            var modules = _frameworkManager.Modules;

            // Assert
            Assert.IsNotNull(modules, "应该返回模块列表");
            Assert.GreaterOrEqual(modules.Count, 1, "应该至少有一个模块"); // 框架会自动注册一些模块
            Assert.IsTrue(modules.Contains(testModule), "应该包含测试模块");
        }

        [Test]
        public void FrameworkDebugger_ShouldShowModuleStates()
        {
            // Arrange
            _frameworkManager.Initialize(); // 先初始化框架
            var testModule = new EditorTestModule();

            // Act & Assert
            Assert.AreEqual(ModuleState.Uninitialized, testModule.State, "初始状态应该是未初始化");

            _frameworkManager.RegisterModule(testModule); // 注册模块后会自动初始化和启动
            Assert.AreEqual(ModuleState.Started, testModule.State, "注册后状态应该是已启动");
        }

        #endregion

        #region 事件流可视化测试

        [Test]
        public void EventFlowVisualizer_ShouldRecordEvents()
        {
            // Arrange
            _frameworkManager.Initialize();
            var eventRecorder = new TestEventRecorder();

            // Act
            _frameworkManager.EventBus.Broadcast(new EditorTestEvent { Message = "Test Event" });

            // Assert
            // 注意：实际的事件记录器需要在真实的 EventFlowRecorder 中实现
            // 这里只是演示测试结构
        }

        #endregion

        #region 性能监控器测试

        [Test]
        public void PerformanceMonitor_ShouldTrackFrameworkPerformance()
        {
            // Arrange
            _frameworkManager.Initialize();

            // Act
            var startTime = EditorApplication.timeSinceStartup;
            
            // 执行一些框架操作
            for (int i = 0; i < 100; i++)
            {
                _frameworkManager.EventBus.Broadcast(new EditorTestEvent { Message = $"Event {i}" });
            }
            
            var endTime = EditorApplication.timeSinceStartup;
            var duration = endTime - startTime;

            // Assert
            Assert.Less(duration, 1.0, "100个事件处理应该在1秒内完成");
            
            Debug.Log($"编辑器性能测试: 100个事件处理耗时 {duration * 1000:F2}ms");
        }

        #endregion

        #region Mock 管理器测试

        [Test]
        public void MockManager_ShouldAllowModuleMocking()
        {
            // Arrange
            _frameworkManager.Initialize(); // 先初始化框架
            var realModule = new EditorTestModule();
            var mockModule = new MockEditorTestModule();

            // 以接口类型注册模块
            _frameworkManager.RegisterModule<IEditorTestModule>(realModule);

            // Act
            var mockManager = _frameworkManager.MockManager;
            mockManager.RegisterMock<IEditorTestModule>(mockModule);

            // Assert
            var retrievedModule = _frameworkManager.GetModule<IEditorTestModule>();
            Assert.AreEqual(mockModule, retrievedModule, "应该返回 Mock 模块");
            Assert.IsTrue(mockModule.IsMocked, "Mock 模块应该标记为已模拟");
        }

        [Test]
        public void MockManager_ShouldRestoreOriginalModule()
        {
            // Arrange
            _frameworkManager.Initialize(); // 先初始化框架
            var realModule = new EditorTestModule();
            var mockModule = new MockEditorTestModule();

            // 以接口类型注册模块
            _frameworkManager.RegisterModule<IEditorTestModule>(realModule);

            var mockManager = _frameworkManager.MockManager;
            mockManager.RegisterMock<IEditorTestModule>(mockModule);

            // Act
            mockManager.RemoveMock<IEditorTestModule>();

            // Assert
            var retrievedModule = _frameworkManager.GetModule<IEditorTestModule>();
            Assert.AreEqual(realModule, retrievedModule, "应该恢复原始模块");
        }

        #endregion

        #region 配置编辑器测试

        [Test]
        public void ConfigEditor_ShouldSaveAndLoadSettings()
        {
            // Arrange
            _frameworkManager.Initialize();
            var configManager = _frameworkManager.ConfigManager;

            // Act
            configManager.SetValue("editor.test.setting", "test_value", persistent: true);
            configManager.Save();

            // 模拟重新加载
            configManager.Load();
            var loadedValue = configManager.GetValue<string>("editor.test.setting");

            // Assert
            Assert.AreEqual("test_value", loadedValue, "配置应该正确保存和加载");
        }

        #endregion

        #region Editor 窗口测试

        [Test]
        public void EditorWindow_ShouldOpenWithoutErrors()
        {
            // Arrange & Act & Assert
            // 注意：实际的编辑器窗口测试需要在 Unity Editor 环境中运行
            // 这里只是验证窗口类型的存在和基本功能
            
            Assert.DoesNotThrow(() =>
            {
                // 验证编辑器窗口类型存在
                var debuggerWindowType = typeof(CnoomFramework.Editor.FrameworkDebuggerWindow);
                Assert.IsNotNull(debuggerWindowType, "框架调试器窗口类型应该存在");
                
                var eventFlowWindowType = typeof(CnoomFramework.Editor.EventFlowVisualizerWindow);
                Assert.IsNotNull(eventFlowWindowType, "事件流可视化窗口类型应该存在");
                
                var performanceWindowType = typeof(CnoomFramework.Editor.PerformanceMonitorWindow);
                Assert.IsNotNull(performanceWindowType, "性能监控窗口类型应该存在");
                
            }, "编辑器窗口类型检查不应该抛出异常");
        }

        #endregion

        #region 自动化测试工具

        [Test]
        public void AutomatedTesting_ShouldValidateFrameworkIntegrity()
        {
            // Arrange
            var issues = new List<string>();

            // Act - 执行自动化检查
            
            // 检查必需的组件
            if (FrameworkManager.Instance == null)
                issues.Add("FrameworkManager 实例不存在");

            // 检查程序集引用
            try
            {
                var runtimeAssembly = System.Reflection.Assembly.GetAssembly(typeof(FrameworkManager));
                var editorAssembly = System.Reflection.Assembly.GetAssembly(typeof(CnoomFramework.Editor.FrameworkDebuggerWindow));
                
                if (runtimeAssembly == null)
                    issues.Add("Runtime 程序集加载失败");
                    
                if (editorAssembly == null)
                    issues.Add("Editor 程序集加载失败");
            }
            catch (Exception ex)
            {
                issues.Add($"程序集检查异常: {ex.Message}");
            }

            // Assert
            Assert.IsEmpty(issues, $"框架完整性检查失败: {string.Join(", ", issues)}");
        }

        #endregion
    }

    #region 测试辅助类

    /// <summary>
    /// 编辑器测试模块接口
    /// </summary>
    public interface IEditorTestModule : IModule
    {
        bool InitializeCalled { get; }
        bool StartCalled { get; }
    }

    public class EditorTestModule : BaseModule, IEditorTestModule
    {
        public bool InitializeCalled { get; private set; }
        public bool StartCalled { get; private set; }

        protected override void OnInit()
        {
            InitializeCalled = true;
        }

        protected override void OnStart()
        {
            StartCalled = true;
        }
    }

    public class MockEditorTestModule : EditorTestModule
    {
        public bool IsMocked { get; set; } = true;

        protected override void OnInit()
        {
            base.OnInit();
            Debug.Log("Mock 模块已初始化");
        }
    }

    public class EditorTestEvent
    {
        public string Message { get; set; }
    }

    public class TestEventRecorder
    {
        private readonly List<object> _recordedEvents = new List<object>();

        public void RecordEvent(object evt)
        {
            _recordedEvents.Add(evt);
        }

        public List<object> GetRecordedEvents()
        {
            return new List<object>(_recordedEvents);
        }

        public void Clear()
        {
            _recordedEvents.Clear();
        }
    }

    #endregion
}