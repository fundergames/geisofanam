using UnityEngine;

namespace Geis.Animation
{
    /// <summary>
    /// Design-time profile documenting animator contracts and optional reference controller for a rig (Synty, Polygon, etc.).
    /// </summary>
    [CreateAssetMenu(fileName = "RigAnimatorProfile", menuName = "Geis/Animation/Rig Animator Profile")]
    public sealed class RigAnimatorProfile : ScriptableObject
    {
        [Tooltip("Required parameters and setup notes for this Animator Controller.")]
        [TextArea(4, 12)]
        public string animatorContractNotes;

        [Tooltip("Optional: controller this profile was authored against.")]
        public RuntimeAnimatorController referenceRuntimeController;
    }
}
