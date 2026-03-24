using System.Collections.Generic;
using System.Linq;
using Funder.Core.Services;
using RogueDeal.Quests;
using RogueDeal.Events;
using RogueDeal.NPCs;
using UnityEngine;

namespace RogueDeal.NPCs
{
    /// <summary>
    /// Manages dialog flow and execution of dialog actions.
    /// </summary>
    public class DialogController : MonoBehaviour
    {
        private DialogTree _currentDialogTree;
        private DialogNode _currentNode;
        private NPCDefinition _currentNPC;
        private IQuestService _questService;
        private bool _nodeActionsExecuted = false;

        private void Awake()
        {
            try
            {
                _questService = GameBootstrap.ServiceLocator?.Resolve<IQuestService>();
            }
            catch (System.Exception)
            {
                // Service might not be initialized yet
            }
        }

        private void Start()
        {
            // Try to get quest service again if it wasn't available in Awake
            if (_questService == null)
            {
                try
                {
                    _questService = GameBootstrap.ServiceLocator?.Resolve<IQuestService>();
                }
                catch (System.Exception)
                {
                    Debug.LogWarning("[DialogController] IQuestService not available. Quest actions will not work.");
                }
            }
        }

        public void StartDialog(NPCDefinition npc)
        {
            if (npc == null || npc.dialogTree == null)
            {
                Debug.LogWarning($"[DialogController] Cannot start dialog - NPC or dialog tree is null");
                return;
            }

            StartDialog(npc, npc.dialogTree);
        }

        public void StartDialog(NPCDefinition npc, DialogTree dialogTree)
        {
            if (npc == null || dialogTree == null)
            {
                Debug.LogWarning($"[DialogController] Cannot start dialog - NPC or dialog tree is null");
                return;
            }

            Debug.Log($"[DialogController] Starting dialog for NPC: {npc.npcId} with tree: {dialogTree.displayName}");
            
            _currentNPC = npc;
            _currentDialogTree = dialogTree;

            DialogNode entryNode = _currentDialogTree.GetEntryNode();
            if (entryNode == null)
            {
                Debug.LogError($"[DialogController] Dialog tree '{_currentDialogTree.displayName}' has no entry node!");
                return;
            }

            Debug.Log($"[DialogController] Found entry node: {entryNode.nodeId}");
            ShowNode(entryNode);

            // Publish event
            Debug.Log($"[DialogController] Raising DialogStartedEvent");
            EventBus<DialogStartedEvent>.Raise(new DialogStartedEvent
            {
                npcId = npc.npcId,
                dialogTreeId = _currentDialogTree.dialogId
            });
        }

        public void ShowNode(DialogNode node)
        {
            if (node == null)
            {
                Debug.LogWarning("[DialogController] Cannot show null dialog node");
                return;
            }

            _currentNode = node;
            _nodeActionsExecuted = false; // Reset flag for new node

            // Don't execute actions yet - wait until player has read the text and clicked continue
            // Actions will be executed in AdvanceDialog() or when choices are shown

            // Publish event for UI to display
            EventBus<DialogNodeShownEvent>.Raise(new DialogNodeShownEvent
            {
                node = node,
                npcId = _currentNPC?.npcId ?? ""
            });
        }

        public void SelectChoice(int choiceIndex)
        {
            if (_currentNode == null || _currentNode.choices == null || choiceIndex < 0 || choiceIndex >= _currentNode.choices.Count)
            {
                Debug.LogWarning($"[DialogController] Invalid choice index: {choiceIndex}");
                return;
            }

            DialogChoice choice = _currentNode.choices[choiceIndex];

            Debug.Log($"[DialogController] Choice selected: '{choice.text}' (index: {choiceIndex})");

            // Check condition
            if (choice.condition != null && !EvaluateCondition(choice.condition))
            {
                Debug.LogWarning($"[DialogController] Choice condition not met for choice: {choice.text}");
                return;
            }

            // Execute choice actions
            if (choice.actions != null && choice.actions.Count > 0)
            {
                Debug.Log($"[DialogController] Executing {choice.actions.Count} action(s) for choice: '{choice.text}'");
                ExecuteActions(choice.actions);
            }
            else
            {
                Debug.Log($"[DialogController] No actions to execute for choice: '{choice.text}'");
            }

            // Publish event
            EventBus<DialogChoiceSelectedEvent>.Raise(new DialogChoiceSelectedEvent
            {
                nodeId = _currentNode.nodeId,
                choiceIndex = choiceIndex,
                choiceText = choice.text
            });

            // Move to next node
            if (!string.IsNullOrEmpty(choice.nextNodeId))
            {
                DialogNode nextNode = _currentDialogTree.GetNode(choice.nextNodeId);
                if (nextNode != null)
                {
                    ShowNode(nextNode);
                }
                else
                {
                    Debug.LogWarning($"[DialogController] Next node '{choice.nextNodeId}' not found in dialog tree");
                    EndDialog();
                }
            }
            else
            {
                EndDialog();
            }
        }

