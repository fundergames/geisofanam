/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using System;
using Geis.Combat;
using Geis.Enemies;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Core.Effects;
using UnityEngine;

namespace RogueDeal.Combat
{
    /// <summary>
    /// Central strike-time validation and effect application for melee, projectiles, spells, and AoE.
    /// </summary>
    public static class CombatStrikeResolver
    {
        public static CombatStrikeKind ResolveStrikeKind(CombatAction action, CombatStrikeKind fallback = CombatStrikeKind.Melee)
        {
            if (action == null)
                return fallback;

            if (action.strikeKindExplicit)
                return action.strikeKind;

            if (action.isProjectile)
                return CombatStrikeKind.Projectile;
            if (action.spawnsPersistentAOE)
                return CombatStrikeKind.AoE;

            return fallback;
        }

        public static bool ShouldRespectPhysicalWeaponGate(CombatAction action, CombatStrikeKind kind)
        {
            if (action != null && action.strikeKindExplicit)
                return action.respectPhysicalWeaponGate;

            CombatStrikeKind resolved = action != null ? ResolveStrikeKind(action, kind) : kind;
            return resolved switch
            {
                CombatStrikeKind.Melee => true,
                CombatStrikeKind.Projectile => true,
                CombatStrikeKind.Spell => false,
                CombatStrikeKind.AoE => false,
                _ => true
            };
        }

        public static bool CanBeDodged(CombatAction action)
        {
            return action == null || action.canBeDodged;
        }

        public static CombatStrikeOutcome TryResolveStrike(
            CombatStrikeKind kind,
            CombatEntity attacker,
            CombatEntity target,
            CombatAction action = null,
            float maxRange = -1f)
        {
            if (attacker == null || target == null || attacker == target)
                return CombatStrikeOutcome.Miss_InvalidTarget;

            CombatEntityData targetData = target.GetEntityData();
            if (targetData == null || !targetData.IsAlive)
                return CombatStrikeOutcome.Miss_InvalidTarget;

            IDefensiveCombatState defensive = target.GetComponentInParent<IDefensiveCombatState>();
            if (defensive != null)
            {
                if (defensive.IsInvulnerable)
                {
                    NotifyStrikeMissed(kind, attacker, target, CombatStrikeOutcome.Miss_Immune, action);
                    return CombatStrikeOutcome.Miss_Immune;
                }

                if (CanBeDodged(action) && defensive.IsDodgeInvulnerable && DodgeInvulnAppliesAgainstAttacker(attacker))
                {
                    NotifyStrikeMissed(kind, attacker, target, CombatStrikeOutcome.Miss_Dodged, action);
                    return CombatStrikeOutcome.Miss_Dodged;
                }

                if (defensive.IsParrying)
                {
                    NotifyStrikeMissed(kind, attacker, target, CombatStrikeOutcome.Miss_Immune, action);
                    return CombatStrikeOutcome.Miss_Immune;
                }
            }

            if (kind == CombatStrikeKind.Melee
                && !EnemyMeleeFacingGate.AllowsHitAtStrikeTime(attacker, target))
            {
                NotifyStrikeMissed(kind, attacker, target, CombatStrikeOutcome.Miss_NotFacing, action);
                return CombatStrikeOutcome.Miss_NotFacing;
            }

            if (ShouldRespectPhysicalWeaponGate(action, kind))
            {
                IPhysicalWeaponHitGate physicalGate = target.GetComponentInParent<IPhysicalWeaponHitGate>();
                if (physicalGate != null && !physicalGate.AllowsPhysicalWeaponHits())
                {
                    CombatEvents.TriggerDamageApplied(new CombatEventData
                    {
                        source = attacker,
                        target = target,
                        damageAmount = 0f,
                        wasCritical = false,
                        wasImmune = true,
                        hitPosition = target.GetHitPoint(),
                        strikeOutcome = CombatStrikeOutcome.Miss_Immune,
                        strikeKind = kind
                    });
                    NotifyStrikeMissed(kind, attacker, target, CombatStrikeOutcome.Miss_Immune, action);
                    return CombatStrikeOutcome.Miss_Immune;
                }
            }

            if (maxRange > 0f)
            {
                Vector3 a = attacker.transform.position;
                Vector3 b = target.transform.position;
                a.y = 0f;
                b.y = 0f;
                if (Vector3.Distance(a, b) > maxRange)
                {
                    NotifyStrikeMissed(kind, attacker, target, CombatStrikeOutcome.Miss_OutOfRange, action);
                    return CombatStrikeOutcome.Miss_OutOfRange;
                }
            }

            return CombatStrikeOutcome.Hit;
        }

