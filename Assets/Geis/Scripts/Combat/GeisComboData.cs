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

// Geis of Anam - Data-driven combo system.
// Defines combo graph (transition table + clips) per weapon. Add branches by editing data only.

using UnityEngine;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Presentation;

namespace Geis.Combat
{
    /// <summary>
    /// Input type for combo transitions (Light, Heavy).
    /// </summary>
    public enum GeisComboInputType
    {
        Light = 0,
        Heavy = 1
    }

    /// <summary>
    /// Single transition: fromState + input → toState.
    /// </summary>
    [System.Serializable]
    public class GeisComboTransition
    {
        [Tooltip("Combo state to transition from (0 = first hit)")]
        public int fromState;

        [Tooltip("Input that triggers this transition")]
        public GeisComboInputType inputType;

        [Tooltip("Combo state to transition to (0 = reset to start)")]
        public int toState;
    }

    /// <summary>
    /// Scheduled SFX/VFX on a combo step (authored in combo graph presentation track).
    /// </summary>
    [System.Serializable]
    public class GeisComboPresentationEvent
    {
        [Tooltip("When to fire relative to attack start (0-1 on the combo clip).")]
        [Range(0f, 1f)]
        public float normalizedTime;

        [Tooltip("Optional label for debugging.")]
        public string eventName;

        public AudioClip sfx;

        public GameObject vfxPrefab;

        [Header("Feel (optional)")]
        public CombatCameraShakeSpec cameraShake;
        public CombatHitStopSpec hitStop;
    }

    /// <summary>
    /// Per combo state (same index as animation clips array): optional RogueDeal action override and multi-hit timing on that clip.
    /// </summary>
    [System.Serializable]
    public class GeisComboStateCombatBinding
    {
        [Tooltip("If set, used instead of GeisWeaponDefinition.combatAction for this combo step.")]
        public CombatAction combatActionOverride;

        [Tooltip("Multi-hit in this clip: contact times as normalized clip time (0-1). One entry = one hit. Empty = SimpleAttackHitDetector uses action/inspector timing.")]
        public float[] multiHitNormalizedTimes;

        [Tooltip("Per-step swing/whoosh/trail SFX and VFX (cyan track in combo editor). Empty falls back to CombatAction.effectBindings.")]
        public GeisComboPresentationEvent[] presentationEvents;

        [Header("Impact feel (at multi-hit / damage times)")]
        [Tooltip("Camera shake when each orange hit marker fires.")]
        public CombatCameraShakeSpec impactCameraShake;

        [Tooltip("Hit-stop when each orange hit marker fires.")]
        public CombatHitStopSpec impactHitStop;
    }

/// <summary>Startup / active / recovery segment of a combo step (fighting-game frame data).</summary>
public enum GeisComboAttackPhase
{
    Startup = 0,
    Active = 1,
    Recovery = 2
}

/// <summary>
/// Per combo state attack phases for interrupt and super-armor rules.
/// </summary>
[System.Serializable]
public class GeisComboStatePhase
{
    [Tooltip("If true, uses the normalized thresholds below instead of shared defaults on GeisComboData.")]
    public bool overridePhaseWindows;

    [Tooltip("Normalized clip time where startup ends and active frames begin.")]
    [Range(0f, 1f)]
    public float startupEndNormalized = 0.2f;

    [Tooltip("Normalized clip time where active ends and recovery begins.")]
    [Range(0f, 1f)]
    public float activeEndNormalized = 0.55f;

    [Tooltip("While in startup, the attacker does not flinch or cancel from incoming hits.")]
    public bool armorDuringStartup = true;

    [Tooltip("When false, dodge i-frames on the defender only avoid hits during the attacker's active phase.")]
    public bool dodgeOnlyAvoidsDuringActive = true;
}

/// <summary>
/// Optional per combo state timing overrides.
/// </summary>
[System.Serializable]
public class GeisComboStateTiming
{
    [Tooltip("If true, this combo step uses its own cancel window instead of the shared defaults below.")]
    public bool overrideCancelWindow;

    [Tooltip("Normalized time (0-1) when this step's cancel window opens.")]
    [Range(0f, 1f)]
    public float cancelWindowStart = 0.5f;

    [Tooltip("Normalized time (0-1) when this step's cancel window closes.")]
    [Range(0f, 1f)]
    public float cancelWindowEnd = 0.7f;
}

