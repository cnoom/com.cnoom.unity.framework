﻿using System;
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
            if (instance == null)
            {
                instance = this as T;

                // Initialize existing instance
                InitializeSingleton();
            }
            else
            {
                // Destory duplicates
                if (!_isQuitting)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
            }
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        #endregion

        #region Fields

        /// <summary>
        ///     The instance.
        /// </summary>
        private static T instance;

        private bool _isQuitting;

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
#if UNITY_6000
                    instance = FindAnyObjectByType<T>();
#else
                    instance = FindObjectOfType<T>();
#endif
                    if (instance == null)
                    {
                        var obj = new GameObject();
                        obj.name = typeof(T).Name;
                        instance = obj.AddComponent<T>();
                        instance.OnMonoSingletonCreated();
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
            if (initializationStatus != SingletonInitializationStatus.None) return;

            initializationStatus = SingletonInitializationStatus.Initializing;
            OnInitializing();
            initializationStatus = SingletonInitializationStatus.Initialized;
            OnInitialized();
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