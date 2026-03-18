using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using RogueDeal.NPCs;
using RogueDeal.Events;

namespace RogueDeal.UI
{
    /// <summary>
    /// UI component that displays dialog. Can be used standalone or with PanelManager.
    /// </summary>
    public class DialogUI : MonoBehaviour
    {
        [Header("Dialog Display")]
        [SerializeField] private GameObject dialogPanel;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogText;
        [SerializeField] private Image speakerPortrait;

        [Header("Choice Buttons")]
        [SerializeField] private Transform choiceButtonContainer;
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("Navigation")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button closeButton;

        [Header("Settings")]
        [SerializeField] private bool autoHideOnDialogEnd = true;
        [SerializeField] private float typewriterSpeed = 0.05f; // Seconds per character
        [SerializeField] private string continueButtonText = "Continue";

        private DialogController _activeDialogController;
        private List<GameObject> _currentChoiceButtons = new List<GameObject>();
        private bool _isSubscribed = false;
        private List<DialogChoice> _pendingChoices = new List<DialogChoice>();
        private bool _waitingForContinueToShowChoices = false;
        private Coroutine _typewriterCoroutine;
        private string _fullDialogText = "";
        private bool _isTyping = false;

        private void Awake()
        {
            Debug.Log("[DialogUI] Awake - Setting up event subscriptions");
            SubscribeToEvents();

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
                // Update continue button text
                UpdateContinueButtonText();
            }

            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);

            // Add click handler to dialog text for advancing
            SetupClickableDialogText();
            
