#if UNITY_EDITOR
/*
 * Copyright (c) 2026 Funder Games
 */

using System.IO;
using UnityEditor;
using UnityEngine;

namespace Geis.Enemies.Editor
{
    public static class EnemyBehaviorPipelineMenu
    {
        private const string DefaultFolder = "Assets/Geis/Data/EnemyAIPhase1/Behaviors";

        [MenuItem("Funder Games/Geis/Enemies/Create Default Behavior Pipeline Assets")]
        public static void CreateDefaultBehaviorAssets()
        {
            Directory.CreateDirectory(DefaultFolder);
            AssetDatabase.Refresh();

            var dead = GetOrCreate<EnemyDeadBehavior>($"{DefaultFolder}/EnemyBehavior_Dead.asset");
            var stagger = GetOrCreate<EnemyStaggerBehavior>($"{DefaultFolder}/EnemyBehavior_Stagger.asset");
            var attackPhase = GetOrCreate<EnemyAttackPhaseBehavior>($"{DefaultFolder}/EnemyBehavior_AttackPhase.asset");
            var acquire = GetOrCreate<EnemyAcquireTargetBehavior>($"{DefaultFolder}/EnemyBehavior_Acquire.asset");
            var melee = GetOrCreate<EnemyMeleeAttackBehavior>($"{DefaultFolder}/EnemyBehavior_MeleeAttack.asset");
            var strafe = GetOrCreate<EnemyCombatStrafeBehavior>($"{DefaultFolder}/EnemyBehavior_Strafe.asset");
            var approach = GetOrCreate<EnemyApproachTargetBehavior>($"{DefaultFolder}/EnemyBehavior_Approach.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"Created default enemy behaviors under {DefaultFolder}. " +
                "Assign them in order on EnemyAiDefinition.behaviorPipeline (Dead → Stagger → Attack Phase → Acquire → Approach → Melee Attack → Strafe).",
                approach);
        }

        [MenuItem("Funder Games/Geis/Enemies/Assign Default Pipeline To Selected Enemy AI Definition")]
        public static void AssignPipelineToSelectedDefinition()
        {
            if (Selection.activeObject is not EnemyAiDefinition definition)
            {
                Debug.LogWarning("Select an EnemyAiDefinition asset in the Project window.");
                return;
            }

            CreateDefaultBehaviorAssets();

            definition.behaviorPipeline = new EnemyBehavior[]
            {
                AssetDatabase.LoadAssetAtPath<EnemyDeadBehavior>($"{DefaultFolder}/EnemyBehavior_Dead.asset"),
                AssetDatabase.LoadAssetAtPath<EnemyStaggerBehavior>($"{DefaultFolder}/EnemyBehavior_Stagger.asset"),
                AssetDatabase.LoadAssetAtPath<EnemyAttackPhaseBehavior>($"{DefaultFolder}/EnemyBehavior_AttackPhase.asset"),
                AssetDatabase.LoadAssetAtPath<EnemyAcquireTargetBehavior>($"{DefaultFolder}/EnemyBehavior_Acquire.asset"),
                AssetDatabase.LoadAssetAtPath<EnemyApproachTargetBehavior>($"{DefaultFolder}/EnemyBehavior_Approach.asset"),
                AssetDatabase.LoadAssetAtPath<EnemyMeleeAttackBehavior>($"{DefaultFolder}/EnemyBehavior_MeleeAttack.asset"),
                AssetDatabase.LoadAssetAtPath<EnemyCombatStrafeBehavior>($"{DefaultFolder}/EnemyBehavior_Strafe.asset")
            };

            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            Debug.Log($"Assigned default behavior pipeline to {definition.name}.", definition);
        }

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            T instance = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(instance, path);
            return instance;
        }
    }
}
#endif
