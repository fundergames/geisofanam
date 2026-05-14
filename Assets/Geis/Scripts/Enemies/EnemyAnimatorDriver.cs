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

using System.Collections.Generic;
using Geis.Animation;
using Geis.Combat;
using RogueDeal.Combat;
using UnityEngine;

namespace Geis.Enemies
{
    public class EnemyAnimatorDriver : MonoBehaviour
    {
        [Header("Locomotion")]
        [SerializeField] private string moveSpeedParameter = "MoveSpeed";
        [Tooltip("Polygon/Synty gait index (Idle=0, Walk=1, Run=2, Sprint=3). Skipped if empty or parameter missing.")]
        [SerializeField] private string currentGaitParameter = "CurrentGait";

        [Header("Behaviour flags (omit from Animator Controller to skip)")]
        [SerializeField] private string hasTargetParameter = "HasTarget";
        [SerializeField] private string strafeParameter = "IsStrafing";
        [SerializeField] private string telegraphParameter = "IsTelegraphing";
        [SerializeField] private string deadParameter = "IsDead";
        [SerializeField] private string attackingParameter = "IsAttacking";
        [SerializeField] private string recoveringParameter = "IsRecovering";
        [SerializeField] private string staggeringParameter = "IsStaggering";

        [Header("Locomotion compatibility")]
        [Tooltip("If set, forced true while alive so controllers like AC_Polygon_Masculine_Geis stay in grounded locomotion (skipped if the parameter is missing).")]
        [SerializeField] private string locomotionGroundedParameter = "IsGrounded";
        [Tooltip("Polygon: default animator values leave IsGrounded false until the first brain tick; Animator may evaluate earlier and stick in fall. Bootstrap runs OnEnable and whenever the runtime controller is assigned.")]
        [SerializeField] private bool bootstrapGroundedOnEnable = true;
        [Tooltip("While grounded (alive), keep fall blend inputs at zero so AC_Polygon-style Falling_BlendTree does not latch.")]
        [SerializeField] private bool suppressFallingBlendWhileGrounded = true;
        [SerializeField] private string fallingDurationParameter = "FallingDuration";
        [SerializeField] private string fallingBlendParameter = "FallingBlend";

        [Tooltip("AC_Polygon_Masculine_Geis enters locomotion via MovementInputHeld / IsWalking; enemies have no input device, so we synthesize those from brain state + MoveSpeed. Disable for fully custom Animator setups.")]
        [SerializeField] private bool drivePolygonStyleMovementIntent = true;
        [Range(0f, 0.5f)]
        [SerializeField] private float polygonWalkMoveSpeedThreshold = 0.05f;
        [SerializeField] private string movementInputHeldParameter = "MovementInputHeld";
        [SerializeField] private string movementInputPressedParameter = "MovementInputPressed";
        [SerializeField] private string movementInputTappedParameter = "MovementInputTapped";
        [SerializeField] private string polygonIsWalkingParameter = "IsWalking";
        [SerializeField] private string polygonIsStoppedParameter = "IsStopped";

        [Header("Discrete brain state (optional)")]
        [Tooltip("Integer parameter written as (int)EnemyBrain.EnemyState — Idle=0, Acquire=1, Approach=2, Strafe=3, Telegraph=4, Attack=5, Recover=6, Stagger=7, Dead=8. Leave empty to disable.")]
        [SerializeField] private string enemyStateParameter = "";

        [Header("Combat triggers")]
        [SerializeField] private string attackTrigger = "Attack";
        [SerializeField] private string hitTrigger = "Hit";

        [Header("Weapon / combo (Polygon-style, optional)")]
        [SerializeField] private string equippedWeaponIndexParameter = "EquippedWeaponIndex";
        [SerializeField] private string comboStateBlendParameter = "ComboStateBlend";
        [SerializeField] private string comboStateIntParameter = "ComboState";
        [Tooltip("When null, tries Resources ComboPlaceholders/GeisComboPlaceholders then GeisComboPlaceholders. Same placeholders as the player combo blend tree.")]
        [SerializeField] private GeisComboPlaceholders enemyComboPlaceholders;

