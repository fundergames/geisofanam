/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
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
        [Header("Behaviour flags (omit from Animator Controller to skip)")]
        [SerializeField] private string hasTargetParameter = "HasTarget";
        [SerializeField] private string strafeParameter = "IsStrafing";
        [SerializeField] private string telegraphParameter = "IsTelegraphing";
        [SerializeField] private string deadParameter = "IsDead";
        [SerializeField] private string attackingParameter = "IsAttacking";
        [SerializeField] private string recoveringParameter = "IsRecovering";
        [SerializeField] private string staggeringParameter = "IsStaggering";

        [Header("Locomotion compatibility")]
        [Tooltip("If set, forced true while alive so controllers like AC_Polygon_Masculine_Geis stay in grounded locomotion.")]
        [SerializeField] private string locomotionGroundedParameter = "IsGrounded";
        [SerializeField] private bool bootstrapGroundedOnEnable = true;
        [SerializeField] private bool suppressFallingBlendWhileGrounded = true;
        [SerializeField] private string fallingDurationParameter = "FallingDuration";
        [SerializeField] private string fallingBlendParameter = "FallingBlend";

        [Tooltip("Uses LocomotionAnimatorApplier (same path as the player) for MoveSpeed, CurrentGait, MovementInputHeld, IsWalking.")]
        [SerializeField] private bool useSharedLocomotionApplier = true;

        [Header("Discrete brain state (optional)")]
        [SerializeField] private string enemyStateParameter = "";

        [Header("Combat triggers")]
        [SerializeField] private string attackTrigger = "Attack";
        [SerializeField] private string hitTrigger = "TakeDamage";

        [Header("Weapon / combo (Polygon-style, optional)")]
        [SerializeField] private string equippedWeaponIndexParameter = "EquippedWeaponIndex";
        [SerializeField] private string comboStateBlendParameter = "ComboStateBlend";
        [SerializeField] private string comboStateIntParameter = "ComboState";
        [SerializeField] private GeisComboPlaceholders enemyComboPlaceholders;

        private const int ComboBlendSlotCount = 32;
        private const float VelocityGaitBlendThresholdMps = 0.12f;

        private EnemyCombatant _combatant;
        private CombatEntity _combatEntity;
        private Animator _animator;
        private AnimatorOverrideController _weaponComboRuntimeOverride;
        private bool _hasFallingBlendParameter;

        private void Awake()
        {
            _combatant = GetComponent<EnemyCombatant>() ?? GetComponentInParent<EnemyCombatant>();
            CacheAnimatorReference();
        }

        private void OnEnable()
        {
            if (bootstrapGroundedOnEnable)
                ApplyLocomotionBootstrap(forceGrounded: true);
        }

        public void UpdateState(EnemyMotor motor, bool hasTarget, bool isStrafing, EnemyBrain.EnemyState brainState)
        {
            CacheAnimatorReference();
            if (_animator == null)
                return;

            float planarSpeedMps = motor != null ? motor.PlanarSpeedMps : 0f;
            int gaitIntent = motor != null ? motor.LocomotionGaitIntent : 0;

            if (useSharedLocomotionApplier)
                ApplySharedLocomotionPresentation(planarSpeedMps, gaitIntent, isStrafing, brainState);
            else
                ApplyLegacyLocomotionPresentation(planarSpeedMps, gaitIntent, isStrafing, brainState);

            SetBoolIfPresent(hasTargetParameter, hasTarget);
            SetBoolIfPresent(telegraphParameter, brainState == EnemyBrain.EnemyState.Telegraph);
            SetBoolIfPresent(deadParameter, brainState == EnemyBrain.EnemyState.Dead);
            SetBoolIfPresent(attackingParameter, brainState == EnemyBrain.EnemyState.Attack);
            SetBoolIfPresent(recoveringParameter, brainState == EnemyBrain.EnemyState.Recover);
            SetBoolIfPresent(staggeringParameter, brainState == EnemyBrain.EnemyState.Stagger);

            if (!string.IsNullOrEmpty(enemyStateParameter))
                SetIntIfPresent(enemyStateParameter, (int)brainState);
        }

        private void ApplySharedLocomotionPresentation(
            float planarSpeedMps,
            int gaitIntent,
            bool isStrafing,
            EnemyBrain.EnemyState brainState)
        {
            EnemyMovementSettings movement = _combatant != null && _combatant.Definition != null
                ? _combatant.Definition.movement
                : null;

            float walkRef = movement != null ? movement.animatorWalkSpeedReference : GeisLocomotionGait.DefaultWalkSpeed;
            float runRef = movement != null ? movement.animatorRunSpeedReference : GeisLocomotionGait.DefaultRunSpeed;
            float sprintRef = movement != null ? movement.animatorSprintSpeedReference : GeisLocomotionGait.DefaultSprintSpeed;

            bool frozenForCombat =
                brainState == EnemyBrain.EnemyState.Telegraph
                || brainState == EnemyBrain.EnemyState.Attack
                || brainState == EnemyBrain.EnemyState.Recover
                || brainState == EnemyBrain.EnemyState.Stagger
                || brainState == EnemyBrain.EnemyState.Dead;

            bool locomotionBrain =
                brainState == EnemyBrain.EnemyState.Approach
                || brainState == EnemyBrain.EnemyState.Strafe;

            int gait = GeisLocomotionGait.FromPlanarSpeed(planarSpeedMps, walkRef, runRef, sprintRef);
            float moveSpeed2D = planarSpeedMps;

            if (!frozenForCombat && locomotionBrain && planarSpeedMps < VelocityGaitBlendThresholdMps && gaitIntent > GeisLocomotionGait.Idle)
            {
                gait = gaitIntent;
                moveSpeed2D = GeisLocomotionGait.ReferenceSpeedForGait(gaitIntent, walkRef, runRef, sprintRef);
            }

            bool wantsLocomotion = !frozenForCombat && locomotionBrain && moveSpeed2D > 0.01f;
            bool isWalking = wantsLocomotion && gait == GeisLocomotionGait.Walk;

            var snap = new LocomotionPresentationSnapshot
            {
                MoveSpeed2D = frozenForCombat ? 0f : moveSpeed2D,
                CurrentGait = frozenForCombat ? GeisLocomotionGait.Idle : gait,
                IsStrafingFloat = isStrafing ? 1f : 0f,
                IsGrounded = brainState != EnemyBrain.EnemyState.Dead,
                MovementInputHeld = wantsLocomotion,
                MovementInputPressed = false,
                MovementInputTapped = false,
                IsWalking = isWalking,
                IsStopped = !wantsLocomotion,
                IsStarting = false,
                FallingDuration = 0f
            };

            var ctx = new LocomotionApplyContext
            {
                AirGaitForAnimator = false,
                HasFallingBlendParameter = _hasFallingBlendParameter,
                FallingBlendValue = 0f,
                IsJumpingValue = false
            };

            LocomotionAnimatorApplier.ApplySyntyLocomotion(_animator, snap, ctx);

            if (suppressFallingBlendWhileGrounded && snap.IsGrounded)
            {
                SetFloatIfPresent(fallingDurationParameter, 0f);
                if (_hasFallingBlendParameter)
                    SetFloatIfPresent(fallingBlendParameter, 0f);
            }
        }

        /// <summary>Minimal float/int writes when shared applier is disabled.</summary>
        private void ApplyLegacyLocomotionPresentation(
            float planarSpeedMps,
            int gaitIntent,
            bool isStrafing,
            EnemyBrain.EnemyState brainState)
        {
            int gait = planarSpeedMps < VelocityGaitBlendThresholdMps ? gaitIntent : gaitIntent;
            SetFloatIfPresent("MoveSpeed", planarSpeedMps);
            SetIntIfPresent("CurrentGait", gait);
            ApplyStrafeIntent(strafeParameter, isStrafing);
            ApplyGroundedAndFallingParameters(brainState != EnemyBrain.EnemyState.Dead);
        }

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
            _hasFallingBlendParameter = _animator != null
                && AnimatorParameterGuard.HasParameter(_animator, fallingBlendParameter);
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
                    "[EnemyAnimatorDriver] Assign Enemy Combo Placeholders on this component or add Resources/ComboPlaceholders/GeisComboPlaceholders.",
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

        public void SetWeaponComboState(int state)
        {
            CacheAnimatorReference();
            if (_animator == null)
                return;

            GeisComboAnimatorBlend.Apply(
                _animator, state, ComboBlendSlotCount, comboStateBlendParameter, comboStateIntParameter);
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
