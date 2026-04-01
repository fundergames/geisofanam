using Geis.InteractInput;
using UnityEngine;

namespace RogueDeal.NPCs
{
    /// <summary>
    /// Component for NPCs that can be interacted with. Handles proximity detection and input.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class NPCInteractable : MonoBehaviour
    {
        [Header("NPC Definition")]
        [SerializeField] private NPCDefinition npcDefinition;

        [Header("Interaction Settings")]
        [Tooltip("Which realm allows talk / interact. NPC dialog is usually PhysicalOnly.")]
        [SerializeField] private InteractRealmScope interactRealm = InteractRealmScope.PhysicalOnly;
        [SerializeField] private bool useTriggerCollider = true;
        [SerializeField] private float interactionRange = 4f;
        [Tooltip("Max distance to show the prompt. If < 0, uses Interaction Range (same as interact).")]
        [SerializeField] private float promptRange = -1f;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject interactionPromptPrefab;
        [SerializeField] private Vector3 promptOffset = new Vector3(0, 2f, 0);
        [SerializeField] private bool showPromptWhenNearby = true;

        private bool _isPlayerInInteractRange = false;
        private bool _isPlayerInPromptRange = false;
        private GameObject _currentPrompt;
        private Collider _triggerCollider;
        private DialogController _dialogController;

        private void Awake()
        {
            _triggerCollider = GetComponent<Collider>();
            _dialogController = GetComponent<DialogController>();

            // Setup trigger collider
            if (useTriggerCollider && _triggerCollider != null)
            {
                _triggerCollider.isTrigger = true;
            }

            // Auto-create DialogController if missing
            if (_dialogController == null && npcDefinition != null && npcDefinition.dialogTree != null)
            {
                _dialogController = gameObject.AddComponent<DialogController>();
            }
        }

        private void Update()
        {
            // Distance first so _isPlayerInInteractRange matches this frame (same order as puzzle triggers).
            CheckPlayerDistance();

            if (_isPlayerInInteractRange &&
                GeisInteractInput.WasInteractPressedThisFrameInRealm(interactRealm))
                TryStartDialog();
        }

        private void CheckPlayerDistance()
        {
            Vector3 playerPos = GeisInteractInput.GetInteractionWorldPositionOrFallback();
            float distance = Vector3.Distance(transform.position, playerPos);
            float promptCutoff = promptRange >= 0f ? promptRange : interactionRange;
            bool inInteract = distance <= interactionRange;
            bool inPrompt = distance <= promptCutoff;

            if (_isPlayerInInteractRange != inInteract || _isPlayerInPromptRange != inPrompt)
                SetProximityState(inInteract, inPrompt);
            else
                UpdateInteractionPrompt();
        }

        private void SetProximityState(bool inInteractRange, bool inPromptRange)
        {
            _isPlayerInInteractRange = inInteractRange;
            _isPlayerInPromptRange = inPromptRange;
            UpdateInteractionPrompt();
        }

        private void UpdateInteractionPrompt()
        {
            if (!showPromptWhenNearby)
                return;

            bool realmOk = GeisInteractInput.IsInteractRealmAllowed(interactRealm);
            bool wantPrompt = _isPlayerInPromptRange && realmOk;

            if (wantPrompt && _currentPrompt == null)
            {
                ShowInteractionPrompt();
            }
            else if (!wantPrompt && _currentPrompt != null)
            {
                HideInteractionPrompt();
            }
        }

        private void ShowInteractionPrompt()
        {
            if (interactionPromptPrefab != null)
            {
                _currentPrompt = Instantiate(interactionPromptPrefab, transform.position + promptOffset, Quaternion.identity);
                _currentPrompt.transform.SetParent(transform);
            }
            else
            {
                // Create simple text prompt if no prefab
                CreateSimplePrompt();
            }
        }

        private void HideInteractionPrompt()
        {
            if (_currentPrompt != null)
            {
                Destroy(_currentPrompt);
                _currentPrompt = null;
            }
        }

        private void CreateSimplePrompt()
        {
            // Create a simple 3D text prompt (optional, can be improved later)
            GameObject promptObj = new GameObject("InteractionPrompt");
            promptObj.transform.SetParent(transform);
            promptObj.transform.localPosition = promptOffset;
            // Could add TextMesh component here if needed
            _currentPrompt = promptObj;
        }

        private void TryStartDialog()
        {
            Debug.Log($"[NPCInteractable] TryStartDialog called on {gameObject.name}");
            
            if (_dialogController == null)
            {
                Debug.LogWarning($"[NPCInteractable] No DialogController found on {gameObject.name}");
                return;
            }

            if (npcDefinition == null)
            {
                Debug.LogWarning($"[NPCInteractable] No NPCDefinition assigned on {gameObject.name}");
                return;
            }

            Debug.Log($"[NPCInteractable] Starting dialog with {npcDefinition.displayName}");
            _dialogController.StartDialog(npcDefinition);
            HideInteractionPrompt();
        }

        public void SetNPCDefinition(NPCDefinition definition)
        {
            npcDefinition = definition;
            if (definition != null && useTriggerCollider)
            {
                interactionRange = definition.interactionRange;
            }
        }

        private void OnDestroy()
        {
            HideInteractionPrompt();
        }
    }
}