# Testing the Combat System

## Quick Test

### Option 1: Runtime Tests (Recommended)

1. **Create Test Assets**:
   - Go to `Tools > Combat System > Create Test Assets`
   - This creates example ScriptableObjects in `Assets/RogueDeal/Resources/Combat/TestAssets/`

2. **Add Test Component**:
   - Create an empty GameObject in your scene
   - Add the `CombatSystemTester` component
   - Check "Run Tests On Start" (or leave it checked)

3. **Run the Scene**:
   - Press Play
   - Tests will run automatically
   - Check the "Test Results" field in the inspector for output
   - Check the Console for detailed logs

### Option 2: Manual Testing

1. **Use Context Menu**:
   - Add `CombatSystemTester` to a GameObject
   - Right-click the component
   - Select "Run All Tests"

### Option 3: Editor Window

1. **Open Test Window**:
   - Go to `Tools > Combat System > Open Test Window`
   - Click "Create Test Assets"
   - Use the window to manage tests

## What Gets Tested

✅ **CombatEntityData**:
- Stat management
- Damage/healing
- Cloning
- Status effect processing

✅ **DamageEffect**:
- Base damage calculation
- Stat scaling
- Defense reduction
- Critical hits

✅ **StatModifierEffect**:
- Add, Multiply, Set modifiers
- Instant vs duration-based

✅ **StatusEffect**:
- Status effect application
- Turn-based processing
- Duration tracking

✅ **MultiEffect**:
- Multiple effects applied together

✅ **Weapon**:
- Damage type multipliers
- Multiplier lookup

✅ **ActionCooldownManager**:
- Turn-based cooldowns
- Cooldown tracking
- Availability checking

✅ **CombatAction**:
- Action creation
- Effect assignment
- Targeting assignment
- Cooldown configuration

## Test Results

Tests will output:
- ✅ Passed tests
- ✗ Failed tests (with error messages)
- Detailed logs in Console

## Example Test Output

```
=== Combat System Tests ===

--- Testing CombatEntityData ---
✓ CombatEntityData tests passed

--- Testing DamageEffect ---
✓ DamageEffect tests passed

--- Testing StatModifierEffect ---
✓ StatModifierEffect tests passed

--- Testing StatusEffect ---
✓ StatusEffect tests passed

--- Testing MultiEffect ---
✓ MultiEffect tests passed

--- Testing Weapon ---
✓ Weapon tests passed

--- Testing ActionCooldownManager ---
✓ ActionCooldownManager tests passed

--- Testing CombatAction ---
✓ CombatAction tests passed

=== Tests Complete ===
```

## Creating Custom Tests

You can extend `CombatSystemTester` to add your own tests:

```csharp
private void TestMyCustomFeature()
{
    Log("\n--- Testing My Feature ---");
    
    // Your test code here
    Assert(condition, "Test message");
    
    Log("✓ My feature tests passed");
}
```

Then call it in `RunAllTests()`.

## Troubleshooting

**Tests fail with null reference**:
- Make sure test assets are created first
- Check that ScriptableObjects are properly initialized

**Tests don't run**:
- Check that "Run Tests On Start" is enabled
- Or use the context menu "Run All Tests"

**Missing assets**:
- Run `Tools > Combat System > Create Test Assets` first

