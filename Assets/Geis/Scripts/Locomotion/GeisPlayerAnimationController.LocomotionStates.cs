/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using Geis.Combat;
using Geis.InputSystem;
using Geis.InteractInput;
using Geis.Attributes;
using Geis.Animation;
using Geis.SoulRealm;
using RogueDeal.Combat;
using RogueDeal.Combat.Targeting;

namespace Geis.Locomotion
{
    public partial class GeisPlayerAnimationController
    {
        #region Shared State

        #region State Change

        /// <summary>
        ///     Switch the current state to the passed in state.
        /// </summary>
        /// <param name="newState">The state to switch to.</param>
        private void SwitchState(AnimationState newState)
        {
            ExitCurrentState();
            EnterState(newState);
        }

        /// <summary>
        ///     Enter the given state.
        /// </summary>
        /// <param name="stateToEnter">The state to enter.</param>
        private void EnterState(AnimationState stateToEnter)
        {
            _currentState = stateToEnter;
            SyncAnimatorApplyRootMotionForState();
            switch (_currentState)
            {
                case AnimationState.Base:
                    EnterBaseState();
                    break;
                case AnimationState.Locomotion:
                    EnterLocomotionState();
                    break;
                case AnimationState.Jump:
                    EnterJumpState();
                    break;
                case AnimationState.Fall:
                    EnterFallState();
                    break;
                case AnimationState.Crouch:
                    EnterCrouchState();
                    break;
                case AnimationState.Attack:
                    EnterAttackState();
                    break;
                case AnimationState.Dodge:
                    EnterDodgeState();
                    break;
            }
        }

        /// <summary>
        ///     Exit the current state.
        /// </summary>
        private void ExitCurrentState()
        {
            switch (_currentState)
            {
                case AnimationState.Locomotion:
                    ExitLocomotionState();
                    break;
                case AnimationState.Jump:
                    ExitJumpState();
                    break;
                case AnimationState.Crouch:
                    ExitCrouchState();
                    break;
                case AnimationState.Attack:
                    ExitAttackState();
                    break;
                case AnimationState.Dodge:
                    ExitDodgeState();
                    break;
            }
        }

        #endregion

        #region Updates

        /// <inheritdoc cref="Update" />
        private void Update()
        {
            if (SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive)
            {
                ApplyMovingGroundRideWhileBodySuppressed();
                SoulRealmManager.Instance.SyncSpectralAnimatorControllerFromBody();
                ApplyComboOverridesIfReady();
                UpdateBestTarget();
                UpdateLockOnAnchorPosition();

                if (!SoulRealmManager.Instance.AllowGhostMovement)
                {
                    ApplyBowParametersToAnimator();
                    return;
                }

                EnsurePresentationAnimatorAdvancing();
            }

            ApplyComboOverridesIfReady();

            switch (_currentState)
            {
                case AnimationState.Locomotion:
                    UpdateLocomotionState();
                    break;
                case AnimationState.Jump:
                    UpdateJumpState();
                    break;
                case AnimationState.Fall:
                    UpdateFallState();
                    break;
                case AnimationState.Crouch:
                    UpdateCrouchState();
                    break;
                case AnimationState.Attack:
                    UpdateAttackState();
                    break;
                case AnimationState.Dodge:
                    UpdateDodgeState();
                    break;
            }
        }

        private LocomotionPresentationSnapshot BuildLocomotionPresentationSnapshot()
        {
            bool animatorStrafe = UseStrafeStyleLocomotionFacing;
            if (_currentState == AnimationState.Dodge && _dodgePreserveStrafeFacing)
                animatorStrafe = true;

            return new LocomotionPresentationSnapshot
            {
                LeanValue = _leanValue,
                HeadLookX = _headLookX,
                HeadLookY = _headLookY,
                BodyLookX = _bodyLookX,
                BodyLookY = _bodyLookY,
                IsStrafingFloat = animatorStrafe ? 1f : 0f,
                InclineAngle = _inclineAngle,
                MoveSpeed2D = _speed2D,
                CurrentGait = (int)_currentGait,
                StrafeDirectionX = _strafeDirectionX,
                StrafeDirectionZ = _strafeDirectionZ,
                ForwardStrafe = _forwardStrafe,
                CameraRotationOffset = _cameraRotationOffset,
                MovementInputHeld = _movementInputHeld,
                MovementInputPressed = _movementInputPressed,
                MovementInputTapped = _movementInputTapped,
                ShuffleDirectionX = _shuffleDirectionX,
                ShuffleDirectionZ = _shuffleDirectionZ,
                IsTurningInPlace = _isTurningInPlace,
                IsCrouching = _isCrouching,
                FallingDuration = _fallingDuration,
                IsGrounded = _isGrounded,
                IsWalking = _isWalking,
                IsStopped = _isStopped,
                IsStarting = _isStarting,
                LocomotionStartDirection = _locomotionStartDirection
            };
        }

