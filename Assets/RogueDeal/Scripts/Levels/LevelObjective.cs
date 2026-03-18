using RogueDeal.Combat;
using RogueDeal.Combat.Cards;
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
        public PokerHandType requiredHand;
        public CardRank requiredRank;
        
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
