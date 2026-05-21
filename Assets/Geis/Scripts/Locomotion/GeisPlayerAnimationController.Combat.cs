/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using System;
using Geis.Animation;
using Geis.Combat;
using Geis.InputSystem;
using Geis.InteractInput;
using Geis.SoulRealm;
using UnityEngine;

namespace Geis.Locomotion
{
    public partial class GeisPlayerAnimationController
    {
        #region Attack (Data-Driven Combo)

        private GeisComboData GetCurrentComboData() => _comboController.GetCurrentComboData();

        private void ApplyComboOverridesIfReady() =>
            _comboController.ApplyOverridesIfReady(PresentationAnimator);

        private void OnLightAttackRequested()
        {
            if (IsBowEquipped)
                return;

            if (!_isGrounded || _isCrouching)
                return;

            var comboData = GetCurrentComboData();

            if (_currentState == AnimationState.Locomotion)
            {
                if (_useDataDrivenCombo && comboData != null)
                {
                    _comboController.BeginFirstAttack(GeisComboInputType.Light);
                    SwitchState(AnimationState.Attack);
                }
                else if (PresentationAnimator != null && HasAnimatorParameter("Attack_1"))
                {
                    _comboController.BeginFirstAttack(GeisComboInputType.Light);
                    SwitchState(AnimationState.Attack);
                }
            }
            else if (_currentState == AnimationState.Attack && _useDataDrivenCombo && comboData != null)
            {
                _inputBuffers.SetComboInputBuffer(GeisComboInputType.Light, Time.unscaledTime);
            }
            else if (_currentState == AnimationState.Dodge && _useDataDrivenCombo && comboData != null)
            {
                _inputBuffers.SetComboInputBuffer(GeisComboInputType.Light, Time.unscaledTime);
            }
        }

        private void OnHeavyAttackRequested()
        {
            if (IsBowEquipped)
                return;

            if (!_isGrounded || _isCrouching)
                return;

            if (_currentState == AnimationState.Locomotion && _useDataDrivenCombo && GetCurrentComboData() != null)
            {
                _comboController.BeginFirstAttack(GeisComboInputType.Heavy);
                SwitchState(AnimationState.Attack);
            }
            else if (_currentState == AnimationState.Attack && _useDataDrivenCombo)
            {
                _inputBuffers.SetComboInputBuffer(GeisComboInputType.Heavy, Time.unscaledTime);
            }
            else if (_currentState == AnimationState.Dodge && _useDataDrivenCombo && GetCurrentComboData() != null)
            {
                _inputBuffers.SetComboInputBuffer(GeisComboInputType.Heavy, Time.unscaledTime);
            }
        }

