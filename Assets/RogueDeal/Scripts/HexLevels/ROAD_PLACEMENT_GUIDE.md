# Road Smart Placement System - Testing & Debugging Guide

## Quick Start: Testing Roads

### 1. Open the Debug Window
- Go to `Tools > Hex Levels > Road Connection Debugger`
- This window shows real-time connection info for all roads

### 2. Place Some Roads
1. Select your HexGrid in the scene
2. In the Inspector, find the `HexLevelEditorTool` component
3. Make sure **Enable Smart Placement** is checked
4. Select a road prefab (any `hex_road_` prefab)
5. Click in the scene to place roads
6. Watch the debug window update in real-time

### 3. What to Look For
✅ **Good**: "Expected" and "Current" prefabs match (green text)
❌ **Bad**: Prefabs don't match (red text) - the wrong variant is being selected

---

## How Road Connections Work

### The System
Roads use a **bitmask pattern** to determine which variant to use:

```
Direction indices (hex neighbors):
   [2]NW   [1]NE
      \    /
[3]W--[HEX]--[0]E
      /    \
   [4]SW   [5]SE
```

Each bit in the bitmask represents a connection in that direction:
- Bit 0 (value 1) = Connection to East
- Bit 1 (value 2) = Connection to NE
- Bit 2 (value 4) = Connection to NW
- Bit 3 (value 8) = Connection to West
- Bit 4 (value 16) = Connection to SW
- Bit 5 (value 32) = Connection to SE

### Examples

**Straight Road (East-West)**:
- Connections: [E, W]
- Bitmask: 0b001001 = 9
- Expected variant: A

**Curve (East-NE)**:
- Connections: [E, NE]
- Bitmask: 0b000011 = 3
- Expected variant: B

**T-Junction (E-NE-NW)**:
- Connections: [E, NE, NW]
- Bitmask: 0b000111 = 7
- Expected variant: E

---

## Available Road Prefabs

From the KayKit Medieval Hexagon Pack:

- **hex_road_A**: Straight road (2 opposite connections)
- **hex_road_B**: Curve (2 adjacent connections)
- **hex_road_C-D**: More curve variants
- **hex_road_E**: T-junction (3 connections)
- **hex_road_F-G**: More T-junction variants
- **hex_road_H**: 4-way intersection
- **hex_road_I**: 6-way intersection (all connections)
- **hex_road_J-M**: Various other patterns

---

## Common Issues & Fixes

### Issue 1: Roads not connecting at all
**Symptoms**: All roads use the same prefab regardless of neighbors
**Fix**: 
1. Check that `Enable Smart Placement` is ON
2. Verify `ConnectionPatternMappings` asset is assigned
3. Check console for errors

### Issue 2: Wrong variant selected
**Symptoms**: Connections look wrong, prefab doesn't match pattern
**Fix**:
1. Open the debug window to see expected vs actual
2. Check if the mapping exists for that pattern
3. The mappings might need to be regenerated

### Issue 3: Roads connect but rotations are wrong
**Symptoms**: Roads connect but pieces are rotated incorrectly
**Fix**:
- Check the rotation index in the debug window
- Rotation should be 0-5 (60° increments)
- If wrong, the bitmask rotation calculation may be off

---

## Testing Checklist

Use this to systematically test your road system:

### Test 1: Single Road
- [ ] Place one road tile
- [ ] Should use variant A (straight, no connections)
- [ ] Check debug window

### Test 2: Straight Line
- [ ] Place two roads next to each other
- [ ] Both should update to variant A (straight)
- [ ] Should be aligned end-to-end

### Test 3: Corner
- [ ] Place two roads at 60° angle
- [ ] Both should update to variant B (curve)
- [ ] Should form a smooth corner

### Test 4: T-Junction
- [ ] Create a T-shaped road (3 roads meeting at one point)
- [ ] Center tile should be variant E (T-junction)
- [ ] Other tiles should connect properly

### Test 5: Cross
- [ ] Create a 4-way intersection
- [ ] Center should be variant H or I
- [ ] All 4 branches should connect

---

## Debug Commands

### In Code
```csharp
// Log info about a specific road
RoadConnectionDebugger.LogRoadConnection(hex, grid, rotation);

// Visualize a pattern
RoadConnectionDebugger.LogExpectedVariant(bitmask, "A");
```

### In Inspector
Add `RoadConnectionTester` component to any GameObject:
- Use context menu "Test All Road Patterns" to see all mappings
- Use "Diagnose Current Grid" to analyze placed roads
- Use "Test Common Patterns" to see expected patterns

---

## Next Steps

1. **Test the basics**: Follow the testing checklist above
2. **Check the debug window**: See if expected == current
3. **Report findings**: Let me know which test fails and I'll fix it

Once roads work perfectly, we can apply the same system to water/rivers!
