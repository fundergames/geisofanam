using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using RogueDeal.Combat.Presentation;
using RogueDeal.Combat.Core.Data;

namespace RogueDeal.Combat.Training
{
    public class TrainingAttackController : MonoBehaviour
    {
        [Header("Combat References")]
        [SerializeField] private CombatExecutor combatExecutor;
        [SerializeField] private CombatEntity playerEntity;
        [SerializeField] private CombatEntity dummyEntity;

        [Header("Actions to Test")]
        [SerializeField] private List<CombatAction> testActions = new List<CombatAction>();
        
        [Header("Attack Controls")]
        [SerializeField] private Key quickAttackKey = Key.Space;
        [SerializeField] private Key ability1Key = Key.Digit1;
        [SerializeField] private Key ability2Key = Key.Digit2;
        [SerializeField] private Key ability3Key = Key.Digit3;
        [SerializeField] private Key ability4Key = Key.Digit4;
        [SerializeField] private Key ability5Key = Key.Digit5;
        
        [Header("Settings")]
        [SerializeField] private bool autoFindReferences = true;
        [SerializeField] private bool showDebugLogs = true;
        
        private bool isAttacking = false;
        
        private void Start()
        {
            if (autoFindReferences)
            {
                FindReferences();
            }
            
            ValidateSetup();
        }
        
        private void Update()
        {
            if (Keyboard.current == null) return;
            
            if (Keyboard.current[quickAttackKey].wasPressedThisFrame)
            {
                PerformQuickAttack();
            }
            
            if (Keyboard.current[ability1Key].wasPressedThisFrame)
            {
                PerformAbility(0);
            }
            
            if (Keyboard.current[ability2Key].wasPressedThisFrame)
            {
                PerformAbility(1);
            }
            
            if (Keyboard.current[ability3Key].wasPressedThisFrame)
            {
                PerformAbility(2);
            }
            
            if (Keyboard.current[ability4Key].wasPressedThisFrame)
            {
                PerformAbility(3);
            }
            
            if (Keyboard.current[ability5Key].wasPressedThisFrame)
            {
                PerformAbility(4);
            }
        }
        
        private void FindReferences()
        {
            if (combatExecutor == null && playerEntity != null)
            {
                combatExecutor = playerEntity.GetComponent<CombatExecutor>();
                if (combatExecutor != null && showDebugLogs)
                    Debug.Log("[TrainingAttackController] Found CombatExecutor on player");
            }
            if (combatExecutor == null)
            {
                var entity = FindFirstObjectByType<CombatEntity>();
                if (entity != null)
                {
                    combatExecutor = entity.GetComponent<CombatExecutor>();
                    if (combatExecutor != null)
                        playerEntity = entity;
                }
            }

            if (playerEntity == null)
            {
                CombatEntity[] entities = FindObjectsByType<CombatEntity>(FindObjectsSortMode.None);
                foreach (var entity in entities)
                {
                    if (entity.gameObject.name.Contains("Player") || entity.gameObject.name.Contains("Hero"))
                    {
                        playerEntity = entity;
                        if (showDebugLogs)
                        {
                            Debug.Log($"[TrainingAttackController] Found player entity: {entity.gameObject.name}");
                        }
                        break;
                    }
                }
            }
            
            if (dummyEntity == null)
            {
                TrainingDummy dummy = FindFirstObjectByType<TrainingDummy>();
                if (dummy != null)
                {
                    dummyEntity = dummy.GetComponent<CombatEntity>();
                    if (dummyEntity != null && showDebugLogs)
                    {
                        Debug.Log($"[TrainingAttackController] Found dummy entity: {dummy.gameObject.name}");
                    }
                }
            }
            
            if (testActions.Count == 0)
            {
                CombatAction[] actions = Resources.LoadAll<CombatAction>("Combat/Actions");
                if (actions.Length > 0)
                {
                    testActions.AddRange(actions);
                    if (showDebugLogs)
                        Debug.Log($"[TrainingAttackController] Loaded {actions.Length} actions from Resources");
                }
            }
        }
        
