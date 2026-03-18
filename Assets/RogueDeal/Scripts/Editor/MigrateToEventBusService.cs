using UnityEngine;
using UnityEditor;
using Funder.Core.Services;

namespace RogueDeal.Editor
{
    /// <summary>
    /// Legacy migration menus. BootstrapConfig is no longer used; GameBootstrap
    /// (Funder.Core.Services) registers IEventBus (EventBusService) in code.
    /// </summary>
    public static class MigrateToEventBusService
    {
        [MenuItem("Funder Games/Rogue Deal/Migration/1. Update EventBus to EventBusService")]
        public static void MigrateEventBus()
        {
            Debug.Log("[Migration] BootstrapConfig is no longer used. GameBootstrap in the Entry scene registers EventBusService automatically.");
            EditorUtility.DisplayDialog("No Longer Used",
                "BootstrapConfig has been replaced by GameBootstrap.\n\n" +
                "IEventBus (EventBusService) is now registered in code by the GameBootstrap component in your Entry scene. No migration needed.", "OK");
        }

        [MenuItem("Funder Games/Rogue Deal/Migration/2. Verify EventBus Configuration")]
        public static void VerifyEventBusConfig()
        {
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            if (bootstrap != null)
            {
                Debug.Log("✅ GameBootstrap found in scene. IEventBus is registered at runtime.");
                EditorUtility.DisplayDialog("Configuration OK", "GameBootstrap is in the scene. IEventBus (EventBusService) is registered at runtime.", "OK");
            }
            else
            {
                Debug.LogWarning("GameBootstrap not found in open scene. Add it to your Entry scene.");
                EditorUtility.DisplayDialog("Add GameBootstrap",
                    "GameBootstrap was not found in the current scene.\n\n" +
                    "Add a GameObject with the GameBootstrap component to your Entry (first) scene so IEventBus and other services are registered.", "OK");
            }
        }

        [MenuItem("Funder Games/Rogue Deal/Migration/3. Open Migration Guide")]
        public static void OpenMigrationGuide()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/RogueDeal/EVENTBUS_MIGRATION_COMPLETE.md");
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
                Debug.Log("Migration guide selected in Project window.");
            }
            else
            {
                Debug.Log("EVENTBUS_MIGRATION_COMPLETE.md not found. Migration is complete; GameBootstrap is used now.");
            }
        }
    }
}