        /// <summary>
        /// Resolves strike rules then applies effects. Returns damage dealt (0 if missed or no damage).
        /// </summary>
        public static float TryApplyEffectsToTarget(
            CombatStrikeKind kind,
            CombatEntity source,
            CombatEntity target,
            BaseEffect[] effects,
            CombatAction action = null,
            float maxRange = -1f,
            float damageMultiplier = 1f,
            Action<CombatEntity, float, bool> triggerHitReaction = null)
        {
            if (source == null || target == null || effects == null || effects.Length == 0)
                return 0f;

            if (CombatAttackInterruptController.BlocksOutgoingDamage(source))
                return 0f;

            CombatStrikeOutcome outcome = TryResolveStrike(kind, source, target, action, maxRange);
            if (outcome != CombatStrikeOutcome.Hit)
                return 0f;

            CombatEntityData sourceData = source.GetEntityData();
            CombatEntityData targetData = target.GetEntityData();
            if (sourceData == null || targetData == null)
                return 0f;

            float hpBefore = targetData.currentHealth;
            bool wasCritical = false;
            Weapon weapon = sourceData.equippedWeapon;

            for (int i = 0; i < effects.Length; i++)
            {
                BaseEffect effect = effects[i];
                if (effect == null)
                    continue;

                CalculatedEffect calculated = effect.Calculate(sourceData, targetData, weapon);
                if (calculated != null && calculated.effectType == EffectType.Damage && damageMultiplier != 1f)
                    calculated.damageAmount *= damageMultiplier;
                if (calculated.wasCritical)
                    wasCritical = true;
                effect.Apply(targetData, calculated);
            }

            float damageDealt = hpBefore - targetData.currentHealth;
            if (damageDealt <= 0f)
                return 0f;

            Vector3 hitPosition = target.GetHitPoint();
            CombatHitDirection hitDirection = CombatHitDirectionUtility.Resolve(source, target);
            CombatEvents.TriggerDamageApplied(new CombatEventData
            {
                source = source,
                target = target,
                damageAmount = damageDealt,
                wasCritical = wasCritical,
                wasImmune = false,
                hitPosition = hitPosition,
                hitDirection = hitDirection,
                strikeOutcome = CombatStrikeOutcome.Hit,
                strikeKind = kind
            });

            if (triggerHitReaction != null)
                triggerHitReaction.Invoke(target, damageDealt, wasCritical);
            else
            {
                CombatEvents.TriggerHitReactionStarted(new CombatEventData
                {
                    source = source,
                    target = target,
                    damageAmount = damageDealt,
                    wasCritical = wasCritical,
                    hitPosition = hitPosition,
                    hitDirection = hitDirection,
                    strikeKind = kind
                });
            }
            return damageDealt;
        }

        public static bool TryApplyActionToTarget(
            CombatStrikeKind kind,
            CombatEntity source,
            CombatEntity target,
            CombatAction action,
            BaseEffect[] effectsOverride = null,
            float maxRange = -1f,
            float damageMultiplier = 1f,
            Action<CombatEntity, float, bool> triggerHitReaction = null)
        {
            if (action == null)
                return false;

            BaseEffect[] effects = effectsOverride;
            if (effects == null || effects.Length == 0)
                effects = action.effects;

            if (effects == null || effects.Length == 0)
                return false;

            CombatStrikeKind resolvedKind = action != null ? ResolveStrikeKind(action, kind) : kind;
            return TryApplyEffectsToTarget(
                       resolvedKind,
                       source,
                       target,
                       effects,
                       action,
                       maxRange,
                       damageMultiplier,
                       triggerHitReaction) > 0f;
        }

        public static float GetMaxMeleeRange(CombatEntityData attackerData, float defaultRange = 2f)
        {
            if (attackerData == null)
                return defaultRange;

            if (attackerData.equippedWeapon != null && attackerData.equippedWeapon.maxRange > 0f)
                return attackerData.equippedWeapon.maxRange;

            if (attackerData.combatProfile != null && attackerData.combatProfile.engagementDistance > 0f)
                return attackerData.combatProfile.engagementDistance;

            return defaultRange;
        }

        private static bool DodgeInvulnAppliesAgainstAttacker(CombatEntity attacker)
        {
            if (attacker == null)
                return true;

            if (attacker.TryGetComponent(out IAttackerPhaseProvider phaseProvider)
                && phaseProvider.DodgeOnlyAvoidsDuringActivePhase
                && phaseProvider.TryGetCurrentAttackPhase(out GeisComboAttackPhase phase))
            {
                return phase == GeisComboAttackPhase.Active;
            }

            return true;
        }

        private static void NotifyStrikeMissed(
            CombatStrikeKind kind,
            CombatEntity attacker,
            CombatEntity target,
            CombatStrikeOutcome outcome,
            CombatAction action)
        {
            CombatEvents.TriggerStrikeMissed(new CombatEventData
            {
                source = attacker,
                target = target,
                hitPosition = target != null ? target.GetHitPoint() : Vector3.zero,
                strikeOutcome = outcome,
                strikeKind = kind,
                ability = null
            });
        }
    }
}
