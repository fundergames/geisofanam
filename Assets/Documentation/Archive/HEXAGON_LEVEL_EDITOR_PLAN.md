# Hexagon Level Editor - Implementation Plan

## Overview
A comprehensive level editor for creating hexagon-based maps using the KayKit Medieval Hexagon Pack. Supports both procedural generation and manual placement tools for intuitive level design.

## Asset Structure Analysis

### Available Assets
- **Tiles**: Base (grass, water, slopes), Coast (A-E variants), Rivers (multiple variants), Roads (A-M variants)
- **Buildings**: 4 color variants (blue, green, red, yellow) + neutral, ~20+ building types per color
- **Units**: 4 color variants + neutral, various unit types (soldiers, cavalry, siege, etc.)
- **Decorations**: Nature (trees, rocks, mountains), Props (crates, barrels, flags, etc.)

## Core Functionality Requirements

### 1. Hexagonal Grid System
- **Hex Coordinate System**: Axial (q, r) or Offset coordinates
- **Grid Management**: 
  - Efficient storage and lookup of hex positions
  - Neighbor detection (6 directions)
  - Distance calculations
  - Pathfinding support
- **Visual Grid**: Optional overlay showing hex boundaries and coordinates

### 2. Tile Placement & Management
- **Tile Types**: Base, Coast, River, Road
- **Smart Placement**: 
  - Auto-detect neighboring tiles for context-aware variants
  - River/road connection logic
  - Coast tile selection based on water neighbors
- **Tile Stacking**: Support for height variations (slopes, multi-level)
- **Tile Replacement**: Easy swap between tile types

### 3. Object Placement System
- **Categories**: Buildings, Units, Decorations
- **Placement Modes**:
  - Single placement (click to place)
  - Multi-placement (drag to place multiple)
  - Brush tool (paint multiple instances)
- **Rotation**: 60-degree increments (6 directions for hexagons)
- **Snapping**: Automatic alignment to hex grid
- **Validation**: Prevent invalid placements (e.g., water buildings on land)

### 4. Procedural Generation
- **Biome Generation**: 
  - Grasslands, forests, coastlines, rivers
  - Configurable size and distribution
- **Building Placement**:
  - Random distribution with density controls
  - Cluster generation (towns, villages)
  - Road network generation
- **Decoration Scatter**:
  - Natural decoration placement (trees, rocks)
  - Density and variation controls
- **Seed System**: Reproducible generation with seed values
- **Layers**: Generate different layers (terrain, roads, buildings, decorations)

### 5. Manual Editing Tools
- **Selection Tools**:
  - Single hex selection
  - Multi-select (box, lasso, paint)
  - Selection filters (by type, category)
- **Editing Operations**:
  - Copy/Paste
  - Delete
  - Rotate
  - Replace (swap object types)
- **Undo/Redo System**: Full history support
- **Grid Tools**:
  - Show/hide grid
  - Grid size adjustment
  - Coordinate display

### 6. Level Data Management
- **Save/Load System**:
  - Serialize hex grid state
  - Save as ScriptableObject or JSON
  - Version control support
- **Level Metadata**:
  - Level name, description
  - Author, creation date
  - Tags/categories
- **Export Options**:
  - Export as prefab
  - Export as scene
  - Export data only (for runtime loading)

## System Architecture

### Core Components

#### 1. HexGrid System
```
HexGrid.cs
- Manages hex coordinate system
- Stores tile/object data
- Provides neighbor/distance calculations
- Handles coordinate conversions (world <-> hex)
```

#### 2. HexTile Component
```
HexTile.cs
- Represents a single hex cell
- Stores tile type and objects
- Handles visual representation
- Manages placement validation
```

#### 3. Level Editor Controller
```
HexLevelEditor.cs
- Main editor controller
- Tool management
- Input handling
- Undo/redo system
- Save/load operations
```

#### 4. Procedural Generator
```
HexProceduralGenerator.cs
- Biome generation algorithms
- Object placement logic
- Noise-based generation
- Rule-based placement
```

#### 5. Editor Tools
```
- PlacementTool.cs
- SelectionTool.cs
- BrushTool.cs
- EraseTool.cs
- PaintTool.cs
```

