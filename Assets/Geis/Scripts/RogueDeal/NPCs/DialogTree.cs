using System;
using System.Collections.Generic;
using UnityEngine;
using RogueDeal.Quests;

namespace RogueDeal.NPCs
{
    [CreateAssetMenu(fileName = "Dialog_", menuName = "Funder Games/Rogue Deal/NPCs/Dialog Tree")]
    public class DialogTree : ScriptableObject
    {
        [Header("Dialog Info")]
        public string dialogId;
        public string displayName;

        [Header("Dialog Nodes")]
        [Tooltip("List of all dialog nodes in this tree")]
        public List<DialogNode> nodes = new List<DialogNode>();

        [Tooltip("The starting node ID (first node shown when dialog begins)")]
        public string entryNodeId;

        public DialogNode GetNode(string nodeId)
        {
            return nodes?.Find(n => n.nodeId == nodeId);
        }

        public DialogNode GetEntryNode()
        {
            if (string.IsNullOrEmpty(entryNodeId) && nodes != null && nodes.Count > 0)
            {
                return nodes[0];
            }
            
            var node = GetNode(entryNodeId);
            if (node == null && nodes != null && nodes.Count > 0)
            {
                Debug.LogWarning($"[DialogTree] Entry node '{entryNodeId}' not found, using first node instead.");
                return nodes[0];
            }
            
            return node;
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(dialogId) && !string.IsNullOrEmpty(displayName))
            {
                dialogId = displayName.Replace(" ", "_").ToLower();
            }

            // Auto-set entry node if not set and we have nodes
            if (string.IsNullOrEmpty(entryNodeId) && nodes != null && nodes.Count > 0)
            {
                entryNodeId = nodes[0].nodeId;
            }
        }
    }

    [Serializable]
    public class DialogNode
    {
        [Header("Node Identity")]
        public string nodeId;
        public string displayName;

        [Header("Dialog Content")]
        [Tooltip("Who is speaking (NPC name or 'Player')")]
        public string speaker;

        [TextArea(3, 6)]
        public string text;

        [Header("Speaker Visual")]
        public Sprite speakerPortrait;

        [Header("Choices")]
        [Tooltip("Player choices available at this node (empty = auto-advance)")]
        public List<DialogChoice> choices = new List<DialogChoice>();

        [Header("Actions")]
        [Tooltip("Actions to execute when this node is reached")]
        public List<DialogAction> actions = new List<DialogAction>();

        [Header("Navigation")]
        [Tooltip("Next node ID (used if no choices, auto-advances)")]
        public string nextNodeId;

        [Tooltip("End dialog after this node")]
        public bool isEndNode = false;

        [Header("Editor Data")]
        [HideInInspector]
        public Vector2 editorPosition;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(nodeId) && !string.IsNullOrEmpty(displayName))
            {
                nodeId = displayName.Replace(" ", "_").ToLower();
            }
        }
    }

    [Serializable]
    public class DialogChoice
    {
        [Tooltip("Text displayed for this choice")]
        public string text;

        [Tooltip("Node to jump to when this choice is selected")]
        public string nextNodeId;

        [Header("Conditions")]
        [Tooltip("Only show this choice if condition is met")]
        public DialogCondition condition;

        [Header("Actions")]
        [Tooltip("Actions to execute when this choice is selected")]
        public List<DialogAction> actions = new List<DialogAction>();
    }

    [Serializable]
    public class DialogAction
    {
        public DialogActionType actionType;

        [Header("Quest Actions")]
        [Tooltip("Quest ID for start/complete quest actions")]
        public string questId;

        [Header("Item Actions")]
        [Tooltip("Item ID for give/take item actions")]
        public string itemId;
        public int itemAmount = 1;

        [Header("Gold Actions")]
        public int goldAmount = 0;

        [Header("Dialog Tree Actions")]
        [Tooltip("Dialog tree to switch to (for modular dialogs)")]
        public DialogTree targetDialogTree;
        
        [Tooltip("Close current dialog before switching (if false, continues in same dialog session)")]
        public bool closeBeforeSwitch = false;
    }

    public enum DialogActionType
    {
        None,
        StartQuest,
        CompleteQuest,
        GiveItem,
        TakeItem,
        GiveGold,
        TakeGold,
        SwitchDialogTree,  // Jump to another dialog tree
        CustomEvent  // For future extensibility
    }

    [Serializable]
    public class DialogCondition
    {
        public DialogConditionType conditionType;

        [Header("Quest Conditions")]
        public string questId;
        public QuestStatus requiredQuestStatus;

        [Header("Item Conditions")]
        public string itemId;
        public int requiredItemAmount = 1;

        public bool Evaluate()
        {
            // Conditions will be evaluated by DialogController
            // This is just the data structure
            return true; // Default implementation
        }
    }

    public enum DialogConditionType
    {
        None,
        QuestStatus,
        QuestCompleted,
        QuestActive,
        HasItem,
        HasEnoughGold
    }
}