    /// <summary>
    /// Data-driven combo definition per weapon. Transition table + clip assignments.
    /// Add new branches by adding transitions and clips; no animator changes.
    /// </summary>
    [CreateAssetMenu(fileName = "ComboData_", menuName = "Funder Games/Geis/Combat/Combo Data")]
    public class GeisComboData : ScriptableObject
    {
        [Header("Transition Table")]
        [Tooltip("Transitions: fromState + inputType → toState. Example: (1, Light) → 2, (1, Heavy) → 3")]
        [SerializeField]
        private GeisComboTransition[] transitions = new GeisComboTransition[0];

        [Header("Animation Clips")]
        [Tooltip("Clips indexed by ComboState. clips[0]=first hit, clips[1]=second, etc. Unused slots can be null.")]
        [SerializeField]
        private AnimationClip[] clips = new AnimationClip[0];

        [Tooltip("Fallback clip when clips[state] is null")]
        [SerializeField]
        private AnimationClip fallbackClip;

        [Header("Combat (RogueDeal)")]
        [Tooltip("Parallel to clips[]: index = combo state. Optional per-step CombatAction override and normalized multi-hit times for that clip.")]
        [SerializeField]
        private GeisComboStateCombatBinding[] stateCombatBindings = new GeisComboStateCombatBinding[0];

        [Header("Timing")]
        [Tooltip("Normalized time (0-1) when cancel window opens. Higher = current attack plays longer before chaining (smoother feel).")]
        [Range(0f, 1f)]
        [SerializeField]
        private float cancelWindowStart = 0.5f;

        [Tooltip("Normalized time (0-1) when cancel window closes")]
        [Range(0f, 1f)]
        [SerializeField]
        private float cancelWindowEnd = 0.7f;

        [Tooltip("Optional per-step cancel windows. Parallel to clips[]; enable Override Cancel Window on a step to customize that attack.")]
        [SerializeField]
        private GeisComboStateTiming[] stateTimings = new GeisComboStateTiming[0];

