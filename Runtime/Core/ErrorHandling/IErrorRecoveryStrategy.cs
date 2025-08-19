using System;

namespace CnoomFramework.Core.ErrorHandling
{
    /// <summary>
    ///     错误恢复策略接口
    /// </summary>
    public interface IErrorRecoveryStrategy
    {
        /// <summary>
        ///     尝试恢复错误
        /// </summary>
        /// <param name="exception">异常</param>
        /// <param name="context">上下文</param>
        /// <returns>恢复结果</returns>
        RecoveryResult TryRecover(Exception exception, object context);
    }

    /// <summary>
    ///     恢复结果
    /// </summary>
    public class RecoveryResult
    {
        /// <summary>
        ///     是否成功恢复
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        ///     恢复消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        ///     恢复数据
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        ///     创建成功结果
        /// </summary>
        public static RecoveryResult Success(string message = null, object data = null)
        {
            return new RecoveryResult
            {
                IsSuccess = true,
                Message = message ?? "Recovery successful",
                Data = data
            };
        }

        /// <summary>
        ///     创建失败结果
        /// </summary>
        public static RecoveryResult Failure(string message, object data = null)
        {
            return new RecoveryResult
            {
                IsSuccess = false,
                Message = message ?? "Recovery failed",
                Data = data
            };
        }
    }
}