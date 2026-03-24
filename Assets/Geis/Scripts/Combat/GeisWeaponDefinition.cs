// Geis of Anam - Unified weapon definition. Single source of truth for visuals, combos, and damage.
// Replaces parallel GeisWeaponSlot + Weapon + GeisComboData + CombatAction arrays.

using UnityEngine;
using RogueDeal.Combat.Core.Data;

namespace Geis.Combat
{
    /// <summary>
    /// Unified weapon data: prefab, combo animations, and damage config.
    /// Use with GeisWeaponSwitcher (unified mode) to replace GeisWeaponSlot + separate Weapon/ComboData arrays.
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon_", menuName = "Geis/Combat/Weapon Definition")]
    public class GeisWeaponDefinition : ScriptableObject
    {
        [Header("Visual")]
        [Tooltip("Prefab to attach to hand (null = unarmed)")]
        public GameObject weaponPrefab;
        [Tooltip("Display name")]
        public string displayName;

        [Header("Combo Animations")]
        [Tooltip("Combo graph and clips for this weapon")]
        public GeisComboData comboData;

        [Header("Damage (RogueDeal)")]
        [Tooltip("Stats for damage calc: baseDamage, type multipliers")]
        public Weapon weaponStats;
        [Tooltip("Effects to apply on hit. Uses weaponStats for scaling. Null = no damage.")]
        public CombatAction combatAction;

        /// <summary>
        /// Gets the Weapon for damage calculation. Returns weaponStats, or null if unarmed.
        /// </summary>
        public Weapon GetWeaponForDamage() => weaponStats;

        /// <summary>
        /// Gets the CombatAction for effects. Returns combatAction, or null.
        /// </summary>
        public CombatAction GetCombatAction() => combatAction;
    }
}
