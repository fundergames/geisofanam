using Geis.SoulRealm;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Dagger puzzle trigger. Activates while a GameObject tagged
    /// <see cref="acceptedTag"/> ("DaggerMovable") is inside this socket zone.
    ///
    /// When the dagger transposition system is built, it tags moved objects as
    /// "DaggerMovable" before placing them. No coupling to the dagger controller
    /// is needed here — this trigger responds purely to the tag.
    ///
    /// Soul-realm behaviour: optionally shows a <see cref="slotIndicatorPrefab"/>
    /// only in the soul realm to teach the player where objects belong ("true position
    /// reveal" from the design doc).
    ///
    /// Default realm: PhysicalOnly (dagger transposition happens in the physical world).
    /// Set to BothRealms if the indicator should also gate in physical realm.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DaggerSocketTrigger : PuzzleTriggerBase
    {
        [Header("Socket")]
        [Tooltip("Tag that identifies objects which can fill this socket (set by the dagger system).")]
        [SerializeField] private string acceptedTag = "DaggerMovable";

        [Header("Soul Realm Indicator")]
        [Tooltip("Optional ghost/glow showing where an object should be placed. " +
                 "Only visible in the soul realm.")]
        [SerializeField] private GameObject slotIndicatorPrefab;
        [SerializeField] private bool showIndicatorInSoulRealm = true;

        [Header("Audio")]
        [SerializeField] private AudioClip socketSound;
        [SerializeField] private AudioClip unsocketSound;
        [SerializeField] private AudioSource audioSource;

        private int        _overlapCount;
        private GameObject _activeIndicator;
        private bool       _indicatorVisible;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            if (!showIndicatorInSoulRealm || slotIndicatorPrefab == null) return;

            bool soulActive = SoulRealmManager.Instance != null &&
                              SoulRealmManager.Instance.IsSoulRealmActive;
            bool wantIndicator = soulActive && !IsActivated;

            if (wantIndicator && !_indicatorVisible)
                ShowIndicator();
            else if (!wantIndicator && _indicatorVisible)
                HideIndicator();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(acceptedTag)) return;

            _overlapCount++;
            if (_overlapCount == 1)
            {
                if (socketSound != null && audioSource != null)
                    audioSource.PlayOneShot(socketSound);

                HideIndicator();
                SetActivated(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(acceptedTag)) return;

            _overlapCount = Mathf.Max(0, _overlapCount - 1);
            if (_overlapCount == 0)
            {
                if (unsocketSound != null && audioSource != null)
                    audioSource.PlayOneShot(unsocketSound);

                SetActivated(false);
            }
        }

        public override void ResetSilent()
        {
            base.ResetSilent();
            _overlapCount = 0;
            HideIndicator();
        }

        private void ShowIndicator()
        {
            if (_activeIndicator == null)
                _activeIndicator = Instantiate(slotIndicatorPrefab, transform.position,
                    transform.rotation, transform);
            _activeIndicator.SetActive(true);
            _indicatorVisible = true;
        }

        private void HideIndicator()
        {
            if (_activeIndicator != null)
                _activeIndicator.SetActive(false);
            _indicatorVisible = false;
        }

        private void OnDestroy() => HideIndicator();

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.5f);
            var col = GetComponent<Collider>();
            if (col != null)
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