        private const int ComboBlendSlotCount = 32;

        private CombatEntity _combatEntity;
        private Animator _animator;
        private AnimatorOverrideController _weaponComboRuntimeOverride;

        private void Awake()
        {
            CacheAnimatorReference();
        }

        private void OnEnable()
        {
            if (bootstrapGroundedOnEnable)
                ApplyLocomotionBootstrap(forceGrounded: true);
        }

        /// <summary>
        /// Pushes brain locomotion and high-level state into Animator parameters so transitions can mirror Acquire → Approach → Strafe → Telegraph → Attack → Recover → Stagger → Dead.
        /// Attack-specific clips may still be fired via <see cref="TriggerAttack"/> / CombatExecutor triggers configured on <see cref="EnemyAttackDefinition"/>.
        /// </summary>
        public void UpdateState(float moveSpeedNormalised, bool hasTarget, bool isStrafing, EnemyBrain.EnemyState brainState, int locomotionGaitIndex)
        {
            CacheAnimatorReference();
            if (_animator == null)
                return;

            SetFloatIfPresent(moveSpeedParameter, moveSpeedNormalised);

            if (!string.IsNullOrEmpty(currentGaitParameter))
                SetIntIfPresent(currentGaitParameter, locomotionGaitIndex);

            SetBoolIfPresent(hasTargetParameter, hasTarget);
            ApplyStrafeIntent(strafeParameter, isStrafing);
            SetBoolIfPresent(telegraphParameter, brainState == EnemyBrain.EnemyState.Telegraph);
            SetBoolIfPresent(deadParameter, brainState == EnemyBrain.EnemyState.Dead);
            SetBoolIfPresent(attackingParameter, brainState == EnemyBrain.EnemyState.Attack);
            SetBoolIfPresent(recoveringParameter, brainState == EnemyBrain.EnemyState.Recover);
            SetBoolIfPresent(staggeringParameter, brainState == EnemyBrain.EnemyState.Stagger);

            if (!string.IsNullOrEmpty(enemyStateParameter))
                SetIntIfPresent(enemyStateParameter, (int)brainState);

            ApplyGroundedAndFallingParameters(brainState != EnemyBrain.EnemyState.Dead);
            ApplyPolygonStyleMovementIntent(moveSpeedNormalised, brainState);
        }

        /// <summary>
        /// Sets Polygon-style grounded/fall blend inputs before the first <see cref="EnemyBrain"/> tick
        /// so the Animator does not evaluate with default IsGrounded=false.
        /// </summary>
        private void ApplyLocomotionBootstrap(bool forceGrounded)
        {
            CacheAnimatorReference();
            if (_animator == null)
                return;

            ApplyGroundedAndFallingParameters(forceGrounded);

            if (_animator.runtimeAnimatorController != null)
                _animator.Update(0f);
        }

        private void ApplyGroundedAndFallingParameters(bool grounded)
        {
            if (_animator == null)
                return;

            if (!string.IsNullOrEmpty(locomotionGroundedParameter))
                SetBoolIfPresent(locomotionGroundedParameter, grounded);

            if (suppressFallingBlendWhileGrounded && grounded)
            {
                SetFloatIfPresent(fallingDurationParameter, 0f);
                SetFloatIfPresent(fallingBlendParameter, 0f);
            }
        }

