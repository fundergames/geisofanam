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

using System;
using UnityEngine;

namespace RogueDeal.Combat.StatusEffects
{
    [Serializable]
    public class StatusEffect
    {
        public StatusEffectType type;
        public int stacks;
        public int damagePerStack;
        public ElementalType element;
        public float duration;
        public bool isPermanent;

        public StatusEffect(StatusEffectType type, int stacks, int damagePerStack = 0, float duration = 0f)
        {
            this.type = type;
            this.stacks = stacks;
            this.damagePerStack = damagePerStack;
            this.duration = duration;
            this.isPermanent = duration <= 0f;
        }

        public StatusEffect Clone()
        {
            return new StatusEffect(type, stacks, damagePerStack, duration)
            {
                element = this.element,
                isPermanent = this.isPermanent
            };
        }
    }
}
