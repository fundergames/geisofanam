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
        [SerializeField] private float defaultDurationSeconds = 6f;

        private int[] _originalLayers;
        private Coroutine _routine;
        private bool _phaseActive;

        private void Awake()
        {
            if (phasedColliders == null || phasedColliders.Length == 0)
                phasedColliders = GetComponentsInChildren<Collider>();
            _originalLayers = new int[phasedColliders.Length];
        }

        /// <summary>Begin phase shift for <paramref name="duration"/> seconds (or default).</summary>
        public void BeginPhaseShift(float duration = -1f)
        {
            if (duration <= 0f)
                duration = defaultDurationSeconds;

            if (_routine != null)
                StopCoroutine(_routine);
            _routine = StartCoroutine(PhaseRoutine(duration));
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
