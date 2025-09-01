using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NUnit.Framework;

namespace CnoomFramework.Tests
{
    /// <summary>
    /// 框架测试运行器 - 统一管理和执行所有测试
    /// </summary>
    public class FrameworkTestRunner
    {
        private static TestReportGenerator _reportGenerator;
        private static TestSuiteConfig _config;

        [MenuItem("CnoomFramework/Run All Tests")]
        public static void RunAllTests()
        {
            RunTestSuite();
        }

        [MenuItem("CnoomFramework/Run Performance Tests")]
        public static void RunPerformanceTests()
        {
            RunTestSuite(performanceOnly: true);
        }

        [MenuItem("CnoomFramework/Generate Test Report")]
        public static void GenerateTestReport()
        {
            if (_reportGenerator == null)
            {
                Debug.LogWarning("请先运行测试套件以生成报告");
                return;
            }

            var report = _reportGenerator.GenerateReport(_config);
            Debug.Log("=== 测试报告 ===\n" + report);
        }

        public static void RunTestSuite(bool performanceOnly = false)
        {
            try
            {
                InitializeTestRunner();
                
                Debug.Log("🚀 开始执行 Cnoom Framework 测试套件...");
                _reportGenerator.StartTestRun();

                // 运行不同类型的测试
                if (!performanceOnly)
                {
                    RunUnitTests();
                    RunIntegrationTests();
                    
                    if (_config.EnableEditorTests)
                    {
                        RunEditorTests();
                    }
                }

                if (_config.EnablePerformanceTests)
                {
                    RunPerformanceTests();
                }

                _reportGenerator.EndTestRun();
                
                // 生成并显示报告
                var report = _reportGenerator.GenerateReport(_config);
                Debug.Log("📊 测试完成！\n" + report);

            }
            catch (Exception ex)
            {
                Debug.LogError($"测试运行器异常: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void InitializeTestRunner()
        {
            _reportGenerator = new TestReportGenerator();
            
            // 尝试加载配置
            _config = Resources.Load<TestSuiteConfig>("TestSuiteConfig");
            if (_config == null)
            {
                Debug.LogWarning("未找到测试配置文件，使用默认配置");
                _config = ScriptableObject.CreateInstance<TestSuiteConfig>();
            }
        }

        private static void RunUnitTests()
        {
            Debug.Log("📝 开始执行单元测试...");

            // 事件总线测试
            RunTestClass<CnoomFramework.Tests.EventBus.EventBusTests>("EventBus");
            
            // 模块系统测试
            RunTestClass<CnoomFramework.Tests.Module.ModuleSystemTests>("Module");
            
            // 配置管理测试
            RunTestClass<CnoomFramework.Tests.Config.ConfigManagerTests>("Config");
            
            // 错误处理测试
            RunTestClass<CnoomFramework.Tests.ErrorHandling.ErrorHandlingTests>("ErrorHandling");
        }

        private static void RunIntegrationTests()
        {
            if (!_config.EnableIntegrationTests)
            {
                Debug.Log("⏭️ 跳过集成测试（已禁用）");
                return;
            }

            Debug.Log("🔗 开始执行集成测试...");
            RunTestClass<CnoomFramework.Tests.Integration.IntegrationTests>("Integration");
        }

        private static void RunEditorTests()
        {
#if UNITY_EDITOR
            Debug.Log("🛠️ 开始执行编辑器测试...");
            RunTestClass<CnoomFramework.Tests.Editor.EditorToolsTests>("Editor");
#else
            Debug.Log("⏭️ 跳过编辑器测试（非编辑器环境）");
#endif
        }

        private static void RunPerformanceTests()
        {
            Debug.Log("⚡ 开始执行性能测试...");
            
            var performanceResults = new Dictionary<string, PerformanceMetrics>();
            
            // 事件总线性能测试
            var eventBusMetrics = BenchmarkEventBusPerformance();
            performanceResults["EventBus_Performance"] = eventBusMetrics;
            _reportGenerator.RecordPerformanceMetrics("EventBus_Performance", eventBusMetrics);

            // 模块管理性能测试
            var moduleMetrics = BenchmarkModulePerformance();
            performanceResults["Module_Performance"] = moduleMetrics;
            _reportGenerator.RecordPerformanceMetrics("Module_Performance", moduleMetrics);

            // 配置管理性能测试
            var configMetrics = BenchmarkConfigPerformance();
            performanceResults["Config_Performance"] = configMetrics;
            _reportGenerator.RecordPerformanceMetrics("Config_Performance", configMetrics);

            Debug.Log($"✅ 性能测试完成，共测试 {performanceResults.Count} 个组件");
        }

        private static void RunTestClass<T>(string category) where T : new()
        {
            var testClass = new T();
            var testMethods = typeof(T).GetMethods();
            
            foreach (var method in testMethods)
            {
                var testAttribute = method.GetCustomAttributes(typeof(TestAttribute), false);
                if (testAttribute.Length > 0)
                {
                    RunSingleTest(testClass, method, category);
                }
            }
        }

        private static void RunSingleTest(object testInstance, System.Reflection.MethodInfo testMethod, string category)
        {
            var testResult = new TestResult($"{testInstance.GetType().Name}.{testMethod.Name}", category);
            
            try
            {
                // 执行 SetUp
                var setupMethod = testInstance.GetType().GetMethod("SetUp");
                setupMethod?.Invoke(testInstance, null);

                // 执行测试方法
                testMethod.Invoke(testInstance, null);
                
                testResult.MarkPassed();
                
                // 执行 TearDown
                var tearDownMethod = testInstance.GetType().GetMethod("TearDown");
                tearDownMethod?.Invoke(testInstance, null);
            }
            catch (Exception ex)
            {
                testResult.MarkFailed(ex.Message, ex.StackTrace);
                Debug.LogError($"❌ 测试失败: {testResult.TestName}\n{ex.Message}");
            }
            
            _reportGenerator.RecordTestResult(testResult);
        }

        #region 性能基准测试

        private static PerformanceMetrics BenchmarkEventBusPerformance()
        {
            var metrics = new PerformanceMetrics();
            var eventBus = new CnoomFramework.Core.EventBuss.Core.EventBus();
            
            // 订阅测试事件
            eventBus.SubscribeBroadcast<TestPerformanceEvent>(evt => { });

            // 执行性能测试
            for (int i = 0; i < _config.PerformanceTestIterations; i++)
            {
                var startMemory = GC.GetTotalMemory(false);
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                eventBus.Broadcast(new TestPerformanceEvent { Data = $"Test {i}" });

                stopwatch.Stop();
                var endMemory = GC.GetTotalMemory(false);

                metrics.AddSample(stopwatch.ElapsedMilliseconds, endMemory - startMemory);
            }

            return metrics;
        }

        private static PerformanceMetrics BenchmarkModulePerformance()
        {
            var metrics = new PerformanceMetrics();

            for (int i = 0; i < _config.PerformanceTestIterations / 10; i++) // 模块操作较重，减少迭代次数
            {
                var startMemory = GC.GetTotalMemory(false);
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // 创建和销毁模块
                var go = new GameObject("PerfTestFramework");
                var fm = go.AddComponent<CnoomFramework.Core.FrameworkManager>();
                var module = new TestPerformanceModule();
                
                fm.RegisterModule(module);
                fm.Initialize();
                fm.Shutdown();
                
                UnityEngine.Object.DestroyImmediate(go);

                stopwatch.Stop();
                var endMemory = GC.GetTotalMemory(false);

                metrics.AddSample(stopwatch.ElapsedMilliseconds, endMemory - startMemory);
            }

            return metrics;
        }

        private static PerformanceMetrics BenchmarkConfigPerformance()
        {
            var metrics = new PerformanceMetrics();
            var configManager = new CnoomFramework.Core.Config.ConfigManager();

            for (int i = 0; i < _config.PerformanceTestIterations; i++)
            {
                var startMemory = GC.GetTotalMemory(false);
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // 配置读写操作
                configManager.SetValue($"perf.test.key.{i}", $"value_{i}");
                var value = configManager.GetValue<string>($"perf.test.key.{i}");

                stopwatch.Stop();
                var endMemory = GC.GetTotalMemory(false);

                metrics.AddSample(stopwatch.ElapsedMilliseconds, endMemory - startMemory);
            }

            return metrics;
        }

        #endregion
    }

    #region 性能测试数据类

    public class TestPerformanceEvent
    {
        public string Data { get; set; }
    }

    public class TestPerformanceModule : CnoomFramework.Core.BaseModule
    {
        protected override void OnInit()
        {
            // 模拟一些初始化工作
            System.Threading.Thread.Sleep(1);
        }
    }

    #endregion
}