        private void ApplyBowParametersToAnimator()
        {
            if (PresentationAnimator == null)
                return;

            if (_animatorHasBowDrawing)
                PresentationAnimator.SetBool(_bowDrawingHash, _isBowDrawing);
            if (_animatorHasBowDrawCharge)
                PresentationAnimator.SetFloat(_bowDrawChargeHash, _bowDrawCharge);
            if (_animatorHasBowAiming)
            {
                bool bowAiming = IsBowEquipped && _isAiming;
                PresentationAnimator.SetBool(_bowAimingHash, bowAiming);
            }
            if (_animatorHasBowChargedShotReady)
                PresentationAnimator.SetBool(_bowChargedShotReadyHash, _isBowChargedShotReady);

            if (_bowDrawLayerIndex >= 0)
            {
                float targetBowLayerWeight = IsBowEquipped ? 1f : 0f;
                float blendSpeed = Mathf.Max(0f, _bowEquipLayerBlendSpeed);
                _currentBowDrawLayerWeight = blendSpeed > 0f
                    ? Mathf.MoveTowards(_currentBowDrawLayerWeight, targetBowLayerWeight, blendSpeed * Time.deltaTime)
                    : targetBowLayerWeight;
                PresentationAnimator.SetLayerWeight(_bowDrawLayerIndex, _currentBowDrawLayerWeight);
            }
        }

        /// <summary>
        ///     Updates the animator to have the latest values.
        /// </summary>
        private void UpdateAnimatorController()
        {
            if (PresentationAnimator == null)
                return;

            bool airGait = LocomotionAirGaitForAnimator;
            var snap = BuildLocomotionPresentationSnapshot();
            var ctx = new LocomotionApplyContext
            {
                AirGaitForAnimator = airGait,
                HasFallingBlendParameter = _hasFallingBlendParameter,
                FallingBlendValue = airGait ? GetFallingBlendParameter(_fallingDuration) : 0f,
                SetIsJumping = false
            };

            LocomotionAnimatorApplier.ApplySyntyLocomotion(PresentationAnimator, snap, ctx);
            ApplyBowParametersToAnimator();
        }

        #endregion

        #endregion

        #region Base State

        #region Setup

        /// <summary>
        ///     Performs the actions required when entering the base state.
        /// </summary>
        private void EnterBaseState()
        {
            _previousRotation = LocomotionTransform.forward;
        }

        /// <summary>
        ///     Calculates the input type and sets the required internal states.
        /// </summary>
        private void CalculateInput()
        {
            bool moveFrozen = GeisInteractInput.IsMovementFrozenForInteraction;
            bool movementDetected = !moveFrozen && _inputReader._movementInputDetected;

            if (movementDetected)
            {
                if (_inputReader._movementInputDuration == 0)
                {
                    _movementInputTapped = true;
                }
                else if (_inputReader._movementInputDuration > 0 && _inputReader._movementInputDuration < _buttonHoldThreshold)
                {
                    _movementInputTapped = false;
                    _movementInputPressed = true;
                    _movementInputHeld = false;
                }
                else
                {
                    _movementInputTapped = false;
                    _movementInputPressed = false;
                    _movementInputHeld = true;
                }

                _inputReader._movementInputDuration += Time.deltaTime;
            }
            else
            {
                _inputReader._movementInputDuration = 0;
                _movementInputTapped = false;
                _movementInputPressed = false;
                _movementInputHeld = false;
            }

            Vector2 composite = GeisInteractInput.GetEffectiveMoveCompositeForLocomotion(_inputReader._moveComposite);
            _moveDirection = GeisLocomotionKinematics.ComputeCameraRelativeMoveDirection(composite, _cameraController);
        }

        #endregion

        #region Movement

        /// <summary>
        ///     Performs the movement of the player
        /// </summary>
        private void Move()
        {
            LayerMask rideMask = _groundLayerMask.value != 0 ? _groundLayerMask : (LayerMask)(-1);
            Vector3 groundRide = GroundRideUtility.GetRideDelta(
                LocomotionTransform, LocomotionController, rideMask, _groundedOffset,
                ref _groundRideSurface, ref _groundRideLastWorldPos, _isGrounded);

            LocomotionController.Move(groundRide + _velocity * Time.deltaTime);

            if (_isLockedOn && _targetLockOnPos != null && _currentLockOnTarget != null)
            {
                _targetLockOnPos.position = ResolveLockOnWorldPosition(_currentLockOnTarget);
                _lockOnIndicator?.SetAnchorTarget(_targetLockOnPos);
            }
            else
            {
                _lockOnIndicator?.ClearTarget();
            }
        }