        public void AdvanceDialog()
        {
            if (_currentNode == null)
                return;

            // Execute node actions if they haven't been executed yet
            // This happens after the player has read the text and clicked continue
            if (!_nodeActionsExecuted && _currentNode.actions != null && _currentNode.actions.Count > 0)
            {
                Debug.Log($"[DialogController] Executing {_currentNode.actions.Count} node action(s) after player clicked continue");
                ExecuteActions(_currentNode.actions);
                _nodeActionsExecuted = true;
            }

            // If node has choices, they must be selected (don't auto-advance)
            if (_currentNode.choices != null && _currentNode.choices.Count > 0)
            {
                return;
            }

            // If it's an end node, close dialog
            if (_currentNode.isEndNode)
            {
                EndDialog();
                return;
            }

            // Auto-advance to next node
            if (!string.IsNullOrEmpty(_currentNode.nextNodeId))
            {
                DialogNode nextNode = _currentDialogTree.GetNode(_currentNode.nextNodeId);
                if (nextNode != null)
                {
                    ShowNode(nextNode);
                }
                else
                {
                    EndDialog();
                }
            }
            else
            {
                EndDialog();
            }
        }

        public void EndDialog()
        {
            string npcId = _currentNPC?.npcId ?? "";

            _currentNode = null;
            _currentDialogTree = null;

            EventBus<DialogEndedEvent>.Raise(new DialogEndedEvent
            {
                npcId = npcId
            });
        }

        private void ExecuteActions(List<DialogAction> actions)
        {
            if (actions == null || actions.Count == 0)
                return;

            foreach (var action in actions)
            {
                ExecuteAction(action);
            }
        }

