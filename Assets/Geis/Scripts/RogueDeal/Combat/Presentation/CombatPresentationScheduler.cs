/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using System.Collections;
using Geis.Combat;
using RogueDeal.Combat.Core.Data;
using UnityEngine;

namespace RogueDeal.Combat.Presentation
{
    /// <summary>
    /// Schedules attack presentation cues (SFX/VFX/feel) from combo data or CombatAction bindings.
    /// </summary>
    public class CombatPresentationScheduler : MonoBehaviour
    {
        [SerializeField] private bool debugLog;

        private CombatVFXController _vfxController;
        private CombatSFXController _sfxController;
        private CombatEntity _combatEntity;
        private Animator _attackerAnimator;
        private Coroutine _activeRoutine;
        private int _scheduleGeneration;
        private int _activeAttackToken;

        private void Awake()
        {
            _vfxController = GetComponent<CombatVFXController>();
            _sfxController = GetComponent<CombatSFXController>();
            _combatEntity = GetComponent<CombatEntity>();
            _attackerAnimator = GetComponentInChildren<Animator>();
            CombatHitStopService.FindOrCreateOn(gameObject);
        }

        public void Cancel()
        {
            CombatPresentationFeelPlayer.CancelAttackFeel(_activeAttackToken, gameObject);
            _scheduleGeneration++;
            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
                _activeRoutine = null;
            }
        }

        /// <summary>
        /// Schedules presentation + impact feel for the current combo step or action fallback.
        /// </summary>
        public void ScheduleFromCombo(GeisComboData comboData, int comboState, CombatAction action)
        {
            CombatPresentationResolve.TryResolve(comboData, comboState, action, out CombatPresentationCue[] presentationCues);

            CombatImpactFeelCue[] impactCues = null;
            if (comboData != null)
                comboData.TryGetImpactFeelCues(comboState, out impactCues);

            bool hasPresentation = presentationCues != null && presentationCues.Length > 0;
            bool hasImpact = impactCues != null && impactCues.Length > 0;

            if (!hasPresentation && !hasImpact)
            {
                if (debugLog)
                    Debug.Log($"[CombatPresentationScheduler] No cues for comboState={comboState} action={action?.name ?? "null"}", this);
                return;
            }

            if (debugLog)
            {
                Debug.Log(
                    $"[CombatPresentationScheduler] Scheduling presentation={presentationCues?.Length ?? 0} " +
                    $"impact={impactCues?.Length ?? 0} comboState={comboState} on {name}",
                    this);
            }

            Cancel();
            _scheduleGeneration++;
            _activeAttackToken = _scheduleGeneration;
            int generation = _scheduleGeneration;
            _activeRoutine = StartCoroutine(ScheduleCoroutine(presentationCues, impactCues, generation, _activeAttackToken));
        }

        private IEnumerator ScheduleCoroutine(
            CombatPresentationCue[] presentationCues,
            CombatImpactFeelCue[] impactCues,
            int generation,
            int attackToken)
        {
            int presentationIndex = 0;
            int impactIndex = 0;
            float elapsed = 0f;

            int presentationCount = presentationCues?.Length ?? 0;
            int impactCount = impactCues?.Length ?? 0;
            const float timeEpsilon = 0.0001f;

            while (presentationIndex < presentationCount || impactIndex < impactCount)
            {
                if (generation != _scheduleGeneration)
                    yield break;

                float nextPresentation = presentationIndex < presentationCount
                    ? presentationCues[presentationIndex].timeSeconds
                    : float.PositiveInfinity;
                float nextImpact = impactIndex < impactCount
                    ? impactCues[impactIndex].timeSeconds
                    : float.PositiveInfinity;
                float nextTime = Mathf.Min(nextPresentation, nextImpact);

                float wait = Mathf.Max(0f, nextTime - elapsed);
                if (wait > 0f)
                    yield return new WaitForSecondsRealtime(wait);
                elapsed = Mathf.Max(elapsed, nextTime);

                if (generation != _scheduleGeneration)
                    yield break;

                if (impactIndex < impactCount && nextTime <= nextImpact + timeEpsilon)
                {
                    CombatImpactFeelCue impact = impactCues[impactIndex];
                    if (debugLog)
                        Debug.Log($"[CombatPresentationScheduler] Impact feel at {nextTime:0.###}s on {name}", this);

                    CombatPresentationFeelPlayer.ApplyImpactFeel(
                        impact.cameraShake,
                        impact.hitStop,
                        attackToken,
                        _attackerAnimator,
                        gameObject);
                    impactIndex++;
                }

                if (presentationIndex < presentationCount && nextTime <= nextPresentation + timeEpsilon)
                {
                    CombatPresentationCue cue = presentationCues[presentationIndex];
                    Vector3 spawnPos = ResolveVfxSpawnPosition();

                    if (debugLog)
                    {
                        Debug.Log(
                            $"[CombatPresentationScheduler] Play '{cue.eventName}' at {nextTime:0.###}s " +
                            $"sfx={(cue.sfx != null ? cue.sfx.name : "null")} on {name}",
                            this);
                    }

                    if (_sfxController == null)
                        _sfxController = GetComponent<CombatSFXController>();

                    CombatEffectBindingPlayer.PlayCue(
                        cue,
                        _vfxController,
                        _sfxController,
                        spawnPos,
                        transform);

                    CombatPresentationFeelPlayer.ApplyCueFeel(cue, attackToken, _attackerAnimator, gameObject);
                    presentationIndex++;
                }
            }

            if (generation == _scheduleGeneration)
                _activeRoutine = null;
        }

        // MVP: entity spawn point / root. Future: weapon socket, blade forward, parented trails (see combat.md VFX placement).
        private Vector3 ResolveVfxSpawnPosition()
        {
            if (_combatEntity != null && _combatEntity.vfxSpawnPoint != null)
                return _combatEntity.vfxSpawnPoint.position;
            return transform.position;
        }
    }
}
