/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using RogueDeal.Combat;
using UnityEngine;

namespace Geis.Combat
{
    /// <summary>
    /// Synty Polygon recoil-direction clips (F/B/L/R = where the body moves, not strike origin).
    /// Use <see cref="GetStateName"/> with strike-from <see cref="CombatHitDirection"/>; mapping is applied internally.
    /// </summary>
    [CreateAssetMenu(fileName = "DirectionalHitReaction_", menuName = "Funder Games/Geis/Combat/Directional Hit Reaction Set")]
    public class DirectionalHitReactionSet : ScriptableObject
    {
        [Header("Clips (Synty recoil direction: F/B/L/R)")]
        [Tooltip("A_Hit_F_React_Sword — body recoils forward (e.g. hit from behind).")]
        public AnimationClip front;
        [Tooltip("A_Hit_B_React_Sword — body recoils backward (e.g. hit from in front).")]
        public AnimationClip back;
        [Tooltip("A_Hit_L_React_Sword — body recoils left (e.g. hit from the right).")]
        public AnimationClip left;
        [Tooltip("A_Hit_R_React_Sword — body recoils right (e.g. hit from the left).")]
        public AnimationClip right;

        [Header("Animator state names (HitReact_F/B/L/R in controller)")]
        public string stateNameFront = "HitReact_F";
        public string stateNameBack = "HitReact_B";
        public string stateNameLeft = "HitReact_L";
        public string stateNameRight = "HitReact_R";

        [Header("Optional per-direction triggers (instead of HitDirection int)")]
        public string triggerFront = "HitReact_Front";
        public string triggerBack = "HitReact_Back";
        public string triggerLeft = "HitReact_Left";
        public string triggerRight = "HitReact_Right";

        public string GetStateName(CombatHitDirection strikeFrom) =>
            GetStateNameForReaction(CombatHitDirectionUtility.ToReactionDirection(strikeFrom));

        public string GetTriggerName(CombatHitDirection strikeFrom) =>
            GetTriggerNameForReaction(CombatHitDirectionUtility.ToReactionDirection(strikeFrom));

        public AnimationClip GetClip(CombatHitDirection strikeFrom) =>
            GetClipForReaction(CombatHitDirectionUtility.ToReactionDirection(strikeFrom));

        private string GetStateNameForReaction(CombatHitDirection reaction)
        {
            switch (reaction)
            {
                case CombatHitDirection.Back: return stateNameBack;
                case CombatHitDirection.Left: return stateNameLeft;
                case CombatHitDirection.Right: return stateNameRight;
                default: return stateNameFront;
            }
        }

        private string GetTriggerNameForReaction(CombatHitDirection reaction)
        {
            switch (reaction)
            {
                case CombatHitDirection.Back: return triggerBack;
                case CombatHitDirection.Left: return triggerLeft;
                case CombatHitDirection.Right: return triggerRight;
                default: return triggerFront;
            }
        }

        private AnimationClip GetClipForReaction(CombatHitDirection reaction)
        {
            switch (reaction)
            {
                case CombatHitDirection.Back: return back;
                case CombatHitDirection.Left: return left;
                case CombatHitDirection.Right: return right;
                default: return front;
            }
        }
    }
}
