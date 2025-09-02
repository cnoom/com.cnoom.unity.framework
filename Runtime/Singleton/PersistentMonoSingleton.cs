using UnityEngine;
using UnityEngine.Serialization;

namespace CnoomFrameWork.Singleton
{
    /// <summary>
    ///     This singleton is persistent across scenes by calling <see cref="UnityEngine.Object.DontDestroyOnLoad(Object)" />.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PersistentMonoSingleton<T> : MonoSingleton<T> where T : MonoSingleton<T>
    {
        /// <summary>
        ///     if this is true, this singleton will auto detach if it finds itself parented on awake
        /// </summary>
        [FormerlySerializedAs("UnparentOnAwake")]
        [Tooltip("if this is true, this singleton will auto detach if it finds itself parented on awake")]
        [SerializeField]
        private bool unparentOnAwake = true;

        /// <summary>
        ///     Static flag to track if application is quitting
        /// </summary>
        private static bool isApplicationQuitting = false;

        /// <summary>
        ///     The instance with application quit protection.
        /// </summary>
        public new static T Instance =>
            // 如果应用程序正在退出，返回null而不是创建新实例
            isApplicationQuitting ? null : MonoSingleton<T>.Instance; // 调用基类的Instance属性获取器

        #region Unity Messages

        /// <summary>
        ///     Called when the application is quitting
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
            isApplicationQuitting = true;
        }

        #endregion

        #region Protected Methods

        protected override void OnInitializing()
        {
            if (unparentOnAwake) transform.SetParent(null);
            base.OnInitializing();
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        }

        #endregion
    }
}