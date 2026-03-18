using UnityEngine;

namespace RogueDeal.Combat.Training
{
    public class TrainingSceneSetup : MonoBehaviour
    {
        [Header("Scene Setup")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Vector3 playerSpawnPosition = new Vector3(-3f, 0f, 0f);
        [SerializeField] private Vector3 dummySpawnPosition = new Vector3(3f, 0f, 0f);
        
        [Header("Training Components")]
        [SerializeField] private TrainingModeManager trainingManager;
        [SerializeField] private TrainingUI trainingUI;
        [SerializeField] private FrameDataAnalyzer frameDataAnalyzer;
        
        [Header("Auto Setup")]
        [SerializeField] private bool setupOnStart = true;
        [SerializeField] private bool activateTrainingMode = true;
        
        private GameObject spawnedPlayer;
        private GameObject spawnedDummy;
        
        private void Start()
        {
            if (setupOnStart)
            {
                SetupTrainingScene();
            }
        }
        
        [ContextMenu("Setup Training Scene")]
        public void SetupTrainingScene()
        {
            Debug.Log("[TrainingSceneSetup] Setting up training scene...");
            
            EnsureTrainingManager();
            EnsureFrameDataAnalyzer();
            SpawnPlayer();
            SpawnDummy();
            ConnectReferences();
            
            if (activateTrainingMode && trainingManager != null)
            {
                trainingManager.ToggleTrainingMode();
            }
            
            Debug.Log("[TrainingSceneSetup] Training scene setup complete!");
        }
        
        private void EnsureTrainingManager()
        {
            if (trainingManager == null)
            {
                trainingManager = FindObjectOfType<TrainingModeManager>();
            }
            
            if (trainingManager == null)
            {
                GameObject managerObj = new GameObject("TrainingModeManager");
                trainingManager = managerObj.AddComponent<TrainingModeManager>();
                managerObj.AddComponent<AttackVisualizer>();
                Debug.Log("[TrainingSceneSetup] Created TrainingModeManager");
            }
        }
        
        private void EnsureFrameDataAnalyzer()
        {
            if (frameDataAnalyzer == null)
            {
                frameDataAnalyzer = FindObjectOfType<FrameDataAnalyzer>();
            }
            
            if (frameDataAnalyzer == null && trainingManager != null)
            {
                frameDataAnalyzer = trainingManager.gameObject.AddComponent<FrameDataAnalyzer>();
                Debug.Log("[TrainingSceneSetup] Created FrameDataAnalyzer");
            }
        }
        
        private void SpawnPlayer()
        {
            if (playerPrefab == null)
            {
                Debug.LogWarning("[TrainingSceneSetup] No player prefab assigned");
                return;
            }
            
            if (spawnedPlayer != null)
            {
                DestroyImmediate(spawnedPlayer);
            }
            
            spawnedPlayer = Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
            spawnedPlayer.name = "Player";
            
            Debug.Log("[TrainingSceneSetup] Spawned player");
        }
        
        private void SpawnDummy()
        {
            if (spawnedDummy != null)
            {
                DestroyImmediate(spawnedDummy);
            }
            
            GameObject dummyBase = enemyPrefab != null ? enemyPrefab : new GameObject("TrainingDummy");
            spawnedDummy = enemyPrefab != null ? Instantiate(dummyBase, dummySpawnPosition, Quaternion.identity) : dummyBase;
            spawnedDummy.name = "TrainingDummy";
            spawnedDummy.transform.position = dummySpawnPosition;
            
            if (!spawnedDummy.GetComponent<TrainingDummy>())
            {
                TrainingDummy dummy = spawnedDummy.AddComponent<TrainingDummy>();
                Debug.Log("[TrainingSceneSetup] Added TrainingDummy component");
            }
            
            if (!spawnedDummy.GetComponent<CombatEntity>())
            {
                CombatEntity entity = spawnedDummy.AddComponent<CombatEntity>();
                Debug.Log("[TrainingSceneSetup] Added CombatEntity component");
            }
            
            Debug.Log("[TrainingSceneSetup] Spawned training dummy");
        }
        
        private void ConnectReferences()
        {
            if (trainingUI != null && trainingManager != null)
            {
                trainingUI.Initialize(trainingManager);
            }
            
            if (trainingManager != null && spawnedDummy != null)
            {
                TrainingDummy dummy = spawnedDummy.GetComponent<TrainingDummy>();
                if (trainingUI != null)
                {
                    trainingUI.SetDummy(dummy);
                }
            }
            
            Debug.Log("[TrainingSceneSetup] Connected references");
        }
        
        [ContextMenu("Clear Training Scene")]
        public void ClearTrainingScene()
        {
            if (spawnedPlayer != null)
            {
                DestroyImmediate(spawnedPlayer);
            }
            
            if (spawnedDummy != null)
            {
                DestroyImmediate(spawnedDummy);
            }
            
            Debug.Log("[TrainingSceneSetup] Cleared training scene");
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(playerSpawnPosition, 0.5f);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(dummySpawnPosition, 0.5f);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(playerSpawnPosition, dummySpawnPosition);
        }
    }
}
