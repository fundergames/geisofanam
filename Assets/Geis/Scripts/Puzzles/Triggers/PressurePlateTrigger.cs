using Geis.SoulRealm;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Activates while one or more colliders with the target tag are overlapping it.
    /// <see cref="PuzzleElementBase"/> realm mode gates when overlaps count (e.g. SoulOnly = soul realm only).
    /// New plates default to <see cref="PuzzleRealmMode.BothRealms"/> (see <see cref="Reset"/>); SoulOnly plates are hidden in the physical world.
    /// The tag may be on the collider&apos;s GameObject or any parent (e.g. CharacterController on a child mesh).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PressurePlateTrigger : PuzzleTriggerBase
    {
        [Header("Detection")]
        [Tooltip("Tag of colliders that can activate this plate.")]
        [SerializeField] private string activatorTag = "Player";
        [Tooltip("Optional second tag (e.g. both ghost and player can activate).")]
        [SerializeField] private string secondaryTag = "";

        [Tooltip("Extra half-extents added to physics overlap queries (soul-realm refresh). Match ~CharacterController radius + a bit of vertical slack so thin plates still register.")]
        [SerializeField] private Vector3 overlapQueryPadding = new Vector3(0.35f, 0.85f, 0.35f);

        [Tooltip("If true, BoxCollider size is expanded once from a captured base so the trigger volume is taller/wider than a thin mesh without scaling the mesh. Use Recapture Collider Base after you resize the collider in the editor.")]
        [SerializeField] private bool inflateBoxColliderOnAwake = true;

        [Tooltip("Added to captured BoxCollider size (Y is split above/below center). Ignored if zero.")]
        [SerializeField] private Vector3 boxColliderInflate = new Vector3(0.12f, 0.55f, 0.12f);

        [SerializeField] private bool _boxBaseCaptured;
        [SerializeField] private Vector3 _storedBaseBoxSize;
        [SerializeField] private Vector3 _storedBaseBoxCenter;

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

        /// <summary>Unity calls this when the component is first added — use BothRealms so standing plates work in the physical world (base default is SoulOnly).</summary>
        private void Reset()
        {
            realmMode = PuzzleRealmMode.BothRealms;
        }

        private void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;

            PuzzleBoxColliderInflate.ApplyIfNeeded(col, inflateBoxColliderOnAwake, boxColliderInflate,
                ref _boxBaseCaptured, ref _storedBaseBoxSize, ref _storedBaseBoxCenter);

            if (plateRenderer == null && plateVisual != null)
                plateRenderer = plateVisual.GetComponent<Renderer>();

            if (useColorTint && plateRenderer != null)
                ApplyVisual(false);
        }

        /// <summary>Call after manually editing the BoxCollider size in the inspector so the next inflate uses your new footprint.</summary>
        [ContextMenu("Recapture collider base (after resizing BoxCollider)")]
        private void RecaptureColliderBase()
        {
            if (!TryGetComponent<BoxCollider>(out var box))
                return;
            _storedBaseBoxSize = box.size;
            _storedBaseBoxCenter = box.center;
            _boxBaseCaptured = true;
            PuzzleBoxColliderInflate.ApplyIfNeeded(box, inflateBoxColliderOnAwake, boxColliderInflate,
                ref _boxBaseCaptured, ref _storedBaseBoxSize, ref _storedBaseBoxCenter);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsAccessibleInCurrentRealm()) return;
            if (!MatchesTag(other)) return;
            if (!CountsActivatorForRealm(other)) return;

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
            if (!CountsActivatorForRealm(other)) return;

            _overlapCount = Mathf.Max(0, _overlapCount - 1);
            if (_overlapCount == 0)
            {
                SetActivated(false);
                ApplyVisual(false);
            }
        }

        /// <summary>
        /// CharacterControllers can miss thin triggers for a frame (or longer). Stay recovers missed Enter.
        /// Assumes a single overlapping activator for this recovery path (typical single-player).
        /// </summary>
        private void OnTriggerStay(Collider other)
        {
            if (!IsAccessibleInCurrentRealm()) return;
            if (!MatchesTag(other)) return;
            if (!CountsActivatorForRealm(other)) return;
            if (IsActivated)
                return;

            _overlapCount = 1;
            SetActivated(true);
            ApplyVisual(true);
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
            Vector3 halfExtents = b.extents + overlapQueryPadding;
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
            return TransformHasTag(other.transform, activatorTag) ||
                   (!string.IsNullOrEmpty(secondaryTag) && TransformHasTag(other.transform, secondaryTag));
        }

        private static bool TransformHasTag(Transform t, string tag)
        {
            if (string.IsNullOrEmpty(tag)) return false;
            for (; t != null; t = t.parent)
            {
                if (t.CompareTag(tag))
                    return true;
            }

            return false;
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
