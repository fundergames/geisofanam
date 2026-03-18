using System.Collections;
using UnityEngine;
using Funder.Core.Services;
using RogueDeal.UI;

namespace RogueDeal.Quests
{
    /// <summary>
    /// Simple component to test quest system - starts a test quest.
    /// Attach to a button or call StartTestQuest() from anywhere.
    /// </summary>
    public class QuestTestStarter : MonoBehaviour
    {
        [Header("Test Quest")]
        [SerializeField] private string testQuestId = "test_quest"; // Changed to match your quest definition

        private void Awake()
        {
            Debug.Log($"[QuestTestStarter] Awake() called on {gameObject.name}");
        }

        private void Start()
        {
            Debug.Log($"[QuestTestStarter] Start() called on {gameObject.name}");
        }

        // Simple test method - call this from button to verify button wiring works
        public void TestButtonClick()
        {
            Debug.LogError("══════ TEST BUTTON CLICKED! ══════ Button is wired correctly!");
        }

        public void StartTestQuest()
        {
            Debug.LogError($"[QuestTestStarter] ══════ StartTestQuest() CALLED! ══════ GameObject: {gameObject.name}");
            
            // First check if QuestPanel exists
            var questPanels = FindObjectsByType<QuestPanel>(FindObjectsSortMode.None);
            Debug.Log($"[QuestTestStarter] Found {questPanels.Length} QuestPanel(s) in scene");
            foreach (var panel in questPanels)
            {
                Debug.Log($"  - QuestPanel on: {panel.gameObject.name}, Active: {panel.gameObject.activeSelf}");
            }
            
            try
            {
                var questService = GameBootstrap.ServiceLocator.Resolve<IQuestService>();
                Debug.Log($"[QuestTestStarter] Attempting to start quest: {testQuestId}");
                
                if (questService.TryStartQuest(testQuestId))
                {
                    Debug.Log($"[QuestTestStarter] ✅ Successfully started quest: {testQuestId}");
                    
                    // Wait a frame to ensure event has been processed, then force refresh
                    StartCoroutine(RefreshQuestPanelAfterDelay());
                }
                else
                {
                    Debug.LogWarning($"[QuestTestStarter] ❌ Failed to start quest: {testQuestId}. Check if quest definition exists in Resources/Quests/");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[QuestTestStarter] Error starting quest: {e.Message}\nStackTrace: {e.StackTrace}");
            }
        }

        private IEnumerator RefreshQuestPanelAfterDelay()
        {
            // Wait for end of frame to ensure event has been processed
            yield return new WaitForEndOfFrame();
            
            // Force refresh quest panel if it exists
            var questPanel = FindFirstObjectByType<QuestPanel>();
            if (questPanel != null)
            {
                Debug.Log("[QuestTestStarter] Found QuestPanel, forcing refresh after delay...");
                questPanel.RefreshDisplay();
            }
            else
            {
                Debug.LogWarning("[QuestTestStarter] No QuestPanel found in scene! Make sure QuestPanel component exists.");
            }
        }
    }
}