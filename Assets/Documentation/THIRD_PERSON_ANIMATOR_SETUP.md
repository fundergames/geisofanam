# Third Person Combat Controller - Animator Setup Guide

This guide explains how to set up the Animator Controller for the `ThirdPersonCombatController`.

## Required Animator Parameters

Add these parameters to your Animator Controller:

### Float Parameters
- **`Speed`** (Float) - Current movement speed (0 = idle, >0 = moving)
  - Used to blend between idle and movement animations

### Bool Parameters
- **`IsGrounded`** (Bool) - Whether the character is on the ground
  - Used for jump/fall animations (if you add them later)
  
- **`IsRunning`** (Bool) - Whether the character is running (holding Shift)
  - Used to blend between walk and run animations
  
- **`IsAttacking`** (Bool) - Whether the character is currently attacking
  - Set to `true` when attack starts, `false` when attack ends
  - Prevents movement and other actions during attacks

### Trigger Parameters
- **`Dash`** (Trigger) - Triggers dash animation
  - Fired when Space is pressed
  - Should transition to Dash state
  
- **`Attack`** (Trigger) - Triggers attack animation
  - Fired when Left Mouse Button is pressed
  - Should transition to Attack state

## Required Animation States

### Base States

1. **Idle** (Default State)
   - Animation: Idle animation clip
   - Loop: Yes
   - Motion Time: 0 (normalized time)
   - Root Motion: Enabled (if your idle has root motion)

2. **Walk**
   - Animation: Walk animation clip
   - Loop: Yes
   - Root Motion: Enabled (if your walk has root motion)

3. **Run**
   - Animation: Run animation clip
   - Loop: Yes
   - Root Motion: Enabled (if your run has root motion)

4. **Dash**
   - Animation: Dash animation clip (e.g., `DashFWD_Battle_RM_SingleSword`)
   - Loop: No
   - Root Motion: **MUST BE ENABLED** (dash uses root motion for movement)
   - Speed: 1.0 (adjust if needed)

5. **Attack** (or multiple attack states for combos)
   - Animation: Attack animation clip (e.g., `Attack02_SingleSword`)
   - Loop: No
   - Root Motion: **MUST BE ENABLED** (attacks use root motion for movement)
   - Speed: 1.0 (adjust if needed)

### Optional States (for combos)

If you want combo attacks, create multiple attack states:
- **Attack1** - First attack in combo
- **Attack2** - Second attack in combo
- **Attack3** - Third attack in combo
- etc.

## State Transitions

### From Idle

**Idle → Walk**
- Condition: `Speed > 0.1` AND `IsRunning = false`
- Has Exit Time: No
- Transition Duration: 0.1s
- Interruption Source: None

**Idle → Run**
- Condition: `Speed > 0.1` AND `IsRunning = true`
- Has Exit Time: No
- Transition Duration: 0.1s
- Interruption Source: None

**Idle → Dash**
- Condition: `Dash` (trigger)
- Has Exit Time: No
- Transition Duration: 0.05s
- Interruption Source: None

**Idle → Attack**
- Condition: `Attack` (trigger)
- Has Exit Time: No
- Transition Duration: 0.05s
- Interruption Source: None

### From Walk

**Walk → Idle**
- Condition: `Speed < 0.1`
- Has Exit Time: No
- Transition Duration: 0.1s
- Interruption Source: None

**Walk → Run**
- Condition: `IsRunning = true`
- Has Exit Time: No
- Transition Duration: 0.1s
- Interruption Source: None

**Walk → Dash**
- Condition: `Dash` (trigger)
- Has Exit Time: No
- Transition Duration: 0.05s
- Interruption Source: None

**Walk → Attack**
- Condition: `Attack` (trigger)
- Has Exit Time: No
- Transition Duration: 0.05s
- Interruption Source: None

### From Run

**Run → Walk**
- Condition: `IsRunning = false` AND `Speed > 0.1`
- Has Exit Time: No
- Transition Duration: 0.1s
- Interruption Source: None

**Run → Idle**
- Condition: `Speed < 0.1`
- Has Exit Time: No
- Transition Duration: 0.1s
- Interruption Source: None

**Run → Dash**
- Condition: `Dash` (trigger)
- Has Exit Time: No
- Transition Duration: 0.05s
- Interruption Source: None

**Run → Attack**
- Condition: `Attack` (trigger)
- Has Exit Time: No
- Transition Duration: 0.05s
- Interruption Source: None

### From Dash

**Dash → Idle**
- Condition: (no conditions - exit when animation finishes)
- Has Exit Time: **Yes**
- Exit Time: 0.95 (exit near end of animation)
- Transition Duration: 0.1s
- Interruption Source: None

**Dash → Walk**
- Condition: `Speed > 0.1` AND `IsRunning = false`
- Has Exit Time: **Yes**
- Exit Time: 0.95
- Transition Duration: 0.1s
- Interruption Source: None

**Dash → Run**
- Condition: `Speed > 0.1` AND `IsRunning = true`
- Has Exit Time: **Yes**
- Exit Time: 0.95
- Transition Duration: 0.1s
- Interruption Source: None

### From Attack

**Attack → Idle**
- Condition: (no conditions - exit when animation finishes)
- Has Exit Time: **Yes**
- Exit Time: 0.95 (exit near end of animation)
- Transition Duration: 0.1s
- Interruption Source: None

