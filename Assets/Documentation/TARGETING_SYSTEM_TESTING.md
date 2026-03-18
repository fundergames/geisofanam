# Targeting System Testing Guide

## Quick Setup (5 minutes)

### Step 1: Create Targeting Strategy Assets

1. In Unity Project window, navigate to a folder (e.g., `Assets/RogueDeal/Resources/Combat/Targeting/`)
2. Right-click → **Create** → **RogueDeal** → **Combat** → **Targeting**
3. Create these 4 assets:
   - `Targeting_NearestEnemy`
   - `Targeting_Cone`
   - `Targeting_ClickToSelect`
   - `Targeting_Directional`

4. Configure each one:
   - **Nearest Enemy**: 
     - `Target Layers`: Set to your Enemy layer (e.g., Layer 6)
     - `Default Range`: `2` (or match your weapon range)
   - **Cone**:
     - `Target Layers`: Same as above
     - `Default Range`: `2`
     - `Default Cone Angle`: `60` (degrees)
   - **Click To Select**:
     - `Target Layers`: Same as above
     - `Default Range`: `2`
   - **Directional**: No settings needed

### Step 2: Configure Your Player

1. Select your **Player GameObject** (the one with `ThirdPersonCombatController`)
2. Find the **TargetingManager** component (auto-created)
3. Assign the strategy assets you just created:
   - Drag `Targeting_NearestEnemy` to **Nearest Enemy Strategy**
   - Drag `Targeting_Cone` to **Cone Targeting Strategy**
   - Drag `Targeting_ClickToSelect` to **Click To Select Strategy**
   - Drag `Targeting_Directional` to **Directional Strategy**
   - (Ground Targeting uses existing `GroundTargetedAOE` if you have one)

4. Check **Enable Debug Keybinds** (for testing)

### Step 3: Set Weapon Range (Optional but Recommended)

1. Find your Weapon ScriptableObject asset
2. Set **Max Range** field:
   - Melee weapon: `2` units
   - Spear: `3` units
   - Bow: `20` units

### Step 4: Set Cone Angle (For Cone Targeting)

1. On your Player GameObject, find **CombatEntity** component
2. Set **Cone Angle** to `60` (degrees)

## Testing Each Mode

### Test 1: Nearest Enemy Targeting

**Setup:**
1. Press **1** key to switch to Nearest Enemy mode
2. Check Console - should see: `"[TargetingManager] Switched to Nearest Enemy targeting"`

**Test:**
1. Move near an enemy (within weapon range)
2. Press **Left Mouse Button** to attack
3. **Expected:**
   - Character rotates toward nearest enemy
   - Attack animation plays
   - Check Console for: `"[NearestEnemyTargetingStrategy] Found target: EnemyName at distance X.XX"`

**Verify:**
- ✅ Character rotates to face nearest enemy
- ✅ Console shows target found message
- ✅ If no enemy in range, console shows: `"No valid targets found within range"`

---

### Test 2: Cone Targeting

**Setup:**
1. Press **2** key to switch to Cone mode
2. Check Console - should see: `"[TargetingManager] Switched to Cone targeting"`

**Test:**
1. Position yourself so an enemy is in front of you (within cone)
2. Position another enemy to the side (outside cone)
3. Press **Left Mouse Button** to attack
4. **Expected:**
   - Character rotates toward enemy in front (within cone)
   - Ignores enemy to the side (outside cone)
   - Console shows: `"[ConeTargetingStrategy] Found target: EnemyName at distance X.XX, angle: XX.X°"`

**Verify:**
- ✅ Only targets enemies in front (within cone angle)
- ✅ Console shows angle measurement
- ✅ If no enemy in cone, console shows: `"No valid targets found within range and cone angle"`

---

### Test 3: Click-to-Select Targeting

**Setup:**
1. Press **3** key to switch to Click-to-Select mode
2. Check Console - should see: `"[TargetingManager] Switched to Click-to-Select targeting"`

**Test:**
1. **Click on an enemy** (not attack button, just click)
   - **Expected:** Console shows: `"[TargetingManager] Locked on to: EnemyName"`
   - **Expected:** Lock-on indicator appears under enemy (red circle with crosshair)

2. **Press Left Mouse Button** to attack
   - **Expected:** Character attacks the locked-on target

3. **Click the same enemy again**
   - **Expected:** Console shows: `"[TargetingManager] Cleared lock-on from: EnemyName"`
   - **Expected:** Lock-on indicator disappears

4. **Click on a different enemy**
   - **Expected:** Lock-on switches to new enemy

5. **Click on ground/non-targetable**
   - **Expected:** Lock-on clears

**Verify:**
- ✅ Lock-on indicator appears under target
- ✅ Indicator follows target as it moves
- ✅ Clicking same target deselects
- ✅ Clicking ground clears lock-on

---

### Test 4: Directional Targeting

