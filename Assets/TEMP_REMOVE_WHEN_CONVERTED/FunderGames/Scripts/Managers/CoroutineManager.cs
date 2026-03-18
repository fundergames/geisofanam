using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CoroutineManager : MonoBehaviour
{
    private static CoroutineManager _instance;
    private readonly Dictionary<Coroutine, IEnumerator> _coroutineMap = new();

    public static CoroutineManager Instance
    {
        get
        {
            if (_instance != null) return _instance;
            _instance = new GameObject("CoroutineManager").AddComponent<CoroutineManager>();
            DontDestroyOnLoad(_instance.gameObject);
            return _instance;
        }
    }

    public static Coroutine ActivateCoroutine(IEnumerator routine, float delay = 0.0f)
    {
        var coroutine = Instance.StartCoroutine(Instance.ActivateCoroutineInternal(routine, delay));
        Instance._coroutineMap.Add(coroutine, routine);
        return coroutine;
    }
    
    public static Coroutine ActivateAction(Action<object[]> action, params object[] parameters)
    {
        return ActivateActionWithDelay(0, action, parameters);
    }
    
    public static Coroutine ActivateActionWithDelay(float delay, Action<object[]> delayedAction, params object[] parameters)
    {
        IEnumerator DelayedActionCoroutine()
        {
            yield return new WaitForSeconds(delay);
            delayedAction?.Invoke(parameters);
        }
        
        return ActivateCoroutine(DelayedActionCoroutine()); ;
    }
    
    private IEnumerator ActivateCoroutineInternal(IEnumerator routine, float delay = 0.0f)
    {
        yield return new WaitForSeconds(delay);
        yield return routine;
        _coroutineMap.Remove(_coroutineMap.FirstOrDefault(x => x.Value == routine).Key);
    }
    
    public static void DeactivateCoroutine(Coroutine coroutine)
    {
        if (!Instance._coroutineMap.ContainsKey(coroutine)) return;
        Instance.StopCoroutine(coroutine);
        Instance._coroutineMap.Remove(coroutine);
    }

    public static void DeactivateAllCoroutines()
    {
        foreach (var kvp in Instance._coroutineMap)
        {
            Instance.StopCoroutine(kvp.Key);
        }
        Instance._coroutineMap.Clear();
    }
}