        /// <summary>
        /// Soul realm suppresses full locomotion <see cref="Update"/>, but the body must still follow
        /// kinematic floors (see <see cref="GroundRideUtility"/>).
        /// </summary>
        private void ApplyMovingGroundRideWhileBodySuppressed()
        {
            if (_controller == null)
                return;

            GroundedCheck();
            LayerMask rideMask = _groundLayerMask.value != 0 ? _groundLayerMask : (LayerMask)(-1);
            Vector3 groundRide = GroundRideUtility.GetRideDelta(
                transform, _controller, rideMask, _groundedOffset,
                ref _groundRideSurface, ref _groundRideLastWorldPos, _isGrounded);
            if (groundRide.sqrMagnitude > 1e-12f)
                _controller.Move(groundRide);
        }

        /// <summary>
        ///     Applies gravity to the player.
        /// </summary>
        private void ApplyGravity()
        {
            if (_velocity.y > Physics.gravity.y)
            {
                _velocity.y += Physics.gravity.y * _gravityMultiplier * Time.deltaTime;
            }
        }

        /// <summary>
        ///     Calculates the movement direction of the player, and sets the relevant flags.
        /// </summary>
        private void CalculateMoveDirection()
        {
            CalculateInput();

            _targetMaxSpeed = GeisLocomotionKinematics.ResolveTargetMaxSpeed(
                _isGrounded,
                IsBowMovementForcedWalk,
                _isCrouching,
                _isSprinting,
                _isWalking,
                _walkSpeed,
                _runSpeed,
                _sprintSpeed,
                _currentMaxSpeed);

            GeisLocomotionKinematics.StepBodyPlanarVelocity(
                ref _currentMaxSpeed,
                ref _velocity.x,
                ref _velocity.z,
                _moveDirection,
                _targetMaxSpeed,
                _ANIMATION_DAMP_TIME,
                _sprintInstantFraction,
                _accelRate,
                _decelRate,
                _speedChangeDamping,
                Time.deltaTime);

            _targetVelocity.x = _moveDirection.x * _currentMaxSpeed;
            _targetVelocity.z = _moveDirection.z * _currentMaxSpeed;

            _speed2D = GeisLocomotionKinematics.RoundPlanarSpeed2D(_velocity);

            Vector3 playerForwardVector = LocomotionTransform.forward;

            _newDirectionDifferenceAngle = playerForwardVector != _moveDirection
                ? Vector3.SignedAngle(playerForwardVector, _moveDirection, Vector3.up)
                : 0f;

            CalculateGait();
        }

        /// <summary>
        ///     <pre>
        ///         Calculates the character gait.
        ///         Calculate what the current locomotion gait is (Walk, Run, Sprint)
        ///         (for use in jumps, landings etc when deciding which animation to use)
        ///         Gait values will be:
        ///         Idle = 0, Walk = 1, Run = 2, Sprint = 3
        ///     </pre>
        /// </summary>
        private void CalculateGait()
        {
            float runThreshold = (_walkSpeed + _runSpeed) / 2;
            float sprintThreshold = (_runSpeed + _sprintSpeed) / 2;

            if (_speed2D < 0.01)
            {
                _currentGait = GaitState.Idle;
            }
            else if (IsBowMovementForcedWalk)
            {
                _currentGait = GaitState.Walk;
            }
            else if (_speed2D < runThreshold)
            {
                _currentGait = GaitState.Walk;
            }
            else if (_speed2D < sprintThreshold)
            {
                _currentGait = GaitState.Run;
            }
            else
            {
                _currentGait = GaitState.Sprint;
            }
        }

