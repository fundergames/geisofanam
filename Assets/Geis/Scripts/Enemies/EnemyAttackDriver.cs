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

using System.Collections;
using System.Collections.Generic;
using Geis.Combat;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Presentation;
using UnityEngine;

namespace Geis.Enemies
{
    [RequireComponent(typeof(CombatExecutor))]
    public class EnemyAttackDriver : MonoBehaviour
    {
        public enum AttackPhase
        {
            None = 0,
            Telegraph = 1,
            Execute = 2,
            Recover = 3
        }

        private readonly Dictionary<string, float> _nextReadyTimes = new Dictionary<string, float>();

        private EnemyCombatant _combatant;
        private EnemyCoordinationContext _coordination;
        private EnemyAnimatorDriver _animatorDriver;
        private CombatEntity _combatEntity;
        private CombatExecutor _combatExecutor;
        private Coroutine _activeRoutine;

        /// <summary>Mirrors player combo index for <see cref="GeisComboData.ResolveCombatAction"/>.</summary>
        private int _weaponComboState;

        public bool IsBusy => _activeRoutine != null;
        public AttackPhase CurrentPhase { get; private set; }
        public EnemyAttackDefinition CurrentAttack { get; private set; }

        private void Awake()
        {
            _combatant = GetComponent<EnemyCombatant>() ?? GetComponentInParent<EnemyCombatant>();
            _coordination = GetComponent<EnemyCoordinationContext>() ?? GetComponentInParent<EnemyCoordinationContext>();
            _animatorDriver = GetComponent<EnemyAnimatorDriver>() ?? GetComponentInParent<EnemyAnimatorDriver>();
            _combatEntity = GetComponent<CombatEntity>() ?? GetComponentInParent<CombatEntity>();
            _combatExecutor = GetComponent<CombatExecutor>() ?? GetComponentInParent<CombatExecutor>();
        }

        public void ResetCombatState()
        {
            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
                _activeRoutine = null;
            }

            _combatExecutor?.ClearCurrentAction();
            CurrentPhase = AttackPhase.None;
            CurrentAttack = null;
            _nextReadyTimes.Clear();
            _coordination?.ClearReservation();
            _weaponComboState = 0;
        }

        public void CancelActiveAttack()
        {
            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
                _activeRoutine = null;
            }

