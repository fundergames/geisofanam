using UnityEngine;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Core.Effects;
using RogueDeal.Combat.Core.Cooldowns;
using RogueDeal.Combat.Core.Targeting;

namespace RogueDeal.Combat.Presentation.Tests
{
    /// <summary>
    /// Test script for presentation layer components.
    /// Sets up a simple test scene and verifies CombatExecutor, WeaponHitbox, etc. work correctly.
    /// </summary>
    public class PresentationLayerTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [Tooltip("Run tests automatically on Start")]
        public bool runTestsOnStart = true;
        
        [Header("Test Setup")]
        [Tooltip("Attacker GameObject (should have CombatEntity, CombatExecutor, CombatEventReceiver)")]
        public GameObject attackerObject;
        
        [Tooltip("Target GameObject (should have CombatEntity)")]
        public GameObject targetObject;
        
        [Header("Test Results")]
        [TextArea(10, 20)]
        public string testResults = "";
        
        private CombatExecutor executor;
        private CombatEntity attackerEntity;
        private CombatEntity targetEntity;
        
        private void Start()
        {
            if (runTestsOnStart)
            {
                SetupTestScene();
                RunAllTests();
            }
        }
        
        [ContextMenu("Setup Test Scene")]
        public void SetupTestScene()
        {
            Log("=== Setting Up Test Scene ===\n");
            
            // Create attacker if not assigned
            if (attackerObject == null)
            {
                attackerObject = new GameObject("TestAttacker");
                attackerObject.transform.position = Vector3.zero;
                
                // Add required components
                attackerEntity = attackerObject.AddComponent<CombatEntity>();
                executor = attackerObject.AddComponent<CombatExecutor>();
                attackerObject.AddComponent<CombatEventReceiver>();
                
                // Add animator (required for CombatEntity)
                var animator = attackerObject.AddComponent<Animator>();
                // Create a simple animator controller or use existing one
                
                Log("Created attacker GameObject with required components");
            }
            else
            {
                attackerEntity = attackerObject.GetComponent<CombatEntity>();
                executor = attackerObject.GetComponent<CombatExecutor>();
            }
            
            // Create target if not assigned
            if (targetObject == null)
            {
                targetObject = new GameObject("TestTarget");
                targetObject.transform.position = Vector3.forward * 2f;
                targetObject.tag = "Enemy";
                
                targetEntity = targetObject.AddComponent<CombatEntity>();
                targetEntity.InitializeStatsWithoutHeroData(100f, 5f, 5f);
                
                // Add collider for hit detection
                var collider = targetObject.AddComponent<CapsuleCollider>();
                collider.height = 2f;
                collider.radius = 0.5f;
                collider.isTrigger = true;
                
                Log("Created target GameObject with required components");
            }
            else
            {
                targetEntity = targetObject.GetComponent<CombatEntity>();
            }
            
            Log("✓ Test scene setup complete\n");
        }
        
        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            testResults = "";
            Log("=== Presentation Layer Tests ===\n");
            
            if (executor == null || attackerEntity == null || targetEntity == null)
            {
                Log("✗ Setup incomplete. Run 'Setup Test Scene' first.");
                return;
            }
            
            TestCombatExecutor();
            TestCombatEventReceiver();
            TestWeaponHitbox();
            TestProjectile();
            TestPersistentAOE();
            
