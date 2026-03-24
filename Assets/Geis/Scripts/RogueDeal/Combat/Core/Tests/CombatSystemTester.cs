using UnityEngine;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Core.Effects;
using RogueDeal.Combat.Core.Cooldowns;
using RogueDeal.Combat.Core.Targeting;
using RogueDeal.Player;

namespace RogueDeal.Combat.Core.Tests
{
    /// <summary>
    /// Test script to verify core combat system functionality.
    /// Add this to a GameObject in a scene to test.
    /// </summary>
    public class CombatSystemTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [Tooltip("Run tests automatically on Start")]
        public bool runTestsOnStart = true;
        
        [Header("Test Results")]
        [TextArea(10, 20)]
        public string testResults = "";
        
        private void Start()
        {
            if (runTestsOnStart)
            {
                RunAllTests();
            }
        }
        
        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            testResults = "";
            Log("=== Combat System Tests ===\n");
            
            TestCombatEntityData();
            TestDamageEffect();
            TestStatModifierEffect();
            TestStatusEffect();
            TestMultiEffect();
            TestWeapon();
            TestCooldownManager();
            TestCombatAction();
            
            Log("\n=== Tests Complete ===");
            Debug.Log(testResults);
        }
        
        private void TestCombatEntityData()
        {
            Log("\n--- Testing CombatEntityData ---");
            
            // Create test entity
            var entity = new CombatEntityData(100f, 10f, 5f, 8f, 5f);
            
            // Test stats
            Assert(entity.maxHealth == 100f, "Max health should be 100");
            Assert(entity.currentHealth == 100f, "Current health should be 100");
            Assert(entity.attack == 10f, "Attack should be 10");
            Assert(entity.defense == 5f, "Defense should be 5");
            
            // Test damage
            entity.TakeDamage(20f);
            Assert(entity.currentHealth == 80f, "Health after 20 damage should be 80");
            
            // Test heal
            entity.Heal(10f);
            Assert(entity.currentHealth == 90f, "Health after 10 heal should be 90");
            
            // Test stat getter
            Assert(entity.GetStat(StatType.Health) == 90f, "GetStat(Health) should return 90");
            Assert(entity.GetStat(StatType.Attack) == 10f, "GetStat(Attack) should return 10");
            
            // Test clone
            var clone = entity.Clone();
            Assert(clone.currentHealth == 90f, "Clone should have same health");
            clone.TakeDamage(10f);
            Assert(entity.currentHealth == 90f, "Original should be unchanged after clone modification");
            
            // Test turn start (status effects)
            var statusEffect = new ActiveStatusEffect
            {
                type = StatusEffectType.Burning,
                stacks = 1,
                damagePerStack = 5,
                duration = 3,
                isPermanent = false
            };
            entity.activeStatusEffects.Add(statusEffect);
            
            float healthBefore = entity.currentHealth;
            entity.OnTurnStart();
            Assert(entity.currentHealth == healthBefore - 5f, "Should take 5 damage from burn");
            Assert(statusEffect.duration == 2, "Status effect duration should decrease");
            
            Log("✓ CombatEntityData tests passed");
        }
        
        private void TestDamageEffect()
        {
            Log("\n--- Testing DamageEffect ---");
            
            // Create test entities
            var attacker = new CombatEntityData(100f, 15f, 5f); // 15 attack
            var target = new CombatEntityData(100f, 10f, 5f); // 5 defense
            
            // Create damage effect
            var damageEffect = ScriptableObject.CreateInstance<DamageEffect>();
            damageEffect.baseDamage = 20f;
            damageEffect.damageType = DamageType.Physical;
            damageEffect.scalingStat = StatType.Attack;
            damageEffect.scalingMultiplier = 1f;
            damageEffect.canCrit = true;
            
            // Calculate damage
            var calculated = damageEffect.Calculate(attacker, target, null);
            
            // Expected: baseDamage (20) + attack (15) * multiplier (1) - defense (5) = 30
            Assert(calculated.damageAmount == 30f, $"Damage should be 30, got {calculated.damageAmount}");
            Assert(calculated.damageType == DamageType.Physical, "Damage type should be Physical");
            
            // Apply damage
            float healthBefore = target.currentHealth;
            damageEffect.Apply(target, calculated);
            Assert(target.currentHealth == healthBefore - calculated.damageAmount, "Target should take calculated damage");
            
            Log("✓ DamageEffect tests passed");
        }
        
        private void TestStatModifierEffect()
        {
            Log("\n--- Testing StatModifierEffect ---");
            
            var target = new CombatEntityData(100f, 10f, 5f);
            
            // Create stat modifier effect (instant)
            var statEffect = ScriptableObject.CreateInstance<StatModifierEffect>();
            statEffect.targetStat = StatType.Attack;
            statEffect.modifierType = ModifierType.Add;
            statEffect.baseValue = 5f;
            statEffect.isInstant = true;
            
            var calculated = statEffect.Calculate(null, target, null);
            statEffect.Apply(target, calculated);
            
            Assert(target.attack == 15f, $"Attack should be 15 (10 + 5), got {target.attack}");
            
            // Test multiply
            statEffect.modifierType = ModifierType.Multiply;
            statEffect.baseValue = 1.5f;
            calculated = statEffect.Calculate(null, target, null);
            statEffect.Apply(target, calculated);
            Assert(target.attack == 22.5f, $"Attack should be 22.5 (15 * 1.5), got {target.attack}");
            
            Log("✓ StatModifierEffect tests passed");
        }
        
        private void TestStatusEffect()
        {
            Log("\n--- Testing StatusEffect ---");
            
            var target = new CombatEntityData(100f, 10f, 5f);
            
            // Create status effect
            var statusEffect = ScriptableObject.CreateInstance<StatusEffect>();
            statusEffect.statusType = StatusEffectType.Burning;
            statusEffect.stacks = 1;
            statusEffect.damagePerStack = 5;
            statusEffect.duration = 3;
            
            var calculated = statusEffect.Calculate(null, target, null);
            statusEffect.Apply(target, calculated);
            
            Assert(target.activeStatusEffects.Count == 1, "Should have 1 active status effect");
            Assert(target.activeStatusEffects[0].type == StatusEffectType.Burning, "Status effect type should be Burning");
            Assert(target.activeStatusEffects[0].duration == 3, "Duration should be 3");
            
            // Process turn
            float healthBefore = target.currentHealth;
            target.OnTurnStart();
            Assert(target.currentHealth == healthBefore - 5f, "Should take 5 damage from burn");
            Assert(target.activeStatusEffects[0].duration == 2, "Duration should decrease to 2");
            
            Log("✓ StatusEffect tests passed");
        }
        
        private void TestMultiEffect()
        {
            Log("\n--- Testing MultiEffect ---");
            
            var attacker = new CombatEntityData(100f, 15f, 5f);
            var target = new CombatEntityData(100f, 10f, 5f);
            
            // Create multiple effects
            var damage1 = ScriptableObject.CreateInstance<DamageEffect>();
            damage1.baseDamage = 10f;
            damage1.damageType = DamageType.Physical;
            
            var damage2 = ScriptableObject.CreateInstance<DamageEffect>();
            damage2.baseDamage = 20f;
            damage2.damageType = DamageType.Fire;
            
            var multiEffect = ScriptableObject.CreateInstance<MultiEffect>();
            multiEffect.effects = new BaseEffect[] { damage1, damage2 };
            
            // Apply all effects
            float healthBefore = target.currentHealth;
            multiEffect.ApplyAll(attacker, target, null);
            
            // Should take damage from both effects
            Assert(target.currentHealth < healthBefore, "Target should take damage from multi-effect");
            
            Log("✓ MultiEffect tests passed");
        }
        
        private void TestWeapon()
        {
            Log("\n--- Testing Weapon ---");
            
            var weapon = ScriptableObject.CreateInstance<Weapon>();
            weapon.weaponName = "Fire Sword";
            weapon.baseDamage = 50f;
            weapon.damageTypeMultiplierArray = new DamageTypeMultiplier[]
            {
                new DamageTypeMultiplier { damageType = DamageType.Physical, multiplier = 1.0f },
                new DamageTypeMultiplier { damageType = DamageType.Fire, multiplier = 1.2f }
            };
            
            // Force build dictionary by calling RebuildMultiplierDictionary
            // (OnEnable might not be called when creating ScriptableObjects in code)
            weapon.RebuildMultiplierDictionary();
            
            // Access property to verify it works
            var multipliers = weapon.damageTypeMultipliers;
            
            // Test individual lookups
            float physicalMult = weapon.GetDamageTypeMultiplier(DamageType.Physical);
            float fireMult = weapon.GetDamageTypeMultiplier(DamageType.Fire);
            float iceMult = weapon.GetDamageTypeMultiplier(DamageType.Ice);
            
            Assert(physicalMult == 1.0f, $"Physical multiplier should be 1.0, got {physicalMult}");
            Assert(fireMult == 1.2f, $"Fire multiplier should be 1.2, got {fireMult}");
            Assert(iceMult == 1.0f, $"Ice multiplier should default to 1.0, got {iceMult}");
            
            // Also verify the dictionary property
            Assert(multipliers.ContainsKey(DamageType.Physical), "Dictionary should contain Physical");
            Assert(multipliers.ContainsKey(DamageType.Fire), "Dictionary should contain Fire");
            Assert(multipliers[DamageType.Physical] == 1.0f, "Dictionary Physical value should be 1.0");
            Assert(multipliers[DamageType.Fire] == 1.2f, "Dictionary Fire value should be 1.2");
            
            Log("✓ Weapon tests passed");
        }
        
        private void TestCooldownManager()
        {
            Log("\n--- Testing ActionCooldownManager ---");
            
            var entity = new CombatEntityData(100f, 10f, 5f);
            var cooldownManager = new ActionCooldownManager(entity);
            
            // Create test action
            var action = ScriptableObject.CreateInstance<CombatAction>();
            action.actionName = "Test Action";
            action.cooldownConfig = new CooldownConfiguration
            {
                cooldownType = CooldownType.TurnBased,
                turnCooldown = 3
            };
            
            // Should be available initially
            Assert(cooldownManager.IsActionAvailable(action), "Action should be available initially");
            
            // Start cooldown
            cooldownManager.StartCooldown(action);
            Assert(!cooldownManager.IsActionAvailable(action), "Action should be on cooldown");
            Assert(cooldownManager.GetCooldownRemaining(action) == 3f, "Cooldown should be 3 turns");
            
            // Advance turns
            cooldownManager.OnTurnStart();
            Assert(cooldownManager.GetCooldownRemaining(action) == 2f, "Cooldown should be 2 turns");
            
            cooldownManager.OnTurnStart();
            cooldownManager.OnTurnStart();
            Assert(cooldownManager.IsActionAvailable(action), "Action should be available after cooldown");
            
            Log("✓ ActionCooldownManager tests passed");
        }
        
        private void TestCombatAction()
        {
            Log("\n--- Testing CombatAction ---");
            
            var action = ScriptableObject.CreateInstance<CombatAction>();
            action.actionName = "Fire Slash";
            action.animationTrigger = "FireSlash";
            action.isCombo = false;
            action.isProjectile = false;
            action.spawnsPersistentAOE = false;
            
            // Create effects
            var damageEffect = ScriptableObject.CreateInstance<DamageEffect>();
            damageEffect.baseDamage = 50f;
            action.effects = new BaseEffect[] { damageEffect };
            
            // Create targeting
            var targeting = ScriptableObject.CreateInstance<SingleTargetSelector>();
            action.targetingStrategy = targeting;
            
            // Create cooldown
            action.cooldownConfig = new CooldownConfiguration
            {
                cooldownType = CooldownType.TurnBased,
                turnCooldown = 3
            };
            
            Assert(action.effects.Length == 1, "Action should have 1 effect");
            Assert(action.targetingStrategy != null, "Action should have targeting strategy");
            Assert(action.cooldownConfig != null, "Action should have cooldown config");
            
            Log("✓ CombatAction tests passed");
        }
        
        private void Assert(bool condition, string message)
        {
            if (!condition)
            {
                Log($"✗ FAILED: {message}");
                throw new System.Exception($"Test failed: {message}");
            }
        }
        
        private void Log(string message)
        {
            testResults += message + "\n";
        }
    }
}

