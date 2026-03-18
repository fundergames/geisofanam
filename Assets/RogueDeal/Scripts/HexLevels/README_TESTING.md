# Hex Level Editor - Testing Guide

## Phase 1: Core Foundation Testing

### Step 1: Create Test Scene

1. Create a new scene: `Assets/RogueDeal/Scenes/HexLevelTest.unity`
2. Add a GameObject named "HexGrid" to the scene
3. Add the following components:
   - `HexGrid` component
   - `HexGridVisualizer` component

### Step 2: Configure HexGrid

1. Select the HexGrid GameObject
2. In the Inspector, set:
   - **Hex Size**: `1.0` (we'll measure from prefabs later)
   - **Max Width**: `100`
   - **Max Height**: `100`

### Step 3: Configure Visualizer

1. In the HexGridVisualizer component:
   - **Show Grid**: `true`
   - **Show Coordinates**: `false` (can enable for debugging)
   - **Grid Color**: White with transparency
   - **Grid Radius**: `10` (shows 10 hexes in each direction)

### Step 4: Test in Scene View

1. Enter Play mode
2. In the Scene view, you should see:
   - Hex grid outlines drawn around the origin
   - Grid should be visible as white hexagons

### Step 5: Test Coordinate Conversion

Create a simple test script to verify coordinate conversion:

```csharp
// Attach this to a GameObject in the scene
using RogueDeal.HexLevels;
using UnityEngine;

public class HexCoordinateTest : MonoBehaviour
{
    private HexGrid hexGrid;
    
    void Start()
    {
        hexGrid = FindFirstObjectByType<HexGrid>();
        
        // Test: Convert world position to hex
        Vector3 worldPos = new Vector3(5f, 0f, 3f);
        HexCoordinate hex = hexGrid.WorldToHex(worldPos);
        Debug.Log($"World {worldPos} -> Hex {hex}");
        
        // Test: Convert hex back to world
        Vector3 backToWorld = hexGrid.HexToWorld(hex);
        Debug.Log($"Hex {hex} -> World {backToWorld}");
        
        // Test: Neighbors
        HexCoordinate[] neighbors = hex.GetNeighbors();
        Debug.Log($"Neighbors of {hex}: {neighbors.Length} neighbors");
        
        // Test: Distance
        HexCoordinate other = new HexCoordinate(3, 2);
        int distance = hex.DistanceTo(other);
        Debug.Log($"Distance from {hex} to {other}: {distance}");
    }
}
```

## Expected Results

- ✅ Hex grid should be visible in Scene view
- ✅ Coordinate conversion should work correctly
- ✅ Neighbor calculation should return 6 neighbors
- ✅ Distance calculation should work
- ✅ Grid bounds checking should work

## Next Steps

Once Phase 1 is verified:
1. Measure actual hex tile size from KayKit prefabs
2. Update HexSize to match actual prefab size
3. Move to Phase 2: Manual Editing Tools

## Troubleshooting

**Grid not visible?**
- Check that `Show Grid` is enabled
- Check that you're in Play mode (or modify OnDrawGizmos to work in edit mode)
- Check Scene view Gizmos are enabled

**Coordinates wrong?**
- Verify HexSize matches your prefab size
- Check that hex coordinate math is correct (test with known values)

**Performance issues?**
- Reduce Grid Radius in visualizer
- Only visualize occupied hexes, not entire grid
