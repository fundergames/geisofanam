using UnityEditor;
using UnityEngine;

namespace RogueDeal.Editor
{
    /// <summary>
    /// Legacy migration menu. BootstrapConfig is no longer used; GameBootstrap
    /// registers IAnalyticsService in code. Attach RogueDealAnalyticsListener to a
    /// GameObject in your bootstrap/entry scene if you need analytics.
    /// </summary>
    public static class UpdateAnalyticsListenerInBootstrap
    {
        [MenuItem("Funder Games/Rogue Deal/Analytics/Update Analytics Listener in Bootstrap")]
        public static void UpdateBootstrapConfig()
        {
            EditorUtility.DisplayDialog("No Longer Used",
                "BootstrapConfig has been replaced by GameBootstrap.\n\n" +
                "To use analytics, ensure:\n" +
                "1. GameBootstrap is in your Entry scene (registers IAnalyticsService).\n" +
                "2. RogueDealAnalyticsListener is on a GameObject in the scene and will resolve services at runtime.", "OK");
        }
    }
}
