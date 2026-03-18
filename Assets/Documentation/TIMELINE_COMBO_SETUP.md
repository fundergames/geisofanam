# Timeline Combo Setup Guide

This guide explains how to create combo attacks using Unity Timeline instead of Animator Controller transitions.

## Why Use Timeline for Combos?

- **Visual Sequencing**: See all animations, VFX, and SFX in one timeline
- **Precise Timing**: Control exactly when effects apply, hitboxes enable, etc.
- **No Animator Complexity**: No need to set up complex state machine transitions
- **Multi-Track Support**: Combine animations, audio, signals, and more

## Step 1: Create a Timeline Asset

1. In Project window, right-click → **Create → Timeline → Timeline**
2. Name it (e.g., `FireSlash_Combo_Timeline`)
3. This creates a `.playable` file

## Step 2: Set Up Timeline Tracks

Open the Timeline window (Window > Sequencing > Timeline) and select your Timeline asset.

### Animation Track (Attacker)

1. Click **+** → **Animation Track**
2. Rename track to `Attacker` or `Character`
3. Drag your combo animation clips onto the track in sequence:
   - `Combo01_InPlace_SingleSword` at 0:00
   - `Combo02_InPlace_SingleSword` at 1:00
   - `Combo03_InPlace_SingleSword` at 2:00
   - etc.

**🔧 CRITICAL: Seamless Root Motion Between Clips**

To make root motion clips continue seamlessly (each clip starts where the previous ended):

**Method 1: Match Offsets (May not work if offsets stay zero)**
1. **Right-click** on each animation clip (except the first one) in the Timeline
2. Select **"Match Offsets to Previous Clip"** from the context menu
   - **Note:** If offsets remain zero after this, the clips may not have root motion baked in, or Unity's feature isn't working
   - Try Method 2 or 3 instead

**Method 2: Manual Position Offset Calculation**
1. **Preview the Timeline** to see where each clip ends:
   - Play the Timeline in the editor
   - Scrub to the end of Clip 1, note the character's position
   - Scrub to the start of Clip 2, note the character's position
   - Calculate: `Offset = (Clip 1 end position) - (Clip 2 start position)`
2. **Select Clip 2** in Timeline
3. In Inspector → **"Clip Transform Offsets"** → **"Position"**
4. Enter the calculated offset values (X, Y, Z)
5. Repeat for Clip 3, using Clip 2's end position

**Method 3: Disable Timeline Position Control (CRITICAL - REQUIRED for root motion)**

**Step 1: Disable Timeline Track Position Control**
1. **Select the Animation Track** (not individual clips) in Timeline
2. In Inspector, find **"Apply Transform Offsets"**
3. **UNCHECK "Position"** (keep Rotation if needed)
   - This disables Timeline's position control entirely
   - Position will be handled entirely by root motion and our code
   - **This is REQUIRED because Timeline applies position in LOCAL SPACE, but root motion is in WORLD SPACE**

**Step 2: Check Animation Clip Import Settings (CRITICAL)**

**Root Transform Position (XZ) - REQUIRED for root motion:**
1. Select each animation clip in Project (e.g., `DashFWD_Battle_RM_SingleSword`)
2. In Inspector → **"Animation"** tab → **"Root Transform Position (XZ)"**
3. **MUST be set to "Root Transform Position (XZ)"** (NOT "Bake Into Pose")
   - If it's set to "Bake Into Pose", the position is baked into the animation and won't work with root motion
   - Change it to "Root Transform Position (XZ)" and click **Apply**
4. Repeat for ALL animation clips used in the Timeline

**Root Transform Rotation (Y) - For rotation:**
1. In the same Animation Inspector, check **"Root Transform Rotation (Y)"**
2. Set to **"Root Transform Rotation (Y)"** if your animations have root rotation
3. Set to **"Bake Into Pose"** if animations don't have root rotation (most common)

**Step 3: Verify Root Motion Node**
1. In the same Animation Inspector, check **"Root Motion Node"**
2. Should be set to the root bone of your character (usually "Root" or "Hips")
3. This determines which bone drives the root motion

**Step 4: Character Orientation vs Animation Forward (IMPORTANT)**

**The Problem:**
- Unity animations typically use **+Z as forward** (animation forward)
- Your character model might face **+X as forward** (character forward)
- This causes root motion to move in the wrong direction (sideways instead of forward)

