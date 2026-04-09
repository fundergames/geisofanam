using UnityEngine;

namespace RogueDeal.Combat
{
    /// <summary>
    /// Soul-realm boss shields that absorb bow/projectile damage without using the fist
    /// <see cref="IPhysicalWeaponHitGate"/> (which only allows hits while <c>Grounded</c>).
    /// Implemented by <c>BossPartShield</c> in game assemblies.
    /// </summary>
    public interface ISoulRealmShieldProjectileSink
    {
        /// <summary>
        /// When Soul Realm is active and the fist is shielded, applies damage to the shield pool.
        /// <paramref name="damageAmount"/> may be updated when the incoming value is &lt;= 0 (e.g. match melee shield chip).
        /// Returns true if the projectile was fully handled (do not apply effects to the fist CombatEntity).
        /// </summary>
        bool TryConsumeSoulRealmProjectileDamage(ref float damageAmount, Vector3 hitPosition);
    }
}
