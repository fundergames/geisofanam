using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Funder.Core.Singleton
{
    public class SingletonManager : MonoBehaviour
    {
        private static SingletonManager _instance;
        private readonly List<ISingleton> _registeredSingletons = new List<ISingleton>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_instance != null)
            {
                return;
            }

            GameObject go = new GameObject("FunderSingletonManager");
            _instance = go.AddComponent<SingletonManager>();
            DontDestroyOnLoad(go);
            _instance.InitializeAutoSingletons();
        }

        private void InitializeAutoSingletons()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    if (type.IsAbstract || !typeof(MonoBehaviour).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    SingletonAttribute attribute = type.GetCustomAttribute<SingletonAttribute>(true);
                    if (attribute == null || !attribute.Automatic)
                    {
                        continue;
                    }

                    PropertyInfo instanceProperty = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    if (instanceProperty == null)
                    {
                        continue;
                    }

                    try
                    {
                        object instance = instanceProperty.GetValue(null);
                        if (instance is ISingleton singleton)
                        {
                            RegisterSingleton(singleton);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[SingletonManager] Failed to initialize singleton {type.Name}: {e.Message}");
                    }
                }
            }
        }

        public void RegisterSingleton(ISingleton singleton)
        {
            if (singleton == null || _registeredSingletons.Contains(singleton))
            {
                return;
            }

            _registeredSingletons.Add(singleton);
        }

        public void UnregisterSingleton(ISingleton singleton)
        {
            if (singleton == null)
            {
                return;
            }

            _registeredSingletons.Remove(singleton);
        }

        private void OnApplicationQuit()
        {
            foreach (ISingleton singleton in _registeredSingletons.ToList())
            {
                singleton?.OnSingletonDestroy();
            }

            _registeredSingletons.Clear();
        }
    }
}
