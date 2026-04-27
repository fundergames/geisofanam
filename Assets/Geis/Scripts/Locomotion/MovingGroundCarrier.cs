/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 *
 * This software and associated documentation files are proprietary and confidential.
 * Unauthorized copying, modification, distribution, or use of this software,
 * via any medium, is strictly prohibited without explicit written permission.
 *
 * This code is provided for personal use only by authorized recipients.
 * It may not be redistributed, sublicensed, or sold in any form.
 */

using UnityEngine;

namespace Geis.Locomotion
{
    /// <summary>
    /// Marks a moving surface (e.g. kinematic puzzle platform) so grounded characters standing on it
    /// receive its motion via <see cref="GroundRideUtility"/>.
    /// </summary>
    public class MovingGroundCarrier : MonoBehaviour
    {
        [Tooltip("World-space transform used for ride delta; defaults to this object.")]
        [SerializeField] private Transform movingReference;

        public Transform MovingTransform => movingReference != null ? movingReference : transform;
    }
}
