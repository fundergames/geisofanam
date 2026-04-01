using System.Collections;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Musical sequence puzzle. Contains child SoulSwitchTrigger steps that must be
    /// activated in the order defined by <see cref="sequenceOrder"/> (or array order if empty).
    /// Wrong order plays an error sound; progress may reset depending on <see cref="resetOnWrong"/>.
    /// Optional <see cref="sequenceTimeLimitSeconds"/> resets partial progress when the window expires.
    /// Completing the full sequence activates this trigger.
    ///
    /// Wire this trigger (not the child steps) into PuzzleGroup.
    /// Default realm: SoulOnly (inherited by child steps automatically).
    /// </summary>
    public class SequenceTrigger : PuzzleTriggerBase
    {
        [Header("Sequence Steps")]
        [Tooltip("Child SoulSwitchTrigger steps (any inspector order).")]
        [SerializeField] private SoulSwitchTrigger[] steps;
        [Tooltip("Order of activation: each value is an index into Steps (0 = first in the Steps list). " +
                 "Array length = number of hits required (can be shorter than Steps). " +
                 "Leave empty to require 0,1,2,... in Steps array order.")]
        [SerializeField] private int[] sequenceOrder;
        [Tooltip("If true, a wrong step resets all progress after the error delay. If false, wrong steps are rejected but progress is kept.")]
        [SerializeField] private bool resetOnWrong = true;
        [Header("Timing (optional)")]
        [Tooltip("If > 0, the sequence must be finished within this many seconds after the first correct step. " +
                 "When time runs out, progress resets and all steps return to inactive (start over).")]
        [SerializeField] private float sequenceTimeLimitSeconds = 0f;
        [Tooltip("Optional colours per step. Applied to a Renderer on each step object.")]
        [SerializeField] private Color[] stepActiveColors;
        [SerializeField] private Color   stepDefaultColor = Color.white;

        [Header("Audio")]
        [SerializeField] private AudioClip[] stepSounds;
        [SerializeField] private AudioClip   errorSound;
        [SerializeField] private AudioClip   solvedSound;
        [SerializeField] private AudioSource audioSource;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId     = Shader.PropertyToID("_Color");

        private MaterialPropertyBlock _stepTintMpb;
        private int _progress;
        private int[] _expectedOrder = System.Array.Empty<int>();
        private float _sequenceDeadline;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            RebuildExpectedOrder();
        }

        private IEnumerator Start()
        {
            // Child PuzzleTriggerBase.Start runs activation visuals first; wait one frame so order is settled.
            yield return null;
            if (steps != null)
            {
                for (int i = 0; i < steps.Length; i++)
                    SetStepColor(i, false);
            }
        }

        private void OnEnable()
        {
            if (steps == null) return;
            foreach (var s in steps)
            {
                if (s == null) continue;
                s.OnTriggerActivated += HandleStepActivated;
            }
        }

        private void OnDisable()
        {
            if (steps == null) return;
            foreach (var s in steps)
            {
                if (s == null) continue;
                s.OnTriggerActivated -= HandleStepActivated;
            }
        }

        private void Update()
        {
            if (IsActivated) return;
            if (sequenceTimeLimitSeconds <= 0f || _progress <= 0) return;
            if (Time.time < _sequenceDeadline) return;

            PlayOneShot(errorSound);
            ResetProgress();
        }

        private void HandleStepActivated(PuzzleTriggerBase trigger)
        {
            if (IsActivated) return;

            int index = System.Array.IndexOf(steps, trigger as SoulSwitchTrigger);
            if (index < 0) return;

            if (_expectedOrder == null || _expectedOrder.Length == 0)
                RebuildExpectedOrder();

            int expectedLen = _expectedOrder.Length;
            if (expectedLen == 0) return;

            int expectedIndex = _expectedOrder[_progress];
            if (index == expectedIndex)
            {
                PlaySound(stepSounds, _progress);
                SetStepColor(index, true);
                _progress++;
                if (_progress < expectedLen && sequenceTimeLimitSeconds > 0f)
                    _sequenceDeadline = Time.time + sequenceTimeLimitSeconds;

                if (_progress >= expectedLen)
                {
                    PlayOneShot(solvedSound);
                    SetActivated(true);
                }
            }
            else
            {
                PlayOneShot(errorSound);
                if (resetOnWrong)
                    StartCoroutine(ResetWithDelay(0.5f));
                else
                    trigger.ResetSilent();
            }
        }

        private IEnumerator ResetWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ResetProgress();
        }

        private void ResetProgress()
        {
            _progress = 0;
            _sequenceDeadline = 0f;
            if (steps != null)
                foreach (var s in steps)
                    s?.ResetSilent();

            if (steps != null)
            {
                for (int i = 0; i < steps.Length; i++)
                    SetStepColor(i, false);
            }
        }

        /// <summary>
        /// Builds <see cref="_expectedOrder"/> from <see cref="sequenceOrder"/> or identity 0..steps.Length-1.
        /// </summary>
        private void RebuildExpectedOrder()
        {
            if (steps == null || steps.Length == 0)
            {
                _expectedOrder = System.Array.Empty<int>();
                return;
            }

            if (sequenceOrder == null || sequenceOrder.Length == 0)
            {
                _expectedOrder = new int[steps.Length];
                for (int i = 0; i < steps.Length; i++)
                    _expectedOrder[i] = i;
                return;
            }

            foreach (int idx in sequenceOrder)
            {
                if (idx < 0 || idx >= steps.Length)
                {
                    Debug.LogWarning(
                        $"[SequenceTrigger] '{name}': sequenceOrder contains invalid index {idx} (steps length {steps.Length}). Using 0..{steps.Length - 1}.",
                        this);
                    _expectedOrder = new int[steps.Length];
                    for (int i = 0; i < steps.Length; i++)
                        _expectedOrder[i] = i;
                    return;
                }
            }

            _expectedOrder = (int[])sequenceOrder.Clone();
        }

        public override void ResetSilent()
        {
            base.ResetSilent();
            ResetProgress();
        }

        private void SetStepColor(int index, bool active)
        {
            if (steps == null || index < 0 || index >= steps.Length || steps[index] == null)
                return;
            var r = steps[index].GetRendererForActivationTinting();
            if (r == null)
                return;
            Color target = active && stepActiveColors != null && index < stepActiveColors.Length
                ? stepActiveColors[index]
                : stepDefaultColor;
            _stepTintMpb ??= new MaterialPropertyBlock();
            r.GetPropertyBlock(_stepTintMpb);
            _stepTintMpb.SetColor(BaseColorId, target);
            _stepTintMpb.SetColor(ColorId, target);
            r.SetPropertyBlock(_stepTintMpb);
        }

        private void PlaySound(AudioClip[] clips, int index)
        {
            if (clips == null || index >= clips.Length || clips[index] == null) return;
            PlayOneShot(clips[index]);
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (clip == null || audioSource == null) return;
            audioSource.PlayOneShot(clip);
        }
    }
}
