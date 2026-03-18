using RogueDeal.Enemies;
using RogueDeal.Combat;
using RogueDeal.Player;
using UnityEditor;
using UnityEngine;

namespace RogueDeal.Editor
{
    public class FixEnemyModelReference : EditorWindow
    {
        [MenuItem("Funder Games/Rogue Deal/Fix Combat Data References")]
        public static void FixCombatDataReferences()
        {
            bool success = true;
            
            success &= FixEnemyModels();
            success &= VerifyAnimatorData();
            
            if (success)
            {
                Debug.Log("✅ All combat data references fixed successfully!");
            }
            else
            {
                Debug.LogWarning("⚠️ Some references could not be fixed. Check the logs above.");
            }
        }
        
        private static bool FixEnemyModels()
        {
            var goblinEnemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/RogueDeal/Resources/Data/Enemies/Enemy_Goblin.asset");
            var testEnemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RogueDeal/Combat/Prefabs/TestEnemy.prefab");
            
            if (goblinEnemy == null)
            {
                Debug.LogError("Could not find Enemy_Goblin.asset!");
                return false;
            }
            
            if (testEnemyPrefab == null)
            {
                Debug.LogError("Could not find TestEnemy.prefab!");
                return false;
            }
            
            SerializedObject serializedEnemy = new SerializedObject(goblinEnemy);
            SerializedProperty modelPrefabProp = serializedEnemy.FindProperty("modelPrefab");
            
            if (modelPrefabProp != null)
            {
                modelPrefabProp.objectReferenceValue = testEnemyPrefab;
                serializedEnemy.ApplyModifiedProperties();
                EditorUtility.SetDirty(goblinEnemy);
                AssetDatabase.SaveAssets();
                
                Debug.Log($"✅ Successfully assigned TestEnemy.prefab to {goblinEnemy.displayName}!");
                return true;
            }
            else
            {
                Debug.LogError("Could not find 'modelPrefab' property on EnemyDefinition!");
                return false;
            }
        }
        
        private static bool VerifyAnimatorData()
        {
            var warriorClass = AssetDatabase.LoadAssetAtPath<ClassDefinition>("Assets/RogueDeal/Resources/Data/Classes/Class_Warrior.asset");
            
            if (warriorClass == null)
            {
                Debug.LogWarning("Class_Warrior.asset not found. Run 'Create Example Data' first.");
                return false;
            }
            
            if (warriorClass.animatorData == null)
            {
                Debug.LogWarning("Class_Warrior has no animator data assigned. Attempting to fix...");
                
                var doubleSwordAnimData = AssetDatabase.LoadAssetAtPath<ClassAnimatorData>("Assets/RogueDeal/Resources/Data/Animators/DoubleSword_AnimatorData.asset");
                
                if (doubleSwordAnimData != null)
                {
                    SerializedObject serializedClass = new SerializedObject(warriorClass);
                    SerializedProperty animDataProp = serializedClass.FindProperty("animatorData");
                    
                    if (animDataProp != null)
                    {
                        animDataProp.objectReferenceValue = doubleSwordAnimData;
                        serializedClass.ApplyModifiedProperties();
                        EditorUtility.SetDirty(warriorClass);
                        AssetDatabase.SaveAssets();
                        
                        Debug.Log("✅ Successfully assigned DoubleSword animator data to Warrior class!");
                        return true;
                    }
                }
                else
                {
                    Debug.LogError("Could not find DoubleSword_AnimatorData.asset!");
                    return false;
                }
            }
            else
            {
                Debug.Log($"✅ Warrior class already has animator data: {warriorClass.animatorData.name}");
            }
            
            if (warriorClass.animatorData != null && warriorClass.animatorData.battleAnimator != null)
            {
                Debug.Log($"✅ Battle animator controller assigned: {warriorClass.animatorData.battleAnimator.name}");
                return true;
            }
            else
            {
                Debug.LogWarning("⚠️ Warrior class animator data exists but battleAnimator is null!");
                return false;
            }
        }
    }
}
