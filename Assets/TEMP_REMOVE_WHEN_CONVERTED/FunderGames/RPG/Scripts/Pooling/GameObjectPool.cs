using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

public class GameObjectPool
{
    private readonly ObjectPool<GameObject> _pool;
    private readonly Transform _root;
    private AsyncOperationHandle<GameObject> _templateHandle;
    private GameObject prefabTemplate;
    private readonly Func<GameObject> _createFunc;
    private readonly Action<GameObject> _actionOnGet;
    private readonly Action<GameObject> _actionOnRelease;
    private readonly Action<GameObject> _actionOnDestroy;
    private readonly string _poolId;

    private static readonly Dictionary<GameObject, GameObjectPool> loadedAssets = new();

    private GameObjectPool(string poolId, GameObject prefab, Transform parent = null,
        Func<GameObject> createFunc = null, Action<GameObject> actionOnGet = null,
        Action<GameObject> actionOnRelease = null, Action<GameObject> actionOnDestroy = null,
        bool collectionCheck = true, int defaultCapacity = 10, int maxSize = 10000)
    {
        prefabTemplate = prefab;
        _createFunc = createFunc;
        _actionOnGet = actionOnGet;
        _actionOnRelease = actionOnRelease;
        _actionOnDestroy = actionOnDestroy;
        _poolId = poolId;
        _root = parent.gameObject.transform;

        _pool = new ObjectPool<GameObject>(CreateFunc, ActionOnGet, ActionOnRelease, ActionOnDestroy, collectionCheck, defaultCapacity, maxSize);
    }
    
    public static async Task<GameObjectPool> CreatePool(string poolId, AssetReference template, Transform parent = null,
        Func<GameObject> createFunc = null, Action<GameObject> actionOnGet = null, 
        Action<GameObject> actionOnRelease = null, Action<GameObject> actionOnDestroy = null,
        bool collectionCheck = true, int defaultCapacity = 10, int maxSize = 10000)
    {
        if (template.OperationHandle.IsValid()) return null;
        var handle = template.LoadAssetAsync<GameObject>();
        await handle.Task;
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Addressables.Release(handle);
            return null;
        }

        var prefab = template.Asset != null ? template.Asset as GameObject : template.editorAsset as GameObject;
        var gop = CreatePool(poolId, prefab, parent, createFunc, actionOnGet, actionOnRelease, actionOnDestroy, collectionCheck,
            defaultCapacity, maxSize);
        
        return gop;
    }

    public static GameObjectPool CreatePool(string poolId, GameObject prefab, Transform parent = null,
        Func<GameObject> createFunc = null, Action<GameObject> actionOnGet = null, 
        Action<GameObject> actionOnRelease = null, Action<GameObject> actionOnDestroy = null,
        bool collectionCheck = true, int defaultCapacity = 10, int maxSize = 10000)
    {
        if (loadedAssets.TryGetValue(prefab, out var pool) && !prefab)
        {
            return pool;
        }

        var gop = new GameObjectPool(poolId, prefab, parent, createFunc, actionOnGet, actionOnRelease, actionOnDestroy, collectionCheck, Mathf.Min(maxSize, defaultCapacity), maxSize);
        loadedAssets.Add(prefab, gop);
        return gop;
    }
    
    public GameObject Get(Transform rootObj = null)
    {
        var obj = _pool?.Get();
        // if(obj != null && rootObj != null) obj.transform.SetParent(rootObj);
        if(obj != null) obj.transform.SetParent(rootObj);
        return obj;
    }

    public void Release(PooledObject obj)
    {
        obj.name = "Unused: " + (_poolId);
        // obj.transform.SetParent(_root.transform);
        // obj.gameObject.SetActive(false);
        _pool.Release(obj.gameObject);
    }

    private GameObject CreateFunc()
    {
        var go = Object.Instantiate(prefabTemplate, _root.transform);
        var poolComponent = !go.TryGetComponent<PooledObject>(out var comp) ? go.AddComponent<PooledObject>() : comp;
        poolComponent.SetPool(this);
        return go;
    }
    
    private void ActionOnGet(GameObject obj)
    {
        _actionOnGet?.Invoke(obj);
        obj.name = "Used: " + (_poolId);
        // obj.SetActive(true);
    }
    
    private void ActionOnRelease(GameObject obj)
    {
        _actionOnRelease?.Invoke(obj);
        obj.SetActive(false);
        obj.name = "Unused: " + (_poolId);
        obj.transform.SetParent(_root);
    }
    
    private void ActionOnDestroy(GameObject obj)
    {
        obj.TryGetComponent<PooledObject>(out var comp);
        comp.SetPool(null);
        _actionOnDestroy?.Invoke(obj);
        Object.Destroy(obj);
    }
}