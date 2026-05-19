/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using Geis.Combat;
using Geis.Enemies;
using Geis.Locomotion;
using RogueDeal.Combat.Presentation;
using UnityEngine;

namespace RogueDeal.Combat
{
    /// <summary>
    /// When this combatant takes damage during an attack, cancels the swing and blocks further outbound hits
    /// until the attack ends. Respects <see cref="IAttackerPhaseProvider.HasSuperArmorDuringCurrentStartup"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatEntity))]
    public sealed class CombatAttackInterruptController : MonoBehaviour
    {
        private CombatEntity _combatEntity;
        private IAttackerPhaseProvider _phaseProvider;
        private CombatExecutor _executor;
        private GeisCombatBridge _combatBridge;
        private SimpleAttackHitDetector _hitDetector;
        private GeisPlayerAnimationController _playerAnimation;
        private EnemyAttackDriver _enemyAttackDriver;

        /// <summary>While true, this entity's melee/projectile strikes from an interrupted swing should not apply.</summary>
        public bool IsAttackInterrupted { get; private set; }

        private void Awake()
        {
            _combatEntity = GetComponent<CombatEntity>();
            _phaseProvider = GetComponent<IAttackerPhaseProvider>()
                ?? GetComponentInChildren<IAttackerPhaseProvider>();
            _executor = GetComponent<CombatExecutor>() ?? GetComponentInChildren<CombatExecutor>();
            _combatBridge = GetComponent<GeisCombatBridge>() ?? GetComponentInChildren<GeisCombatBridge>();
            _hitDetector = GetComponent<SimpleAttackHitDetector>() ?? GetComponentInChildren<SimpleAttackHitDetector>();
            _playerAnimation = GetComponent<GeisPlayerAnimationController>()
                ?? GetComponentInChildren<GeisPlayerAnimationController>();
            _enemyAttackDriver = GetComponent<EnemyAttackDriver>() ?? GetComponentInChildren<EnemyAttackDriver>();
        }

        private void OnEnable()
        {
            CombatEvents.OnDamageApplied += OnDamageApplied;
        }

        private void OnDisable()
        {
            CombatEvents.OnDamageApplied -= OnDamageApplied;
        }

        private void Update()
        {
            if (!IsAttackInterrupted)
                return;

            if (!IsCurrentlyAttacking())
                ClearAttackInterrupt();
        }

        private void OnDamageApplied(CombatEventData data)
        {
            if (_combatEntity == null || data.target != _combatEntity)
                return;

            if (data.wasImmune || data.damageAmount <= 0f)
                return;

            if (_phaseProvider != null && _phaseProvider.HasSuperArmorDuringCurrentStartup)
                return;

            InterruptActiveAttack();
        }

        /// <summary>Cancels animation, hit windows, and scheduled strikes for this combatant.</summary>
        public void InterruptActiveAttack()
        {
            IsAttackInterrupted = true;

            _enemyAttackDriver?.CancelActiveAttack();
            _playerAnimation?.InterruptAttackFromIncomingHit();
            _combatBridge?.CancelPendingHits();
            _executor?.InterruptCurrentAction();
            _hitDetector?.CancelPendingHitChecks();
        }

        public void ClearAttackInterrupt()
        {
            IsAttackInterrupted = false;
        }

        /// <summary>Call when a new attack begins so a prior interrupt does not block the next swing.</summary>
        public void NotifyAttackStarted()
        {
            ClearAttackInterrupt();
        }

        public static bool BlocksOutgoingDamage(CombatEntity attacker)
        {
            if (attacker == null)
                return false;

            var controller = attacker.GetComponent<CombatAttackInterruptController>()
                ?? attacker.GetComponentInChildren<CombatAttackInterruptController>();
            return controller != null && controller.IsAttackInterrupted;
        }

        private bool IsCurrentlyAttacking()
        {
            if (_enemyAttackDriver != null && _enemyAttackDriver.IsBusy)
                return true;

            if (_playerAnimation != null && _playerAnimation.IsInAttackState)
                return true;

            if (_executor != null && _executor.IsExecuting)
                return true;

            return false;
        }
    }
}
