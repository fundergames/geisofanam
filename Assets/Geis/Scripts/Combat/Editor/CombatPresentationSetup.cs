/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

#if UNITY_EDITOR
using Geis.Enemies;
using RogueDeal.Combat;
using RogueDeal.Combat.Presentation;
using UnityEditor;
using UnityEngine;

namespace Geis.Combat.Editor
{
    public static class CombatPresentationSetup
    {
        private const string PlayerPrefabPath = "Assets/Geis/Combat/Prefabs/Player.prefab";
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemies/P_Enemy_Phase1Humanoid.prefab";

        [MenuItem("Geis/Combat/Setup Presentation On Selected")]
        public static void SetupOnSelected()
        {
            GameObject root = Selection.activeGameObject;
            if (root == null)
            {
                Debug.LogWarning("[CombatPresentationSetup] Select a GameObject with CombatEntity (player or enemy).");
                return;
            }

            WirePresentationComponents(root);
            Debug.Log($"[CombatPresentationSetup] Presentation components ready on '{root.name}'.", root);
        }

        [MenuItem("Geis/Combat/Add Presentation To Player Prefab")]
        public static void AddToPlayerPrefab()
        {
            ModifyPrefab(PlayerPrefabPath);
        }

        [MenuItem("Geis/Combat/Add Presentation To Phase1 Enemy Prefab")]
        public static void AddToPhase1EnemyPrefab()
        {
            ModifyPrefab(EnemyPrefabPath);
        }

        private static void ModifyPrefab(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                Debug.LogError($"[CombatPresentationSetup] Prefab not found at {path}");
                return;
            }

            WirePresentationComponents(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log($"[CombatPresentationSetup] Updated prefab at {path}");
        }

        private static void WirePresentationComponents(GameObject root)
        {
            if (root.GetComponent<CombatEntity>() == null && root.GetComponentInChildren<CombatEntity>() == null)
            {
                Debug.LogWarning($"[CombatPresentationSetup] No CombatEntity on '{root.name}'.");
                return;
            }

            GameObject target = root.GetComponent<CombatEntity>() != null ? root : root.GetComponentInChildren<CombatEntity>().gameObject;

            if (target.GetComponent<CombatVFXController>() == null)
                Undo.AddComponent<CombatVFXController>(target);

            CombatSFXController sfx = target.GetComponent<CombatSFXController>();
            if (sfx == null)
                sfx = Undo.AddComponent<CombatSFXController>(target);

            CombatPresentationRuntimeSetup.EnsureAbilityAudioSource(target, sfx);

            if (target.GetComponent<CombatPresentationScheduler>() == null)
                Undo.AddComponent<CombatPresentationScheduler>(target);
        }
    }
}
#endif
