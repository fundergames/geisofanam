# Quick Start Guide - Modular Character System

## 5-Minute Setup

### Step 1: Create a Body Part Asset
1. Right-click in Project: `Create > Funder Games > Rogue Deal > Character > Body Part`
2. Name it: `BodyPart_Head_Warrior`
3. Assign the body part prefab (with SkinnedMeshRenderer)
4. Set category: `Head`

### Step 2: Create Character Visual Data
1. Right-click: `Create > Funder Games > Rogue Deal > Character > Visual Data`
2. Name it: `CharacterVisual_Warrior`
3. Assign base character prefab
4. Add body parts to the list

### Step 3: Add Components to Prefab
1. Open your base character prefab
2. Add `CharacterVisualManager` component to root
3. Add `EquipmentAttachmentPoint` components to bones:
   - Right hand bone → `weapon_r` (slot: Weapon)
   - Head bone → `helmet` (slot: Helmet)

### Step 4: Initialize in Code
```csharp
CharacterVisualManager visual = character.GetComponent<CharacterVisualManager>();
CharacterVisualData data = Resources.Load<CharacterVisualData>("CharacterVisual_Warrior");
visual.Initialize(data, playerCharacter);
```

## Common Attachment Point Names

| Slot | Common Names |
|------|-------------|
| Weapon | `weapon_r`, `weapon_l`, `weapon`, `hand_r`, `hand_l` |
| Helmet | `helmet`, `head`, `hat` |
| Armor | `armor`, `chest`, `torso` |
| Arms | `arms`, `shoulders` |
| Legs | `legs`, `pants` |
| Accessory | `accessory`, `back`, `cloak` |

## Quick Checklist

- [ ] Body parts extracted to separate prefabs
- [ ] CharacterBodyPartData assets created
- [ ] Base character prefab has skeleton only
- [ ] CharacterVisualManager added to prefab
- [ ] EquipmentAttachmentPoint components added to bones
- [ ] CharacterVisualData asset created
- [ ] Code updated to initialize system

## Troubleshooting

**Body parts not showing?**
- Check body parts use same skeleton
- Verify CharacterVisualData is assigned
- Check body parts are set to `visibleByDefault = true`

**Equipment not attaching?**
- Verify EquipmentAttachmentPoint components exist
- Check attachment point names match conventions
- Ensure equipment items have `equipmentModel` assigned

**Need more help?**
See `MODULAR_CHARACTER_SYSTEM_GUIDE.md` for detailed documentation.