        /// <summary>
        ///     Calculates the face move direction based on the locomotion of the character.
        /// </summary>
        private void FaceMoveDirection()
        {
            Vector3 characterForward = new Vector3(LocomotionTransform.forward.x, 0f, LocomotionTransform.forward.z).normalized;
            Vector3 characterRight = new Vector3(LocomotionTransform.right.x, 0f, LocomotionTransform.right.z).normalized;
            Vector3 directionForward = new Vector3(_moveDirection.x, 0f, _moveDirection.z).normalized;

            _cameraForward = _cameraController.GetCameraForwardZeroedYNormalised();
            Quaternion strafingTargetRotation = GetBowFacingQuaternion();

            _strafeAngle = characterForward != directionForward ? Vector3.SignedAngle(characterForward, directionForward, Vector3.up) : 0f;

            _isTurningInPlace = false;
            _isIdleLooking = false;

            if (UseStrafeStyleLocomotionFacing)
            {
                if (_moveDirection.magnitude > 0.01)
                {
                    if (_cameraForward != Vector3.zero)
                    {
                        // Shuffle direction values - these are separate from the strafe values as we don't want to lerp, we need to know immediately
                        // what direction to shuffle, and then lock the value so it doesn't return to zero once we lose input (so the blend tree works
                        // to the end of the anim clip)
                        _shuffleDirectionZ = Vector3.Dot(characterForward, directionForward);
                        _shuffleDirectionX = Vector3.Dot(characterRight, directionForward);

                        UpdateStrafeDirection(
                            Vector3.Dot(characterForward, directionForward),
                            Vector3.Dot(characterRight, directionForward)
                        );
                        _cameraRotationOffset = Mathf.Lerp(_cameraRotationOffset, 0f, _rotationSmoothing * Time.deltaTime);

                        float targetValue = _strafeAngle > _forwardStrafeMinThreshold && _strafeAngle < _forwardStrafeMaxThreshold ? 1f : 0f;

                        if (Mathf.Abs(_forwardStrafe - targetValue) <= 0.001f)
                        {
                            _forwardStrafe = targetValue;
                        }
                        else
                        {
                            float t = Mathf.Clamp01(_STRAFE_DIRECTION_DAMP_TIME * Time.deltaTime);
                            _forwardStrafe = Mathf.SmoothStep(_forwardStrafe, targetValue, t);
                        }
                    }

                    // Strafe body faces camera forward. While aiming, that forward is updated only by look (right stick / mouse) via GeisCameraController —
                    // left stick affects movement & strafe blend trees only, not yaw rotation.
                    LocomotionTransform.rotation = RotateTowardsClamped(LocomotionTransform.rotation, strafingTargetRotation);
                }
                else if (_isAiming && (_cameraForward != Vector3.zero || Mathf.Abs(_cameraController.GetCameraForward().y) > 0.001f))
                {
                    // Aim idle: face camera (look-driven yaw/pitch for bow). Horizontal projection can be ~0 when looking straight up/down.
                    LocomotionTransform.rotation = RotateTowardsClamped(LocomotionTransform.rotation, GetBowFacingQuaternion());
                    _isIdleLooking = true;
                    UpdateStrafeDirection(1f, 0f);
                    _shuffleDirectionZ = 1;
                    _shuffleDirectionX = 0;
                    _cameraRotationOffset = Mathf.Lerp(_cameraRotationOffset, 0f, _rotationSmoothing * Time.deltaTime);
                }
                else
                {
                    // Idle: look only, no shuffle, no rotation
                    _isIdleLooking = true;
                    UpdateStrafeDirection(1f, 0f);
                    _shuffleDirectionZ = 1;
                    _shuffleDirectionX = 0;

                    float t = 20 * Time.deltaTime;
                    float newOffset = 0f;

                    if (characterForward != _cameraForward)
                    {
                        newOffset = Vector3.SignedAngle(characterForward, _cameraForward, Vector3.up);
                    }

                    _cameraRotationOffset = Mathf.Lerp(_cameraRotationOffset, newOffset, t);
                }
            }
            else
            {
                UpdateStrafeDirection(1f, 0f);
                _cameraRotationOffset = Mathf.Lerp(_cameraRotationOffset, 0f, _rotationSmoothing * Time.deltaTime);

                _shuffleDirectionZ = 1;
                _shuffleDirectionX = 0;

                Vector3 faceDirection = new Vector3(_velocity.x, 0f, _velocity.z);

                if (faceDirection == Vector3.zero)
                {
                    return;
                }

                LocomotionTransform.rotation = RotateTowardsClamped(LocomotionTransform.rotation, Quaternion.LookRotation(faceDirection));
            }
        }

        /// <summary>
        /// Exponentially smooths toward <paramref name="target"/> but caps the per-frame angular step to
        /// <see cref="_maxTurnDegPerSecond"/> so large redirects (e.g. 180° pivots) finish in bounded time
        /// instead of asymptotically creeping the last few degrees.
        /// </summary>
        private Quaternion RotateTowardsClamped(Quaternion current, Quaternion target)
        {
            Quaternion smoothed = Quaternion.Slerp(current, target,
                1f - Mathf.Exp(-_rotationSmoothing * Time.deltaTime));
            if (_maxTurnDegPerSecond <= 0f)
                return smoothed;
            return Quaternion.RotateTowards(current, target,
                Mathf.Max(_maxTurnDegPerSecond * Time.deltaTime,
                    Quaternion.Angle(current, smoothed)));
        }

        /// <summary>
        /// Bow aim root rotation uses only planar camera yaw. Up/down bow aim should come from the upper-body layer,
        /// not by pitching the whole character transform.
        /// </summary>
        private Quaternion GetBowFacingQuaternion()
        {
            Vector3 camFwdFull = _cameraController.GetCameraForward();
            if (camFwdFull.sqrMagnitude < 1e-8f)
                return LocomotionTransform.rotation;

            bool bowAimStance = IsBowEquipped && (_isAiming || _isBowDrawing);

            if (bowAimStance)
            {
                Vector3 camPlanar = camFwdFull;
                camPlanar.y = 0f;
                if (camPlanar.sqrMagnitude < 1e-8f)
                    return LocomotionTransform.rotation;

                Quaternion q = Quaternion.LookRotation(camPlanar.normalized, Vector3.up)
                    * Quaternion.Euler(0f, BowAimRootYawOffsetDegrees, 0f);
                q *= Quaternion.Euler(0f, _bowAimBodyEulerOffset.y, 0f);
                return q;
            }

            if (_cameraForward.sqrMagnitude < 1e-8f)
                return LocomotionTransform.rotation;

            return Quaternion.LookRotation(_cameraForward);
        }

