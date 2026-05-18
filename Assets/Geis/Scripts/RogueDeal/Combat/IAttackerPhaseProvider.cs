/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using Geis.Combat;

namespace RogueDeal.Combat
{
    /// <summary>
    /// Optional attacker component exposing combo phase for strike resolution (dodge windows, super armor).
    /// </summary>
    public interface IAttackerPhaseProvider
    {
        bool TryGetCurrentAttackPhase(out GeisComboAttackPhase phase);
        bool HasSuperArmorDuringCurrentStartup { get; }
        bool DodgeOnlyAvoidsDuringActivePhase { get; }
    }
}
