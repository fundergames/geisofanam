using Funder.Core.Events;
using Funder.Core.Randoms;
using RogueDeal.Events;
using RogueDeal.Levels;
using RogueDeal.Player;
using RogueDeal.UI;
using RogueDeal.Utils;
using UnityEngine;

namespace RogueDeal.Combat
{
    public class CombatController : MonoBehaviour
    {
        [Header("Combat Data")]
        [SerializeField] private LevelDefinition testLevel;
        
        [Header("Scene References")]
        [SerializeField] private CombatSceneManager sceneManager;
        [SerializeField] private CombatFlowStateMachine flowStateMachine;
        
        private CombatManager combatManager;
        private PlayerCharacter player;
        private bool combatActive = false;

        public CombatManager CombatManager => combatManager;

        private void Start()
        {
            Debug.Log("[CombatController] Start called - setting up camera...");
            SetupCamera();
            
            player = PlayerDataManager.Instance.CurrentPlayer;
            
            if (player == null)
            {
                Debug.LogError("No player found! Cannot start combat.");
                return;
            }

            IRandomHub randomHub = RandomHubProvider.Get();
            combatManager = new CombatManager(randomHub);
            
            if (sceneManager != null)
            {
                Debug.Log("[CombatController] Scene Manager found, setting CombatManager reference...");
                sceneManager.SetCombatManager(combatManager);
            }
            else
            {
                Debug.LogWarning("[CombatController] Scene Manager is NULL! Trying to find it in the scene...");
                sceneManager = FindFirstObjectByType<CombatSceneManager>();
                if (sceneManager != null)
                {
                    Debug.Log("[CombatController] Found CombatSceneManager via FindFirstObjectByType!");
                    sceneManager.SetCombatManager(combatManager);
                }
                else
                {
                    Debug.LogError("[CombatController] Could not find CombatSceneManager in scene!");
                }
            }

            if (flowStateMachine != null)
            {
                Debug.Log("[CombatController] Flow State Machine found, setting CombatManager reference...");
                flowStateMachine.SetCombatManager(combatManager);
            }
            else
            {
                Debug.LogWarning("[CombatController] Flow State Machine is NULL! Trying to find it in the scene...");
                flowStateMachine = FindFirstObjectByType<CombatFlowStateMachine>();
                if (flowStateMachine != null)
                {
                    Debug.Log("[CombatController] Found CombatFlowStateMachine via FindFirstObjectByType!");
                    flowStateMachine.SetCombatManager(combatManager);
                }
                else
                {
                    Debug.LogWarning("[CombatController] Could not find CombatFlowStateMachine in scene! FSM will not be active.");
                }
            }
            
            SubscribeToEvents();
            
            LevelDefinition levelToLoad = GetLevelToLoad();
            if (levelToLoad != null)
            {
                StartCombat(levelToLoad);
            }
            else
            {
                Debug.LogError("[CombatController] No level to load! Please select a level from Stage Select.");
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            EventBus<CombatStartedEvent>.Subscribe(OnCombatStarted);
            EventBus<HandDealtEvent>.Subscribe(OnHandDealt);
            EventBus<PlayerAttackEvent>.Subscribe(OnPlayerAttack);
            EventBus<EnemyAttackEvent>.Subscribe(OnEnemyAttack);
            EventBus<EnemyDefeatedEvent>.Subscribe(OnEnemyDefeated);
            EventBus<CombatEndedEvent>.Subscribe(OnCombatEnded);
            EventBus<DrawCardsRequestEvent>.Subscribe(OnDrawCardsRequest);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus<CombatStartedEvent>.Unsubscribe(OnCombatStarted);
            EventBus<HandDealtEvent>.Unsubscribe(OnHandDealt);
            EventBus<PlayerAttackEvent>.Unsubscribe(OnPlayerAttack);
            EventBus<EnemyAttackEvent>.Unsubscribe(OnEnemyAttack);
            EventBus<EnemyDefeatedEvent>.Unsubscribe(OnEnemyDefeated);
            EventBus<CombatEndedEvent>.Unsubscribe(OnCombatEnded);
            EventBus<DrawCardsRequestEvent>.Unsubscribe(OnDrawCardsRequest);
        }

        private LevelDefinition GetLevelToLoad()
        {
            if (LevelManager.Instance != null)
            {
                var selectedLevel = LevelManager.Instance.GetSelectedLevel();
                if (selectedLevel != null)
                {
                    Debug.Log($"[CombatController] Loading selected level from LevelManager: {selectedLevel.displayName}");
                    Debug.Log($"[CombatController] Selected level enemy spawns: {selectedLevel.enemySpawns?.Count ?? 0}");
                    return selectedLevel;
                }
                else
                {
                    Debug.Log("[CombatController] LevelManager exists but returned null selected level");
                }
            }
            else
            {
                Debug.Log("[CombatController] No LevelManager instance found");
            }
            
            if (testLevel != null)
            {
                Debug.LogWarning($"[CombatController] No level selected from LevelManager, using test level: {testLevel.displayName}");
                Debug.Log($"[CombatController] Test level enemy spawns: {testLevel.enemySpawns?.Count ?? 0}");
                return testLevel;
            }
            
            Debug.LogError("[CombatController] No test level configured in CombatController!");
            return null;
        }

        public void StartCombat(LevelDefinition level)
        {
            if (combatActive)
            {
                Debug.LogWarning("Combat already active!");
                return;
            }

            combatActive = true;
            combatManager.StartCombat(player, level);
        }

        private void OnDrawCardsRequest(DrawCardsRequestEvent evt)
        {
            if (!combatActive)
                return;

            combatManager.DrawCards(evt.heldCardFlags);
        }

        private void OnCombatStarted(CombatStartedEvent evt)
        {
            Debug.Log($"Combat started! Stage: {evt.stageId}, Enemies: {evt.enemyCount}");
        }

        private void OnHandDealt(HandDealtEvent evt)
        {
            Debug.Log($"New hand dealt - Turn {evt.turnNumber}");
        }

        private void OnPlayerAttack(PlayerAttackEvent evt)
        {
            Debug.Log($"Player attacked with {evt.handType} for {evt.damageDealt} damage! Crit: {evt.isCrit}");
        }

        private void OnEnemyAttack(EnemyAttackEvent evt)
        {
            if (evt.dodged)
            {
                Debug.Log($"Enemy attack dodged!");
            }
            else
            {
                Debug.Log($"Enemy dealt {evt.damageDealt} damage to player!");
            }
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
            Debug.Log($"Enemy defeated! Gold: {evt.goldDropped}, Items: {evt.itemsDropped.Length}");
        }

        private void OnCombatEnded(CombatEndedEvent evt)
        {
            combatActive = false;
            
            if (evt.playerVictory)
            {
                Debug.Log($"Victory! Turns: {evt.turnsUsed}/{evt.totalTurns}, Gold: {evt.goldEarned}, XP: {evt.xpEarned}");
            }
            else
            {
                Debug.Log("Defeat!");
            }
        }
        
        private void SetupCamera()
        {
            Debug.Log("[CombatController] SetupCamera() called");
            
            Camera mainCamera = Camera.main;
            
            if (mainCamera == null)
            {
                Debug.LogError("[CombatController] Main Camera not found! Searching for any camera...");
                mainCamera = FindFirstObjectByType<Camera>();
                if (mainCamera == null)
                {
                    Debug.LogError("[CombatController] No camera found at all!");
                    return;
                }
                Debug.Log($"[CombatController] Found camera: {mainCamera.name}");
            }
            
            Vector3 cameraPosition = new Vector3(0f, 5f, -8f);
            Vector3 lookAtPosition = new Vector3(0f, 1f, 0f);
            
            Debug.Log($"[CombatController] Moving camera from {mainCamera.transform.position} to {cameraPosition}");
            
            mainCamera.transform.position = cameraPosition;
            mainCamera.transform.LookAt(lookAtPosition);
            
            Debug.Log($"[CombatController] ✅ Camera positioned at {cameraPosition}, looking at {lookAtPosition}");
        }
    }
}
