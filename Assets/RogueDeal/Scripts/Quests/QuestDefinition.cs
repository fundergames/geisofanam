using System;
using System.Collections.Generic;
using UnityEngine;

namespace RogueDeal.Quests
{
    [CreateAssetMenu(fileName = "Quest_", menuName = "Funder Games/Rogue Deal/Quests/Quest Definition")]
    public class QuestDefinition : ScriptableObject
    {
        [Header("Quest Identity")]
        public string questId;
        public string displayName;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;

        [Header("Objectives")]
        public List<QuestObjective> objectives = new List<QuestObjective>();

        [Header("Rewards")]
        public int goldReward;
        public int xpReward;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(questId) && !string.IsNullOrEmpty(displayName))
            {
                questId = displayName.Replace(" ", "_").ToLower();
            }
        }
    }

    [Serializable]
    public class QuestObjective
    {
        public string objectiveId;
        public string description;
        public string signalKey;  // e.g., "enemy_defeated", "item_collected"
        public string targetId;   // e.g., "slime", "potion"
        public int targetAmount;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(objectiveId) && !string.IsNullOrEmpty(description))
            {
                objectiveId = description.Replace(" ", "_").ToLower();
            }
        }
    }
}