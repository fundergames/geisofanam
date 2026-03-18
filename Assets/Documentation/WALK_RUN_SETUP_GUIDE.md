# Walk/Run Animator Setup Guide

## How Walk/Run Should Work

The `ThirdPersonCombatController` script sets:
- **`Speed`** (Float): 0 = idle, 0.5 = walking, 1.0 = running
- **`Run`** (Bool): true when holding Shift, false otherwise

## Recommended Animator Setup

### Option 1: Speed-Based (Recommended)

Use the `Speed` parameter to blend between Idle and Run Forward:

**Idle State:**
- Transitions:
  - **Idle → Run Forward**
    - Condition: `Speed > 0.1` (Float, Greater)
    - Has Exit Time: **No**
    - Transition Duration: 0.1s

**Run Forward State:**
- Transitions:
  - **Run Forward → Idle**
    - Condition: `Speed < 0.1` (Float, Less)
    - Has Exit Time: **No**
    - Transition Duration: 0.1s

**Benefits:**
- Simple and responsive
- Works with any Speed value
- Smooth transitions

### Option 2: Run Bool-Based

Use the `Run` bool parameter:

**Idle State:**
- Transitions:
  - **Idle → Run Forward**
    - Condition: `Run = true` (Bool)
    - Has Exit Time: **No**
    - Transition Duration: 0.1s

**Run Forward State:**
- Transitions:
  - **Run Forward → Idle**
    - Condition: `Run = false` (Bool)
    - Has Exit Time: **No**
    - Transition Duration: 0.1s

**Benefits:**
- Simple on/off behavior
- Works well if you only have walk/run (no walking state)

### Option 3: Combined (Speed + Run Bool)

Use both parameters for more control:

**Idle State:**
- Transitions:
  - **Idle → Run Forward**
    - Conditions: `Speed > 0.1` AND `Run = true`
    - Has Exit Time: **No**
    - Transition Duration: 0.1s

**Run Forward State:**
- Transitions:
  - **Run Forward → Idle**
    - Conditions: `Speed < 0.1` OR `Run = false`
    - Has Exit Time: **No**
    - Transition Duration: 0.1s

## Current Issue with Your Controller

Your current setup has:
- Idle → Run Forward uses `Run` **trigger** (wrong!)
- Should use `Run` **bool = true** OR `Speed > 0.1`

## Fix Instructions

1. Open Animator window
2. Select the transition from **Idle → Run Forward**
3. In the Inspector, find the condition
4. Change:
   - Parameter: `Run`
   - Type: Change from **Trigger** to **Bool**
   - Condition: **true**

OR

1. Delete the current transition
2. Create new transition: Idle → Run Forward
3. Add condition:
   - Parameter: `Speed`
   - Type: **Float**
   - Condition: **Greater**
   - Value: **0.1**
4. Set:
   - Has Exit Time: **No**
   - Transition Duration: **0.1**

## Dash Root Motion Issue

If dash is animating but not moving, check:

1. **Animator Component:**
   - ✅ `Apply Root Motion` must be **enabled**

2. **Animation Clip Settings:**
   - Select the DashFWD_Battle_RM_SingleSword animation clip
   - In Inspector, check **Root Transform Position (XZ)**
   - Should be set to: **Root Transform Position (XZ)**
   - NOT: **Bake Into Pose**

3. **OnAnimatorMove:**
   - The script has `OnAnimatorMove()` which applies root motion
   - Make sure it's not being blocked

4. **Dash State Settings:**
   - Select DashFWD_Battle_RM_SingleSword state in Animator
   - Make sure it's not set to "Write Defaults" in a way that breaks root motion
   - Motion Time should be normal (1.0 speed)

## Testing

After fixing:
1. Press Play
2. Move with WASD - should see Run Forward animation
3. Hold Shift + Move - should still run (Speed increases)
4. Press Space - dash should move the character forward

