using UnityEditor;
using UnityEngine;
using System.IO;

namespace RogueDeal.Editor
{
    public static class CleanupLegacyEventBus
    {
        [MenuItem("Funder Games/Rogue Deal/Migration/4. Remove Legacy EventBus Code")]
        public static void RemoveLegacyCode()
        {
            var filesToDelete = new[]
            {
                "Assets/FunderCore/Scripts/Systems/Core/Examples/SimpleEventBus.cs",
                "Assets/FunderCore/Scripts/Systems/Core/Interfaces/IEventBus.cs",
                "Assets/FunderCore/Scripts/Systems/Core/Runtime/EventBusExtensions.cs",
                "Assets/FunderCore/Scripts/Systems/Core/Examples/ExampleGameService.cs",
                "Assets/FunderCore/Scripts/Systems/Core/Examples/ExampleServiceUser.cs"
            };

            var confirmMessage = "This will DELETE the following legacy files:\n\n";
            int foundCount = 0;
            foreach (var file in filesToDelete)
            {
                if (File.Exists(file))
                {
                    confirmMessage += $"✓ {Path.GetFileName(file)}\n";
                    foundCount++;
                }
            }
            
            if (foundCount == 0)
            {
                EditorUtility.DisplayDialog("Nothing to Clean Up", 
                    "All legacy files have already been removed or don't exist.\n\nYour project is clean!", "OK");
                Debug.Log("✅ No legacy files found - cleanup already complete!");
                return;
            }
            
            confirmMessage += "\nMake sure you've run the migration tool first!\n\nContinue?";

            if (!EditorUtility.DisplayDialog("Remove Legacy EventBus Code", confirmMessage, "Yes, Delete", "Cancel"))
            {
                Debug.Log("Cleanup cancelled.");
                return;
            }

            int deletedCount = 0;
            int notFoundCount = 0;

            foreach (var filePath in filesToDelete)
            {
                if (File.Exists(filePath))
                {
                    AssetDatabase.DeleteAsset(filePath);
                    deletedCount++;
                    Debug.Log($"✅ Deleted: {filePath}");
                }
                else
                {
                    notFoundCount++;
                    Debug.LogWarning($"⚠️  Not found (already deleted?): {filePath}");
                }
            }

            AssetDatabase.Refresh();

            var resultMessage = $"Legacy cleanup complete!\n\n";
            resultMessage += $"✅ Files deleted: {deletedCount}\n";
            if (notFoundCount > 0)
            {
                resultMessage += $"⚠️  Files not found: {notFoundCount}\n";
            }
            resultMessage += $"\nYour project now uses only the modern EventBus system!";

            Debug.Log("════════════════════════════════════════");
            Debug.Log(resultMessage);
            Debug.Log("════════════════════════════════════════");

            EditorUtility.DisplayDialog("Cleanup Complete", resultMessage, "OK");
        }

        [MenuItem("Funder Games/Rogue Deal/Migration/4. Remove Legacy EventBus Code", true)]
        public static bool ValidateRemoveLegacyCode()
        {
            return File.Exists("Assets/FunderCore/Scripts/Systems/Core/Examples/SimpleEventBus.cs");
        }
    }
}
