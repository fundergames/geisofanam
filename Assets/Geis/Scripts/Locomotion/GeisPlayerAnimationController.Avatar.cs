/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using Geis.SoulRealm;
using UnityEngine;

namespace Geis.Locomotion
{
    /// <summary>
    /// Swaps the active CharacterController / Animator between physical body and soul-realm ghost
    /// so one <see cref="GeisPlayerAnimationController"/> drives both realms.
    /// </summary>
    public partial class GeisPlayerAnimationController
    {
        private Transform _avatarTransform;
        private CharacterController _avatarController;
        private Animator _avatarAnimator;

        /// <summary>True when locomotion is applied to the soul ghost rather than this component's transform.</summary>
        public bool IsSoulRealmAvatarActive =>
            _avatarTransform != null && _avatarTransform != transform;

        /// <summary>When true, this controller owns full locomotion/combat Update and animator presentation (spectral driver stands down).</summary>
        public bool IsDrivingSoulRealmLocomotion =>
            IsSoulRealmAvatarActive
            && SoulRealmManager.Instance != null
            && SoulRealmManager.Instance.IsSoulRealmActive
            && SoulRealmManager.Instance.AllowGhostMovement;

        private Transform LocomotionTransform => _avatarTransform != null ? _avatarTransform : transform;
        private CharacterController LocomotionController => _avatarController != null ? _avatarController : _controller;
        private Animator PresentationAnimator => _avatarAnimator != null ? _avatarAnimator : _animator;

        private void BindPhysicalLocomotionAvatar()
        {
            _avatarTransform = null;
            _avatarController = null;
            _avatarAnimator = null;
            RefreshAnimatorChildRigFlag();
        }

        /// <summary>Called by <see cref="Geis.SoulRealm.SoulRealmManager"/> on soul-realm entry.</summary>
        public void BindSoulRealmLocomotionAvatar(
            Transform ghostTransform,
            CharacterController ghostController,
            Animator spectralAnimator)
        {
            _avatarTransform = ghostTransform;
            _avatarController = ghostController;
            _avatarAnimator = spectralAnimator;
            RefreshAnimatorChildRigFlag();
            RefreshPresentationAnimatorCaches();
            SyncCapsuleToLocomotionController(_isCrouching);
            EnsurePresentationAnimatorAdvancing();
            ApplyComboOverridesIfReady();

            if (spectralAnimator == null)
            {
                Debug.LogWarning(
                    "[GeisPlayerAnimationController] Soul-realm locomotion bound without a spectral Animator; " +
                    "ghost movement may look frozen.",
                    this);
            }
        }

        /// <summary>Called by <see cref="SoulRealmManager"/> on soul-realm exit.</summary>
        public void ExitSoulRealmLocomotionAvatar()
        {
            BindPhysicalLocomotionAvatar();
            RefreshPresentationAnimatorCaches();
            SyncCapsuleToLocomotionController(_isCrouching);
            ApplyComboOverridesIfReady();
        }

        private void RefreshAnimatorChildRigFlag()
        {
            Animator anim = PresentationAnimator;
            _animatorIsOnChild = anim != null && anim.transform != transform;
        }

        /// <summary>
        /// <see cref="SoulSpectralAnimatorDriver"/> sets speed to 0 during enter/exit-hold freeze; restore when this controller drives the ghost.
        /// </summary>
        private void EnsurePresentationAnimatorAdvancing()
        {
            if (PresentationAnimator != null && PresentationAnimator.speed < 1f)
                PresentationAnimator.speed = 1f;
        }

        private void SyncCapsuleToLocomotionController(bool crouching)
        {
            if (LocomotionController == null)
                return;

            if (crouching)
            {
                LocomotionController.center = new Vector3(0f, _capsuleCrouchingCentre, 0f);
                LocomotionController.height = _capsuleCrouchingHeight;
            }
            else
            {
                LocomotionController.center = new Vector3(0f, _capsuleStandingCentre, 0f);
                LocomotionController.height = _capsuleStandingHeight;
            }
        }
    }
}
