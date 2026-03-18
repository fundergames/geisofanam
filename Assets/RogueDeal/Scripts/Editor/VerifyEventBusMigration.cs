using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace RogueDeal.Editor
{
    public static class VerifyEventBusMigration
    {
        [MenuItem("Funder Games/Rogue Deal/Migration/5. Verify All Files")]
        public static void VerifyAllFiles()
        {
            var results = new System.Text.StringBuilder();
            results.AppendLine("════════════════════════════════════════");
            results.AppendLine("EVENTBUS MIGRATION VERIFICATION");
            results.AppendLine("════════════════════════════════════════\n");

            var issuesFound = new List<string>();
            var filesChecked = 0;

            var allCsFiles = Directory.GetFiles("Assets/RogueDeal/Scripts", "*.cs", SearchOption.AllDirectories);
            
            foreach (var filePath in allCsFiles)
            {
                if (filePath.Contains("Editor")) continue;
                
                filesChecked++;
                var content = File.ReadAllText(filePath);
                
                var usesIEventBus = Regex.IsMatch(content, @"\bIEventBus\b");
                var hasEventsImport = content.Contains("using Funder.Core.Events;");
                var hasServicesImport = content.Contains("using Funder.Core.Services;");
                
                if (usesIEventBus && !hasEventsImport)
                {
                    issuesFound.Add($"⚠️  {Path.GetFileName(filePath)} - Uses IEventBus but missing 'using Funder.Core.Events;'");
                }
            }

            results.AppendLine($"Files checked: {filesChecked}");
            results.AppendLine($"Issues found: {issuesFound.Count}\n");

            if (issuesFound.Count > 0)
            {
                results.AppendLine("ISSUES:");
                foreach (var issue in issuesFound)
                {
                    results.AppendLine(issue);
                }
                results.AppendLine("\n❌ Migration incomplete - please fix the issues above");
            }
            else
            {
                results.AppendLine("✅ ALL FILES VERIFIED!");
                results.AppendLine("✅ No missing imports found");
                results.AppendLine("✅ Migration is complete!");
            }

            results.AppendLine("\n════════════════════════════════════════");

            Debug.Log(results.ToString());

            if (issuesFound.Count > 0)
            {
                EditorUtility.DisplayDialog("Migration Verification", 
                    $"Found {issuesFound.Count} issue(s).\n\nCheck the console for details.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Migration Verification", 
                    "✅ All files verified!\n\nNo issues found. Migration is complete!", "Awesome!");
            }
        }
    }
}
