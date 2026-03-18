using UnityEngine;
using UnityEditor;
using RogueDeal.Combat;
using RogueDeal.Combat.Presentation;

namespace RogueDeal.Combat.Editor
{
    /// <summary>
    /// Editor helper to quickly set up a test scene for presentation layer testing.
    /// </summary>
    public class PresentationLayerTestHelper
    {
        [MenuItem("Tools/Combat System/Create Presentation Test Scene")]
        public static void CreateTestScene()
        {
            // Create new scene or use current
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects
            );
            
            // Create ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            
            // Create attacker
            var attacker = CreateAttacker();
            attacker.transform.position = new Vector3(0, 0, 0);
            
            // Create target
            var target = CreateTarget();
            target.transform.position = new Vector3(0, 0, 3);
            
            // Create weapon hitbox
            CreateWeaponHitbox(attacker);
            
            // Create tester
            var tester = new GameObject("PresentationLayerTester");
            var testerComponent = tester.AddComponent<RogueDeal.Combat.Presentation.Tests.PresentationLayerTester>();
            testerComponent.attackerObject = attacker;
            testerComponent.targetObject = target;
            
            // Add lighting
            var light = new GameObject("Directional Light");
            light.AddComponent<Light>().type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            
            // Save scene
            string scenePath = "Assets/Scenes/CombatPresentationTest.unity";
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            
            Debug.Log($"✅ Created test scene at {scenePath}");
            Debug.Log("Scene includes:");
            Debug.Log("  - Attacker (with CombatEntity, CombatExecutor, CombatEventReceiver)");
            Debug.Log("  - Target (with CombatEntity, Collider)");
            Debug.Log("  - Weapon Hitbox");
            Debug.Log("  - PresentationLayerTester");
        }
        
        private static GameObject CreateAttacker()
        {
            var attacker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            attacker.name = "Attacker";
            attacker.transform.localScale = new Vector3(1, 1, 1);
            
            // Remove default collider (we'll add our own if needed)
            Object.DestroyImmediate(attacker.GetComponent<Collider>());
            
            // Add combat components
            var combatEntity = attacker.AddComponent<CombatEntity>();
            combatEntity.InitializeStatsWithoutHeroData(100f, 15f, 5f);
            
            var executor = attacker.AddComponent<CombatExecutor>();
            var eventReceiver = attacker.AddComponent<CombatEventReceiver>();
            
            // Add animator (required)
            var animator = attacker.AddComponent<Animator>();
            // Note: You'll need to assign an Animator Controller manually
            
            // Add collider for movement/hit detection
            var collider = attacker.AddComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.5f;
            
            return attacker;
        }
        
        private static GameObject CreateTarget()
        {
            var target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            target.name = "Target";
            target.transform.localScale = new Vector3(1, 1, 1);
            target.tag = "Enemy";
            
            // Remove default collider
            Object.DestroyImmediate(target.GetComponent<Collider>());
            
            // Add combat entity
            var combatEntity = target.AddComponent<CombatEntity>();
            combatEntity.InitializeStatsWithoutHeroData(100f, 10f, 5f);
            
            // Add trigger collider for hit detection
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
            
            var hitbox = weapon.AddComponent<WeaponHitbox>();
            var collider = weapon.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.3f, 0.3f, 1f);
            collider.isTrigger = true;
            collider.enabled = false; // Start disabled
            
            hitbox.targetLayers = LayerMask.GetMask("Default");
            hitbox.validTargetTags = new string[] { "Enemy" };
        }
    }
}

