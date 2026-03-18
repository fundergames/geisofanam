using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public static class PoolManager
{
    private static readonly Dictionary<string, dynamic> Pools = new Dictionary<string, dynamic>();
    private static readonly Dictionary<string, GameObject> RootPool = new Dictionary<string, GameObject>();

    public static void AddPool<T>(string poolId, 
        Func<T> createFunc = null, 
        Action<T> actionOnGet = null, 
        Action<T> actionOnRelease = null, 
        Action<T> actionOnDestroy = null,
        bool collectionCheck = true,
        int defaultCapacity = 10,
        int maxSize = 10000) where T : class, new()
    {
        if (!Pools.TryGetValue(poolId, out _))
        {
            if (createFunc == null)
            {
                if (typeof(T) == typeof(GameObject))
                {
                    var rootObj = new GameObject
                    {
                        name = $"PoolRoot_{poolId}"
                    };
                    RootPool.Add(poolId, rootObj);
                    createFunc = () =>
                    {
                        var obj = new GameObject();
                        obj.transform.SetParent(rootObj.transform);
                        return obj as T;
                    };
                }
                else
                    createFunc = CreateFunc<T>;
            }
            
            var pool = new ObjectPool<T>(createFunc, actionOnGet, actionOnRelease, actionOnDestroy, collectionCheck, defaultCapacity, maxSize);
            Pools.Add(poolId, pool);
            return;
        }
        Debug.Log($"Attempting to add pool named {poolId} failed because it already exists.");
    }

    public static ObjectPool<T> GetPool<T>(string poolId) where T : class
    {
        if (Pools.TryGetValue(poolId, out dynamic pool))
        {
            return pool as ObjectPool<T>;
        }
        Debug.Log($"Attempting to get pool named {poolId} failed because it doesn't exist.");
        return null;
    }

    public static T Get<T>(string poolId) where T : class
    {
        var pool = GetPool<T>(poolId);
        return pool?.Get();
    }
    
    public static void Release<T>(string poolId, T obj) where T : class
    {
        var pool = GetPool<T>(poolId);
        pool?.Release(obj);
    }

    private static T CreateFunc<T>() where T : class, new()
    {
        var obj = new T();
        return obj;
    }
}
