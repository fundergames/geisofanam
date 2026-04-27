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
using Geis.SoulRealm;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using UnityEngine;

namespace RogueDeal.Boss
{
    /// <summary>
    /// Final phase: dual-realm sequence per encounter design.
    ///
    /// Intended order:
    /// - Break each fist's soul shield (pins fists)
    /// - Physical beams x2
    /// - Destroy both pinned fists (physical)
    /// - Break physical crit shield
    /// - Soul beams x3
    /// - Break soul crit shield
    /// - Hit crit spot in Physical
    /// - Hit crit spot in Soul for win
    /// </summary>
    public sealed class GiantBossPhase3DualRealmLoop : IBossPhase
    {
        private enum Step
        {
            PinFists,
            PhysicalBeams,
            BreakFists,
            BreakPhysicalShield,
            SoulBeams,
            BreakSoulCritShield,
            PhysicalCritSpotHit,
            SoulCritSpotHit
        }

        private GiantBossController _boss;
        private Step _step;
        private Coroutine _routine;
        private bool _disabled;

        private bool _rightPinned;
        private bool _leftPinned;
        private bool _rightBroken;
        private bool _leftBroken;
        private bool _physicalCritSpotHit;
        private bool _soulCritSpotHit;

        public bool IsComplete => false; // final phase: ends on boss defeat, not auto-advance

        public void OnEnter(GiantBossController boss)
        {
            _boss = boss;
            _disabled = false;

            BossPart.OnPartBroken += HandlePartBroken;
            BossPartShield.OnShieldDestroyed += HandleBossPartShieldDestroyed;
            PhysicalShieldTarget.OnBroken += HandlePhysicalShieldBroken;
            SoulShieldTarget.OnBroken += HandleSoulShieldBroken;
            CritSpot.OnCritHit += HandleCritHit;

            boss.StopSlamLoop();
            boss.CritSpot?.SetVulnerable(false);

            if (!ValidateRequiredReferences())
            {
                // Fail-soft: keep the encounter playable (slams continue) rather than hanging in WaitUntil().
                _disabled = true;
                boss.StartSlamLoop();
                Debug.LogError("[GiantBoss] Phase 3 disabled: missing required phase-3 references on GiantBossController.", boss);
                return;
            }

            HardResetToPinStep();
            _routine = boss.StartCoroutine(MainLoop());

            Debug.Log("[GiantBoss] Phase 3 entered (dual-realm crit loop).");
        }

        public void OnUpdate(GiantBossController boss) { }

        public void OnExit(GiantBossController boss)
        {
            BossPart.OnPartBroken -= HandlePartBroken;
            BossPartShield.OnShieldDestroyed -= HandleBossPartShieldDestroyed;
            PhysicalShieldTarget.OnBroken -= HandlePhysicalShieldBroken;
            SoulShieldTarget.OnBroken -= HandleSoulShieldBroken;
            CritSpot.OnCritHit -= HandleCritHit;

            if (_routine != null)
            {
                boss.StopCoroutine(_routine);
                _routine = null;
            }

            // Fail-safe cleanup
            if (!_disabled)
                SetAllPhase3TargetsInactive();
            boss.CritSpot?.SetVulnerable(false);
            _boss = null;

            Debug.Log("[GiantBoss] Phase 3 exited.");
        }

        private void HardResetToPinStep()
        {
            _step = Step.PinFists;

            _rightPinned = false;
            _leftPinned = false;
            _rightBroken = false;
            _leftBroken = false;
            _physicalCritSpotHit = false;
            _soulCritSpotHit = false;

            // Phase 3 starts with soul shields on both fists; breaking each shield pins that fist in place.
            _boss.ResetPartsForPhase(useShields: true);
            if (_boss.RightHandPart != null) _boss.RightHandPart.SetState(BossPartState.Shielded);
            if (_boss.LeftHandPart != null)  _boss.LeftHandPart.SetState(BossPartState.Shielded);

            // Everything else hidden.
            _boss.Phase3PhysicalCritShield?.ResetShield();
            _boss.Phase3PhysicalCritShield?.SetActive(false);

            _boss.Phase3SoulCritShield?.ResetShield();
            _boss.Phase3SoulCritShield?.SetActive(false);

            _boss.CritSpot?.SetVulnerable(false);
        }

