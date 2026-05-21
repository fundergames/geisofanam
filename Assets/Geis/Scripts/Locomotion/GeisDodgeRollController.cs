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
    /// Dodge / roll playback, direction selection, and transient dodge state.
    /// </summary>
    public sealed class GeisDodgeRollController
    {
        public bool IsRoll { get; private set; }
        public bool RequestIsRoll { get; set; }
        public int AnimatorDir { get; private set; }
        public bool PreserveStrafeFacing { get; set; }
        public bool AnimatorEnteredLeaf { get; set; }
        public float StateTimeout { get; set; }
        public float StateEnteredAtUnscaled { get; private set; } = -1f;

        public bool LoggedAnimatorMissing { get; set; }
        public bool LoggedForwardRollMissing { get; set; }

        public static bool IsDodgeLeafShortNameHash(int shortNameHash) =>
            shortNameHash == LocomotionAnimatorIds.DodgeLeafFront
            || shortNameHash == LocomotionAnimatorIds.DodgeLeafBack
            || shortNameHash == LocomotionAnimatorIds.DodgeLeafLeft
            || shortNameHash == LocomotionAnimatorIds.DodgeLeafRight
            || shortNameHash == LocomotionAnimatorIds.RollLeafForward
            || shortNameHash == LocomotionAnimatorIds.RollLeafBack
            || shortNameHash == LocomotionAnimatorIds.RollLeafLeft
            || shortNameHash == LocomotionAnimatorIds.RollLeafRight;

        public void ResetTransientState()
        {
            IsRoll = false;
            RequestIsRoll = false;
            AnimatorEnteredLeaf = false;
            StateTimeout = 0f;
            StateEnteredAtUnscaled = -1f;
            PreserveStrafeFacing = false;
        }

        public int ComputeDirectionIndex(
            Vector2 moveComposite,
            float deadzone,
            bool isLockedOn,
            Vector3 planarAwayFromTarget,
            Vector3 cameraRelativeWorldDirection,
            Vector3 bodyForward,
            Vector3 bodyRight)
        {
            if (moveComposite.sqrMagnitude < deadzone * deadzone)
            {
                if (isLockedOn && planarAwayFromTarget.sqrMagnitude > 0.0001f)
                    return GeisDodgeDirectionUtility.WorldDirectionToIndex(planarAwayFromTarget, bodyForward, bodyRight);

                return 1;
            }

            return GeisDodgeDirectionUtility.WorldDirectionToIndex(
                cameraRelativeWorldDirection,
                bodyForward,
                bodyRight);
        }

        public void BeginDodge(
            bool isRoll,
            int direction,
            bool preserveStrafeFacing,
            float fallbackDuration,
            float nowUnscaled)
        {
            IsRoll = isRoll;
            RequestIsRoll = false;
            AnimatorDir = direction;
            PreserveStrafeFacing = preserveStrafeFacing;
            AnimatorEnteredLeaf = false;
            StateTimeout = fallbackDuration;
            StateEnteredAtUnscaled = nowUnscaled;
        }

        public void UpgradeToRoll(int direction, float fallbackDuration, float nowUnscaled, bool preserveStrafeFacing)
        {
            IsRoll = true;
            AnimatorDir = direction;
            PreserveStrafeFacing = preserveStrafeFacing;
            AnimatorEnteredLeaf = false;
            StateTimeout = fallbackDuration;
            StateEnteredAtUnscaled = nowUnscaled;
        }

        public void EndDodge()
        {
            IsRoll = false;
            StateEnteredAtUnscaled = -1f;
        }

        public void PlayLeafCrossFade(
            Animator animator,
            int dir,
            bool forceRestart,
            bool hasDodgeDirectionParam,
            bool hasDodgeTrigger,
            bool hasRollTrigger)
        {
            if (animator == null)
                return;

            animator.ResetTrigger(LocomotionAnimatorIds.Dodge);
            animator.ResetTrigger(LocomotionAnimatorIds.RollTrigger);

            if (hasDodgeDirectionParam)
                animator.SetInteger(LocomotionAnimatorIds.DodgeDirection, dir);

            int primaryHash = IsRoll
                ? GetRollNestedHashForDirection(dir)
                : GetDodgeNestedHashForDirection(dir);
            int fallbackHash = IsRoll
                ? GetRollLeafHashForDirection(dir)
                : GetDodgeLeafHashForDirection(dir);

            if (forceRestart)
            {
                if (animator.HasState(0, primaryHash))
                    animator.Play(primaryHash, 0, 0f);
                else if (animator.HasState(0, fallbackHash))
                    animator.Play(fallbackHash, 0, 0f);
                else if (IsRoll && hasRollTrigger)
                    animator.SetTrigger(LocomotionAnimatorIds.RollTrigger);
                else if (hasDodgeTrigger)
                    animator.SetTrigger(LocomotionAnimatorIds.Dodge);
                return;
            }

            if (animator.HasState(0, primaryHash))
                animator.CrossFadeInFixedTime(primaryHash, 0.05f, 0, 0f);
            else if (animator.HasState(0, fallbackHash))
                animator.CrossFadeInFixedTime(fallbackHash, 0.05f, 0, 0f);
            else if (IsRoll && hasRollTrigger)
                animator.SetTrigger(LocomotionAnimatorIds.RollTrigger);
            else if (hasDodgeTrigger)
                animator.SetTrigger(LocomotionAnimatorIds.Dodge);
        }

        public bool HasRollLeafStateForDirection(Animator animator, int dir)
        {
            if (animator == null)
                return false;

            int nested = GetRollNestedHashForDirection(dir);
            int leaf = GetRollLeafHashForDirection(dir);
            return animator.HasState(0, nested) || animator.HasState(0, leaf);
        }

        public Vector3 GetFacingWorld(int dirIndex, Vector3 camFwd, Vector3 camRight)
        {
            switch (dirIndex)
            {
                case 0: return camFwd;
                case 1: return -camFwd;
                case 2: return -camRight;
                case 3: return camRight;
                default: return camFwd;
            }
        }

        public static int GetDodgeLeafHashForDirection(int dir)
        {
            switch (dir)
            {
                case 0: return LocomotionAnimatorIds.DodgeLeafFront;
                case 1: return LocomotionAnimatorIds.DodgeLeafBack;
                case 2: return LocomotionAnimatorIds.DodgeLeafLeft;
                case 3: return LocomotionAnimatorIds.DodgeLeafRight;
                default: return LocomotionAnimatorIds.DodgeLeafFront;
            }
        }

        private static int GetDodgeNestedHashForDirection(int dir)
        {
            switch (dir)
            {
                case 0: return LocomotionAnimatorIds.DodgeNestedFront;
                case 1: return LocomotionAnimatorIds.DodgeNestedBack;
                case 2: return LocomotionAnimatorIds.DodgeNestedLeft;
                case 3: return LocomotionAnimatorIds.DodgeNestedRight;
                default: return LocomotionAnimatorIds.DodgeNestedFront;
            }
        }

        private static int GetRollLeafHashForDirection(int dir)
        {
            switch (dir)
            {
                case 0: return LocomotionAnimatorIds.RollLeafForward;
                case 1: return LocomotionAnimatorIds.RollLeafBack;
                case 2: return LocomotionAnimatorIds.RollLeafLeft;
                case 3: return LocomotionAnimatorIds.RollLeafRight;
                default: return LocomotionAnimatorIds.RollLeafForward;
            }
        }

        private static int GetRollNestedHashForDirection(int dir)
        {
            switch (dir)
            {
                case 0: return LocomotionAnimatorIds.RollNestedForward;
                case 1: return LocomotionAnimatorIds.RollNestedBack;
                case 2: return LocomotionAnimatorIds.RollNestedLeft;
                case 3: return LocomotionAnimatorIds.RollNestedRight;
                default: return LocomotionAnimatorIds.RollNestedForward;
            }
        }
    }
}
