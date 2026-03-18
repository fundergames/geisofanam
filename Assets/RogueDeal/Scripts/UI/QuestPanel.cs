using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Funder.Core.Services;
using RogueDeal.Quests;
using RogueDeal.Events;

namespace RogueDeal.UI
{
    /// <summary>
    /// Manages quest icons displayed on the right side of the screen.
    /// Shows compact circular icons with progress, expandable to show details.
    /// </summary>
    public class QuestPanel : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject questIconPrefab;
        [SerializeField] private QuestDetailWindow questDetailWindowPrefab;

        [Header("Container")]
        [SerializeField] private Transform questContainer;

        [Header("Settings")]
        [SerializeField] private int maxDisplayedQuests = 5;
        [SerializeField] private bool onlyShowActiveQuests = true;
        [SerializeField] private float iconSpacing = 90f;
        [SerializeField] private Vector2 iconSize = new Vector2(80f, 80f);
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        private IQuestService _questService;
        private Dictionary<string, GameObject> _questIcons = new Dictionary<string, GameObject>();
        private Dictionary<string, QuestDefinition> _questCache = new Dictionary<string, QuestDefinition>();
        private QuestDetailWindow _currentDetailWindow;

        private void Awake()
        {
            // Force log immediately - even if there's an error, this should show
            try
            {
                Debug.Log($"[QuestPanel] ═══ Awake() START ═══ GameObject: {gameObject.name}, Active: {gameObject.activeSelf}, ActiveInHierarchy: {gameObject.activeInHierarchy}");
                
                // Subscribe early in Awake to catch events that might fire during Start
                EventBus<QuestStateChangedEvent>.Subscribe(OnQuestStateChanged);
                Debug.Log("[QuestPanel] Subscribed to QuestStateChangedEvent in Awake");
                
                Debug.Log($"[QuestPanel] ═══ Awake() END ═══");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[QuestPanel] ERROR in Awake(): {e.Message}\n{e.StackTrace}");
            }
        }

        private void OnEnable()
        {
            Debug.Log($"[QuestPanel] OnEnable() called - GameObject: {gameObject.name}, Active: {gameObject.activeSelf}");
        }

        private void Start()
        {
            Debug.Log($"[QuestPanel] Start() called on GameObject: {gameObject.name}");
            
            if (questIconPrefab == null)
            {
                Debug.LogError("[QuestPanel] Quest Icon prefab not assigned!");
                return;
            }

            if (questContainer == null)
            {
                questContainer = transform;
                Debug.Log("[QuestPanel] Using self as quest container");
            }

            // Setup container to be on the right side
            SetupContainerLayout();

            try
            {
                _questService = GameBootstrap.ServiceLocator.Resolve<IQuestService>();
                Debug.Log("[QuestPanel] ✅ Successfully resolved IQuestService");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[QuestPanel] Failed to resolve IQuestService: {e.Message}");
                return;
            }

            Debug.Log("[QuestPanel] Subscribed to QuestStateChangedEvent (in Awake)");
            
            // Initial refresh with a small delay to ensure everything is initialized
            Invoke(nameof(RefreshDisplay), 0.1f);
        }

        private void OnDestroy()
        {
            EventBus<QuestStateChangedEvent>.Unsubscribe(OnQuestStateChanged);
        }

        private void OnQuestStateChanged(QuestStateChangedEvent evt)
        {
            Debug.Log($"[QuestPanel] ═══ OnQuestStateChanged EVENT RECEIVED ═══ QuestId: {evt.questId}, Status: {evt.status}");
            if (showDebugLogs)
                Debug.Log($"[QuestPanel] Quest state changed: {evt.questId} -> {evt.status}");
            RefreshDisplay();
        }