        private bool ValidateRequiredReferences()
        {
            if (_boss == null)
                return false;

            // Minimal required gameplay targets for the loop.
            return _boss.Phase3SoulCritShield != null
                && _boss.Phase3PhysicalCritShield != null
                && _boss.CritSpot != null
                && _boss.RightHandPart != null
                && _boss.LeftHandPart != null;
        }

        private IEnumerator MainLoop()
        {
            while (_boss != null)
            {
                switch (_step)
                {
                    case Step.PinFists:
                        yield return new WaitUntil(() => _rightPinned && _leftPinned);
                        BeginPhysicalBeamsStep();
                        break;

                    case Step.PhysicalBeams:
                        yield return FireTrackingBeams(RealmSimulationGroup.Physical, 2);
                        BeginPhysicalFistsStep();
                        break;

                    case Step.BreakFists:
                        yield return RunTimedStep(
                            RealmSimulationGroup.Physical,
                            _boss.ResolvePhysicalPinnedFistTimerSeconds(_boss.CurrentPhaseIndex),
                            () => _rightBroken && _leftBroken);
                        if (!(_rightBroken && _leftBroken))
                            HardResetToPinStep();
                        break;

                    case Step.BreakPhysicalShield:
                        yield return new WaitUntil(() => _step != Step.BreakPhysicalShield);
                        break;

                    case Step.SoulBeams:
                        yield return FireTrackingBeams(RealmSimulationGroup.Soul, 3);
                        BeginSoulCritShieldStep();
                        break;

                    case Step.BreakSoulCritShield:
                        yield return RunTimedStep(
                            RealmSimulationGroup.Soul,
                            _boss.ResolveSoulCritShieldTimerSeconds(_boss.CurrentPhaseIndex),
                            () => _step != Step.BreakSoulCritShield);
                        if (_step == Step.BreakSoulCritShield)
                            HardResetToPinStep();
                        break;

                    case Step.PhysicalCritSpotHit:
                        yield return new WaitUntil(() => _physicalCritSpotHit || _boss == null);
                        if (_boss == null) yield break;
                        BeginSoulCritSpotHitStep();
                        break;

                    case Step.SoulCritSpotHit:
                        yield return new WaitUntil(() => _soulCritSpotHit || _boss == null);
                        if (_boss == null) yield break;
                        _boss.ForceDefeatBoss();
                        break;
                }

                yield return null;
            }
        }

        private void BeginPhysicalBeamsStep()
        {
            _step = Step.PhysicalBeams;
            SoulRealmManager.Instance?.ForceExitSoulRealm();
        }

        private void BeginPhysicalFistsStep()
        {
            _step = Step.BreakFists;

            // Fists are now damage objectives.
            if (_boss.RightHandPart != null) _boss.RightHandPart.SetState(BossPartState.Pinned);
            if (_boss.LeftHandPart != null)  _boss.LeftHandPart.SetState(BossPartState.Pinned);
        }

        private void BeginPhysicalCritShieldStep()
        {
            _step = Step.BreakPhysicalShield;

            _boss.Phase3PhysicalCritShield?.ResetShield();
            _boss.Phase3PhysicalCritShield?.SetActive(true);
        }

        private void BeginSoulBeamsStep()
        {
            _boss.Phase3PhysicalCritShield?.SetActive(false);
            _step = Step.SoulBeams;
        }

        private void BeginSoulCritShieldStep()
        {
            _step = Step.BreakSoulCritShield;

            _boss.Phase3SoulCritShield?.ResetShield();
            _boss.Phase3SoulCritShield?.SetActive(true);
        }

        private void BeginPhysicalCritSpotHitStep()
        {
            _step = Step.PhysicalCritSpotHit;
            SoulRealmManager.Instance?.ForceExitSoulRealm();
            _boss.CritSpot?.SetVulnerable(true, requiresSoulRealm: false);
        }

        private void BeginSoulCritSpotHitStep()
        {
            _step = Step.SoulCritSpotHit;
            _boss.CritSpot?.SetVulnerable(true, requiresSoulRealm: true);
        }

        private void HandlePhysicalShieldBroken(PhysicalShieldTarget shield)
        {
            if (_boss == null || shield == null)
                return;
            if (_step == Step.BreakPhysicalShield && shield == _boss.Phase3PhysicalCritShield)
                BeginSoulBeamsStep();
        }

