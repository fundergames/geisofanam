using System.Collections;
using Geis.SoulRealm;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using UnityEngine;

namespace RogueDeal.Boss
{
    /// <summary>
    /// Data-driven phase runner for the Giant Soul Warden encounter.
    /// Uses <see cref="GiantBossPhaseDefinition"/> fields to enforce timers and objective gates.
    /// </summary>
    public sealed class GiantBossSequencePhase : IBossPhase
    {
        private readonly int _phaseIndex1Based;
        private GiantBossController _boss;
        private GiantBossPhaseDefinition _data;

        private Coroutine _routine;
        private bool _disabled;

        // Stun tracking
        private bool _rightStunned;
        private bool _leftStunned;

        // Completion gates
        private bool _rightBroken;
        private bool _leftBroken;
        private bool _physicalCritShieldBroken;
        private bool _soulCritShieldBroken;
        private bool _physicalCritSpotHit;
        private bool _soulCritSpotHit;

        private float _stunElapsed;
        private float _completionElapsed;

        public GiantBossSequencePhase(int phaseIndex1Based)
        {
            _phaseIndex1Based = phaseIndex1Based;
        }

        public bool IsComplete { get; private set; }

        public void OnEnter(GiantBossController boss)
        {
            _boss = boss;
            _data = boss != null ? boss.Definition.GetPhaseData(_phaseIndex1Based) : null;

            IsComplete = false;
            _disabled = false;

            BossPart.OnPartBroken += HandlePartBroken;
            BossPartShield.OnShieldDestroyed += HandleBossPartShieldDestroyed;
            PhysicalShieldTarget.OnBroken += HandlePhysicalShieldBroken;
            SoulShieldTarget.OnBroken += HandleSoulShieldBroken;
            CritSpot.OnCritHit += HandleCritHit;

            if (!ValidateRequiredReferences())
            {
                _disabled = true;
                boss?.StartSlamLoop();
                Debug.LogError($"[GiantBoss] Phase {_phaseIndex1Based} disabled: missing required references.", boss);
                return;
            }

            ResetPhaseState();
            ApplyPhaseStartSetup();
            _routine = boss.StartCoroutine(RunPhase());

            Debug.Log($"[GiantBoss] Phase {_phaseIndex1Based} entered (data-driven sequence).");
        }

        public void OnUpdate(GiantBossController boss)
        {
            // If the phase is configured to advance by soul-drain threshold, preserve that behavior.
            // This allows phases to progress via the existing CritSpot -> DrainSouls pipeline without needing
            // additional bespoke gate objects.
            if (_boss == null || _data == null)
                return;

            // If the phase has explicit objective gates configured, do not auto-advance via soul threshold.
            // In that case, completion is driven by the structured objective chain in RunPhase().
            if (HasExplicitObjectiveChain(_data))
                return;

            int phaseCount = _boss.Definition != null ? _boss.Definition.PhaseCount : 1;
            bool isFinalPhase = _phaseIndex1Based >= phaseCount;
            if (isFinalPhase)
                return;

            float threshold = _data.exitSoulPercentThreshold;
            if (threshold > 0f && _boss.SoulPercent <= threshold)
                IsComplete = true;
        }

        private static bool HasExplicitObjectiveChain(GiantBossPhaseDefinition data)
        {
            if (data == null)
                return false;

            return data.stunGateSeconds > 0f
                   || data.completionGateSeconds > 0f
                   || data.stunByBreakingSoulShield
                   || data.requireBothFistsDestroyed
                   || data.requirePhysicalCritShield
                   || data.requireSoulCritShield
                   || data.requirePhysicalCritSpotHit
                   || data.requireSoulCritSpotHit
                   || data.physicalBeamsCount > 0
                   || data.soulBeamsCount > 0;
        }

        public void OnExit(GiantBossController boss)
        {
            BossPart.OnPartBroken -= HandlePartBroken;
            BossPartShield.OnShieldDestroyed -= HandleBossPartShieldDestroyed;
            PhysicalShieldTarget.OnBroken -= HandlePhysicalShieldBroken;
            SoulShieldTarget.OnBroken -= HandleSoulShieldBroken;
            CritSpot.OnCritHit -= HandleCritHit;

            if (_routine != null && boss != null)
            {
                boss.StopCoroutine(_routine);
                _routine = null;
            }

            if (!_disabled)
                SetAllObjectiveTargetsInactive();

            boss?.CritSpot?.SetVulnerable(false);
            _boss = null;
            _data = null;
        }

        private bool ValidateRequiredReferences()
        {
            if (_boss == null || _data == null)
                return false;
            if (_boss.RightHandPart == null || _boss.LeftHandPart == null)
                return false;

            // Only require optional targets if the phase data says so.
            if (_data.requirePhysicalCritShield && _boss.Phase3PhysicalCritShield == null)
                return false;
            if (_data.requireSoulCritShield && _boss.Phase3SoulCritShield == null)
                return false;
            if ((_data.requirePhysicalCritSpotHit || _data.requireSoulCritSpotHit || _data.critSpotVulnerableWindow > 0f) && _boss.CritSpot == null)
                return false;

            return true;
        }

