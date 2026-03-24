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