        private void ValidateSetup()
        {
            bool isValid = true;

            if (combatExecutor == null)
            {
                Debug.LogError("[TrainingAttackController] Missing CombatExecutor! Assign player with CombatExecutor or assign in Inspector.");
                isValid = false;
            }

            if (playerEntity == null)
            {
                Debug.LogError("[TrainingAttackController] Missing Player CombatEntity! Assign it in the Inspector.");
                isValid = false;
            }
            else
            {
                var playerData = playerEntity.GetEntityData();
                if (playerData == null || playerData.maxHealth <= 0)
                {
                    Debug.LogWarning($"[TrainingAttackController] Player {playerEntity.gameObject.name} has no stats! Initializing default stats...");
                    playerEntity.InitializeStatsWithoutHeroData(100f, 25f, 10f);
                }
            }
            
            if (dummyEntity == null)
            {
                Debug.LogError("[TrainingAttackController] Missing Dummy CombatEntity! Assign it in the Inspector.");
                isValid = false;
            }
            else
            {
                var dummyData = dummyEntity.GetEntityData();
                if (dummyData == null || dummyData.maxHealth <= 0)
                {
                    Debug.LogWarning($"[TrainingAttackController] Dummy {dummyEntity.gameObject.name} has no stats! This should be handled by TrainingDummy component.");
                }
            }
            
            if (testActions.Count == 0)
            {
                Debug.LogWarning("[TrainingAttackController] No actions assigned! Add CombatAction assets in Inspector or Resources/Combat/Actions.");
            }

            if (isValid && showDebugLogs)
            {
                Debug.Log("[TrainingAttackController] Setup complete! Press SPACE to attack, or 1-5 for specific actions.");
                if (testActions.Count > 0)
                {
                    Debug.Log("[TrainingAttackController] Loaded actions:");
                    for (int i = 0; i < testActions.Count; i++)
                    {
                        Debug.Log($"  [{i + 1}] {testActions[i].actionName}");
                    }
                }
            }
        }
        
        public void PerformQuickAttack()
        {
            if (testActions.Count > 0)
                PerformAbility(0);
            else
                Debug.LogWarning("[TrainingAttackController] No actions available for quick attack!");
        }

        public void PerformAbility(int index)
        {
            if (isAttacking)
            {
                if (showDebugLogs)
                    Debug.Log("[TrainingAttackController] Already attacking, ignoring input");
                return;
            }
            if (index < 0 || index >= testActions.Count)
            {
                Debug.LogWarning($"[TrainingAttackController] Action index {index} out of range (have {testActions.Count} actions)");
                return;
            }
            if (combatExecutor == null || playerEntity == null || dummyEntity == null)
            {
                Debug.LogError("[TrainingAttackController] Missing required references!");
                return;
            }
            CombatAction action = testActions[index];
            if (action == null)
            {
                Debug.LogWarning($"[TrainingAttackController] Action at index {index} is null!");
                return;
            }
            if (showDebugLogs)
                Debug.Log($"[TrainingAttackController] Executing action: {action.actionName}");
            isAttacking = true;
            combatExecutor.ExecuteAction(action);
            StartCoroutine(WaitForAttackComplete());
        }

        private System.Collections.IEnumerator WaitForAttackComplete()
        {
            yield return new WaitUntil(() => combatExecutor == null || !combatExecutor.IsExecuting);
            isAttacking = false;
        }

        public void SetPlayerEntity(CombatEntity player)
        {
            playerEntity = player;
            if (player != null)
                combatExecutor = player.GetComponent<CombatExecutor>();
        }

        public void SetDummyEntity(CombatEntity dummy)
        {
            dummyEntity = dummy;
        }

        public void AddAction(CombatAction action)
        {
            if (action != null && !testActions.Contains(action))
                testActions.Add(action);
        }

        public void ClearActions()
        {
            testActions.Clear();
        }

        [ContextMenu("Print Current Setup")]
        public void PrintSetup()
        {
            Debug.Log("=== Training Attack Controller Setup ===");
            Debug.Log($"Combat Executor: {(combatExecutor != null ? combatExecutor.gameObject.name : "NULL")}");
            Debug.Log($"Player Entity: {(playerEntity != null ? playerEntity.gameObject.name : "NULL")}");
            Debug.Log($"Dummy Entity: {(dummyEntity != null ? dummyEntity.gameObject.name : "NULL")}");
            Debug.Log($"Actions ({testActions.Count}):");
            for (int i = 0; i < testActions.Count; i++)
            {
                Debug.Log($"  [{i + 1}] {(testActions[i] != null ? testActions[i].actionName : "NULL")}");
            }
        }
    }
}
