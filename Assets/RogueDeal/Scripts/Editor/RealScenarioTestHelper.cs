using UnityEngine;
using UnityEditor;
using RogueDeal.Combat;
using RogueDeal.Combat.Presentation;
using RogueDeal.Combat.Presentation.Tests;
using RogueDeal.Player;

namespace RogueDeal.Combat.Editor
{
    /// <summary>
    /// Helper to create a complete test scene for real scenario testing.
    /// </summary>
    public class RealScenarioTestHelper
    {
        [MenuItem("Tools/Combat System/Create Real Scenario Test Scene")]
        public static void CreateTestScene()
        {
            // Create new scene
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects
            );
            
            // Create ground
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(5, 1, 5);
            
            // Try to load HeroData for attacker and target
            HeroData attackerHeroData = null;
            HeroData targetHeroData = null;
            
            // Try to find HeroData assets
            string[] guids = AssetDatabase.FindAssets("t:HeroData");
            if (guids.Length > 0)
            {
                var firstHeroData = AssetDatabase.LoadAssetAtPath<HeroData>(
                    AssetDatabase.GUIDToAssetPath(guids[0])
                );
                attackerHeroData = firstHeroData;
                
                if (guids.Length > 1)
                {
                    var secondHeroData = AssetDatabase.LoadAssetAtPath<HeroData>(
                        AssetDatabase.GUIDToAssetPath(guids[1])
                    );
                    targetHeroData = secondHeroData;
                }
                else
                {
                    targetHeroData = firstHeroData; // Use same for both if only one found
                }
            }
            
            // Note: Entities will be created dynamically by RealScenarioTester at runtime
            // We just need to set up the tester with HeroData references
            
            // Create spawn point GameObjects
            var attackerSpawnPoint = new GameObject("AttackerSpawnPoint");
            attackerSpawnPoint.transform.position = new Vector3(0, 0, 0);
            
            var targetSpawnPoint = new GameObject("TargetSpawnPoint");
            targetSpawnPoint.transform.position = new Vector3(0, 0, 3);
            
            // Create tester
            var tester = new GameObject("RealScenarioTester");
            var testerComponent = tester.AddComponent<RealScenarioTester>();
            testerComponent.attackerHeroData = attackerHeroData;
            testerComponent.attackerSpawnPoint = attackerSpawnPoint.transform;
            testerComponent.targetHeroData = targetHeroData;
            testerComponent.targetSpawnPoint = targetSpawnPoint.transform;
            
            // Add camera
            var camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = new Vector3(0, 5, -5);
                camera.transform.LookAt(new Vector3(0, 1, 1.5f));
            }
            