        private void OnDodgeRequested()
        {
            if (GeisInteractInput.IsMovementFrozenForInteraction)
                return;

            if (PresentationAnimator == null || !HasAnimatorParameter("Dodge") || !HasAnimatorParameter("DodgeDirection"))
            {
                if (!_dodgeController.LoggedAnimatorMissing)
                {
                    _dodgeController.LoggedAnimatorMissing = true;
                    Debug.LogWarning(
                        "[GeisPlayerAnimationController] Animator is missing Dodge (Trigger) and/or DodgeDirection (Int), or dodge states. " +
                        "Run menu: Geis → Animator → Setup Directional Dodge & Roll Clips.");
                }

                return;
            }

            if (_dodgeDoubleTapRollEnabled && _dodgeController.RequestIsRoll && !HasAnimatorParameter("Roll"))
            {
                if (!_dodgeController.LoggedForwardRollMissing)
                {
                    _dodgeController.LoggedForwardRollMissing = true;
                    Debug.LogWarning(
                        "[GeisPlayerAnimationController] Animator is missing Roll (Trigger). " +
                        "Run menu: Geis → Animator → Setup Directional Dodge & Roll Clips.",
                        this);
                }
            }

            Vector2 moveComposite = GetEffectiveMoveComposite();
            if (_requireMovementInputForDodge
                && moveComposite.sqrMagnitude < _dodgeInputDeadzone * _dodgeInputDeadzone)
            {
                return;
            }

            float nowUnscaled = Time.unscaledTime;
            bool rollIntent = _inputBuffers.RecordDodgeTapAndGetRollIntent(nowUnscaled, _dodgeDoubleTapRollEnabled);

            if (_debugDodgeDoubleTap)
            {
                float followUpRemaining = _inputBuffers.DodgeRollFollowUpExpiresAtUnscaled > 0f
                    ? _inputBuffers.DodgeRollFollowUpExpiresAtUnscaled - nowUnscaled
                    : -1f;
                Debug.Log(
                    $"[GeisPlayerAnimationController] Dodge press — state={_currentState} rollIntent={rollIntent} followUpRemaining={followUpRemaining:F3}s isRoll={_dodgeController.IsRoll}",
                    this);
            }

            if (_currentState == AnimationState.Attack)
            {
                _inputBuffers.BufferDodgeFromAttackCancel(rollIntent, nowUnscaled);
                return;
            }

            if (_currentState == AnimationState.Dodge)
            {
                if (rollIntent && !_dodgeController.IsRoll)
                    UpgradeActiveDodgeToRoll();

                return;
            }

            if (_currentState != AnimationState.Locomotion || !_isGrounded || _isCrouching)
                return;

            _dodgeController.RequestIsRoll = rollIntent;
            if (!rollIntent && _dodgeDoubleTapRollEnabled)
                _inputBuffers.ArmRollFollowUpWindow(nowUnscaled, _dodgeDoubleTapRollEnabled);
            else if (rollIntent)
                _inputBuffers.ClearRollFollowUpWindow();

            SwitchState(AnimationState.Dodge);
        }

        private void UpgradeActiveDodgeToRoll()
        {
            if (PresentationAnimator == null)
                return;

            bool preserveCameraFacing = ShouldPreserveCameraFacingOnDodge(_speed2D) && !_isLockedOn;
            if (preserveCameraFacing)
                SnapBodyYawToCameraForward();

            int dir = ComputeDodgeDirectionIndex();
            _dodgeController.UpgradeToRoll(dir, _dodgeFallbackDuration, Time.unscaledTime, _isLockedOn || preserveCameraFacing);

            _dodgeController.PlayLeafCrossFade(
                PresentationAnimator,
                dir,
                forceRestart: true,
                HasAnimatorParameter("DodgeDirection"),
                HasAnimatorParameter("Dodge"),
                HasAnimatorParameter("Roll"));
            SyncRollAnimatorPlayback();

            if (_debugDodgeDoubleTap)
            {
                Debug.Log(
                    $"[GeisPlayerAnimationController] Upgraded dodge to roll — dir={dir} distanceMult={_rollDistanceMultiplier}",
                    this);
            }
        }

        private Vector2 GetEffectiveMoveComposite()
        {
            if (_inputReader == null)
                return Vector2.zero;

            return GeisInteractInput.GetEffectiveMoveCompositeForLocomotion(_inputReader.EffectiveMoveComposite);
        }

        private int ComputeDodgeDirectionIndex()
        {
            Vector2 m = GetEffectiveMoveComposite();
            Vector3 bodyForward = GetPlanarForward();
            Vector3 bodyRight = Vector3.Cross(Vector3.up, bodyForward).normalized;

            return _dodgeController.ComputeDirectionIndex(
                m,
                _dodgeInputDeadzone,
                _isLockedOn && _currentLockOnTarget != null,
                GetPlanarDirectionAwayFromLockOnTarget(),
                GetCameraRelativeDodgeWorldDirection(m),
                bodyForward,
                bodyRight);
        }

        private bool ShouldPreserveCameraFacingOnDodge(float planarSpeedAtPress = -1f)
        {
            if (_isAiming || _isBowDrawing)
                return true;

            return IsWithinStrafeStyleSpeedCap(planarSpeedAtPress);
        }

        private void SnapBodyYawToCameraForward()
        {
            if (_cameraController == null)
                return;

            Vector3 forward = _cameraController.GetCameraForwardZeroedYNormalised();
            if (forward.sqrMagnitude < 0.0001f)
                return;

            LocomotionTransform.rotation = Quaternion.LookRotation(forward);
        }

