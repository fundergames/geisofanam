using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RogueDeal.Boss
{
    /// <summary>
    /// Drives humanoid hand IK toward each <see cref="BossPart"/> (slam anchor / hit volume).
    ///
    /// <b>Setup:</b> Put this component on the <b>same GameObject</b> as the <b>Humanoid</b> Animator
    /// that drives the skinned mesh (e.g. <c>Boss_Mesh</c>). Unity only invokes <see cref="OnAnimatorIK"/>
    /// on that object. A separate Animator on the parent for slam-only clips will not receive IK for this rig.
    /// Enable <b>IK Pass</b> on the layers of <b>this</b> Animator Controller (the idle/rig controller).
    ///
    /// Do not parent skinned fist geometry to BossPart — IK moves the hand bones.
    ///
    /// <b>Edit Mode:</b> Enable <see cref="previewTwoBoneIkInEditMode"/> to draw an analytic two-bone arm chain
    /// to each slam anchor (Unity does not run <see cref="OnAnimatorIK"/> while not playing).
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class GiantBossSlamIk : MonoBehaviour
    {
        [Tooltip("Must live on **this** GameObject. If empty, uses GetComponent<Animator>() here. " +
                 "Use the Humanoid Animator for the colossus mesh — not a separate slam Animator on the parent.")]
        [SerializeField] private Animator animator;

        [Tooltip("If set and hand parts below are empty, RightHandPart / LeftHandPart are read from here (can be on a parent).")]
        [SerializeField] private GiantBossController giantBossController;

        [Header("Slam anchors (BossPart transforms)")]
        [SerializeField] private BossPart rightHandPart;
        [SerializeField] private BossPart leftHandPart;

        [Header("Elbow hints (recommended)")]
        [Tooltip("Place slightly in front / to the side of the body so the solver bends the arm naturally.")]
        [SerializeField] private Transform rightElbowHint;
        [SerializeField] private Transform leftElbowHint;

        [Tooltip("IK blend while the hand is in an active slam phase.")]
        [Range(0f, 1f)]
        [SerializeField] private float slamIkWeight = 1f;

        [Header("Debug")]
        [SerializeField] private bool warnIfAnchorIsUnderHandBone = true;

        [Tooltip("Draw slam anchors, hand bones, elbow hints, and IK lines in the Scene view (Play Mode).")]
        [SerializeField] private bool debugDrawGizmos;

        [Tooltip("If on, gizmos only when this object is selected (reduces clutter).")]
        [SerializeField] private bool debugGizmosOnlyWhenSelected;

        [Tooltip("Log animator / BossPart resolution once on Awake.")]
        [SerializeField] private bool debugLogSetupOnAwake;

        [Tooltip("Log when R/L IK weight crosses ~0 (BossPart state drives IK).")]
        [SerializeField] private bool debugLogIkWeightChanges;

        [Tooltip("World-space offset for debug labels (avoids z-fighting with the mesh).")]
        [SerializeField] private Vector3 debugLabelOffset = new Vector3(0f, 0.15f, 0f);

        [Header("Edit Mode — IK preview")]
        [Tooltip("While not in Play Mode, draw a two-bone arm solve (shoulder → elbow → hand) to each BossPart anchor. " +
                 "Uses bone lengths from the current pose. Approximates runtime humanoid IK for layout; not identical to Play Mode. " +
                 "BossPart refs are resolved from GiantBossController on a parent in Edit Mode (Awake does not run).")]
        [SerializeField] private bool previewTwoBoneIkInEditMode = true;

        [Tooltip("Multiply edit-mode preview sphere / line thickness (boss scale varies).")]
        [SerializeField] private float editModePreviewGizmoScale = 1f;

        private HumanBodyBones _rightHandBone = HumanBodyBones.RightHand;
        private HumanBodyBones _leftHandBone = HumanBodyBones.LeftHand;

        private float _dbgRightIkWeight;
        private float _dbgLeftIkWeight;
        private float _lastLoggedRightIk = -1f;
        private float _lastLoggedLeftIk = -1f;

        private void Awake()
        {
            if (!Application.isPlaying)
                return;

            ResolveAnimatorAndBossParts();
        }

        private void OnEnable()
        {
            // Edit Mode: cache BossPart refs from parent controller (Awake does not run).
            if (!Application.isPlaying)
                ResolveAnimatorAndBossParts();
        }

        /// <summary>
        /// Fills animator / giantBossController / hand parts the same way as Awake, without disabling the component.
        /// Safe in Edit Mode for gizmos and inspector.
        /// </summary>
        private void ResolveAnimatorAndBossParts()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            if (Application.isPlaying && animator == null)
            {
                Debug.LogError(
                    "[GiantBossSlamIk] No Animator on this GameObject. Move this component onto the boss mesh " +
                    "object that has the Humanoid Animator (e.g. Boss_Mesh), or add an Animator here.",
                    this);
                enabled = false;
                return;
            }

            if (Application.isPlaying && animator != null && animator.transform != transform)
            {
                Debug.LogError(
                    "[GiantBossSlamIk] Assigned Animator must be on this same GameObject. OnAnimatorIK only runs there.",
                    animator);
                enabled = false;
                return;
            }

            if (Application.isPlaying && animator != null && !animator.isHuman)
            {
                Debug.LogWarning(
                    "[GiantBossSlamIk] Animator is not Humanoid — Unity hand IK will not work. " +
                    "Use the skinned mesh's Humanoid Animator on this object.",
                    animator);
            }

            if (giantBossController == null)
                giantBossController = GetComponentInParent<GiantBossController>();

            if (rightHandPart == null && giantBossController != null)
                rightHandPart = giantBossController.RightHandPart;
            if (leftHandPart == null && giantBossController != null)
                leftHandPart = giantBossController.LeftHandPart;

            if (!Application.isPlaying)
                return;

            ValidateAnchorsNotUnderHandBones();

            if (debugLogSetupOnAwake)
                LogSetupDebug();
        }

        private void LogSetupDebug()
        {
            var ctrl = animator.runtimeAnimatorController != null
                ? animator.runtimeAnimatorController.name
                : "(none)";

            Debug.Log(
                "[GiantBossSlamIk] Setup — " +
                $"gameObject={name}, animator={animator.name}, humanoid={animator.isHuman}, " +
                $"controller={ctrl}, ikPass=enable on layers in Animator window\n" +
                $"  rightHandPart={(rightHandPart != null ? rightHandPart.name : "null")} " +
                $"state={(rightHandPart != null ? rightHandPart.State.ToString() : "-")}\n" +
                $"  leftHandPart={(leftHandPart != null ? leftHandPart.name : "null")} " +
                $"state={(leftHandPart != null ? leftHandPart.State.ToString() : "-")}\n" +
                $"  giantBossController={(giantBossController != null ? giantBossController.name : "null")}",
                this);
        }

        private void ValidateAnchorsNotUnderHandBones()
        {
            if (!warnIfAnchorIsUnderHandBone || animator == null || !animator.isHuman)
                return;

            WarnIfUnderHand(rightHandPart, _rightHandBone, "Right");
            WarnIfUnderHand(leftHandPart, _leftHandBone, "Left");
        }

        private void WarnIfUnderHand(BossPart part, HumanBodyBones handBone, string label)
        {
            if (part == null)
                return;

            Transform hand = animator.GetBoneTransform(handBone);
            if (hand == null)
                return;

            if (part.transform == hand || part.transform.IsChildOf(hand))
            {
                Debug.LogWarning(
                    $"[GiantBossSlamIk] {label} BossPart is under the {handBone} bone. " +
                    "Move the slam anchor out of the hand hierarchy (e.g. under the boss root) " +
                    "and drive its world position for the slam. IK targets the hand — it cannot " +
                    "be the same transform as the hand bone.",
                    part);
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null || !isActiveAndEnabled)
                return;

            _dbgRightIkWeight = ApplyHandIk(AvatarIKGoal.RightHand, AvatarIKHint.RightElbow, rightElbowHint, rightHandPart);
            _dbgLeftIkWeight = ApplyHandIk(AvatarIKGoal.LeftHand, AvatarIKHint.LeftElbow, leftElbowHint, leftHandPart);

            if (debugLogIkWeightChanges)
                MaybeLogIkWeightChange();
        }

        private void MaybeLogIkWeightChange()
        {
            const float eps = 0.02f;
            if (_lastLoggedRightIk < 0f)
                _lastLoggedRightIk = _dbgRightIkWeight;
            if (_lastLoggedLeftIk < 0f)
                _lastLoggedLeftIk = _dbgLeftIkWeight;

            if (Mathf.Abs(_dbgRightIkWeight - _lastLoggedRightIk) > eps)
            {
                _lastLoggedRightIk = _dbgRightIkWeight;
                Debug.Log(
                    $"[GiantBossSlamIk] Right IK weight={_dbgRightIkWeight:F2} " +
                    $"(BossPart={(rightHandPart != null ? rightHandPart.State.ToString() : "null")})",
                    rightHandPart != null ? rightHandPart.gameObject : this);
            }

            if (Mathf.Abs(_dbgLeftIkWeight - _lastLoggedLeftIk) > eps)
            {
                _lastLoggedLeftIk = _dbgLeftIkWeight;
                Debug.Log(
                    $"[GiantBossSlamIk] Left IK weight={_dbgLeftIkWeight:F2} " +
                    $"(BossPart={(leftHandPart != null ? leftHandPart.State.ToString() : "null")})",
                    leftHandPart != null ? leftHandPart.gameObject : this);
            }
        }

        /// <summary>Returns the IK weight applied this frame (for debug).</summary>
        private float ApplyHandIk(
            AvatarIKGoal goal,
            AvatarIKHint elbowHint,
            Transform elbowHintTransform,
            BossPart part)
        {
            float w = GetIkWeight(part);
            animator.SetIKPositionWeight(goal, w);
            animator.SetIKRotationWeight(goal, w);

            if (w <= 0.0001f || part == null)
            {
                animator.SetIKHintPositionWeight(elbowHint, 0f);
                return 0f;
            }

            Transform t = part.transform;
            animator.SetIKPosition(goal, t.position);
            animator.SetIKRotation(goal, t.rotation);

            if (elbowHintTransform != null)
            {
                animator.SetIKHintPositionWeight(elbowHint, w);
                animator.SetIKHintPosition(elbowHint, elbowHintTransform.position);
            }
            else
            {
                animator.SetIKHintPositionWeight(elbowHint, 0f);
            }

            return w;
        }

        private float GetIkWeight(BossPart part)
        {
            if (part == null || slamIkWeight <= 0f)
                return 0f;

            switch (part.State)
            {
                case BossPartState.Slamming:
                case BossPartState.Grounded:
                case BossPartState.Shielded:
                case BossPartState.Pinned:
                    return slamIkWeight;
                default:
                    return 0f;
            }
        }

        private void OnDrawGizmos()
        {
            DrawEditModePreviewIfNeeded();
            if (!debugDrawGizmos || debugGizmosOnlyWhenSelected)
                return;
            DrawIkDebugGizmosPlayMode();
        }

        private void OnDrawGizmosSelected()
        {
            DrawEditModePreviewIfNeeded();
            if (!debugDrawGizmos || !debugGizmosOnlyWhenSelected)
                return;
            DrawIkDebugGizmosPlayMode();
        }

        private void DrawEditModePreviewIfNeeded()
        {
#if UNITY_EDITOR
            if (Application.isPlaying || !previewTwoBoneIkInEditMode)
                return;

            // Ensure refs populated for Edit Mode (serialized BossPart slots are often empty until Awake).
            ResolveAnimatorAndBossParts();

            Animator a = animator != null ? animator : GetComponent<Animator>();
            if (a == null)
            {
                DrawEditModeSetupHint("No Animator on this GameObject.");
                return;
            }

            if (!a.isHuman)
            {
                DrawEditModeSetupHint("Animator needs a Humanoid avatar for IK preview.");
                return;
            }

            BossPart r = rightHandPart;
            BossPart l = leftHandPart;
            if (r == null && giantBossController != null)
                r = giantBossController.RightHandPart;
            if (l == null && giantBossController != null)
                l = giantBossController.LeftHandPart;

            if (r == null && l == null)
            {
                DrawEditModeSetupHint(
                    "No BossPart refs. Assign Right/Left Hand Part,\n" +
                    "or add GiantBossController on a parent with hands wired.");
                return;
            }

            DrawEditModeTwoBonePreview(a, r, l);
#endif
        }

#if UNITY_EDITOR
        private void DrawEditModeSetupHint(string message, Vector3 worldPosition)
        {
            float sz = HandleUtility.GetHandleSize(worldPosition) * 0.12f * Mathf.Max(0.25f, editModePreviewGizmoScale);
            Gizmos.color = new Color(1f, 0.92f, 0.16f, 0.95f);
            Gizmos.DrawWireSphere(worldPosition, sz);
            Handles.color = new Color(1f, 0.85f, 0.2f);
            Handles.Label(worldPosition + Vector3.up * sz * 2f, "GiantBossSlamIk (edit preview)\n" + message);
        }

        private void DrawEditModeSetupHint(string message)
        {
            DrawEditModeSetupHint(message, transform.position);
        }
#endif

        private void DrawIkDebugGizmosPlayMode()
        {
            if (!Application.isPlaying)
                return;

            Animator a = animator != null ? animator : GetComponent<Animator>();
            if (a == null || !a.isHuman)
                return;

            DrawHandDebug(a, "R", _rightHandBone, rightHandPart, rightElbowHint, _dbgRightIkWeight, new Color(1f, 0.45f, 0.15f));
            DrawHandDebug(a, "L", _leftHandBone, leftHandPart, leftElbowHint, _dbgLeftIkWeight, new Color(0.25f, 0.75f, 1f));
        }

#if UNITY_EDITOR
        private void DrawEditModeTwoBonePreview(Animator a, BossPart rightPart, BossPart leftPart)
        {
            if (rightPart != null)
            {
                DrawEditModeArm(
                    a,
                    "R",
                    HumanBodyBones.RightUpperArm,
                    HumanBodyBones.RightLowerArm,
                    HumanBodyBones.RightHand,
                    rightPart,
                    rightElbowHint,
                    new Color(0.15f, 0.95f, 0.35f));
            }

            if (leftPart != null)
            {
                DrawEditModeArm(
                    a,
                    "L",
                    HumanBodyBones.LeftUpperArm,
                    HumanBodyBones.LeftLowerArm,
                    HumanBodyBones.LeftHand,
                    leftPart,
                    leftElbowHint,
                    new Color(0.25f, 0.75f, 1f));
            }
        }

        private void DrawEditModeArm(
            Animator a,
            string label,
            HumanBodyBones upperArm,
            HumanBodyBones lowerArm,
            HumanBodyBones handBone,
            BossPart part,
            Transform elbowHint,
            Color color)
        {
            Transform shoulder = a.GetBoneTransform(upperArm);
            Transform elbowTr = a.GetBoneTransform(lowerArm);
            Transform handTr = a.GetBoneTransform(handBone);
            if (shoulder == null || elbowTr == null || handTr == null || part == null)
            {
                if (part != null)
                {
                    DrawEditModeSetupHint(
                        $"{label}: missing {DescribeMissingBone(shoulder, elbowTr, handTr)} — check Humanoid avatar mapping.",
                        part.transform.position);
                }

                return;
            }

            float upperLen = Vector3.Distance(shoulder.position, elbowTr.position);
            float lowerLen = Vector3.Distance(elbowTr.position, handTr.position);
            if (upperLen < 1e-4f || lowerLen < 1e-4f)
                return;

            float g = HandleUtility.GetHandleSize(shoulder.position) * 0.08f * Mathf.Max(0.25f, editModePreviewGizmoScale);

            Vector3 target = part.transform.position;
            Vector3 pole = elbowHint != null ? elbowHint.position : elbowTr.position;

            if (!TrySolveTwoBoneIk(shoulder.position, target, pole, upperLen, lowerLen, out Vector3 solvedElbow))
                return;

            Gizmos.color = new Color(color.r, color.g, color.b, 0.9f);
            Gizmos.DrawLine(shoulder.position, solvedElbow);
            Gizmos.DrawLine(solvedElbow, target);
            Gizmos.DrawWireSphere(solvedElbow, g);
            Gizmos.DrawWireSphere(target, g * 0.65f);

            Handles.color = color;
            Handles.Label(
                solvedElbow + debugLabelOffset,
                $"{label} edit preview (two-bone)\nelbow (solved)");

            Gizmos.color = new Color(color.r, color.g, color.b, 0.35f);
            Gizmos.DrawLine(shoulder.position, elbowTr.position);
            Gizmos.DrawLine(elbowTr.position, handTr.position);
            Handles.Label(
                elbowTr.position + debugLabelOffset,
                $"{label} current pose\n(editor idle)");
        }

        private static string DescribeMissingBone(Transform shoulder, Transform elbowTr, Transform handTr)
        {
            if (shoulder == null && elbowTr == null && handTr == null)
                return "upper arm / lower arm / hand bones";
            if (shoulder == null)
                return "upper arm bone";
            if (elbowTr == null)
                return "lower arm bone";
            return "hand bone";
        }
#endif

        /// <summary>
        /// Two-bone reach: shoulder at <paramref name="root"/>, wrist at <paramref name="target"/>,
        /// bend plane passes through <paramref name="poleHint"/>.
        /// </summary>
        private static bool TrySolveTwoBoneIk(
            Vector3 root,
            Vector3 target,
            Vector3 poleHint,
            float upperLen,
            float lowerLen,
            out Vector3 elbow)
        {
            Vector3 toTarget = target - root;
            float c = toTarget.magnitude;
            float minReach = Mathf.Abs(upperLen - lowerLen) + 0.001f;
            float maxReach = upperLen + lowerLen - 0.001f;
            if (c < 0.0001f || minReach >= maxReach)
            {
                elbow = root;
                return false;
            }

            c = Mathf.Clamp(c, minReach, maxReach);
            Vector3 toDir = toTarget.normalized;

            float x = (upperLen * upperLen - lowerLen * lowerLen + c * c) / (2f * c);
            float hSq = upperLen * upperLen - x * x;
            float h = Mathf.Sqrt(Mathf.Max(0f, hSq));
            Vector3 mid = root + toDir * x;

            Vector3 planeNormal = Vector3.Cross(toDir, poleHint - root);
            if (planeNormal.sqrMagnitude < 1e-8f)
                planeNormal = Vector3.Cross(toDir, Vector3.up);
            planeNormal.Normalize();

            Vector3 perp = Vector3.Cross(toDir, planeNormal).normalized;
            Vector3 a = mid + perp * h;
            Vector3 b = mid - perp * h;
            elbow = Vector3.SqrMagnitude(a - poleHint) < Vector3.SqrMagnitude(b - poleHint) ? a : b;
            return true;
        }

        private void DrawHandDebug(
            Animator a,
            string label,
            HumanBodyBones handBone,
            BossPart part,
            Transform elbowHint,
            float ikWeight,
            Color color)
        {
            Transform hand = a.GetBoneTransform(handBone);
            if (part == null && hand == null)
                return;

            if (hand != null)
            {
                Gizmos.color = color;
                Gizmos.DrawWireSphere(hand.position, 0.08f);

#if UNITY_EDITOR
                Handles.color = color;
                Handles.Label(hand.position + debugLabelOffset, $"{label} bone\n{hand.name}");
#endif
            }

            if (part != null)
            {
                Vector3 anchorPos = part.transform.position;
                if (hand != null)
                {
                    Gizmos.color = new Color(color.r, color.g, color.b, 0.65f);
                    Gizmos.DrawLine(hand.position, anchorPos);
                }

                Gizmos.color = new Color(color.r, color.g, color.b, 0.95f);
                float r = 0.12f + ikWeight * 0.08f;
                Gizmos.DrawWireSphere(anchorPos, r);

#if UNITY_EDITOR
                Handles.color = color;
                Handles.Label(
                    anchorPos + debugLabelOffset + Vector3.up * 0.12f,
                    $"{label} anchor ({part.name})\nstate={part.State}\nIK w={ikWeight:F2}");
#endif
            }

            if (elbowHint != null)
            {
                Gizmos.color = new Color(color.r, color.g, color.b, 0.5f);
                Gizmos.DrawWireSphere(elbowHint.position, 0.06f);
#if UNITY_EDITOR
                Handles.Label(elbowHint.position + debugLabelOffset, $"{label} elbow hint");
#endif
            }
        }
    }
}
