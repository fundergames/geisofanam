# Real Scenario Testing Guide

This guide shows you how to test the combat system in a real gameplay scenario.

## Quick Start

### Option 1: Create Test Scene (Recommended)

1. **Create Scene**:
   - Go to `Tools > Combat System > Create Real Scenario Test Scene`
   - This creates a complete test scene with:
     - Attacker (blue capsule)
     - Target (red capsule)
     - Weapon hitbox
     - RealScenarioTester component

2. **Run the Scene**:
   - Press Play
   - The tester automatically creates 3 test actions
   - Use controls to test combat

### Option 2: Manual Setup

1. **Set up Scene** (or use existing test scene):
   - Attacker GameObject with: CombatEntity, CombatExecutor, CombatEventReceiver
   - Target GameObject with: CombatEntity, Collider (trigger), tagged "Enemy"
   - Weapon Hitbox (optional, for collision-based hits)

2. **Add Tester**:
   - Create empty GameObject
   - Add `RealScenarioTester` component
   - Assign Attacker and Target GameObjects

3. **Run**:
   - Press Play
   - Test actions will be created automatically

## Controls

- **[Space]** - Execute Next Action
- **[T]** - Advance Turn (process status effects, cooldowns)

## Test Actions Created

The tester automatically creates 3 test actions:

### 1. Basic Attack
- **Type**: Simple melee attack
- **Damage**: 25 base + Attack stat
- **Cooldown**: None
- **Effects**: Physical damage only

### 2. Fire Slash
- **Type**: Multi-effect attack
- **Damage**: 30 physical + 15 fire
- **Status**: Applies Burn (5 damage/turn for 3 turns)
- **Cooldown**: 3 turns
- **Effects**: Physical damage + Fire damage + Burn status

### 3. Whirlwind Slash
- **Type**: 3-hit combo
- **Damage**: 20 per hit (first 2), 40 (last hit)
- **Cooldown**: 2 turns
- **Effects**: Physical damage (combo)

## Testing Scenarios

### Scenario 1: Basic Combat Flow

1. Press **[Space]** to execute Basic Attack
2. Watch Console for execution logs
3. Check target's HP in the on-screen display
4. Press **[T]** to advance turn
5. Repeat

**Expected**: Target takes damage, HP decreases

### Scenario 2: Status Effects

1. Press **[Space]** to execute Fire Slash
2. Check target's status effects (should show Burn)
3. Press **[T]** to advance turn
4. Check target's HP (should decrease from burn damage)
5. Advance 2 more turns
6. Check that burn expires

**Expected**: 
- Burn applied on hit
- 5 damage per turn for 3 turns
- Burn expires after 3 turns

### Scenario 3: Cooldowns

1. Press **[Space]** to execute Fire Slash
2. Immediately press **[Space]** again
3. Should fail (on cooldown)
4. Press **[T]** 3 times (advance 3 turns)
5. Press **[Space]** again
6. Should succeed (cooldown expired)

**Expected**: 
- Action goes on cooldown after use
- Cannot use again until cooldown expires
- Cooldown decreases each turn

### Scenario 4: Combo Attacks

1. Press **[Space]** to execute Whirlwind Slash
2. Check Console for combo hit messages
3. (In real scenario, animation events would trigger `OnComboHit()`)
4. Each hit should apply damage

**Expected**: 
- Combo starts
- Multiple hits apply damage
- Last hit does bonus damage

### Scenario 5: Multiple Actions

1. Execute Basic Attack
2. Execute Fire Slash
3. Execute Whirlwind Slash
4. Advance turns
5. Execute actions again (check cooldowns)

**Expected**: 
- Basic Attack always available
- Fire Slash on 3-turn cooldown
- Whirlwind Slash on 2-turn cooldown

## On-Screen Display

The tester shows a combat log in the top-left corner with:
- Current turn number
- Attacker HP and stats
- Target HP and stats
- Active status effects
- Controls reminder

## Testing Animation Events

To test with actual animations:

1. **Set up Animator**:
   - Assign Animator Controller to Attacker
   - Create animation clips for attacks

2. **Add Animation Events**:
   - In animation clip, add events:
     - `OnCombatEvent` with string `"EnableHitbox"` (at hit frame)
     - `OnCombatEvent` with string `"DisableHitbox"` (after hit)
     - `OnCombatEvent` with string `"ApplyEffects"` (at damage frame)
     - `OnCombatEvent` with string `"ComboHit"` (for combo attacks)

3. **Test**:
   - Execute actions
   - Watch animations play
   - Verify events trigger correctly
   - Check hit detection works

## Testing Hit Detection

To test weapon hitbox collision:

1. **Enable Hitbox**:
   - Via animation event: `"EnableHitbox"`
   - Or manually: `weaponHitbox.Enable()`

2. **Move Weapon Through Target**:
   - Animate weapon swing
   - Or manually move weapon GameObject

3. **Verify**:
   - Effects are applied
   - No double-hits (same target hit only once per swing)
   - Console shows hit messages

## Testing Projectiles

To test projectile system:

1. **Create Projectile Action**:
   - Set `isProjectile = true`
   - Assign `projectilePrefab`
   - Set `projectileSpeed`

2. **Execute Action**:
   - Projectile spawns
   - Moves toward target
   - Applies effects on arrival

3. **Verify**:
   - Projectile moves correctly
   - Effects apply on arrival
   - Projectile despawns

## Testing Persistent AOE

To test AOE zones:

1. **Create AOE Action**:
   - Set `spawnsPersistentAOE = true`
   - Assign `persistentAOEPrefab`
   - Set `pulseCount` and `pulseDuration`

2. **Execute Action**:
   - AOE spawns at target position
   - Pulses at intervals

3. **Test Movement**:
   - Move target into zone → takes damage
   - Move target out of zone → stops taking damage
   - Move target back in → takes damage again

4. **Verify**:
   - Only entities in zone during pulse take damage
   - AOE despawns after all pulses

## Troubleshooting

**"Action failed to execute"**:
- Check cooldown (action might be on cooldown)
- Check targeting (target might be out of range)
- Check if target is alive

**"No targets resolved"**:
- Verify target has `CombatEntity` component
- Check target is within range
- Verify layer masks match

**"Effects not applying"**:
- Check target is alive
- Verify effects are assigned to action
- Check Console for errors

**"Status effects not processing"**:
- Press **[T]** to advance turn
- Status effects process on turn start
- Check target's `activeStatusEffects` list

## Next Steps

Once real scenario testing works:
1. Test with your actual animations
2. Create your own CombatActions
3. Test with multiple targets
4. Test in actual gameplay scenarios
5. Move on to simulation layer for balance testing

