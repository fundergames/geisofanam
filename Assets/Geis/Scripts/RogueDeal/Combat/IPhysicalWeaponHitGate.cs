using UnityEngine;

namespace RogueDeal.Combat
{
    /// <summary>
    /// Optional gate used by hit-detection / AoE combat to decide whether an entity
    /// should be eligible for physical weapon hit application right now.
    ///
    /// Implement on targets that are only hittable during specific windows (e.g. boss parts).
    /// </summary>
    public interface IPhysicalWeaponHitGate
    {
        /// <summary>
        /// Return true if this entity is currently allowed to receive physical weapon hits.
        /// </summary>
        bool AllowsPhysicalWeaponHits();
    }
}

