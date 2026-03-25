using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Activates while one or more colliders with the target tag are overlapping it.
    /// Works in soul realm (ghost tag) or physical realm (player tag) depending on realm mode.
    ///
    /// Default realm: SoulOnly — put the ghost tag on the SoulGhost root.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PressurePlateTrigger : PuzzleTriggerBase
    {
        [Header("Detection")]
        [Tooltip("Tag of colliders that can activate this plate.")]
        [SerializeField] private string activatorTag = "Player";
        [Tooltip("Optional second tag (e.g. both ghost and player can activate).")]
        [SerializeField] private string secondaryTag = "";

        [Header("Visuals")]
        [Tooltip("Transform to scale when pressed (the plate mesh).")]
        [SerializeField] private Transform plateVisual;
        [SerializeField] private Vector3 pressedScale  = new Vector3(1f, 0.5f, 1f);
        [SerializeField] private Vector3 releasedScale = Vector3.one;

        private int _overlapCount;
        private Vector3 _originalScale;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;

            if (plateVisual != null)
                _originalScale = plateVisual.localScale;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsAccessibleInCurrentRealm()) return;
            if (!MatchesTag(other)) return;

            _overlapCount++;
            if (_overlapCount == 1)
            {
                SetActivated(true);
                ApplyVisual(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!MatchesTag(other)) return;

            _overlapCount = Mathf.Max(0, _overlapCount - 1);
            if (_overlapCount == 0)
            {
                SetActivated(false);
                ApplyVisual(false);
            }
        }

        public override void ResetSilent()
        {
            base.ResetSilent();
            _overlapCount = 0;
            ApplyVisual(false);
        }

        private bool MatchesTag(Collider other)
        {
            return other.CompareTag(activatorTag) ||
                   (!string.IsNullOrEmpty(secondaryTag) && other.CompareTag(secondaryTag));
        }

        private void ApplyVisual(bool pressed)
        {
            if (plateVisual == null) return;
            plateVisual.localScale = pressed ? pressedScale : releasedScale;
        }
    }
}
