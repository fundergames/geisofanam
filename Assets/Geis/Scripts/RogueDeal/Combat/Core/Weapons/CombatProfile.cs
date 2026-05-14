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

namespace RogueDeal.Combat.Core.Data
{
    /// <summary>
    /// Combat profile for a character. Defines engagement distance, combat range, and line of sight requirements.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatProfile_", menuName = "Funder Games/Geis/Rogue Deal/Combat/Combat Profiles/Combat Profile")]
    public class CombatProfile : ScriptableObject
    {
        [Header("Basic Info")]
        public string profileName;
        [TextArea(2, 4)]
        public string description;
        
        [Header("Combat Range")]
        [Tooltip("Type of combat range (Melee, Ranged, Magic)")]
        public CombatRange combatRange = CombatRange.Melee;
        
        [Header("Engagement")]
        [Tooltip("How close the character needs to be to attack (in Unity units)")]
        public float engagementDistance = 1.5f;
        
        [Tooltip("Does this character require line of sight to attack?")]
        public bool requiresLineOfSight = true;
        
        [Header("Animation")]
        [Tooltip("Animator override controller for this combat profile")]
        public RuntimeAnimatorController animatorOverrideController;
        
        [Header("Movement")]
        [Tooltip("Speed multiplier when moving to engage target")]
        public float movementSpeedMultiplier = 1f;
        
        [Tooltip("Does this character return to origin after attacking?")]
        public bool returnToOriginAfterAttack = true;
    }
    
    /// <summary>
    /// Type of combat range
    /// </summary>
    public enum CombatRange
    {
        Melee,
        Ranged,
        Magic
    }
}

