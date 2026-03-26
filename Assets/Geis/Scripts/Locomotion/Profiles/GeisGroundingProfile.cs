using UnityEngine;

namespace Geis.Locomotion
{
    [CreateAssetMenu(fileName = "GroundingProfile", menuName = "Geis/Locomotion/Grounding Profile")]
    public sealed class GeisGroundingProfile : ScriptableObject
    {
        [Tooltip("Layers used for ground checks, incline rays, and ceiling checks.")]
        public LayerMask groundLayerMask = ~0;

        [Tooltip("Offset below character center for grounded sphere check.")]
        public float groundedOffset = GeisLocomotionTuningDefaults.GroundedOffset;
    }
}
