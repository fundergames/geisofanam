/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

namespace RogueDeal.Combat
{
    /// <summary>
    /// Optional defensive state on entities that can avoid strikes (dodge i-frames, parry, global invuln).
    /// Consulted by <see cref="CombatStrikeResolver"/> at apply time.
    /// </summary>
    public interface IDefensiveCombatState
    {
        bool IsInvulnerable { get; }
        bool IsDodgeInvulnerable { get; }
        bool IsParrying { get; }
    }
}
