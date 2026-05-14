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

using UnityEngine;

namespace RogueDeal.Player
{
    [CreateAssetMenu(fileName = "AnimatorData", menuName = "Funder Games/Geis/Rogue Deal/Character/Animator Data")]
    public class ClassAnimatorData : ScriptableObject
    {
        public RuntimeAnimatorController battleAnimator;
        public RuntimeAnimatorController characterSelectAnimator;
        
        [Header("Animations")]
        public AnimationClip idleClip;
        public AnimationClip attack1Clip;
        public AnimationClip attack2Clip;
        public AnimationClip attack3Clip;
        public AnimationClip attack4Clip;
        public AnimationClip attack5Clip;
        public AnimationClip tauntAnimationClip;
        public AnimationClip battleIdleClip;
        public AnimationClip levelUpClip;
        public AnimationClip dieClip;
        public AnimationClip dizzyClip;
        public AnimationClip takeDamage1Clip;
        public AnimationClip takeDamage2Clip;
        public AnimationClip defendClip;
        public AnimationClip victoryClip;
        public AnimationClip sprintClip;
        public AnimationClip comboClip;
    }
}
