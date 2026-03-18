using System;
using System.Collections.Generic;

namespace Funder.Core.Events
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _events = new Dictionary<Type, Delegate>();
        private static readonly object _lock = new object();

        public static void Subscribe<T>(Action<T> handler) where T : IEvent
        {
            if (handler == null)
            {
                return;
            }

            Type eventType = typeof(T);
            lock (_lock)
            {
                if (_events.TryGetValue(eventType, out Delegate current))
                {
                    _events[eventType] = Delegate.Combine(current, handler);
                }
                else
                {
                    _events[eventType] = handler;
                }
            }
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : IEvent
        {
            if (handler == null)
            {
                return;
            }

            Type eventType = typeof(T);
            lock (_lock)
            {
                if (!_events.TryGetValue(eventType, out Delegate current))
                {
                    return;
                }

                Delegate updated = Delegate.Remove(current, handler);
                if (updated == null)
                {
                    _events.Remove(eventType);
                }
                else
                {
                    _events[eventType] = updated;
                }
            }
        }

        public static void Publish<T>(T eventData) where T : IEvent
        {
            Delegate handler;
            lock (_lock)
            {
                _events.TryGetValue(typeof(T), out handler);
            }

            (handler as Action<T>)?.Invoke(eventData);
        }

        public static void Publish<T>() where T : IEvent, new()
        {
            Publish(new T());
        }

        public static void Clear<T>() where T : IEvent
        {
            lock (_lock)
            {
                _events.Remove(typeof(T));
            }
        }

        public static void ClearAll()
        {
            lock (_lock)
            {
                _events.Clear();
            }
        }

        public static bool HasSubscribers<T>() where T : IEvent
        {
            lock (_lock)
            {
                return _events.ContainsKey(typeof(T));
            }
        }

        public static int GetSubscriberCount<T>() where T : IEvent
        {
            lock (_lock)
            {
                if (_events.TryGetValue(typeof(T), out Delegate handler))
                {
                    return handler?.GetInvocationList().Length ?? 0;
                }

                return 0;
            }
        }
    }
}
