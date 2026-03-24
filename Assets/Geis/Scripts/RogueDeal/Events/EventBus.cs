using System;
using Funder.Core.Services;
using Funder.Core.Events;

namespace RogueDeal.Events
{
    public static class EventBus<TEvent> where TEvent : IEvent
    {
        private static IEventBus _cachedInstance;
        private static bool _loggedError = false;

        private static IEventBus Instance
        {
            get
            {
                if (_cachedInstance != null)
                    return _cachedInstance;

                if (GameBootstrap.ServiceLocator == null)
                {
                    if (!_loggedError)
                    {
                        UnityEngine.Debug.LogError("[EventBus] ServiceLocator not initialized. Make sure GameBootstrap is running and you started from the Entry scene.");
                        _loggedError = true;
                    }
                    return null;
                }

                try
                {
                    _cachedInstance = GameBootstrap.ServiceLocator.Resolve<IEventBus>();
                    _loggedError = false;
                    return _cachedInstance;
                }
                catch (InvalidOperationException)
                {
                    if (!_loggedError)
                    {
                        UnityEngine.Debug.LogError("[EventBus] IEventBus service is not registered in ServiceLocator. This usually means the GameBootstrap hasn't finished initializing yet, or you're loading the Combat scene directly instead of from Entry scene.");
                        _loggedError = true;
                    }
                    return null;
                }
            }
        }

        public static IDisposable Subscribe(Action<TEvent> handler, int priority = 0)
        {
            var instance = Instance;
            if (instance == null)
                return null;
                
            return instance.Subscribe(handler, priority);
        }

        public static void Unsubscribe(Action<TEvent> handler)
        {
        }

        public static void Raise(TEvent evt)
        {
            Instance?.Publish(evt);
        }
    }
}
