using UnityEditor;
using UnityEngine;
using Funder.Core.Services;
using System.Linq;

namespace RogueDeal.Editor
{
    public static class UpdateAnalyticsListenerInBootstrap
    {
        [MenuItem("Funder Games/Rogue Deal/Analytics/Update Analytics Listener in Bootstrap")]
        public static void UpdateBootstrapConfig()
        {
            var configPath = "Assets/RogueDeal/Resources/Configs/BootstrapConfig_RogueDeal.asset";
            var config = AssetDatabase.LoadAssetAtPath<BootstrapConfig>(configPath);

            if (config == null)
            {
                Debug.LogError($"[UpdateAnalyticsListener] Bootstrap config not found at {configPath}");
                return;
            }

            if (config.services == null || config.services.Length == 0)
            {
                Debug.LogError("[UpdateAnalyticsListener] No services found in bootstrap config");
                return;
            }

            ServiceRegistryEntry analyticsListener = null;
            foreach (var service in config.services)
            {
                service.ResolveTypes();
                if (service.InterfaceType != null && 
                    service.InterfaceType.FullName == "Funder.Core.Analytics.IAnalyticsEventListener")
                {
                    analyticsListener = service;
                    break;
                }
            }

            if (analyticsListener == null)
            {
                Debug.LogError("[UpdateAnalyticsListener] Analytics listener service not found in bootstrap config");
                return;
            }

            var newScriptPath = "Assets/RogueDeal/Scripts/Analytics/RogueDealAnalyticsListener.cs";
            var newScript = AssetDatabase.LoadAssetAtPath<MonoScript>(newScriptPath);

            if (newScript == null)
            {
                Debug.LogError($"[UpdateAnalyticsListener] Could not find script at {newScriptPath}");
                return;
            }

            var oldImplementationName = analyticsListener.GetImplementationTypeName();
            
            analyticsListener.implementationScript = newScript;
            analyticsListener.ResolveTypes();

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[UpdateAnalyticsListener] ✅ Updated analytics listener from {oldImplementationName} to RogueDealAnalyticsListener");
            Debug.Log($"[UpdateAnalyticsListener] Bootstrap config updated successfully!");
            Debug.Log($"[UpdateAnalyticsListener] Please restart Play mode for changes to take effect.");
        }
    }
}
