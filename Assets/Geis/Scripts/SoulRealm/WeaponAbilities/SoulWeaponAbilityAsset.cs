using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Per-weapon soul ability hook. Subclass for concrete behavior (puzzle pulses, echoes, etc.).
    /// Data-driven: assign instances on each <see cref="GeisWeaponDefinition"/>.
    /// </summary>
    public abstract class SoulWeaponAbilityAsset : ScriptableObject
    {
        /// <summary>Short label for UI / logs (optional).</summary>
        public virtual string AbilityDisplayName => name;

        public abstract void Activate(in SoulWeaponAbilityContext context);
    }
}
