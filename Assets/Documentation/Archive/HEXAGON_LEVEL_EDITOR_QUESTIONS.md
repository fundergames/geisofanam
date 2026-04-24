# Hexagon Level Editor - Remaining Questions

## Critical Questions (Need answers before starting implementation)

### 1. Height System ⚠️ **MOST IMPORTANT**
The KayKit pack has sloped tiles (low/high slopes). What level of height support do you want?

**Option A: Slopes Only** (Simplest)
- Use existing sloped tile variants (hex_grass_sloped_low, hex_grass_sloped_high)
- No vertical stacking
- Fastest to implement

**Option B: Full 3D Height** (Most Flexible)
- Support multiple elevation levels (stack tiles vertically)
- Can create multi-level structures
- More complex, but allows for complex terrain

**Option C: Both** (Recommended)
- Use slopes for terrain variation
- Also support stacking for structures/buildings
- Best of both worlds

**Which option do you prefer?**

---

### 2. Save Location
Where should hex level data files be saved?

**Options:**
- `Assets/RogueDeal/Resources/Data/HexLevels/` (matches existing level structure)
- `Assets/HexLevels/` (dedicated folder)
- `Assets/RogueDeal/Resources/Data/Levels/Hex/` (subfolder of existing)
- Other location?

---

### 3. Scene Management
How should levels be managed in Unity scenes?

**Option A: Data-Only** (Recommended for runtime)
- Levels saved as ScriptableObjects
- Runtime loads data and instantiates prefabs
- No scene files per level
- More flexible, easier to version control

**Option B: Scene-Based**
- Each level is a Unity scene
- Editor works directly in scene
- Simpler for editor, but harder for runtime loading

**Option C: Hybrid**
- Editor works in scenes
- Export to ScriptableObject for runtime
- Best of both, but more complex

**Which approach do you prefer?**

---

### 4. Integration with Existing Systems
Should hex levels integrate with the existing `LevelDefinition` system?

**Option A: Extend LevelDefinition**
- Add hex grid data to existing `LevelDefinition` ScriptableObject
- Single system for all levels
- May require refactoring existing levels

**Option B: Separate System**
- Create new `HexLevelDefinition` ScriptableObject
- Keep separate from existing level system
- Can integrate later if needed

**Option C: Optional Integration**
- Hex levels are separate by default
- Optional reference from `LevelDefinition` to hex level
- Most flexible

**Which approach?**

---

### 5. Hex Tile Size
What's the size/scale of the hex tiles? (We can measure from prefabs, but if you know the intended size, that helps)

**Default assumption**: We'll measure from the prefab meshes and use that as the base size.

---

## Nice-to-Have (Can decide during implementation)

### 6. Grid Origin
- Center at (0,0,0) - hex at origin is center of map
- Corner-based - origin is at corner of grid
- **Recommendation**: Center-based (simpler math)

### 7. Undo/Redo Depth
- How many operations to remember?
- **Recommendation**: 50-100 operations

### 8. Default Hex Size
- If we need a default, what should it be?
- **Recommendation**: Measure from prefabs automatically

---

## Quick Answers Needed

Please answer:
1. **Height System**: A, B, or C?
2. **Save Location**: Path preference?
3. **Scene Management**: A, B, or C?
4. **Integration**: A, B, or C?

Once these are answered, we can start implementation! 🚀