        private Vector3 GetCameraRelativeDodgeWorldDirection(Vector2 m)
        {
            Vector3 dir = GeisLocomotionKinematics.ComputeCameraRelativeMoveDirection(m, _cameraController);
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : dir;
        }

        private Vector3 GetPlanarDirectionAwayFromLockOnTarget()
        {
            Vector3 toTarget = ResolveLockOnWorldPosition(_currentLockOnTarget) - LocomotionTransform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                Vector3 back = -GetPlanarForward();
                return back.sqrMagnitude > 0.0001f ? back : Vector3.back;
            }

            return (-toTarget).normalized;
        }

        private void EnterDodgeState()
        {
            float planarSpeedAtDodgePress = _speed2D;
            _velocity.x = 0f;
            _velocity.z = 0f;

            bool isRoll = _dodgeController.RequestIsRoll;
            bool preserveCameraFacing = ShouldPreserveCameraFacingOnDodge(planarSpeedAtDodgePress) && !_isLockedOn;
            if (preserveCameraFacing)
                SnapBodyYawToCameraForward();

            int dir = ComputeDodgeDirectionIndex();

            if (isRoll && !_dodgeController.HasRollLeafStateForDirection(PresentationAnimator, dir))
            {
                if (!_dodgeController.LoggedForwardRollMissing)
                {
                    _dodgeController.LoggedForwardRollMissing = true;
                    Debug.LogWarning(
                        "[GeisPlayerAnimationController] Animator is missing one or more directional roll states. " +
                        "Run menu: Geis → Animator → Setup Directional Dodge & Roll Clips.",
                        this);
                }
            }

            Vector3 faceWorld = _dodgeController.GetFacingWorld(
                dir,
                _cameraController.GetCameraForwardZeroedYNormalised(),
                _cameraController.GetCameraRightZeroedYNormalised());

            bool preserveStrafeFacing = _isLockedOn || preserveCameraFacing || (_isStrafing && !isRoll);
            _dodgeController.BeginDodge(isRoll, dir, preserveStrafeFacing, _dodgeFallbackDuration, Time.unscaledTime);

            if (!preserveStrafeFacing && faceWorld.sqrMagnitude > 0.0001f)
                LocomotionTransform.rotation = Quaternion.LookRotation(faceWorld);

            _dodgeController.PlayLeafCrossFade(
                PresentationAnimator,
                dir,
                forceRestart: false,
                HasAnimatorParameter("DodgeDirection"),
                HasAnimatorParameter("Dodge"),
                HasAnimatorParameter("Roll"));
            SyncRollAnimatorPlayback();

            if (isRoll)
                _inputBuffers.ClearRollFollowUpWindow();
            else if (_dodgeDoubleTapRollEnabled)
                _inputBuffers.ArmRollFollowUpWindow(Time.unscaledTime, _dodgeDoubleTapRollEnabled);
        }

        private Vector3 GetPlanarForward()
        {
            Vector3 f = LocomotionTransform.forward;
            f.y = 0f;
            float sq = f.sqrMagnitude;
            return sq > 0.0001f ? f / Mathf.Sqrt(sq) : Vector3.forward;
        }

