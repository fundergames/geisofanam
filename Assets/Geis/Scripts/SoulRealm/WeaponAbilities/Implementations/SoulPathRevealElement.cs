using System.Collections;
using System.Collections.Generic;
using Geis.Puzzles;
using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Attach to hidden anchors or puzzle hints. <see cref="PathRevealSoulWeaponAbility"/> activates
    /// instances within range (or all registered when using global reveal on the ability asset).
    /// Colliders stay enabled while meshes are hidden so overlap pulses can detect this object; use triggers if you
    /// should not block movement.
    ///
    /// <b>Stay hidden until permanent:</b> when enabled, pulse abilities do not flash this object;
    /// call <see cref="RevealPermanent"/> (or <see cref="Reveal"/> which forwards) from a puzzle or
    /// script to unlock. After unlock, add <see cref="PuzzleRealmVisual"/> / <see cref="PuzzleElementBase"/>
    /// on the same object (or parent) so it dissolves in/out on realm transitions like other realm props.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class SoulPathRevealElement : MonoBehaviour
    {
        [Tooltip("If empty, all renderers under this object are toggled.")]
        [SerializeField] private Renderer[] revealRenderers;
        [Tooltip("If empty, all colliders under this object are used. Kept enabled while hidden so path-reveal overlap can hit; assign a trigger if you only need detection.")]
        [SerializeField] private Collider[] revealColliders;

        [Tooltip("If true, Path Reveal / Lyre pulses do not show this object. Use RevealPermanent() or wire Reveal() from gameplay to unlock; then realm presentation (dissolve, mode) applies.")]
        [SerializeField] private bool stayHiddenUntilPermanentActivation;

        private bool _revealed;
        private bool _permanentlyUnlocked;
        private readonly List<bool> _rendererWasEnabled = new List<bool>();
        private Coroutine _hideRoutine;

        public bool IsRevealed => _revealed;

        /// <summary>True after <see cref="RevealPermanent"/> (or <see cref="Reveal"/> when gated).</summary>
        public bool IsPermanentlyUnlocked => _permanentlyUnlocked;

        private void Awake()
        {
            CacheRenderers();
            CacheColliders();
            ApplyHiddenState();
        }

        private void OnDestroy()
        {
            if (_hideRoutine != null)
                StopCoroutine(_hideRoutine);
        }

        private void CacheRenderers()
        {
            if (revealRenderers != null && revealRenderers.Length > 0)
                return;
            revealRenderers = GetComponentsInChildren<Renderer>(true);
        }

        private void CacheColliders()
        {
            if (revealColliders != null && revealColliders.Length > 0)
                return;
            revealColliders = GetComponentsInChildren<Collider>(true);
        }

        /// <summary>
        /// Hides meshes; keeps colliders <b>enabled</b> so <see cref="PathRevealSoulWeaponAbility"/> /
        /// <see cref="LyreWaveReleaseSoulWeaponAbility"/> overlap can find this object before any reveal.
        /// <see cref="PuzzleElementBase"/> still gates colliders by realm (e.g. SoulOnly off in physical world).
        /// </summary>
        private void ApplyHiddenState()
        {
            _rendererWasEnabled.Clear();
            if (revealRenderers != null)
            {
                for (var i = 0; i < revealRenderers.Length; i++)
                {
                    var r = revealRenderers[i];
                    if (r == null) continue;
                    _rendererWasEnabled.Add(r.enabled);
                    r.enabled = false;
                }
            }

            EnableCollidersForPulseDetection();
        }

        private void EnableCollidersForPulseDetection()
        {
            if (revealColliders == null) return;
            for (var i = 0; i < revealColliders.Length; i++)
            {
                var c = revealColliders[i];
                if (c != null)
                    c.enabled = true;
            }
        }

        /// <summary>Shows meshes and optional colliders for this hint.</summary>
        public void Reveal()
        {
            if (stayHiddenUntilPermanentActivation)
                RevealPermanent();
            else
            {
                if (_revealed) return;
                _revealed = true;
                ApplyShownState();
            }
        }

        /// <summary>
        /// Permanently unlocks this hint: meshes/colliders follow <see cref="PuzzleElementBase"/> realm rules
        /// (dissolve, SoulOnly / PhysicalOnly / Both) when that component is present.
        /// </summary>
        public void RevealPermanent()
        {
            if (_permanentlyUnlocked)
                return;
            _permanentlyUnlocked = true;
            _revealed = true;

            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }

            ApplyShownState();
            var puzzle = GetComponent<PuzzleElementBase>() ?? GetComponentInParent<PuzzleElementBase>();
            if (puzzle != null)
                puzzle.ReapplyRealmPresentationAfterExternalVisibilityChange();
        }

        /// <summary>Reveal, then hide again after <paramref name="visibleSeconds"/> (Soul Realm path pulse).</summary>
        public void RevealTemporary(float visibleSeconds)
        {
            if (stayHiddenUntilPermanentActivation)
                return;

            if (!_revealed)
            {
                _revealed = true;
                ApplyShownState();
            }

            if (_hideRoutine != null)
                StopCoroutine(_hideRoutine);
            _hideRoutine = StartCoroutine(HideAfterDelay(visibleSeconds));
        }

        private void ApplyShownState()
        {
            if (revealRenderers != null)
            {
                for (var i = 0; i < revealRenderers.Length; i++)
                {
                    var r = revealRenderers[i];
                    if (r != null)
                        r.enabled = true;
                }
            }

            if (revealColliders != null)
            {
                for (var i = 0; i < revealColliders.Length; i++)
                {
                    var c = revealColliders[i];
                    if (c != null)
                        c.enabled = true;
                }
            }
        }

        /// <summary>Editor / reset: hide again.</summary>
        public void HideForPuzzleReset()
        {
            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }

            _revealed = false;
            _permanentlyUnlocked = false;
            if (revealRenderers == null)
            {
                EnableCollidersForPulseDetection();
                NotifyPuzzleRealmPresentationRefresh();
                return;
            }
            for (var i = 0; i < revealRenderers.Length; i++)
            {
                var r = revealRenderers[i];
                if (r != null && i < _rendererWasEnabled.Count && !_rendererWasEnabled[i])
                    r.enabled = false;
            }

            EnableCollidersForPulseDetection();

            NotifyPuzzleRealmPresentationRefresh();
        }

        private void NotifyPuzzleRealmPresentationRefresh()
        {
            var puzzle = GetComponent<PuzzleElementBase>() ?? GetComponentInParent<PuzzleElementBase>();
            if (puzzle != null)
                puzzle.ReapplyRealmPresentationAfterExternalVisibilityChange();
        }

        private IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _hideRoutine = null;
            HideForPuzzleReset();
        }
    }
}
