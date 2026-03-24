# Quest System Setup Guide

## Overview

The quest system allows you to create quests that track player progress through objectives and reward completion. The system integrates with the existing combat events to automatically track progress.

## Components Created

1. **QuestDefinition** - ScriptableObject for defining quests
2. **QuestService** - Service that manages quest state and progress
3. **QuestInfoDisplay** - Component to display a single quest using the QuestInfo prefab
4. **QuestPanel** - Component that manages a list of active quests
5. **QuestSignalBridge** - Converts game events into quest signals

## Setup Steps

### 1. Register QuestService in BootstrapConfig

1. Open `Assets/RogueDeal/Resources/Configs/BootstrapConfig_RogueDeal` in Inspector
2. In the **Services** array, click `+` to add a new entry
3. Configure:
   - **Interface Script**: `Assets/RogueDeal/Scripts/Quests/IQuestService.cs`
   - **Implementation Script**: `Assets/RogueDeal/Scripts/Quests/QuestService.cs`
   - **Order**: `10` (after EventBus, before gameplay services)
   - **Config**: (leave empty)

### 2. Add QuestSignalBridge to Entry Scene

1. Open the Entry scene
2. Find or create a GameObject (can be on GameBootstrap or a separate object)
3. Add Component → `QuestSignalBridge`
4. This will automatically convert combat events to quest signals

### 3. Create Quest Resources Folder

1. Create folder: `Assets/RogueDeal/Resources/Quests`
2. This is where quest definitions will be stored

### 4. Create a Test Quest

1. Right-click in Project → `Create → Funder Games → Rogue Deal → Quests → Quest Definition`
2. Name it: `Quest_Defeat5Enemies`
3. Configure:
   - **Quest Id**: `defeat_5_enemies`
   - **Display Name**: `Defeat 5 Enemies`
   - **Description**: `Test your combat skills by defeating 5 enemies`
   - **Icon**: (optional, assign a sprite)
   - **Objectives**: Add one objective:
     - **Objective Id**: `defeat_enemies`
     - **Description**: `Defeat 5 enemies`
     - **Signal Key**: `enemy_defeated`
     - **Target Id**: (leave empty for any enemy)
     - **Target Amount**: `5`
   - **Gold Reward**: `100`
   - **XP Reward**: `50`
4. Save the asset to `Assets/RogueDeal/Resources/Quests/`

### 5. Setup Quest UI

1. Find or create a Canvas in your scene (where you want quests to display)
2. Create an empty GameObject as a container (e.g., "QuestPanel")
3. Add Component → `QuestPanel`
4. Configure:
   - **Quest Info Prefab**: Drag `Assets/RogueDeal/Prefabs/UI/QuestInfo.prefab`
   - **Quest Container**: Assign the container GameObject (or leave as self)
   - **Max Displayed Quests**: `5`
   - **Only Show Active Quests**: ✓

### 6. Add QuestInfoDisplay Component (Optional)

If you want to manually control quest displays, add the `QuestInfoDisplay` component to the QuestInfo prefab:
1. Open `Assets/RogueDeal/Prefabs/UI/QuestInfo.prefab`
2. Add Component → `QuestInfoDisplay`
3. Assign references:
   - **Title Text**: `TextMission` (TextMeshProUGUI)
   - **Info Text**: `TextMissionInfo` (TextMeshProUGUI)
   - **Icon Image**: `HomeMisstionIcon` (Image)

Note: The QuestPanel will automatically populate these if you don't add the component manually.

## Starting a Quest

To start a quest programmatically:

```csharp
var questService = GameBootstrap.ServiceLocator.Resolve<IQuestService>();
questService.TryStartQuest("defeat_5_enemies");
```

Or create a simple test button/method to start quests.

## Quest Signal Types

Current signals available:
- `enemy_defeated` - Triggered when any enemy is defeated
  - Use empty `targetId` for any enemy
  - Use specific enemy `enemyId` for specific enemies
- `combat_completed` - Triggered when combat ends successfully

## Example Quest Objectives

**Defeat Enemies:**
- Signal Key: `enemy_defeated`
- Target Id: (empty) or specific enemy ID
- Target Amount: number to defeat

**Complete Combats:**
- Signal Key: `combat_completed`
- Target Id: (empty)
- Target Amount: number of combats

## Testing

1. Start the game from Entry scene
2. In code or via button, start the test quest: `questService.TryStartQuest("defeat_5_enemies")`
3. Enter combat and defeat 5 enemies
4. The quest should automatically complete and the UI should update

## Notes

- Quest progress is saved automatically via PlayerPrefs
- Quests must be in `Resources/Quests/` folder to be loaded automatically
- The QuestPanel automatically refreshes when quest state changes
- Only active quests are displayed by default (set `Only Show Active Quests` to false to show completed)