        /// <summary>
        ///     Checks if the player has stopped moving.
        /// </summary>
        private void CheckIfStopped()
        {
            _isStopped = _moveDirection.magnitude == 0 && _speed2D < .5;
        }

        /// <summary>
        ///     Checks if the player has started moving.
        /// </summary>
        private void CheckIfStarting()
        {
            _locomotionStartTimer = VariableOverrideDelayTimer(_locomotionStartTimer);

            bool isStartingCheck = false;

            if (_locomotionStartTimer <= 0.0f)
            {
                if (_moveDirection.magnitude > 0.01 && _speed2D < 1 && !_isStrafing)
                {
                    isStartingCheck = true;
                }

                if (isStartingCheck)
                {
                    if (!_isStarting)
                    {
                        _locomotionStartDirection = _newDirectionDifferenceAngle;
                        PresentationAnimator.SetFloat(_locomotionStartDirectionHash, _locomotionStartDirection);
                    }

                    float delayTime = 0.2f;
                    _leanDelay = delayTime;
                    _headLookDelay = delayTime;
                    _bodyLookDelay = delayTime;

                    _locomotionStartTimer = delayTime;
                }
            }
            else
            {
                isStartingCheck = true;
            }

            _isStarting = isStartingCheck;
            PresentationAnimator.SetBool(_isStartingHash, _isStarting);
        }

        /// <summary>
        ///     Updates the strafe direction variables to those provided.
        /// </summary>
        /// <param name="TargetZ">The value to set for Z axis.</param>
        /// <param name="TargetX">The value to set for X axis.</param>
        private void UpdateStrafeDirection(float TargetZ, float TargetX)
        {
            _strafeDirectionZ = Mathf.Lerp(_strafeDirectionZ, TargetZ, _ANIMATION_DAMP_TIME * Time.deltaTime);
            _strafeDirectionX = Mathf.Lerp(_strafeDirectionX, TargetX, _ANIMATION_DAMP_TIME * Time.deltaTime);
            _strafeDirectionZ = Mathf.Round(_strafeDirectionZ * 1000f) / 1000f;
            _strafeDirectionX = Mathf.Round(_strafeDirectionX * 1000f) / 1000f;
        }

        #endregion

        #region Ground Checks

        /// <summary>
        ///     Checks if the character is grounded.
        /// </summary>
        private void GroundedCheck()
        {
            // Use bottom of CharacterController capsule (center - height/2) plus grounded offset for tolerance
            float sphereY = LocomotionController.transform.position.y + LocomotionController.center.y - (LocomotionController.height * 0.5f) - _groundedOffset;
            Vector3 spherePosition = new Vector3(
                LocomotionController.transform.position.x,
                sphereY,
                LocomotionController.transform.position.z
            );
            // Fallback: if layer mask is "Nothing" (0), use all layers so ground is detected
            LayerMask mask = _groundLayerMask.value != 0 ? _groundLayerMask : (LayerMask)(-1);
            _isGrounded = Physics.CheckSphere(spherePosition, LocomotionController.radius, mask, QueryTriggerInteraction.Ignore);

            if (_isGrounded)
            {
                GroundInclineCheck();
            }
        }

        /// <summary>
        ///     Checks for ground incline and sets the required variables.
        /// </summary>
        private void GroundInclineCheck()
        {
            float rayDistance = Mathf.Infinity;
            _rearRayPos.rotation = Quaternion.Euler(transform.rotation.x, 0, 0);
            _frontRayPos.rotation = Quaternion.Euler(transform.rotation.x, 0, 0);

            Physics.Raycast(_rearRayPos.position, _rearRayPos.TransformDirection(-Vector3.up), out RaycastHit rearHit, rayDistance, _groundLayerMask);
            Physics.Raycast(
                _frontRayPos.position,
                _frontRayPos.TransformDirection(-Vector3.up),
                out RaycastHit frontHit,
                rayDistance,
                _groundLayerMask
            );

            Vector3 hitDifference = frontHit.point - rearHit.point;
            float xPlaneLength = new Vector2(hitDifference.x, hitDifference.z).magnitude;

            _inclineAngle = Mathf.Lerp(_inclineAngle, Mathf.Atan2(hitDifference.y, xPlaneLength) * Mathf.Rad2Deg, 20f * Time.deltaTime);
        }

