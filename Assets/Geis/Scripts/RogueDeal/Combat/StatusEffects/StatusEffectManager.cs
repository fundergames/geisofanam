using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RogueDeal.Combat.StatusEffects
{
    public class StatusEffectManager
    {
        private readonly List<StatusEffect> activeEffects = new List<StatusEffect>();
        private readonly CharacterStats targetStats;

        public IReadOnlyList<StatusEffect> ActiveEffects => activeEffects;

        public StatusEffectManager(CharacterStats stats)
        {
            targetStats = stats;
        }

        public void AddEffect(StatusEffect effect)
        {
            var existing = activeEffects.FirstOrDefault(e => e.type == effect.type);
            if (existing != null)
            {
                existing.stacks += effect.stacks;
                if (!existing.isPermanent && effect.duration > existing.duration)
                {
                    existing.duration = effect.duration;
                }
            }
            else
            {
                activeEffects.Add(effect.Clone());
            }
        }

        public int ProcessEffectsOnTurnStart()
        {
            int totalDamage = 0;
            var effectsToRemove = new List<StatusEffect>();

            foreach (var effect in activeEffects)
            {
                int damage = effect.damagePerStack * effect.stacks;
                totalDamage += damage;

                effect.stacks--;
                if (effect.stacks <= 0)
                {
                    effectsToRemove.Add(effect);
                }
            }

            foreach (var effect in effectsToRemove)
            {
                activeEffects.Remove(effect);
            }

            return totalDamage;
        }

        public void UpdateDurations(float deltaTime)
        {
            var effectsToRemove = new List<StatusEffect>();

            foreach (var effect in activeEffects)
            {
                if (!effect.isPermanent)
                {
                    effect.duration -= deltaTime;
                    if (effect.duration <= 0f)
                    {
                        effectsToRemove.Add(effect);
                    }
                }
            }

            foreach (var effect in effectsToRemove)
            {
                activeEffects.Remove(effect);
            }
        }

        public bool HasEffect(StatusEffectType type)
        {
            return activeEffects.Any(e => e.type == type);
        }

        public StatusEffect GetEffect(StatusEffectType type)
        {
            return activeEffects.FirstOrDefault(e => e.type == type);
        }

        public void RemoveEffect(StatusEffectType type)
        {
            activeEffects.RemoveAll(e => e.type == type);
        }

        public void ClearAllEffects()
        {
            activeEffects.Clear();
        }
    }
}
