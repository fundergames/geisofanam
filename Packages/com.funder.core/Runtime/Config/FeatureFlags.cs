using UnityEngine;

namespace Funder.Core.Config
{
    /// <summary>
    /// ScriptableObject for feature toggles. Load from Resources (e.g. Configs/FeatureFlags).
    /// </summary>
    public class FeatureFlags : ScriptableObject
    {
        [Header("Features")]
        public bool EnableAudio = true;
        public bool EnableAnalytics = true;
        public bool EnableAchievements = false;
        public bool EnableDiagnosticHUD = false;
        public bool EnableDevConsole = true;
        public bool EnableTimeOverlay = true;
        public bool EnableGameHUD = true;
    }
}
