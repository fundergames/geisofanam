/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using Geis.Combat;

namespace Geis.Locomotion
{
    /// <summary>
    /// Jump, combo, and dodge input buffers plus double-tap dodge roll detection.
    /// </summary>
    public sealed class GeisInputBufferTracker
    {
        private float _inputBufferSeconds = GeisLocomotionTuningDefaults.InputBufferSeconds;
        private float _jumpBufferSeconds = GeisLocomotionTuningDefaults.JumpBufferSeconds;
        private float _dodgeDoubleTapWindow = GeisLocomotionTuningDefaults.DodgeDoubleTapWindow;

        private float _jumpBufferedAt = -1f;

        private GeisComboInputType? _comboInputBuffered;
        private float _comboInputBufferedAt = -1f;

        private float _dodgeInputBufferedAt = -1f;
        private bool _dodgeInputBufferIsRoll;

        private float _lastDodgeTapAt = -1f;
        private float _dodgeRollFollowUpExpiresAtUnscaled = -1f;

        public float InputBufferSeconds
        {
            get => _inputBufferSeconds;
            set => _inputBufferSeconds = value;
        }

        public float JumpBufferSeconds
        {
            get => _jumpBufferSeconds;
            set => _jumpBufferSeconds = value;
        }

        public float DodgeDoubleTapWindow
        {
            get => _dodgeDoubleTapWindow;
            set => _dodgeDoubleTapWindow = value;
        }

        public float JumpBufferedAt => _jumpBufferedAt;
        public float LastDodgeTapAt => _lastDodgeTapAt;
        public float DodgeRollFollowUpExpiresAtUnscaled => _dodgeRollFollowUpExpiresAtUnscaled;

        public void ResetCombatBuffers()
        {
            ClearComboInputBuffer();
            _dodgeInputBufferedAt = -1f;
            _dodgeInputBufferIsRoll = false;
            _lastDodgeTapAt = -1f;
            _dodgeRollFollowUpExpiresAtUnscaled = -1f;
        }

        public void ResetJumpBuffer() => _jumpBufferedAt = -1f;

        public void BufferJump(float nowUnscaled) => _jumpBufferedAt = nowUnscaled;

        public bool IsJumpBufferFresh(float nowUnscaled) =>
            GeisInputBufferUtility.IsFresh(_jumpBufferedAt, _jumpBufferSeconds, nowUnscaled);

        public bool TryConsumeJumpBuffer(float nowUnscaled)
        {
            if (!IsJumpBufferFresh(nowUnscaled))
            {
                _jumpBufferedAt = -1f;
                return false;
            }

            _jumpBufferedAt = -1f;
            return true;
        }

        public void SetComboInputBuffer(GeisComboInputType input, float nowUnscaled)
        {
            _comboInputBuffered = input;
            _comboInputBufferedAt = nowUnscaled;
        }

        public void ClearComboInputBuffer()
        {
            _comboInputBuffered = null;
            _comboInputBufferedAt = -1f;
        }

        public bool TryConsumeComboInputBuffer(float nowUnscaled, out GeisComboInputType input)
        {
            input = default;
            if (!_comboInputBuffered.HasValue)
                return false;

            if (!GeisInputBufferUtility.IsFresh(_comboInputBufferedAt, _inputBufferSeconds, nowUnscaled))
            {
                ClearComboInputBuffer();
                return false;
            }

            input = _comboInputBuffered.Value;
            ClearComboInputBuffer();
            return true;
        }

        public void PruneStaleComboBuffer(float nowUnscaled)
        {
            if (_comboInputBuffered.HasValue
                && !GeisInputBufferUtility.IsFresh(_comboInputBufferedAt, _inputBufferSeconds, nowUnscaled))
            {
                ClearComboInputBuffer();
            }
        }

        public void BufferDodgeFromAttackCancel(bool isRoll, float nowUnscaled)
        {
            _dodgeInputBufferedAt = nowUnscaled;
            _dodgeInputBufferIsRoll = isRoll;
        }

        public bool TryConsumeDodgeInputBuffer(float nowUnscaled, out bool isRoll)
        {
            isRoll = false;
            if (_dodgeInputBufferedAt < 0f)
                return false;

            if (!GeisInputBufferUtility.IsFresh(_dodgeInputBufferedAt, _inputBufferSeconds, nowUnscaled))
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

        public void PruneStaleDodgeBuffer(float nowUnscaled)
        {
            if (_dodgeInputBufferedAt >= 0f
                && !GeisInputBufferUtility.IsFresh(_dodgeInputBufferedAt, _inputBufferSeconds, nowUnscaled))
            {
                _dodgeInputBufferedAt = -1f;
                _dodgeInputBufferIsRoll = false;
            }
        }

        /// <summary>
        /// Records a dodge press and returns whether this press should trigger a roll (double-tap).
        /// </summary>
        public bool RecordDodgeTapAndGetRollIntent(float nowUnscaled, bool dodgeDoubleTapRollEnabled)
        {
            float dtSinceLast = _lastDodgeTapAt > 0f ? (nowUnscaled - _lastDodgeTapAt) : -1f;
            bool rollIntent = dodgeDoubleTapRollEnabled
                && _lastDodgeTapAt > 0f
                && dtSinceLast <= _dodgeDoubleTapWindow
                && nowUnscaled <= _dodgeRollFollowUpExpiresAtUnscaled;

            _lastDodgeTapAt = nowUnscaled;
            return rollIntent;
        }

        public void ArmRollFollowUpWindow(float nowUnscaled, bool dodgeDoubleTapRollEnabled)
        {
            if (dodgeDoubleTapRollEnabled)
                _dodgeRollFollowUpExpiresAtUnscaled = nowUnscaled + _dodgeDoubleTapWindow;
        }

        public void ClearRollFollowUpWindow() => _dodgeRollFollowUpExpiresAtUnscaled = -1f;
    }
}