**What You CAN Change on Animation Clips:**
Unfortunately, **you cannot change the forward direction of root motion in Unity's animation import settings**. The root motion direction is baked into the animation file itself. You would need to re-export the animation from your 3D software with a different forward axis.

**What You CAN Change:**
1. **Animation Import Settings → Root Transform Position (XZ)**: Must be set to "Root Transform Position (XZ)" (not "Bake Into Pose")
2. **Animation Import Settings → Root Motion Node**: Should be set to your character's root bone (usually "Root" or "Hips")
3. **Animation Import Settings → Root Transform Rotation (Y)**: Set to "Root Transform Rotation (Y)" if animations have rotation, or "Bake Into Pose" if not

**Solution Options (Handled in Code):**

**Option A: Automatic Character Rotation (Current Implementation)**
- `CombatExecutor` now automatically rotates the character before playing the Timeline
- It rotates the character so the animation's forward (Z) points at the target
- This works if your character forward is X and animation forward is Z
- **No changes needed to animation clips** - this is handled automatically

**Option B: Automatic Root Motion Rotation (Fallback)**
- The `TimelineRootMotionController` component has `rotateRootMotionToCharacterForward` enabled by default
- This rotates root motion deltas from animation forward (Z) to character forward (X)
- This is a fallback if Option A doesn't work perfectly
- You can disable this if you fix the character rotation instead

**If Neither Option Works:**
- Check your character's actual forward direction in the scene (what is `transform.forward`?)
- Check your animation's forward direction (what direction does root motion move in world space?)
- Adjust the rotation code in `CombatExecutor.StartTimelineCombo()` to match your specific setup

**If position STILL resets after all steps:**
- Check Console logs for `[TimelineRootMotionController]` messages
- Look for "Position reset" warnings - these show when Timeline tries to reset position
- Verify that `animator.applyRootMotion = true` (handled automatically by code)
- Check that the character's root GameObject is the one with the Animator (not a child)

**Method 3: Code-Based Position Tracking (Automatic Fallback)**
- The `CombatExecutor` now includes automatic position tracking that detects and prevents position resets
- If clips still reset, check the Console for warnings like: `"Timeline reset position! Restoring from..."`

**For clips WITHOUT root motion (like your middle attack):**
- Still use **"Match Offsets to Previous Clip"** so it starts where the previous clip ended
- The clip will stay in place (no root motion) but at the correct position
- The code will also prevent it from resetting to origin

**Troubleshooting:**
- If position still resets, check that your Animation Track is bound to the correct GameObject
- Ensure `animator.applyRootMotion = true` is set (handled automatically by `CombatExecutor`)
- Check Console logs for position tracking messages

**⚠️ IMPORTANT: Editor Scrubbing vs Runtime Playback**

When **scrubbing** through Timeline in the editor, you may see position resets between clips. This is a known Timeline limitation. However, during **runtime playback**, the `CombatExecutor` code will prevent position resets.

**To fix scrubbing behavior in editor (if needed for preview):**

**Option 1: Disable Timeline's Position Control (Recommended for root motion)**
1. **Select the Animation Track** (not individual clips) in Timeline
2. In the Inspector, find **"Apply Transform Offsets"**
3. **UNCHECK "Position"** (keep Rotation checked if needed)
   - This prevents Timeline from resetting position between clips
   - Root motion will be handled entirely by `CombatExecutor` code
   - This works for both scrubbing and runtime

