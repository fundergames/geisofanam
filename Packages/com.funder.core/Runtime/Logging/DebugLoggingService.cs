using UnityEngine;

namespace Funder.Core.Logging
{
    /// <summary>
    /// Default logging service that forwards to Unity Debug.
    /// </summary>
    public sealed class DebugLoggingService : ILoggingService
    {
        public void Debug(string category, string message)
        {
            UnityEngine.Debug.Log($"[{category}] {message}");
        }

        public void Info(string category, string message)
        {
            UnityEngine.Debug.Log($"[{category}] {message}");
        }

        public void Warning(string category, string message)
        {
            UnityEngine.Debug.LogWarning($"[{category}] {message}");
        }

        public void Error(string category, string message)
        {
            UnityEngine.Debug.LogError($"[{category}] {message}");
        }
    }
}