        private void UpdateDodgeState()
        {
            ApplyGravity();
            _dodgeController.StateTimeout -= Time.deltaTime;

            _inputBuffers.PruneStaleComboBuffer(Time.unscaledTime);
            _inputBuffers.PruneStaleDodgeBuffer(Time.unscaledTime);

            GroundedCheck();
            if (!_isGrounded)
            {
                SwitchState(AnimationState.Fall);
                return;
            }

            if (PresentationAnimator != null)
            {
                AnimatorStateInfo info = PresentationAnimator.GetCurrentAnimatorStateInfo(0);
                if (PresentationAnimator.IsInTransition(0))
                {
                    AnimatorStateInfo next = PresentationAnimator.GetNextAnimatorStateInfo(0);
                    if (GeisDodgeRollController.IsDodgeLeafShortNameHash(next.shortNameHash))
                        _dodgeController.AnimatorEnteredLeaf = true;
                }
                else if (GeisDodgeRollController.IsDodgeLeafShortNameHash(info.shortNameHash))
                {
                    _dodgeController.AnimatorEnteredLeaf = true;
                }

                float recoveryThreshold = _dodgeController.IsRoll
                    ? _rollRecoveryStartNormalizedTime
                    : _dodgeRecoveryStartNormalizedTime;
                bool inRecoveryWindow = _dodgeController.AnimatorEnteredLeaf
                    && GeisDodgeRollController.IsDodgeLeafShortNameHash(info.shortNameHash)
                    && !PresentationAnimator.IsInTransition(0)
                    && info.normalizedTime >= recoveryThreshold;

                if (inRecoveryWindow)
                {
                    var comboData = GetCurrentComboData();
                    if (_useDataDrivenCombo && comboData != null && !_isCrouching
                        && _inputBuffers.TryConsumeComboInputBuffer(Time.unscaledTime, out var queuedAttack))
                    {
                        _comboController.BeginFirstAttack(queuedAttack);
                        SwitchState(AnimationState.Attack);
                        return;
                    }

                    if (!_dodgeController.IsRoll)
                    {
                        Vector2 composite = GetEffectiveMoveComposite();
                        float threshold = _dodgeMoveCancelStickThreshold;
                        if (composite.sqrMagnitude >= threshold * threshold)
                        {
                            SeedPlanarVelocityAfterDodgeExit();
                            SwitchState(AnimationState.Locomotion);
                            return;
                        }
                    }
                }

                if (_dodgeController.AnimatorEnteredLeaf && !PresentationAnimator.IsInTransition(0)
                    && !GeisDodgeRollController.IsDodgeLeafShortNameHash(info.shortNameHash))
                {
                    SeedPlanarVelocityAfterDodgeExit();
                    SwitchState(AnimationState.Locomotion);
                    return;
                }

                if (info.length > 0.01f && info.normalizedTime >= 0.99f && !PresentationAnimator.IsInTransition(0)
                    && GeisDodgeRollController.IsDodgeLeafShortNameHash(info.shortNameHash))
                {
                    SeedPlanarVelocityAfterDodgeExit();
                    SwitchState(AnimationState.Locomotion);
                    return;
                }
            }

            UpdateDodgeInvulnerabilityFromAnimator();

            if (_dodgeController.StateTimeout <= 0f)
            {
                SeedPlanarVelocityAfterDodgeExit();
                SwitchState(AnimationState.Locomotion);
            }
            else
            {
                UpdateAnimatorController();
            }
        }

        private void ExitDodgeState()
        {
            if (_dodgeController.IsRoll)
            {
                _velocity.x = 0f;
                _velocity.z = 0f;
            }

            _dodgeController.EndDodge();
            SyncRollAnimatorPlayback();
            if (_defensiveCombatState != null)
                _defensiveCombatState.SetDodgeInvulnerable(false);
        }

        private void EnterAttackState()
        {
            _velocity.x = 0f;
            _velocity.z = 0f;
            ClearMovementInputStateForAttack();
            _inputBuffers.ResetJumpBuffer();

            _comboController.EnterAttack(
                PresentationAnimator,
                HasAnimatorParameter("Attack"),
                HasAnimatorParameter("Attack_1"));
            _attackStateTimeout = _comboController.AttackStateTimeout;
        }

