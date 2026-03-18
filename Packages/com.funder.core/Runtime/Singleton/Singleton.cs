using System;
using UnityEngine;

namespace Funder.Core.Singleton
{
    public abstract class Singleton<T> : MonoBehaviour, ISingleton where T : Singleton<T>
    {
        private static T _instance;
        private static bool _isApplicationQuitting;

        public static T Instance
        {
            get
            {
                if (_isApplicationQuitting)
                {
                    Debug.LogWarning($"[Singleton] {typeof(T).Name} access denied - application is quitting");
                    return null;
                }

                if (_instance == null || _instance.gameObject == null)
                {
                    if (_instance != null && _instance.gameObject == null)
                    {
                        Debug.LogWarning($"[Singleton] {typeof(T).Name} instance exists but GameObject is null - clearing reference");
                        _instance = null;
                    }

                    _instance = FindFirstObjectByType<T>();

                    if (_instance == null)
                    {
                        SingletonAttribute attribute = GetSingletonAttribute();

                        if (attribute != null && attribute.Automatic)
                        {
                            string objectName = string.IsNullOrEmpty(attribute.Name) ? typeof(T).Name : attribute.Name;
                            GameObject go = new GameObject(objectName);
                            _instance = go.AddComponent<T>();

                            if (attribute.Persistent)
                            {
                                DontDestroyOnLoad(go);
                            }

                            go.hideFlags = attribute.HideFlags;
                        }
                        else
                        {
                            Debug.LogError($"[Singleton] No instance of {typeof(T).Name} found and Automatic creation is disabled.");
                        }
                    }
                }

                return _instance;
            }
        }

        public static bool IsInstantiated => _instance != null;

        protected virtual void Awake()
        {
            _isApplicationQuitting = false;

            if (_instance != null && _instance != this)
            {
                SingletonAttribute attribute = GetSingletonAttribute();
                if (attribute != null && attribute.RemoveDuplicates)
                {
                    Debug.LogWarning($"[Singleton] Duplicate {typeof(T).Name} detected, destroying duplicate.");
                    Destroy(gameObject);
                    return;
                }
            }

            _instance = this as T;

            SingletonAttribute singletonAttribute = GetSingletonAttribute();
            if (singletonAttribute != null && singletonAttribute.Persistent)
            {
                DontDestroyOnLoad(gameObject);
            }

            OnSingletonAwake();
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                OnSingletonDestroy();
                _instance = null;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _isApplicationQuitting = true;
        }

        public virtual void OnSingletonAwake()
        {
        }

        public virtual void OnSingletonDestroy()
        {
        }

        private static SingletonAttribute GetSingletonAttribute()
        {
            Type type = typeof(T);
            object[] attributes = type.GetCustomAttributes(typeof(SingletonAttribute), true);
            return attributes.Length > 0 ? attributes[0] as SingletonAttribute : null;
        }
    }
}
