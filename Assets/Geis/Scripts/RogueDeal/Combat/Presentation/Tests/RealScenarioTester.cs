using UnityEngine;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Core.Effects;
using RogueDeal.Combat.Core.Cooldowns;
using RogueDeal.Combat.Core.Targeting;
using RogueDeal.Player;
using RogueDeal.Combat.Presentation;

namespace RogueDeal.Combat.Presentation.Tests
{
    /// <summary>
    /// Tests the combat system in a real scenario with actual gameplay flow.
    /// Demonstrates: action execution, hit detection, effects, cooldowns.
    /// </summary>
    public class RealScenarioTester : MonoBehaviour
    {
        [Header("Scene Setup")]
        [Tooltip("HeroData for the attacker (will instantiate prefab from HeroVisualData)")]
        public HeroData attackerHeroData;
        
        [Tooltip("Transform position to spawn the attacker (if null, uses default position)")]
        public Transform attackerSpawnPoint;
        
        [Tooltip("HeroData for the target (will instantiate prefab from HeroVisualData)")]
        public HeroData targetHeroData;
        
        [Tooltip("Transform position to spawn the target (if null, uses default position)")]
        public Transform targetSpawnPoint;
        
        [Header("Test Actions")]
        [Tooltip("Actions to test (will be created if empty)")]
        public CombatAction[] testActions;
        
        [Header("Controls")]
        [Tooltip("Key to execute next action")]
        public KeyCode executeActionKey = KeyCode.Space;
        
        [Tooltip("Key to advance turn (for turn-based)")]
        public KeyCode advanceTurnKey = KeyCode.T;
        
        [Header("Debug Info")]
        [TextArea(5, 10)]
        public string combatLog = "";
        
        private GameObject attacker;
        private GameObject target;
        private CombatExecutor executor;
        private CombatEntity attackerEntity;
        private CombatEntity targetEntity;
        private int currentActionIndex = 0;
        private int turnNumber = 1;
        
        private void Start()
        {
            SetupScene();
            CreateTestActions();
            LogCombatState();
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(executeActionKey))
            {
                ExecuteNextAction();
            }
            
            if (Input.GetKeyDown(advanceTurnKey))
            {
                AdvanceTurn();
            }
        }
        
        private void SetupScene()
        {
            // Get spawn positions and rotations from transforms or use defaults
            Vector3 attackerPos = attackerSpawnPoint != null ? attackerSpawnPoint.position : new Vector3(0, 0, 0);
            Quaternion attackerRot = attackerSpawnPoint != null ? attackerSpawnPoint.rotation : Quaternion.identity;
            
            Vector3 targetPos = targetSpawnPoint != null ? targetSpawnPoint.position : new Vector3(0, 0, 3);
            Quaternion targetRot = targetSpawnPoint != null ? targetSpawnPoint.rotation : Quaternion.identity;
            
            // Create attacker from HeroData
            attacker = CreateEntityFromHeroData(attackerHeroData, attackerPos, attackerRot, "Attacker", true);
            
            if (attacker != null)
            {
                attackerEntity = attacker.GetComponent<CombatEntity>();
                executor = attacker.GetComponent<CombatExecutor>();
                
                if (attackerEntity == null)
                {
                    Debug.LogError("[RealScenarioTester] Attacker missing CombatEntity component!");
                }
                
                if (executor == null)
                {
                    Debug.LogError("[RealScenarioTester] Attacker missing CombatExecutor component!");
                }
            }
            
            // Create target from HeroData
            target = CreateEntityFromHeroData(targetHeroData, targetPos, targetRot, "Target", false);
            
            if (target != null)
            {
                targetEntity = target.GetComponent<CombatEntity>();
                
                // Ensure target has proper tag and collider
                target.tag = "Enemy";
                var collider = target.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.isTrigger = true;
                }
            }
            
