using System;
using System.Collections.Generic;

namespace Funder.Core.Events
{
    /// <summary>
    /// Service implementation of IEventBus that delegates to the static EventBus.
    /// </summary>
    public sealed class EventBusService : IEventBus
    {
        public IDisposable Subscribe<T>(Action<T> handler, int priority = 0) where T : IEvent
        {
            EventBus.Subscribe(handler);
            return new SubscriptionToken<T>(handler);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : IEvent
        {
            EventBus.Unsubscribe(handler);
        }

        public void Publish<T>(T eventData) where T : IEvent
        {
            EventBus.Publish(eventData);
        }

        private sealed class SubscriptionToken<T> : IDisposable where T : IEvent
        {
            private Action<T> _handler;
            private bool _disposed;

            public SubscriptionToken(Action<T> handler)
            {
                _handler = handler;
            }

            public void Dispose()
            {
                if (_disposed) return;
                if (_handler != null)
                {
                    EventBus.Unsubscribe(_handler);
                    _handler = null;
                }
                _disposed = true;
            }
        }
    }
}
