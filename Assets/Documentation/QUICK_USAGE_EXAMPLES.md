# Quick Usage Examples

## Method 1: Using the Example Script (Easiest)

1. **Create an empty GameObject** in your scene
2. **Add Component**: `Character Visual Example`
3. **Assign in Inspector**:
   - `baseCharacterPrefab`: Your BaseCharacter.prefab
   - `characterVisualData`: Your CharacterVisual_Warrior.asset
4. **Right-click the component** → `Spawn Character`
5. Done! Character appears with all body parts

## Method 2: In Your Own Code

### Basic Spawning
```csharp
using RogueDeal.Combat.Visual;
using UnityEngine;

// Spawn character
GameObject character = Instantiate(baseCharacterPrefab, position, rotation);

// Get visual manager
CharacterVisualManager visual = character.GetComponent<CharacterVisualManager>();

// Initialize with visual data
CharacterVisualData warriorVisual = Resources.Load<CharacterVisualData>("Characters/CharacterVisual_Warrior");
visual.Initialize(warriorVisual, playerCharacter);
```

### With PlayerVisual (Automatic)
```csharp
using RogueDeal.Combat;
using RogueDeal.Combat.Visual;

// Spawn character
GameObject character = Instantiate(baseCharacterPrefab);

// Get PlayerVisual
PlayerVisual playerVisual = character.GetComponent<PlayerVisual>();

// Set visual data (in Inspector or code)
playerVisual.characterVisualData = warriorVisual;

// Initialize (automatically initializes CharacterVisualManager too)
playerVisual.Initialize(playerCharacter);
```

### Equip/Unequip at Runtime
```csharp
// Equip an item
CharacterVisualManager visual = character.GetComponent<CharacterVisualManager>();
visual.EquipItem(swordItem);

// Unequip an item
visual.UnequipSlot(EquipmentSlot.Weapon);
```

## Method 3: Inspector Setup (No Code)

1. **Open BaseCharacter.prefab**
2. **Add CharacterVisualManager** component
3. **Assign CharacterVisualData** in the Inspector
4. **Save prefab**
5. **Instantiate prefab** - it will automatically use the visual data!

## Common Patterns

### Pattern 1: Character Spawner
```csharp
public class CharacterSpawner : MonoBehaviour
{
    public CharacterVisualData[] characterVariants;
    
    public GameObject SpawnCharacter(int variantIndex, Vector3 position)
    {
        GameObject character = Instantiate(baseCharacterPrefab, position, Quaternion.identity);
        CharacterVisualManager visual = character.GetComponent<CharacterVisualManager>();
        visual.Initialize(characterVariants[variantIndex], null);
        return character;
    }
}
```

### Pattern 2: Equipment System Integration
```csharp
// When player equips item
void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem item)
{
    CharacterVisualManager visual = GetComponent<CharacterVisualManager>();
    
    if (item != null)
        visual.EquipItem(item);
    else
        visual.UnequipSlot(slot);
}
```

### Pattern 3: Character Customization
```csharp
// Change character appearance at runtime
public void ChangeCharacterAppearance(CharacterVisualData newVisual)
{
    CharacterVisualManager visual = GetComponent<CharacterVisualManager>();
    visual.Initialize(newVisual, playerCharacter);
}
```

## Testing Checklist

- [ ] Body parts appear when character spawns
- [ ] Equipment attaches to correct bones
- [ ] Body parts hide when equipment is equipped (if configured)
- [ ] Multiple characters can use same base prefab with different visuals
- [ ] Equipment changes work at runtime
- [ ] Animations still work correctly

## Troubleshooting

**Nothing appears?**
- Check CharacterVisualData has body parts assigned
- Verify body part prefabs exist and are valid
- Check CharacterVisualManager is on the prefab

**Equipment doesn't attach?**
- Verify EquipmentAttachmentPoint components exist
- Check attachment point names match slot conventions
- Ensure EquipmentItem has equipmentModel assigned

**Body parts in wrong position?**
- Check body part attachment settings in BodyPartData
- Verify skeleton matches between base and body parts
- Adjust positionOffset/rotationOffset in BodyPartData

