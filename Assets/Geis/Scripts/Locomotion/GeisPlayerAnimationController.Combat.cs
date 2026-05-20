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
        #region Attack (Data-Driven Combo)

        private GeisComboData GetCurrentComboData()
        {
            if (_weaponSwitcher != null && _weaponSwitcher.TryGetComboForWeapon(_weaponSwitcher.CurrentWeaponIndex, out var unifiedCombo))
                return unifiedCombo;
            if (_weaponComboData != null && _weaponSwitcher != null)
            {
                int idx = _weaponSwitcher.CurrentWeaponIndex;
                var data = _weaponComboData.GetComboForWeapon(idx);
                if (data != null) return data;
            }
            return _comboData;
        }

        /// <summary>
        /// Applies combo clips from GeisComboData to the animator via AnimatorOverrideController.
        /// Uses placeholders in the blend tree; no Sync step needed. Call on Start and when combo data changes.
        /// </summary>
        private void ApplyComboOverridesIfReady()
        {
            if (!_useDataDrivenCombo || PresentationAnimator == null) return;

            var comboData = GetCurrentComboData();
            if (comboData == null) return;
            if (comboData == _lastAppliedComboData) return;

            var placeholders = _comboPlaceholders != null
                ? _comboPlaceholders
                : Resources.Load<GeisComboPlaceholders>("GeisComboPlaceholders");
            if (placeholders == null) return;

            var current = PresentationAnimator.runtimeAnimatorController;
            RuntimeAnimatorController baseController = null;
            if (current is AnimatorOverrideController aoc)
                baseController = aoc.runtimeAnimatorController;
            else if (current != null)
                baseController = current;

            if (baseController == null) return;

            if (_comboOverrideController == null || _comboOverrideController.runtimeAnimatorController != baseController)
                _comboOverrideController = new AnimatorOverrideController(baseController);

            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            for (int i = 0; i < 32; i++)
            {
                var placeholder = placeholders.GetPlaceholder(i);
                var clip = comboData.GetClipForState(i);
                if (placeholder != null && clip != null)
                    overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(placeholder, clip));
            }

            if (overrides.Count > 0)
                _comboOverrideController.ApplyOverrides(overrides);

            PresentationAnimator.runtimeAnimatorController = _comboOverrideController;
            _lastAppliedComboData = comboData;
        }

        private void OnLightAttackRequested()
        {
            if (IsBowEquipped)
                return;

            if (!_isGrounded || _isCrouching) return;

            var comboData = GetCurrentComboData();

            if (_currentState == AnimationState.Locomotion)
            {
                if (_useDataDrivenCombo && comboData != null)
                {
                    _firstAttackInputType = GeisComboInputType.Light;
                    _currentComboState = 0;
                    SwitchState(AnimationState.Attack);
                }
                else if (PresentationAnimator != null && HasAnimatorParameter("Attack_1"))
                {
                    _firstAttackInputType = GeisComboInputType.Light;
                    SwitchState(AnimationState.Attack);
                }
            }
            else if (_currentState == AnimationState.Attack && _useDataDrivenCombo && comboData != null)
            {
                SetComboInputBuffer(GeisComboInputType.Light);
            }
            else if (_currentState == AnimationState.Dodge && _useDataDrivenCombo && comboData != null)
            {
                // Dodge → Attack cancel: buffer; UpdateDodgeState consumes inside the dodge recovery window.
                SetComboInputBuffer(GeisComboInputType.Light);
            }
        }

        private void OnHeavyAttackRequested()
        {
            // Bow draw/shot uses RT via GeisBowController; never start heavy melee while bow is equipped.
            if (IsBowEquipped)
                return;

            if (!_isGrounded || _isCrouching) return;

            if (_currentState == AnimationState.Locomotion && _useDataDrivenCombo && GetCurrentComboData() != null)
            {
                _firstAttackInputType = GeisComboInputType.Heavy;
                _currentComboState = 0;
                SwitchState(AnimationState.Attack);
            }
            else if (_currentState == AnimationState.Attack && _useDataDrivenCombo)
            {
                SetComboInputBuffer(GeisComboInputType.Heavy);
            }
            else if (_currentState == AnimationState.Dodge && _useDataDrivenCombo && GetCurrentComboData() != null)
            {
                SetComboInputBuffer(GeisComboInputType.Heavy);
            }
        }

        private void OnDodgeRequested()
        {
            if (GeisInteractInput.IsMovementFrozenForInteraction)
                return;
            if (PresentationAnimator == null || !HasAnimatorParameter("Dodge") || !HasAnimatorParameter("DodgeDirection"))
            {
                if (!_loggedDodgeAnimatorMissing)
                {
                    _loggedDodgeAnimatorMissing = true;
                    Debug.LogWarning(
                        "[GeisPlayerAnimationController] Animator is missing Dodge (Trigger) and/or DodgeDirection (Int), or dodge states. " +
                        "Run menu: Geis → Animator → Setup Directional Dodge & Roll Clips.");
                }

                return;
            }
            if (_dodgeDoubleTapRollEnabled && _dodgeRequestIsRoll && !HasAnimatorParameter("Roll"))
            {
                if (!_loggedForwardRollMissing)
                {
                    _loggedForwardRollMissing = true;
                    Debug.LogWarning(
                        "[GeisPlayerAnimationController] Animator is missing Roll (Trigger). " +
                        "Run menu: Geis → Animator → Setup Directional Dodge & Roll Clips.",
                        this);
                }
            }
            if (_requireMovementInputForDodge &&
                GeisInteractInput.GetEffectiveMoveCompositeForLocomotion(_inputReader._moveComposite).sqrMagnitude <
                _dodgeInputDeadzone * _dodgeInputDeadzone)
                return;

            // Double-tap: second press must land within the button window AND while a dash follow-up is armed.
            float nowUnscaled = Time.unscaledTime;
            float dtSinceLast = _lastDodgeTapAt > 0f ? (nowUnscaled - _lastDodgeTapAt) : -1f;
            bool rollIntent = _dodgeDoubleTapRollEnabled
                && _lastDodgeTapAt > 0f
                && dtSinceLast <= _dodgeDoubleTapWindow
                && nowUnscaled <= _dodgeRollFollowUpExpiresAtUnscaled;
            _lastDodgeTapAt = nowUnscaled;

            if (_debugDodgeDoubleTap)
            {
                float followUpRemaining = _dodgeRollFollowUpExpiresAtUnscaled > 0f
                    ? _dodgeRollFollowUpExpiresAtUnscaled - nowUnscaled
                    : -1f;
                Debug.Log(
                    $"[GeisPlayerAnimationController] Dodge press — state={_currentState} dtSinceLast={dtSinceLast:F3}s window={_dodgeDoubleTapWindow:F3}s rollEnabled={_dodgeDoubleTapRollEnabled} rollIntent={rollIntent} followUpRemaining={followUpRemaining:F3}s isRoll={_dodgeIsRoll}",
                    this);
            }

            // Attack → Dodge cancel: buffer; UpdateAttackState consumes inside the combo cancel window.
            if (_currentState == AnimationState.Attack)
            {
                _dodgeInputBufferedAt = nowUnscaled;
                _dodgeInputBufferIsRoll = rollIntent;
                return;
            }

            if (_currentState == AnimationState.Dodge)
            {
                if (rollIntent && !_dodgeIsRoll)
                    UpgradeActiveDodgeToRoll();

                return;
            }

            if (_currentState != AnimationState.Locomotion || !_isGrounded || _isCrouching)
                return;

            _dodgeRequestIsRoll = rollIntent;
            if (!rollIntent && _dodgeDoubleTapRollEnabled)
                _dodgeRollFollowUpExpiresAtUnscaled = nowUnscaled + _dodgeDoubleTapWindow;
            else if (rollIntent)
                _dodgeRollFollowUpExpiresAtUnscaled = -1f;

            SwitchState(AnimationState.Dodge);
        }

        /// <summary>Swaps the active dash clip to a roll without exiting gameplay Dodge state.</summary>
        private void UpgradeActiveDodgeToRoll()
        {
            if (PresentationAnimator == null)
                return;

            bool preserveCameraFacing = ShouldPreserveCameraFacingOnDodge(_speed2D) && !_isLockedOn;
            if (preserveCameraFacing)
                SnapBodyYawToCameraForward();

            int stickDirection = ComputeDodgeDirectionIndex();
            int dir = stickDirection;

            _dodgeIsRoll = true;
            _dodgeAnimatorDir = dir;
            _dodgeStateEnteredAtUnscaled = Time.unscaledTime;
            _dodgeRollFollowUpExpiresAtUnscaled = -1f;
            _dodgePreserveStrafeFacing = _isLockedOn || preserveCameraFacing;
            _dodgeStateTimeout = _dodgeFallbackDuration;

            if (HasAnimatorParameter("DodgeDirection"))
                PresentationAnimator.SetInteger(_dodgeDirectionHash, dir);

            _dodgeAnimatorEnteredLeaf = false;
            PlayDodgeLeafCrossFade(dir, forceRestart: true);
            SyncRollAnimatorPlayback();

            if (_debugDodgeDoubleTap)
            {
                Debug.Log(
                    $"[GeisPlayerAnimationController] Upgraded dodge to roll — dir={dir} clip={GetRollLeafHashForDirection(dir)} distanceMult={_rollDistanceMultiplier}",
                    this);
            }
        }

        /// <summary>CrossFades directly into the dodge leaf (bypasses brittle Any-State + DodgeDirection routing).</summary>
        private void PlayDodgeLeafCrossFade(int dir, bool forceRestart = false)
        {
            if (PresentationAnimator == null)
                return;

            PresentationAnimator.ResetTrigger(_dodgeTriggerHash);
            PresentationAnimator.ResetTrigger(_rollTriggerHash);

            if (HasAnimatorParameter("DodgeDirection"))
                PresentationAnimator.SetInteger(_dodgeDirectionHash, dir);

            int primaryHash = _dodgeIsRoll
                ? GetRollNestedHashForDirection(dir)
                : GetDodgeNestedStateHashForDirection(dir);
            int fallbackHash = _dodgeIsRoll
                ? GetRollLeafHashForDirection(dir)
                : GetDodgeLeafHashForDirection(dir);

            if (forceRestart)
            {
                if (PresentationAnimator.HasState(0, primaryHash))
                    PresentationAnimator.Play(primaryHash, 0, 0f);
                else if (PresentationAnimator.HasState(0, fallbackHash))
                    PresentationAnimator.Play(fallbackHash, 0, 0f);
                else if (_dodgeIsRoll && HasAnimatorParameter("Roll"))
                    PresentationAnimator.SetTrigger(_rollTriggerHash);
                else if (HasAnimatorParameter("Dodge"))
                    PresentationAnimator.SetTrigger(_dodgeTriggerHash);
                return;
            }

            if (PresentationAnimator.HasState(0, primaryHash))
                PresentationAnimator.CrossFadeInFixedTime(primaryHash, 0.05f, 0, 0f);
            else if (PresentationAnimator.HasState(0, fallbackHash))
                PresentationAnimator.CrossFadeInFixedTime(fallbackHash, 0.05f, 0, 0f);
            else if (_dodgeIsRoll && HasAnimatorParameter("Roll"))
                PresentationAnimator.SetTrigger(_rollTriggerHash);
            else if (HasAnimatorParameter("Dodge"))
                PresentationAnimator.SetTrigger(_dodgeTriggerHash);
        }

        private void SyncRollAnimatorPlayback()
        {
            if (PresentationAnimator == null)
                return;

            // Dedicated roll clips use baked timing; only speed up sidestep fallbacks.
            PresentationAnimator.speed = 1f;
        }

        private static int GetDodgeLeafHashForDirection(int dir)
        {
            switch (dir)
            {
                case 0: return _dodgeLeafFrontHash;
                case 1: return _dodgeLeafBackHash;
                case 2: return _dodgeLeafLeftHash;
                case 3: return _dodgeLeafRightHash;
                default: return _dodgeLeafFrontHash;
            }
        }

        private static int GetDodgeNestedStateHashForDirection(int dir)
        {
            switch (dir)
            {
                case 0: return _dodgeNestedFrontHash;
                case 1: return _dodgeNestedBackHash;
                case 2: return _dodgeNestedLeftHash;
                case 3: return _dodgeNestedRightHash;
                default: return _dodgeNestedFrontHash;
            }
        }

        private static int GetRollLeafHashForDirection(int dir)
        {
            switch (dir)
            {
                case 0: return _rollLeafForwardHash;
                case 1: return _rollLeafBackHash;
                case 2: return _rollLeafLeftHash;
                case 3: return _rollLeafRightHash;
                default: return _rollLeafForwardHash;
            }
        }

        private static int GetRollNestedHashForDirection(int dir)
        {
            switch (dir)
            {
                case 0: return _rollNestedForwardHash;
                case 1: return _rollNestedBackHash;
                case 2: return _rollNestedLeftHash;
                case 3: return _rollNestedRightHash;
                default: return _rollNestedForwardHash;
            }
        }

        private bool HasRollLeafStateForDirection(int dir)
        {
            if (PresentationAnimator == null)
                return false;

            int nested = GetRollNestedHashForDirection(dir);
            int leaf = GetRollLeafHashForDirection(dir);
            return PresentationAnimator.HasState(0, nested) || PresentationAnimator.HasState(0, leaf);
        }

        private Vector2 GetDodgeMoveComposite()
        {
            if (_inputReader == null)
                return Vector2.zero;
            return GeisInteractInput.GetEffectiveMoveCompositeForLocomotion(_inputReader._moveComposite);
        }

        private int ComputeDodgeDirectionIndex()
        {
            Vector2 m = GetDodgeMoveComposite();
            float deadzone = _dodgeInputDeadzone;

            if (m.sqrMagnitude < deadzone * deadzone)
            {
                if (_isLockedOn && _currentLockOnTarget != null)
                    return DirectionWorldToDodgeIndex(GetPlanarDirectionAwayFromLockOnTarget());
                return 1;
            }

            return DirectionWorldToDodgeIndex(GetCameraRelativeDodgeWorldDirection(m));
        }

        /// <summary>At or below <see cref="_strafeStyleMaxPlanarSpeed"/> (and not sprinting): dodge keeps camera-forward yaw.</summary>
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

        private int DirectionWorldToDodgeIndex(Vector3 worldDirection)
        {
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < 0.0001f)
                return 1;

            worldDirection.Normalize();
            Vector3 local = LocomotionTransform.InverseTransformDirection(worldDirection);
            float lx = local.x;
            float lz = local.z;
            if (Mathf.Abs(lz) >= Mathf.Abs(lx))
                return lz >= 0f ? 0 : 1;
            return lx >= 0f ? 3 : 2;
        }

        private static int DodgeDirectionIndexToFacingIndex(int dirIndex)
        {
            return dirIndex;
        }

        private Vector3 GetDodgeFacingWorld(int dirIndex)
        {
            Vector3 camFwd = _cameraController.GetCameraForwardZeroedYNormalised();
            Vector3 camRight = _cameraController.GetCameraRightZeroedYNormalised();
            switch (dirIndex)
            {
                case 0: return camFwd;
                case 1: return -camFwd;
                case 2: return -camRight;
                case 3: return camRight;
                default: return camFwd;
            }
        }

        private void EnterDodgeState()
        {
            float planarSpeedAtDodgePress = _speed2D;
            _dodgeStateEnteredAtUnscaled = Time.unscaledTime;
            _velocity.x = 0f;
            _velocity.z = 0f;

            _dodgeIsRoll = _dodgeRequestIsRoll;
            _dodgeRequestIsRoll = false;

            bool preserveCameraFacing = ShouldPreserveCameraFacingOnDodge(planarSpeedAtDodgePress) && !_isLockedOn;
            if (preserveCameraFacing)
                SnapBodyYawToCameraForward();

            int stickDirection = ComputeDodgeDirectionIndex();
            int dir = stickDirection;
            _dodgeAnimatorDir = dir;

            if (_dodgeIsRoll && !HasRollLeafStateForDirection(dir))
            {
                if (!_loggedForwardRollMissing)
                {
                    _loggedForwardRollMissing = true;
                    Debug.LogWarning(
                        "[GeisPlayerAnimationController] Animator is missing one or more directional roll states. " +
                        "Run menu: Geis → Animator → Setup Directional Dodge & Roll Clips.",
                        this);
                }
            }

            int facingIndex = DodgeDirectionIndexToFacingIndex(dir);
            Vector3 faceWorld = GetDodgeFacingWorld(facingIndex);

            // Walk/run (not sprint): snap to camera forward above, then hold yaw through the dodge clip.
            _dodgePreserveStrafeFacing = _isLockedOn || preserveCameraFacing || (_isStrafing && !_dodgeIsRoll);

            if (!_dodgePreserveStrafeFacing && faceWorld.sqrMagnitude > 0.0001f)
                LocomotionTransform.rotation = Quaternion.LookRotation(faceWorld);

            PlayDodgeLeafCrossFade(dir);
            SyncRollAnimatorPlayback();

            if (_dodgeIsRoll)
                _dodgeRollFollowUpExpiresAtUnscaled = -1f;
            else if (_dodgeDoubleTapRollEnabled)
                _dodgeRollFollowUpExpiresAtUnscaled = Time.unscaledTime + _dodgeDoubleTapWindow;

            _dodgeAnimatorEnteredLeaf = false;
            _dodgeStateTimeout = _dodgeFallbackDuration;
        }

        private Vector3 GetPlanarForward()
        {
            Vector3 f = LocomotionTransform.forward;
            f.y = 0f;
            float sq = f.sqrMagnitude;
            return sq > 0.0001f ? f / Mathf.Sqrt(sq) : Vector3.forward;
        }

        private static bool IsDodgeLeafShortNameHash(int shortNameHash)
        {
            return shortNameHash == _dodgeLeafFrontHash || shortNameHash == _dodgeLeafBackHash
                || shortNameHash == _dodgeLeafLeftHash || shortNameHash == _dodgeLeafRightHash
                || shortNameHash == _rollLeafForwardHash || shortNameHash == _rollLeafBackHash
                || shortNameHash == _rollLeafLeftHash || shortNameHash == _rollLeafRightHash;
        }

        private void UpdateDodgeState()
        {
            ApplyGravity();
            _dodgeStateTimeout -= Time.deltaTime;

            // Prune stale buffers before testing recovery cancels.
            if (_comboInputBuffered.HasValue && !IsBufferFresh(_comboInputBufferedAt))
                ClearComboInputBuffer();
            if (_dodgeInputBufferedAt >= 0f && !IsBufferFresh(_dodgeInputBufferedAt))
            {
                _dodgeInputBufferedAt = -1f;
                _dodgeInputBufferIsRoll = false;
            }

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
                    if (IsDodgeLeafShortNameHash(next.shortNameHash))
                        _dodgeAnimatorEnteredLeaf = true;
                }
                else if (IsDodgeLeafShortNameHash(info.shortNameHash))
                {
                    _dodgeAnimatorEnteredLeaf = true;
                }

                // Dodge recovery window: once the leaf clip passes the recovery threshold, allow attack/move cancels.
                float recoveryThreshold = _dodgeIsRoll
                    ? _rollRecoveryStartNormalizedTime
                    : _dodgeRecoveryStartNormalizedTime;
                bool inRecoveryWindow = _dodgeAnimatorEnteredLeaf
                    && IsDodgeLeafShortNameHash(info.shortNameHash)
                    && !PresentationAnimator.IsInTransition(0)
                    && info.normalizedTime >= recoveryThreshold;

                if (inRecoveryWindow)
                {
                    // Attack-cancel has priority over move-cancel so a queued attack fires instantly after the dodge active frames.
                    var comboData = GetCurrentComboData();
                    if (_useDataDrivenCombo && comboData != null && !_isCrouching
                        && TryConsumeComboInputBuffer(out var queuedAttack))
                    {
                        _firstAttackInputType = queuedAttack;
                        _currentComboState = 0;
                        SwitchState(AnimationState.Attack);
                        return;
                    }

                    // Let the forward roll finish its authored root-motion arc. If we move-cancel it as soon as the
                    // recovery window opens while the stick is still held, the character visibly hitches back into
                    // locomotion mid-roll.
                    if (!_dodgeIsRoll)
                    {
                        Vector2 composite = _inputReader != null
                            ? GeisInteractInput.GetEffectiveMoveCompositeForLocomotion(_inputReader._moveComposite)
                            : Vector2.zero;
                        float threshold = _dodgeMoveCancelStickThreshold;
                        if (composite.sqrMagnitude >= threshold * threshold)
                        {
                            SeedPlanarVelocityAfterDodgeExit();
                            SwitchState(AnimationState.Locomotion);
                            return;
                        }
                    }
                }

                // Do not rely on normalizedTime alone: after the dodge clip the Animator transitions to Idle_Standing
                // (exit ~0.92). Layer 0 then reports Idle's normalizedTime, so the old >= 0.99 check never passes and
                // gameplay stayed in Dodge until _dodgeFallbackDuration (~1.2s) with zero scripted locomotion.
                if (_dodgeAnimatorEnteredLeaf && !PresentationAnimator.IsInTransition(0)
                    && !IsDodgeLeafShortNameHash(info.shortNameHash))
                {
                    SeedPlanarVelocityAfterDodgeExit();
                    SwitchState(AnimationState.Locomotion);
                    return;
                }

                if (info.length > 0.01f && info.normalizedTime >= 0.99f && !PresentationAnimator.IsInTransition(0)
                    && IsDodgeLeafShortNameHash(info.shortNameHash))
                {
                    SeedPlanarVelocityAfterDodgeExit();
                    SwitchState(AnimationState.Locomotion);
                    return;
                }
            }

            UpdateDodgeInvulnerabilityFromAnimator();

            if (_dodgeStateTimeout <= 0f)
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
            if (_dodgeIsRoll)
            {
                _velocity.x = 0f;
                _velocity.z = 0f;
            }

            _dodgeIsRoll = false;
            _dodgeStateEnteredAtUnscaled = -1f;
            SyncRollAnimatorPlayback();
            if (_defensiveCombatState != null)
                _defensiveCombatState.SetDodgeInvulnerable(false);
        }

        private void EnterAttackState()
        {
            _attackAnimatorEnteredLeaf = false;
            _velocity.x = 0f;
            _velocity.z = 0f;
            ClearMovementInputStateForAttack();
            _jumpBufferedAt = -1f;

            if (_useDataDrivenCombo && PresentationAnimator != null && HasAnimatorParameter("Attack")
                && (HasAnimatorParameter("ComboStateBlend") || HasAnimatorParameter("ComboState")))
            {
                var comboData = GetCurrentComboData();
                ComboAttackPlayback.EnterComboAttack(PresentationAnimator, _attackTriggerHash, _currentComboState, COMBO_BLEND_SLOTS);
                _attackStateTimeout = ComboAttackPlayback.GetEnterAttackTimeout(comboData);
                int weaponIdx = GetCurrentWeaponIndex();
                OnAttackPerformed?.Invoke(weaponIdx);
            }
            else if (PresentationAnimator != null && HasAnimatorParameter("Attack_1"))
            {
                PresentationAnimator.SetTrigger(_attack1Hash);
                _attackStateTimeout = 1.5f;
                int weaponIdx = GetCurrentWeaponIndex();
                OnAttackPerformed?.Invoke(weaponIdx);
            }
        }

        private int GetCurrentWeaponIndex()
        {
            return _weaponSwitcher != null ? _weaponSwitcher.CurrentWeaponIndex : 0;
        }

        private void UpdateAttackState()
        {
            ApplyGravity();
            ClearMovementInputStateForAttack();
            _attackStateTimeout -= Time.deltaTime;

            // Expire stale buffers so presses from far earlier do not trigger a cancel when the window finally opens.
            if (_comboInputBuffered.HasValue && !IsBufferFresh(_comboInputBufferedAt))
                ClearComboInputBuffer();
            if (_dodgeInputBufferedAt >= 0f && !IsBufferFresh(_dodgeInputBufferedAt))
            {
                _dodgeInputBufferedAt = -1f;
                _dodgeInputBufferIsRoll = false;
            }

            var comboData = GetCurrentComboData();
            bool inAttackAnimatorLeaf = false;

            if (PresentationAnimator != null)
            {
                AnimatorStateInfo info = PresentationAnimator.GetCurrentAnimatorStateInfo(0);

                if (PresentationAnimator.IsInTransition(0))
                {
                    AnimatorStateInfo next = PresentationAnimator.GetNextAnimatorStateInfo(0);
                    if (IsAttackLeafShortNameHash(next.shortNameHash))
                        _attackAnimatorEnteredLeaf = true;
                }
                else if (IsAttackLeafShortNameHash(info.shortNameHash))
                {
                    _attackAnimatorEnteredLeaf = true;
                }

                inAttackAnimatorLeaf = _attackAnimatorEnteredLeaf
                    && !PresentationAnimator.IsInTransition(0)
                    && IsAttackLeafShortNameHash(info.shortNameHash);

                // Do not trust locomotion/idle normalizedTime before the Attack leaf actually plays, or after it ends.
                if (_attackAnimatorEnteredLeaf && !PresentationAnimator.IsInTransition(0)
                    && !IsAttackLeafShortNameHash(info.shortNameHash))
                {
                    SwitchState(AnimationState.Locomotion);
                    return;
                }
            }

            if (_useDataDrivenCombo && comboData != null && PresentationAnimator != null && inAttackAnimatorLeaf)
            {
                AnimatorStateInfo info = PresentationAnimator.GetCurrentAnimatorStateInfo(0);
                float normalizedTime = info.normalizedTime % 1f;
                comboData.GetCancelWindow(_currentComboState, out float cancelWindowStart, out float cancelWindowEnd);
                bool inCancelWindow = normalizedTime >= cancelWindowStart && normalizedTime <= cancelWindowEnd;

                if (inCancelWindow)
                {
                    // Dodge cancel has priority over combo continuation (most recent intent; standard action-game feel).
                    if (TryConsumeDodgeInputBuffer(out bool bufferedDodgeIsRoll)
                        && _isGrounded && !_isCrouching
                        && HasAnimatorParameter("Dodge") && HasAnimatorParameter("DodgeDirection"))
                    {
                        ClearComboInputBuffer();
                        _dodgeRequestIsRoll = bufferedDodgeIsRoll;
                        SwitchState(AnimationState.Dodge);
                        return;
                    }

                    // Combo continuation.
                    if (TryConsumeComboInputBuffer(out var input)
                        && ComboAttackPlayback.TryContinueCombo(
                            PresentationAnimator, comboData, input, ref _currentComboState, _attackTriggerHash,
                            COMBO_BLEND_SLOTS, out _attackStateTimeout))
                    {
                        int weaponIdx = GetCurrentWeaponIndex();
                        OnAttackPerformed?.Invoke(weaponIdx);
                        return;
                    }

                    Vector2 moveComposite = _inputReader != null
                        ? GeisInteractInput.GetEffectiveMoveCompositeForLocomotion(_inputReader._moveComposite)
                        : Vector2.zero;
                    float moveCancelThreshold = _attackMoveCancelStickThreshold;
                    if (moveComposite.sqrMagnitude >= moveCancelThreshold * moveCancelThreshold)
                    {
                        SeedPlanarVelocityFromStick(_attackExitVelocityCarry);
                        SwitchState(AnimationState.Locomotion);
                        return;
                    }
                }

                // Clip-authoritative exit: once the attack clip passes its recovery threshold and is not transitioning
                // to another combo step, return to locomotion immediately instead of idling until _attackStateTimeout.
                if (normalizedTime >= _attackRecoveryExitNormalizedTime && !PresentationAnimator.IsInTransition(0))
                {
                    SwitchState(AnimationState.Locomotion);
                    return;
                }
            }

            // Safety fallback.
            if (_attackStateTimeout <= 0f)
            {
                SwitchState(AnimationState.Locomotion);
                return;
            }

            UpdateAnimatorController();
        }

        private static bool IsAttackLeafShortNameHash(int shortNameHash)
        {
            return shortNameHash == _attackLeafHash;
        }

        private void ExitAttackState()
        {
            _attackAnimatorEnteredLeaf = false;
            _currentComboState = 0;
            // Intentionally NOT clearing _comboInputBuffered / _dodgeInputBufferedAt here: when we leave Attack via a
            // cancel (e.g. Attack → Dodge), the follow-up state may still want to consume the buffered input later
            // (e.g. Dodge → Attack during dodge recovery). Stale buffers are pruned via IsBufferFresh checks.
        }

        /// <summary>
        /// Sets the blend tree parameter so the correct clip is selected. Unity's Simple1D uses
        /// thresholds 0..1 over 32 slots, so we pass state/31. Use ComboStateBlend (Float) if present,
        /// else fall back to ComboState (Int) - which only works if blend tree has thresholds 0,1,2,...
        /// </summary>
        private void SetComboStateBlend(int state)
        {
            GeisComboAnimatorBlend.Apply(PresentationAnimator, state, COMBO_BLEND_SLOTS);
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

            if (_inputReader != null)
                _inputReader._movementInputDuration = 0f;
        }

        /// <summary>True when <paramref name="bufferedAtUnscaled"/> is non-negative and within <see cref="_inputBufferSeconds"/> of now.</summary>
        private bool IsBufferFresh(float bufferedAtUnscaled)
        {
            return bufferedAtUnscaled >= 0f
                && (Time.unscaledTime - bufferedAtUnscaled) <= _inputBufferSeconds;
        }

        /// <summary>Stamps the combo input buffer so <see cref="UpdateAttackState"/> can consume it during the cancel window.</summary>
        private void SetComboInputBuffer(GeisComboInputType input)
        {
            _comboInputBuffered = input;
            _comboInputBufferedAt = Time.unscaledTime;
        }

        /// <summary>Clears the combo input buffer (both value and timestamp).</summary>
        private void ClearComboInputBuffer()
        {
            _comboInputBuffered = null;
            _comboInputBufferedAt = -1f;
        }

        /// <summary>Returns the buffered combo input if its buffer timestamp is fresh, otherwise clears and returns false.</summary>
        private bool TryConsumeComboInputBuffer(out GeisComboInputType input)
        {
            input = default;
            if (!_comboInputBuffered.HasValue) return false;
            if (!IsBufferFresh(_comboInputBufferedAt))
            {
                ClearComboInputBuffer();
                return false;
            }
            input = _comboInputBuffered.Value;
            ClearComboInputBuffer();
            return true;
        }

        /// <summary>Returns true (and clears) if a fresh dodge buffer is available; otherwise clears stale buffers and returns false.</summary>
        private bool TryConsumeDodgeInputBuffer()
        {
            return TryConsumeDodgeInputBuffer(out _);
        }

        /// <summary>
        /// Returns true (and clears) if a fresh dodge buffer is available. <paramref name="isRoll"/> is set to whether
        /// the buffered press was part of a double-tap (forward-roll variant).
        /// </summary>
        private bool TryConsumeDodgeInputBuffer(out bool isRoll)
        {
            isRoll = false;
            if (_dodgeInputBufferedAt < 0f) return false;
            if (!IsBufferFresh(_dodgeInputBufferedAt))
            {
                _dodgeInputBufferedAt = -1f;
                _dodgeInputBufferIsRoll = false;
                return false;
            }
            isRoll = _dodgeInputBufferIsRoll;
            _dodgeInputBufferedAt = -1f;
            _dodgeInputBufferIsRoll = false;
            return true;
        }

        /// <summary>Pre-seeds planar velocity from the current move stick so exiting an attack/dodge into movement does not stall.</summary>
        private void SeedPlanarVelocityFromStick(float carryFraction)
        {
            if (_inputReader == null || _cameraController == null)
                return;

            Vector2 composite = GeisInteractInput.GetEffectiveMoveCompositeForLocomotion(_inputReader._moveComposite);
            if (composite.sqrMagnitude < 0.0001f)
                return;

            Vector3 camRelative = GeisLocomotionKinematics.ComputeCameraRelativeMoveDirection(composite, _cameraController);

            float scale = Mathf.Max(0f, carryFraction) * _currentMaxSpeed;
            _velocity.x = camRelative.x * scale;
            _velocity.z = camRelative.z * scale;
        }

        private void SeedPlanarVelocityAfterDodgeExit()
        {
            float carry = _dodgeIsRoll ? _rollExitVelocityCarry : _dodgeExitVelocityCarry;
            if (carry <= 0.0001f)
            {
                _velocity.x = 0f;
                _velocity.z = 0f;
                return;
            }

            SeedPlanarVelocityFromStick(carry);
        }

        /// <summary>
        ///     Applies root motion during Attack and Dodge to the CharacterController transform.
        ///     Locomotion, Jump, Fall, Crouch use script-driven movement via Move() - no root motion here.
        /// </summary>
        private void OnAnimatorMove()
        {
            if (_animatorIsOnChild)
                return;

            ApplyAttackDodgeRootMotionToBody();
        }

        /// <summary>
        /// When the Animator is on a child rig, Unity only invokes <see cref="OnAnimatorMove"/> on that child.
        /// Apply attack/dodge root motion to the body here and keep the visual rig snapped to the capsule.
        /// </summary>
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

                if (_dodgeIsRoll)
                {
                    AnimatorStateInfo rollInfo = PresentationAnimator.GetCurrentAnimatorStateInfo(0);
                    if (IsDodgeLeafShortNameHash(rollInfo.shortNameHash) && rollInfo.normalizedTime > 0.85f)
                    {
                        float fade = 1f - Mathf.InverseLerp(0.85f, 0.98f, rollInfo.normalizedTime);
                        deltaPosition.x *= fade;
                        deltaPosition.z *= fade;
                    }
                    else if (!HasRollLeafStateForDirection(_dodgeAnimatorDir)
                        && !Mathf.Approximately(_rollDistanceMultiplier, 1f))
                    {
                        deltaPosition.x *= _rollDistanceMultiplier;
                        deltaPosition.z *= _rollDistanceMultiplier;
                    }
                }

                deltaPosition.y += _velocity.y * Time.deltaTime;

                LocomotionController.Move(deltaPosition);

                if (_applyRootRotationDuringDodge && !_dodgePreserveStrafeFacing
                    && PresentationAnimator.deltaRotation != Quaternion.identity)
                    LocomotionTransform.rotation = LocomotionTransform.rotation * PresentationAnimator.deltaRotation;
            }
        }

        /// <summary>Prevents locomotion root motion from accumulating on the mesh child while the capsule moves on the root.</summary>
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
