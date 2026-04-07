namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Optional per-frame tick for secondary ability while the SoulRealmWeapon map is enabled (e.g. hold-to-pull in the physical realm).
    /// </summary>
    public interface ISoulWeaponSecondaryHoldTick
    {
        void TickSecondaryWhileAbilityMapEnabled(in SoulWeaponAbilityContext context, bool ability2Held);
    }
}