            // Add lighting
            var light = new GameObject("Directional Light");
            light.AddComponent<Light>().type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            
            // Save scene
            string scenePath = "Assets/Scenes/CombatRealScenarioTest.unity";
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            
            Debug.Log($"✅ Created real scenario test scene at {scenePath}");
            Debug.Log("\nScene Setup:");
            Debug.Log("  - RealScenarioTester (will create entities at runtime from HeroData)");
            if (attackerHeroData != null)
            {
                Debug.Log($"  - Attacker HeroData: {attackerHeroData.name}");
            }
            if (targetHeroData != null)
            {
                Debug.Log($"  - Target HeroData: {targetHeroData.name}");
            }
            Debug.Log("\nControls:");
            Debug.Log("  [Space] - Execute Next Action");
            Debug.Log("  [T] - Advance Turn");
            Debug.Log("\nThe tester will create 3 test actions:");
            Debug.Log("  1. Basic Attack (no cooldown)");
            Debug.Log("  2. Fire Slash (physical + fire damage + burn, 3 turn cooldown)");
            Debug.Log("  3. Whirlwind Slash (3-hit combo, 2 turn cooldown)");
            Debug.Log("\nNote: Entities are created dynamically at runtime from HeroData.");
            Debug.Log("Assign HeroData assets in the RealScenarioTester component to use your character prefabs.");
        }
        
        // Helper methods removed - entities are now created at runtime by RealScenarioTester
        // Keeping these for reference but they're no longer used
        private static GameObject CreateAttacker(HeroData heroData = null)
        {
            GameObject attacker = null;
            
            // Try to use prefab from HeroData if available
            if (heroData != null && heroData.HeroVisualData != null && heroData.HeroVisualData.characterPrefab != null)
            {
                attacker = PrefabUtility.InstantiatePrefab(heroData.HeroVisualData.characterPrefab) as GameObject;
                if (attacker != null)
                {
                    attacker.name = $"Attacker ({heroData.PlayerName})";
                    Debug.Log($"✅ Using prefab from HeroData: {heroData.name}");
                }
            }
            
            // Fallback to primitive if no prefab
            if (attacker == null)
            {
                attacker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                attacker.name = "Attacker";
                attacker.transform.localScale = new Vector3(1, 1, 1);
                Object.DestroyImmediate(attacker.GetComponent<Collider>());
                
                // Color for visibility
                var renderer = attacker.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.blue;
                }
            }
            
            // Add combat components (if not already present)
            var combatEntity = attacker.GetComponent<CombatEntity>();
            if (combatEntity == null)
            {
                combatEntity = attacker.AddComponent<CombatEntity>();
            }
            
            // Initialize stats from HeroData or use defaults
            if (heroData != null)
            {
                combatEntity.SetHeroData(heroData);
            }
            else
            {
                combatEntity.InitializeStatsWithoutHeroData(100f, 15f, 5f);
            }
            
            // Add executor and event receiver if not present
            if (attacker.GetComponent<CombatExecutor>() == null)
            {
                attacker.AddComponent<CombatExecutor>();
            }
            if (attacker.GetComponent<CombatEventReceiver>() == null)
            {
                attacker.AddComponent<CombatEventReceiver>();
            }
            
            // Get or add animator
            var animator = attacker.GetComponent<Animator>();
            if (animator == null)
            {
                animator = attacker.GetComponentInChildren<Animator>();
            }
            if (animator == null)
            {
                animator = attacker.AddComponent<Animator>();
            }
            
            // Try to assign animator controller from HeroData or default
            if (heroData != null && heroData.AnimatorData != null && heroData.AnimatorData.battleAnimator != null)
            {
                animator.runtimeAnimatorController = heroData.AnimatorData.battleAnimator;
                Debug.Log($"✅ Assigned animator controller from HeroData: {heroData.name}");
            }
            else
            {
                // Try to load default animator data
                var animatorData = AssetDatabase.LoadAssetAtPath<ClassAnimatorData>(
                    "Assets/RogueDeal/Resources/Data/Animators/NoWeapon_AnimatorData.asset"
                );
                
                if (animatorData != null && animatorData.battleAnimator != null)
                {
                    animator.runtimeAnimatorController = animatorData.battleAnimator;
                    Debug.Log($"✅ Assigned default animator controller from {animatorData.name}");
                }
                else
                {
                    Debug.LogWarning("⚠ No animator controller found. Assign one manually.");
                }
            }
            
            // Add animation test helper for manual testing
            if (attacker.GetComponent<Presentation.Tests.AnimationTestHelper>() == null)
            {
                attacker.AddComponent<Presentation.Tests.AnimationTestHelper>();
            }
            
            // Add collider if not present
            var collider = attacker.GetComponent<Collider>();
            if (collider == null)
            {
                collider = attacker.AddComponent<CapsuleCollider>();
                if (collider is CapsuleCollider capsule)
                {
                    capsule.height = 2f;
                    capsule.radius = 0.5f;
                }
            }
            
            return attacker;
        }
        
        private static GameObject CreateTarget(HeroData heroData = null)
        {
            GameObject target = null;
            
            // Try to use prefab from HeroData if available
            if (heroData != null && heroData.HeroVisualData != null && heroData.HeroVisualData.characterPrefab != null)
            {
                target = PrefabUtility.InstantiatePrefab(heroData.HeroVisualData.characterPrefab) as GameObject;
                if (target != null)
                {
                    target.name = $"Target ({heroData.PlayerName})";
                    Debug.Log($"✅ Using prefab from HeroData: {heroData.name}");
                }
            }
            
            // Fallback to primitive if no prefab
            if (target == null)
            {
                target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                target.name = "Target";
                target.transform.localScale = new Vector3(1, 1, 1);
                
                // Color for visibility
                var renderer = target.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.red;
                }
            }
            
            target.tag = "Enemy";
            
            // Remove existing colliders and add trigger collider
            var existingColliders = target.GetComponents<Collider>();
            foreach (var col in existingColliders)
            {
                Object.DestroyImmediate(col);
            }
            
            // Add combat entity (if not already present)
            var combatEntity = target.GetComponent<CombatEntity>();
            if (combatEntity == null)
            {
                combatEntity = target.AddComponent<CombatEntity>();
            }
            
            // Initialize stats from HeroData or use defaults
            if (heroData != null)
            {
                combatEntity.SetHeroData(heroData);
            }
            else
            {
                combatEntity.InitializeStatsWithoutHeroData(100f, 10f, 5f);
            }
            
            // Add trigger collider
            var collider = target.AddComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.5f;
            collider.isTrigger = true;
            
            return target;
        }
        
        private static void CreateWeaponHitbox(GameObject parent)
        {
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
    }
}

