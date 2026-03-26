using UnityEngine;

namespace RogueDeal.Boss
{
    /// <summary>
    /// Data asset describing one destructible part of the giant boss (hand, soul core, etc.).
    ///
    /// Used by BossPart at runtime to initialise its HP pool, shield stats, and behaviour flags.
    /// Keeping data in a ScriptableObject means designers can tune each part independently in
    /// the Inspector and create multiple variant bosses by swapping definitions.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BossPart_",
        menuName  = "Funder Games/Rogue Deal/Boss/Boss Part Definition")]
    public class BossPartDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Machine-readable ID used for logging and save data")]
        public string partId;
        [Tooltip("Human-readable label shown in logs and optionally in UI")]
        public string displayName;

        [Header("Health")]
        [Tooltip("HP this part has per cycle. Reset to this value at the start of each slam/crit cycle.")]
        public float maxHealth = 100f;

        [Header("Soul Realm Shield (Phase 2)")]
        [Tooltip("If true, this part gains a soul-realm-only shield when Phase 2 begins. " +
                 "The shield must be destroyed by the spectral ghost before physical attacks land.")]
        public bool hasSoulShieldInPhase2;
        [Tooltip("Total HP of the soul shield")]
        public float shieldHealth = 75f;
        [Tooltip("Damage dealt to the shield per ghost light-attack")]
        public float shieldDamagePerHit = 25f;
    }
}