        private void UpdateAttackState()
        {
            ApplyGravity();
            ClearMovementInputStateForAttack();
            _attackStateTimeout = _comboController.AttackStateTimeout;
            _attackStateTimeout -= Time.deltaTime;
            _comboController.AttackStateTimeout = _attackStateTimeout;

            _inputBuffers.PruneStaleComboBuffer(Time.unscaledTime);
            _inputBuffers.PruneStaleDodgeBuffer(Time.unscaledTime);

            var comboData = GetCurrentComboData();
            bool inAttackAnimatorLeaf = false;

            if (PresentationAnimator != null)
            {
                AnimatorStateInfo info = PresentationAnimator.GetCurrentAnimatorStateInfo(0);

                if (PresentationAnimator.IsInTransition(0))
                {
                    AnimatorStateInfo next = PresentationAnimator.GetNextAnimatorStateInfo(0);
                    if (GeisComboAttackController.IsAttackLeafShortNameHash(next.shortNameHash))
                        _comboController.AttackAnimatorEnteredLeaf = true;
                }
                else if (GeisComboAttackController.IsAttackLeafShortNameHash(info.shortNameHash))
                {
                    _comboController.AttackAnimatorEnteredLeaf = true;
                }

                inAttackAnimatorLeaf = _comboController.AttackAnimatorEnteredLeaf
                    && !PresentationAnimator.IsInTransition(0)
                    && GeisComboAttackController.IsAttackLeafShortNameHash(info.shortNameHash);

                if (_comboController.AttackAnimatorEnteredLeaf && !PresentationAnimator.IsInTransition(0)
                    && !GeisComboAttackController.IsAttackLeafShortNameHash(info.shortNameHash))
                {
                    SwitchState(AnimationState.Locomotion);
                    return;
                }
            }

            if (_useDataDrivenCombo && comboData != null && PresentationAnimator != null && inAttackAnimatorLeaf)
            {
                AnimatorStateInfo info = PresentationAnimator.GetCurrentAnimatorStateInfo(0);
                float normalizedTime = info.normalizedTime % 1f;
                comboData.GetCancelWindow(_comboController.CurrentComboState, out float cancelWindowStart, out float cancelWindowEnd);
                bool inCancelWindow = normalizedTime >= cancelWindowStart && normalizedTime <= cancelWindowEnd;

                if (inCancelWindow)
                {
                    if (_inputBuffers.TryConsumeDodgeInputBuffer(Time.unscaledTime, out bool bufferedDodgeIsRoll)
                        && _isGrounded && !_isCrouching
                        && HasAnimatorParameter("Dodge") && HasAnimatorParameter("DodgeDirection"))
                    {
                        _inputBuffers.ClearComboInputBuffer();
                        _dodgeController.RequestIsRoll = bufferedDodgeIsRoll;
                        SwitchState(AnimationState.Dodge);
                        return;
                    }

                    if (_inputBuffers.TryConsumeComboInputBuffer(Time.unscaledTime, out var input)
                        && _comboController.TryContinueCombo(PresentationAnimator, input, out float newTimeout))
                    {
                        _attackStateTimeout = newTimeout;
                        return;
                    }

                    Vector2 moveComposite = GetEffectiveMoveComposite();
                    if (moveComposite.sqrMagnitude >= _attackMoveCancelStickThreshold * _attackMoveCancelStickThreshold)
                    {
                        SeedPlanarVelocityFromStick(_attackExitVelocityCarry);
                        SwitchState(AnimationState.Locomotion);
                        return;
                    }
                }

                if (normalizedTime >= _attackRecoveryExitNormalizedTime && !PresentationAnimator.IsInTransition(0))
                {
                    SwitchState(AnimationState.Locomotion);
                    return;
                }
            }

            if (_attackStateTimeout <= 0f)
            {
                SwitchState(AnimationState.Locomotion);
                return;
            }

            UpdateAnimatorController();
        }

        private void ExitAttackState()
        {
            _comboController.AttackAnimatorEnteredLeaf = false;
            _comboController.ResetComboState();
        }

        private void ClearMovementInputStateForAttack()
        {
            _movementInputTapped = false;
            _movementInputPressed = false;
            _movementInputHeld = false;
            _speed2D = 0f;
            _moveDirection = Vector3.zero;
            _targetVelocity.x = 0f;
            _targetVelocity.z = 0f;
            _inputReader?.ResetMovementTapState();
        }

        private void SeedPlanarVelocityFromStick(float carryFraction)
        {
            if (_inputReader == null || _cameraController == null)
                return;

            Vector2 composite = GetEffectiveMoveComposite();
            if (composite.sqrMagnitude < 0.0001f)
                return;

            Vector3 camRelative = GeisLocomotionKinematics.ComputeCameraRelativeMoveDirection(composite, _cameraController);

            float scale = Mathf.Max(0f, carryFraction) * _currentMaxSpeed;
            _velocity.x = camRelative.x * scale;
            _velocity.z = camRelative.z * scale;
        }

