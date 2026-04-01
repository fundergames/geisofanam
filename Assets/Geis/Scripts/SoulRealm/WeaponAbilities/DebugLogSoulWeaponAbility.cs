using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Placeholder ability for iteration: logs to the Unity Console when activated.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SoulAbility_Debug_",
        menuName = "Geis/Soul Realm/Debug Soul Weapon Ability (Log)")]
    public sealed class DebugLogSoulWeaponAbility : SoulWeaponAbilityAsset
    {
        [SerializeField] private string abilityLabel = "Ability";

        public override string AbilityDisplayName => string.IsNullOrEmpty(abilityLabel) ? name : abilityLabel;

        public override bool AllowActivationInSoulRealm => true;

        public override bool AllowActivationInPhysicalRealm => true;

        public override void Activate(in SoulWeaponAbilityContext context)
        {
            PlayDefaultActivationVfx(context);

            var defName = context.WeaponDefinition != null ? context.WeaponDefinition.displayName : "(no definition)";
            Debug.Log(
                $"[SoulWeaponAbility] {AbilityDisplayName} | slot={context.WeaponSlotIndex} weapon=\"{defName}\"",
                context.Owner);
        }
    }
}
