# Setting Up Animation Events for Combat System

Your animator controller has these triggers:
- `Attack_1`, `Attack_2`, `Attack_3` (for attacks)
- `CastSpell` (for spells)
- `TakeDamage` (for hit reactions)
- `Die` (for death)

## Quick Setup Guide

### Step 1: Assign Animator Controller

1. Select your **Attacker** GameObject in the test scene
2. In the Inspector, find the **Animator** component
3. Assign your **BaseBattleController** (or similar) to the **Controller** field

### Step 2: Add Animation Events to Attack Clips

For each attack animation clip (`attack1Clip`, `attack2Clip`, `attack3Clip`):

1. **Open the Animation Clip**:
   - Select the clip in Project window
   - Open the **Animation** window (Window > Animation > Animation)

2. **Add Animation Events**:
   - Scrub to the frame where the hit should connect (e.g., frame 15-20)
   - Click the **Add Event** button (or right-click timeline)
   - In the event inspector:
     - **Function**: `OnCombatEvent`
     - **String Parameter**: `"EnableHitbox"`

3. **Add More Events**:
   - At hit frame: `"ApplyEffects"` (applies damage/effects)
   - After hit frame: `"DisableHitbox"`

### Example: Attack_1 Animation Events

```
Frame 10: OnCombatEvent("EnableHitbox")
Frame 15: OnCombatEvent("ApplyEffects")  // Damage frame
Frame 20: OnCombatEvent("DisableHitbox")
Frame 25: (animation ends, returns to Idle)
```

### Step 3: For Combo Attacks

If using combo attacks, add events to each clip:

**Attack_1 clip:**
- Frame 15: `"EnableHitbox"`
- Frame 20: `"ApplyEffects"`
- Frame 25: `"DisableHitbox"`

**Attack_2 clip:**
- Frame 15: `"EnableHitbox"`
- Frame 20: `"ApplyEffects"` + `"ComboHit"` (triggers next hit)
- Frame 25: `"DisableHitbox"`

**Attack_3 clip:**
- Frame 15: `"EnableHitbox"`
- Frame 20: `"ApplyEffects"` + `"ComboHit"` (final hit)
- Frame 25: `"DisableHitbox"`

## Available Animation Events

The `CombatEventReceiver` component handles these events:

- `"EnableHitbox"` - Activates weapon hitbox for collision detection
- `"DisableHitbox"` - Deactivates weapon hitbox
- `"ApplyEffects"` - Applies all effects from the current action
- `"ComboHit"` - Registers a combo hit (for multi-hit combos)
- `"SpawnVFX:EffectName"` - Spawns VFX (e.g., `"SpawnVFX:FireSlash"`)
- `"PlaySFX:SoundName"` - Plays sound (e.g., `"PlaySFX:SwordFire"`)
- `"FireProjectile"` - Spawns projectile (for ranged attacks)
- `"SpawnPersistentAOE"` - Spawns AOE zone
- `"MoveTo"` - Moves character toward target
- `"ReturnToOrigin"` - Returns character to original position

## Testing Without Animation Events

If you haven't set up animation events yet, the system will:
- **Automatically apply effects** when there's no animator controller
- **Apply effects immediately** if the trigger doesn't exist

You can test the combat system fully without animations - just check the on-screen display to see HP decreasing.

## Manual Testing (AnimationTestHelper)

The `AnimationTestHelper` component (added to attacker) lets you manually trigger events:

- **[E]** - Enable Hitbox
- **[D]** - Disable Hitbox  
- **[A]** - Apply Effects
- **[C]** - Combo Hit

Use these to test the combat flow while setting up animation events.

## Example: Fire Slash with VFX

For a Fire Slash attack using `Attack_2`:

```
Frame 5:  OnCombatEvent("SpawnVFX:FireSlash")
Frame 10: OnCombatEvent("PlaySFX:SwordFire")
Frame 15: OnCombatEvent("EnableHitbox")
Frame 20: OnCombatEvent("ApplyEffects")
Frame 25: OnCombatEvent("DisableHitbox")
```

## Integration with ClassAnimatorData

Your `ClassAnimatorData` assets have:
- `attack1Clip`, `attack2Clip`, `attack3Clip` - Use these for attacks
- `battleAnimator` - The animator controller to assign

The test helper automatically loads `NoWeapon_AnimatorData` and assigns the `battleAnimator` to the attacker.

## Next Steps

1. ✅ Animator controller is assigned (done by test helper)
2. ⏳ Add animation events to attack clips
3. ⏳ Test with actual animations
4. ⏳ Create your own CombatActions with proper triggers