**Setup:**
1. Press **4** key to switch to Directional mode
2. Check Console - should see: `"[TargetingManager] Switched to Directional targeting"`

**Test:**
1. **Face a direction** (use WASD to move, character faces movement direction)
2. Press **Left Mouse Button** to attack
3. **Expected:**
   - Character attacks in facing direction
   - No target selection needed
   - Weapon colliders detect hits automatically
   - Console shows: `"[ThirdPersonCombatController] Set current action for weapon collider: ActionName"`

**Verify:**
- ✅ Character attacks in facing direction
- ✅ No rotation toward enemies
- ✅ Weapon colliders handle hit detection
- ✅ Works even with no enemies nearby

---

### Test 5: Ground Targeting (AOE)

**Setup:**
1. Press **5** key to switch to Ground Targeting mode
2. Check Console - should see: `"[TargetingManager] Switched to Ground targeting"`

**Test:**
1. **Click on the ground** where you want AOE
   - **Expected:** AOE preview indicator appears (orange circle)
   - **Expected:** Preview shows affected area

2. Press **Left Mouse Button** to cast
   - **Expected:** Spell casts at ground position

**Verify:**
- ✅ AOE preview appears on ground click
- ✅ Preview shows radius of effect
- ✅ Preview stays at ground position (doesn't follow)

---

## Debug Console Messages

Watch the Console for these messages:

### Successful Targeting:
```
[TargetingManager] Switched to [Mode] targeting
[NearestEnemyTargetingStrategy] Found target: EnemyName at distance 1.50
[ThirdPersonCombatController] Set current action for weapon collider: ActionName
```

### No Target Found:
```
[NearestEnemyTargetingStrategy] No valid targets found within range 2.00
```

### Lock-On:
```
[TargetingManager] Locked on to: EnemyName
[TargetingManager] Cleared lock-on from: EnemyName
```

## Visual Indicators

### Lock-On Indicator
- **Location:** Under locked target (ground level)
- **Appearance:** Red circle with crosshair
- **Behavior:** Follows target if movable

### AOE Preview Indicator
- **Location:** On ground at click position
- **Appearance:** Orange circle showing radius
- **Behavior:** Stays at ground position

## Troubleshooting

### Problem: "No valid targets found"
**Solutions:**
- Check enemy is on correct Layer (set in targeting strategy)
- Check enemy has `CombatEntity` component
- Check enemy is within range (weapon range or default range)
- Check enemy is alive (`IsAlive` = true)

### Problem: Lock-on indicator not appearing
**Solutions:**
- Check `LockOnIndicator` component exists (auto-created by ThirdPersonCombatController)
- Check Console for errors about LineRenderer
- Verify target is valid (has CombatEntity, is alive)

### Problem: Targeting mode not switching
**Solutions:**
- Check `Enable Debug Keybinds` is checked on TargetingManager
- Check strategy assets are assigned in TargetingManager
- Check Console for errors

### Problem: Character not rotating toward target
**Solutions:**
- Check target was found (look for Console messages)
- Check `useWeaponColliders` setting (should be true for directional, false for others)
- Verify target is not null

## Range Indicator (Visual Debug)

### Quick Setup
The `RangeIndicator` component automatically shows your attack range as a green circle on the ground.

**To enable:**
1. The component is auto-created by `TargetingManager` (or add manually)
2. Set **Display Mode**:
   - **Always**: Always visible
   - **Debug Only**: Only in debug builds/editor
   - **On Hover**: (Future feature)
   - **Never**: Hidden

**Controls:**
- Press **R** key to toggle range indicator on/off (when debug keybinds enabled)

**What it shows:**
- Green circle on ground showing attack range
- Range comes from: Weapon → CombatProfile → Default (2 units)
- Updates automatically when weapon changes

**In Editor:**
- Range also shows as a gizmo when object is selected (green circle)

## Quick Test Checklist

- [ ] Created 4 targeting strategy assets
- [ ] Assigned strategies to TargetingManager
- [ ] Set weapon range (optional)
- [ ] Set cone angle (optional)
- [ ] Tested Nearest Enemy (key 1)
- [ ] Tested Cone Targeting (key 2)
- [ ] Tested Click-to-Select (key 3)
- [ ] Tested Directional (key 4)
- [ ] Tested Ground Targeting (key 5)
- [ ] Verified lock-on indicator appears
- [ ] Verified range indicator appears (press R)
- [ ] Verified Console messages appear

## Next: Add Damage/SFX/VFX

Once targeting is working, you can add:
1. **Damage**: Already handled by `WeaponHitbox` or `CombatExecutor`
2. **SFX**: Add to `CombatAction.effectBindings` (animation event → AudioClip)
3. **VFX**: Add to `CombatAction.effectBindings` (animation event → VFX prefab)

See `TARGETING_SYSTEM_SETUP.md` for details.
