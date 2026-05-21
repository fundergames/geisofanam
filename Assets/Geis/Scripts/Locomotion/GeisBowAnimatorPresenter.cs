/*

 * Copyright (c) 2026 Funder Games

 *

 * All rights reserved.

 */



using Geis.Animation;

using UnityEngine;



namespace Geis.Locomotion

{

    /// <summary>

    /// Drives the character Bow_Draw layer while the bow weapon is equipped.

    /// Exiting bow for melee snaps the layer off immediately so Prop_R / Hand_R return to locomotion.

    /// </summary>

    public sealed class GeisBowAnimatorPresenter

    {

        public readonly struct Snapshot

        {

            public readonly bool IsDrawing;

            public readonly float DrawCharge;

            public readonly bool IsAiming;

            public readonly bool IsBowEquipped;

            public readonly bool IsChargedShotReady;



            public Snapshot(

                bool isDrawing,

                float drawCharge,

                bool isAiming,

                bool isBowEquipped,

                bool isChargedShotReady)

            {

                IsDrawing = isDrawing;

                DrawCharge = drawCharge;

                IsAiming = isAiming;

                IsBowEquipped = isBowEquipped;

                IsChargedShotReady = isChargedShotReady;

            }



            public bool IsActiveBowUse => IsAiming || IsDrawing;

        }



        private bool _hasDrawing;

        private bool _hasDrawCharge;

        private bool _hasAiming;

        private bool _hasChargedShotReady;

        private int _drawLayerIndex = -1;

        private float _currentLayerWeight;

        private float _equipLayerBlendSpeed = GeisLocomotionTuningDefaults.BowEquipLayerBlendSpeed;

        private bool _snapBowIdleOnLayer;

        private bool _wasActiveBowUse;



        public float EquipLayerBlendSpeed

        {

            get => _equipLayerBlendSpeed;

            set => _equipLayerBlendSpeed = value;

        }



        public void RefreshCaches(Animator animator, bool bowEquipped)

        {

            CacheParameters(animator);



            if (animator == null)

                return;



            if (_drawLayerIndex >= 0)

                _currentLayerWeight = animator.GetLayerWeight(_drawLayerIndex);



            if (bowEquipped)

            {

                _snapBowIdleOnLayer = true;

                _wasActiveBowUse = false;

            }

        }



        public void Apply(Animator animator, in Snapshot snapshot, float deltaTime)

        {

            if (animator == null)

                return;



            if (!snapshot.IsBowEquipped)

            {

                ForceExitBowPresentation(animator, reevaluateAnimator: true);

                return;

            }



            if (snapshot.IsActiveBowUse)

            {

                if (_hasDrawing)

                    animator.SetBool(BowAnimatorIds.BowDrawing, snapshot.IsDrawing);

                if (_hasDrawCharge)

                    animator.SetFloat(BowAnimatorIds.BowDrawCharge, snapshot.DrawCharge);

                if (_hasAiming)

                    animator.SetBool(BowAnimatorIds.BowAiming, snapshot.IsAiming);

                if (_hasChargedShotReady)

                    animator.SetBool(BowAnimatorIds.BowChargedShotReady, snapshot.IsChargedShotReady);



                _wasActiveBowUse = true;

                _snapBowIdleOnLayer = false;

            }

            else

            {

                ClearBowParameters(animator);



                if (_snapBowIdleOnLayer || _wasActiveBowUse)

                    SnapBowLayerToIdle(animator);



                _wasActiveBowUse = false;

                _snapBowIdleOnLayer = false;

            }



            BlendLayerWeight(animator, 1f, deltaTime);

        }



        /// <summary>

        /// Snaps bow presentation off before a non-bow weapon is shown.

        /// </summary>

        public void ForceExitBowPresentation(Animator animator, bool reevaluateAnimator)

        {

            CacheParameters(animator);

            ClearBowParameters(animator);



            _currentLayerWeight = 0f;

            _wasActiveBowUse = false;

            _snapBowIdleOnLayer = false;



            if (_drawLayerIndex >= 0)

                animator.SetLayerWeight(_drawLayerIndex, 0f);



            if (reevaluateAnimator)

                animator.Update(0f);

        }



        private void CacheParameters(Animator animator)

        {

            if (animator == null)

            {

                _hasDrawing = false;

                _hasDrawCharge = false;

                _hasAiming = false;

                _hasChargedShotReady = false;

                _drawLayerIndex = -1;

                return;

            }



            _hasDrawing = AnimatorParameterGuard.HasParameter(animator, BowAnimatorIds.BowDrawingName);

            _hasDrawCharge = AnimatorParameterGuard.HasParameter(animator, BowAnimatorIds.BowDrawChargeName);

            _hasAiming = AnimatorParameterGuard.HasParameter(animator, BowAnimatorIds.BowAimingName);

            _hasChargedShotReady = AnimatorParameterGuard.HasParameter(animator, BowAnimatorIds.BowChargedShotReadyName);

            _drawLayerIndex = animator.GetLayerIndex(BowAnimatorIds.DrawLayerName);

        }



        private void SnapBowLayerToIdle(Animator animator)

        {

            if (_drawLayerIndex < 0)

                _drawLayerIndex = animator.GetLayerIndex(BowAnimatorIds.DrawLayerName);



            if (_drawLayerIndex < 0)

                return;



            animator.Play(BowAnimatorIds.BowIdleState, _drawLayerIndex, 0f);

        }



        private void BlendLayerWeight(Animator animator, float targetWeight, float deltaTime)

        {

            if (_drawLayerIndex < 0)

                return;



            float blendSpeed = Mathf.Max(0.01f, _equipLayerBlendSpeed);

            _currentLayerWeight = Mathf.MoveTowards(_currentLayerWeight, targetWeight, blendSpeed * deltaTime);

            animator.SetLayerWeight(_drawLayerIndex, _currentLayerWeight);

        }



        private void ClearBowParameters(Animator animator)

        {

            if (_hasDrawing)

                animator.SetBool(BowAnimatorIds.BowDrawing, false);

            if (_hasDrawCharge)

                animator.SetFloat(BowAnimatorIds.BowDrawCharge, 0f);

            if (_hasAiming)

                animator.SetBool(BowAnimatorIds.BowAiming, false);

            if (_hasChargedShotReady)

                animator.SetBool(BowAnimatorIds.BowChargedShotReady, false);

        }

    }

}