            _combatExecutor?.ClearCurrentAction();
            CurrentPhase = AttackPhase.None;
            CurrentAttack = null;
            _coordination?.ClearReservation();
            _weaponComboState = 0;
        }

        public bool TryStartAttack(CombatEntity target, float distanceToTarget, bool hasLineOfSight)
        {
            if (target == null || IsBusy || _combatant == null || _combatant.Definition == null)
                return false;

            EnemyAttackDefinition attack = SelectAttack(distanceToTarget, hasLineOfSight);
            if (attack == null || ResolveCombatAction(attack) == null)
                return false;

            _activeRoutine = StartCoroutine(AttackRoutine(attack, target));
            return true;
        }

        public bool HasAnyAttackInRange(float distanceToTarget, bool hasLineOfSight)
        {
            return SelectAttack(distanceToTarget, hasLineOfSight) != null;
        }

        private CombatAction ResolveCombatAction(EnemyAttackDefinition attack)
        {
            if (attack == null || _combatant?.Definition == null)
                return null;

            GeisWeaponDefinition wd = _combatant.Definition.weaponDefinition;
            GeisComboData cd = wd?.comboData;
            if (wd != null && cd != null)
            {
                CombatAction fromCombo = cd.ResolveCombatAction(_weaponComboState, wd.GetCombatAction());
                if (fromCombo != null)
                    return fromCombo;
            }

            return attack.action;
        }

        /// <summary>
        /// When <see cref="GeisWeaponDefinition.comboData"/> defines multi-hit times for <see cref="_weaponComboState"/>,
        /// those seconds-from-attack-start values drive damage (see <see cref="CombatExecutor.ExecuteActionWithScheduledEffectTimes"/>).
        /// </summary>
        private bool TryGetComboHitTimesFromWeapon(out float[] secondsFromAttackStart)
        {
            secondsFromAttackStart = null;
            GeisComboData cd = _combatant?.Definition?.weaponDefinition?.comboData;
            if (cd == null)
                return false;

            return cd.TryGetMultiHitTimesSeconds(_weaponComboState, out secondsFromAttackStart)
                   && secondsFromAttackStart != null
                   && secondsFromAttackStart.Length > 0;
        }

        private static string AttackCooldownKey(EnemyAttackDefinition attack, CombatAction resolved)
        {
            if (!string.IsNullOrEmpty(attack.attackId))
                return attack.attackId;
            if (resolved != null && !string.IsNullOrEmpty(resolved.name))
                return resolved.name;
            return "enemy_attack";
        }

        private void AdvanceWeaponComboAfterAttack(EnemyAttackDefinition attack)
        {
            if (attack == null || _combatant?.Definition == null)
                return;

            GeisWeaponDefinition wd = _combatant.Definition.weaponDefinition;
            GeisComboData cd = wd?.comboData;
            if (cd == null)
            {
                _weaponComboState = 0;
                return;
            }

            if (cd.TryGetNextState(_weaponComboState, attack.comboAdvanceInput, out int nextState))
                _weaponComboState = nextState;
            else
                _weaponComboState = 0;
        }

        /// <summary>
        /// Only when using <see cref="EnemyAiDefinition.weaponDefinition"/> — avoids overwriting Polygon <c>ComboStateBlend</c> for legacy explicit-<see cref="CombatAction"/> enemies.
        /// </summary>
        private void SyncWeaponAnimatorBeforeSwing()
        {
            EnemyAiDefinition def = _combatant?.Definition;
            if (def?.weaponDefinition == null)
                return;

            _animatorDriver?.SyncAnimatorWeaponSlotFromDefinition(def);
            _animatorDriver?.SetWeaponComboState(_weaponComboState);
        }

        private EnemyAttackDefinition SelectAttack(float distanceToTarget, bool hasLineOfSight)
        {
            EnemyAttackDefinition[] attacks = _combatant.Definition.attacks;
            if (attacks == null || attacks.Length == 0)
                return null;

            EnemyAttackDefinition best = null;
            int bestWeight = int.MinValue;
            float now = Time.time;

            for (int i = 0; i < attacks.Length; i++)
            {
                EnemyAttackDefinition attack = attacks[i];
                if (attack == null)
                    continue;

                CombatAction resolved = ResolveCombatAction(attack);
                if (resolved == null)
                    continue;

                if (distanceToTarget < attack.minRange || distanceToTarget > attack.maxRange + 0.12f)
                    continue;

                if (attack.requiresLineOfSight && !hasLineOfSight)
                    continue;

                string attackKey = AttackCooldownKey(attack, resolved);
                if (_nextReadyTimes.TryGetValue(attackKey, out float readyAt) && readyAt > now)
                    continue;

                int weight = Mathf.Max(1, attack.selectionWeight);
                if (best == null || weight > bestWeight)
                {
                    best = attack;
                    bestWeight = weight;
                }
            }

            return best;
        }

        private IEnumerator AttackRoutine(EnemyAttackDefinition attack, CombatEntity target)
        {
            CombatAction runtimeAction = ResolveCombatAction(attack);

            CurrentAttack = attack;
            CurrentPhase = AttackPhase.Telegraph;
            _coordination?.TryReserveAttackWindow(target);
            _coordination?.MarkEngagedTarget(target);

            SyncWeaponAnimatorBeforeSwing();

            if (_animatorDriver != null && !string.IsNullOrEmpty(attack.telegraphTrigger))
                _animatorDriver.TriggerAttack(attack.telegraphTrigger);

            float telegraphRemaining = attack.telegraphDuration;
            while (telegraphRemaining > 0f)
            {
                telegraphRemaining -= Time.deltaTime;
                yield return null;
            }

            CurrentPhase = AttackPhase.Execute;
            runtimeAction = ResolveCombatAction(attack);
            SyncWeaponAnimatorBeforeSwing();

            // Coroutines run after Update; CombatExecutor often completes the same frame when no animation
            // timeline runs. Without a yield, CurrentPhase never stays Execute during EnemyBrain.Update — so
            // EnemyState.Attack / IsAttacking never latch for one tick (Animator + debugging).
            yield return null;

            _animatorDriver?.TriggerAttack(attack.attackTriggerOverride);

            bool executed = false;
            bool haveComboHitSchedule = TryGetComboHitTimesFromWeapon(out float[] scheduledHits);
            if (haveComboHitSchedule
                && _combatExecutor != null
                && runtimeAction != null)
            {
                executed = _combatExecutor.ExecuteActionWithScheduledEffectTimes(runtimeAction, scheduledHits);
            }

            if (!executed && _combatExecutor != null && runtimeAction != null)
                executed = _combatExecutor.ExecuteAction(runtimeAction);

            if (!executed && _combatEntity != null && runtimeAction != null)
            {
                float liveDistance = float.PositiveInfinity;
                if (target != null)
                {
                    Vector3 p = transform.position;
                    Vector3 q = target.transform.position;
                    p.y = 0f;
                    q.y = 0f;
                    liveDistance = Vector3.Distance(p, q);
                }

                if (target != null && liveDistance <= attack.maxRange + 0.15f)
                {
                    CombatActionDamageUtility.ApplyActionToTargets(
                        _combatEntity,
                        runtimeAction,
                        new List<CombatEntity> { target });
                }
            }

            float executionTimeout = Mathf.Max(attack.executionTimeout, 0.05f);
            if (haveComboHitSchedule && scheduledHits != null && scheduledHits.Length > 0)
            {
                float lastHit = 0f;
                for (int i = 0; i < scheduledHits.Length; i++)
                    lastHit = Mathf.Max(lastHit, scheduledHits[i]);
                executionTimeout = Mathf.Max(executionTimeout, lastHit + 0.35f);
            }

            float elapsed = 0f;
            while (_combatExecutor != null && _combatExecutor.IsExecuting && elapsed < executionTimeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_combatExecutor != null && _combatExecutor.IsExecuting)
                _combatExecutor.CompleteAction();

            string attackKey = AttackCooldownKey(attack, runtimeAction);
            _nextReadyTimes[attackKey] = Time.time + attack.cooldownSeconds;

            CurrentPhase = AttackPhase.Recover;
            float recoverRemaining = attack.recoveryDuration;
            while (recoverRemaining > 0f)
            {
                recoverRemaining -= Time.deltaTime;
                yield return null;
            }

            AdvanceWeaponComboAfterAttack(attack);

            CurrentPhase = AttackPhase.None;
            CurrentAttack = null;
            _coordination?.ClearReservation();
            _activeRoutine = null;
        }
    }
}