        /// <summary>
        /// Polygon controllers gate transitions into locomotion blend trees on player input bools.
        /// Enemies only move via NavMesh/agent velocity — mirror intent here when those parameters exist.
        /// </summary>
        private void ApplyPolygonStyleMovementIntent(float moveSpeedNormalised, EnemyBrain.EnemyState brainState)
        {
            if (!drivePolygonStyleMovementIntent || _animator == null || _animator.runtimeAnimatorController == null)
                return;

            if (!AnimatorParameterGuard.HasParameter(_animator, movementInputHeldParameter))
                return;

            bool frozenForCombat =
                brainState == EnemyBrain.EnemyState.Telegraph
                || brainState == EnemyBrain.EnemyState.Attack
                || brainState == EnemyBrain.EnemyState.Recover
                || brainState == EnemyBrain.EnemyState.Stagger
                || brainState == EnemyBrain.EnemyState.Dead;

            bool locomotionBrain =
                brainState == EnemyBrain.EnemyState.Approach
                || brainState == EnemyBrain.EnemyState.Strafe;

            bool wantsWalk =
                !frozenForCombat
                && (locomotionBrain || moveSpeedNormalised > polygonWalkMoveSpeedThreshold);

            SetBoolIfPresent(movementInputHeldParameter, wantsWalk);
            SetBoolIfPresent(movementInputPressedParameter, false);
            SetBoolIfPresent(movementInputTappedParameter, false);
            SetBoolIfPresent(polygonIsWalkingParameter, wantsWalk);
            SetBoolIfPresent(polygonIsStoppedParameter, !wantsWalk);
        }

        /// <summary>
        /// AC_Polygon_Masculine_Geis exposes <c>IsStrafing</c> as a float blend (0/1), not a bool.
        /// </summary>
        private void ApplyStrafeIntent(string parameterName, bool strafing)
        {
            if (_animator == null || string.IsNullOrEmpty(parameterName))
                return;

            if (AnimatorParameterGuard.HasParameterOfType(_animator, parameterName, AnimatorControllerParameterType.Bool))
                _animator.SetBool(parameterName, strafing);
            else if (AnimatorParameterGuard.HasParameterOfType(_animator, parameterName, AnimatorControllerParameterType.Float))
                _animator.SetFloat(parameterName, strafing ? 1f : 0f);
        }

        private void CacheAnimatorReference()
        {
            if (_animator != null)
                return;

            _combatEntity = GetComponent<CombatEntity>() ?? GetComponentInParent<CombatEntity>();
            _animator = _combatEntity != null ? _combatEntity.animator : GetComponentInChildren<Animator>();
        }

        public void ApplyAnimatorOverride(RuntimeAnimatorController controller)
        {
            CacheAnimatorReference();
            if (_animator == null || controller == null)
                return;

            _weaponComboRuntimeOverride = null;
            _animator.runtimeAnimatorController = controller;
            ApplyLocomotionBootstrap(forceGrounded: true);
        }

        /// <summary>
        /// Applies <see cref="EnemyAiDefinition.animatorOverrideController"/> and, when <c>weaponDefinition.comboData</c> is set,
        /// maps <see cref="GeisComboData"/> clips onto <see cref="GeisComboPlaceholders"/> like the player
        /// (<see cref="Geis.Locomotion.GeisPlayerAnimationController"/>), so <c>Attack</c> + <c>ComboStateBlend</c> select real weapon swings.
        /// </summary>
        public void ApplyAnimatorOverrideFromDefinition(EnemyAiDefinition definition)
        {
            if (definition == null)
                return;

            CacheAnimatorReference();
            if (_animator == null)
                return;

            RuntimeAnimatorController source = definition.animatorOverrideController;
            if (source == null)
                return;

            GeisComboData comboData = definition.weaponDefinition != null ? definition.weaponDefinition.comboData : null;

            if (comboData == null)
            {
                _weaponComboRuntimeOverride = null;
                ApplyAnimatorOverride(source);
                return;
            }

            GeisComboPlaceholders placeholders = enemyComboPlaceholders != null
                ? enemyComboPlaceholders
                : Resources.Load<GeisComboPlaceholders>("ComboPlaceholders/GeisComboPlaceholders")
                  ?? Resources.Load<GeisComboPlaceholders>("GeisComboPlaceholders");

            if (placeholders == null)
            {
                Debug.LogWarning(
                    "[EnemyAnimatorDriver] Assign Enemy Combo Placeholders on this component or add Resources/ComboPlaceholders/GeisComboPlaceholders. " +
                    "Without placeholder assets, GeisComboData clips cannot be applied to the Animator.",
                    this);
                _weaponComboRuntimeOverride = null;
                ApplyAnimatorOverride(source);
                return;
            }

            RuntimeAnimatorController baseController = source;
            if (source is AnimatorOverrideController incomingAoc)
                baseController = incomingAoc.runtimeAnimatorController;

            _weaponComboRuntimeOverride = new AnimatorOverrideController(baseController);

            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            for (int i = 0; i < ComboBlendSlotCount; i++)
            {
                AnimationClip placeholder = placeholders.GetPlaceholder(i);
                AnimationClip clip = comboData.GetClipForState(i);
                if (placeholder != null && clip != null)
                    overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(placeholder, clip));
            }

