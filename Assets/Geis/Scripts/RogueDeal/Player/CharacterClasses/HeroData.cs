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
using UnityEngine;
using RogueDeal.Combat;

namespace RogueDeal.Player
{
    [CreateAssetMenu(fileName = "Hero Data", menuName = "RogueDeal/Character/Hero Data")]
    public class HeroData : ScriptableObject
    {
        [SerializeField] private string playerName;
        [SerializeField] private int level;
        [SerializeField] private float levelProgress;
        [SerializeField] private int power;
        [SerializeField] private StatsData statList;
        [SerializeField] private CharacterClassData characterClass;
        [SerializeField] private HeroVisualData heroVisualData;
        [SerializeField] private ClassAnimatorData animatorData;
        
        public string PlayerName => playerName;
        public int Level => level;
        public float LevelProgress => levelProgress;
        public int Power => power;
        public StatsData StatList => statList;
        public CharacterClassData CharacterClass => characterClass;
        public HeroVisualData HeroVisualData => heroVisualData;
        public ClassAnimatorData AnimatorData => animatorData;
    }
}