        private void ResetPhaseState()
        {
            _rightStunned = false;
            _leftStunned = false;
            _rightBroken = false;
            _leftBroken = false;
            _physicalCritShieldBroken = false;
            _soulCritShieldBroken = false;
            _physicalCritSpotHit = false;
            _soulCritSpotHit = false;

            _stunElapsed = 0f;
            _completionElapsed = 0f;
        }

        private void ApplyPhaseStartSetup()
        {
            // Reset fists for this phase; shield spawning is still driven by useShieldedHands.
            _boss.ResetPartsForPhase(_data.useShieldedHands);

            // Hide optional objective targets by default.
            SetAllObjectiveTargetsInactive();

            _boss.CritSpot?.SetVulnerable(false);

            // Keep existing slam loop; this provides the "Fist 1 slam, Fist 2 slam" rhythm for all phases.
            _boss.StartSlamLoop();
        }

        private IEnumerator RunPhase()
        {
            while (_boss != null && !IsComplete)
            {
                // ── Stun gate (optional) ────────────────────────────────────────
                if (_data.stunGateSeconds > 0f)
                {
                    _stunElapsed = 0f;
                    while (_boss != null && _stunElapsed < _data.stunGateSeconds)
                    {
                        if (_rightStunned && _leftStunned)
                            break;

                        _stunElapsed += RealmSimulation.DeltaTime(RealmSimulationGroup.Universal);
                        yield return null;
                    }

                    if (!(_rightStunned && _leftStunned))
                    {
                        RestartPhaseFromBeginning();
                        yield return null;
                        continue;
                    }
                }
                else
                {
                    // If no stun timer, still wait for both fists to be stunned (prevents skipping).
                    yield return new WaitUntil(() => _rightStunned && _leftStunned || _boss == null);
                    if (_boss == null) yield break;
                }

                // ── Completion gate (optional timer) ───────────────────────────
                _completionElapsed = 0f;

                // Break both fists (physical HP) if required.
                if (_data.requireBothFistsDestroyed)
                {
                    // Ensure fists stay in place and are hittable.
                    _boss.RightHandPart?.SetState(BossPartState.Pinned);
                    _boss.LeftHandPart?.SetState(BossPartState.Pinned);

                    float pinnedElapsed = 0f;
                    float pinnedLimit = _boss.ResolvePhysicalPinnedFistTimerSeconds(_phaseIndex1Based);

                    while (_boss != null && !(_rightBroken && _leftBroken))
                    {
                        if (pinnedLimit > 0f)
                        {
                            pinnedElapsed += RealmSimulation.DeltaTime(RealmSimulationGroup.Physical);
                            if (pinnedElapsed >= pinnedLimit)
                            {
                                RestartPhaseFromBeginning();
                                goto ContinueOuter;
                            }
                        }

                        if (CompletionTimerExpired())
                        {
                            RestartPhaseFromBeginning();
                            goto ContinueOuter;
                        }

                        yield return null;
                    }
                }

                // Beams (physical) — after fists are broken (per encounter design).
                if (_data.physicalBeamsCount > 0)
                {
                    SoulRealmManager.Instance?.ForceExitSoulRealm();
                    yield return FireTrackingBeams(RealmSimulationGroup.Physical, _data.physicalBeamsCount);
                }

                // Physical crit shield gate
                if (_data.requirePhysicalCritShield)
                {
                    SoulRealmManager.Instance?.ForceExitSoulRealm();
                    _boss.Phase3PhysicalCritShield?.ResetShield();
                    _boss.Phase3PhysicalCritShield?.SetActive(true);

                    while (_boss != null && !_physicalCritShieldBroken)
                    {
                        if (CompletionTimerExpired())
                        {
                            RestartPhaseFromBeginning();
                            goto ContinueOuter;
                        }

                        yield return null;
                    }
                }

                // Beams (soul)
                if (_data.soulBeamsCount > 0)
                    yield return FireTrackingBeams(RealmSimulationGroup.Soul, _data.soulBeamsCount);

                // Soul crit shield gate
                if (_data.requireSoulCritShield)
                {
                    _boss.Phase3SoulCritShield?.ResetShield();
                    _boss.Phase3SoulCritShield?.SetActive(true);

                    float soulShieldElapsed = 0f;
                    float soulShieldLimit = _boss.ResolveSoulCritShieldTimerSeconds(_phaseIndex1Based);

                    while (_boss != null && !_soulCritShieldBroken)
                    {
                        if (soulShieldLimit > 0f)
                        {
                            soulShieldElapsed += RealmSimulation.DeltaTime(RealmSimulationGroup.Soul);
                            if (soulShieldElapsed >= soulShieldLimit)
                            {
                                RestartPhaseFromBeginning();
                                goto ContinueOuter;
                            }
                        }

                        if (CompletionTimerExpired())
                        {
                            RestartPhaseFromBeginning();
                            goto ContinueOuter;
                        }

                        yield return null;
                    }
                }

                // Crit spot (physical hit gate)
                if (_data.requirePhysicalCritSpotHit)
                {
                    SoulRealmManager.Instance?.ForceExitSoulRealm();
                    _boss.CritSpot?.SetVulnerable(true, requiresSoulRealm: false);

                    while (_boss != null && !_physicalCritSpotHit)
                    {
                        if (CompletionTimerExpired())
                        {
                            RestartPhaseFromBeginning();
                            goto ContinueOuter;
                        }

                        yield return null;
                    }
                }

                // Crit spot (soul hit gate)
                if (_data.requireSoulCritSpotHit)
                {
                    _boss.CritSpot?.SetVulnerable(true, requiresSoulRealm: true);

                    while (_boss != null && !_soulCritSpotHit)
                    {
                        if (CompletionTimerExpired())
                        {
                            RestartPhaseFromBeginning();
                            goto ContinueOuter;
                        }

                        yield return null;
                    }
                }

                // ── Phase completion (objective chain) ──────────────────────────
                // If this is not the final phase, completing the chain can advance the phase.
                // Final phase completion is handled by boss defeat (souls to 0), not phase-advance.
                int phaseCount = _boss.Definition != null ? _boss.Definition.PhaseCount : 1;
                bool isFinalPhase = _phaseIndex1Based >= phaseCount;
                if (!isFinalPhase)
                    IsComplete = true;

                ContinueOuter:
                yield return null;
            }
        }

