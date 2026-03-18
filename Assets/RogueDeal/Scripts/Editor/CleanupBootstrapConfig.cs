using UnityEditor;
using UnityEngine;

namespace RogueDeal.Editor
{
    /// <summary>
    /// Legacy migration menu. BootstrapConfig is no longer used; services are registered
    /// by GameBootstrap (Funder.Core.Services) in the Entry scene.
    /// </summary>
    public static class CleanupBootstrapConfig
    {
        [MenuItem("Funder Games/Rogue Deal/Migration/6. Remove Example Service from Config")]
        public static void RemoveExampleService()
        {
            EditorUtility.DisplayDialog("No Longer Used",
                "BootstrapConfig has been replaced by GameBootstrap.\n\n" +
                "Services (IEventBus, ILoggingService, etc.) are now registered in code by the GameBootstrap component in your Entry scene.\n\n" +
                "No config cleanup is needed.", "OK");
        }
    }
}
