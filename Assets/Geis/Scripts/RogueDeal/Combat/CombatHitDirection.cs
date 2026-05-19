/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

namespace RogueDeal.Combat
{
    /// <summary>
    /// Direction the strike came from, relative to the victim's facing (planar XZ).
    /// Animator / Synty clip indices use <see cref="CombatHitDirectionUtility.ToReactionDirection"/> (F/B/L/R = recoil direction).
    /// </summary>
    public enum CombatHitDirection
    {
        Front = 0,
        Back = 1,
        Left = 2,
        Right = 3
    }
}
