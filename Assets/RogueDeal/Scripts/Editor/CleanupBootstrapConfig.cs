using UnityEditor;
using UnityEngine;
using System.Linq;
using Funder.Core.Services;

namespace RogueDeal.Editor
{
    public static class CleanupBootstrapConfig
    {
        [MenuItem("Funder Games/Rogue Deal/Migration/6. Remove Example Service from Config")]
        public static void RemoveExampleService()
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
            Debug.Log("CLEANUP BOOTSTRAP CONFIG");
            Debug.Log("════════════════════════════════════════\n");

            int removedCount = 0;
            for (int i = servicesProperty.arraySize - 1; i >= 0; i--)
            {
                var serviceEntry = servicesProperty.GetArrayElementAtIndex(i);
                var interfaceScript = serviceEntry.FindPropertyRelative("interfaceScript");
                var implementationScript = serviceEntry.FindPropertyRelative("implementationScript");

                if (interfaceScript.objectReferenceValue != null)
                {
                    var interfaceName = interfaceScript.objectReferenceValue.name;
                    
                    if (interfaceName == "IExampleGameService" || interfaceName.Contains("Example"))
                    {
                        Debug.Log($"✅ Removing: {interfaceName} (Example service)");
                        servicesProperty.DeleteArrayElementAtIndex(i);
                        removedCount++;
                    }
                    else if (implementationScript.objectReferenceValue == null)
                    {
                        Debug.LogWarning($"⚠️  Found service with missing implementation: {interfaceName}");
                    }
                }
            }

            if (removedCount > 0)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"\n✅ Removed {removedCount} example service(s) from config");
                Debug.Log("════════════════════════════════════════\n");

                EditorUtility.DisplayDialog("Config Cleaned", 
                    $"Successfully removed {removedCount} example service(s) from BootstrapConfig.\n\nThe error should be gone now!", "OK");
            }
            else
            {
                Debug.Log("✅ No example services found - config is already clean!");
                Debug.Log("════════════════════════════════════════\n");
                
                EditorUtility.DisplayDialog("Already Clean", 
                    "No example services found in the config.\n\nYour config is already clean!", "OK");
            }
        }
    }
}
