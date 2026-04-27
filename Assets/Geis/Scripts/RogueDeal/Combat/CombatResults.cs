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
using RogueDeal.Levels;

namespace RogueDeal.Combat
{
    public class CombatResults
    {
        public LevelDefinition Level { get; private set; }
        public bool Victory { get; private set; }
        public int TurnsUsed { get; private set; }
        public int StarsEarned { get; private set; }
        public int GoldEarned { get; private set; }
        public int XPEarned { get; set; }
        public float CombatDuration { get; private set; }

        public CombatResults(LevelDefinition level, bool victory, int turnsUsed, float combatDuration)
        {
            Level = level;
            Victory = victory;
            TurnsUsed = turnsUsed;
            CombatDuration = combatDuration;

            if (victory)
            {
                CalculateRewards();
            }
            else
            {
                StarsEarned = 0;
                GoldEarned = 0;
                XPEarned = 0;
            }
        }

        private void CalculateRewards()
        {
            StarsEarned = Level.CalculateStars(TurnsUsed, true);
            
            GoldEarned = Level.baseGoldReward;
            XPEarned = Level.baseXPReward;

            float starMultiplier = 1f + (StarsEarned - 1) * 0.25f;
            GoldEarned = Mathf.RoundToInt(GoldEarned * starMultiplier);
            XPEarned = Mathf.RoundToInt(XPEarned * starMultiplier);
        }

        public void SaveToLevelManager()
        {
            if (!Victory || Level == null)
            {
                return;
            }

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.CompleteLevel(Level, StarsEarned, GoldEarned, XPEarned);
                Debug.Log($"[CombatResults] Level {Level.GetLevelCode()} completed with {StarsEarned} stars!");
            }
            else
            {
                Debug.LogWarning("[CombatResults] LevelManager not found, cannot save progress!");
            }
        }

        public override string ToString()
        {
            if (Victory)
            {
                return $"Victory! Level: {Level.displayName}, Stars: {StarsEarned}, Turns: {TurnsUsed}/{Level.totalTurns}, Gold: {GoldEarned}, XP: {XPEarned}";
            }
            else
            {
                return $"Defeat! Level: {Level.displayName}, Turns: {TurnsUsed}";
            }
        }
    }
}
