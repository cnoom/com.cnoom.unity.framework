using UnityEngine;

namespace CnoomFramework.Core
{
    /// <summary>
    ///     框架初始化器，用于在Unity启动时自动初始化框架
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class FrameworkInitializer : MonoBehaviour
    {
        [SerializeField] private bool _autoInitialize = true;
        [SerializeField] private bool _enableDebugLog = true;

        private void Awake()
        {
            if (_autoInitialize)
            {
                // 确保FrameworkManager实例存在并初始化
                var frameworkManager = FrameworkManager.Instance;
                if (!frameworkManager.IsInitialized) frameworkManager.Initialize();
            }
        }

        /// <summary>
        ///     手动初始化框架
        /// </summary>
        [ContextMenu("Initialize Framework")]
        public void InitializeFramework()
        {
            var frameworkManager = FrameworkManager.Instance;
            if (!frameworkManager.IsInitialized)
                frameworkManager.Initialize();
            else
                Debug.LogWarning("Framework is already initialized.");
        }

        /// <summary>
        ///     关闭框架
        /// </summary>
        [ContextMenu("Shutdown Framework")]
        public void ShutdownFramework()
        {
            var frameworkManager = FrameworkManager.Instance;
            frameworkManager.Shutdown();
        }
    }
}