        /// <summary>
        ///     Checks the height of the ceiling above the player to make sure there is room to stand up if crouching.
        /// </summary>
        private void CeilingHeightCheck()
        {
            float rayDistance = Mathf.Infinity;
            float minimumStandingHeight = _capsuleStandingHeight - _frontRayPos.localPosition.y;

            Vector3 midpoint = new Vector3(
                LocomotionTransform.position.x,
                LocomotionTransform.position.y + _frontRayPos.localPosition.y,
                LocomotionTransform.position.z);
            if (Physics.Raycast(midpoint, LocomotionTransform.TransformDirection(Vector3.up), out RaycastHit ceilingHit, rayDistance, _groundLayerMask))
            {
                _cannotStandUp = ceilingHit.distance < minimumStandingHeight;
            }
            else
            {
                _cannotStandUp = false;
            }
        }

        #endregion

        #region Falling

        /// <summary>
        ///     Resets the falling duration variables.
        /// </summary>
        private void ResetFallingDuration()
        {
            _fallStartTime = Time.time;
            _fallingDuration = 0f;
        }

        /// <summary>
        ///     Updates the falling duration variables.
        /// </summary>
        private void UpdateFallingDuration()
        {
            _fallingDuration = Time.time - _fallStartTime;
        }

        #endregion

        #region Checks

        /// <summary>
        ///     Checks if body turns can be enabled, and enabled or disables as required.
        /// </summary>
        private void CheckEnableTurns()
        {
            _headLookDelay = VariableOverrideDelayTimer(_headLookDelay);
            _enableHeadTurn = _headLookDelay == 0.0f && !_isStarting;
            _bodyLookDelay = VariableOverrideDelayTimer(_bodyLookDelay);
            _enableBodyTurn = _bodyLookDelay == 0.0f && !_isStarting;
        }

        /// <summary>
        ///     Checks if lean can be enabled, then enabled or disables as required.
        /// </summary>
        private void CheckEnableLean()
        {
            _leanDelay = VariableOverrideDelayTimer(_leanDelay);
            _enableLean = _leanDelay == 0.0f && !(_isStarting || _isTurningInPlace);
        }

        #endregion

        #region Lean and Offsets

        /// <summary>
        ///     Calculates the required rotational additives based on the passed in parameters.
        /// </summary>
        /// <param name="leansActivated">If leans are activated or not.</param>
        /// <param name="headLookActivated">If head look is activated or not.</param>
        /// <param name="bodyLookActivated">If body look is activated or not.</param>
        private void CalculateRotationalAdditives(bool leansActivated, bool headLookActivated, bool bodyLookActivated)
        {
            if (headLookActivated || leansActivated || bodyLookActivated)
            {
                _currentRotation = LocomotionTransform.forward;

                _rotationRate = _currentRotation != _previousRotation
                    ? Vector3.SignedAngle(_currentRotation, _previousRotation, Vector3.up) / Time.deltaTime * -1f
                    : 0f;
            }

            _initialLeanValue = leansActivated ? _rotationRate : 0f;

            float leanSmoothness = 5;
            float maxLeanRotationRate = 275.0f;

            float referenceValue = _speed2D / _sprintSpeed;
            _leanValue = CalculateSmoothedValue(
                _leanValue,
                _initialLeanValue,
                maxLeanRotationRate,
                leanSmoothness,
                _leanCurve,
                referenceValue,
                true
            );

            float headTurnSmoothness = 5f;

            if (headLookActivated && (_isTurningInPlace || _isIdleLooking))
            {
                _initialTurnValue = Mathf.Clamp(_cameraRotationOffset, -_headLookLimitDegrees, _headLookLimitDegrees);
                _headLookX = Mathf.Lerp(_headLookX, _initialTurnValue / 200, 5f * Time.deltaTime);
            }
            else
            {
                _initialTurnValue = headLookActivated ? _rotationRate : 0f;
                _headLookX = CalculateSmoothedValue(
                    _headLookX,
                    _initialTurnValue,
                    maxLeanRotationRate,
                    headTurnSmoothness,
                    _headLookXCurve,
                    _headLookX,
                    false
                );
            }

            float bodyTurnSmoothness = 5f;

            if (bodyLookActivated && (_isTurningInPlace || _isIdleLooking))
            {
                _initialTurnValue = Mathf.Clamp(_cameraRotationOffset, -_headLookLimitDegrees, _headLookLimitDegrees);
                _bodyLookX = Mathf.Lerp(_bodyLookX, _initialTurnValue / 200, 5f * Time.deltaTime);
            }
            else
            {
                _initialTurnValue = bodyLookActivated ? _rotationRate : 0f;
                _bodyLookX = CalculateSmoothedValue(
                    _bodyLookX,
                    _initialTurnValue,
                    maxLeanRotationRate,
                    bodyTurnSmoothness,
                    _bodyLookXCurve,
                    _bodyLookX,
                    false
                );
            }

            float cameraTilt = _cameraController.GetCameraTiltX();
            cameraTilt = (cameraTilt > 180f ? cameraTilt - 360f : cameraTilt) / -180;
            cameraTilt = Mathf.Clamp(cameraTilt, -0.1f, 1.0f);
            _headLookY = cameraTilt;
            _bodyLookY = cameraTilt;

            if (IsBowEquipped && (_isAiming || _isBowDrawing) && !IsBowMovementForcedWalk)
            {
                _headLookX *= _bowAimHeadLookMultiplier;
                _headLookY *= _bowAimHeadLookMultiplier;
                _bodyLookX *= _bowAimBodyLookMultiplier;
                _bodyLookY *= _bowAimBodyLookMultiplier;
            }

            if (IsBowMovementForcedWalk)
            {
                // Keep the bow upper body planted while the lower-body locomotion continues underneath it.
                _leanValue = 0f;
                _headLookX = 0f;
                _headLookY = 0f;
                _bodyLookX = 0f;
                _bodyLookY = 0f;
            }

            _previousRotation = _currentRotation;
        }

