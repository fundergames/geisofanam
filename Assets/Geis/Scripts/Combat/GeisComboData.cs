// Geis of Anam - Data-driven combo system.
// Defines combo graph (transition table + clips) per weapon. Add branches by editing data only.

using UnityEngine;
using RogueDeal.Combat.Core.Data;

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
    /// Per combo state (same index as animation clips array): optional RogueDeal action override and multi-hit timing on that clip.
    /// </summary>
    [System.Serializable]
    public class GeisComboStateCombatBinding
    {
        [Tooltip("If set, used instead of GeisWeaponDefinition.combatAction for this combo step.")]
        public CombatAction combatActionOverride;

        [Tooltip("Multi-hit in this clip: contact times as normalized clip time (0-1). One entry = one hit. Empty = SimpleAttackHitDetector uses action/inspector timing.")]
        public float[] multiHitNormalizedTimes;
    }

    /// <summary>
    /// Data-driven combo definition per weapon. Transition table + clip assignments.
    /// Add new branches by adding transitions and clips; no animator changes.
    /// </summary>
    [CreateAssetMenu(fileName = "ComboData_", menuName = "Geis/Combat/Combo Data")]
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

            AnimationClip clip = GetClipForState(state);
            float len = clip != null ? clip.length : (fallbackClip != null ? fallbackClip.length : 1f);

            int n = binding.multiHitNormalizedTimes.Length;
            secondsFromAttackStart = new float[n];
            for (int i = 0; i < n; i++)
                secondsFromAttackStart[i] = Mathf.Clamp01(binding.multiHitNormalizedTimes[i]) * len;
            return true;
        }

        private GeisComboStateCombatBinding GetBinding(int state)
        {
            if (stateCombatBindings == null || state < 0 || state >= stateCombatBindings.Length)
                return null;
            return stateCombatBindings[state];
        }
    }
}
