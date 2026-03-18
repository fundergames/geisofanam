# Step-by-Step Setup Guide - Modular Character System

## Overview
This guide will walk you through converting your existing `TestPlayer1.prefab` to use the new modular system.

## Step 1: Extract Body Parts from TestPlayer1.prefab

### 1.1 Identify Body Parts
1. Open `TestPlayer1.prefab` in Unity
2. Look for all the `Body##` GameObjects (Body19, Body17, Body16, etc.)
3. These are your body part meshes with SkinnedMeshRenderer components

### 1.2 Create Body Part Prefabs
For each body part mesh:

1. **In the Hierarchy** (when prefab is open):
   - Right-click on a body part GameObject (e.g., "Body19")
   - Select "Unpack Prefab" (if it's part of a nested prefab)
   - Drag it out of the prefab hierarchy temporarily

2. **Create New Prefab**:
   - Drag the body part GameObject from Hierarchy to Project window
   - Save it to: `Assets/RogueDeal/Combat/Prefabs/BodyParts/`
   - Name it descriptively: `BodyPart_Head.prefab`, `BodyPart_Torso.prefab`, etc.

3. **Repeat** for all body parts you want to make modular

### 1.3 Create BodyPartData Assets
For each body part prefab you created:

1. Right-click in Project: `Create > Funder Games > Rogue Deal > Character > Body Part`
2. Name it to match: `BodyPartData_Head.asset`, `BodyPartData_Torso.asset`, etc.
3. Configure each asset:
   - **bodyPartName**: "Head", "Torso", "Arms", etc.
   - **category**: Select appropriate category (Head, Torso, Arms, etc.)
   - **bodyPartPrefab**: Drag the body part prefab you created
   - **visibleByDefault**: `true`
   - **canBeHiddenByEquipment**: `true` (so equipment can hide it)

## Step 2: Create Base Character Prefab

### 2.1 Create New Base Prefab
1. Create a new prefab: `Assets/RogueDeal/Combat/Prefabs/BaseCharacter.prefab`
2. Start with a copy of TestPlayer1.prefab structure but **remove all body part meshes**

### 2.2 Keep Essential Components
Your base prefab should have:
- ✅ Root GameObject with name (e.g., "BaseCharacter")
- ✅ **Animator** component (with Avatar and Controller)
- ✅ **CombatEntity** component
- ✅ **PlayerVisual** component (if used)
- ✅ **All skeleton bones** (root, neck_01, hand_r, hand_l, etc.)
- ✅ **Attachment point transforms** (weapon_r, weapon_l, etc.)

### 2.3 Remove Body Parts
Remove or disable:
- ❌ All `Body##` GameObjects (Body19, Body17, etc.)
- ❌ Equipment meshes (they'll be attached at runtime)
- ❌ Keep the skeleton bones and attachment points!

### 2.4 Add CharacterVisualManager
1. Select the root GameObject of your base prefab
2. Add Component: `Character Visual Manager`
3. Configure:
   - **bodyPartsRoot**: Drag the root transform (or leave null to use root)
   - **animator**: Drag the Animator component

## Step 3: Add Equipment Attachment Points

### 3.1 Find Attachment Bones
In your base prefab, locate these bones:
- `weapon_r` (right hand)
- `weapon_l` (left hand)  
- `neck_01` or head bone (for helmet)
- Torso bone (for armor)
- etc.

### 3.2 Add EquipmentAttachmentPoint Components
For each attachment location:

1. Select the bone GameObject (e.g., `weapon_r`)
2. Add Component: `Equipment Attachment Point`
3. Configure:
   - **attachmentPointName**: "weapon_r" (match the bone name)
   - **slot**: Select `Weapon`
   - **positionOffset**: (0, 0, 0) - adjust if needed
   - **rotationOffset**: (0, 0, 0) - adjust if needed
   - **scale**: (1, 1, 1) - adjust if needed
   - **hideBodyPartsWhenEquipped**: `true` if equipment should hide body parts
   - **bodyPartsToHide**: Add categories like `Hands` if equipping weapon should hide hands

### 3.3 Common Attachment Points
Set up these attachment points:

| Bone Name | Slot | Purpose |
|-----------|------|---------|
| `weapon_r` | Weapon | Right hand weapon |
| `weapon_l` | Weapon | Left hand weapon (optional) |
| `neck_01` or head bone | Helmet | Head equipment |
| Torso bone | Armor | Chest armor |
| Shoulder bones | Arms | Arm/shoulder equipment |
| Hip/leg bones | Legs | Leg equipment |
| Back bone | Accessory | Cloaks, backpacks, etc. |

## Step 4: Create Character Visual Data

### 4.1 Create Visual Data Asset
1. Right-click in Project: `Create > Funder Games > Rogue Deal > Character > Visual Data`
2. Name it: `CharacterVisual_Warrior.asset` (or your character name)

### 4.2 Configure Visual Data
1. Select the asset
2. In Inspector:
   - **characterName**: "Warrior" (or your character name)
   - **baseCharacterPrefab**: Drag your `BaseCharacter.prefab`
   - **bodyParts**: Click `+` and add all your `BodyPartData_*.asset` files
   - **scaleMultiplier**: 1.0 (adjust if needed)

### 4.3 Add Default Equipment (Optional)
If you want default equipment:
1. In **defaultEquipment** list, click `+`
2. Set **slot**: e.g., `Weapon`
3. Set **equipmentItem**: Drag an EquipmentItem asset that has an `equipmentModel` assigned

## Step 5: Use in Code

### 5.1 Basic Usage
```csharp
using RogueDeal.Combat;
using RogueDeal.Combat.Visual;
using RogueDeal.Player;

// When spawning a character
GameObject characterPrefab = Resources.Load<GameObject>("Prefabs/BaseCharacter");
GameObject character = Instantiate(characterPrefab);

// Get the visual manager
CharacterVisualManager visualManager = character.GetComponent<CharacterVisualManager>();

// Load character visual data
CharacterVisualData visualData = Resources.Load<CharacterVisualData>("Characters/CharacterVisual_Warrior");

// Initialize
visualManager.Initialize(visualData, playerCharacter);
```

### 5.2 With PlayerVisual Integration
If you're using `PlayerVisual`, it will automatically initialize the visual manager:

```csharp
// In your character spawning code
GameObject character = Instantiate(baseCharacterPrefab);
PlayerVisual playerVisual = character.GetComponent<PlayerVisual>();

// Assign visual data to PlayerVisual
playerVisual.characterVisualData = visualData; // Set in Inspector or via code

// Initialize PlayerVisual (this will also initialize CharacterVisualManager)
playerVisual.Initialize(playerCharacter);
```

### 5.3 Runtime Equipment Changes
The system automatically handles equipment from `PlayerCharacter`:

```csharp
// When player equips an item
playerCharacter.equipment[EquipmentSlot.Weapon] = swordItem;

// The CharacterVisualManager will automatically:
// 1. Find the weapon_r attachment point
// 2. Instantiate the swordItem.equipmentModel
// 3. Attach it to the bone
// 4. Hide body parts if configured
```

## Step 6: Testing

### 6.1 Test in Scene
1. Create a test scene
2. Add your `BaseCharacter.prefab` to the scene
3. Add `CharacterVisualManager` component if not already present
4. In Inspector, assign a `CharacterVisualData` asset
5. Press Play - body parts should appear!

### 6.2 Test Equipment
1. Create an `EquipmentItem` asset
2. Assign an `equipmentModel` prefab to it
3. In code or Inspector, equip it to the character
4. Verify it appears at the correct attachment point

## Quick Reference

### File Structure
```
Assets/RogueDeal/Combat/
├── Prefabs/
│   ├── BaseCharacter.prefab (skeleton only)
│   ├── BodyParts/
│   │   ├── BodyPart_Head.prefab
│   │   ├── BodyPart_Torso.prefab
│   │   └── ...
│   └── TestPlayer1.prefab (original, can be kept for reference)
├── Scripts/Combat/Visual/
│   ├── CharacterVisualManager.cs
│   ├── CharacterVisualData.cs
│   ├── CharacterBodyPartData.cs
│   └── EquipmentAttachmentPoint.cs
└── Data/Characters/ (create this folder)
    ├── CharacterVisual_Warrior.asset
    ├── BodyPartData_Head.asset
    ├── BodyPartData_Torso.asset
    └── ...
```

### Common Issues & Solutions

**Body parts not showing?**
- Check that body parts use the same skeleton/rig
- Verify CharacterVisualData has body parts assigned
- Check body parts are set to `visibleByDefault = true`

**Equipment not attaching?**
- Verify EquipmentAttachmentPoint components exist on bones
- Check attachment point names match (weapon_r, etc.)
- Ensure EquipmentItem has `equipmentModel` prefab assigned

**Body parts clipping with equipment?**
- Enable `hideBodyPartsWhenEquipped` on attachment point
- Add body part categories to `bodyPartsToHide` array

## Next Steps

1. ✅ Extract body parts from TestPlayer1.prefab
2. ✅ Create BodyPartData assets
3. ✅ Create BaseCharacter prefab
4. ✅ Add EquipmentAttachmentPoint components
5. ✅ Create CharacterVisualData asset
6. ✅ Test in scene
7. ✅ Integrate into your character spawning code

## Example: Complete Setup for One Character

1. **Extract 4 body parts**: Head, Torso, Arms, Legs
2. **Create 4 BodyPartData assets**: One for each body part
3. **Create BaseCharacter prefab**: Skeleton + components only
4. **Add 2 attachment points**: weapon_r (Weapon), neck_01 (Helmet)
5. **Create CharacterVisual_Warrior.asset**: Assign all 4 body parts
6. **In code**: `visualManager.Initialize(warriorVisual, playerCharacter)`

Done! Your character is now modular and customizable.

