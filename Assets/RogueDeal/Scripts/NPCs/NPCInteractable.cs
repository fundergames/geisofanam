using UnityEngine;
using UnityEngine.InputSystem;

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
        [SerializeField] private bool useTriggerCollider = true;
        [SerializeField] private float interactionRange = 4f;
        [SerializeField] private Key interactionKey = Key.E;
        [SerializeField] private bool requirePlayerTag = true;
        [SerializeField] private string playerTag = "Player";

        [Header("Visual Feedback")]
        [SerializeField] private GameObject interactionPromptPrefab;
        [SerializeField] private Vector3 promptOffset = new Vector3(0, 2f, 0);
        [SerializeField] private bool showPromptWhenNearby = true;

        private bool _isPlayerInRange = false;
        private GameObject _currentPrompt;
        private Collider _triggerCollider;
        private DialogController _dialogController;
        private GameObject _cachedPlayer;
        private float _playerSearchCooldown;

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
            // Check for interaction input
            if (_isPlayerInRange && Keyboard.current != null && Keyboard.current[interactionKey].wasPressedThisFrame)
            {
                TryStartDialog();
            }

            // Always run distance-based detection: primary when not using trigger,
            // or as fallback when trigger may miss (e.g. layer matrix, CharacterController timing)
            CheckPlayerDistance();
        }

        private void CheckPlayerDistance()
        {
            // Refresh player reference periodically (handles late spawn, destroyed player)
            _playerSearchCooldown -= Time.deltaTime;
            if (_cachedPlayer == null || _playerSearchCooldown <= 0f)
            {
                _cachedPlayer = GameObject.FindGameObjectWithTag(playerTag);
                _playerSearchCooldown = 0.5f;
            }

            GameObject player = _cachedPlayer;
            if (player == null || !player.activeInHierarchy)
            {
                _isPlayerInRange = false;
                UpdateInteractionPrompt();
                return;
            }

            float distance = Vector3.Distance(transform.position, player.transform.position);
            bool wasInRange = _isPlayerInRange;
            _isPlayerInRange = distance <= interactionRange;

            if (wasInRange != _isPlayerInRange)
            {
                UpdateInteractionPrompt();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!useTriggerCollider)
                return;

            if (requirePlayerTag && !other.CompareTag(playerTag))
                return;

            _isPlayerInRange = true;
            UpdateInteractionPrompt();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!useTriggerCollider)
                return;

            if (requirePlayerTag && !other.CompareTag(playerTag))
                return;

            _isPlayerInRange = false;
            UpdateInteractionPrompt();
        }

        private void UpdateInteractionPrompt()
        {
            if (!showPromptWhenNearby)
                return;

            if (_isPlayerInRange && _currentPrompt == null)
            {
                ShowInteractionPrompt();
            }
            else if (!_isPlayerInRange && _currentPrompt != null)
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