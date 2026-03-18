using UnityEngine;
using System.Collections.Generic;

namespace FunderGames.RPG.OpenWorld
{
    /// <summary>
    /// Spawns enemies at various locations in the open world
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [System.Serializable]
        public class SpawnPoint
        {
            public Transform transform;
            public float spawnRadius = 5f;
            public bool isActive = true;
            public float respawnTime = 30f;
            public int maxEnemies = 3;
            public List<GameObject> enemyPrefabs = new List<GameObject>();
        }
        
        [Header("Spawn Settings")]
        [SerializeField] private SpawnPoint[] spawnPoints;
        [SerializeField] private float globalSpawnInterval = 10f;
        [SerializeField] private int maxTotalEnemies = 20;
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private bool continuousSpawning = true;
        
        [Header("Enemy Settings")]
        [SerializeField] private GameObject[] enemyPrefabs;
        [SerializeField] private float minSpawnDistance = 10f;
        [SerializeField] private float maxSpawnDistance = 50f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool showSpawnRanges = true;
        
        // Private variables
        private List<GameObject> activeEnemies = new List<GameObject>();
        private Dictionary<SpawnPoint, float> spawnTimers = new Dictionary<SpawnPoint, float>();
        private float globalSpawnTimer;
        private Transform player;
        
        private void Start()
        {
            InitializeSpawner();
            
            if (spawnOnStart)
            {
                SpawnInitialEnemies();
            }
        }
        
        private void Update()
        {
            if (continuousSpawning)
            {
                UpdateSpawning();
            }
            
            CleanupDeadEnemies();
        }
        
