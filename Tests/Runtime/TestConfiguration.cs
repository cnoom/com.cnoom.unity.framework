using System.Linq;
using UnityEngine;

namespace CnoomFramework.Tests
{
    /// <summary>
    /// 测试配置类，提供测试环境下的安全配置
    /// </summary>
    public static class TestConfiguration
    {
        /// <summary>
        /// 检查当前是否在Unity测试环境中
        /// </summary>
        public static bool IsInTestEnvironment
        {
            get
            {
                // 检查多种测试环境标识
                var stackTrace = System.Environment.StackTrace;
                return stackTrace.Contains("NUnit") || 
                       stackTrace.Contains("TestRunner") ||
                       stackTrace.Contains("UnityTest") ||
                       stackTrace.Contains("TestMethod") ||
                       Application.productName.Contains("Test") ||
                       Application.dataPath.Contains("Test") ||
                       System.AppDomain.CurrentDomain.GetAssemblies()
                           .Any(a => a.FullName.ToLower().Contains("nunit") || 
                                     a.FullName.ToLower().Contains("testrunner") ||
                                     a.FullName.ToLower().Contains("unityengine.testtools"));
            }
        }

        /// <summary>
        /// 检查是否应该跳过资源密集型测试
        /// </summary>
        public static bool ShouldSkipIntensiveTests =>
            IsInTestEnvironment && !Application.isEditor;

        /// <summary>
        /// 获取测试环境的超时时间（毫秒）
        /// </summary>
        public static int GetTestTimeout(int defaultTimeout = 5000)
        {
            return IsInTestEnvironment ? defaultTimeout * 2 : defaultTimeout;
        }

        /// <summary>
        /// 获取测试环境的安全循环次数
        /// </summary>
        public static int GetSafeIterationCount(int requestedCount)
        {
            if (IsInTestEnvironment)
            {
                // 在测试环境下限制循环次数
                return Mathf.Min(requestedCount, 100);
            }
            return requestedCount;
        }

        /// <summary>
        /// 检查是否应该跳过并发测试
        /// </summary>
        public static bool ShouldSkipConcurrencyTests =>
            IsInTestEnvironment && !Application.isEditor;

        /// <summary>
        /// 获取测试环境的安全线程数
        /// </summary>
        public static int GetSafeThreadCount(int requestedThreads)
        {
            if (IsInTestEnvironment)
            {
                // 在测试环境下限制线程数
                return Mathf.Min(requestedThreads, 3);
            }
            return requestedThreads;
        }
    }
}