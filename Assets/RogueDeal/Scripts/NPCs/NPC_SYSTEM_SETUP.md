# NPC Dialog System Setup Guide

## Overview

The NPC dialog system allows you to create NPCs that players can interact with, have conversations with, and start/complete quests through dialog. It follows the same architecture patterns as the quest system.

## Components Created

1. **NPCDefinition** - ScriptableObject defining NPC data
2. **DialogTree** - ScriptableObject defining dialog flow
3. **DialogNode** - Serializable dialog node data (part of DialogTree)
4. **NPCInteractable** - Component for proximity detection and input handling
5. **DialogController** - Component managing dialog flow and actions
6. **DialogUI** - UI component for displaying dialog
7. **DialogEvents** - Event definitions for dialog system

## Setup Steps

### 1. Create NPC Definition

1. Right-click in Project → `Create → Funder Games → Rogue Deal → NPCs → NPC Definition`
2. Name it: `NPC_Villager`
3. Configure:
   - **NPC Id**: `villager_1`
   - **Display Name**: `Villager`
   - **Description**: `A friendly villager`
   - **Dialog Tree**: (will assign after creating dialog tree)
   - **Interaction Range**: `2.5`
   - **Interaction Key**: `E`

### 2. Create Dialog Tree

1. Right-click in Project → `Create → Funder Games → Rogue Deal → NPCs → Dialog Tree`
2. Name it: `Dialog_Villager_Quest`
3. Configure:
   - **Dialog Id**: `villager_quest_dialog`
   - **Display Name**: `Villager Quest Dialog`

### 3. Add Dialog Nodes

In the Dialog Tree asset:

**Node 1 - Greeting:**
- **Node Id**: `greeting`
- **Speaker**: `Villager`
- **Text**: `Hello there! I need help with something.`
- **Next Node Id**: `quest_offer`
- **Is End Node**: ☐

**Node 2 - Quest Offer:**
- **Node Id**: `quest_offer`
- **Speaker**: `Villager`
- **Text**: `Can you defeat 5 enemies for me?`
- **Choices**:
  - Choice 1:
    - **Text**: `Sure, I'll help!`
    - **Next Node Id**: `quest_accepted`
    - **Actions**: Add action → **Action Type**: `StartQuest`, **Quest Id**: `test_quest`
  - Choice 2:
    - **Text**: `Maybe later...`
    - **Next Node Id**: `quest_declined`
- **Is End Node**: ☐

**Node 3 - Quest Accepted:**
- **Node Id**: `quest_accepted`
- **Speaker**: `Villager`
- **Text**: `Thank you! I'll be waiting.`
- **Is End Node**: ✓

**Node 4 - Quest Declined:**
- **Node Id**: `quest_declined`
- **Speaker**: `Villager`
- **Text**: `Alright, come back if you change your mind.`
- **Is End Node**: ✓

4. Set **Entry Node Id**: `greeting`

### 4. Link NPC to Dialog Tree

1. Open the NPC Definition asset
2. Assign the Dialog Tree to the **Dialog Tree** field

### 5. Setup NPC in Scene

1. Create an empty GameObject in your scene
2. Name it: `NPC_Villager`
3. Add components:
   - **NPCInteractable** component
   - **DialogController** component (or it will be auto-added)
   - **Collider** component (for trigger detection)
4. Configure NPCInteractable:
   - **NPC Definition**: Drag your NPC Definition asset
   - **Use Trigger Collider**: ✓
   - **Interaction Key**: `E`
   - **Player Tag**: `Player`

5. Configure Collider:
   - **Is Trigger**: ✓ (automatically set by NPCInteractable)
   - **Size**: Adjust to interaction range (e.g., radius 2.5 for SphereCollider)

### 6. Setup Dialog UI

1. Create or find a Canvas in your scene
2. Create a Dialog Panel:
   - **Panel** (Image) for background
   - **Speaker Name** (TextMeshProUGUI)
   - **Dialog Text** (TextMeshProUGUI)
   - **Speaker Portrait** (Image, optional)
   - **Continue Button** (Button)
   - **Close Button** (Button)
   - **Choice Container** (Empty GameObject with VerticalLayoutGroup if using multiple choices)
3. Add **DialogUI** component to the Dialog Panel
4. Assign all references in Inspector:
   - **Dialog Panel**: The panel GameObject
   - **Speaker Name Text**: Speaker name TextMeshProUGUI
   - **Dialog Text**: Dialog text TextMeshProUGUI
   - **Speaker Portrait**: Portrait Image
   - **Choice Button Container**: Container for choice buttons
   - **Choice Button Prefab**: A button prefab (will be instantiated for choices)
   - **Continue Button**: Continue button
   - **Close Button**: Close button

### 7. Create Choice Button Prefab

1. Create a Button in your Dialog UI
2. Add TextMeshProUGUI as child for choice text
3. Configure button as needed
4. Save as prefab: `Assets/RogueDeal/Prefabs/UI/DialogChoiceButton.prefab`
5. Assign to DialogUI's **Choice Button Prefab** field

## Usage

1. Place NPC in scene with NPCInteractable component
2. Player walks near NPC (enters trigger area)
3. Interaction prompt appears (if configured)
4. Player presses E (or configured key)
5. Dialog UI appears showing first dialog node
6. Player selects choices or clicks Continue
7. Dialog advances through nodes
8. Quest actions execute when triggered
9. Dialog ends when reaching end node or clicking Close

## Dialog Action Types

- **StartQuest**: Starts a quest by ID
- **CompleteQuest**: Completes a quest (when implemented)
- **GiveGold/TakeGold**: Gives or takes gold
- **GiveItem/TakeItem**: Gives or takes items

## Dialog Conditions

- **QuestStatus**: Check if quest has specific status
- **QuestCompleted**: Check if quest is completed
- **QuestActive**: Check if quest is active
- **HasItem**: Check if player has item (when implemented)
- **HasEnoughGold**: Check if player has enough gold (when implemented)

## Notes

- NPCs must have a Collider component for trigger detection
- DialogController is auto-created if missing when Dialog Tree is assigned
- Dialog UI automatically subscribes to dialog events
- Quest actions integrate directly with IQuestService
- Dialog trees can be reused across multiple NPCs