            if (overrides.Count > 0)
                _weaponComboRuntimeOverride.ApplyOverrides(overrides);

            _animator.runtimeAnimatorController = _weaponComboRuntimeOverride;
            ApplyLocomotionBootstrap(forceGrounded: true);
        }

        /// <summary>
        /// Sets animator weapon slot when <paramref name="definition"/> uses <see cref="EnemyAiDefinition.weaponDefinition"/> (same indices as <see cref="Geis.Combat.GeisWeaponSwitcher"/>).
        /// </summary>
        public void SyncAnimatorWeaponSlotFromDefinition(EnemyAiDefinition definition)
        {
            if (definition?.weaponDefinition == null)
                return;

            SetEquippedWeaponSlotIndex(definition.animatorEquippedWeaponSlotIndex);
        }

        public void SetEquippedWeaponSlotIndex(int slotIndex)
        {
            CacheAnimatorReference();
            SetIntIfPresent(equippedWeaponIndexParameter, slotIndex);
        }

        /// <summary>
        /// Mirrors <c>GeisPlayerAnimationController.SetComboStateBlend</c>: prefers Float ComboStateBlend, else Int ComboState.
        /// </summary>
        public void SetWeaponComboState(int state)
        {
            CacheAnimatorReference();
            if (_animator == null)
                return;

            state = Mathf.Max(0, state);

            if (AnimatorParameterGuard.HasParameterOfType(_animator, comboStateBlendParameter, AnimatorControllerParameterType.Float))
            {
                float blend = (float)state / (ComboBlendSlotCount - 1);
                _animator.SetFloat(Animator.StringToHash(comboStateBlendParameter), blend);
                return;
            }

            SetIntIfPresent(comboStateIntParameter, state);
        }

        public void TriggerAttack(string overrideTrigger = null)
        {
            CacheAnimatorReference();
            string trigger = string.IsNullOrEmpty(overrideTrigger) ? attackTrigger : overrideTrigger;
            TriggerIfPresent(trigger);
        }

        public void TriggerHitReaction()
        {
            CacheAnimatorReference();
            TriggerIfPresent(hitTrigger);
        }

        private void SetBoolIfPresent(string parameterName, bool value)
        {
            if (_animator == null || string.IsNullOrEmpty(parameterName))
                return;

            if (AnimatorParameterGuard.HasParameterOfType(_animator, parameterName, AnimatorControllerParameterType.Bool))
                _animator.SetBool(parameterName, value);
        }

        private void SetFloatIfPresent(string parameterName, float value)
        {
            if (_animator == null || string.IsNullOrEmpty(parameterName))
                return;

            if (AnimatorParameterGuard.HasParameterOfType(_animator, parameterName, AnimatorControllerParameterType.Float))
                _animator.SetFloat(parameterName, value);
        }

        private void SetIntIfPresent(string parameterName, int value)
        {
            if (_animator == null || string.IsNullOrEmpty(parameterName))
                return;

            if (AnimatorParameterGuard.HasParameterOfType(_animator, parameterName, AnimatorControllerParameterType.Int))
                _animator.SetInteger(parameterName, value);
        }

        private void TriggerIfPresent(string parameterName)
        {
            if (_animator == null || string.IsNullOrEmpty(parameterName))
                return;

            AnimatorParameterGuard.TrySetTrigger(_animator, parameterName);
        }
    }
}
