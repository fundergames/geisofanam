using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using RogueDeal.Combat;

namespace RogueDeal.Editor
{
    public class FixCombatSceneDamagePopup : EditorWindow
    {
        [MenuItem("RogueDeal/Fix Combat Scene Damage Popup Setup")]
        public static void FixCombatScene()
        {
            Scene combatScene = SceneManager.GetSceneByPath("Assets/RogueDeal/Scenes/Combat.unity");
            
            if (!combatScene.IsValid() || !combatScene.isLoaded)
            {
                EditorUtility.DisplayDialog("Error", 
                    "Combat scene is not loaded!\n\nPlease open the Combat scene first.", 
                    "OK");
                return;
            }

            bool madeChanges = false;

            GameObject damagePopupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/RogueDeal/Prefabs/UI/DamagePopup.prefab");

            if (damagePopupPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", 
                    "DamagePopup prefab not found!\n\nPath: Assets/RogueDeal/Prefabs/UI/DamagePopup.prefab", 
                    "OK");
                return;
            }

            DamagePopup damagePopupScript = damagePopupPrefab.GetComponent<DamagePopup>();
            if (damagePopupScript != null)
            {
                SerializedObject so = new SerializedObject(damagePopupScript);
                SerializedProperty critColorProp = so.FindProperty("criticalColor");
                
                Color currentCritColor = critColorProp.colorValue;
                if (currentCritColor != Color.yellow)
                {
                    critColorProp.colorValue = Color.yellow;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(damagePopupPrefab);
                    Debug.Log("Updated DamagePopup prefab critical color to yellow");
                    madeChanges = true;
                }
            }

            DamagePopupManager manager = GameObject.FindFirstObjectByType<DamagePopupManager>();
            if (manager != null)
            {
                SerializedObject managerSO = new SerializedObject(manager);
                SerializedProperty prefabProp = managerSO.FindProperty("damagePopupPrefab");
                
                if (prefabProp.objectReferenceValue == null)
                {
                    prefabProp.objectReferenceValue = damagePopupPrefab;
                    managerSO.ApplyModifiedProperties();
                    EditorUtility.SetDirty(manager);
                    Debug.Log("Connected DamagePopup prefab to DamagePopupManager");
                    madeChanges = true;
                }
            }

            EnemyVisual[] enemyVisuals = GameObject.FindObjectsByType<EnemyVisual>(FindObjectsSortMode.None);
            
            foreach (EnemyVisual enemyVisual in enemyVisuals)
            {
                SerializedObject enemySO = new SerializedObject(enemyVisual);
                
                SerializedProperty popupPrefabProp = enemySO.FindProperty("damagePopupPrefab");
                if (popupPrefabProp.objectReferenceValue == null)
                {
                    popupPrefabProp.objectReferenceValue = damagePopupPrefab;
                    Debug.Log($"Connected DamagePopup prefab to {enemyVisual.gameObject.name}");
                    madeChanges = true;
                }

                SerializedProperty healthBarProp = enemySO.FindProperty("enemyHealthBar");
                EnemyHealthBar healthBar = enemyVisual.GetComponentInChildren<EnemyHealthBar>();
                if (healthBar != null)
                {
                    if (healthBarProp.objectReferenceValue == null)
                    {
                        healthBarProp.objectReferenceValue = healthBar;
                        Debug.Log($"Connected EnemyHealthBar to {enemyVisual.gameObject.name}");
                        madeChanges = true;
                    }

                    Canvas healthBarCanvas = healthBar.GetComponent<Canvas>();
                    if (healthBarCanvas != null && healthBarCanvas.renderMode == RenderMode.WorldSpace)
                    {
                        RectTransform healthBarRect = healthBar.GetComponent<RectTransform>();
                        if (healthBarRect != null)
                        {
                            if (healthBarRect.localScale.x > 0.1f || healthBarRect.localScale.y > 0.1f)
                            {
                                healthBarRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                                Debug.Log($"Fixed EnemyHealthBar scale for {enemyVisual.gameObject.name}");
                                madeChanges = true;
                            }
                        }
                    }
                }

                SerializedProperty spawnPointProp = enemySO.FindProperty("damagePopupSpawnPoint");
                if (spawnPointProp.objectReferenceValue == null)
                {
                    Transform spawnPoint = enemyVisual.transform.Find("DamagePopupSpawnPoint");
                    if (spawnPoint == null)
                    {
                        GameObject spawnPointObj = new GameObject("DamagePopupSpawnPoint");
                        spawnPointObj.transform.SetParent(enemyVisual.transform);
                        spawnPointObj.transform.localPosition = new Vector3(0, 2.5f, 0);
                        spawnPointObj.transform.localRotation = Quaternion.identity;
                        spawnPointObj.transform.localScale = Vector3.one;
                        spawnPoint = spawnPointObj.transform;
                        Debug.Log($"Created DamagePopupSpawnPoint for {enemyVisual.gameObject.name}");
                        madeChanges = true;
                    }
                    spawnPointProp.objectReferenceValue = spawnPoint;
                    madeChanges = true;
                }

                enemySO.ApplyModifiedProperties();
                EditorUtility.SetDirty(enemyVisual);
            }

            if (madeChanges)
            {
                EditorSceneManager.MarkSceneDirty(combatScene);
                AssetDatabase.SaveAssets();
                
                EditorUtility.DisplayDialog("Success!", 
                    $"Combat scene damage popup setup fixed!\n\n" +
                    $"✅ DamagePopup prefab critical color → Yellow\n" +
                    $"✅ DamagePopupManager configured\n" +
                    $"✅ {enemyVisuals.Length} enemy(s) configured\n\n" +
                    "Save the scene to keep changes.", 
                    "Great!");
            }
            else
            {
                EditorUtility.DisplayDialog("Info", 
                    "Everything is already configured correctly!", 
                    "OK");
            }
        }
    }
}
