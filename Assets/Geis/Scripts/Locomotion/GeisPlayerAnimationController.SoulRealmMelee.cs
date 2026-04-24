using Geis.Animation;
using Geis.Combat;
using Geis.Combat.Music;
using Geis.SoulRealm;
using UnityEngine;

namespace Geis.Locomotion
{
    /// <summary>
    /// Soul realm uses the spectral animator for melee (body locomotion <see cref="MonoBehaviour.Update"/> is suppressed).
    /// </summary>
    public partial class GeisPlayerAnimationController
    {
        private bool TryProcessSoulRealmMeleeInput(GeisComboInputType inputType)
        {
            SoulRealmManager mgr = SoulRealmManager.Instance;
            if (mgr == null || !mgr.IsSoulRealmActive)
                return false;

            if (!mgr.AllowGhostMovement)
                return true;

            Animator spec = mgr.SpectralAnimator;
            if (spec == null)
                return true;

            SoulGhostMotor ghostMotor = mgr.GhostMotor;
            if (ghostMotor == null || !ghostMotor.IsGroundedPublic || _isCrouching)
                return true;

            GeisComboData comboData = GetCurrentComboData();

            if (_attackStateTimeout > 0f)
            {
                if (_useDataDrivenCombo && comboData != null)
                    SetComboInputBuffer(inputType);
                return true;
            }

            if (_useDataDrivenCombo && comboData != null &&
                AnimatorParameterGuard.HasParameter(spec, "Attack") &&
                (AnimatorParameterGuard.HasParameter(spec, "ComboStateBlend") ||
                 AnimatorParameterGuard.HasParameter(spec, "ComboState")))
            {
                _firstAttackInputType = inputType;
                _currentComboState = 0;
                ClearComboInputBuffer();
                SoulRealmTriggerComboAttackEnter(spec, comboData);
                return true;
            }

            if (AnimatorParameterGuard.HasParameter(spec, "Attack_1"))
            {
                _firstAttackInputType = inputType;
                _currentComboState = 0;
                ClearComboInputBuffer();
                spec.SetTrigger(_attack1Hash);
                _attackStateTimeout = 1.5f;
                int weaponIdx = GetWeaponIndexForMusic();
                CombatMusicController.Instance?.OnAttackPerformed(_firstAttackInputType, 0, weaponIdx);
                OnAttackPerformed?.Invoke(weaponIdx);
                return true;
            }

            return true;
        }

        private void SoulRealmTriggerComboAttackEnter(Animator spec, GeisComboData comboData)
        {
            SoulSetComboBlendOnAnimator(spec, _currentComboState);
            spec.SetTrigger(_attackTriggerHash);
            _attackStateTimeout = comboData != null ? 2f : 1.5f;
            int weaponIdx = GetWeaponIndexForMusic();
            CombatMusicController.Instance?.OnAttackPerformed(_firstAttackInputType, _currentComboState, weaponIdx);
            OnAttackPerformed?.Invoke(weaponIdx);
        }

        private static void SoulSetComboBlendOnAnimator(Animator spec, int state)
        {
            if (AnimatorParameterGuard.HasParameter(spec, "ComboStateBlend"))
                spec.SetFloat(Animator.StringToHash("ComboStateBlend"), (float)state / (COMBO_BLEND_SLOTS - 1));
            else if (AnimatorParameterGuard.HasParameter(spec, "ComboState"))
                spec.SetInteger(Animator.StringToHash("ComboState"), state);
        }

        private void UpdateSoulRealmMeleeCombat()
        {
            SoulRealmManager mgr = SoulRealmManager.Instance;
            if (mgr == null || !mgr.IsSoulRealmActive)
                return;

            Animator spec = mgr.SpectralAnimator;
            if (spec == null || _attackStateTimeout <= 0f)
                return;

            _attackStateTimeout -= Time.deltaTime;

            // Prune stale buffers just like the physical body UpdateAttackState so soul-realm melee feels identical.
            if (_comboInputBuffered.HasValue && !IsBufferFresh(_comboInputBufferedAt))
                ClearComboInputBuffer();

            GeisComboData comboData = GetCurrentComboData();

            if (_useDataDrivenCombo && comboData != null)
            {
                AnimatorStateInfo info = spec.GetCurrentAnimatorStateInfo(0);
                float normalizedTime = info.normalizedTime % 1f;
                comboData.GetCancelWindow(_currentComboState, out float cancelWindowStart, out float cancelWindowEnd);
                bool inCancelWindow = normalizedTime >= cancelWindowStart &&
                                      normalizedTime <= cancelWindowEnd;

                if (inCancelWindow && TryConsumeComboInputBuffer(out var input))
                {
                    if (comboData.TryGetNextState(_currentComboState, input, out int nextState))
                    {
                        _currentComboState = nextState;
                        SoulSetComboBlendOnAnimator(spec, _currentComboState);
                        spec.SetTrigger(_attackTriggerHash);
                        AnimationClip clip = comboData.GetClipForState(_currentComboState);
                        _attackStateTimeout = clip != null ? clip.length + 0.2f : 1.5f;
                        int weaponIdx = GetWeaponIndexForMusic();
                        CombatMusicController.Instance?.OnAttackPerformed(input, _currentComboState, weaponIdx);
                        OnAttackPerformed?.Invoke(weaponIdx);
                    }
                }
            }

            if (_attackStateTimeout <= 0f)
            {
                _currentComboState = 0;
                ClearComboInputBuffer();
            }
        }
    }
}
