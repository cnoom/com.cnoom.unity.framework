using UnityEngine;

namespace CnoomFramework.Tests
{
    /// <summary>
    /// 简单的卡死诊断器 - 用于排查具体卡死位置
    /// </summary>
    public static class HangDiagnostic
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void DiagnoseHang()
        {
            Debug.Log("[HangDiagnostic] 开始卡死诊断");
            
            try
            {
                Debug.Log($"[HangDiagnostic] 环境检查: Time={Time.realtimeSinceStartup:F1}s, isPlaying={Application.isPlaying}, isEditor={Application.isEditor}");
                
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                Debug.Log($"[HangDiagnostic] 加载的程序集数量: {assemblies.Length}");
                
                var stackTrace = System.Environment.StackTrace;
                bool isTestEnv = stackTrace.Contains("NUnit") || 
                               stackTrace.Contains("TestRunner") ||
                               stackTrace.Contains("UnityTest");
                Debug.Log($"[HangDiagnostic] 检测到测试环境: {isTestEnv}");
                
                try
                {
                    var fmType = typeof(CnoomFramework.Core.FrameworkManager);
                    Debug.Log($"[HangDiagnostic] FrameworkManager类型可访问: {fmType != null}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[HangDiagnostic] FrameworkManager类型访问失败: {ex.Message}");
                }
                
                Debug.Log("[HangDiagnostic] 诊断完成");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HangDiagnostic] 诊断异常: {ex.Message}");
            }
        }
    }
}