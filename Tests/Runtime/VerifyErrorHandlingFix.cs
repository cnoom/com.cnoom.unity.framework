using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CnoomFramework.Core.ErrorHandling;

namespace CnoomFramework.Tests
{
    /// <summary>
    /// 验证错误处理修复的简单测试
    /// </summary>
    public class VerifyErrorHandlingFix
    {
        private ErrorRecoveryManager _errorManager;

        [SetUp]
        public void SetUp()
        {
            _errorManager = new ErrorRecoveryManager();
            _errorManager.RegisterRecoveryStrategy<Exception>(new TestSafeRecoveryStrategy());
            SafeExecutor.SetErrorRecoveryManager(_errorManager);
        }

        [TearDown]
        public void TearDown()
        {
            SafeExecutor.SetErrorRecoveryManager(null);
            _errorManager = null;
        }

        [Test]
        [Timeout(5000)]
        public void VerifyAsyncExceptionHandling()
        {
            // 预期的日志序列
            LogAssert.Expect(LogType.Warning, "[CnoomFramework] Medium severity error: Test async exception");
            LogAssert.Expect(LogType.Log, "[CnoomFramework] Attempting recovery for Exception");
            LogAssert.Expect(LogType.Log, "[TestSafeRecoveryStrategy] Handling exception: Exception");
            LogAssert.Expect(LogType.Log, "[CnoomFramework] Successfully recovered from Exception: Test recovery success");

            Debug.Log("[VerifyErrorHandlingFix] 开始测试异步异常处理");

            // 执行测试
            var result = SafeExecutor.ExecuteWithResult(() =>
            {
                System.Threading.Thread.Sleep(10);
                throw new Exception("Test async exception");
            });

            // 验证结果
            Assert.IsFalse(result.IsSuccess);
            Assert.IsNotNull(result.Exception);

            Debug.Log("[VerifyErrorHandlingFix] 异步异常处理测试完成");
        }

        /// <summary>
        /// 测试用的简单恢复策略
        /// </summary>
        private class TestSafeRecoveryStrategy : IErrorRecoveryStrategy
        {
            public RecoveryResult TryRecover(Exception exception, object context = null)
            {
                Debug.Log($"[TestSafeRecoveryStrategy] Handling exception: {exception?.GetType().Name}");
                return RecoveryResult.Success("Test recovery success");
            }
        }
    }
}