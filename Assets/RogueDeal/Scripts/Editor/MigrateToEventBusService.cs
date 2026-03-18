using UnityEngine;
using UnityEditor;
using Funder.Core.Services;

namespace RogueDeal.Editor
{
    public static class MigrateToEventBusService
    {
        [MenuItem("Funder Games/Rogue Deal/Migration/1. Update EventBus to EventBusService")]
        public static void MigrateEventBus()
        {
            var config = Resources.Load<BootstrapConfig>("Configs/BootstrapConfig_RogueDeal");
            
            if (config == null)
            {
                Debug.LogError("Could not find BootstrapConfig_RogueDeal in Resources/Configs");
                return;
            }

            var interfaceScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Packages/com.funder.core/Runtime/EventBus/Interfaces/IEventBus.cs");
            
            var implementationScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Packages/com.funder.core/Runtime/EventBus/EventBusService.cs");

            if (interfaceScript == null || implementationScript == null)
            {
                Debug.LogError("Could not find EventBus scripts. Make sure the Funder Core package is installed.");
                Debug.LogError($"Interface found: {interfaceScript != null}");
                Debug.LogError($"Implementation found: {implementationScript != null}");
                return;
            }

            bool updated = false;

            var serializedObject = new SerializedObject(config);
            var servicesProperty = serializedObject.FindProperty("services");

            for (int i = 0; i < servicesProperty.arraySize; i++)
            {
                var serviceEntry = servicesProperty.GetArrayElementAtIndex(i);
                var interfaceScriptProp = serviceEntry.FindPropertyRelative("interfaceScript");
                var implementationScriptProp = serviceEntry.FindPropertyRelative("implementationScript");

                var currentInterface = interfaceScriptProp.objectReferenceValue as MonoScript;
                var currentImplementation = implementationScriptProp.objectReferenceValue as MonoScript;

                if (currentImplementation != null && currentImplementation.name == "SimpleEventBus")
                {
                    Debug.Log($"Found SimpleEventBus at index {i}, updating to EventBusService...");
                    
                    interfaceScriptProp.objectReferenceValue = interfaceScript;
                    implementationScriptProp.objectReferenceValue = implementationScript;
                    
                    updated = true;
                    Debug.Log($"✅ Updated entry {i}: Interface={interfaceScript.name}, Implementation={implementationScript.name}");
                }
            }

            if (updated)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                Debug.Log("════════════════════════════════════════");
                Debug.Log("✅ MIGRATION COMPLETE!");
                Debug.Log("════════════════════════════════════════");
                Debug.Log("BootstrapConfig now uses EventBusService (modern system)");
                Debug.Log("");
                Debug.Log("Next steps:");
                Debug.Log("1. Use menu: Funder → Rogue Deal → Migration → 2. Verify Configuration");
                Debug.Log("2. Press Play from Entry scene to test");
                Debug.Log("════════════════════════════════════════");
            }
            else
            {
                Debug.LogWarning("No SimpleEventBus entry found in config.");
                Debug.LogWarning("Migration may have already been completed or config is using a different setup.");
                Debug.Log("Use 'Verify EventBus Configuration' to check current state.");
            }
        }

        [MenuItem("Funder Games/Rogue Deal/Migration/2. Verify EventBus Configuration")]
        public static void VerifyEventBusConfig()
        {
            var config = Resources.Load<BootstrapConfig>("Configs/BootstrapConfig_RogueDeal");
            
            if (config == null)
            {
                Debug.LogError("Could not find BootstrapConfig_RogueDeal");
                return;
            }

            var serializedObject = new SerializedObject(config);
            var servicesProperty = serializedObject.FindProperty("services");

            Debug.Log("════════════════════════════════════════");
            Debug.Log("BOOTSTRAP CONFIGURATION");
            Debug.Log("════════════════════════════════════════");
            
            bool usingModernEventBus = false;
            bool hasIssues = false;
            
            for (int i = 0; i < servicesProperty.arraySize; i++)
            {
                var serviceEntry = servicesProperty.GetArrayElementAtIndex(i);
                var interfaceScriptProp = serviceEntry.FindPropertyRelative("interfaceScript");
                var implementationScriptProp = serviceEntry.FindPropertyRelative("implementationScript");
                var orderProp = serviceEntry.FindPropertyRelative("order");

                var interfaceScript = interfaceScriptProp.objectReferenceValue as MonoScript;
                var implementationScript = implementationScriptProp.objectReferenceValue as MonoScript;

                string interfaceName = interfaceScript != null ? interfaceScript.name : "NULL";
                string implementationName = implementationScript != null ? implementationScript.name : "NULL";

                string status = "";
                if (implementationScript == null)
                {
                    status = " ❌ MISSING IMPLEMENTATION";
                    hasIssues = true;
                }
                else if (implementationName == "EventBusService")
                {
                    status = " ✅ MODERN";
                    usingModernEventBus = true;
                }
                else if (implementationName == "SimpleEventBus")
                {
                    status = " ⚠️  LEGACY - Run migration!";
                    hasIssues = true;
                }
                else if (interfaceName.Contains("Example"))
                {
                    status = " ⚠️  EXAMPLE SERVICE - Should be removed";
                    hasIssues = true;
                }

                Debug.Log($"[{i}] Order: {orderProp.intValue}, Interface: {interfaceName}, Implementation: {implementationName}{status}");
            }
            
            Debug.Log("════════════════════════════════════════");
            
            if (hasIssues)
            {
                Debug.LogWarning("⚠️  Configuration has issues!");
                Debug.LogWarning("Run: Funder → Rogue Deal → Migration → 6. Remove Example Service from Config");
            }
            else if (usingModernEventBus)
            {
                Debug.Log("✅ Configuration is correct!");
                Debug.Log("Using modern EventBusService from Funder.Core.Events");
                Debug.Log("You're ready to test in Play mode!");
            }
            else
            {
                Debug.LogWarning("⚠️  Still using legacy SimpleEventBus");
                Debug.LogWarning("Run: Funder → Rogue Deal → Migration → 1. Update EventBus to EventBusService");
            }
            
            Debug.Log("════════════════════════════════════════");
        }

        [MenuItem("Funder Games/Rogue Deal/Migration/3. Open Migration Guide")]
        public static void OpenMigrationGuide()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/RogueDeal/EVENTBUS_MIGRATION_COMPLETE.md");
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
                Debug.Log("Migration guide selected in Project window. Check the Inspector!");
            }
            else
            {
                Debug.LogError("Could not find EVENTBUS_MIGRATION_COMPLETE.md");
            }
        }
    }
}
