# Weapon Collider-Based Combat Setup

The combat system now supports **weapon collider-based hit detection** instead of range-based targeting. This provides more realistic, physics-based combat where you actually hit enemies with your weapon.

## How It Works

1. **Player attacks** → Animation plays
2. **Animation events** enable/disable weapon collider during attack swing
3. **WeaponHitbox component** detects collisions with enemies
4. **Damage is applied** automatically when weapon touches enemy

## Setup Steps

### 1. Enable Weapon Collider Mode

On your **Player GameObject** with `ThirdPersonCombatController`:
- ✅ Ensure **"Use Weapon Colliders"** is checked (this is the default)
- This mode uses weapon colliders instead of targeting system

### 2. Add WeaponHitbox to Your Weapon

Your weapon (or hand bone where weapon attaches) needs a `WeaponHitbox` component:

1. **Find or create weapon GameObject:**
   - Usually on `hand_r/weapon_r` or similar bone
   - Or create a child GameObject for the weapon

2. **Add Collider:**
   - Add a **BoxCollider** or **CapsuleCollider**
   - Size it to match your weapon's hit area
   - Set **Is Trigger = true** (required!)
   - **Disable it** initially (it will be enabled by animation events)

3. **Add WeaponHitbox Component:**
   - Add `WeaponHitbox` component
   - Set **Target Layers**: Which layers can be hit (e.g., "Enemy" layer)
   - Set **Valid Target Tags**: Usually `["Enemy"]`

### 3. Set Up Animation Events

Your attack animations need to enable/disable the weapon collider at the right times:

**In your attack animations, add Animation Events:**

1. **At attack start (when weapon begins swinging):**
   - Function: `WeaponHitbox.Enable`
   - This activates the collider

2. **At attack end (when swing completes):**
   - Function: `WeaponHitbox.Disable`
   - This deactivates the collider

**Example Timeline:**
```
[0.0s] Attack animation starts
[0.1s] Animation Event: WeaponHitbox.Enable()  ← Weapon can hit now
[0.4s] Animation Event: WeaponHitbox.Disable() ← Weapon stops hitting
[0.6s] Attack animation ends
```

### 4. Verify Setup

- **Weapon has Collider** (trigger, disabled initially)
- **Weapon has WeaponHitbox component**
- **Attack animations have Enable/Disable events**
- **Use Weapon Colliders** is enabled on player controller

## How Damage Works

When weapon collider hits an enemy:

1. `WeaponHitbox.OnTriggerEnter()` detects collision
2. Checks if target has `CombatEntity` component
3. Gets current action from `CombatExecutor.GetCurrentAction()`
4. Applies all effects from that action (damage, etc.)
5. Prevents double-hits on same target per swing

## Debugging

### If attacks don't deal damage:

1. **Check Console** for messages:
   - `"[WeaponHitbox] Hit detected..."` - Collision detected
   - `"[WeaponHitbox] Applied X damage..."` - Damage applied
   - `"[WeaponHitbox] Hit detected but no current action available"` - Action not set

2. **Verify Weapon Collider:**
   - Is collider enabled during attack? (Check animation events)
   - Is collider a trigger?
   - Is collider size correct? (not too small/large)

3. **Verify Current Action:**
   - Check `"[ThirdPersonCombatController] Set current action..."` in Console
   - Ensure action has effects configured

4. **Verify Target:**
   - Does enemy have `CombatEntity` component?
   - Is enemy on correct layer/tag?
   - Is enemy alive?

## Comparison: Weapon Colliders vs Targeting

### Weapon Colliders (New Default):
- ✅ More realistic - actually hit enemies with weapon
- ✅ Better for action combat
- ✅ Works with any weapon size/shape
- ⚠️ Requires animation events
- ⚠️ Requires weapon collider setup

### Targeting System (Legacy):
- ✅ No animation events needed
- ✅ Always hits if in range
- ❌ Less realistic
- ❌ Requires targeting strategy configuration

## Switching Between Modes

In `ThirdPersonCombatController`:
- **Use Weapon Colliders = true** → Weapon collider mode (default)
- **Use Weapon Colliders = false** → Legacy targeting mode

## Example Setup

```
Player (ThirdPersonCombatController)
├── PlayerVisual (Animator)
└── Hand_R
    └── Weapon_R
        ├── BoxCollider (Trigger, Disabled)
        └── WeaponHitbox
            ├── Target Layers: Enemy
            └── Valid Target Tags: ["Enemy"]
```

**Attack Animation Events:**
- Frame 5: `WeaponHitbox.Enable()`
- Frame 20: `WeaponHitbox.Disable()`

This ensures the weapon can only hit during the actual swing!

