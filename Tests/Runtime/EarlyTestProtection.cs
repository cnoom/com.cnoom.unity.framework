using UnityEngine;

namespace CnoomFramework.Tests
{
    /// <summary>
    /// 最早期的测试保护 - 在任何其他代码之前运行
    /// </summary>
    public static class EarlyTestProtection
    {
        private static bool _hasProtectionStarted = false;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void StartEarlyProtection()
        {
            if (_hasProtectionStarted) return;
            _hasProtectionStarted = true;
            
            Debug.Log("[EarlyTestProtection] 启动早期保护");
            
            try
            {
                var stackTrace = System.Environment.StackTrace;
                bool isTestEnv = stackTrace.Contains("NUnit") || 
                               stackTrace.Contains("TestRunner") ||
                               stackTrace.Contains("UnityTest") ||
                               Application.productName.Contains("Test");
                               
                Debug.Log($"[EarlyTestProtection] 检测到测试环境: {isTestEnv}");
                
                if (isTestEnv)
                {
                    Debug.Log("[EarlyTestProtection] 设置安全模式");
                    Application.runInBackground = true;
                    CreateHangMonitor();
                }
                
                Debug.Log("[EarlyTestProtection] 早期保护设置完成");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[EarlyTestProtection] 早期保护设置失败: {ex.Message}");
            }
        }
        
        private static void CreateHangMonitor()
        {
            var go = new GameObject("EarlyHangMonitor");
            var monitor = go.AddComponent<EarlyHangMonitor>();
            Object.DontDestroyOnLoad(go);
        }
        
        /// <summary>
        /// 强制重置保护状态（仅用于测试）
        /// </summary>
        public static void ForceReset()
        {
            _hasProtectionStarted = false;
        }
    }
    
    /// <summary>
    /// 早期卡死监控器
    /// </summary>
    internal class EarlyHangMonitor : MonoBehaviour
    {
        private float _startTime;
        private int _heartbeat = 0;
        
        private void Start()
        {
            _startTime = Time.realtimeSinceStartup;
            InvokeRepeating(nameof(Heartbeat), 1f, 1f);
        }
        
        private void Heartbeat()
        {
            _heartbeat++;
            
            if (_heartbeat % 10 == 0) // 每10秒输出一次
            {
                var elapsed = Time.realtimeSinceStartup - _startTime;
                Debug.Log($"[EarlyHangMonitor] 运行时间: {elapsed:F1}s");
                
                if (elapsed > 60f)
                {
                    Debug.LogError("[EarlyHangMonitor] 检测到长时间运行，可能发生卡死，强制停止");
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#endif
                }
            }
        }
    }
}