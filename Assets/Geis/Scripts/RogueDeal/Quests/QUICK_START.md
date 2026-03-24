# Quest System - Quick Start

## ✅ What's Been Created

1. **QuestDefinition.cs** - ScriptableObject for creating quests
2. **QuestService.cs** - Service that manages quest state
3. **QuestInfoDisplay.cs** - Displays a single quest using QuestInfo prefab
4. **QuestPanel.cs** - Manages and displays active quests
5. **QuestSignalBridge.cs** - Converts game events to quest signals
6. **QuestTestStarter.cs** - Helper to start test quests

## 🚀 Quick Setup (5 minutes)

### 1. Register Service
- Open `Assets/RogueDeal/Resources/Configs/BootstrapConfig_RogueDeal`
- Add service entry:
  - Interface: `IQuestService.cs`
  - Implementation: `QuestService.cs`
  - Order: `10`

### 2. Add QuestSignalBridge
- In Entry scene, add `QuestSignalBridge` component to any GameObject

### 3. Create Quest Folder
- Create: `Assets/RogueDeal/Resources/Quests/`

### 4. Create Test Quest
- Right-click → `Create → Funder Games → Rogue Deal → Quests → Quest Definition`
- Set Quest Id: `defeat_5_enemies`
- Add objective:
  - Signal Key: `enemy_defeated`
  - Target Amount: `5`
- Save to `Resources/Quests/`

### 5. Setup UI
- Add `QuestPanel` component to a GameObject
- Assign `QuestInfo.prefab` to the Quest Info Prefab field

### 6. Test
- Add `QuestTestStarter` to a button
- Click to start quest
- Enter combat and defeat 5 enemies
- Quest completes automatically!

## 📝 Notes

- Quest progress saves automatically
- UI updates when quest state changes
- See `QUEST_SYSTEM_SETUP.md` for detailed instructions
