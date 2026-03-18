using UnityEngine;

namespace Funder.Core.Analytics
{
    /// <summary>
    /// No-op analytics service when no real analytics provider is registered.
    /// Optionally logs to Debug in development.
    /// </summary>
    public sealed class NoOpAnalyticsService : IAnalyticsService
    {
        private readonly bool _logToConsole;

        public NoOpAnalyticsService(bool logToConsole = false)
        {
            _logToConsole = logToConsole;
        }

        public void LogEvent(string eventName, params (string key, object value)[] parameters)
        {
            if (_logToConsole && parameters != null && parameters.Length > 0)
            {
                var parts = new System.Text.StringBuilder();
                foreach (var (k, v) in parameters)
                    parts.Append($"{k}={v}; ");
                Debug.Log($"[Analytics] {eventName} | {parts}");
            }
        }
    }
}