        private void HandleBossPartShieldDestroyed(BossPartShield destroyedShield)
        {
            if (_boss == null || destroyedShield == null)
                return;
            if (_step != Step.PinFists)
                return;

            BossPart owner = destroyedShield.OwnerPart;
            if (owner == null)
                return;

            if (owner == _boss.RightHandPart)
            {
                _rightPinned = true;
                _boss.RightHandPart?.SetState(BossPartState.Pinned);
            }
            else if (owner == _boss.LeftHandPart)
            {
                _leftPinned = true;
                _boss.LeftHandPart?.SetState(BossPartState.Pinned);
            }
        }

        private void HandleSoulShieldBroken(SoulShieldTarget shield)
        {
            if (_boss == null || shield == null)
                return;

            if (_step == Step.BreakSoulCritShield && shield == _boss.Phase3SoulCritShield)
            {
                _boss.Phase3SoulCritShield?.SetActive(false);
                BeginPhysicalCritSpotHitStep();
            }
        }

        private void HandlePartBroken(BossPart part)
        {
            if (_boss == null || part == null)
                return;
            if (_step != Step.BreakFists)
                return;

            if (part == _boss.RightHandPart) _rightBroken = true;
            if (part == _boss.LeftHandPart)  _leftBroken = true;

            if (_rightBroken && _leftBroken)
                BeginPhysicalCritShieldStep();
        }

        private IEnumerator FireTrackingBeams(RealmSimulationGroup group, int count)
        {
            float interval = Mathf.Max(0.02f, _boss.ResolveTrackingBeamInterval(_boss.CurrentPhaseIndex));

            for (int i = 0; i < Mathf.Max(0, count); i++)
            {
                TryApplyBeamDamage(group);
                yield return RealmSimulation.WaitForSecondsRealm(group, interval);
            }
        }

        private void TryApplyBeamDamage(RealmSimulationGroup group)
        {
            if (_boss == null)
                return;
            if (!RealmSimulation.IsSimulating(group))
                return;

            CombatEntity player = _boss.PlayerEntity;
            if (player == null)
                return;

            CombatEntityData playerData = player.GetEntityData();
            if (playerData == null || !playerData.IsAlive)
                return;

            Vector3 origin = _boss.Phase3EyeOrigin != null ? _boss.Phase3EyeOrigin.position : _boss.transform.position;
            Vector3 target = player.GetHitPoint();
            Vector3 dir = (target - origin);
            float dist = dir.magnitude;
            if (dist <= 0.01f)
                return;
            dir /= dist;

            Vector3 end = target;
            bool hitPlayer = true;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                var hitEntity = hit.collider.GetComponentInParent<CombatEntity>();
                hitPlayer = hitEntity == player;
            }

            _boss.PlayTrackingBeamVfx(group, origin, end);

            // If it didn't reach the player, the VFX still shows where it hit, but no damage is applied.
            if (!hitPlayer)
                return;

            float dmg = Mathf.Max(0f, _boss.ResolveTrackingBeamDamage(_boss.CurrentPhaseIndex));
            if (dmg <= 0f)
                return;

            playerData.TakeDamage(dmg);
            CombatEvents.TriggerDamageApplied(new CombatEventData
            {
                source = null,
                target = player,
                damageAmount = dmg,
                wasCritical = false,
                wasImmune = false,
                hitPosition = target
            });
        }

        private void HandleCritHit(CritSpot spot, float damage)
        {
            if (_boss == null || spot == null)
                return;
            if (spot != _boss.CritSpot)
                return;

            if (_step == Step.PhysicalCritSpotHit)
            {
                _physicalCritSpotHit = true;
                _boss.CritSpot?.SetVulnerable(false);
            }
            else if (_step == Step.SoulCritSpotHit)
            {
                _soulCritSpotHit = true;
                _boss.CritSpot?.SetVulnerable(false);
            }
        }

        private IEnumerator RunTimedStep(RealmSimulationGroup group, float seconds, System.Func<bool> successCondition)
        {
            seconds = Mathf.Max(0f, seconds);
            float elapsed = 0f;

            while (_boss != null && elapsed < seconds)
            {
                if (successCondition != null && successCondition())
                    yield break;

                elapsed += RealmSimulation.DeltaTime(group);
                yield return null;
            }
        }

        private void SetAllPhase3TargetsInactive()
        {
            _boss?.Phase3SoulCritShield?.SetActive(false);
            _boss?.Phase3PhysicalCritShield?.SetActive(false);
        }
    }
}