        [Header("Attack phases")]
        [Tooltip("Default startup end (normalized) when a step has no per-state phase override.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float defaultStartupEndNormalized = 0.2f;

        [Tooltip("Default active end (normalized) when a step has no per-state phase override.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float defaultActiveEndNormalized = 0.55f;

        [Tooltip("Optional per-step phase windows and armor. Parallel to clips[].")]
        [SerializeField]
        private GeisComboStatePhase[] statePhases = new GeisComboStatePhase[0];

        /// <summary>
        /// Try to find a transition from currentState with the given input. Returns true and out nextState if found.
        /// </summary>
        public bool TryGetNextState(int currentState, GeisComboInputType inputType, out int nextState)
        {
            nextState = 0;
            if (transitions == null) return false;

            foreach (var t in transitions)
            {
                if (t.fromState == currentState && t.inputType == inputType)
                {
                    nextState = t.toState;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get the clip for the given combo state. Returns fallbackClip if null.
        /// </summary>
        public AnimationClip GetClipForState(int state)
        {
            if (clips != null && state >= 0 && state < clips.Length && clips[state] != null)
                return clips[state];
            return fallbackClip;
        }

        public float CancelWindowStart => cancelWindowStart;
        public float CancelWindowEnd => cancelWindowEnd;
        public int ClipCount => clips != null ? clips.Length : 0;

        /// <summary>
        /// Resolve the cancel window for the current combo state, falling back to the shared defaults.
        /// </summary>
        public void GetCancelWindow(int state, out float start, out float end)
        {
            start = Mathf.Clamp01(cancelWindowStart);
            end = Mathf.Clamp01(cancelWindowEnd);

            GeisComboStateTiming timing = GetTiming(state);
            if (timing != null && timing.overrideCancelWindow)
            {
                start = Mathf.Clamp01(timing.cancelWindowStart);
                end = Mathf.Clamp01(timing.cancelWindowEnd);
            }

            if (end < start)
                end = start;
        }

        /// <summary>
        /// Combat action for this combo step: binding override if set, otherwise <paramref name="weaponDefault"/>.
        /// </summary>
        public CombatAction ResolveCombatAction(int state, CombatAction weaponDefault)
        {
            var binding = GetBinding(state);
            if (binding != null && binding.combatActionOverride != null)
                return binding.combatActionOverride;
            return weaponDefault;
        }

        /// <summary>
        /// If this state has multi-hit normalized times, converts them to seconds from attack start using the resolved clip length.
        /// </summary>
        public bool TryGetMultiHitTimesSeconds(int state, out float[] secondsFromAttackStart)
        {
            secondsFromAttackStart = null;
            var binding = GetBinding(state);
            if (binding == null || binding.multiHitNormalizedTimes == null || binding.multiHitNormalizedTimes.Length == 0)
                return false;

            float len = GetClipLengthSeconds(state);
            int n = binding.multiHitNormalizedTimes.Length;
            secondsFromAttackStart = new float[n];
            for (int i = 0; i < n; i++)
                secondsFromAttackStart[i] = Mathf.Clamp01(binding.multiHitNormalizedTimes[i]) * len;
            return true;
        }

        /// <summary>
        /// Combo-step presentation events for this state. Returns false when empty (caller should fall back to CombatAction.effectBindings).
        /// </summary>
        public bool TryGetPresentationEvents(int state, out GeisComboPresentationEvent[] events)
        {
            events = null;
            var binding = GetBinding(state);
            if (binding?.presentationEvents == null || binding.presentationEvents.Length == 0)
                return false;

            events = binding.presentationEvents;
            return true;
        }

        /// <summary>
        /// Clip length used to convert normalized times to seconds-from-attack-start.
        /// </summary>
        public float GetClipLengthSeconds(int state)
        {
            AnimationClip clip = GetClipForState(state);
            if (clip != null)
                return clip.length;
            return fallbackClip != null ? fallbackClip.length : 1f;
        }

        /// <summary>
        /// Impact feel scheduled at each <see cref="GeisComboStateCombatBinding.multiHitNormalizedTimes"/> entry.
        /// </summary>
        public bool TryGetImpactFeelCues(int state, out CombatImpactFeelCue[] cues)
        {
            cues = null;
            var binding = GetBinding(state);
            if (binding == null || binding.multiHitNormalizedTimes == null || binding.multiHitNormalizedTimes.Length == 0)
                return false;

            bool hasShake = binding.impactCameraShake.enabled;
            bool hasStop = binding.impactHitStop.enabled;
            if (!hasShake && !hasStop)
                return false;

            float clipLen = GetClipLengthSeconds(state);
            int n = binding.multiHitNormalizedTimes.Length;
            cues = new CombatImpactFeelCue[n];
            for (int i = 0; i < n; i++)
            {
                cues[i] = new CombatImpactFeelCue
                {
                    timeSeconds = Mathf.Clamp01(binding.multiHitNormalizedTimes[i]) * clipLen,
                    cameraShake = binding.impactCameraShake,
                    hitStop = binding.impactHitStop
                };
            }

            return true;
        }

        private GeisComboStateCombatBinding GetBinding(int state)
        {
            if (stateCombatBindings == null || state < 0 || state >= stateCombatBindings.Length)
                return null;
            return stateCombatBindings[state];
        }

        private GeisComboStateTiming GetTiming(int state)
        {
            if (stateTimings == null || state < 0 || state >= stateTimings.Length)
                return null;
            return stateTimings[state];
        }

        public void GetPhaseWindows(int state, out float startupEnd, out float activeEnd)
        {
            startupEnd = Mathf.Clamp01(defaultStartupEndNormalized);
            activeEnd = Mathf.Clamp01(defaultActiveEndNormalized);
            if (activeEnd < startupEnd)
                activeEnd = startupEnd;

            GeisComboStatePhase phase = GetPhase(state);
            if (phase != null && phase.overridePhaseWindows)
            {
                startupEnd = Mathf.Clamp01(phase.startupEndNormalized);
                activeEnd = Mathf.Clamp01(phase.activeEndNormalized);
                if (activeEnd < startupEnd)
                    activeEnd = startupEnd;
            }
        }

        public GeisComboAttackPhase GetAttackPhase(int state, float normalizedTime)
        {
            float t = Mathf.Clamp01(normalizedTime % 1f);
            GetPhaseWindows(state, out float startupEnd, out float activeEnd);

            if (t < startupEnd)
                return GeisComboAttackPhase.Startup;
            if (t < activeEnd)
                return GeisComboAttackPhase.Active;
            return GeisComboAttackPhase.Recovery;
        }

        public bool HasSuperArmorDuringStartup(int state)
        {
            GeisComboStatePhase phase = GetPhase(state);
            return phase == null || phase.armorDuringStartup;
        }

        /// <summary>
        /// When true, defender dodge only avoids this attack during the attacker's active frames.
        /// </summary>
        public bool DodgeOnlyAvoidsDuringActivePhase(int state)
        {
            GeisComboStatePhase phase = GetPhase(state);
            return phase == null || phase.dodgeOnlyAvoidsDuringActive;
        }

        private GeisComboStatePhase GetPhase(int state)
        {
            if (statePhases == null || state < 0 || state >= statePhases.Length)
                return null;
            return statePhases[state];
        }
    }
}
