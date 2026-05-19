/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

namespace RogueDeal.Combat
{
    /// <summary>
    /// Optional per-entity hit reaction playback (e.g. directional player flinch).
    /// When present, <see cref="CombatEntity"/> skips the generic <c>TakeDamage</c> trigger.
    /// </summary>
    public interface ICombatHitReactionPresenter
    {
        void PresentHitReaction(CombatEventData data);
    }
}
