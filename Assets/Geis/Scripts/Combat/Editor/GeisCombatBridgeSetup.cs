/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 *
 * This software and associated documentation files are proprietary and confidential.
 * Unauthorized copying, modification, distribution, or use of this software,
 * via any medium, is strictly prohibited without explicit written permission.
 *
 * This code is provided for personal use only by authorized recipients.
 * It may not be redistributed, sublicensed, or sold in any form.
 */

using UnityEngine;
using UnityEditor;
using Geis.Combat;
using Geis.Locomotion;
using RogueDeal.Combat;
using RogueDeal.Combat.Presentation;

namespace Geis.Combat.Editor
{
    public static class GeisCombatBridgeSetup
    {
        [MenuItem("Tools/Geis/Add Combat Bridge to Selected Player")]
        public static void AddCombatBridgeToSelection()
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                Debug.LogWarning("[GeisCombatBridge] Select a player GameObject first.");
                return;
            }

            var geis = go.GetComponent<GeisPlayerAnimationController>();
            if (geis == null)
            {
                geis = go.GetComponentInChildren<GeisPlayerAnimationController>();
                if (geis == null)
                {
                    Debug.LogWarning("[GeisCombatBridge] Selection must have GeisPlayerAnimationController.");
                    return;
                }
                go = geis.gameObject;
            }

            if (go.GetComponent<GeisCombatBridge>() != null)
            {
                Debug.Log("[GeisCombatBridge] GeisCombatBridge already present.");
                return;
            }

            EnsureComponent<CombatEntity>(go);
            EnsureComponent<CombatExecutor>(go);
            EnsureComponent<SimpleAttackHitDetector>(go);

            var combatEntity = go.GetComponent<CombatEntity>();
            if (combatEntity != null && combatEntity.GetEntityData() == null)
                combatEntity.InitializeStatsWithoutHeroData(100f, 10f, 5f);

            var bridge = go.AddComponent<GeisCombatBridge>();
            var so = new SerializedObject(bridge);
            so.FindProperty("_geisController").objectReferenceValue = geis;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"[GeisCombatBridge] Added combat bridge to {go.name}. Assign CombatAction and Weapon per slot in Inspector.");
        }

        private static void EnsureComponent<T>(GameObject go) where T : Component
        {
            if (go.GetComponent<T>() == null)
            {
                go.AddComponent<T>();
                Debug.Log($"[GeisCombatBridge] Added {typeof(T).Name}.");
            }
        }
    }
}
