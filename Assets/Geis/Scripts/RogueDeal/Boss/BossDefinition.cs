using System.Collections.Generic;
using UnityEngine;

namespace RogueDeal.Boss
{
    /// <summary>
    /// Data asset defining a boss encounter: stats, phase thresholds, attack parameters, and soul anchor config.
    /// Assign to a BossController to drive the Soul Warden (or any future boss) encounter.
    /// </summary>
    [CreateAssetMenu(fileName = "Boss_", menuName = "Funder Games/Rogue Deal/Boss/Boss Definition")]
    public class BossDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string bossId;
        public string bossName = "Soul Warden";
        public string title = "Guardian of the Veil";
        [TextArea(2, 4)]
        public string loreDescription;
        public Sprite portrait;

        [Header("Base Stats")]
        public float maxHealth = 500f;
        public float attack = 25f;
        public float defense = 10f;

        [Header("Attack Settings")]
        [Tooltip("Seconds between attacks in Phase 1 (reduced by attackSpeedMultiplier in later phases)")]
        public float baseAttackInterval = 3f;
        public float baseAttackDamage = 20f;

        [Header("AOE Attack (Phase 3)")]
        public float aoeAttackDamage = 15f;
        public float aoeAttackRadius = 4f;
        [Range(0f, 1f)]
        [Tooltip("Probability of using an AOE attack instead of a direct attack in phases where AOE is enabled")]
        public float aoeAttackChance = 0.35f;

        [Header("Soul Anchor")]
        [Tooltip("Health of each soul anchor the player must destroy in the soul realm")]
        public float anchorHealth = 50f;

        [Header("Phase Configuration")]
        [Tooltip("Ordered list of phases. phaseNumber must match the intended phase order (1, 2, 3).")]
        public List<BossPhaseData> phases = new List<BossPhaseData>
        {
            new BossPhaseData
            {
                phaseNumber = 1,
                hpThresholdPercent = 1.0f,
                requiresSoulAnchorBreak = false,
                anchorCount = 0,
                attackSpeedMultiplier = 1.0f,
                enableAOEAttacks = false,
                phaseTransitionMessage = string.Empty
            },
            new BossPhaseData
            {
                phaseNumber = 2,
                hpThresholdPercent = 0.67f,
                requiresSoulAnchorBreak = true,
                anchorCount = 1,
                attackSpeedMultiplier = 1.25f,
                enableAOEAttacks = false,
                phaseTransitionMessage = "The Soul Warden splits its essence..."
            },
            new BossPhaseData
            {
                phaseNumber = 3,
                hpThresholdPercent = 0.33f,
                requiresSoulAnchorBreak = true,
                anchorCount = 2,
                attackSpeedMultiplier = 1.6f,
                enableAOEAttacks = true,
                phaseTransitionMessage = "The Soul Warden unleashes its full power!"
            }
        };
    }

    /// <summary>
    /// Configuration for a single boss phase. Phases are entered when boss HP drops below hpThresholdPercent.
    /// </summary>
    [System.Serializable]
    public class BossPhaseData
    {
        [Tooltip("Phase identifier (1 = first phase, 2 = mid, 3 = final)")]
        public int phaseNumber;

        [Tooltip("Boss transitions into this phase when its HP drops below this fraction of max HP (1.0 = 100%)")]
        [Range(0f, 1f)]
        public float hpThresholdPercent = 1f;

        [Tooltip("If true, boss becomes immune when entering this phase until soul anchors are destroyed")]
        public bool requiresSoulAnchorBreak;

        [Tooltip("Number of soul anchors that must be destroyed to break immunity in this phase")]
        public int anchorCount = 1;

        [Tooltip("Multiplier applied to attack speed (1 = base, 1.5 = 50% faster)")]
        public float attackSpeedMultiplier = 1f;

        [Tooltip("Whether AOE attacks are available in this phase")]
        public bool enableAOEAttacks;

        [Tooltip("Narrative message shown to the player on phase transition")]
        public string phaseTransitionMessage;
    }
}
