/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using System;
using System.Collections.Generic;
using Geis.Animation;
using Geis.Combat;
using UnityEngine;

namespace Geis.Locomotion
{
    /// <summary>
    /// Data-driven combo override application and attack playback state.
    /// </summary>
    public sealed class GeisComboAttackController
    {
        public const int DefaultBlendSlots = GeisComboAnimatorBlend.DefaultSlotCount;

        public event Action<int> AttackPerformed;

        public int CurrentComboState { get; private set; }
        public GeisComboInputType FirstAttackInputType { get; private set; }
        public bool UseDataDrivenCombo { get; private set; }
        public bool AttackAnimatorEnteredLeaf { get; set; }
        public float AttackStateTimeout { get; set; }

        private GeisWeaponSwitcher _weaponSwitcher;
        private GeisComboPlaceholders _comboPlaceholders;
        private AnimatorOverrideController _comboOverrideController;
        private GeisComboData _lastAppliedComboData;

        public void Configure(
            GeisWeaponSwitcher weaponSwitcher,
            GeisComboPlaceholders comboPlaceholders,
            Animator presentationAnimator)
        {
            _weaponSwitcher = weaponSwitcher;
            _comboPlaceholders = comboPlaceholders;

            UseDataDrivenCombo = _weaponSwitcher != null && presentationAnimator != null
                && AnimatorParameterGuard.HasParameter(presentationAnimator, "Attack")
                && (AnimatorParameterGuard.HasParameter(presentationAnimator, "ComboStateBlend")
                    || AnimatorParameterGuard.HasParameter(presentationAnimator, "ComboState"));
        }

        public void ResetComboState()
        {
            CurrentComboState = 0;
            AttackAnimatorEnteredLeaf = false;
            AttackStateTimeout = 0f;
        }

        public GeisComboData GetCurrentComboData()
        {
            if (_weaponSwitcher == null)
                return null;

            return _weaponSwitcher.TryGetComboForWeapon(_weaponSwitcher.CurrentWeaponIndex, out var combo)
                ? combo
                : null;
        }

        public void ApplyOverridesIfReady(Animator presentationAnimator)
        {
            if (!UseDataDrivenCombo || presentationAnimator == null)
                return;

            var comboData = GetCurrentComboData();
            if (comboData == null || comboData == _lastAppliedComboData)
                return;

            var placeholders = _comboPlaceholders != null
                ? _comboPlaceholders
                : Resources.Load<GeisComboPlaceholders>("GeisComboPlaceholders");
            if (placeholders == null)
                return;

            var current = presentationAnimator.runtimeAnimatorController;
            RuntimeAnimatorController baseController = null;
            if (current is AnimatorOverrideController aoc)
                baseController = aoc.runtimeAnimatorController;
            else if (current != null)
                baseController = current;

            if (baseController == null)
                return;

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

            presentationAnimator.runtimeAnimatorController = _comboOverrideController;
            _lastAppliedComboData = comboData;
        }

        public void DestroyOverrideController()
        {
            if (_comboOverrideController != null)
            {
                UnityEngine.Object.Destroy(_comboOverrideController);
                _comboOverrideController = null;
            }

            _lastAppliedComboData = null;
        }

        public void BeginFirstAttack(GeisComboInputType inputType)
        {
            FirstAttackInputType = inputType;
            CurrentComboState = 0;
        }

        public bool EnterAttack(Animator presentationAnimator, bool hasAttackParam, bool hasAttack1Param)
        {
            AttackAnimatorEnteredLeaf = false;

            if (UseDataDrivenCombo && presentationAnimator != null && hasAttackParam)
            {
                var comboData = GetCurrentComboData();
                ComboAttackPlayback.EnterComboAttack(
                    presentationAnimator,
                    LocomotionAnimatorIds.Attack,
                    CurrentComboState,
                    DefaultBlendSlots);
                AttackStateTimeout = ComboAttackPlayback.GetEnterAttackTimeout(comboData);
                AttackPerformed?.Invoke(GetCurrentWeaponIndex());
                return true;
            }

            if (presentationAnimator != null && hasAttack1Param)
            {
                presentationAnimator.SetTrigger(LocomotionAnimatorIds.Attack1);
                AttackStateTimeout = 1.5f;
                AttackPerformed?.Invoke(GetCurrentWeaponIndex());
                return true;
            }

            return false;
        }

        public bool TryContinueCombo(Animator presentationAnimator, GeisComboInputType input, out float newTimeout)
        {
            newTimeout = 0f;
            var comboData = GetCurrentComboData();
            if (comboData == null || presentationAnimator == null)
                return false;

            int state = CurrentComboState;
            if (!ComboAttackPlayback.TryContinueCombo(
                    presentationAnimator,
                    comboData,
                    input,
                    ref state,
                    LocomotionAnimatorIds.Attack,
                    DefaultBlendSlots,
                    out newTimeout))
            {
                return false;
            }

            CurrentComboState = state;
            AttackStateTimeout = newTimeout;
            AttackPerformed?.Invoke(GetCurrentWeaponIndex());
            return true;
        }

        public static bool IsAttackLeafShortNameHash(int shortNameHash) =>
            shortNameHash == LocomotionAnimatorIds.Attack;

        private int GetCurrentWeaponIndex() =>
            _weaponSwitcher != null ? _weaponSwitcher.CurrentWeaponIndex : 0;
    }
}
