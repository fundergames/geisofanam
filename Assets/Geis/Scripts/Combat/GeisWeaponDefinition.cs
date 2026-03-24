// Geis of Anam - Unified weapon definition. Single source of truth for visuals, combos, and damage.
// Single asset for prefab + GeisComboData + RogueDeal Weapon/CombatAction.

using UnityEngine;
using RogueDeal.Combat.Core.Data;

namespace Geis.Combat
{
    /// <summary>
    /// Unified weapon data: prefab, combo animations, and damage config.
    /// Assign per slot on GeisWeaponSwitcher (replaces separate Weapon/ComboData arrays on other components).
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
        [Tooltip("Combo graph and clips. Per-state CombatAction / multi-hit times live on this asset (State Combat Bindings).")]
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
