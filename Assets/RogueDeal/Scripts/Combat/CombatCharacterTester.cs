using UnityEngine;
using RogueDeal.Player;
using RogueDeal.Enemies;

namespace RogueDeal.Combat
{
    public class CombatCharacterTester : MonoBehaviour
    {
        [Header("Test Hero")]
        [SerializeField] private HeroData testHeroData;
        
        [Header("Test Enemies")]
        [SerializeField] private EnemyDefinition[] testEnemies;
        
        [Header("Test Settings")]
        [SerializeField] private bool useTestHero = false;
        [SerializeField] private bool useTestEnemies = false;
        [SerializeField] private int testWorldLevel = 1;
        
        [Header("Animation Testing")]
        [SerializeField] private bool testAnimationsOnStart = false;
        [SerializeField] private float animationTestDelay = 2f;
        
        private CombatSceneManager sceneManager;
        
        private void Start()
        {
            sceneManager = FindFirstObjectByType<CombatSceneManager>();
            
            if (testAnimationsOnStart)
            {
                Invoke(nameof(TestAllAnimations), animationTestDelay);
            }
        }
        
        public HeroData GetTestHeroData()
        {
            return useTestHero ? testHeroData : null;
        }
        
        public EnemyDefinition[] GetTestEnemies()
        {
            return useTestEnemies ? testEnemies : null;
        }
        
        [ContextMenu("Test Player Attack Animation")]
        public void TestPlayerAttackAnimation()
        {
            PlayerVisual playerVisual = FindFirstObjectByType<PlayerVisual>();
            if (playerVisual != null)
            {
                Debug.Log("[CombatCharacterTester] Testing player attack animation");
                playerVisual.TriggerAnimation("Attack");
            }
            else
            {
                Debug.LogWarning("[CombatCharacterTester] No PlayerVisual found in scene");
            }
        }
        
        [ContextMenu("Test Player Damage Animation")]
        public void TestPlayerDamageAnimation()
        {
            PlayerVisual playerVisual = FindFirstObjectByType<PlayerVisual>();
            if (playerVisual != null)
            {
                Debug.Log("[CombatCharacterTester] Testing player damage animation");
                playerVisual.AnimateDamage(25);
            }
            else
            {
                Debug.LogWarning("[CombatCharacterTester] No PlayerVisual found in scene");
            }
        }
        
        [ContextMenu("Test Enemy Attack Animation")]
        public void TestEnemyAttackAnimation()
        {
            EnemyVisual[] enemyVisuals = FindObjectsByType<EnemyVisual>(FindObjectsSortMode.None);
            
            if (enemyVisuals.Length > 0)
            {
                Debug.Log($"[CombatCharacterTester] Testing attack animation for {enemyVisuals.Length} enemies");
                foreach (var enemy in enemyVisuals)
                {
                    enemy.TriggerAnimation("Attack");
                }
            }
            else
            {
                Debug.LogWarning("[CombatCharacterTester] No EnemyVisuals found in scene");
            }
        }
        
        [ContextMenu("Test Enemy Damage Animation")]
        public void TestEnemyDamageAnimation()
        {
            EnemyVisual[] enemyVisuals = FindObjectsByType<EnemyVisual>(FindObjectsSortMode.None);
            
            if (enemyVisuals.Length > 0)
            {
                Debug.Log($"[CombatCharacterTester] Testing damage animation for {enemyVisuals.Length} enemies");
                foreach (var enemy in enemyVisuals)
                {
                    enemy.AnimateDamage(Random.Range(10, 50), Random.value > 0.7f);
                }
            }
            else
            {
                Debug.LogWarning("[CombatCharacterTester] No EnemyVisuals found in scene");
            }
        }
        
        [ContextMenu("Test All Animations")]
        public void TestAllAnimations()
        {
            Debug.Log("[CombatCharacterTester] Testing all animations in sequence");
            
            Invoke(nameof(TestPlayerAttackAnimation), 0.5f);
            Invoke(nameof(TestEnemyDamageAnimation), 1.0f);
            Invoke(nameof(TestEnemyAttackAnimation), 2.5f);
            Invoke(nameof(TestPlayerDamageAnimation), 3.0f);
        }
        
        [ContextMenu("List All Character Models")]
        public void ListAllCharacterModels()
        {
            Debug.Log("=== Player Character ===");
            PlayerVisual playerVisual = FindFirstObjectByType<PlayerVisual>();
            if (playerVisual != null)
            {
                LogCharacterInfo("Player", playerVisual.gameObject, playerVisual.Animator);
            }
            
            Debug.Log("=== Enemy Characters ===");
            EnemyVisual[] enemyVisuals = FindObjectsByType<EnemyVisual>(FindObjectsSortMode.None);
            for (int i = 0; i < enemyVisuals.Length; i++)
            {
                LogCharacterInfo($"Enemy {i}", enemyVisuals[i].gameObject, enemyVisuals[i].Animator);
            }
        }
        
        private void LogCharacterInfo(string name, GameObject obj, Animator animator)
        {
            Debug.Log($"[{name}]");
            Debug.Log($"  GameObject: {obj.name}");
            Debug.Log($"  Active: {obj.activeSelf}");
            Debug.Log($"  Position: {obj.transform.position}");
            
            if (animator != null)
            {
                Debug.Log($"  Animator: {animator.runtimeAnimatorController?.name ?? "None"}");
                Debug.Log($"  Has Attack Parameter: {HasParameter(animator, "Attack")}");
                Debug.Log($"  Has Damage Parameter: {HasParameter(animator, "Damage")}");
                Debug.Log($"  Has Spawn Parameter: {HasParameter(animator, "Spawn")}");
            }
            else
            {
                Debug.Log("  Animator: None");
            }
            
            SkinnedMeshRenderer[] renderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
            Debug.Log($"  SkinnedMeshRenderers: {renderers.Length}");
            
            foreach (var renderer in renderers)
            {
                Debug.Log($"    - {renderer.name}: {renderer.sharedMesh?.name ?? "No mesh"}");
            }
        }
        
        private bool HasParameter(Animator anim, string paramName)
        {
            if (anim == null || anim.runtimeAnimatorController == null)
                return false;
            
            foreach (var param in anim.parameters)
            {
                if (param.name == paramName)
                    return true;
            }
            return false;
        }
        
        [ContextMenu("Respawn All Characters")]
        public void RespawnAllCharacters()
        {
            if (sceneManager == null)
            {
                sceneManager = FindFirstObjectByType<CombatSceneManager>();
            }
            
            if (sceneManager != null)
            {
                Debug.Log("[CombatCharacterTester] Requesting character respawn");
                
                PlayerVisual[] players = FindObjectsByType<PlayerVisual>(FindObjectsSortMode.None);
                foreach (var player in players)
                {
                    if (player != null && player.gameObject != null)
                    {
                        Destroy(player.gameObject);
                    }
                }
                
                EnemyVisual[] enemies = FindObjectsByType<EnemyVisual>(FindObjectsSortMode.None);
                foreach (var enemy in enemies)
                {
                    if (enemy != null && enemy.gameObject != null)
                    {
                        Destroy(enemy.gameObject);
                    }
                }
            }
            else
            {
                Debug.LogWarning("[CombatCharacterTester] CombatSceneManager not found");
            }
        }
    }
}
