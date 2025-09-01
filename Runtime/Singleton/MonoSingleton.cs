﻿﻿﻿using System;
using UnityEngine;

namespace CnoomFrameWork.Singleton
{
    /// <summary>
    ///     The basic MonoBehaviour singleton implementation, this singleton is destroyed after scene changes, use
    ///     <see cref="PersistentMonoSingleton{T}" /> if you want a persistent and global singleton instance.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class MonoSingleton<T> : MonoBehaviour, ISingleton where T : MonoSingleton<T>
    {
        #region Unity Messages

        /// <summary>
        ///     Use this for initialization.
        /// </summary>
        protected virtual void Awake()
        {
            Debug.Log($"[MonoSingleton] {typeof(T).Name} Awake 被调用");
            
            if (instance == null)
            {
                Debug.Log($"[MonoSingleton] 设置 {typeof(T).Name} 为单例实例");
                instance = this as T;

                // Initialize existing instance
                Debug.Log($"[MonoSingleton] 开始初始化 {typeof(T).Name} 单例");
                InitializeSingleton();
                Debug.Log($"[MonoSingleton] {typeof(T).Name} 单例初始化完成");
            }
            else
            {
                Debug.Log($"[MonoSingleton] 检测到 {typeof(T).Name} 重复实例，即将销毁");
                // Destory duplicates
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
            }
        }

        #endregion

        #region Fields

        /// <summary>
        ///     The instance.
        /// </summary>
        private static T instance;

        /// <summary>
        ///     The initialization status of the singleton's instance.
        /// </summary>
        private SingletonInitializationStatus initializationStatus = SingletonInitializationStatus.None;

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.Log($"[MonoSingleton] {typeof(T).Name} 实例不存在，开始查找或创建");
                    
#if UNITY_6000
                    instance = FindAnyObjectByType<T>();
#else
                    instance = FindObjectOfType<T>();
#endif
                    if (instance == null)
                    {
                        Debug.Log($"[MonoSingleton] 未找到 {typeof(T).Name} 实例，创建新实例");
                        var obj = new GameObject();
                        obj.name = typeof(T).Name;
                        Debug.Log($"[MonoSingleton] GameObject 创建完成，即将添加组件");
                        instance = obj.AddComponent<T>();
                        Debug.Log($"[MonoSingleton] 组件添加完成，调用OnMonoSingletonCreated");
                        instance.OnMonoSingletonCreated();
                        Debug.Log($"[MonoSingleton] {typeof(T).Name} 实例创建完成");
                    }
                    else
                    {
                        Debug.Log($"[MonoSingleton] 找到现有 {typeof(T).Name} 实例");
                    }
                }

                return instance;
            }
        }

        /// <summary>
        ///     Gets whether the singleton's instance is initialized.
        /// </summary>
        public virtual bool IsInitialized => initializationStatus == SingletonInitializationStatus.Initialized;

        #endregion

        #region Protected Methods

        /// <summary>
        ///     This gets called once the singleton's instance is created.
        /// </summary>
        protected virtual void OnMonoSingletonCreated()
        {
        }

        protected virtual void OnInitializing()
        {
        }

        protected virtual void OnInitialized()
        {
        }

        #endregion

        #region Public Methods

        public virtual void InitializeSingleton()
        {
            Debug.Log($"[MonoSingleton] InitializeSingleton 被调用 - {typeof(T).Name}");
            
            if (initializationStatus != SingletonInitializationStatus.None) 
            {
                Debug.Log($"[MonoSingleton] {typeof(T).Name} 已初始化，跳过");
                return;
            }

            Debug.Log($"[MonoSingleton] 设置 {typeof(T).Name} 状态为Initializing");
            initializationStatus = SingletonInitializationStatus.Initializing;
            
            Debug.Log($"[MonoSingleton] 调用 {typeof(T).Name} OnInitializing");
            OnInitializing();
            
            Debug.Log($"[MonoSingleton] 设置 {typeof(T).Name} 状态为Initialized");
            initializationStatus = SingletonInitializationStatus.Initialized;
            
            Debug.Log($"[MonoSingleton] 调用 {typeof(T).Name} OnInitialized");
            OnInitialized();
            
            Debug.Log($"[MonoSingleton] {typeof(T).Name} InitializeSingleton 完成");
        }

        public virtual void ClearSingleton()
        {
        }

        public static void CreateInstance()
        {
            DestroyInstance();
            instance = Instance;
        }

        public static void DestroyInstance()
        {
            if (instance == null) return;

            instance.ClearSingleton();
            instance = default;
        }

        #endregion
    }
}