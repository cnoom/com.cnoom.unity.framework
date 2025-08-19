using UnityEngine;

namespace CnoomFramework.Core.Performance
{
    /// <summary>
    ///     性能监控组件，负责在Unity的Update循环中更新性能监控模块
    /// </summary>
    [AddComponentMenu("")]
    internal class PerformanceMonitorComponent : MonoBehaviour
    {
        private PerformanceMonitorModule _module;

        private void Update()
        {
            if (_module != null) _module.Update();
        }

        /// <summary>
        ///     初始化组件
        /// </summary>
        /// <param name="module">性能监控模块</param>
        public void Initialize(PerformanceMonitorModule module)
        {
            _module = module;
        }
    }
}