        /// <summary>
        /// Initialize the spawner
        /// </summary>
        private void InitializeSpawner()
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            
            // Initialize spawn timers
            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint != null)
                {
                    spawnTimers[spawnPoint] = 0f;
                }
            }
            
            // Validate spawn points
            ValidateSpawnPoints();
        }
        
        /// <summary>
        /// Validate spawn points and create defaults if needed
        /// </summary>
        private void ValidateSpawnPoints()
        {
            if (spawnPoints.Length == 0)
            {
                // Create default spawn points around the spawner
                spawnPoints = new SpawnPoint[4];
                for (int i = 0; i < 4; i++)
                {
                    GameObject spawnPointObj = new GameObject($"SpawnPoint_{i}");
                    spawnPointObj.transform.SetParent(transform);
                    
                    float angle = i * 90f * Mathf.Deg2Rad;
                    float distance = 20f;
                    Vector3 position = transform.position + new Vector3(
                        Mathf.Cos(angle) * distance,
                        0,
                        Mathf.Sin(angle) * distance
                    );
                    
                    spawnPointObj.transform.position = position;
                    
                    spawnPoints[i] = new SpawnPoint
                    {
                        transform = spawnPointObj.transform,
                        spawnRadius = 5f,
                        isActive = true,
                        respawnTime = 30f,
                        maxEnemies = 2,
                        enemyPrefabs = new List<GameObject>(enemyPrefabs)
                    };
                }
            }
        }
        
        /// <summary>
        /// Spawn initial enemies
        /// </summary>
        private void SpawnInitialEnemies()
        {
            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint != null && spawnPoint.isActive)
                {
                    SpawnEnemiesAtPoint(spawnPoint, 1);
                }
            }
        }
        
        /// <summary>
        /// Update spawning logic
        /// </summary>
        private void UpdateSpawning()
        {
            // Global spawn timer
            globalSpawnTimer += Time.deltaTime;
            if (globalSpawnTimer >= globalSpawnInterval)
            {
                globalSpawnTimer = 0f;
                TryGlobalSpawn();
            }
            
            // Individual spawn point timers
            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint == null || !spawnPoint.isActive) continue;
                
                spawnTimers[spawnPoint] += Time.deltaTime;
                if (spawnTimers[spawnPoint] >= spawnPoint.respawnTime)
                {
                    spawnTimers[spawnPoint] = 0f;
                    TrySpawnAtPoint(spawnPoint);
                }
            }
        }
        
        /// <summary>
        /// Try to spawn enemies globally
        /// </summary>
        private void TryGlobalSpawn()
        {
            if (activeEnemies.Count >= maxTotalEnemies) return;
            
            // Find a random spawn point
            var availablePoints = System.Array.FindAll(spawnPoints, sp => sp != null && sp.isActive);
            if (availablePoints.Length == 0) return;
            
            var randomPoint = availablePoints[Random.Range(0, availablePoints.Length)];
            SpawnEnemiesAtPoint(randomPoint, 1);
        }
        
        /// <summary>
        /// Try to spawn at a specific spawn point
        /// </summary>
        private void TrySpawnAtPoint(SpawnPoint spawnPoint)
        {
            if (spawnPoint == null || !spawnPoint.isActive) return;
            
            // Count enemies near this spawn point
            int nearbyEnemies = CountEnemiesNearPoint(spawnPoint.transform.position, spawnPoint.spawnRadius);
            
            if (nearbyEnemies < spawnPoint.maxEnemies)
            {
                int enemiesToSpawn = spawnPoint.maxEnemies - nearbyEnemies;
                SpawnEnemiesAtPoint(spawnPoint, enemiesToSpawn);
            }
        }
        
        /// <summary>
        /// Spawn enemies at a specific spawn point
        /// </summary>
        private void SpawnEnemiesAtPoint(SpawnPoint spawnPoint, int count)
        {
            if (spawnPoint == null || spawnPoint.enemyPrefabs.Count == 0) return;
            
            for (int i = 0; i < count; i++)
            {
                if (activeEnemies.Count >= maxTotalEnemies) break;
                
                // Select random enemy prefab
                GameObject enemyPrefab = spawnPoint.enemyPrefabs[Random.Range(0, spawnPoint.enemyPrefabs.Count)];
                if (enemyPrefab == null) continue;
                
                // Calculate spawn position
                Vector3 spawnPosition = GetRandomSpawnPosition(spawnPoint);
                
                // Check if position is valid
                if (IsValidSpawnPosition(spawnPosition))
                {
                    GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
                    activeEnemies.Add(enemy);
                    
                    // Set enemy name
                    enemy.name = $"{enemyPrefab.name}_{activeEnemies.Count}";
                    
                    Debug.Log($"Spawned {enemy.name} at {spawnPosition}");
                }
            }
        }
        
        /// <summary>
        /// Get random spawn position within spawn point radius
        /// </summary>
        private Vector3 GetRandomSpawnPosition(SpawnPoint spawnPoint)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnPoint.spawnRadius;
            Vector3 spawnPosition = spawnPoint.transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // Raycast down to find ground
            if (Physics.Raycast(spawnPosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
            {
                spawnPosition.y = hit.point.y;
            }
            
            return spawnPosition;
        }
        
        /// <summary>
        /// Check if spawn position is valid
        /// </summary>
        private bool IsValidSpawnPosition(Vector3 position)
        {
            // Check distance from player
            if (player != null)
            {
                float distanceFromPlayer = Vector3.Distance(position, player.position);
                if (distanceFromPlayer < minSpawnDistance || distanceFromPlayer > maxSpawnDistance)
                {
                    return false;
                }
            }
            
            // Check if position is clear
            Collider[] colliders = Physics.OverlapSphere(position, 1f);
            foreach (var collider in colliders)
            {
                if (collider.CompareTag("Player") || collider.CompareTag("Enemy"))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Count enemies near a position
        /// </summary>
        private int CountEnemiesNearPoint(Vector3 position, float radius)
        {
            int count = 0;
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null && Vector3.Distance(enemy.transform.position, position) <= radius)
                {
                    count++;
                }
            }
            return count;
        }
        
        /// <summary>
        /// Clean up dead enemies
        /// </summary>
        private void CleanupDeadEnemies()
        {
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] == null)
                {
                    activeEnemies.RemoveAt(i);
                }
            }
        }
        
        /// <summary>
        /// Force spawn enemies at a specific point
        /// </summary>
        public void ForceSpawnAtPoint(int spawnPointIndex, int count = 1)
        {
            if (spawnPointIndex >= 0 && spawnPointIndex < spawnPoints.Length)
            {
                SpawnEnemiesAtPoint(spawnPoints[spawnPointIndex], count);
            }
        }
        
        /// <summary>
        /// Clear all enemies
        /// </summary>
        public void ClearAllEnemies()
        {
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    DestroyImmediate(enemy);
                }
            }
            activeEnemies.Clear();
        }
        
        /// <summary>
        /// Get current enemy count
        /// </summary>
        public int GetEnemyCount()
        {
            return activeEnemies.Count;
        }
        
        /// <summary>
        /// Toggle spawn point
        /// </summary>
        public void ToggleSpawnPoint(int index)
        {
            if (index >= 0 && index < spawnPoints.Length)
            {
                spawnPoints[index].isActive = !spawnPoints[index].isActive;
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!showSpawnRanges) return;
            
            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint == null || spawnPoint.transform == null) continue;
                
                // Spawn point range
                Gizmos.color = spawnPoint.isActive ? Color.green : Color.red;
                Gizmos.DrawWireSphere(spawnPoint.transform.position, spawnPoint.spawnRadius);
                
                // Spawn point center
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(spawnPoint.transform.position, 0.5f);
            }
            
            // Global spawn range
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, minSpawnDistance);
            Gizmos.DrawWireSphere(transform.position, maxSpawnDistance);
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("Enemy Spawner");
            GUILayout.Label($"Active Enemies: {activeEnemies.Count}/{maxTotalEnemies}");
            GUILayout.Label($"Global Timer: {globalSpawnTimer:F1}s");
            
            if (GUILayout.Button("Force Spawn"))
            {
                TryGlobalSpawn();
            }
            
            if (GUILayout.Button("Clear All"))
            {
                ClearAllEnemies();
            }
            
            GUILayout.EndArea();
        }
    }
}