#### 6. Data Structures
```
HexLevelData.cs (ScriptableObject)
- Serialized level data
- Hex grid state
- Object placements
- Metadata

HexCoordinate.cs
- Hex coordinate struct
- Conversion utilities
- Neighbor calculations
```

### Editor Window
```
HexLevelEditorWindow.cs
- Main editor UI
- Tool palette
- Asset browser
- Property panels
- Generation controls
```

## User Interface Design

### Editor Window Layout
```
┌─────────────────────────────────────────────────┐
│  Toolbar: [Select] [Place] [Brush] [Erase] ... │
├──────────┬──────────────────────────┬──────────┤
│          │                          │          │
│  Tools   │    Scene View            │ Properties│
│  Panel   │    (Hex Grid)            │ Panel    │
│          │                          │          │
│  [Tile]  │                          │ Selected │
│  [Build] │                          │ Object   │
│  [Unit]  │                          │ Details  │
│  [Deco]  │                          │          │
│          │                          │          │
│  Asset   │                          │          │
│  Browser │                          │          │
│          │                          │          │
├──────────┴──────────────────────────┴──────────┤
│  Status Bar: [Grid: 20x20] [Mode: Edit]        │
└─────────────────────────────────────────────────┘
```

### Key UI Elements
1. **Tool Palette**: Visual buttons for all editing tools
2. **Asset Browser**: Categorized list of placeable assets
3. **Properties Panel**: Edit selected object properties
4. **Generation Panel**: Procedural generation controls
5. **Grid Controls**: Toggle grid, adjust size, coordinate display
6. **Level Info**: Name, description, metadata editing

## Feature Breakdown

### Phase 1: Core Foundation ✅ COMPLETE
- [x] Hex coordinate system implementation
- [x] Basic hex grid data structure
- [x] World-to-hex coordinate conversion
- [x] Hex neighbor calculations
- [x] Basic tile placement (data structure ready)
- [x] Simple editor window (visualization working)

### Phase 2: Manual Editing
- [ ] Selection system (single/multi)
- [ ] Object placement tool
- [ ] Rotation support
- [ ] Delete/erase tool
- [ ] Grid visualization
- [ ] Basic undo/redo

### Phase 3: Smart Placement
- [ ] Context-aware tile selection (coast, rivers, roads)
- [ ] Auto-connection logic for roads/rivers
- [ ] Placement validation
- [ ] Snap-to-grid refinement

### Phase 4: Procedural Generation
- [ ] Basic terrain generation (grass, water)
- [ ] Biome generation
- [ ] Random object placement
- [ ] Seed-based generation
- [ ] Generation presets

### Phase 5: Advanced Tools
- [ ] Brush tool (paint multiple)
- [ ] Copy/paste functionality
- [ ] Multi-select operations
- [ ] Advanced undo/redo
- [ ] Grid manipulation (resize, clear)

### Phase 6: Polish & Optimization
- [ ] Save/load system
- [ ] Export functionality
- [ ] Performance optimization
- [ ] UI polish
- [ ] Documentation

## Resolved Decisions

### Confirmed Requirements
- **Max Grid Size**: 100x100 hexes (10,000 total)
- **Runtime Loading**: Both editor and runtime level loading required
- **Color Variants**: UI selector to switch between blue/green/red/yellow/neutral variants
- **Placement Rules**: 
  - Manual editing: Free placement (user can place anything anywhere)
  - Procedural generation: Smart rules (buildings on land, appropriate tile connections, etc.)