        private void SetupContainerLayout()
        {
            var rectTransform = questContainer.GetComponent<RectTransform>();
            if (rectTransform == null)
                rectTransform = questContainer.gameObject.AddComponent<RectTransform>();

            // Anchor to top-right
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = new Vector2(-20f, -20f);

            // Add Vertical Layout Group for stacking icons
            var layoutGroup = questContainer.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = questContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                layoutGroup.spacing = iconSpacing - iconSize.y;
                layoutGroup.childAlignment = TextAnchor.UpperRight;
                layoutGroup.childControlHeight = false;
                layoutGroup.childControlWidth = false;
                layoutGroup.childForceExpandHeight = false;
                layoutGroup.childForceExpandWidth = false;
            }
        }

        public void RefreshDisplay()
        {
            Debug.Log($"[QuestPanel] RefreshDisplay() called - Service: {_questService != null}, Prefab: {questIconPrefab != null}");
            
            if (_questService == null)
            {
                Debug.LogWarning("[QuestPanel] Quest service is null, cannot refresh display. Will retry when service is available.");
                return;
            }
            
            if (questIconPrefab == null)
            {
                Debug.LogError("[QuestPanel] Quest Icon prefab is null! Cannot create quest icons.");
                return;
            }

            // Get all quest progress
            var allProgress = _questService.GetAllProgress();
            if (showDebugLogs)
            {
                Debug.Log($"[QuestPanel] Refreshing display. Total quests: {allProgress.Count}");
                foreach (var p in allProgress)
                {
                    Debug.Log($"  - Quest: {p.questId}, Status: {p.status}");
                }
            }
            
            // Filter to active quests if needed
            var questsToShow = onlyShowActiveQuests
                ? allProgress.Where(p => p.status == QuestStatus.Active).Take(maxDisplayedQuests)
                : allProgress.Where(p => !p.IsTerminal).Take(maxDisplayedQuests);

            var questsList = questsToShow.ToList();
            if (showDebugLogs)
                Debug.Log($"[QuestPanel] Quests to show: {questsList.Count} (Active only: {onlyShowActiveQuests})");
            
            if (questsList.Count == 0)
            {
                if (showDebugLogs)
                    Debug.Log("[QuestPanel] No quests to display");
                // Hide all existing icons
                foreach (var kvp in _questIcons)
                {
                    if (kvp.Value != null)
                        kvp.Value.SetActive(false);
                }
                return;
            }

            // Track which quests should be displayed
            var displayedQuestIds = new HashSet<string>();

            foreach (var progress in questsList)
            {
                displayedQuestIds.Add(progress.questId);
                
                // Get or create quest icon
                if (!_questIcons.TryGetValue(progress.questId, out var iconObj))
                {
                    iconObj = Instantiate(questIconPrefab, questContainer, false);
                    _questIcons[progress.questId] = iconObj;
                    
                    // Setup RectTransform
                    var rectTransform = iconObj.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.sizeDelta = iconSize;
                        rectTransform.localScale = Vector3.one;
                    }
                    
                    iconObj.SetActive(true);
                    
                    if (showDebugLogs)
                        Debug.Log($"[QuestPanel] Created quest icon for: {progress.questId}");
                }
                else
                {
                    iconObj.SetActive(true);
                }

                // Load quest definition if needed
                if (!_questCache.TryGetValue(progress.questId, out var questDef))
                {
                    questDef = LoadQuestDefinition(progress.questId);
                    if (questDef != null)
                        _questCache[progress.questId] = questDef;
                }

                // Update icon display
                var iconButton = iconObj.GetComponent<QuestIconButton>();
                if (iconButton == null)
                {
                    iconButton = iconObj.AddComponent<QuestIconButton>();
                }

                if (iconButton != null && questDef != null)
                {
                    iconButton.SetQuest(progress, questDef, questDetailWindowPrefab);
                    if (showDebugLogs)
                        Debug.Log($"[QuestPanel] Updated quest icon: {questDef.displayName}");
                }
                else if (questDef == null)
                {
                    Debug.LogWarning($"[QuestPanel] Quest definition not found: {progress.questId}");
                }
            }

            // Hide/remove quests that are no longer active
            var toRemove = new List<string>();
            foreach (var kvp in _questIcons)
            {
                if (!displayedQuestIds.Contains(kvp.Key))
                {
                    Destroy(kvp.Value);
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var questId in toRemove)
            {
                _questIcons.Remove(questId);
            }

            // Rebuild layout
            if (questContainer != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(questContainer as RectTransform);
            }
        }

        private QuestDefinition LoadQuestDefinition(string questId)
        {
            // Try to load from Resources - check both possible locations
            // First try Data/Quests (matches LevelManager pattern)
            var allQuests = Resources.LoadAll<QuestDefinition>("Data/Quests");
            var quest = allQuests.FirstOrDefault(q => q.questId == questId);
            
            // If not found, try Quests (legacy location)
            if (quest == null)
            {
                allQuests = Resources.LoadAll<QuestDefinition>("Quests");
                quest = allQuests.FirstOrDefault(q => q.questId == questId);
            }
            
            return quest;
        }

        /// <summary>
        /// Shows the quest detail window for the specified quest.
        /// Called when a quest icon is clicked.
        /// </summary>
        public void ShowQuestDetails(QuestProgress progress, QuestDefinition definition)
        {
            if (definition == null || progress == null)
                return;

            // Close existing detail window if open
            if (_currentDetailWindow != null)
            {
                _currentDetailWindow.Close();
            }

            // Create or reuse detail window
            if (questDetailWindowPrefab != null)
            {
                // Try to find existing detail window in scene
                _currentDetailWindow = FindFirstObjectByType<QuestDetailWindow>();
                
                if (_currentDetailWindow == null)
                {
                    // Create new detail window
                    var canvas = GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        var detailObj = Instantiate(questDetailWindowPrefab.gameObject, canvas.transform, false);
                        _currentDetailWindow = detailObj.GetComponent<QuestDetailWindow>();
                    }
                    else
                    {
                        var detailObj = Instantiate(questDetailWindowPrefab.gameObject, transform.root, false);
                        _currentDetailWindow = detailObj.GetComponent<QuestDetailWindow>();
                    }
                }

                if (_currentDetailWindow != null)
                {
                    // Position near the quest icon (slightly to the left)
                    var rectTransform = _currentDetailWindow.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.anchorMin = new Vector2(1f, 1f);
                        rectTransform.anchorMax = new Vector2(1f, 1f);
                        rectTransform.pivot = new Vector2(1f, 1f);
                        rectTransform.anchoredPosition = new Vector2(-120f, -20f);
                    }

                    _currentDetailWindow.Show(progress, definition);
                }
            }
        }
    }
}