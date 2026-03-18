# Automatic Setup Instructions

## 🚀 Quick Start

I've created an **Editor Wizard** that will automatically extract everything from your `TestPlayer1.prefab` and create all the necessary prefabs and data assets!

## How to Use

### Step 1: Open the Wizard
1. In Unity Editor, go to: **`Funder Games > Rogue Deal > Character Visual Setup Wizard`**
2. A window will open

### Step 2: Run the Setup
1. The wizard will **auto-detect** your `TestPlayer1.prefab`
2. Click **"🚀 Setup Complete Modular System"**
3. Wait for it to complete (progress bar will show)
4. Done! ✅

## What It Creates

The wizard automatically creates:

### 📦 Prefabs
- **`BaseCharacter.prefab`** - Skeleton-only character prefab
- **`BodyPart_*.prefab`** - Individual body part prefabs (one for each body part found)

### 📋 Data Assets
- **`BodyPartData_*.asset`** - Body part data assets (one for each body part)
- **`CharacterVisual_Warrior.asset`** - Complete character visual configuration

### 🎯 Components Added
- **CharacterVisualManager** on BaseCharacter
- **EquipmentAttachmentPoint** components on bones (weapon_r, weapon_l, etc.)

## Output Locations

All assets are created in:
- **Base Prefab**: `Assets/RogueDeal/Combat/Prefabs/ModularCharacters/`
- **Body Parts**: `Assets/RogueDeal/Combat/Prefabs/BodyParts/`
- **Data Assets**: `Assets/RogueDeal/Combat/Data/CharacterVisuals/`

## Using the Created Assets

### Option 1: Inspector Setup
1. Open `BaseCharacter.prefab`
2. Find `CharacterVisualManager` component
3. Assign `CharacterVisual_Warrior.asset` to the `visualData` field
4. Save prefab
5. Instantiate - it will automatically use the visual data!

### Option 2: Code
```csharp
GameObject character = Instantiate(baseCharacterPrefab);
CharacterVisualManager visual = character.GetComponent<CharacterVisualManager>();
CharacterVisualData warriorVisual = Resources.Load<CharacterVisualData>("CharacterVisuals/CharacterVisual_Warrior");
visual.Initialize(warriorVisual, playerCharacter);
```

### Option 3: Example Script
1. Add `CharacterVisualExample` component to a GameObject
2. Assign `BaseCharacter.prefab` and `CharacterVisual_Warrior.asset`
3. Right-click component → `Spawn Character`

## Step-by-Step Options

The wizard also has individual buttons if you want to do it step-by-step:

1. **📦 Extract Body Parts Only** - Just extracts body part prefabs
2. **🎨 Create Base Character Prefab** - Creates the base prefab with attachment points
3. **📋 Create All Data Assets** - Creates BodyPartData and CharacterVisualData assets

## Troubleshooting

**Wizard can't find TestPlayer1.prefab?**
- Manually drag `TestPlayer1.prefab` into the "TestPlayer1 Prefab" field

**Body parts not extracted correctly?**
- Check the Console for which body parts were found
- You can manually adjust the extraction logic in the wizard if needed

**Attachment points not added?**
- The wizard looks for bones named: `weapon_r`, `weapon_l`, `hand_r`, `hand_l`, `neck_01`, `head`
- If your bones have different names, you can manually add `EquipmentAttachmentPoint` components

**Want to customize?**
- All created assets can be edited in the Inspector
- You can create multiple `CharacterVisualData` assets for different character variants
- Mix and match body parts to create new characters!

## Next Steps

After running the wizard:

1. ✅ Test the BaseCharacter prefab in a scene
2. ✅ Verify body parts appear correctly
3. ✅ Test equipment attachment
4. ✅ Create additional character variants if needed
5. ✅ Integrate into your character spawning code

## Manual Override

If you prefer to set things up manually, see:
- `STEP_BY_STEP_SETUP.md` - Detailed manual instructions
- `MODULAR_CHARACTER_SYSTEM_GUIDE.md` - Complete system documentation

