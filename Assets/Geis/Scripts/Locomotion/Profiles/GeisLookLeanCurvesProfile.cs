using UnityEngine;

namespace Geis.Locomotion
{
    [CreateAssetMenu(fileName = "LookLeanCurvesProfile", menuName = "Geis/Locomotion/Look & Lean Curves Profile")]
    public sealed class GeisLookLeanCurvesProfile : ScriptableObject
    {
        [Header("Head look")]
        public bool enableHeadTurn = true;
        public float headLookDelay;
        public AnimationCurve headLookXCurve;
        [Tooltip("Degrees beyond which head look clamps; character may rotate in place instead.")]
        public float headLookLimitDegrees = GeisLocomotionTuningDefaults.HeadLookLimitDegrees;

        [Header("Body look")]
        public bool enableBodyTurn = true;
        public float bodyLookDelay;
        public AnimationCurve bodyLookXCurve;

        [Header("Lean")]
        public bool enableLean = true;
        public float leanDelay;
        public AnimationCurve leanCurve;
        [Tooltip("Delay before lean/head looks sync after transitions.")]
        public float leansHeadLooksDelay;
    }
}