            if (attackerEntity == null || executor == null || targetEntity == null)
            {
                Debug.LogWarning("[RealScenarioTester] Failed to create entities. Check HeroData assignments.");
            }
            else
            {
                Log($"✓ Created attacker from {(attackerHeroData != null ? attackerHeroData.name : "default")} at {attackerPos}");
                Log($"✓ Created target from {(targetHeroData != null ? targetHeroData.name : "default")} at {targetPos}");
            }
        }
        
        /// <summary>
        /// Creates a combat entity GameObject from HeroData at the specified position and rotation.
        /// </summary>
        private GameObject CreateEntityFromHeroData(HeroData heroData, Vector3 position, Quaternion rotation, string defaultName, bool isAttacker)
        {
            GameObject entity = null;
            
            // Try to use prefab from HeroData if available
            if (heroData != null && heroData.HeroVisualData != null && heroData.HeroVisualData.characterPrefab != null)
            {
                entity = Instantiate(heroData.HeroVisualData.characterPrefab, position, rotation);
                entity.name = $"{defaultName} ({heroData.PlayerName})";
                Debug.Log($"[RealScenarioTester] Instantiated prefab from HeroData: {heroData.name}");
            }
            
            // Fallback to primitive if no prefab
            if (entity == null)
            {
                entity = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                entity.name = defaultName;
                entity.transform.position = position;
                entity.transform.rotation = rotation;
                entity.transform.localScale = new Vector3(1, 1, 1);
                
                // Remove primitive collider (we'll add a proper one)
                var primitiveCollider = entity.GetComponent<Collider>();
                if (primitiveCollider != null)
                {
                    Destroy(primitiveCollider);
                }
                
                // Color for visibility
                var renderer = entity.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = isAttacker ? Color.blue : Color.red;
                }
            }
            
            // Add combat components (if not already present)
            var combatEntity = entity.GetComponent<CombatEntity>();
            if (combatEntity == null)
            {
                combatEntity = entity.AddComponent<CombatEntity>();
            }
            
            // Initialize from HeroData or use defaults
            if (heroData != null)
            {
                combatEntity.SetHeroData(heroData);
            }
            else
            {
                // Use defaults based on role
                if (isAttacker)
                {
                    combatEntity.InitializeStatsWithoutHeroData(100f, 15f, 5f);
                }
                else
                {
                    combatEntity.InitializeStatsWithoutHeroData(100f, 10f, 5f);
                }
            }
            
            // Attacker-specific setup
            if (isAttacker)
            {
                // Add executor and event receiver if not present
                if (entity.GetComponent<CombatExecutor>() == null)
                {
                    entity.AddComponent<CombatExecutor>();
                }
                if (entity.GetComponent<CombatEventReceiver>() == null)
                {
                    entity.AddComponent<CombatEventReceiver>();
                }
                
                // Add animation test helper
                if (entity.GetComponent<AnimationTestHelper>() == null)
                {
                    entity.AddComponent<AnimationTestHelper>();
                }
            }
            
            // Get or add animator
            var animator = entity.GetComponent<Animator>();
            if (animator == null)
            {
                animator = entity.GetComponentInChildren<Animator>();
            }
            if (animator == null)
            {
                animator = entity.AddComponent<Animator>();
            }
            
            // Assign animator controller from HeroData if available
            if (heroData != null && heroData.AnimatorData != null && heroData.AnimatorData.battleAnimator != null)
            {
                animator.runtimeAnimatorController = heroData.AnimatorData.battleAnimator;
                Debug.Log($"[RealScenarioTester] Assigned animator controller from HeroData: {heroData.name}");
            }
            
            // Ensure collider exists
            var collider = entity.GetComponent<Collider>();
            if (collider == null)
            {
                collider = entity.AddComponent<CapsuleCollider>();
                if (collider is CapsuleCollider capsule)
                {
                    capsule.height = 2f;
                    capsule.radius = 0.5f;
                }
            }
            
            // Create weapon hitbox for attacker
            if (isAttacker)
            {
                CreateWeaponHitbox(entity);
            }
            
            return entity;
        }
        
        /// <summary>
        /// Creates a weapon hitbox for the attacker.
        /// </summary>
        private void CreateWeaponHitbox(GameObject parent)
        {
            // Check if weapon hitbox already exists
            var existingHitbox = parent.GetComponentInChildren<WeaponHitbox>();
            if (existingHitbox != null)
            {
                return; // Already has a weapon hitbox
            }
            
            var weapon = new GameObject("Weapon");
            weapon.transform.SetParent(parent.transform);
            weapon.transform.localPosition = new Vector3(0, 1, 0.5f);
            
            var collider = weapon.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.3f, 0.3f, 1f);
            collider.isTrigger = true;
            collider.enabled = false;
            
            var hitbox = weapon.AddComponent<WeaponHitbox>();
            hitbox.targetLayers = LayerMask.GetMask("Default");
            hitbox.validTargetTags = new string[] { "Enemy" };
        }
        
        private void CreateTestActions()
        {
            if (testActions != null && testActions.Length > 0)
            {
                // Validate loaded actions
                bool hasValidActions = true;
                foreach (var action in testActions)
                {
                    if (action == null) continue;
                    
                    bool isValid = ValidateAction(action);
                    if (!isValid)
                    {
                        hasValidActions = false;
                        Log($"⚠ Action '{action.actionName}' is misconfigured (missing effects, trigger, or targeting)");
                    }
                }
                
                if (hasValidActions)
                {
                    Log($"Using {testActions.Length} action(s) assigned in Inspector");
                    return;
                }
                else
                {
                    Log("Some actions are misconfigured. Creating fallback actions...");
                }
            }
            
            Log("No valid actions assigned. Attempting to load from Resources...");
            
            // Try to load from Resources first
            var loadedActions = new System.Collections.Generic.List<CombatAction>();
            
            // Try to load Action_FireSlash
            var fireSlashAsset = Resources.Load<CombatAction>("Combat/TestAssets/Action_FireSlash");
            if (fireSlashAsset != null)
            {
                if (ValidateAction(fireSlashAsset))
                {
                    loadedActions.Add(fireSlashAsset);
                    Log($"✓ Loaded and validated Action_FireSlash from Resources");
                }
                else
                {
                    Log($"⚠ Action_FireSlash from Resources is misconfigured. Will create fallback.");
                }
            }
            
            // If we loaded some valid actions, use them
            if (loadedActions.Count > 0)
            {
                testActions = loadedActions.ToArray();
                Log($"✓ Using {testActions.Length} action(s) loaded from Resources");
                return;
            }
            
            // Fallback: Create actions programmatically
            Log("No valid actions found. Creating test actions programmatically...");
            
            // Create Basic Attack
            var basicAttack = CreateBasicAttack();
            
            // Create Fire Slash (with burn)
            var fireSlash = CreateFireSlash();
            
            // Create Multi-Hit Combo
            var comboAttack = CreateComboAttack();
            
            testActions = new CombatAction[] { basicAttack, fireSlash, comboAttack };
            
            Log("✓ Created 3 test actions: Basic Attack, Fire Slash, Combo Attack");
        }
        
        /// <summary>
        /// Validates that an action has the minimum required configuration
        /// </summary>
        private bool ValidateAction(CombatAction action)
        {
            if (action == null) return false;
            
            // Check if it has effects
            bool hasEffects = action.effects != null && action.effects.Length > 0;
            
            // Check if it has targeting
            bool hasTargeting = action.targetingStrategy != null;
            
            // Check if it has animation
            // For combos: combo animations are sufficient
            // For non-combos: need animation trigger
            bool hasAnimation = false;
            if (action.isCombo)
            {
                // Combo needs valid combo animations
                hasAnimation = action.comboAnimations != null && 
                              action.comboAnimations.Length > 0 &&
                              action.comboAnimations[0] != null;
            }
            else
            {
                // Non-combo needs animation trigger
                hasAnimation = !string.IsNullOrEmpty(action.animationTrigger);
            }
            
            return hasEffects && hasTargeting && hasAnimation;
        }
        
        private CombatAction CreateBasicAttack()
        {
            var action = ScriptableObject.CreateInstance<CombatAction>();
            action.actionName = "Basic Attack";
            action.description = "A simple melee attack";
            // Uses Attack_1 trigger from your animator
            action.animationTrigger = "Attack_1";
            
            // Damage effect
            var damage = ScriptableObject.CreateInstance<DamageEffect>();
            damage.baseDamage = 25f;
            damage.damageType = DamageType.Physical;
            damage.scalingStat = StatType.Attack;
            damage.scalingMultiplier = 1f;
            action.effects = new BaseEffect[] { damage };
            
            // Targeting
            var targeting = ScriptableObject.CreateInstance<SingleTargetSelector>();
            targeting.maxRange = 5f;
            targeting.targetLayers = LayerMask.GetMask("Default");
            action.targetingStrategy = targeting;
            
            // No cooldown
            action.cooldownConfig = new CooldownConfiguration
            {
                cooldownType = CooldownType.None
            };
            
            return action;
        }
        
        private CombatAction CreateFireSlash()
        {
            var action = ScriptableObject.CreateInstance<CombatAction>();
            action.actionName = "Fire Slash";
            action.description = "A fiery attack that deals physical and fire damage, and applies burn";
            // Uses Attack_2 trigger (you can change this to CastSpell if it's a spell)
            action.animationTrigger = "Attack_2";
            
            // Physical damage
            var physicalDamage = ScriptableObject.CreateInstance<DamageEffect>();
            physicalDamage.baseDamage = 30f;
            physicalDamage.damageType = DamageType.Physical;
            physicalDamage.scalingStat = StatType.Attack;
            
            // Fire damage
            var fireDamage = ScriptableObject.CreateInstance<DamageEffect>();
            fireDamage.baseDamage = 15f;
            fireDamage.damageType = DamageType.Fire;
            fireDamage.scalingStat = StatType.Magic;
            
            // Burn status
            var burn = ScriptableObject.CreateInstance<StatusEffect>();
            burn.statusType = StatusEffectType.Burning;
            burn.stacks = 1;
            burn.damagePerStack = 5;
            burn.duration = 3;
            
            // Multi-effect
            var multiEffect = ScriptableObject.CreateInstance<MultiEffect>();
            multiEffect.effects = new BaseEffect[] { physicalDamage, fireDamage, burn };
            action.effects = new BaseEffect[] { multiEffect };
            
            // Targeting
            var targeting = ScriptableObject.CreateInstance<SingleTargetSelector>();
            targeting.maxRange = 5f;
            targeting.targetLayers = LayerMask.GetMask("Default");
            action.targetingStrategy = targeting;
            
            // 3 turn cooldown
            action.cooldownConfig = new CooldownConfiguration
            {
                cooldownType = CooldownType.TurnBased,
                turnCooldown = 3
            };
            
            return action;
        }
        
        private CombatAction CreateComboAttack()
        {
            var action = ScriptableObject.CreateInstance<CombatAction>();
            action.actionName = "Whirlwind Slash";
            action.description = "A 3-hit combo attack";
            // Starts with Attack_1, then Attack_2, then Attack_3
            action.animationTrigger = "Attack_1";
            action.isCombo = true;
            action.comboHitCount = 3;
            // Combo animations (if you want to use specific clips)
            // action.comboAnimations = new AnimationClip[] { attack1Clip, attack2Clip, attack3Clip };
            
            // Main damage (for all hits)
            var damage = ScriptableObject.CreateInstance<DamageEffect>();
            damage.baseDamage = 20f;
            damage.damageType = DamageType.Physical;
            damage.scalingStat = StatType.Attack;
            action.effects = new BaseEffect[] { damage };
            
            // Last hit does bonus damage
            var bonusDamage = ScriptableObject.CreateInstance<DamageEffect>();
            bonusDamage.baseDamage = 40f;
            bonusDamage.damageType = DamageType.Physical;
            bonusDamage.scalingStat = StatType.Attack;
            action.perHitEffects = new BaseEffect[] { damage, damage, bonusDamage };
            
            // Targeting
            var targeting = ScriptableObject.CreateInstance<SingleTargetSelector>();
            targeting.maxRange = 5f;
            targeting.targetLayers = LayerMask.GetMask("Default");
            action.targetingStrategy = targeting;
            
            // 2 turn cooldown
            action.cooldownConfig = new CooldownConfiguration
            {
                cooldownType = CooldownType.TurnBased,
                turnCooldown = 2
            };
            
            return action;
        }
        
        [ContextMenu("Execute Next Action")]
        public void ExecuteNextAction()
        {
            if (testActions == null || testActions.Length == 0)
            {
                Log("No actions available!");
                return;
            }
            
            if (executor == null)
            {
                Log("CombatExecutor not found!");
                return;
            }
            
            var action = testActions[currentActionIndex];
            Log($"\n--- Turn {turnNumber}: Attempting {action.actionName} ---");
            
            bool executed = executor.ExecuteAction(action);
            
            if (executed)
            {
                Log($"✓ {action.actionName} executed!");
                Log($"  Animation: {action.animationTrigger}");
                Log($"  Effects: {action.effects?.Length ?? 0} effect(s)");
                
                // Check cooldown
                var cooldownRemaining = executor.GetCooldownManager().GetCooldownRemaining(action);
                if (cooldownRemaining > 0)
                {
                    Log($"  Cooldown: {cooldownRemaining} turns remaining");
                }
            }
            else
            {
                Log($"✗ {action.actionName} failed to execute (check cooldown or targeting)");
            }
            
            // Cycle to next action
            currentActionIndex = (currentActionIndex + 1) % testActions.Length;
            
            LogCombatState();
        }
        
        [ContextMenu("Advance Turn")]
        public void AdvanceTurn()
        {
            turnNumber++;
            Log($"\n--- Turn {turnNumber} Started ---");
            
            if (executor != null)
            {
                executor.OnTurnStart();
            }
            
            if (targetEntity != null)
            {
                var targetData = targetEntity.GetEntityData();
                if (targetData != null)
                {
                    targetData.OnTurnStart();
                    Log($"Target processed status effects. HP: {targetData.currentHealth:F1}/{targetData.maxHealth:F1}");
                }
            }
            
            LogCombatState();
        }
        
        private void LogCombatState()
        {
            if (attackerEntity == null || targetEntity == null) return;
            
            var attackerData = attackerEntity.GetEntityData();
            var targetData = targetEntity.GetEntityData();
            
            if (attackerData == null || targetData == null) return;
            
            combatLog = $"=== Combat State ===\n";
            combatLog += $"Turn: {turnNumber}\n\n";
            combatLog += $"Attacker:\n";
            combatLog += $"  HP: {attackerData.currentHealth:F1}/{attackerData.maxHealth:F1}\n";
            combatLog += $"  Attack: {attackerData.attack:F1}\n";
            combatLog += $"  Status Effects: {attackerData.activeStatusEffects.Count}\n\n";
            combatLog += $"Target:\n";
            combatLog += $"  HP: {targetData.currentHealth:F1}/{targetData.maxHealth:F1}\n";
            combatLog += $"  Defense: {targetData.defense:F1}\n";
            combatLog += $"  Status Effects: {targetData.activeStatusEffects.Count}\n";
            
            if (targetData.activeStatusEffects.Count > 0)
            {
                combatLog += $"  Active Effects:\n";
                foreach (var effect in targetData.activeStatusEffects)
                {
                    combatLog += $"    - {effect.type} ({effect.duration} turns, {effect.stacks} stacks)\n";
                }
            }
            
            combatLog += $"\nControls:\n";
            combatLog += $"  [{executeActionKey}] - Execute Next Action\n";
            combatLog += $"  [{advanceTurnKey}] - Advance Turn\n";
        }
        
        private void Log(string message)
        {
            Debug.Log($"[RealScenarioTester] {message}");
            combatLog += message + "\n";
        }
        
        private void OnGUI()
        {
            // Display combat log in top-left corner
            if (!string.IsNullOrEmpty(combatLog))
            {
                GUI.Box(new Rect(10, 10, 400, 300), "");
                GUI.Label(new Rect(20, 20, 380, 280), combatLog);
            }
        }
    }
}

