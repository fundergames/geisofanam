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

namespace RogueDeal.Combat
{
    [CreateAssetMenu(fileName = "NewAbility", menuName = "Funder Games/Geis/Rogue Deal/Combat/Ability")]
    public class AbilityData : ScriptableObject
    {
        [Header("Basic Info")]
        public string abilityName;
        public Sprite icon;
        
        [Header("Gameplay")]
        public float cooldown;
        public float range;
        public TargetType targetType;
        
        [Header("Effects")]
        public EffectData[] effects;
        
        [Header("Visuals")]
        public GameObject vfxPrefab;
        public AudioClip sfx;
        public AnimationClip animation;
        
        [Header("Advanced Sequencing")]
        public CombatSequenceAsset sequenceAsset;
    }
}
