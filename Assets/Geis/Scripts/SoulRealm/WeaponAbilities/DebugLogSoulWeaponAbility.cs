/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 *
 * This software and associated documentation files are proprietary and confidential.
 * Unauthorized copying, modification, distribution, or use of this software,
 * via any medium, is strictly prohibited without explicit written permission.
 *
 * This code is provided for personal use only by authorized recipients.
 * It may not be redistributed, sublicensed, or sold in any form.
 */

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
