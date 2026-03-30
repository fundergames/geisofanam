// Geis of Anam - Unified weapon definition. Single source of truth for visuals, combos, and damage.
// Single asset for prefab + GeisComboData + RogueDeal Weapon/CombatAction.

using UnityEngine;
using RogueDeal.Combat.Core.Data;
using Geis.SoulRealm.WeaponAbilities;

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

        [Header("Soul realm")]
        [Tooltip("Spectral ability 1 (e.g. Q / D-pad Left). Only used while Soul Realm is active.")]
        [SerializeField] private SoulWeaponAbilityAsset soulAbilityPrimary;
        [Tooltip("Spectral ability 2 (e.g. F / D-pad Right). Only used while Soul Realm is active.")]
        [SerializeField] private SoulWeaponAbilityAsset soulAbilitySecondary;
        [Tooltip("If true, successful hits with this weapon build Lyre resonance (see LyreResonanceMeter on player).")]
        [SerializeField] private bool buildsLyreResonance;

        public SoulWeaponAbilityAsset PrimarySoulAbility => soulAbilityPrimary;
        public SoulWeaponAbilityAsset SecondarySoulAbility => soulAbilitySecondary;

        /// <summary>When true, Lyre resonance meter on the player adds charge on damaging hits.</summary>
        public bool BuildsLyreResonance => buildsLyreResonance;

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
