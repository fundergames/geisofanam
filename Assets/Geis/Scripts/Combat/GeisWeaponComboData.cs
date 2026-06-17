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

// Geis of Anam - Resolves combo data per weapon slot.
// Maps EquippedWeaponIndex (0=Unarmed, 1=Knife, 2=Sword, 3=Bow) to GeisComboData.

using UnityEngine;

namespace Geis.Combat
{
    /// <summary>
    /// Legacy per-slot combo map. Player combat uses <see cref="GeisWeaponDefinition.comboData"/> on <see cref="GeisWeaponSwitcher"/> instead.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponComboData_", menuName = "Funder Games/Geis/Combat/Weapon Combo Data")]
    public class GeisWeaponComboData : ScriptableObject
    {
        [Tooltip("Combo data per weapon: [0]=Unarmed, [1]=Knife, [2]=Sword, [3]=Bow")]
        [SerializeField]
        private GeisComboData[] weaponCombos = new GeisComboData[4];

        /// <summary>
        /// Get combo data for the given weapon index (0-3). Returns null if out of range or unassigned.
        /// </summary>
        public GeisComboData GetComboForWeapon(int weaponIndex)
        {
            if (weaponCombos == null || weaponIndex < 0 || weaponIndex >= weaponCombos.Length)
                return null;
            return weaponCombos[weaponIndex];
        }
    }
}
