using System;
using RogueDeal.Events;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Soul-realm puzzle trigger that responds to a sword resonance pulse.
    /// Activates when a <see cref="SoulPulseEvent"/> is raised and this receptor is within
    /// the pulse's combined radius (<c>evt.Radius + detectionRadius</c>).
    ///
    /// When the sword soul-pulse system is built, its controller calls
    /// <see cref="RaisePulse"/> at the ghost's position. Until then, use the
    /// context-menu test action or raise the event directly in code.
    ///
    /// Default realm: SoulOnly.
    /// </summary>
    public class SoulPulseReceptorTrigger : PuzzleTriggerBase
    {
        [Header("Detection")]
        [Tooltip("Additional radius around this receptor that counts as 'within range' of a pulse.")]
        [SerializeField] private float detectionRadius = 2f;

        [Header("Audio")]
        [SerializeField] private AudioClip resonateSound;
        [SerializeField] private AudioSource audioSource;

        [Header("Visual")]
        [Tooltip("Optional particle or glow object shown while activated.")]
        [SerializeField] private GameObject activeVFX;

        private IDisposable _subscription;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            _subscription = EventBus<SoulPulseEvent>.Subscribe(OnPulseReceived);
        }

        private void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        private void OnPulseReceived(SoulPulseEvent evt)
        {
            if (IsActivated) return;
            if (!IsAccessibleInCurrentRealm()) return;

            float dist = Vector3.Distance(transform.position, evt.Origin);
            if (dist > evt.Radius + detectionRadius) return;

            if (resonateSound != null && audioSource != null)
                audioSource.PlayOneShot(resonateSound);

            if (activeVFX != null)
                activeVFX.SetActive(true);

            SetActivated(true);
        }

        public override void ResetSilent()
        {
            base.ResetSilent();
            if (activeVFX != null)
                activeVFX.SetActive(false);
        }

        // ── Static helper ────────────────────────────────────────────────────────

        /// <summary>
        /// Raise a soul resonance pulse at <paramref name="origin"/> with the given
        /// <paramref name="radius"/>. Call this from the sword soul-pulse system, or from
        /// a test script / context menu action.
        /// </summary>
        public static void RaisePulse(Vector3 origin, float radius)
        {
            EventBus<SoulPulseEvent>.Raise(new SoulPulseEvent { Origin = origin, Radius = radius });
        }

        // ── Editor test ──────────────────────────────────────────────────────────

        [ContextMenu("Test: Raise Pulse At This Position")]
        private void TestRaisePulse()
        {
            RaisePulse(transform.position, detectionRadius);
        }
    }
}
