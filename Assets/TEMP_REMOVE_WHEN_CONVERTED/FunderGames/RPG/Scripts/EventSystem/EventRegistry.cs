using System;
using System.Collections.Generic;

public static class EventRegistry
{
    private static readonly Dictionary<Type, List<Action<object>>> EventHandlers = new();

    public static void Register<T>(Action<T> handler)
    {
        var eventType = typeof(T);
        if (!EventHandlers.ContainsKey(eventType))
        {
            EventHandlers[eventType] = new List<Action<object>>();
        }

        EventHandlers[eventType].Add(obj => handler((T)obj));
    }

    public static void Unregister<T>(Action<T> handler)
    {
        var eventType = typeof(T);
        if (EventHandlers.ContainsKey(eventType))
        {
            EventHandlers[eventType].Remove(obj => handler((T)obj));
        }
    }

    public static void Trigger<T>(T eventData)
    {
        var eventType = typeof(T);
        if (!EventHandlers.ContainsKey(eventType)) return;
        foreach (var handler in EventHandlers[eventType])
        {
            handler(eventData);
        }
    }
}