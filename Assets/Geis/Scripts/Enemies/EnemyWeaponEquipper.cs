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

using Geis.Combat;
using RogueDeal.Combat;
using UnityEngine;

namespace Geis.Enemies
{
    /// <summary>
    /// Instantiates <see cref="GeisWeaponDefinition.weaponPrefab"/> on the enemy skeleton — mirrors attachment behaviour of <see cref="GeisWeaponSwitcher"/> without player input.
    /// </summary>
    public class EnemyWeaponEquipper : MonoBehaviour
    {
        [Tooltip("Optional override when the rig uses non-standard socket names.")]
        [SerializeField] private Transform manualRightHandAttach;

        [SerializeField] private Transform manualLeftHandAttach;

        [SerializeField] private string[] rightHandSocketNames = { "Prop_R_Socket", "Prop_R", "weapon_r", "hand_r", "Hand_R", "Weapon" };

        [SerializeField] private string[] leftHandSocketNames = { "Prop_L_Socket", "Prop_L", "weapon_l", "hand_l", "Hand_L", "Weapon_L" };

        [SerializeField] private bool useHumanoidHandFallback = true;

        private GameObject _spawnedInstance;

        public void ApplyFromDefinition(EnemyAiDefinition definition, CombatEntity combatEntity)
        {
            ClearAttachedWeapon();

            GeisWeaponDefinition weaponDef = definition?.weaponDefinition;
            if (weaponDef == null || weaponDef.weaponPrefab == null)
                return;

            Animator animator = combatEntity != null ? combatEntity.animator : GetComponentInChildren<Animator>();
            if (animator == null)
                return;

            Transform parent = ResolveAttachmentParent(animator, weaponDef.AttachmentHand);
            if (parent == null)
                parent = animator.transform;

            GameObject prefab = weaponDef.weaponPrefab;
            _spawnedInstance = Instantiate(prefab, parent);
            _spawnedInstance.transform.localPosition = prefab.transform.localPosition;
            _spawnedInstance.transform.localRotation = prefab.transform.localRotation;
            _spawnedInstance.transform.localScale = prefab.transform.localScale;
            _spawnedInstance.name = prefab.name + "_EquippedEnemy";
        }

        private Transform ResolveAttachmentParent(Animator animator, WeaponAttachmentHand hand)
        {
            if (hand == WeaponAttachmentHand.LeftHand)
            {
                if (manualLeftHandAttach != null)
                    return manualLeftHandAttach;
                Transform t = FindFirstByNames(animator.transform, leftHandSocketNames);
                if (t != null)
                    return t;
                if (useHumanoidHandFallback && animator.avatar != null && animator.avatar.isHuman)
                    return animator.GetBoneTransform(HumanBodyBones.LeftHand);
            }
            else
            {
                if (manualRightHandAttach != null)
                    return manualRightHandAttach;
                Transform t = FindFirstByNames(animator.transform, rightHandSocketNames);
                if (t != null)
                    return t;
                if (useHumanoidHandFallback && animator.avatar != null && animator.avatar.isHuman)
                    return animator.GetBoneTransform(HumanBodyBones.RightHand);
            }

            return animator.transform;
        }

        private static Transform FindFirstByNames(Transform root, string[] names)
        {
            if (names == null)
                return null;
            foreach (string n in names)
            {
                Transform found = FindTransformRecursive(root, n);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static Transform FindTransformRecursive(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;
            foreach (Transform child in parent)
            {
                Transform found = FindTransformRecursive(child, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private void ClearAttachedWeapon()
        {
            if (_spawnedInstance != null)
            {
                Destroy(_spawnedInstance);
                _spawnedInstance = null;
            }
        }

        private void OnDestroy()
        {
            ClearAttachedWeapon();
        }
    }
}