        private void SeedPlanarVelocityAfterDodgeExit()
        {
            float carry = _dodgeController.IsRoll ? _rollExitVelocityCarry : _dodgeExitVelocityCarry;
            if (carry <= 0.0001f)
            {
                _velocity.x = 0f;
                _velocity.z = 0f;
                return;
            }

            SeedPlanarVelocityFromStick(carry);
        }

        private void SyncRollAnimatorPlayback()
        {
            if (PresentationAnimator != null)
                PresentationAnimator.speed = 1f;
        }

        private void OnAnimatorMove()
        {
            if (_animatorIsOnChild)
                return;

            ApplyAttackDodgeRootMotionToBody();
        }

        private void LateUpdate()
        {
            if (PresentationAnimator == null)
                return;

            if (SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive
                && !SoulRealmManager.Instance.AllowGhostMovement)
                return;

            SyncAnimatorApplyRootMotionForState();

            if (_animatorIsOnChild)
            {
                ApplyAttackDodgeRootMotionToBody();
                RealignVisualRigUnderBody();
            }
        }

        private void SyncAnimatorApplyRootMotionForState()
        {
            bool wantsRootMotion = _currentState == AnimationState.Attack
                || _currentState == AnimationState.Dodge;

            if (PresentationAnimator.applyRootMotion != wantsRootMotion)
                PresentationAnimator.applyRootMotion = wantsRootMotion;
        }

        private void ApplyAttackDodgeRootMotionToBody()
        {
            if (PresentationAnimator == null || !PresentationAnimator.applyRootMotion || LocomotionController == null || !LocomotionController.enabled)
                return;

            if (_currentState == AnimationState.Attack)
            {
                var deltaPosition = PresentationAnimator.deltaPosition;
                deltaPosition.y += _velocity.y * Time.deltaTime;

                LocomotionController.Move(deltaPosition);

                if (_applyRootRotationDuringAttack && PresentationAnimator.deltaRotation != Quaternion.identity)
                    LocomotionTransform.rotation = LocomotionTransform.rotation * PresentationAnimator.deltaRotation;
            }
            else if (_currentState == AnimationState.Dodge)
            {
                var deltaPosition = PresentationAnimator.deltaPosition;

                if (_dodgeController.IsRoll)
                {
                    AnimatorStateInfo rollInfo = PresentationAnimator.GetCurrentAnimatorStateInfo(0);
                    if (GeisDodgeRollController.IsDodgeLeafShortNameHash(rollInfo.shortNameHash) && rollInfo.normalizedTime > 0.85f)
                    {
                        float fade = 1f - Mathf.InverseLerp(0.85f, 0.98f, rollInfo.normalizedTime);
                        deltaPosition.x *= fade;
                        deltaPosition.z *= fade;
                    }
                    else if (!_dodgeController.HasRollLeafStateForDirection(PresentationAnimator, _dodgeController.AnimatorDir)
                        && !Mathf.Approximately(_rollDistanceMultiplier, 1f))
                    {
                        deltaPosition.x *= _rollDistanceMultiplier;
                        deltaPosition.z *= _rollDistanceMultiplier;
                    }
                }

                deltaPosition.y += _velocity.y * Time.deltaTime;

                LocomotionController.Move(deltaPosition);

                if (_applyRootRotationDuringDodge && !_dodgeController.PreserveStrafeFacing
                    && PresentationAnimator.deltaRotation != Quaternion.identity)
                    LocomotionTransform.rotation = LocomotionTransform.rotation * PresentationAnimator.deltaRotation;
            }
        }

        private void RealignVisualRigUnderBody()
        {
            Transform rig = PresentationAnimator.transform;
            if (rig == LocomotionTransform)
                return;

            rig.localPosition = Vector3.zero;
            rig.localRotation = Quaternion.identity;
        }

        #endregion
    }
}
