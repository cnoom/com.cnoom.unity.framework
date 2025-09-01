using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;

namespace CnoomFramework.Tests
{
    /// <summary>
    /// 测试套件验证器 - 验证测试环境和依赖是否正确设置
    /// </summary>
    public class TestSuiteValidator
    {
        [MenuItem("CnoomFramework/Validate Test Suite")]
        public static void ValidateTestSuite()
        {
            Debug.Log("🔍 开始验证 Cnoom Framework 测试套件...");
            
            bool allValid = true;
            
            // 验证程序集引用
            allValid &= ValidateAssemblyReferences();
            
            // 验证测试类和方法
            allValid &= ValidateTestClasses();
            
            // 验证测试配置
            allValid &= ValidateTestConfiguration();
            
            // 验证Unity Test Framework
            allValid &= ValidateUnityTestFramework();
            
            if (allValid)
            {
                Debug.Log("✅ 测试套件验证通过！所有组件都正确配置。");
                Debug.Log("💡 您可以通过 CnoomFramework → Run All Tests 来运行完整的测试套件。");
            }
            else
            {
                Debug.LogError("❌ 测试套件验证失败！请检查上述错误并修复。");
            }
        }

        private static bool ValidateAssemblyReferences()
        {
            Debug.Log("📦 验证程序集引用...");
            bool valid = true;
            
            try
            {
                // 验证核心框架程序集
                var frameworkAssembly = Assembly.GetAssembly(typeof(CnoomFramework.Core.FrameworkManager));
                if (frameworkAssembly == null)
                {
                    Debug.LogError("❌ 无法找到 CnoomFramework.Runtime 程序集");
                    valid = false;
                }
                else
                {
                    Debug.Log("✅ CnoomFramework.Runtime 程序集加载成功");
                }

                // 验证测试程序集
                var testAssemblyType = Type.GetType("CnoomFramework.Tests.EventBus.EventBusTests");
                if (testAssemblyType == null)
                {
                    Debug.LogError("❌ 无法找到测试程序集类型");
                    valid = false;
                }
                else
                {
                    Debug.Log("✅ 测试程序集类型加载成功");
                }

                // 验证NUnit引用
                var testAttributeType = Type.GetType("NUnit.Framework.TestAttribute, nunit.framework");
                if (testAttributeType == null)
                {
                    Debug.LogError("❌ 无法找到 NUnit.Framework");
                    valid = false;
                }
                else
                {
                    Debug.Log("✅ NUnit.Framework 引用正确");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ 程序集验证异常: {ex.Message}");
                valid = false;
            }
            
            return valid;
        }

        private static bool ValidateTestClasses()
        {
            Debug.Log("🧪 验证测试类...");
            bool valid = true;
            
            string[] testClasses = {
                "CnoomFramework.Tests.EventBus.EventBusTests",
                "CnoomFramework.Tests.Module.ModuleSystemTests", 
                "CnoomFramework.Tests.Config.ConfigManagerTests",
                "CnoomFramework.Tests.ErrorHandling.ErrorHandlingTests",
                "CnoomFramework.Tests.Performance.PerformanceMonitorTests",
                "CnoomFramework.Tests.Integration.IntegrationTests"
            };

            foreach (var className in testClasses)
            {
                try
                {
                    var type = Type.GetType(className);
                    if (type == null)
                    {
                        Debug.LogError($"❌ 找不到测试类: {className}");
                        valid = false;
                        continue;
                    }

                    // 检查测试方法
                    var methods = type.GetMethods();
                    int testMethodCount = 0;
                    
                    foreach (var method in methods)
                    {
                        var testAttributes = method.GetCustomAttributes(false);
                        foreach (var attr in testAttributes)
                        {
                            if (attr.GetType().Name == "TestAttribute")
                            {
                                testMethodCount++;
                                break;
                            }
                        }
                    }

                    if (testMethodCount > 0)
                    {
                        Debug.Log($"✅ {className}: 找到 {testMethodCount} 个测试方法");
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ {className}: 没有找到测试方法");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ 验证测试类 {className} 时出错: {ex.Message}");
                    valid = false;
                }
            }
            
            return valid;
        }

        private static bool ValidateTestConfiguration()
        {
            Debug.Log("⚙️ 验证测试配置...");
            
            // 尝试加载测试配置
            var config = Resources.Load<TestSuiteConfig>("TestSuiteConfig");
            if (config == null)
            {
                Debug.LogWarning("⚠️ 未找到测试配置文件，将使用默认配置");
                Debug.Log("💡 您可以创建 TestSuiteConfig 资源文件来自定义测试行为");
                return true; // 不是必须的，所以返回true
            }
            else
            {
                Debug.Log("✅ 测试配置文件加载成功");
                Debug.Log($"   - 性能测试: {(config.EnablePerformanceTests ? "启用" : "禁用")}");
                Debug.Log($"   - 集成测试: {(config.EnableIntegrationTests ? "启用" : "禁用")}");
                Debug.Log($"   - Editor测试: {(config.EnableEditorTests ? "启用" : "禁用")}");
                return true;
            }
        }

        private static bool ValidateUnityTestFramework()
        {
            Debug.Log("🎮 验证Unity Test Framework...");
            bool valid = true;

            try
            {
                // 检查Unity Test Runner是否可用
                var testRunnerType = Type.GetType("UnityEditor.TestTools.TestRunner.Api.TestRunnerApi, UnityEditor.TestRunner");
                if (testRunnerType == null)
                {
                    Debug.LogError("❌ Unity Test Runner 不可用");
                    valid = false;
                }
                else
                {
                    Debug.Log("✅ Unity Test Runner 可用");
                }

                // 检查PlayMode测试支持
                var playModeType = Type.GetType("UnityEngine.TestTools.UnityTestAttribute, UnityEngine.TestRunner");
                if (playModeType == null)
                {
                    Debug.LogError("❌ PlayMode 测试支持不可用");
                    valid = false;
                }
                else
                {
                    Debug.Log("✅ PlayMode 测试支持可用");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Unity Test Framework 验证异常: {ex.Message}");
                valid = false;
            }

            return valid;
        }

        [MenuItem("CnoomFramework/Test Environment Info")]
        public static void ShowTestEnvironmentInfo()
        {
            Debug.Log("📊 测试环境信息:");
            Debug.Log($"   Unity 版本: {Application.unityVersion}");
            Debug.Log($"   平台: {Application.platform}");
            Debug.Log($"   编辑器版本: {Application.version}");
            Debug.Log($"   .NET 版本: {System.Environment.Version}");
            
            // 显示内存信息
            long memory = GC.GetTotalMemory(false);
            Debug.Log($"   当前内存使用: {memory / 1024 / 1024:F1} MB");
            
            // 显示框架信息
            if (CnoomFramework.Core.FrameworkManager.Instance != null)
            {
                Debug.Log($"   框架状态: 已初始化");
                Debug.Log($"   注册模块数: {CnoomFramework.Core.FrameworkManager.Instance.ModuleCount}");
            }
            else
            {
                Debug.Log($"   框架状态: 未初始化");
            }
        }
    }
}