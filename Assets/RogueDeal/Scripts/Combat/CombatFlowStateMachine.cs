using Funder.Core.Events;
using Funder.Core.FSM;
using RogueDeal.Combat.Cards;
using RogueDeal.Combat.States;
using RogueDeal.Enemies;
using RogueDeal.Events;
using RogueDeal.UI;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RogueDeal.Combat
{
    public class CombatFlowStateMachine : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CardHandUI cardHandUI;
        [SerializeField] private Button actionButton;
        [SerializeField] private TextMeshProUGUI buttonText;

        [Header("Character References")]
        [SerializeField] private PlayerVisual playerVisual;
        [SerializeField] private Transform enemyContainer;

        [Header("Controllers")]
        [SerializeField] private CombatIntroController introController;

        [Header("Timing Settings")]
        [SerializeField] private float discardCardDelay = 0.1f;
        [SerializeField] private float highlightDuration = 1f;
        [SerializeField] private float attackMoveSpeed = 5f;
        [SerializeField] private float attackReturnSpeed = 3f;
        [SerializeField] private float delayBeforeEnemyAttack = 0.5f;
        [SerializeField] private float delayAfterEnemyAttack = 0.5f;

        [Header("Position Settings")]
        [SerializeField] private Vector3 attackPositionOffset = new Vector3(-2f, 0f, 0f);

        private StateMachine stateMachine;
        private CombatManager combatManager;
        private EnemyVisual currentEnemyVisual;
        private EnemyInstance currentEnemy;
        private Vector3 playerStartPosition;
        private PokerHandType? lastHandType;

        private CombatIntroState combatIntroState;
        private WaitingForPlayerState waitingForPlayerState;
        private DealingCardsState dealingCardsState;
        private DiscardingCardsState discardingCardsState;
        private HighlightingHandState highlightingHandState;
        private PlayerAttackingState playerAttackingState;
        private EnemyReactingState enemyReactingState;
        private EnemyAttackingState enemyAttackingState;
        private PlayerReturningState playerReturningState;
        private ClearingHandState clearingHandState;
        private ReadyToDealAgainState readyToDealAgainState;
        private PlayerMovingToEnemyState playerMovingToEnemyState;

        public StateMachine StateMachine => stateMachine;
        public CardHandUI CardHandUI => cardHandUI;
        public CombatManager CombatManager => combatManager;
        public PlayerVisual PlayerVisual => playerVisual;
        public EnemyVisual CurrentEnemyVisual => currentEnemyVisual;
        public EnemyInstance CurrentEnemy => currentEnemy;
        public Vector3 PlayerStartPosition => playerStartPosition;
        public PokerHandType? LastHandType => lastHandType;
        public CombatIntroController IntroController => introController;

        public float DiscardCardDelay => discardCardDelay;
        public float HighlightDuration => highlightDuration;
        public float AttackMoveSpeed => attackMoveSpeed;
        public float AttackReturnSpeed => attackReturnSpeed;
        public float DelayBeforeEnemyAttack => delayBeforeEnemyAttack;
        public float DelayAfterEnemyAttack => delayAfterEnemyAttack;
        public Vector3 AttackPositionOffset => attackPositionOffset;

        private void Awake()
        {
            Debug.Log("[CombatFlowStateMachine] Awake - initializing and subscribing to events...");
            InitializeStateMachine();
            SubscribeToEvents();

            if (playerVisual != null)
                playerStartPosition = playerVisual.transform.position;
            
            Debug.Log("[CombatFlowStateMachine] Awake complete - subscribed to events.");
        }

        private void Start()
        {
            if (introController == null)
            {
                introController = GetComponentInChildren<CombatIntroController>();
                if (introController == null)
                {
                    introController = FindFirstObjectByType<CombatIntroController>();
                }
                
                if (introController != null)
                {
                    Debug.Log("[CombatFlowStateMachine] Auto-found CombatIntroController");
                }
                else
                {
                    Debug.LogWarning("[CombatFlowStateMachine] CombatIntroController not found! Combat intro will be skipped.");
                }
            }

            if (cardHandUI == null)
            {
                cardHandUI = FindFirstObjectByType<CardHandUI>();
                if (cardHandUI == null)
                    Debug.LogWarning("[CombatFlowStateMachine] CardHandUI not found! Assign it in the Inspector.");
            }

            if (actionButton == null || buttonText == null)
            {
                var combatUI = GameObject.Find("CombatUI");
                if (combatUI != null)
                {
                    if (actionButton == null)
                    {
                        var drawButton = combatUI.transform.Find("DrawButton");
                        if (drawButton != null)
                            actionButton = drawButton.GetComponent<Button>();
                    }
                    
                    if (buttonText == null && actionButton != null)
                        buttonText = actionButton.GetComponentInChildren<TextMeshProUGUI>();
                }
                
                if (actionButton == null)
                    Debug.LogWarning("[CombatFlowStateMachine] Action Button not found! Assign it in the Inspector.");
                if (buttonText == null)
                    Debug.LogWarning("[CombatFlowStateMachine] Button Text not found! Assign it in the Inspector.");
            }

            if (actionButton != null)
                actionButton.onClick.AddListener(OnActionButtonClicked);

            if (cardHandUI != null)
            {
                cardHandUI.autoHandleEvents = false;
                Debug.Log("[CombatFlowStateMachine] CardHandUI.autoHandleEvents set to false - FSM will handle events.");
            }
        }

        private void Update()
        {
            stateMachine?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (actionButton != null)
                actionButton.onClick.RemoveListener(OnActionButtonClicked);
        }

        private void InitializeStateMachine()
        {
            stateMachine = new StateMachine();

            combatIntroState = new CombatIntroState(this);
            waitingForPlayerState = new WaitingForPlayerState(this);
            dealingCardsState = new DealingCardsState(this);
            discardingCardsState = new DiscardingCardsState(this);
            highlightingHandState = new HighlightingHandState(this);
            playerAttackingState = new PlayerAttackingState(this);
            enemyReactingState = new EnemyReactingState(this);
            enemyAttackingState = new EnemyAttackingState(this);
            playerReturningState = new PlayerReturningState(this);
            clearingHandState = new ClearingHandState(this);
            readyToDealAgainState = new ReadyToDealAgainState(this);
            playerMovingToEnemyState = new PlayerMovingToEnemyState(this);

            stateMachine.AddState(combatIntroState);
            stateMachine.AddState(waitingForPlayerState);
            stateMachine.AddState(dealingCardsState);
            stateMachine.AddState(discardingCardsState);
            stateMachine.AddState(highlightingHandState);
            stateMachine.AddState(playerAttackingState);
            stateMachine.AddState(enemyReactingState);
            stateMachine.AddState(enemyAttackingState);
            stateMachine.AddState(playerReturningState);
            stateMachine.AddState(clearingHandState);
            stateMachine.AddState(readyToDealAgainState);
            stateMachine.AddState(playerMovingToEnemyState);

            stateMachine.AddTransition(typeof(CombatIntroState), typeof(DealingCardsState));
            stateMachine.AddTransition(typeof(DealingCardsState), typeof(WaitingForPlayerState));
            stateMachine.AddTransition(typeof(WaitingForPlayerState), typeof(DiscardingCardsState));
            stateMachine.AddTransition(typeof(DiscardingCardsState), typeof(HighlightingHandState));
            stateMachine.AddTransition(typeof(HighlightingHandState), typeof(PlayerAttackingState));
            stateMachine.AddTransition(typeof(PlayerAttackingState), typeof(EnemyReactingState));
            stateMachine.AddTransition(typeof(PlayerAttackingState), typeof(EnemyAttackingState));
            stateMachine.AddTransition(typeof(PlayerAttackingState), typeof(PlayerReturningState));
            stateMachine.AddTransition(typeof(EnemyReactingState), typeof(EnemyAttackingState));
            stateMachine.AddTransition(typeof(EnemyReactingState), typeof(PlayerReturningState));
            stateMachine.AddTransition(typeof(EnemyReactingState), typeof(PlayerMovingToEnemyState));
            stateMachine.AddTransition(typeof(EnemyAttackingState), typeof(PlayerReturningState));
            stateMachine.AddTransition(typeof(EnemyAttackingState), typeof(ClearingHandState));
            stateMachine.AddTransition(typeof(PlayerReturningState), typeof(EnemyAttackingState));
            stateMachine.AddTransition(typeof(PlayerReturningState), typeof(ClearingHandState));
            stateMachine.AddTransition(typeof(ClearingHandState), typeof(ReadyToDealAgainState));
            stateMachine.AddTransition(typeof(ReadyToDealAgainState), typeof(DealingCardsState));
            stateMachine.AddTransition(typeof(PlayerMovingToEnemyState), typeof(DealingCardsState));
        }

        private void SubscribeToEvents()
        {
            EventBus<CombatStartedEvent>.Subscribe(OnCombatStarted);
            EventBus<HandDealtEvent>.Subscribe(OnHandDealt);
            EventBus<HandEvaluatedEvent>.Subscribe(OnHandEvaluated);
            EventBus<PlayerAttackEvent>.Subscribe(OnPlayerAttack);
            EventBus<EnemyAttackEvent>.Subscribe(OnEnemyAttack);
            EventBus<EnemyDefeatedEvent>.Subscribe(OnEnemyDefeated);
            EventBus<CombatEndedEvent>.Subscribe(OnCombatEnded);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus<CombatStartedEvent>.Unsubscribe(OnCombatStarted);
            EventBus<HandDealtEvent>.Unsubscribe(OnHandDealt);
            EventBus<HandEvaluatedEvent>.Unsubscribe(OnHandEvaluated);
            EventBus<PlayerAttackEvent>.Unsubscribe(OnPlayerAttack);
            EventBus<EnemyAttackEvent>.Unsubscribe(OnEnemyAttack);
            EventBus<EnemyDefeatedEvent>.Unsubscribe(OnEnemyDefeated);
            EventBus<CombatEndedEvent>.Unsubscribe(OnCombatEnded);
        }

        public void SetCombatManager(CombatManager manager)
        {
            combatManager = manager;
            Debug.Log("[CombatFlowStateMachine] CombatManager set successfully.");
        }

        public void SetPlayerVisual(PlayerVisual visual)
        {
            playerVisual = visual;
            if (playerVisual != null)
            {
                playerStartPosition = playerVisual.transform.position;
                Debug.Log($"[CombatFlowStateMachine] PlayerVisual set to {playerVisual.name} at {playerStartPosition}");
            }
        }

        public void SetCurrentEnemy(EnemyVisual visual, EnemyInstance instance)
        {
            currentEnemyVisual = visual;
            currentEnemy = instance;
            Debug.Log($"[CombatFlowStateMachine] Current enemy set: {instance?.definition?.displayName ?? "null"}");
        }

        public void RefreshEnemyVisual()
        {
            if (combatManager == null || combatManager.CurrentEnemy == null)
            {
                Debug.LogWarning("[CombatFlowStateMachine] RefreshEnemyVisual: CombatManager or CurrentEnemy is null!");
                currentEnemyVisual = null;
                return;
            }

            EnemyInstance targetEnemy = combatManager.CurrentEnemy;
            Debug.Log($"[CombatFlowStateMachine] RefreshEnemyVisual: Looking for visual matching enemy: {targetEnemy.definition.displayName}");

            if (enemyContainer != null)
            {
                EnemyVisual[] allVisuals = enemyContainer.GetComponentsInChildren<EnemyVisual>(true);
                foreach (var visual in allVisuals)
                {
                    if (visual != null && visual.gameObject.activeSelf && visual.EnemyInstance == targetEnemy)
                    {
                        currentEnemyVisual = visual;
                        currentEnemy = targetEnemy;
                        Debug.Log($"[CombatFlowStateMachine] RefreshEnemyVisual: Found matching active visual {visual.name}");
                        return;
                    }
                }
            }

            EnemyVisual[] allSceneVisuals = FindObjectsByType<EnemyVisual>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var visual in allSceneVisuals)
            {
                if (visual != null && visual.EnemyInstance == targetEnemy)
                {
                    currentEnemyVisual = visual;
                    currentEnemy = targetEnemy;
                    Debug.Log($"[CombatFlowStateMachine] RefreshEnemyVisual: Found matching visual via scene search: {visual.name}");
                    return;
                }
            }

            Debug.LogWarning($"[CombatFlowStateMachine] RefreshEnemyVisual: No active EnemyVisual found matching {targetEnemy.definition.displayName}!");
            currentEnemyVisual = null;
        }

        private void OnActionButtonClicked()
        {
            if (stateMachine.Current is WaitingForPlayerState)
            {
                stateMachine.TryGo<DiscardingCardsState>();
            }
            else if (stateMachine.Current is ReadyToDealAgainState)
            {
                stateMachine.TryGo<ClearingHandState>();
            }
        }

        private void OnCombatStarted(CombatStartedEvent evt)
        {
            Debug.Log("[CombatFlowStateMachine] Combat started event received! Waiting for combatants to spawn...");
            
            if (playerVisual == null)
            {
                playerVisual = FindFirstObjectByType<PlayerVisual>();
                if (playerVisual != null)
                {
                    playerStartPosition = playerVisual.transform.position;
                    Debug.Log($"[CombatFlowStateMachine] Auto-found PlayerVisual at {playerStartPosition}");
                }
                else
                {
                    Debug.LogWarning("[CombatFlowStateMachine] PlayerVisual not found! Player attack animations may not work.");
                }
            }

            if (enemyContainer == null)
            {
                var enemySpawns = GameObject.Find("EnemySpawns");
                if (enemySpawns != null)
                {
                    enemyContainer = enemySpawns.transform;
                    Debug.Log($"[CombatFlowStateMachine] Auto-found EnemySpawns container");
                }
            }
        }
        
        public void OnCombatantsSpawned()
        {
            Debug.Log("[CombatFlowStateMachine] OnCombatantsSpawned called!");
            
            if (playerVisual == null)
            {
                playerVisual = FindFirstObjectByType<PlayerVisual>();
                if (playerVisual != null)
                {
                    playerStartPosition = playerVisual.transform.position;
                    Debug.Log($"[CombatFlowStateMachine] Found PlayerVisual at {playerStartPosition}");
                }
            }

            if (enemyContainer == null)
            {
                var enemySpawns = GameObject.Find("EnemySpawns");
                if (enemySpawns != null)
                {
                    enemyContainer = enemySpawns.transform;
                    Debug.Log("[CombatFlowStateMachine] Auto-found EnemySpawns container");
                }
            }

            RefreshEnemyVisual();

            if (introController != null)
            {
                introController.SetPlayerVisual(playerVisual);
                introController.SetEnemyContainer(enemyContainer);
            }

            Debug.Log("[CombatFlowStateMachine] Transitioning to CombatIntroState...");
            stateMachine.TryGo<CombatIntroState>();
        }

        private void OnHandDealt(HandDealtEvent evt)
        {
            Debug.Log($"[CombatFlowStateMachine] Hand dealt event received! Cards: {evt.cards?.Count ?? 0}");
            
            if (cardHandUI != null)
            {
                cardHandUI.DealHand(evt.cards);
                Debug.Log("[CombatFlowStateMachine] Dealt cards to CardHandUI");
            }
            else
            {
                Debug.LogWarning("[CombatFlowStateMachine] CardHandUI is null! Cannot deal cards.");
            }

            if (stateMachine.Current is ReadyToDealAgainState)
            {
                Debug.Log("[CombatFlowStateMachine] Transitioning to DealingCardsState...");
                stateMachine.TryGo<DealingCardsState>();
            }
            else
            {
                Debug.Log($"[CombatFlowStateMachine] Skipping DealingCardsState transition - current state: {stateMachine.Current?.GetType().Name}");
            }
        }

        private void OnHandEvaluated(HandEvaluatedEvent evt)
        {
            lastHandType = evt.handType;
        }

        private void OnPlayerAttack(PlayerAttackEvent evt)
        {
            currentEnemy = evt.target;
            enemyReactingState.SetReactionData(evt.damageDealt, evt.target.isDefeated, evt.isCrit);
        }

        private void OnEnemyAttack(EnemyAttackEvent evt)
        {
            enemyAttackingState.SetAttackData(evt.dodged, evt.damageDealt);
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
        }

        private void OnCombatEnded(CombatEndedEvent evt)
        {
            SetButtonEnabled(false);
        }

        public void SetButtonText(string text)
        {
            if (buttonText != null)
                buttonText.text = text;
        }

        public void SetButtonEnabled(bool enabled)
        {
            if (actionButton != null)
                actionButton.interactable = enabled;
        }

        public void RaiseDrawCardsRequest(System.Collections.Generic.List<bool> heldFlags)
        {
            EventBus<DrawCardsRequestEvent>.Raise(new DrawCardsRequestEvent
            {
                heldCardFlags = heldFlags
            });
        }
    }
}
