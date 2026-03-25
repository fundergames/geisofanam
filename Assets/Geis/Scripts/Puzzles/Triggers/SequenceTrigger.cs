using System.Collections;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Musical sequence puzzle. Contains N child SoulSwitchTrigger steps that must be
    /// activated in the correct order. Wrong order plays an error sound and resets progress.
    /// Completing the full sequence activates this trigger.
    ///
    /// Wire this trigger (not the child steps) into PuzzleGroup.
    /// Default realm: SoulOnly (inherited by child steps automatically).
    /// </summary>
    public class SequenceTrigger : PuzzleTriggerBase
    {
        [Header("Sequence Steps")]
        [Tooltip("Child SoulSwitchTrigger steps in the correct activation order.")]
        [SerializeField] private SoulSwitchTrigger[] steps;
        [Tooltip("Optional colours per step. Applied to a Renderer on each step object.")]
        [SerializeField] private Color[] stepActiveColors;
        [SerializeField] private Color   stepDefaultColor = Color.white;

        [Header("Audio")]
        [SerializeField] private AudioClip[] stepSounds;
        [SerializeField] private AudioClip   errorSound;
        [SerializeField] private AudioClip   solvedSound;
        [SerializeField] private AudioSource audioSource;

        private int _progress;
        private Renderer[] _stepRenderers;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            _stepRenderers = new Renderer[steps != null ? steps.Length : 0];
            for (int i = 0; i < _stepRenderers.Length; i++)
                if (steps[i] != null)
                    _stepRenderers[i] = steps[i].GetComponentInChildren<Renderer>();
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

        private void HandleStepActivated(PuzzleTriggerBase trigger)
        {
            if (IsActivated) return;

            int index = System.Array.IndexOf(steps, trigger as SoulSwitchTrigger);
            if (index < 0) return;

            if (index == _progress)
            {
                // Correct step
                PlaySound(stepSounds, _progress);
                SetStepColor(index, true);
                _progress++;

                if (_progress >= steps.Length)
                {
                    // Full sequence complete
                    PlayOneShot(solvedSound);
                    SetActivated(true);
                }
            }
            else
            {
                // Wrong step
                PlayOneShot(errorSound);
                StartCoroutine(ResetWithDelay(0.5f));
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
            if (steps != null)
                foreach (var s in steps)
                    s?.ResetSilent();

            for (int i = 0; i < _stepRenderers.Length; i++)
                SetStepColor(i, false);
        }

        public override void ResetSilent()
        {
            base.ResetSilent();
            ResetProgress();
        }

        private void SetStepColor(int index, bool active)
        {
            if (index >= _stepRenderers.Length || _stepRenderers[index] == null) return;
            Color target = active && stepActiveColors != null && index < stepActiveColors.Length
                ? stepActiveColors[index]
                : stepDefaultColor;
            _stepRenderers[index].material.color = target;
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
