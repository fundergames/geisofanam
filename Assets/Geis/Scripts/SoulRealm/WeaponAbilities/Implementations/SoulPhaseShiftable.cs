using System.Collections;
using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Dagger-Flute: temporarily moves colliders to a "phased" layer and scales a ghost visual.
    /// Configure project layers so phased objects ignore blocking geometry.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SoulPhaseShiftable : MonoBehaviour
    {
        [SerializeField] private Collider[] phasedColliders;
        [SerializeField] private GameObject ghostVisualRoot;
        [SerializeField] private int phasedLayer = 2;
        [SerializeField] private float ghostScale = 0.85f;

        [Tooltip("How long phase shift lasts when BeginPhaseShift is called with duration 0 or less (e.g. dagger ability Phase Duration = 0).")]
        [SerializeField] private float defaultDurationSeconds = 6f;

        [Tooltip("Drives realm dissolve pulse + physical semi-transparency; add on the same object or leave empty.")]
        [SerializeField] private SoulPhaseShiftPresentation presentation;

        private int[] _originalLayers;
        private Coroutine _routine;
        private bool _phaseActive;

        /// <summary>True after the player finishes a physical-realm hold pull (see dagger Phase Shift).</summary>
        public bool IsPhysicalSolidified { get; private set; }

        private void Awake()
        {
            if (presentation == null)
                presentation = GetComponent<SoulPhaseShiftPresentation>();

            if (phasedColliders == null || phasedColliders.Length == 0)
                phasedColliders = GetComponentsInChildren<Collider>();
            _originalLayers = new int[phasedColliders.Length];
        }

        /// <summary>Begin phase shift for <paramref name="duration"/> seconds. Use 0 or less to use <see cref="defaultDurationSeconds"/>.</summary>
        public void BeginPhaseShift(float duration = -1f)
        {
            if (duration <= 0f)
                duration = Mathf.Max(0.05f, defaultDurationSeconds);

            if (_routine != null)
                StopCoroutine(_routine);
            _routine = StartCoroutine(PhaseRoutine(duration));
        }

        /// <summary>Visual-only progress for hold-to-solidify in the physical realm (0–1).</summary>
        public void SetPhysicalPullProgress01(float progress)
        {
            presentation?.SetPullProgress01(progress);
        }

        /// <summary>Clears pull preview when the player releases F early or cancels.</summary>
        public void ClearPhysicalPullVisual()
        {
            presentation?.SetPullProgress01(0f);
        }

        /// <summary>Locks full opaque presentation after a completed physical pull.</summary>
        public void CompletePhysicalSolidify()
        {
            IsPhysicalSolidified = true;
            presentation?.SetSolidified(true);
        }

        /// <summary>Return to ethereal pulse + (optional) semi-transparent physical look — for level reset / puzzles.</summary>
        public void ResetPhysicalSoulPresentation()
        {
            IsPhysicalSolidified = false;
            presentation?.ResetToEthereal();
        }

        private IEnumerator PhaseRoutine(float duration)
        {
            ApplyPhased(true);
            yield return new WaitForSeconds(duration);
            ApplyPhased(false);
            _routine = null;
        }

        private void ApplyPhased(bool phased)
        {
            if (phasedColliders == null) return;

            if (phased)
            {
                _phaseActive = true;
                for (var i = 0; i < phasedColliders.Length; i++)
                {
                    var c = phasedColliders[i];
                    if (c == null) continue;
                    if (_originalLayers != null && i < _originalLayers.Length)
                        _originalLayers[i] = c.gameObject.layer;
                    c.gameObject.layer = phasedLayer;
                }
            }
            else if (_phaseActive)
            {
                _phaseActive = false;
                for (var i = 0; i < phasedColliders.Length; i++)
                {
                    var c = phasedColliders[i];
                    if (c == null) continue;
                    if (_originalLayers != null && i < _originalLayers.Length)
                        c.gameObject.layer = _originalLayers[i];
                }
            }

            if (ghostVisualRoot != null)
            {
                ghostVisualRoot.SetActive(phased);
                if (phased)
                    ghostVisualRoot.transform.localScale = Vector3.one * ghostScale;
            }
        }

        private void OnDisable()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            if (_phaseActive)
                ApplyPhased(false);
        }
    }
}