            // Also allow clicking on the dialog panel itself
            SetupClickableDialogPanel();
        }

        private void OnEnable()
        {
            Debug.Log("[DialogUI] OnEnable - Ensuring event subscriptions");
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            Debug.Log("[DialogUI] OnDisable - Keeping subscriptions active");
            // Don't unsubscribe on disable - we need to receive events even when panel is hidden
        }

        private void Start()
        {
            // Hide dialog panel initially
            if (dialogPanel != null)
            {
                dialogPanel.SetActive(false);
                Debug.Log("[DialogUI] Dialog panel hidden initially");
            }
            else
            {
                Debug.LogWarning("[DialogUI] Dialog panel reference is null!");
            }

            // Ensure click handlers are set up (in case references weren't ready in Awake)
            SetupClickableDialogText();
            SetupClickableDialogPanel();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            
            if (continueButton != null)
                continueButton.onClick.RemoveListener(OnContinueClicked);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        private void SetupClickableDialogText()
        {
            if (dialogText == null) return;

            // Add EventTrigger for click handling
            var eventTrigger = dialogText.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = dialogText.gameObject.AddComponent<EventTrigger>();
            }

            // Remove existing pointer click entry if any
            eventTrigger.triggers.RemoveAll(t => t.eventID == EventTriggerType.PointerClick);

            // Add pointer click event
            var entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((data) => { OnDialogTextClicked(); });
            eventTrigger.triggers.Add(entry);

            // Ensure the text is raycastable
            dialogText.raycastTarget = true;
        }

        private void SetupClickableDialogPanel()
        {
            if (dialogPanel == null) return;

            // Try to get or add an Image component for raycast target
            var image = dialogPanel.GetComponent<Image>();
            if (image == null)
            {
                image = dialogPanel.AddComponent<Image>();
                image.color = new Color(0, 0, 0, 0); // Transparent
            }
            image.raycastTarget = true;

            // Add EventTrigger for click handling
            var eventTrigger = dialogPanel.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = dialogPanel.AddComponent<EventTrigger>();
            }

            // Remove existing pointer click entry if any
            eventTrigger.triggers.RemoveAll(t => t.eventID == EventTriggerType.PointerClick);

            // Add pointer click event
            var entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((data) => { OnDialogTextClicked(); });
            eventTrigger.triggers.Add(entry);
        }

        private void UpdateDialogClickability()
        {
            // Disable clicking on dialog when choices are shown
            bool choicesVisible = _currentChoiceButtons != null && _currentChoiceButtons.Count > 0;
            
            if (dialogText != null)
            {
                dialogText.raycastTarget = !choicesVisible;
            }

            if (dialogPanel != null)
            {
                var image = dialogPanel.GetComponent<Image>();
                if (image != null)
                {
                    image.raycastTarget = !choicesVisible;
                }
            }
        }

        private void SubscribeToEvents()
        {
            if (_isSubscribed) return;

            Debug.Log("[DialogUI] Subscribing to EventBus events");
            EventBus<DialogStartedEvent>.Subscribe(OnDialogStarted);
            EventBus<DialogEndedEvent>.Subscribe(OnDialogEnded);
            EventBus<DialogNodeShownEvent>.Subscribe(OnDialogNodeShown);
            _isSubscribed = true;
        }

        private void UnsubscribeFromEvents()
        {
            if (!_isSubscribed) return;

            Debug.Log("[DialogUI] Unsubscribing from EventBus events");
            EventBus<DialogStartedEvent>.Unsubscribe(OnDialogStarted);
            EventBus<DialogEndedEvent>.Unsubscribe(OnDialogEnded);
            EventBus<DialogNodeShownEvent>.Unsubscribe(OnDialogNodeShown);
            _isSubscribed = false;
        }

        private void OnDialogStarted(DialogStartedEvent evt)
        {
            Debug.Log($"[DialogUI] OnDialogStarted event received for NPC: {evt.npcId}");
            
            // Find DialogController - try multiple methods for reliability
            _activeDialogController = FindFirstObjectByType<DialogController>();
            
            if (_activeDialogController == null)
            {
                // Try finding all and logging
                var allControllers = FindObjectsByType<DialogController>(FindObjectsSortMode.None);
                if (allControllers != null && allControllers.Length > 0)
                {
                    _activeDialogController = allControllers[0];
                    Debug.Log($"[DialogUI] Found DialogController via FindObjectsByType (found {allControllers.Length} total)");
                }
                else
                {
                    Debug.LogWarning("[DialogUI] Could not find DialogController in scene!");
                }
            }
            else
            {
                Debug.Log("[DialogUI] Found DialogController");
            }
            
            if (dialogPanel != null)
            {
                dialogPanel.SetActive(true);
                Debug.Log("[DialogUI] Dialog panel activated");
            }
            else
            {
                Debug.LogError("[DialogUI] Cannot show dialog - dialogPanel is null!");
            }

            if (continueButton != null)
                continueButton.gameObject.SetActive(false);

            if (closeButton != null)
                closeButton.gameObject.SetActive(false);
        }

        private void OnDialogEnded(DialogEndedEvent evt)
        {
            ClearDialogDisplay();

            if (autoHideOnDialogEnd && dialogPanel != null)
                dialogPanel.SetActive(false);

            _activeDialogController = null;
        }

        private void OnDialogNodeShown(DialogNodeShownEvent evt)
        {
            if (evt.node == null)
            {
                Debug.LogWarning("[DialogUI] OnDialogNodeShown received null node!");
                return;
            }

            Debug.Log($"[DialogUI] OnDialogNodeShown - Node: {evt.node.nodeId}, Choices count: {evt.node.choices?.Count ?? 0}");

            // Update speaker info
            if (speakerNameText != null)
            {
                speakerNameText.text = evt.node.speaker ?? "NPC";
            }

            // Update dialog text with typewriter effect
            if (dialogText != null)
            {
                _fullDialogText = evt.node.text ?? "";
                StartTypewriterEffect(_fullDialogText);
            }

            // Update portrait
            if (speakerPortrait != null && evt.node.speakerPortrait != null)
            {
                speakerPortrait.sprite = evt.node.speakerPortrait;
                speakerPortrait.gameObject.SetActive(true);
            }
            else if (speakerPortrait != null)
            {
                speakerPortrait.gameObject.SetActive(false);
            }

            // Clear previous choices and reset state
            ClearChoiceButtons();
            _pendingChoices.Clear();
            _waitingForContinueToShowChoices = false;

            // Ensure we have a controller reference
            if (_activeDialogController == null)
            {
                _activeDialogController = FindFirstObjectByType<DialogController>();
                if (_activeDialogController == null)
                {
                    Debug.LogWarning("[DialogUI] DialogController is null! Cannot get available choices.");
                }
            }

            // Get available choices but don't show them yet
            var availableChoices = _activeDialogController?.GetAvailableChoices();
            
            Debug.Log($"[DialogUI] Available choices: {availableChoices?.Count ?? 0}");
            if (availableChoices != null && availableChoices.Count > 0)
            {
                // Store choices for later, show continue button first
                _pendingChoices = availableChoices;
                _waitingForContinueToShowChoices = true;
                Debug.Log($"[DialogUI] Storing {availableChoices.Count} choice(s) - will show after continue");
                
                // Show continue button to reveal choices
                if (continueButton != null)
                {
                    continueButton.gameObject.SetActive(true);
                    UpdateContinueButtonText();
                }
            }
            else
            {
                Debug.Log("[DialogUI] No choices available - showing continue button");
                // No choices - show continue button or auto-advance
                if (continueButton != null)
                {
                    continueButton.gameObject.SetActive(true);
                    UpdateContinueButtonText();
                }
            }

            if (closeButton != null)
                closeButton.gameObject.SetActive(true);

            // Update clickability based on whether choices are shown
            UpdateDialogClickability();
        }

        private void ShowChoices(List<DialogChoice> choices)
        {
            if (choiceButtonContainer == null)
            {
                Debug.LogError("[DialogUI] Choice container is null! Cannot show choices.");
                return;
            }

            if (choiceButtonPrefab == null)
            {
                Debug.LogError("[DialogUI] Choice button prefab is null! Cannot show choices.");
                return;
            }

            Debug.Log($"[DialogUI] ShowChoices called with {choices.Count} choice(s)");

            // Hide dialog text, speaker name, and portrait when showing choices
            if (dialogText != null)
                dialogText.gameObject.SetActive(false);
            
            if (speakerNameText != null)
                speakerNameText.gameObject.SetActive(false);
            
            if (speakerPortrait != null)
                speakerPortrait.gameObject.SetActive(false);

            if (continueButton != null)
                continueButton.gameObject.SetActive(false);

            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                Debug.Log($"[DialogUI] Creating choice button {i}: '{choice.text}'");
                
                GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceButtonContainer, false);
                buttonObj.SetActive(true); // Ensure it's active
                _currentChoiceButtons.Add(buttonObj);

                Button button = buttonObj.GetComponent<Button>();
                if (button == null)
                {
                    button = buttonObj.GetComponentInChildren<Button>();
                }

                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

                if (buttonText != null)
                {
                    buttonText.text = choice.text ?? $"Choice {i + 1}";
                    Debug.Log($"[DialogUI] Set button text to: '{buttonText.text}'");
                }
                else
                {
                    Debug.LogWarning($"[DialogUI] Choice button {i} has no TextMeshProUGUI component!");
                }

                if (button != null)
                {
                    // Remove any existing listeners first
                    button.onClick.RemoveAllListeners();
                    
                    int choiceIndex = i; // Capture for closure
                    button.onClick.AddListener(() => 
                    {
                        Debug.Log($"[DialogUI] Choice {choiceIndex} clicked: '{choice.text}'");
                        OnChoiceClicked(choiceIndex);
                    });
                    Debug.Log($"[DialogUI] Added click listener to choice button {i}");
                }
                else
                {
                    Debug.LogError($"[DialogUI] Choice button {i} has no Button component!");
                }
            }

            Debug.Log($"[DialogUI] Created {_currentChoiceButtons.Count} choice button(s)");
            
            // Update clickability - disable dialog clicks when choices are shown
            UpdateDialogClickability();
        }

        private void ClearChoiceButtons()
        {
            foreach (var button in _currentChoiceButtons)
            {
                if (button != null)
                    Destroy(button);
            }
            _currentChoiceButtons.Clear();
            
            // Show dialog text, speaker name, and portrait again when choices are cleared
            // (They will be updated with new content when next node is shown)
            if (dialogText != null)
                dialogText.gameObject.SetActive(true);
            
            if (speakerNameText != null)
                speakerNameText.gameObject.SetActive(true);
            
            // Portrait will be shown/hidden based on whether new node has one
            
            // Re-enable dialog clicks when choices are cleared
            UpdateDialogClickability();
        }

        private void ClearDialogDisplay()
        {
            // Stop any ongoing typewriter effect
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }
            _isTyping = false;
            _fullDialogText = "";

            if (speakerNameText != null)
                speakerNameText.text = "";

            if (dialogText != null)
                dialogText.text = "";

            if (speakerPortrait != null)
                speakerPortrait.gameObject.SetActive(false);

            ClearChoiceButtons();
            _pendingChoices.Clear();
            _waitingForContinueToShowChoices = false;

            if (continueButton != null)
                continueButton.gameObject.SetActive(false);

            if (closeButton != null)
                closeButton.gameObject.SetActive(false);
        }

        private void OnChoiceClicked(int choiceIndex)
        {
            Debug.Log($"[DialogUI] OnChoiceClicked - Index: {choiceIndex}");
            
            if (_activeDialogController == null)
            {
                Debug.LogError("[DialogUI] Cannot select choice - DialogController is null!");
                // Try to find it again
                _activeDialogController = FindFirstObjectByType<DialogController>();
                if (_activeDialogController == null)
                {
                    Debug.LogError("[DialogUI] Still cannot find DialogController!");
                    return;
                }
            }

            Debug.Log($"[DialogUI] Calling SelectChoice({choiceIndex}) on DialogController");
            _activeDialogController.SelectChoice(choiceIndex);
        }

        private void OnContinueClicked()
        {
            // If text is still typing, skip to end instead of advancing
            if (_isTyping)
            {
                SkipTypewriter();
                return;
            }

            // Execute pending node actions before showing choices or advancing
            if (_activeDialogController != null)
            {
                _activeDialogController.ExecutePendingNodeActions();
            }

            // If we have pending choices, show them instead of advancing
            if (_waitingForContinueToShowChoices && _pendingChoices != null && _pendingChoices.Count > 0)
            {
                Debug.Log($"[DialogUI] Continue clicked - showing {_pendingChoices.Count} choice(s)");
                _waitingForContinueToShowChoices = false;
                ShowChoices(_pendingChoices);
                _pendingChoices.Clear();
                return;
            }

            // Otherwise, advance the dialog normally
            if (_activeDialogController != null)
            {
                _activeDialogController.AdvanceDialog();
            }
        }

        private void OnCloseClicked()
        {
            if (_activeDialogController != null)
            {
                _activeDialogController.EndDialog();
            }
        }

        private void OnDialogTextClicked()
        {
            // Don't advance if choices are currently shown (user must select a choice)
            if (_currentChoiceButtons != null && _currentChoiceButtons.Count > 0)
            {
                Debug.Log("[DialogUI] Dialog text clicked but choices are shown - ignoring click");
                return;
            }

            // If text is still typing, skip to end
            if (_isTyping)
            {
                SkipTypewriter();
                return;
            }

            // Otherwise, act like continue button was clicked
            Debug.Log("[DialogUI] Dialog text/panel clicked - advancing dialog");
            OnContinueClicked();
        }

        private void StartTypewriterEffect(string fullText)
        {
            // Stop any existing typewriter effect
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
            }

            if (dialogText == null || string.IsNullOrEmpty(fullText))
            {
                if (dialogText != null)
                    dialogText.text = fullText;
                _isTyping = false;
                return;
            }

            _isTyping = true;
            _typewriterCoroutine = StartCoroutine(TypewriterCoroutine(fullText));
        }

        private IEnumerator TypewriterCoroutine(string fullText)
        {
            if (dialogText == null)
            {
                _isTyping = false;
                yield break;
            }

            dialogText.text = "";
            int currentIndex = 0;

            while (currentIndex < fullText.Length)
            {
                dialogText.text += fullText[currentIndex];
                currentIndex++;
                yield return new WaitForSeconds(typewriterSpeed);
            }

            _isTyping = false;
            _typewriterCoroutine = null;
        }

        private void SkipTypewriter()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            if (dialogText != null && !string.IsNullOrEmpty(_fullDialogText))
            {
                dialogText.text = _fullDialogText;
            }

            _isTyping = false;
        }

        private void UpdateContinueButtonText()
        {
            if (continueButton == null) return;

            // Try to find TextMeshProUGUI component in the button or its children
            TextMeshProUGUI buttonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = continueButtonText;
            }
            else
            {
                // Fallback to regular Text component
                Text textComponent = continueButton.GetComponentInChildren<Text>();
                if (textComponent != null)
                {
                    textComponent.text = continueButtonText;
                }
            }
        }
    }
}