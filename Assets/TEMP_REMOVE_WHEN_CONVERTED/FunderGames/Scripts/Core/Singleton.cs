using UnityEngine;

namespace FunderGames.Core
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = FindObjectOfType<T>();

                if (_instance != null) return _instance;
                var singletonObject = new GameObject(typeof(T).Name);
                _instance = singletonObject.AddComponent<T>();
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject); // Prevent multiple instances
            }
            else
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject); // Optional: Persist across scenes
            }
        }
    }
}