        private bool CompletionTimerExpired()
        {
            if (_data.completionGateSeconds <= 0f)
                return false;

            _completionElapsed += RealmSimulation.DeltaTime(RealmSimulationGroup.Universal);
            return _completionElapsed >= _data.completionGateSeconds;
        }

        private void RestartPhaseFromBeginning()
        {
            ResetPhaseState();
            ApplyPhaseStartSetup();
        }

        private void SetAllObjectiveTargetsInactive()
        {
            _boss?.Phase3PhysicalCritShield?.SetActive(false);
            _boss?.Phase3SoulCritShield?.SetActive(false);
        }

        private void HandleBossPartShieldDestroyed(BossPartShield destroyedShield)
        {
            if (_boss == null || destroyedShield == null)
                return;

            if (!_data.stunByBreakingSoulShield)
                return;

            var owner = destroyedShield.OwnerPart;
            if (owner == null)
                return;

            if (owner == _boss.RightHandPart)
            {
                _rightStunned = true;
                _boss.RightHandPart?.SetState(BossPartState.Pinned);
            }
            else if (owner == _boss.LeftHandPart)
            {
                _leftStunned = true;
                _boss.LeftHandPart?.SetState(BossPartState.Pinned);
            }
        }

        private void HandlePartBroken(BossPart part)
        {
            if (_boss == null || part == null)
                return;

            if (!_data.stunByBreakingSoulShield)
            {
                // In physical-stun phases, breaking the fist is the stun.
                if (part == _boss.RightHandPart) _rightStunned = true;
                if (part == _boss.LeftHandPart)  _leftStunned = true;
            }
            else
            {
                // Fail-soft: if soul shields are missing / not used, phase 2/3 can otherwise
                // wait forever for "stun" even though fists can be broken normally.
                if (part == _boss.RightHandPart) _rightStunned = true;
                if (part == _boss.LeftHandPart)  _leftStunned = true;
            }

            if (part == _boss.RightHandPart) _rightBroken = true;
            if (part == _boss.LeftHandPart)  _leftBroken = true;
        }

        private void HandlePhysicalShieldBroken(PhysicalShieldTarget shield)
        {
            if (_boss == null || shield == null)
                return;
            if (shield == _boss.Phase3PhysicalCritShield)
                _physicalCritShieldBroken = true;
        }

        private void HandleSoulShieldBroken(SoulShieldTarget shield)
        {
            if (_boss == null || shield == null)
                return;
            if (shield == _boss.Phase3SoulCritShield)
                _soulCritShieldBroken = true;
        }

        private void HandleCritHit(CritSpot spot, float damage)
        {
            if (_boss == null || spot == null)
                return;
            if (spot != _boss.CritSpot)
                return;

            // One-shot objective gates for structured phases
            if (_data.requirePhysicalCritSpotHit || _data.requireSoulCritSpotHit)
            {
                bool inSoulRealm = SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive;
                if (inSoulRealm)
                    _soulCritSpotHit = true;
                else
                    _physicalCritSpotHit = true;

                _boss.CritSpot?.SetVulnerable(false);
                return;
            }

            // Default: existing soul-drain pipeline (repeat hits until threshold or window ends)
            _boss.DrainSouls(damage);
        }

        private IEnumerator FireTrackingBeams(RealmSimulationGroup group, int count)
        {
            int n = Mathf.Max(0, count);
            float interval = Mathf.Max(0.02f, _boss.ResolveTrackingBeamInterval(_phaseIndex1Based));

            for (int i = 0; i < n; i++)
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

            if (!hitPlayer)
                return;

            float dmg = Mathf.Max(0f, _boss.ResolveTrackingBeamDamage(_phaseIndex1Based));
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
    }
}

