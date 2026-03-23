// Geis of Anam - Combat Music System
// ScriptableObject mapping attack types to note configuration.

using UnityEngine;

namespace Geis.Combat.Music
{
    /// <summary>
    /// Maps attack types to note config (velocity multiplier, duration). Optional override for tuning.
    /// </summary>
    [CreateAssetMenu(fileName = "AttackNoteMapping_", menuName = "Geis/Combat/Music/Attack Note Mapping")]
    public class AttackNoteMapping : ScriptableObject
    {
        [Header("Light Attack")]
        [Range(0.5f, 1f)]
        [SerializeField] private float lightVelocity = 0.7f;
        [Range(0.1f, 2f)]
        [SerializeField] private float lightDurationMultiplier = 0.8f;

        [Header("Heavy Attack")]
        [Range(0.7f, 1f)]
        [SerializeField] private float heavyVelocity = 0.9f;
        [Range(0.5f, 2f)]
        [SerializeField] private float heavyDurationMultiplier = 1.2f;

        [Header("Charged Attack")]
        [Range(0.7f, 1f)]
        [SerializeField] private float chargedVelocity = 0.85f;
        [Range(0.5f, 2f)]
        [SerializeField] private float chargedDurationMultiplier = 1.1f;

        [Header("Finisher")]
        [Range(0.9f, 1f)]
        [SerializeField] private float finisherVelocity = 1f;
        [Range(1f, 2f)]
        [SerializeField] private float finisherDurationMultiplier = 1.5f;

        public float GetVelocity(AttackNoteType type)
        {
            return type switch
            {
                AttackNoteType.Light => lightVelocity,
                AttackNoteType.Heavy => heavyVelocity,
                AttackNoteType.Charged => chargedVelocity,
                AttackNoteType.Finisher => finisherVelocity,
                _ => 0.75f
            };
        }

        public float GetDurationMultiplier(AttackNoteType type)
        {
            return type switch
            {
                AttackNoteType.Light => lightDurationMultiplier,
                AttackNoteType.Heavy => heavyDurationMultiplier,
                AttackNoteType.Charged => chargedDurationMultiplier,
                AttackNoteType.Finisher => finisherDurationMultiplier,
                _ => 1f
            };
        }
    }
}