        /// <summary>
        ///     Calculates a smoothed value between the given variable and target variable, from the given parameters.
        /// </summary>
        /// <param name="mainVariable">The variable to smooth.</param>
        /// <param name="newValue">The target new value.</param>
        /// <param name="maxRateChange">The max rate of change.</param>
        /// <param name="smoothness">The smoothness amount.</param>
        /// <param name="referenceCurve">The reference curve.</param>
        /// <param name="referenceValue">The reference value.</param>
        /// <param name="isMultiplier">If the value is a multiplier or not.</param>
        /// <returns>The smoothed value.</returns>
        private float CalculateSmoothedValue(
            float mainVariable,
            float newValue,
            float maxRateChange,
            float smoothness,
            AnimationCurve referenceCurve,
            float referenceValue,
            bool isMultiplier
        )
        {
            float changeVariable = newValue / maxRateChange;

            changeVariable = Mathf.Clamp(changeVariable, -1.0f, 1.0f);

            if (isMultiplier)
            {
                float multiplier = referenceCurve.Evaluate(referenceValue);
                changeVariable *= multiplier;
            }
            else
            {
                changeVariable = referenceCurve.Evaluate(changeVariable);
            }

            if (!changeVariable.Equals(mainVariable))
            {
                changeVariable = Mathf.Lerp(mainVariable, changeVariable, smoothness * Time.deltaTime);
            }

            return changeVariable;
        }

        /// <summary>
        ///     Provides a clamped override delay to avoid animation transition issues.
        /// </summary>
        /// <param name="timeVariable">The time variable to use.</param>
        /// <returns>A clamped override delay.</returns>
        private float VariableOverrideDelayTimer(float timeVariable)
        {
            if (timeVariable > 0.0f)
            {
                timeVariable -= Time.deltaTime;
                timeVariable = Mathf.Clamp(timeVariable, 0.0f, 1.0f);
            }
            else
            {
                timeVariable = 0.0f;
            }

            return timeVariable;
        }

        #endregion


        #endregion

        #region Locomotion State

        /// <summary>
        ///     Sets up the locomotion state upon entry.
        /// </summary>
        private void EnterLocomotionState()
        {
            _inputReader.onJumpPerformed += LocomotionToJumpState;
        }

        /// <summary>
        ///     Updates the locomotion state.
        /// </summary>
        private void UpdateLocomotionState()
        {
            UpdateBestTarget();
            GroundedCheck();

            // Recharge coyote time whenever we're genuinely on the ground in a locomotion-friendly state.
            if (_isGrounded)
                _coyoteTimer = _coyoteTimeSeconds;

            if (!_isGrounded)
            {
                SwitchState(AnimationState.Fall);
            }

            if (_isCrouching)
            {
                SwitchState(AnimationState.Crouch);
            }

            CheckEnableTurns();
            CheckEnableLean();
            CalculateRotationalAdditives(_enableLean, _enableHeadTurn, _enableBodyTurn);

            CalculateMoveDirection();
            CheckIfStarting();
            CheckIfStopped();
            FaceMoveDirection();
            Move();
            UpdateAnimatorController();
        }

        /// <summary>
        ///     Performs the required actions when exiting the locomotion state.
        /// </summary>
        private void ExitLocomotionState()
        {
            _inputReader.onJumpPerformed -= LocomotionToJumpState;
        }

        /// <summary>
        ///     Moves from the locomotion to the jump state.
        /// </summary>
        private void LocomotionToJumpState()
        {
            SwitchState(AnimationState.Jump);
        }

        #endregion

        #region Jump State

