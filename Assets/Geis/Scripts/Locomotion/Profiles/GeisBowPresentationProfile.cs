/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using UnityEngine;

namespace Geis.Locomotion
{
    [CreateAssetMenu(fileName = "BowPresentationProfile", menuName = "Funder Games/Geis/Locomotion/Bow Presentation Profile")]
    public sealed class GeisBowPresentationProfile : ScriptableObject
    {
        [Tooltip("How quickly the Bow_Draw upper-body layer blends in/out when equipping or unequipping the bow.")]
        public float equipLayerBlendSpeed = GeisLocomotionTuningDefaults.BowEquipLayerBlendSpeed;

        [Tooltip("Local euler offset (degrees) applied after the bow base facing. While bow aiming/drawing, only Y (yaw) is applied to the root.")]
        public Vector3 aimBodyEulerOffset;
    }
}