**Attack → Walk**
- Condition: `Speed > 0.1` AND `IsRunning = false`
- Has Exit Time: **Yes**
- Exit Time: 0.95
- Transition Duration: 0.1s
- Interruption Source: None

**Attack → Run**
- Condition: `Speed > 0.1` AND `IsRunning = true`
- Has Exit Time: **Yes**
- Exit Time: 0.95
- Transition Duration: 0.1s
- Interruption Source: None

**Attack → Attack (for combos)**
- Condition: `Attack` (trigger)
- Has Exit Time: **Yes**
- Exit Time: 0.7 (allow combo input during attack)
- Transition Duration: 0.1s
- Interruption Source: None

## Animation Events

Add these animation events to your animation clips:

### Dash Animation
- **Event Name**: `OnDashStart`
  - Time: 0.0 (at start of animation)
  - Function: `ThirdPersonCombatController.OnDashStart()`
  
- **Event Name**: `OnDashEnd`
  - Time: 0.95 (near end of animation)
  - Function: `ThirdPersonCombatController.OnDashEnd()`

### Attack Animation(s)
- **Event Name**: `OnAttackStart` (Optional)
  - Time: 0.0 (at start of animation)
  - Function: `ThirdPersonCombatController.OnAttackStart()`
  - **Note**: This is optional - used for debug logging
  
- **Event Name**: `OnAttackEnd` (Optional)
  - Time: 0.95 (near end of animation)
  - Function: `ThirdPersonCombatController.OnAttackEnd()`
  - **Note**: This is OPTIONAL! The system will automatically detect when the attack ends by monitoring the `IsAction` parameter. Only add this if you want precise timing control.

### Animation Events with Animator Override Controller

**Important**: Animation events are stored in the animation clip itself, not the controller. If you use an `Animator Override Controller`:

- ✅ **Events will work** - They're part of the animation clip, so swapping clips via Override Controller preserves the events
- ⚠️ **You need events in each clip** - If you have 3 different attack animations, you'd need to add `OnAttackEnd` to all 3 clips
- ✅ **But you don't need them!** - The fallback system automatically resets the attack state when `IsAction` becomes `false`, so events are optional

**Recommendation**: 
- **Skip animation events** for `OnAttackEnd` - let the fallback system handle it
- **Add animation events** only if you need precise timing for other things (hit frames, VFX, sound effects)

## Root Motion Setup

### Critical Settings for Root Motion

1. **Animator Component**
   - ✅ **Apply Root Motion**: Must be **ENABLED**
   - This allows animations to move the character

2. **Animation Clips (Dash and Attacks)**
   - ✅ **Root Transform Position (XZ)**: Set to **"Root Transform Position (XZ)"** (NOT "Bake Into Pose")
   - ✅ **Root Transform Rotation (Y)**: Set to **"Root Transform Rotation (Y)"** (if animations have rotation) or "Bake Into Pose" (if not)
   - ✅ **Root Motion Node**: Set to your character's root bone (usually "Root" or "Hips")

3. **Animation States (Dash and Attacks)**
   - ✅ **Write Defaults**: Can be enabled or disabled (your preference)
   - ✅ **Motion**: Your animation clip
   - ✅ **Speed**: 1.0 (adjust if needed)

## Example Animator Controller Structure

```
Animator Controller
├── Parameters
│   ├── Speed (Float)
│   ├── IsGrounded (Bool)
│   ├── IsRunning (Bool)
│   ├── IsAttacking (Bool)
│   ├── Dash (Trigger)
│   └── Attack (Trigger)
│
└── Layers
    └── Base Layer
        ├── Idle (Default State)
        ├── Walk
        ├── Run
        ├── Dash
        └── Attack
```

## Testing Checklist

- [ ] All parameters are created
- [ ] All states are created with correct animations
- [ ] All transitions are set up with correct conditions
- [ ] Root motion is enabled on Animator component
- [ ] Animation clips have root motion settings correct
- [ ] Animation events are added to Dash and Attack animations
- [ ] Character moves with root motion during dash
- [ ] Character moves with root motion during attacks
- [ ] Transitions between states are smooth
- [ ] Combo attacks work (if implemented)

## Troubleshooting

### Character doesn't move during dash/attack
- Check that `Apply Root Motion` is enabled on Animator
- Check that animation clips have `Root Transform Position (XZ)` set correctly
- Check that `OnAnimatorMove()` is being called (add debug log)

### Character moves in wrong direction
- Check character's forward direction in scene
- Check animation's forward direction
- The controller rotates character before dash/attack, but root motion direction comes from animation

### Attacks don't trigger
- Check that `Attack` trigger parameter exists
- Check that transition conditions are correct
- Check that `IsAttacking` bool is being set correctly

### Dash doesn't work
- Check that `Dash` trigger parameter exists
- Check that transition conditions are correct
- Check that dash animation has root motion enabled

## Next Steps

1. Create or open your Animator Controller
2. Add all required parameters
3. Create all required states
4. Set up transitions
5. Assign animation clips
6. Add animation events
7. Test in Play mode

For more details on root motion setup, see `TIMELINE_COMBO_SETUP.md` (sections about root motion apply here too).