        /// <summary>
        ///     Sets up the Jump state upon entry.
        /// </summary>
        private void EnterJumpState()
        {
            PresentationAnimator.SetBool(_isJumpingAnimHash, true);

            _isSliding = false;
            // Consume any pending buffered jump so it doesn't re-fire on the upcoming Fall landing.
            _jumpBufferedAt = -1f;

            _velocity = new Vector3(_velocity.x, _jumpForce, _velocity.z);
        }

        /// <summary>
        ///     updates the jump state.
        /// </summary>
        private void UpdateJumpState()
        {
            UpdateBestTarget();

            // Decay coyote while airborne so a jump-then-dropkick chain doesn't get a second free coyote jump on apex.
            if (_coyoteTimer > 0f)
                _coyoteTimer = Mathf.Max(0f, _coyoteTimer - Time.deltaTime);

            ApplyGravity();

            if (_velocity.y <= 0f)
            {
                PresentationAnimator.SetBool(_isJumpingAnimHash, false);
                SwitchState(AnimationState.Fall);
            }

            GroundedCheck();

            CalculateRotationalAdditives(false, _enableHeadTurn, _enableBodyTurn);
            CalculateMoveDirection();
            FaceMoveDirection();
            Move();
            UpdateAnimatorController();
        }

        /// <summary>
        ///     Performs the required actions upon exiting the jump state.
        /// </summary>
        private void ExitJumpState()
        {
            PresentationAnimator.SetBool(_isJumpingAnimHash, false);
        }

        #endregion

        #region Fall State

        /// <summary>
        ///     Sets up the fall state upon entry.
        /// </summary>
        private void EnterFallState()
        {
            ResetFallingDuration();
            _velocity.y = 0f;

            DeactivateCrouch();
            _isSliding = false;
        }

        /// <summary>
        ///     Updates the fall state.
        /// </summary>
        private void UpdateFallState()
        {
            UpdateBestTarget();

            // Coyote timer decays while airborne so a late jump press no longer qualifies after the window.
            if (_coyoteTimer > 0f)
                _coyoteTimer = Mathf.Max(0f, _coyoteTimer - Time.deltaTime);

            CalculateRotationalAdditives(false, _enableHeadTurn, _enableBodyTurn);

            CalculateMoveDirection();
            FaceMoveDirection();

            ApplyGravity();
            Move();

            // GroundedCheck must run AFTER Move() so we detect landing using the new position
            GroundedCheck();
            UpdateAnimatorController();

            // Use _isGrounded (Physics.CheckSphere) instead of LocomotionController.isGrounded - CharacterController
            // isGrounded is unreliable and often fails to detect landing
            if (_isGrounded)
            {
                // Jump buffer: a jump pressed just before landing fires immediately instead of being dropped.
                if (IsJumpBufferFresh())
                {
                    _jumpBufferedAt = -1f;
                    _coyoteTimer = _coyoteTimeSeconds;
                    SwitchState(AnimationState.Jump);
                }
                else
                {
                    SwitchState(AnimationState.Locomotion);
                }
            }

            UpdateFallingDuration();
        }

        #endregion

        #region Crouch State

        /// <summary>
        ///     Sets up the crouch state upon entry.
        /// </summary>
        private void EnterCrouchState()
        {
            _inputReader.onJumpPerformed += CrouchToJumpState;
        }

        /// <summary>
        ///     Updates the crouch state.
        /// </summary>
        private void UpdateCrouchState()
        {
            UpdateBestTarget();

            GroundedCheck();
            if (!_isGrounded)
            {
                DeactivateCrouch();
                CapsuleCrouchingSize(false);
                SwitchState(AnimationState.Fall);
            }

            CeilingHeightCheck();

            if (!_crouchKeyPressed && !_cannotStandUp)
            {
                DeactivateCrouch();
                SwitchToLocomotionState();
            }

            if (!_isCrouching)
            {
                CapsuleCrouchingSize(false);
                SwitchToLocomotionState();
            }

            CheckEnableTurns();
            CheckEnableLean();

            CalculateRotationalAdditives(false, _enableHeadTurn, false);

            CalculateMoveDirection();
            CheckIfStarting();
            CheckIfStopped();

            FaceMoveDirection();
            Move();
            UpdateAnimatorController();
        }

        /// <summary>
        ///     Performs the required actions upon exiting the crouch state.
        /// </summary>
        private void ExitCrouchState()
        {
            _inputReader.onJumpPerformed -= CrouchToJumpState;
        }

        /// <summary>
        ///     Moves from the crouch state to the jump state.
        /// </summary>
        private void CrouchToJumpState()
        {
            if (!_cannotStandUp)
            {
                DeactivateCrouch();
                SwitchState(AnimationState.Jump);
            }
        }

        /// <summary>
        ///     Moves from the crouch state to the locomotion state.
        /// </summary>
        private void SwitchToLocomotionState()
        {
            DeactivateCrouch();
            SwitchState(AnimationState.Locomotion);
        }

        #endregion
    }
}
