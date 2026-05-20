/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using Geis.InputSystem;
using Geis.Locomotion;
using UnityEngine;

namespace Geis.SoulRealm
{
    /// <summary>
    /// Marker on the soul-realm ghost root (puzzles, boss triggers). Locomotion, combat, and dodge are owned by
    /// <see cref="GeisPlayerAnimationController"/> via <see cref="GeisPlayerAnimationController.BindSoulRealmLocomotionAvatar"/>.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class SoulGhostMotor : MonoBehaviour
    {
        [SerializeField] private GeisInputReader inputReader;

        private GeisPlayerAnimationController _bodyLocomotion;

        public void Configure(
            GeisInputReader reader,
            GeisPlayerAnimationController bodyLocomotion = null,
            GeisCameraController cameraController = null)
        {
            inputReader = reader;
            _bodyLocomotion = bodyLocomotion;
        }

        /// <summary>Kept for realm-entry call sites; velocity sync is handled by the body controller.</summary>
        public void SyncFromBodyForSoulRealm(GeisPlayerAnimationController body)
        {
            _bodyLocomotion = body ?? _bodyLocomotion;
        }

        /// <summary>Kept for realm-entry call sites after the ghost is teleported to the body pose.</summary>
        public void RefreshGroundedAfterSoulRealmTeleport()
        {
        }

        /// <summary>Horizontal movement speed (mirrors body <c>_speed2D</c>).</summary>
        public float MirrorSpeed2D
        {
            get
            {
                Vector3 planar = PlanarVelocity;
                float s = planar.magnitude;
                return Mathf.Round(s * 1000f) / 1000f;
            }
        }

        public Vector3 PlanarVelocity =>
            _bodyLocomotion != null ? _bodyLocomotion.LocomotionPlanarVelocity : Vector3.zero;

        public float VerticalVelocity =>
            _bodyLocomotion != null ? _bodyLocomotion.LocomotionVerticalVelocity : 0f;

        public bool IsGroundedPublic =>
            _bodyLocomotion != null && _bodyLocomotion.LocomotionIsGrounded;
    }
}
