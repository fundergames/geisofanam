# Modular Character System Guide

## Overview

The Modular Character System allows you to create custom characters by combining body parts and equipment at runtime, rather than having everything hardcoded in a single prefab.

## System Components

### 1. CharacterVisualData (ScriptableObject)
Defines a complete character configuration including:
- Body parts to use
- Default equipment
- Base prefab reference
- Scale settings

**Create via**: `Assets > Create > Funder Games > Rogue Deal > Character > Visual Data`

### 2. CharacterBodyPartData (ScriptableObject)
Defines a single body part (head, torso, arms, legs, etc.):
- Body part prefab (with SkinnedMeshRenderer)
- Material overrides
- Attachment settings
- Visibility options

**Create via**: `Assets > Create > Funder Games > Rogue Deal > Character > Body Part`

### 3. CharacterVisualManager (MonoBehaviour)
Runtime component that:
- Attaches body parts to the character
- Manages equipment attachment/detachment
- Handles body part visibility based on equipment
- Coordinates with PlayerCharacter for equipment

**Add to**: Character prefab root or model root

### 4. EquipmentAttachmentPoint (MonoBehaviour)
Defines where equipment can attach:
- Attachment point name
- Equipment slot type
- Transform offsets
- Body part hiding rules

**Add to**: Bone transforms where equipment should attach (e.g., hand_r, hand_l, head)

## Setup Workflow

### Step 1: Prepare Base Character Prefab

1. Create a base character prefab with:
   - Skeleton/rig (all bones)
   - Animator component
   - Core components (CombatEntity, etc.)
   - **NO body part meshes** (these will be added at runtime)

2. Add `CharacterVisualManager` component to the root

3. Add `EquipmentAttachmentPoint` components to bones where equipment attaches:
   - Right hand: `weapon_r` (slot: Weapon)
   - Left hand: `weapon_l` (slot: Weapon, optional)
   - Head: `helmet` (slot: Helmet)
   - Torso: `armor` (slot: Armor)
   - etc.

### Step 2: Extract Body Parts

1. For each body part mesh in your current prefab:
   - Create a new prefab with just that SkinnedMeshRenderer
   - Ensure it uses the same skeleton/rig
   - Save to `Assets/RogueDeal/Combat/Prefabs/BodyParts/`

2. Create a `CharacterBodyPartData` asset for each body part:
   - Assign the body part prefab
   - Set category (Head, Torso, Arms, etc.)
   - Configure attachment settings if needed

### Step 3: Create Character Visual Data

1. Create a `CharacterVisualData` asset:
   - Name it (e.g., "CharacterVisual_Warrior_Male")
   - Assign base character prefab
   - Add all body parts to the list
   - Optionally add default equipment

2. Create multiple variants:
   - Different body part combinations
   - Different default equipment
   - Different scales

### Step 4: Initialize Character

In your character initialization code:

```csharp
// Get CharacterVisualManager component
CharacterVisualManager visualManager = characterPrefab.GetComponent<CharacterVisualManager>();

// Load CharacterVisualData
CharacterVisualData visualData = Resources.Load<CharacterVisualData>("Characters/Warrior_Male");

// Initialize
visualManager.Initialize(visualData, playerCharacter);
```

## Equipment System Integration

The system automatically integrates with `PlayerCharacter.equipment`:

- When equipment is equipped via `PlayerCharacter`, the visual manager will:
  1. Find the appropriate attachment point
  2. Instantiate the equipment model
  3. Attach it to the bone
  4. Hide body parts if configured

- Equipment items should have:
  - `equipmentModel` prefab assigned
  - Correct `slot` enum value

## Example: Creating a Custom Character

### 1. Create Body Part Assets

```
BodyPart_Head_Warrior.asset
  - bodyPartPrefab: Prefab with head mesh
  - category: Head

BodyPart_Torso_Warrior.asset
  - bodyPartPrefab: Prefab with torso mesh
  - category: Torso

BodyPart_Arms_Warrior.asset
  - bodyPartPrefab: Prefab with arms mesh
  - category: Arms

BodyPart_Legs_Warrior.asset
  - bodyPartPrefab: Prefab with legs mesh
  - category: Legs
```

### 2. Create Character Visual Data

```
CharacterVisual_Warrior_Male.asset
  - characterName: "Warrior Male"
  - baseCharacterPrefab: BaseCharacter.prefab
  - bodyParts:
    - BodyPart_Head_Warrior
    - BodyPart_Torso_Warrior
    - BodyPart_Arms_Warrior
    - BodyPart_Legs_Warrior
  - defaultEquipment:
    - slot: Weapon, equipmentItem: Sword_Basic
```

### 3. Use in Code

```csharp
// Spawn character
GameObject character = Instantiate(baseCharacterPrefab);

// Get visual manager
CharacterVisualManager visual = character.GetComponent<CharacterVisualManager>();

// Load and apply visual data
CharacterVisualData warriorVisual = Resources.Load<CharacterVisualData>("Characters/Warrior_Male");
visual.Initialize(warriorVisual, playerCharacter);
```

## Migration from Old System

### Current Prefab Structure
```
TestPlayer1.prefab
  ├── Body19 (SkinnedMeshRenderer)
  ├── Body17 (SkinnedMeshRenderer)
  ├── Body16 (SkinnedMeshRenderer)
  ├── ... (many more body parts)
  ├── weapon_r (Transform)
  ├── weapon_l (Transform)
  └── ... (equipment meshes)
```

### New Modular Structure
```
BaseCharacter.prefab (skeleton only)
  ├── CharacterVisualManager
  ├── EquipmentAttachmentPoint (weapon_r)
  ├── EquipmentAttachmentPoint (weapon_l)
  └── ... (attachment points)

BodyPart_Head.prefab (separate)
BodyPart_Torso.prefab (separate)
... (all body parts as separate prefabs)

CharacterVisual_Warrior.asset (configuration)
  - References all body part assets
  - References base prefab
```

## Benefits

1. **Reusability**: Same base prefab for all characters
2. **Customization**: Easy to mix and match body parts
3. **Memory**: Only load what's needed
4. **Maintainability**: Update body parts independently
5. **Scalability**: Add new body parts without modifying base prefab

## Tips

- Keep body part prefabs lightweight (just the mesh)
- Use consistent naming for attachment points
- Test body parts with different skeletons to ensure compatibility
- Use material overrides in CharacterBodyPartData for color variations
- Create body part variants (e.g., different hair styles) as separate assets

## Troubleshooting

**Body parts not appearing:**
- Check that body parts use the same skeleton/rig
- Verify CharacterVisualData is assigned correctly
- Ensure body parts are set to visibleByDefault

**Equipment not attaching:**
- Verify EquipmentAttachmentPoint components are added
- Check attachment point names match slot conventions
- Ensure equipment items have equipmentModel assigned

**Body parts clipping with equipment:**
- Use hideBodyPartsWhenEquipped on attachment points
- Configure bodyPartsToHide array
- Adjust equipment model scale/position in attachment point

