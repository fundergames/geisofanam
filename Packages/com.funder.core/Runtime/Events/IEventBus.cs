using System;

namespace Funder.Core.Events
{
    public interface IEventBus
    {
        IDisposable Subscribe<T>(Action<T> handler, int priority = 0) where T : IEvent;
        void Unsubscribe<T>(Action<T> handler) where T : IEvent;
        void Publish<T>(T eventData) where T : IEvent;
    }
}
