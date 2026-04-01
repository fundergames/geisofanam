using RogueDeal.Combat.Core.Data;
using UnityEngine;

namespace RogueDeal.Combat.Presentation
{
    /// <summary>
    /// Puzzle volumes (e.g. sword-break zones) that should register hits from
    /// <see cref="SimpleAttackHitDetector"/> using the same overlap spheres as melee combat,
    /// without weapon colliders or <see cref="WeaponHitbox"/>.
    /// </summary>
    public interface IPuzzleMeleeHitSink
    {
        /// <param name="source">Null when the hit is forwarded from a soul-realm weapon ability (e.g. Emberblade wave).</param>
        void OnMeleeHitFromSimpleAttack(SimpleAttackHitDetector source, CombatAction action, int weaponSlotIndex, int hitWindowIndex);
    }
}