### Technical Decisions
- **Coordinate System**: Axial (q, r) - simpler math and neighbor calculations
- **Data Format**: ScriptableObject for level data (compatible with Unity's asset system)
- **Save Format**: Store only occupied hexes for efficiency

## Technical Considerations

### Hex Coordinate System
- **Axial Coordinates** (q, r): Recommended for simplicity
- **Conversion Formulas**:
  - World to Hex: `q = (√3/3 * x - 1/3 * z) / size`
  - Hex to World: `x = size * (√3 * q + √3/2 * r)`
- **Neighbor Offsets**: `[(1,0), (1,-1), (0,-1), (-1,0), (-1,1), (0,1)]`
- **Grid Bounds**: -50 to +50 in both q and r (100x100 centered at origin)

### Performance
- **Spatial Partitioning**: Use dictionary for O(1) hex lookup
- **Object Pooling**: Reuse tile/object GameObjects
- **LOD System**: Consider for large maps
- **Batching**: Static batching for placed objects

### Data Serialization
- **Compact Format**: Store only occupied hexes
- **Versioning**: Support for future format changes
- **Compression**: Consider for large levels

## Integration Points

### With Existing Systems
- **LevelDefinition**: Extend to support hex-based level layouts
- **Combat System**: Use hex grid for combat positioning
- **Navigation**: Hex-based pathfinding integration

### Asset Management
- **Prefab Registry**: Auto-discover KayKit prefabs
- **Category System**: Organize by type (tile, building, unit, decoration)
- **Preview System**: Show asset previews in browser

## Future Enhancements

1. **Multi-layer Editing**: Support for multiple elevation levels
2. **Terrain Painting**: Texture/material painting on tiles
3. **Animation Support**: Animated objects/effects
4. **Lighting Tools**: Light placement and management
5. **Collision Editing**: Custom collision shapes
6. **Scripting Support**: Custom placement rules via scripts
7. **Template System**: Save/load common patterns
8. **Symmetry Tools**: Mirror/rotate entire sections
9. **Height System**: Full 3D height variation
10. **Multiplayer Editing**: Collaborative editing support

## Success Criteria

1. ✅ Intuitive placement of any KayKit asset
2. ✅ Procedural generation produces varied, interesting maps
3. ✅ Manual editing is fast and responsive
4. ✅ Levels can be saved and loaded reliably
5. ✅ Editor is performant with large maps (1000+ hexes)
6. ✅ Clear, discoverable UI
7. ✅ Undo/redo works for all operations
8. ✅ Smart placement reduces manual work

## Questions to Resolve - ANSWERS

1. **Grid Size Limits**: ✅ **100x100 maximum** (10,000 hexes max)
2. **Height System**: ⚠️ **Needs clarification** - Do you want:
   - **Option A**: Just slopes (low/high sloped tiles that exist in the pack)
   - **Option B**: Full 3D height system (stack tiles vertically, multiple elevation levels)
   - **Option C**: Both - slopes for terrain variation, but also support stacking for multi-level structures
3. **Runtime vs Editor**: ✅ **Both** - Need runtime level loading AND editor functionality
4. **Multi-scene**: ✅ **Color switching** - UI to switch between color variants (blue/green/red/yellow/neutral)
5. **Asset Variants**: ✅ **Color picker/selector** in editor UI
6. **Validation Rules**: ✅ **Manual = free placement**, **Procedural = sensible rules** (e.g., buildings on land, not water)

## Remaining Questions to Resolve

### Critical (Need before starting):
1. **Height System Clarification**: See #2 above - which option do you prefer?
2. **Hex Tile Size**: What's the actual size/scale of the hex tiles? (We'll measure from prefabs if needed)
3. **Coordinate System**: Axial (q, r) or Offset coordinates? (Axial recommended for simplicity)
4. **Save Location**: Where should level data files be saved? 
   - `Assets/RogueDeal/Resources/Data/HexLevels/`?
   - `Assets/HexLevels/`?
   - Other?
5. **Scene Management**: 
   - Create new scenes per level, or work in a single editor scene?
   - Should levels be saved as scenes or just as data (ScriptableObject)?
6. **Integration**: Should hex levels integrate with existing `LevelDefinition` system, or be separate?

### Nice to Have (Can decide during implementation):
7. **Default Hex Size**: If we need to calculate, what's a good default? (We'll measure from prefabs)
8. **Grid Origin**: Center at (0,0,0) or corner-based?
9. **Undo/Redo Depth**: How many operations to remember? (Default: 50-100)

---

## Next Steps

1. ✅ Review and approve this plan
2. ⏳ **Answer remaining critical questions** (especially height system)
3. ⏳ Begin Phase 1 implementation
4. ⏳ Iterate based on feedback
