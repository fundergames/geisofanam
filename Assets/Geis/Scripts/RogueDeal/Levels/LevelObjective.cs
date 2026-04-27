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

using RogueDeal.Combat;
using System;
using UnityEngine;

namespace RogueDeal.Levels
{
    [Serializable]
    public class LevelObjective
    {
        public string objectiveName;
        [TextArea(2, 3)]
        public string description;

        public ObjectiveType type;

        [Header("Type-Specific Data")]
        public int targetValue;

        [Header("Rewards")]
        public bool isRequired = true;
        public int bonusGold = 0;
        public int bonusXP = 0;

        public bool isCompleted;

        public bool CheckCompletion(int currentValue)
        {
            switch (type)
            {
                case ObjectiveType.DefeatAllEnemies:
                case ObjectiveType.CompleteWithinTurns:
                case ObjectiveType.CompleteWithinTime:
                    return currentValue >= targetValue;
                default:
                    return false;
            }
        }
    }

    public enum ObjectiveType
    {
        DefeatAllEnemies,
        CompleteWithinTurns,
        CompleteWithinTime,
        GetSpecificHand,
        GetHandWithRank,
        DontTakeDamage,
        UseSpecificClass
    }
}
