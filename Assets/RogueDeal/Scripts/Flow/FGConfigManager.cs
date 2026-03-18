using Funder.Core.Services;
using UnityEngine;

namespace Funder.GameFlow
{
    public static class FGConfigManager
    {
        private static FGAppConfig _cachedConfig;

        public static void SetConfig(FGAppConfig config)
        {
            _cachedConfig = config;
            if (config != null)
            {
                Debug.Log($"[FGConfigManager] Using config: {config.name}");
            }
        }

        public static FGAppConfig GetConfig()
        {
            if (_cachedConfig != null)
            {
                return _cachedConfig;
            }

            var searchPaths = new[]
            {
                "Configs/AppConfig_RogueDeal",
                "FunderCore/FGAppConfig",
                "Configs/FGAppConfig"
            };

            foreach (var path in searchPaths)
            {
                _cachedConfig = Resources.Load<FGAppConfig>(path);
                if (_cachedConfig != null)
                {
                    Debug.Log($"[FGConfigManager] Auto-detected config at: Resources/{path}");
                    return _cachedConfig;
                }
            }

            Debug.LogError("[FGConfigManager] No FGAppConfig found! Searched paths:\n" +
                string.Join("\n", searchPaths));
            return null;
        }

        public static void ClearCache()
        {
            _cachedConfig = null;
        }
    }
}
