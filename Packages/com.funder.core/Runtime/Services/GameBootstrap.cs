using Funder.Core.Analytics;
using Funder.Core.Events;
using Funder.Core.Logging;
using UnityEngine;

namespace Funder.Core.Services
{
    /// <summary>
    /// Central bootstrap MonoBehaviour. Place in the first scene (e.g. Entry).
    /// Registers core services so Resolve&lt;IEventBus&gt;(), etc. work.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        private static bool _isInitialized;

        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Service locator for resolving IEventBus, ILoggingService, IRandomHub, etc.
        /// </summary>
        public static ServiceLocator ServiceLocator => ServiceLocator.Instance;

        private void Awake()
        {
            if (_isInitialized)
                return;

            var locator = ServiceLocator.Instance;
            locator.Register<IEventBus>(new EventBusService());
            locator.Register<ILoggingService>(new DebugLoggingService());
            locator.Register<IAnalyticsService>(new NoOpAnalyticsService(logToConsole: Application.isEditor));

            _isInitialized = true;
            Debug.Log("[GameBootstrap] Core services registered (IEventBus, ILoggingService, IAnalyticsService).");
        }
    }
}
