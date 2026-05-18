/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using UnityEngine;

namespace RogueDeal.Combat
{
    /// <summary>
    /// Player defensive flags for <see cref="CombatStrikeResolver"/>.
    /// Updated by <see cref="Geis.Locomotion.GeisPlayerAnimationController"/> during dodge and future parry.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerDefensiveCombatState : MonoBehaviour, IDefensiveCombatState
    {
        [SerializeField] private bool isInvulnerable;
        [SerializeField] private bool isDodgeInvulnerable;
        [SerializeField] private bool isParrying;

        public bool IsInvulnerable => isInvulnerable;
        public bool IsDodgeInvulnerable => isDodgeInvulnerable;
        public bool IsParrying => isParrying;

        public void SetInvulnerable(bool value) => isInvulnerable = value;
        public void SetDodgeInvulnerable(bool value) => isDodgeInvulnerable = value;
        public void SetParrying(bool value) => isParrying = value;
    }
}