**Option 2: Manual Position Offsets (If Option 1 doesn't work)**
1. For each clip (except the first), right-click → **"Match Offsets to Previous Clip"**
2. **Select each animation clip** and in Inspector → **"Clip Transform Offsets"**:
   - Verify the **Position** offset is correct
   - Manually calculate: Position offset = (Previous clip's end position) - (Current clip's start position)
   - You may need to preview the Timeline to see where the previous clip ends

**Note:** The runtime code (`CombatExecutor`) will handle position continuity during actual gameplay regardless of scrubbing behavior. If scrubbing still shows resets but runtime works, that's acceptable - the scrubbing issue is a Timeline editor limitation.

### Signal Track (for Effects)

1. Click **+** → **Signal Track**
2. Create Signal Emitter assets:
   - Right-click in Project → **Create → Timeline → Signal**
   - Name them: `ApplyEffects`, `ComboHit1`, `ComboHit2`, etc.
3. Place Signal Emitters on the timeline at hit moments
4. In Signal Emitter Inspector:
   - **Signal**: Select your signal asset
   - **Retroactive**: Unchecked
   - **Emit Once**: Checked

### Audio Track (Optional)

1. Click **+** → **Audio Track**
2. Drag SFX clips onto the track

### Activation Track (Optional - for VFX)

1. Click **+** → **Activation Track**
2. Bind to VFX GameObjects
3. Enable/disable them at specific times

## Step 3: Configure Signal Receivers

The `CombatExecutor` component automatically receives Timeline signals. You need to:

1. Select your Timeline asset
2. In the Signal Track, set up Signal Emitters that call:
   - `OnTimelineApplyEffects()` - Applies main effects
   - `OnTimelineComboHit(int)` - Applies per-hit effects (pass hit number 1, 2, 3, etc.)

**Note**: Timeline signals need to be connected to the `CombatExecutor` GameObject. The system automatically binds the Signal Track to the PlayableDirector.

## Step 4: Assign Timeline to CombatAction

1. Open your `Action_FireSlash` asset (or create a new CombatAction)
2. Set `isCombo = true`
3. Set `comboHitCount = 5` (or however many hits)
4. **Assign `timelineAsset`** to your Timeline asset
5. Add effects and targeting strategy as normal

## Step 5: Timeline Track Naming Conventions

The system automatically binds tracks based on names:

- **Attacker/Character/Player**: Binds to attacker's Animator
- **Target**: Binds to target's Animator (first target)
- **Signal Track**: Automatically connected to CombatExecutor

## Example Timeline Setup

```
Timeline: FireSlash_Combo (Duration: 5 seconds)

Track: Attacker (Animation Track)
  ├─ Combo01_InPlace_SingleSword [0:00 - 1:00]
  ├─ Combo02_InPlace_SingleSword [1:00 - 2:00]
  ├─ Combo03_InPlace_SingleSword [2:00 - 3:00]
  ├─ Combo04_InPlace_SingleSword [3:00 - 4:00]
  └─ Combo05_InPlace_SingleSword [4:00 - 5:00]

Track: Effects (Signal Track)
  ├─ Signal: ComboHit1 [0:30] → Calls OnTimelineComboHit(1)
  ├─ Signal: ComboHit2 [1:30] → Calls OnTimelineComboHit(2)
  ├─ Signal: ComboHit3 [2:30] → Calls OnTimelineComboHit(3)
  ├─ Signal: ComboHit4 [3:30] → Calls OnTimelineComboHit(4)
  └─ Signal: ComboHit5 [4:30] → Calls OnTimelineComboHit(5)

Track: Audio (Audio Track)
  ├─ SwordSwing1 [0:00]
  ├─ SwordSwing2 [1:00]
  └─ FireWhoosh [2:00]
```

## Timeline Signals Setup

To connect signals to CombatExecutor methods:

1. Create Signal assets (Create → Timeline → Signal)
2. In Timeline, add Signal Emitters at hit moments
3. The system automatically connects them to CombatExecutor

**Available Methods:**
- `OnTimelineApplyEffects()` - Applies main effects array
- `OnTimelineComboHit(int hitNumber)` - Applies per-hit effects (hitNumber: 1, 2, 3, etc.)

## Benefits Over Animator Controller

✅ **Visual**: See entire combo sequence at a glance  
✅ **Precise**: Frame-accurate timing for effects  
✅ **Flexible**: Easy to add/remove hits, adjust timing  
✅ **Multi-Media**: Combine animations, audio, VFX in one place  
✅ **No State Machine**: No complex transition setup needed  

## Troubleshooting

**Timeline doesn't play:**
- Check that `timelineAsset` is assigned in CombatAction
- Verify PlayableDirector component exists on the entity
- Check Timeline bindings in Inspector

**Effects don't apply:**
- Set up Signal Emitters in Timeline
- Ensure signals call `OnTimelineApplyEffects()` or `OnTimelineComboHit()`
- Check that effects array is populated in CombatAction

**Animations don't play:**
- Verify Animation Track is named `Attacker`, `Character`, or `Player`
- Check that animation clips are assigned
- Ensure Animator component exists on the entity

