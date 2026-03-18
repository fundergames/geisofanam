using UnityEngine;
using Funder.Core.Services;
using RogueDeal.Quests;

namespace RogueDeal.UI
{
    /// <summary>
    /// Diagnostic component to check QuestPanel setup.
    /// Add this temporarily to help debug quest panel issues.
    /// </summary>
    public class QuestPanelDiagnostics : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("=== QUEST PANEL DIAGNOSTICS ===");
            
            // Check for QuestPanel in scene
            var questPanels = FindObjectsByType<QuestPanel>(FindObjectsSortMode.None);
            Debug.Log($"QuestPanels found: {questPanels.Length}");
            
            foreach (var panel in questPanels)
            {
                Debug.Log($"  - {panel.gameObject.name}: Active={panel.gameObject.activeSelf}, Enabled={panel.enabled}");
            }
            
            // Check for QuestService
            try
            {
                var questService = GameBootstrap.ServiceLocator.Resolve<IQuestService>();
                Debug.Log($"QuestService: Found, Progress Count: {questService.GetAllProgress().Count}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"QuestService: NOT FOUND - {e.Message}");
            }
            
            // Check for quest definitions
            var quests = Resources.LoadAll<QuestDefinition>("Quests");
            Debug.Log($"Quest Definitions in Resources/Quests/: {quests.Length}");
            foreach (var quest in quests)
            {
                Debug.Log($"  - {quest.name}: Id={quest.questId}");
            }
            
            Debug.Log("=== END DIAGNOSTICS ===");
        }
    }
}