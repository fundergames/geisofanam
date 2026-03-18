# Testing the Presentation Layer

The presentation layer components (CombatExecutor, WeaponHitbox, Projectile, PersistentAOE) require Unity GameObjects and scene setup to test properly.

## Quick Test Setup

### Option 1: Automated Test (Recommended)

1. **Add Test Component**:
   - Create an empty GameObject in your scene
   - Add the `PresentationLayerTester` component
   - Check "Run Tests On Start"

2. **Run the Scene**:
   - Press Play
   - The tester will automatically:
     - Create test attacker and target GameObjects
     - Set up required components
     - Run all tests
   - Check the "Test Results" field in the Inspector
   - Check the Console for detailed output

### Option 2: Manual Scene Setup

1. **Create Attacker**:
   - Create a GameObject named "Attacker"
   - Add components:
     - `CombatEntity`
     - `CombatExecutor`
     - `CombatEventReceiver`
     - `Animator` (required)
   - Position at (0, 0, 0)

2. **Create Target**:
   - Create a GameObject named "Target"
   - Add components:
     - `CombatEntity`
     - `CapsuleCollider` (set as trigger)
   - Tag as "Enemy"
   - Position at (0, 0, 2)

3. **Create Weapon Hitbox** (Optional):
   - Create child GameObject under Attacker named "Weapon"
   - Add components:
     - `WeaponHitbox`
     - `BoxCollider` (set as trigger)
   - Position in front of attacker

4. **Assign in Tester**:
   - Add `PresentationLayerTester` to scene
   - Assign Attacker and Target GameObjects
   - Run tests

## What Gets Tested

✅ **CombatExecutor**:
- Action execution
- Target resolution
- Cooldown management
- Action context storage

✅ **CombatEventReceiver**:
- Component existence
- (Animation events tested manually)

✅ **WeaponHitbox**:
- Enable/disable functionality
- Collider activation

✅ **Projectile**:
- Creation and initialization
- Effect assignment

✅ **PersistentAOE**:
- Creation and initialization
- Effect assignment

## Manual Testing: Animation Events

To test animation events manually:

1. **Set up Animation**:
   - Create or use an animation clip
   - Add Animation Events at specific frames
   - Event function: `OnCombatEvent`
   - Event string: `"EnableHitbox"`, `"DisableHitbox"`, etc.

2. **Test Events**:
   - Play animation
   - Verify events trigger correctly
   - Check Console for any errors

## Manual Testing: Full Combat Flow

1. **Create Test Action**:
   - Use `Tools > Combat System > Create Test Assets`
   - Or create manually in Project window

2. **Execute Action**:
   - Get `CombatExecutor` component
   - Call `ExecuteAction(testAction)`
   - Verify:
     - Animation plays
     - Targets are resolved
     - Effects are applied
     - Cooldown starts

3. **Test Hit Detection**:
   - Enable weapon hitbox via animation event
   - Move weapon collider through target
   - Verify effects are applied
   - Verify no double-hits

4. **Test Projectile**:
   - Create action with `isProjectile = true`
   - Execute action
   - Verify projectile spawns and moves
   - Verify effects apply on arrival

5. **Test Persistent AOE**:
   - Create action with `spawnsPersistentAOE = true`
   - Execute action
   - Verify AOE spawns
   - Verify pulses occur
   - Move target in/out of zone
   - Verify effects only apply when in zone

## Troubleshooting

**"CombatExecutor not found"**:
- Make sure `CombatExecutor` component is added to attacker GameObject
- Check that `CombatEntity` component exists (required)

**"Targets not resolved"**:
- Check that target has `CombatEntity` component
- Verify target is within range
- Check layer masks in targeting strategy

**"Animation not playing"**:
- Ensure Animator component exists
- Verify animation trigger name matches
- Check Animator Controller is assigned

**"Hitbox not detecting"**:
- Verify collider is set as trigger
- Check layer masks match
- Ensure hitbox is enabled via animation event
- Verify target has collider

## Example Animation Event Setup

In your animation clip, add events:

- Frame 10: `OnCombatEvent` with string `"EnableHitbox"`
- Frame 20: `OnCombatEvent` with string `"SpawnVFX:FireSlash"`
- Frame 25: `OnCombatEvent` with string `"ApplyEffects"`
- Frame 30: `OnCombatEvent` with string `"DisableHitbox"`
- Frame 40: `OnCombatEvent` with string `"ReturnToOrigin"`

## Next Steps

Once presentation layer tests pass:
1. Test with real animations
2. Test with multiple targets
3. Test combo attacks
4. Test projectile and AOE in actual gameplay
5. Move on to simulation layer

