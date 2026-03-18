using UnityEngine;
using UnityEditor;
using RogueDeal.Combat;

namespace RogueDeal.Editor
{
    public static class QuickEnemyUISetup
    {
        [MenuItem("GameObject/RogueDeal/Add Health Bar to Enemy", false, 0)]
        public static void AddHealthBarToSelectedEnemy()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select an enemy GameObject first!", "OK");
                return;
            }

            EnemyVisual enemyVisual = selected.GetComponent<EnemyVisual>();
            if (enemyVisual == null)
            {
                EditorUtility.DisplayDialog("Error", "Selected GameObject must have an EnemyVisual component!", "OK");
                return;
            }

            string healthBarPath = "Assets/RogueDeal/Prefabs/UI/EnemyHealthBar.prefab";
            GameObject healthBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(healthBarPath);

            if (healthBarPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", 
                    $"Health bar prefab not found at:\n{healthBarPath}\n\n" +
                    "Please create it first using:\nRogueDeal > Setup Enemy UI Components", 
                    "OK");
                return;
            }

            GameObject healthBarInstance = (GameObject)PrefabUtility.InstantiatePrefab(healthBarPrefab, selected.transform);
            healthBarInstance.name = "EnemyHealthBar";
            
            healthBarInstance.transform.localPosition = new Vector3(0, 2, 0);
            healthBarInstance.transform.localRotation = Quaternion.identity;
            healthBarInstance.transform.localScale = Vector3.one;

            EnemyHealthBar healthBar = healthBarInstance.GetComponent<EnemyHealthBar>();
            if (healthBar != null)
            {
                healthBar.SetFollowTarget(selected.transform);
            }

            EditorUtility.SetDirty(selected);
            
            EditorUtility.DisplayDialog("Success", 
                "Health bar added to enemy!\n\n" +
                "Don't forget to:\n" +
                "1. Assign DamagePopup prefab to EnemyVisual\n" +
                "2. Set the EnemyHealthBar field in EnemyVisual", 
                "OK");
        }

        [MenuItem("GameObject/RogueDeal/Setup Enemy Visual Complete", false, 1)]
        public static void SetupCompleteEnemyVisual()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select an enemy GameObject first!", "OK");
                return;
            }

            EnemyVisual enemyVisual = selected.GetComponent<EnemyVisual>();
            if (enemyVisual == null)
            {
                EditorUtility.DisplayDialog("Error", "Selected GameObject must have an EnemyVisual component!", "OK");
                return;
            }

            string healthBarPath = "Assets/RogueDeal/Prefabs/UI/EnemyHealthBar.prefab";
            string damagePopupPath = "Assets/RogueDeal/Prefabs/UI/DamagePopup.prefab";

            GameObject healthBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(healthBarPath);
            GameObject damagePopupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(damagePopupPath);

            if (healthBarPrefab == null || damagePopupPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", 
                    "Prefabs not found!\n\n" +
                    "Please create them first using:\nRogueDeal > Setup Enemy UI Components", 
                    "OK");
                return;
            }

            Transform existingHealthBar = selected.transform.Find("EnemyHealthBar");
            if (existingHealthBar != null)
            {
                if (!EditorUtility.DisplayDialog("Warning", 
                    "Health bar already exists. Replace it?", 
                    "Yes", "No"))
                {
                    return;
                }
                GameObject.DestroyImmediate(existingHealthBar.gameObject);
            }

            GameObject healthBarInstance = (GameObject)PrefabUtility.InstantiatePrefab(healthBarPrefab, selected.transform);
            healthBarInstance.name = "EnemyHealthBar";
            healthBarInstance.transform.localPosition = new Vector3(0, 2, 0);
            healthBarInstance.transform.localRotation = Quaternion.identity;
            healthBarInstance.transform.localScale = Vector3.one;

            Transform spawnPoint = selected.transform.Find("DamagePopupSpawnPoint");
            if (spawnPoint == null)
            {
                GameObject spawnPointObj = new GameObject("DamagePopupSpawnPoint");
                spawnPointObj.transform.SetParent(selected.transform);
                spawnPointObj.transform.localPosition = new Vector3(0, 2.5f, 0);
                spawnPointObj.transform.localRotation = Quaternion.identity;
                spawnPointObj.transform.localScale = Vector3.one;
                spawnPoint = spawnPointObj.transform;
            }

            SerializedObject so = new SerializedObject(enemyVisual);
            so.FindProperty("enemyHealthBar").objectReferenceValue = healthBarInstance.GetComponent<EnemyHealthBar>();
            so.FindProperty("damagePopupPrefab").objectReferenceValue = damagePopupPrefab;
            so.FindProperty("damagePopupSpawnPoint").objectReferenceValue = spawnPoint;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(selected);
            
            EditorUtility.DisplayDialog("Success!", 
                "Enemy Visual setup complete!\n\n" +
                "✅ Health bar added\n" +
                "✅ Damage popup prefab assigned\n" +
                "✅ Spawn point created\n\n" +
                "Your enemy is ready for combat!", 
                "Awesome!");
        }

        [MenuItem("GameObject/RogueDeal/Add Health Bar to Enemy", true)]
        [MenuItem("GameObject/RogueDeal/Setup Enemy Visual Complete", true)]
        public static bool ValidateEnemySelected()
        {
            return Selection.activeGameObject != null && 
                   Selection.activeGameObject.GetComponent<EnemyVisual>() != null;
        }
    }
}
