using System;
using System.Collections.Generic;

namespace Funder.Core.Services
{
    public class ServiceLocator
    {
        private static readonly ServiceLocator _instance = new ServiceLocator();
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly object _lock = new object();

        public static ServiceLocator Instance => _instance;

        public void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            lock (_lock)
            {
                _services[typeof(T)] = service;
            }
        }

        public T Get<T>() where T : class
        {
            lock (_lock)
            {
                if (_services.TryGetValue(typeof(T), out object service))
                {
                    return service as T;
                }
            }

            return null;
        }

        /// <summary>Alias for Get&lt;T&gt; for compatibility with GameBootstrap usage.</summary>
        public T Resolve<T>() where T : class
        {
            return Get<T>();
        }

        /// <summary>Alias for TryGet for compatibility with GameBootstrap usage.</summary>
        public bool TryResolve<T>(out T service) where T : class
        {
            return TryGet(out service);
        }

        public bool TryGet<T>(out T service) where T : class
        {
            service = Get<T>();
            return service != null;
        }

        public bool Contains<T>() where T : class
        {
            lock (_lock)
            {
                return _services.ContainsKey(typeof(T));
            }
        }

        public void Unregister<T>() where T : class
        {
            lock (_lock)
            {
                _services.Remove(typeof(T));
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _services.Clear();
            }
        }
    }
}