        private void ExecuteAction(DialogAction action)
        {
            if (action == null || action.actionType == DialogActionType.None)
                return;

            // Ensure quest service is available before executing quest actions
            if (action.actionType == DialogActionType.StartQuest || action.actionType == DialogActionType.CompleteQuest)
            {
                if (_questService == null)
                {
                    try
                    {
                        _questService = GameBootstrap.ServiceLocator?.Resolve<IQuestService>();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[DialogController] Cannot execute quest action - IQuestService not available: {e.Message}");
                        return;
                    }
                }

                if (_questService == null)
                {
                    Debug.LogError("[DialogController] Cannot execute quest action - IQuestService is null");
                    return;
                }
            }

            switch (action.actionType)
            {
                case DialogActionType.StartQuest:
                    if (!string.IsNullOrEmpty(action.questId))
                    {
                        Debug.Log($"[DialogController] Attempting to start quest: {action.questId}");
                        
                        // Check if quest is already active - this is fine, just acknowledge it
                        if (_questService.IsQuestActive(action.questId))
                        {
                            Debug.Log($"[DialogController] Quest {action.questId} is already active. Continuing dialog.");
                            break;
                        }
                        
                        // Check if quest is already completed - also fine, just acknowledge it
                        if (_questService.IsQuestCompleted(action.questId))
                        {
                            Debug.Log($"[DialogController] Quest {action.questId} is already completed. Continuing dialog.");
                            break;
                        }
                        
                        // Try to start the quest
                        bool success = _questService.TryStartQuest(action.questId);
                        if (success)
                        {
                            Debug.Log($"[DialogController] ✅ Successfully started quest: {action.questId}");
                        }
                        else
                        {
                            // If it failed, check why and log appropriately
                            if (_questService.TryGetProgress(action.questId, out var existingProgress))
                            {
                                Debug.Log($"[DialogController] Quest {action.questId} already exists with status: {existingProgress.status}. Continuing dialog.");
                            }
                            else
                            {
                                Debug.LogError($"[DialogController] ❌ Failed to start quest: {action.questId}. Quest definition may not exist. Check console for QuestService error messages.");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("[DialogController] StartQuest action has no questId specified!");
                    }
                    break;

                case DialogActionType.CompleteQuest:
                    if (_questService != null && !string.IsNullOrEmpty(action.questId))
                    {
                        // Note: QuestService doesn't have TryCompleteQuest, only checks if completed
                        // We might need to add this, or quests auto-complete when objectives are done
                        Debug.LogWarning($"[DialogController] CompleteQuest action not fully implemented. Quest: {action.questId}");
                    }
                    break;

                case DialogActionType.GiveGold:
                    // TODO: Integrate with player inventory/gold system
                    Debug.Log($"[DialogController] Give gold: {action.goldAmount} (not yet implemented)");
                    break;

                case DialogActionType.TakeGold:
                    // TODO: Integrate with player inventory/gold system
                    Debug.Log($"[DialogController] Take gold: {action.goldAmount} (not yet implemented)");
                    break;

                case DialogActionType.GiveItem:
                case DialogActionType.TakeItem:
                    // TODO: Integrate with player inventory system
                    Debug.Log($"[DialogController] Item action: {action.actionType} {action.itemAmount}x {action.itemId} (not yet implemented)");
                    break;

                case DialogActionType.SwitchDialogTree:
                    if (action.targetDialogTree != null)
                    {
                        Debug.Log($"[DialogController] Switching to dialog tree: {action.targetDialogTree.displayName}");
                        
                        if (action.closeBeforeSwitch)
                        {
                            EndDialog();
                            if (_currentNPC != null)
                            {
                                StartDialog(_currentNPC, action.targetDialogTree);
                            }
                        }
                        else
                        {
                            SwitchToDialogTree(action.targetDialogTree);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[DialogController] SwitchDialogTree action has no target dialog tree!");
                    }
                    break;

                case DialogActionType.CustomEvent:
                    // For future extensibility
                    break;
            }
        }

        private void SwitchToDialogTree(DialogTree newTree)
        {
            if (newTree == null)
            {
                Debug.LogError("[DialogController] Cannot switch to null dialog tree!");
                return;
            }

            _currentDialogTree = newTree;
            
            DialogNode entryNode = newTree.GetEntryNode();
            if (entryNode == null)
            {
                Debug.LogError($"[DialogController] Dialog tree '{newTree.displayName}' has no entry node!");
                return;
            }

            Debug.Log($"[DialogController] Switched to dialog tree: {newTree.displayName}");
            ShowNode(entryNode);
        }

        private bool EvaluateCondition(DialogCondition condition)
        {
            if (condition == null || condition.conditionType == DialogConditionType.None)
                return true;

            if (_questService == null)
            {
                Debug.LogWarning("[DialogController] Cannot evaluate condition - IQuestService not available");
                return false;
            }

            switch (condition.conditionType)
            {
                case DialogConditionType.QuestStatus:
                    if (!string.IsNullOrEmpty(condition.questId))
                    {
                        if (_questService.TryGetProgress(condition.questId, out var progress))
                        {
                            return progress.status == condition.requiredQuestStatus;
                        }
                    }
                    return false;

                case DialogConditionType.QuestCompleted:
                    return !string.IsNullOrEmpty(condition.questId) && _questService.IsQuestCompleted(condition.questId);

                case DialogConditionType.QuestActive:
                    return !string.IsNullOrEmpty(condition.questId) && _questService.IsQuestActive(condition.questId);

                case DialogConditionType.HasItem:
                case DialogConditionType.HasEnoughGold:
                    // TODO: Implement when inventory system is integrated
                    Debug.LogWarning($"[DialogController] Condition type {condition.conditionType} not yet implemented");
                    return false;

                default:
                    return true;
            }
        }

        public DialogNode GetCurrentNode()
        {
            return _currentNode;
        }

        public List<DialogChoice> GetAvailableChoices()
        {
            if (_currentNode == null || _currentNode.choices == null)
                return new List<DialogChoice>();

            // Filter choices based on conditions
            return _currentNode.choices.Where(c => 
                c.condition == null || EvaluateCondition(c.condition)
            ).ToList();
        }

        public bool IsDialogActive()
        {
            return _currentNode != null && _currentDialogTree != null;
        }

        /// <summary>
        /// Executes pending node actions if they haven't been executed yet.
        /// Called when player clicks continue to show choices or advance dialog.
        /// </summary>
        public void ExecutePendingNodeActions()
        {
            if (_currentNode == null)
            {
                Debug.LogWarning("[DialogController] ExecutePendingNodeActions called but _currentNode is null");
                return;
            }

            Debug.Log($"[DialogController] ExecutePendingNodeActions - Node: {_currentNode.nodeId}, Actions executed: {_nodeActionsExecuted}, Actions count: {_currentNode.actions?.Count ?? 0}");

            if (!_nodeActionsExecuted && _currentNode.actions != null && _currentNode.actions.Count > 0)
            {
                Debug.Log($"[DialogController] ✅ Executing {_currentNode.actions.Count} pending node action(s) for node: {_currentNode.nodeId}");
                ExecuteActions(_currentNode.actions);
                _nodeActionsExecuted = true;
                Debug.Log($"[DialogController] ✅ Finished executing node actions");
            }
            else if (_nodeActionsExecuted)
            {
                Debug.Log($"[DialogController] Node actions already executed for node: {_currentNode.nodeId}");
            }
            else if (_currentNode.actions == null || _currentNode.actions.Count == 0)
            {
                Debug.Log($"[DialogController] No actions to execute for node: {_currentNode.nodeId}");
            }
        }
    }
}