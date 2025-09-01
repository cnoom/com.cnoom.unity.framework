using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace CnoomFramework.Tests
{
    /// <summary>
    /// 测试运行保护器，防止测试环境下的无限循环和卡死
    /// </summary>
    public static class TestRunProtector
    {
        private static readonly Stopwatch _testTimer = new Stopwatch();
        private static bool _hasStarted = false;
        private static float _lastLogTime = 0f;
        
        /// <summary>
        /// 开始测试保护
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void StartProtection()
        {
            if (TestConfiguration.IsInTestEnvironment && !_hasStarted)
            {
                _hasStarted = true;
                _testTimer.Start();
                _lastLogTime = Time.realtimeSinceStartup;
                Debug.Log("[TestRunProtector] 测试保护已启动");
                Debug.Log($"[TestRunProtector] 检测到测试环境，当前产品名: {Application.productName}");
                
                // 设置全局超时保护
                Application.runInBackground = true;
                
                // 启动周期性检查
                StartPeriodicCheck();
            }
        }
        
        /// <summary>
        /// 启动周期性检查
        /// </summary>
        private static void StartPeriodicCheck()
        {
            // 使用Unity的协程系统进行周期性检查
            var go = new GameObject("TestProtectorChecker");
            var checker = go.AddComponent<TestProtectorChecker>();
            checker.StartChecking();
        }
        
        /// <summary>
        /// 检查是否应该终止当前操作
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <returns>是否应该终止</returns>
        public static bool ShouldAbort(string operationName, int timeoutMs = 30000)
        {
            if (!TestConfiguration.IsInTestEnvironment) return false;
            
            if (_testTimer.ElapsedMilliseconds > timeoutMs)
            {
                Debug.LogError($"[TestRunProtector] 操作 '{operationName}' 超时 ({timeoutMs}ms)，强制终止");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 记录进度
        /// </summary>
        /// <param name="message">进度信息</param>
        public static void LogProgress(string message)
        {
            if (!TestConfiguration.IsInTestEnvironment) return;
            
            var currentTime = Time.realtimeSinceStartup;
            if (currentTime - _lastLogTime > 5f) // 每5秒记录一次
            {
                Debug.Log($"[TestRunProtector] 运行中: {message} (已运行 {_testTimer.ElapsedMilliseconds}ms)");
                _lastLogTime = currentTime;
            }
        }
        
        /// <summary>
        /// 重置计时器
        /// </summary>
        public static void ResetTimer()
        {
            _testTimer.Restart();
            _lastLogTime = Time.realtimeSinceStartup;
        }
        
        /// <summary>
        /// 停止保护
        /// </summary>
        public static void StopProtection()
        {
            if (_hasStarted)
            {
                _testTimer.Stop();
                _hasStarted = false;
                Debug.Log($"[TestRunProtector] 测试保护已停止，运行时间: {_testTimer.ElapsedMilliseconds}ms");
            }
        }
    }
    
    /// <summary>
    /// 测试保护检查器组件
    /// </summary>
    internal class TestProtectorChecker : MonoBehaviour
    {
        private float _nextCheckTime;
        private int _checkCount = 0;
        
        public void StartChecking()
        {
            _nextCheckTime = Time.realtimeSinceStartup + 2f;
            Debug.Log("[TestProtectorChecker] 开始周期性检查");
            
            // 立即执行第一次检查
            try
            {
                Debug.Log("[TestProtectorChecker] 执行初始诊断检查...");
                PerformDiagnosticCheck();
                Debug.Log("[TestProtectorChecker] 初始诊断检查完成");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TestProtectorChecker] 初始诊断检查失败: {ex.Message}");
                Debug.LogError($"详细异常: {ex}");
            }
        }
        
        private void PerformDiagnosticCheck()
        {
            Debug.Log($"[TestProtectorChecker] 当前时间: {Time.realtimeSinceStartup}");
            Debug.Log($"[TestProtectorChecker] 应用状态: isPlaying={Application.isPlaying}, isEditor={Application.isEditor}");
            Debug.Log($"[TestProtectorChecker] 产品名称: {Application.productName}");
            Debug.Log($"[TestProtectorChecker] 数据路径: {Application.dataPath}");
            
            // 检查FrameworkManager状态
            try
            {
                var fmInstance = CnoomFramework.Core.FrameworkManager.Instance;
                Debug.Log($"[TestProtectorChecker] FrameworkManager实例存在: {fmInstance != null}");
                if (fmInstance != null)
                {
                    Debug.Log($"[TestProtectorChecker] FrameworkManager已初始化: {fmInstance.IsInitialized}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TestProtectorChecker] FrameworkManager检查失败: {ex.Message}");
            }
        }
        
        private void Update()
        {
            try
            {
                if (Time.realtimeSinceStartup >= _nextCheckTime)
                {
                    _checkCount++;
                    Debug.Log($"[TestProtectorChecker] 第 {_checkCount} 次检查，测试仍在运行...");
                    
                    // 每次检查时输出更多诊断信息
                    if (_checkCount % 5 == 0) // 每10秒输出详细信息
                    {
                        PerformDiagnosticCheck();
                    }
                    
                    // 检查是否超时
                    if (_checkCount > 15) // 30秒后强制停止
                    {
                        Debug.LogError("[TestProtectorChecker] 测试运行超过30秒，可能发生卡死");
                        
                        // 尝试强制停止测试
#if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
#endif
                        return;
                    }
                    
                    _nextCheckTime = Time.realtimeSinceStartup + 2f;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TestProtectorChecker] Update异常: {ex.Message}");
                Debug.LogError($"详细异常: {ex}");
            }
        }
    }
}