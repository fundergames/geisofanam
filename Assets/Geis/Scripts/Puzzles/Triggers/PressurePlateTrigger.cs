using Geis.SoulRealm;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Activates while one or more colliders with the target tag are overlapping it.
    /// <see cref="PuzzleElementBase"/> realm mode gates when overlaps count (e.g. SoulOnly = soul realm only).
    /// The tag is checked on the <b>same GameObject as the collider</b> (usually the CharacterController root), not the prefab root.
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

        [Header("Visuals — color (optional)")]
        [Tooltip("If set, used for tint; otherwise uses Renderer on plateVisual.")]
        [SerializeField] private Renderer plateRenderer;
        [Tooltip("Tint the plate material when pressed (URP Lit uses _BaseColor).")]
        [SerializeField] private bool useColorTint = false;
        [SerializeField] private Color releasedColor = new Color(0.45f, 0.85f, 0.45f, 1f);
        [SerializeField] private Color pressedColor  = new Color(0.15f, 0.95f, 0.35f, 1f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId     = Shader.PropertyToID("_Color");

        private int _overlapCount;
        private MaterialPropertyBlock _mpb;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;

            if (plateRenderer == null && plateVisual != null)
                plateRenderer = plateVisual.GetComponent<Renderer>();

            if (useColorTint && plateRenderer != null)
                ApplyVisual(false);
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

        /// <summary>
        /// After soul realm ends, the ghost is disabled and Unity fires trigger exits that are not
        /// "walked off" overlaps. Recompute who is actually on the plate so puzzle outputs are not
        /// spuriously deactivated while a platform is mid-move.
        /// </summary>
        public static void RefreshOverlapsAfterSoulRealmExit()
        {
            var plates = Object.FindObjectsByType<PressurePlateTrigger>(FindObjectsSortMode.None);
            for (int i = 0; i < plates.Length; i++)
            {
                if (plates[i] != null && plates[i].isActiveAndEnabled)
                    plates[i].RecomputeOverlapsFromPhysics();
            }
        }

        private void RecomputeOverlapsFromPhysics()
        {
            var col = GetComponent<Collider>();
            if (col == null)
                return;

            if (!IsAccessibleInCurrentRealm())
            {
                _overlapCount = 0;
                if (IsActivated)
                {
                    SetActivated(false);
                    ApplyVisual(false);
                }
                return;
            }

            Bounds b = col.bounds;
            Vector3 halfExtents = b.extents + new Vector3(0.03f, 0.08f, 0.03f);
            Collider[] hits = Physics.OverlapBox(b.center, halfExtents, transform.rotation, ~0, QueryTriggerInteraction.Ignore);

            int n = 0;
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h == null || h == col)
                    continue;
                if (!MatchesTag(h))
                    continue;
                if (!CountsActivatorForRealm(h))
                    continue;
                n++;
            }

            _overlapCount = n;
            bool shouldBeActive = n > 0;
            if (shouldBeActive != IsActivated)
                SetActivated(shouldBeActive);
            ApplyVisual(shouldBeActive);
        }

        private bool CountsActivatorForRealm(Collider other)
        {
            bool hasGhost = other.GetComponentInParent<SoulGhostMotor>() != null;
            bool soulOn = SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive;

            return RealmMode switch
            {
                PuzzleRealmMode.SoulOnly => soulOn && hasGhost,
                PuzzleRealmMode.PhysicalOnly => !soulOn && !hasGhost,
                PuzzleRealmMode.BothRealms => true,
                _ => false,
            };
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
            if (plateVisual != null)
                plateVisual.localScale = pressed ? pressedScale : releasedScale;

            if (!useColorTint || plateRenderer == null)
                return;

            _mpb ??= new MaterialPropertyBlock();
            plateRenderer.GetPropertyBlock(_mpb);
            var c = pressed ? pressedColor : releasedColor;
            _mpb.SetColor(BaseColorId, c);
            _mpb.SetColor(ColorId, c);
            plateRenderer.SetPropertyBlock(_mpb);
        }
    }
}
