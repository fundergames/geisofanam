using System.Collections.Generic;
using UnityEngine;
using RogueDeal.Quests;

namespace RogueDeal.NPCs
{
    [CreateAssetMenu(fileName = "NPC_", menuName = "Funder Games/Rogue Deal/NPCs/NPC Definition")]
    public class NPCDefinition : ScriptableObject
    {
        [Header("Basic Info")]
        public string npcId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public GameObject modelPrefab;

        [Header("Dialog")]
        [Tooltip("The dialog tree this NPC uses for conversations")]
        public DialogTree dialogTree;

        [Header("Available Quests")]
        [Tooltip("Quests this NPC can offer (will be filtered by quest state in dialog)")]
        public List<QuestDefinition> availableQuests = new List<QuestDefinition>();

        [Header("Visual")]
        public Vector3 spawnOffset = Vector3.zero;
        public float scale = 1f;

        [Header("Interaction")]
        [Tooltip("Interaction range (if using distance-based detection)")]
        public float interactionRange = 2.5f;

        [Tooltip("Key to press to interact (e.g., 'E', 'F')")]
        public string interactionKey = "E";

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(npcId) && !string.IsNullOrEmpty(displayName))
            {
                npcId = displayName.Replace(" ", "_").ToLower();
            }
        }
    }
}