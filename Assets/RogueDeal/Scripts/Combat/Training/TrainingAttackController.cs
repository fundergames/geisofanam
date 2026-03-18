using UnityEngine;
using UnityEngine.InputSystem;
using RogueDeal.Combat.TurnBased;
using System.Collections.Generic;

namespace RogueDeal.Combat.Training
{
    public class TrainingAttackController : MonoBehaviour
    {
        [Header("Combat References")]
        [SerializeField] private TurnBasedCombatPresenter combatPresenter;
        [SerializeField] private CombatEntity playerEntity;
        [SerializeField] private CombatEntity dummyEntity;
        
        [Header("Abilities to Test")]
        [SerializeField] private List<AbilityData> testAbilities = new List<AbilityData>();
        
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
            if (combatPresenter == null)
            {
                combatPresenter = FindObjectOfType<TurnBasedCombatPresenter>();
                if (combatPresenter != null && showDebugLogs)
                {
                    Debug.Log("[TrainingAttackController] Found TurnBasedCombatPresenter");
                }
            }
            
            if (playerEntity == null)
            {
                CombatEntity[] entities = FindObjectsOfType<CombatEntity>();
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
                TrainingDummy dummy = FindObjectOfType<TrainingDummy>();
                if (dummy != null)
                {
                    dummyEntity = dummy.GetComponent<CombatEntity>();
                    if (dummyEntity != null && showDebugLogs)
                    {
                        Debug.Log($"[TrainingAttackController] Found dummy entity: {dummy.gameObject.name}");
                    }
                }
            }
            
            if (testAbilities.Count == 0)
            {
                AbilityData[] abilities = Resources.LoadAll<AbilityData>("Combat/Abilities");
                if (abilities.Length > 0)
                {
                    testAbilities.AddRange(abilities);
                    if (showDebugLogs)
                    {
                        Debug.Log($"[TrainingAttackController] Loaded {abilities.Length} abilities from Resources");
                    }
                }
            }
        }
        
        private void ValidateSetup()
        {
            bool isValid = true;
            
            if (combatPresenter == null)
            {
                Debug.LogError("[TrainingAttackController] Missing TurnBasedCombatPresenter! Assign it in the Inspector.");
                isValid = false;
            }
            
            if (playerEntity == null)
            {
                Debug.LogError("[TrainingAttackController] Missing Player CombatEntity! Assign it in the Inspector.");
                isValid = false;
            }
            else
            {
                if (playerEntity.stats == null)
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
                if (dummyEntity.stats == null)
                {
                    Debug.LogWarning($"[TrainingAttackController] Dummy {dummyEntity.gameObject.name} has no stats! This should be handled by TrainingDummy component.");
                }
            }
            
            if (testAbilities.Count == 0)
            {
                Debug.LogWarning("[TrainingAttackController] No abilities assigned! Add some in the Inspector or create them in Resources/Combat/Abilities.");
            }
            
            if (isValid && showDebugLogs)
            {
                Debug.Log("[TrainingAttackController] Setup complete! Press SPACE to attack, or 1-5 for specific abilities.");
                if (testAbilities.Count > 0)
                {
                    Debug.Log($"[TrainingAttackController] Loaded abilities:");
                    for (int i = 0; i < testAbilities.Count; i++)
                    {
                        Debug.Log($"  [{i + 1}] {testAbilities[i].abilityName}");
                    }
                }
            }
        }
        
        public void PerformQuickAttack()
        {
            if (testAbilities.Count > 0)
            {
                PerformAbility(0);
            }
            else
            {
                Debug.LogWarning("[TrainingAttackController] No abilities available for quick attack!");
            }
        }
        
        public void PerformAbility(int index)
        {
            if (isAttacking)
            {
                if (showDebugLogs)
                {
                    Debug.Log("[TrainingAttackController] Already attacking, ignoring input");
                }
                return;
            }
            
            if (index < 0 || index >= testAbilities.Count)
            {
                Debug.LogWarning($"[TrainingAttackController] Ability index {index} out of range (have {testAbilities.Count} abilities)");
                return;
            }
            
            if (combatPresenter == null || playerEntity == null || dummyEntity == null)
            {
                Debug.LogError("[TrainingAttackController] Missing required references!");
                return;
            }
            
            AbilityData ability = testAbilities[index];
            
            if (ability == null)
            {
                Debug.LogWarning($"[TrainingAttackController] Ability at index {index} is null!");
                return;
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"[TrainingAttackController] Executing ability: {ability.abilityName}");
            }
            
            isAttacking = true;
            combatPresenter.ExecuteTurnBasedAbility(playerEntity, ability, dummyEntity);
            StartCoroutine(WaitForAttackComplete());
        }
        
        private System.Collections.IEnumerator WaitForAttackComplete()
        {
            yield return new WaitUntil(() => combatPresenter.IsExecutionComplete);
            isAttacking = false;
        }
        
        public void SetPlayerEntity(CombatEntity player)
        {
            playerEntity = player;
        }
        
        public void SetDummyEntity(CombatEntity dummy)
        {
            dummyEntity = dummy;
        }
        
        public void AddAbility(AbilityData ability)
        {
            if (!testAbilities.Contains(ability))
            {
                testAbilities.Add(ability);
            }
        }
        
        public void ClearAbilities()
        {
            testAbilities.Clear();
        }
        
        [ContextMenu("Print Current Setup")]
        public void PrintSetup()
        {
            Debug.Log("=== Training Attack Controller Setup ===");
            Debug.Log($"Combat Presenter: {(combatPresenter != null ? combatPresenter.name : "NULL")}");
            Debug.Log($"Player Entity: {(playerEntity != null ? playerEntity.gameObject.name : "NULL")}");
            Debug.Log($"Dummy Entity: {(dummyEntity != null ? dummyEntity.gameObject.name : "NULL")}");
            Debug.Log($"Abilities ({testAbilities.Count}):");
            for (int i = 0; i < testAbilities.Count; i++)
            {
                Debug.Log($"  [{i + 1}] {(testAbilities[i] != null ? testAbilities[i].abilityName : "NULL")}");
            }
        }
    }
}