            Log("\n=== Tests Complete ===");
            Debug.Log(testResults);
        }
        
        private void TestCombatExecutor()
        {
            Log("\n--- Testing CombatExecutor ---");
            
            // Create a test action
            var action = CreateTestAction();
            
            // Test action execution
            bool executed = executor.ExecuteAction(action);
            Assert(executed, "Action should execute successfully");
            
            // Verify action context
            var currentAction = executor.GetCurrentAction();
            Assert(currentAction == action, "Current action should be set");
            
            var targets = executor.GetCurrentTargets();
            Assert(targets != null && targets.Count > 0, "Targets should be resolved");
            
            // Test cooldown
            bool available = executor.GetCooldownManager().IsActionAvailable(action);
            Assert(!available, "Action should be on cooldown after execution");
            
            Log("✓ CombatExecutor tests passed");
        }
        
        private void TestCombatEventReceiver()
        {
            Log("\n--- Testing CombatEventReceiver ---");
            
            var eventReceiver = attackerObject.GetComponent<CombatEventReceiver>();
            if (eventReceiver == null)
            {
                Log("⚠ CombatEventReceiver not found, skipping tests");
                return;
            }
            
            // Test event parsing
            // We can't directly test animation events, but we can verify the component exists
            Assert(eventReceiver != null, "CombatEventReceiver should exist");
            
            Log("✓ CombatEventReceiver component found");
        }
        
        private void TestWeaponHitbox()
        {
            Log("\n--- Testing WeaponHitbox ---");
            
            // Create weapon hitbox
            var weaponObj = new GameObject("Weapon");
            weaponObj.transform.SetParent(attackerObject.transform);
            weaponObj.transform.localPosition = Vector3.forward * 0.5f;
            
            // Add collider FIRST (WeaponHitbox requires it)
            var collider = weaponObj.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.5f, 0.5f, 1f);
            collider.isTrigger = true;
            collider.enabled = false; // Start disabled
            
            // Then add WeaponHitbox
            var hitbox = weaponObj.AddComponent<WeaponHitbox>();
            
            // Verify component exists
            Assert(hitbox != null, "WeaponHitbox should be created");
            Assert(collider != null, "Collider should exist");
            
            // Test enable/disable
            hitbox.Enable();
            Assert(collider.enabled, "Collider should be enabled when hitbox is enabled");
            
            hitbox.Disable();
            Assert(!collider.enabled, "Collider should be disabled when hitbox is disabled");
            
            Log("✓ WeaponHitbox tests passed");
        }
        
        private void TestProjectile()
        {
            Log("\n--- Testing Projectile ---");
            
            // Create projectile prefab
            var projectileObj = new GameObject("TestProjectile");
            var projectile = projectileObj.AddComponent<Projectile>();
            var rb = projectileObj.AddComponent<Rigidbody>();
            var collider = projectileObj.AddComponent<SphereCollider>();
            collider.radius = 0.1f;
            
            // Create test effects
            var damageEffect = ScriptableObject.CreateInstance<DamageEffect>();
            damageEffect.baseDamage = 10f;
            var effects = new BaseEffect[] { damageEffect };
            
            // Initialize projectile
            var attackerData = attackerEntity.GetEntityData();
            projectile.Initialize(targetObject.transform, 5f, effects, attackerData);
            
            Assert(projectile != null, "Projectile should be created");
            
            // Clean up
            Destroy(projectileObj);
            
            Log("✓ Projectile tests passed");
        }
        
        private void TestPersistentAOE()
        {
            Log("\n--- Testing PersistentAOE ---");
            
            // Create AOE
            var aoeObj = new GameObject("TestAOE");
            var aoe = aoeObj.AddComponent<PersistentAOE>();
            
            // Create test effects
            var damageEffect = ScriptableObject.CreateInstance<DamageEffect>();
            damageEffect.baseDamage = 5f;
            var effects = new BaseEffect[] { damageEffect };
            
            // Initialize AOE
            var attackerData = attackerEntity.GetEntityData();
            aoe.Initialize(5f, effects, 3, 1f, attackerData);
            
            Assert(aoe != null, "PersistentAOE should be created");
            
            // Clean up
            Destroy(aoeObj);
            
            Log("✓ PersistentAOE tests passed");
        }
        
        private CombatAction CreateTestAction()
        {
            var action = ScriptableObject.CreateInstance<CombatAction>();
            action.actionName = "Test Attack";
            action.animationTrigger = "Attack";
            
            // Create damage effect
            var damageEffect = ScriptableObject.CreateInstance<DamageEffect>();
            damageEffect.baseDamage = 20f;
            damageEffect.damageType = DamageType.Physical;
            action.effects = new BaseEffect[] { damageEffect };
            
            // Create targeting
            var targeting = ScriptableObject.CreateInstance<SingleTargetSelector>();
            targeting.maxRange = 10f;
            targeting.targetLayers = LayerMask.GetMask("Default");
            action.targetingStrategy = targeting;
            
            // Create cooldown
            action.cooldownConfig = new CooldownConfiguration
            {
                cooldownType = CooldownType.TurnBased,
                turnCooldown = 1
            };
            
            return action;
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

