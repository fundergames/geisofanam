/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

namespace RogueDeal.Combat
{
  public enum CombatStrikeKind
    {
        Melee = 0,
        Projectile = 1,
        Spell = 2,
        AoE = 3
    }

    public enum CombatStrikeOutcome
    {
        Hit = 0,
        Miss_Dodged = 1,
        Miss_Immune = 2,
        Miss_InvalidTarget = 3,
        Miss_OutOfRange = 4,
        Miss_NotFacing = 5
    }
}
