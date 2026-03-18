using Funder.Core.Events;
using RogueDeal.Enemies;
using RogueDeal.Events;
using RogueDeal.Player;
using RogueDeal.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RogueDeal.Combat
{
    public class CombatSceneManager : MonoBehaviour
    {
        [Header("Spawn Points")]
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private Transform[] enemySpawnPoints;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject playerVisualPrefab;
        [SerializeField] private GameObject enemyVisualPrefab;
        
        [Header("Combat References")]
        [SerializeField] private CombatController combatController;
        
        [Header("UI References")]
        [SerializeField] private LevelProgressIndicator levelProgressIndicator;
        
        [Header("Positioning Settings")]
        [SerializeField] private float fightDistance = 3f;
        [SerializeField] private float enemySpacingBuffer = 3f;
        
        private PlayerVisual playerVisual;
        private List<EnemyVisual> enemyVisuals = new List<EnemyVisual>();
        private CombatManager combatManager;
        
        private void Awake()
        {
            Debug.Log("[CombatSceneManager] Awake - subscribing to events...");
            SubscribeToEvents();
            Debug.Log($"[CombatSceneManager] CombatManager reference: {(combatManager != null ? "SET" : "NULL")}");
        }
        
        private void Start()
        {
            Debug.Log("[CombatSceneManager] Start - ready for combat!");
            Debug.Log($"[CombatSceneManager] Start - CombatManager is {(combatManager != null ? "SET" : "NULL")}");
            Debug.Log($"[CombatSceneManager] Start - Player spawn point: {(playerSpawnPoint != null ? playerSpawnPoint.position.ToString() : "NULL")}");
            Debug.Log($"[CombatSceneManager] Start - Player prefab: {(playerVisualPrefab != null ? playerVisualPrefab.name : "NULL")}");
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void SubscribeToEvents()
        {
            Debug.Log("[CombatSceneManager] Subscribing to EventBus events...");
            EventBus<CombatStartedEvent>.Subscribe(OnCombatStarted);
            EventBus<PlayerAttackEvent>.Subscribe(OnPlayerAttack);
            EventBus<EnemyAttackEvent>.Subscribe(OnEnemyAttack);
            EventBus<EnemyDefeatedEvent>.Subscribe(OnEnemyDefeated);
            Debug.Log("[CombatSceneManager] Successfully subscribed to all events!");
        }
        
        private void UnsubscribeFromEvents()
        {
            EventBus<CombatStartedEvent>.Unsubscribe(OnCombatStarted);
            EventBus<PlayerAttackEvent>.Unsubscribe(OnPlayerAttack);
            EventBus<EnemyAttackEvent>.Unsubscribe(OnEnemyAttack);
            EventBus<EnemyDefeatedEvent>.Unsubscribe(OnEnemyDefeated);
        }
        
        public void SetCombatManager(CombatManager manager)
        {
            combatManager = manager;
            Debug.Log($"[CombatSceneManager] CombatManager set: {(manager != null ? "SUCCESS" : "NULL")}");
        }
        
        private void OnCombatStarted(CombatStartedEvent evt)
        {
            Debug.Log($"[CombatSceneManager] ⚡ OnCombatStarted received! StageId: {evt.stageId}, Enemies: {evt.enemyCount}");
            Debug.Log($"[CombatSceneManager] CombatManager: {(combatManager != null ? "OK" : "NULL")}, Player: {(combatManager?.Player != null ? combatManager.Player.characterName : "NULL")}");
            
            if (levelProgressIndicator != null && combatManager != null && combatManager.CurrentLevel != null)
            {
                Debug.Log("[CombatSceneManager] Initializing level progress indicator...");
                levelProgressIndicator.Initialize(combatManager.CurrentLevel);
            }
            
            StartCoroutine(SpawnCombatantsRoutine());
        }
        
        private IEnumerator SpawnCombatantsRoutine()
        {
            Debug.Log("[CombatSceneManager] SpawnCombatantsRoutine started");
            yield return new WaitForSeconds(0.2f);
            
            Debug.Log("[CombatSceneManager] Spawning player...");
            SpawnPlayer();
            
            yield return new WaitForSeconds(0.3f);
            
            Debug.Log("[CombatSceneManager] Spawning enemies...");
            SpawnEnemies();
            
            Debug.Log("[CombatSceneManager] All combatants spawned, notifying systems...");
            NotifyCombatantsSpawned();
        }
        
        private void SpawnPlayer()
        {
            Debug.Log("[CombatSceneManager] SpawnPlayer() called");
            
            if (combatManager == null || combatManager.Player == null)
            {
                Debug.LogError($"[CombatSceneManager] Cannot spawn player - CombatManager: {(combatManager != null ? "OK" : "NULL")}, Player: {(combatManager?.Player != null ? "OK" : "NULL")}");
                return;
            }
            
            if (playerSpawnPoint == null)
            {
                Debug.LogWarning("[CombatSceneManager] Player spawn point not set, using default position.");
                playerSpawnPoint = transform;
            }
            
            Debug.Log($"[CombatSceneManager] Player spawn position: {playerSpawnPoint.position}");
            
            GameObject characterModel = GetPlayerCharacterModel();
            
            if (characterModel != null)
            {
                Debug.Log($"[CombatSceneManager] Using character model from HeroVisualData: {characterModel.name}");
                GameObject playerObj = InstantiatePlayerWithModel(characterModel);
                NotifyPlayerSpawned();
            }
            else if (playerVisualPrefab != null)
            {
                Debug.Log($"[CombatSceneManager] Using player visual prefab: {playerVisualPrefab.name}");
                GameObject playerObj = Instantiate(playerVisualPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
                SetupPlayerVisual(playerObj);
                NotifyPlayerSpawned();
            }
            else
            {
                Debug.Log("[CombatSceneManager] No player prefab, searching scene for existing PlayerVisual...");
                playerVisual = FindFirstObjectByType<PlayerVisual>();
                
                if (playerVisual != null)
                {
                    Debug.Log("[CombatSceneManager] Found existing PlayerVisual in scene");
                    playerVisual.transform.position = playerSpawnPoint.position;
                    playerVisual.transform.rotation = playerSpawnPoint.rotation;
                    playerVisual.Initialize(combatManager.Player);
                    playerVisual.AnimateSpawn();
                    Debug.Log("[CombatSceneManager] ✅ Player spawned successfully!");
                    
                    NotifyPlayerSpawned();
                }
                else
                {
                    Debug.LogWarning("[CombatSceneManager] No PlayerVisual found in scene or prefab!");
                }
            }
        }
        
        private GameObject GetPlayerCharacterModel()
        {
            PlayerCharacter player = combatManager.Player;
            if (player == null || player.classDefinition == null)
            {
                Debug.LogWarning("[CombatSceneManager] Player or ClassDefinition is null");
                return null;
            }
            
            string className = player.classDefinition.displayName;
            if (string.IsNullOrEmpty(className))
            {
                Debug.LogWarning("[CombatSceneManager] Class display name is null or empty");
                return null;
            }
            
            string visualDataPath = $"Data/HeroVisuals/Hero_{className}_VisualData";
            HeroVisualData visualData = Resources.Load<HeroVisualData>(visualDataPath);
            
            if (visualData == null)
            {
                Debug.LogWarning($"[CombatSceneManager] Could not load HeroVisualData at path: {visualDataPath}");
                return null;
            }
            
            GameObject characterPrefab = visualData.characterPrefab;
            if (characterPrefab == null)
            {
                Debug.LogWarning($"[CombatSceneManager] Character prefab is null in HeroVisualData: {visualData.name}");
                return null;
            }
            
            Debug.Log($"[CombatSceneManager] Using character model from HeroVisualData: {visualData.name} -> {characterPrefab.name}");
            return characterPrefab;
        }
        
        private GameObject InstantiatePlayerWithModel(GameObject characterModel)
        {
            Debug.Log($"[CombatSceneManager] Instantiating player with character model: {characterModel.name}");
            
            GameObject playerContainer = new GameObject($"Player_{combatManager.Player.characterName}");
            playerContainer.transform.position = playerSpawnPoint.position;
            playerContainer.transform.rotation = Quaternion.Euler(0, 90, 0);
            SceneManager.MoveGameObjectToScene(playerContainer, gameObject.scene);
            
            GameObject modelInstance = Instantiate(characterModel, playerContainer.transform);
            modelInstance.name = "Model";
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            
            SetupPlayerVisual(playerContainer);
            
            return playerContainer;
        }
        
        private void SetupPlayerVisual(GameObject playerObj)
        {
            SceneManager.MoveGameObjectToScene(playerObj, gameObject.scene);
            playerVisual = playerObj.GetComponent<PlayerVisual>();
            
            if (playerVisual == null)
            {
                Debug.Log("[CombatSceneManager] Adding PlayerVisual component...");
                playerVisual = playerObj.AddComponent<PlayerVisual>();
            }
            
            Debug.Log("[CombatSceneManager] Initializing player visual...");
            playerVisual.Initialize(combatManager.Player);
            
            Debug.Log($"[CombatSceneManager] Player GameObject details:");
            Debug.Log($"  - Name: {playerObj.name}");
            Debug.Log($"  - Active: {playerObj.activeSelf}");
            Debug.Log($"  - Position: {playerObj.transform.position}");
            Debug.Log($"  - Rotation: {playerObj.transform.rotation.eulerAngles}");
            Debug.Log($"  - Scale: {playerObj.transform.localScale}");
            Debug.Log($"  - Layer: {playerObj.layer}");
            Debug.Log($"  - Scene: {playerObj.scene.name}");
            
            SkinnedMeshRenderer[] smrs = playerObj.GetComponentsInChildren<SkinnedMeshRenderer>();
            Debug.Log($"  - SkinnedMeshRenderers found: {smrs.Length}");
            
            playerVisual.AnimateSpawn();
            Debug.Log($"[CombatSceneManager] ✅ Player spawned successfully at {playerSpawnPoint.position} facing right!");
        }
        
        private void NotifyPlayerSpawned()
        {
            if (playerVisual == null)
            {
                Debug.LogWarning("[CombatSceneManager] NotifyPlayerSpawned called but playerVisual is null!");
                return;
            }
            
            CombatFlowStateMachine stateMachine = FindFirstObjectByType<CombatFlowStateMachine>();
            if (stateMachine != null)
            {
                Debug.Log("[CombatSceneManager] Notifying CombatFlowStateMachine of spawned player...");
                stateMachine.SetPlayerVisual(playerVisual);
            }
            else
            {
                Debug.LogWarning("[CombatSceneManager] Could not find CombatFlowStateMachine to notify!");
            }
            
            CombatIntroController introController = FindFirstObjectByType<CombatIntroController>();
            if (introController != null)
            {
                Debug.Log("[CombatSceneManager] Notifying CombatIntroController of spawned player...");
                introController.SetPlayerVisual(playerVisual);
                
                if (levelProgressIndicator != null)
                {
                    Debug.Log("[CombatSceneManager] Setting progress indicator on IntroController...");
                    introController.SetProgressIndicator(levelProgressIndicator);
                }
                
                if (combatManager != null)
                {
                    introController.SetCombatManager(combatManager);
                }
            }
            else
            {
                Debug.LogWarning("[CombatSceneManager] Could not find CombatIntroController to notify!");
            }
        }
        
        private void NotifyCombatantsSpawned()
        {
            CombatFlowStateMachine stateMachine = FindFirstObjectByType<CombatFlowStateMachine>();
            if (stateMachine != null)
            {
                Debug.Log("[CombatSceneManager] Notifying CombatFlowStateMachine that combatants are spawned...");
                stateMachine.OnCombatantsSpawned();
            }
            else
            {
                Debug.LogWarning("[CombatSceneManager] Could not find CombatFlowStateMachine!");
            }
        }
        
        private void SpawnEnemies()
        {
            Debug.Log("[CombatSceneManager] SpawnEnemies() called");
            
            if (combatManager == null)
            {
                Debug.LogError("[CombatSceneManager] Cannot spawn enemies - CombatManager is null!");
                return;
            }
            
            ClearEnemies();
            
            var enemyInstances = GetEnemyInstancesFromManager();
            
            if (enemyInstances == null || enemyInstances.Count == 0)
            {
                Debug.LogWarning("[CombatSceneManager] No enemy instances found!");
                return;
            }
            
            Debug.Log($"[CombatSceneManager] Found {enemyInstances.Count} enemies to spawn");
            
            Camera mainCamera = Camera.main;
            float cameraHalfWidth = GetCameraHalfWidth(mainCamera);
            
            for (int i = 0; i < enemyInstances.Count; i++)
            {
                EnemyInstance enemyInstance = enemyInstances[i];
                
                Debug.Log($"[CombatSceneManager] Spawning enemy {i}: {enemyInstance.definition.displayName}");
                
                Transform spawnPoint = GetEnemySpawnPoint(i);
                Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
                
                Vector3 spawnPosition = CalculateEnemyPosition(i, cameraHalfWidth, spawnPoint);
                spawnPosition += enemyInstance.definition.spawnOffset;
                
                Debug.Log($"[CombatSceneManager] Calculated spawn position {i}: {spawnPosition}");
                
                GameObject enemyObj = SpawnEnemyWithModel(enemyInstance, spawnPosition, spawnRotation, spawnPoint, i);
                
                if (enemyObj != null)
                {
                    EnemyVisual enemyVisual = enemyObj.GetComponent<EnemyVisual>();
                    if (enemyVisual != null)
                    {
                        enemyVisuals.Add(enemyVisual);
                        Debug.Log($"[CombatSceneManager] ✅ Enemy {i} spawned successfully!");
                    }
                }
                else
                {
                    Debug.LogWarning($"[CombatSceneManager] Failed to spawn enemy {enemyInstance.definition.displayName}");
                }
            }
            
            Debug.Log($"[CombatSceneManager] ✅ Total enemies spawned: {enemyVisuals.Count}");
        }
        
        private List<EnemyInstance> GetEnemyInstancesFromManager()
        {
            if (combatManager == null)
                return null;
            
            var enemiesField = typeof(CombatManager).GetField("enemies", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (enemiesField != null)
            {
                return enemiesField.GetValue(combatManager) as List<EnemyInstance>;
            }
            
            return null;
        }
        
        private Transform GetEnemySpawnPoint(int index)
        {
            if (enemySpawnPoints != null && index >= 0 && index < enemySpawnPoints.Length)
            {
                return enemySpawnPoints[index];
            }
            
            return transform;
        }
        
        private Vector3 CalculateEnemyPosition(int enemyIndex, float cameraHalfWidth, Transform spawnPoint)
        {
            Vector3 basePosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            
            if (enemyIndex == 0)
            {
                return basePosition;
            }
            
            float playerFightX = basePosition.x - fightDistance;
            float distanceBetweenEnemies = fightDistance + cameraHalfWidth + enemySpacingBuffer;
            
            float enemyX = basePosition.x + (distanceBetweenEnemies * enemyIndex);
            
            return new Vector3(enemyX, basePosition.y, basePosition.z);
        }
        
        private GameObject SpawnEnemyWithModel(EnemyInstance enemyInstance, Vector3 spawnPosition, Quaternion spawnRotation, Transform spawnPoint, int index)
        {
            GameObject enemyPrefab = GetEnemyPrefab(enemyInstance);
            
            if (enemyPrefab == null)
            {
                Debug.LogWarning($"[CombatSceneManager] No prefab found for enemy {enemyInstance.definition.displayName}");
                return null;
            }
            
            Debug.Log($"[CombatSceneManager] Enemy prefab: {enemyPrefab.name}");
            
            bool hasEnemyVisual = enemyPrefab.GetComponent<EnemyVisual>() != null;
            GameObject enemyContainer;
            
            if (hasEnemyVisual)
            {
                enemyContainer = Instantiate(enemyPrefab, spawnPosition, spawnRotation);
            }
            else
            {
                enemyContainer = new GameObject($"Enemy_{enemyInstance.definition.displayName}_{index}");
                enemyContainer.transform.position = spawnPosition;
                enemyContainer.transform.rotation = spawnRotation;
                
                GameObject modelInstance = Instantiate(enemyPrefab, enemyContainer.transform);
                modelInstance.name = "Model";
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
            }
            
            SceneManager.MoveGameObjectToScene(enemyContainer, gameObject.scene);
            
            if (spawnPoint != null && spawnPoint.parent != null)
            {
                enemyContainer.transform.SetParent(spawnPoint.parent);
                Debug.Log($"[CombatSceneManager] Parented enemy to {spawnPoint.parent.name}");
            }
            
            enemyContainer.transform.position = spawnPosition;
            enemyContainer.transform.rotation = Quaternion.Euler(0, -90, 0);
            enemyContainer.transform.localScale = Vector3.one * enemyInstance.definition.scale;
            
            Debug.Log($"[CombatSceneManager] Instantiated enemy at {spawnPosition} facing left");
            
            EnemyVisual enemyVisual = enemyContainer.GetComponent<EnemyVisual>();
            
            if (enemyVisual == null)
            {
                Debug.Log("[CombatSceneManager] Adding EnemyVisual component...");
                enemyVisual = enemyContainer.AddComponent<EnemyVisual>();
            }
            
            Debug.Log("[CombatSceneManager] Initializing enemy visual...");
            enemyVisual.Initialize(enemyInstance);
            
            Debug.Log($"[CombatSceneManager] Enemy {index} GameObject details:");
            Debug.Log($"  - Name: {enemyContainer.name}");
            Debug.Log($"  - Active: {enemyContainer.activeSelf}");
            Debug.Log($"  - Position: {enemyContainer.transform.position}");
            Debug.Log($"  - Rotation: {enemyContainer.transform.rotation.eulerAngles}");
            Debug.Log($"  - Scale: {enemyContainer.transform.localScale}");
            
            SkinnedMeshRenderer[] smrs = enemyContainer.GetComponentsInChildren<SkinnedMeshRenderer>();
            Debug.Log($"  - SkinnedMeshRenderers found: {smrs.Length}");
            
            enemyVisual.AnimateSpawn();
            
            return enemyContainer;
        }
        
        private GameObject GetEnemyPrefab(EnemyInstance enemyInstance)
        {
            if (enemyInstance.definition.modelPrefab != null)
            {
                return enemyInstance.definition.modelPrefab;
            }
            
            if (enemyVisualPrefab != null)
            {
                return enemyVisualPrefab;
            }
            
            return null;
        }
        
        private float GetCameraHalfWidth(Camera camera)
        {
            if (camera == null)
                return 5f;

            float distance = Mathf.Abs(camera.transform.position.z);
            float halfHeight = camera.orthographicSize > 0 
                ? camera.orthographicSize 
                : distance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            
            return halfHeight * camera.aspect;
        }
        
        private void OnPlayerAttack(PlayerAttackEvent evt)
        {
            StartCoroutine(PlayerAttackRoutine(evt));
        }
        
        private IEnumerator PlayerAttackRoutine(PlayerAttackEvent evt)
        {
            if (playerVisual != null)
            {
                playerVisual.TriggerAnimation("Attack_1");
                yield return new WaitForSeconds(0.3f);
            }
            
            EnemyVisual targetVisual = FindEnemyVisual(evt.target);
            if (targetVisual != null)
            {
                targetVisual.TriggerAnimation("Damage");
                targetVisual.AnimateDamage(evt.damageDealt, evt.isCrit);
            }
        }
        
        private void OnEnemyAttack(EnemyAttackEvent evt)
        {
            StartCoroutine(EnemyAttackRoutine(evt));
        }
        
        private IEnumerator EnemyAttackRoutine(EnemyAttackEvent evt)
        {
            EnemyVisual attackerVisual = FindEnemyVisual(evt.attacker);
            if (attackerVisual != null)
            {
                attackerVisual.TriggerAnimation("Attack_1");
                yield return new WaitForSeconds(0.3f);
            }
            
            if (!evt.dodged && playerVisual != null)
            {
                playerVisual.AnimateDamage(evt.damageDealt);
                playerVisual.TriggerAnimation("Damage");
            }
        }
        
        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
            EnemyVisual defeatedVisual = FindEnemyVisual(evt.enemy);
            if (defeatedVisual != null)
            {
                defeatedVisual.TriggerAnimation("Defeat");
                defeatedVisual.AnimateDefeat();
            }
        }
        
        private EnemyVisual FindEnemyVisual(EnemyInstance enemy)
        {
            if (enemy == null)
                return null;
            
            foreach (var visual in enemyVisuals)
            {
                if (visual != null && visual.EnemyInstance == enemy)
                {
                    return visual;
                }
            }
            
            return null;
        }
        
        private void ClearEnemies()
        {
            foreach (var enemyVisual in enemyVisuals)
            {
                if (enemyVisual != null)
                {
                    Destroy(enemyVisual.gameObject);
                }
            }
            
            enemyVisuals.Clear();
        }
        
        [ContextMenu("Debug: List All Spawned Characters")]
        private void DebugListAllCharacters()
        {
            Debug.Log("=== Spawned Characters Debug ===");
            
            if (playerVisual != null)
            {
                Debug.Log($"Player: {playerVisual.name}");
                Debug.Log($"  - Animator: {(playerVisual.Animator != null ? playerVisual.Animator.runtimeAnimatorController?.name : "None")}");
                Debug.Log($"  - Position: {playerVisual.transform.position}");
            }
            else
            {
                Debug.Log("Player: Not spawned");
            }
            
            Debug.Log($"\nEnemies: {enemyVisuals.Count}");
            for (int i = 0; i < enemyVisuals.Count; i++)
            {
                EnemyVisual enemy = enemyVisuals[i];
                if (enemy != null)
                {
                    Debug.Log($"Enemy {i}: {enemy.name}");
                    Debug.Log($"  - Animator: {(enemy.Animator != null ? enemy.Animator.runtimeAnimatorController?.name : "None")}");
                    Debug.Log($"  - Position: {enemy.transform.position}");
                }
            }
            
            Debug.Log("================================");
        }
        
        [ContextMenu("Debug: Test All Attack Animations")]
        private void DebugTestAttackAnimations()
        {
            if (playerVisual != null)
            {
                Debug.Log("Testing player attack animation");
                playerVisual.TriggerAnimation("Attack_1");
            }
            
            foreach (var enemy in enemyVisuals)
            {
                if (enemy != null)
                {
                    Debug.Log($"Testing attack animation for {enemy.name}");
                    enemy.TriggerAnimation("Attack_1");
                }
            }
        }
        
        [ContextMenu("Debug: Test All Damage Animations")]
        private void DebugTestDamageAnimations()
        {
            if (playerVisual != null)
            {
                Debug.Log("Testing player damage animation");
                playerVisual.AnimateDamage(25);
            }
            
            foreach (var enemy in enemyVisuals)
            {
                if (enemy != null)
                {
                    Debug.Log($"Testing damage animation for {enemy.name}");
                    enemy.AnimateDamage(Random.Range(10, 50), Random.value > 0.7f);
                }
            }
        }
    }
}
