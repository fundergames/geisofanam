# Card Combat System

This system allows characters to be rendered inside cards with a "window" effect, and when they attack, they physically jump out of the card, move to the enemy, attack, and then return to the card.

## Overview

The card combat system consists of several components that work together:

1. **CardCombatant** - Extended Combatant class that handles card-specific behavior
2. **JumpOutOfCardStep** - Action step that makes characters jump out of their cards
3. **CardWorldMovementStep** - Movement step for characters in the main world
4. **CardAttackStep** - Attack step for card characters
5. **JumpBackIntoCardStep** - Action step that returns characters to their cards
6. **CardAttackAction** - Complete attack action for card characters
7. **CardAttackSequence** - Pre-configured sequence for card attacks
8. **CardCombatSetup** - Utility script for setting up the system

## Setup Instructions

### 1. Add CardCombatant Component

Add the `CardCombatant` component to your existing combatant GameObjects. This component will:
- Store references to the card transform
- Remember the original position within the card
- Handle the transition between card and world space

### 2. Use CardCombatSetup Utility

The easiest way to set up the system is to use the `CardCombatSetup` utility:

1. Create an empty GameObject in your scene
2. Add the `CardCombatSetup` component to it
3. In the inspector, you can:
   - Manually assign combatants and card transforms
   - Or let it auto-detect them on start
   - Use the context menu "Setup Card Combat System" to run setup manually

### 3. Create Card Attack Actions

Instead of using the regular `AttackAction`, use `CardAttackAction` for characters that live in cards. This action will:
- Automatically jump the character out of the card
- Move them to the target
- Execute the attack
- Return them to the card

### 4. Set Up Action Sequences

You can create custom action sequences using the new card-specific steps:

1. **JumpOutOfCardStep** - Character jumps out of card
2. **CardWorldMovementStep** - Character moves in world space
3. **CardAttackStep** - Character attacks the target
4. **JumpBackIntoCardStep** - Character returns to card

## How It Works

### Card Rendering
- Characters are parented to card transforms
- The card system uses stencil masking to create the "window" effect
- Characters appear to live inside the card's environment

### Attack Sequence
1. **Jump Out**: Character unparents from card and jumps to a position in front of the card
2. **Move to Target**: Character hops toward the enemy using `CardWorldMovementStep`
3. **Attack**: Character plays attack animation and deals damage
4. **Return**: Character jumps back to the card and reparents to it

### Stencil System Integration
The system works with your existing stencil masking setup:
- Characters maintain their stencil IDs when jumping out of cards
- The card window effect continues to work during combat
- Characters can be seen both in the card and in the main world

## Configuration Options

### JumpOutOfCardStep
- `jumpPower`: Height of the jump out of the card
- `duration`: How long the jump takes
- `cardExitOffset`: Distance from the card to land

### CardWorldMovementStep
- `jumpPower`: Height of each hop
- `numJumps`: Number of hops to the target
- `duration`: Total movement time
- `approachDistance`: How close to get to the target

### CardAttackStep
- `attackDamage`: Amount of damage to deal
- `attackAnimationTrigger`: Animation trigger to play
- `attackDuration`: Total attack time
- `damageDelay`: When to apply damage during the attack

## Example Usage

### Basic Setup
```csharp
// The CardCombatSetup utility will handle most of the setup automatically
// Just add it to a GameObject in your scene and it will:
// 1. Find all Combatants
// 2. Add CardCombatant components
// 3. Create card transforms if none exist
// 4. Set up the parent-child relationships
```

### Custom Action Sequence
```csharp
// Create a custom sequence with specific timing
var sequence = ScriptableObject.CreateInstance<CardAttackSequence>();

// Or create individual steps
var jumpOut = ScriptableObject.CreateInstance<JumpOutOfCardStep>();
var moveToTarget = ScriptableObject.CreateInstance<CardWorldMovementStep>();
var attack = ScriptableObject.CreateInstance<CardAttackStep>();
var jumpBack = ScriptableObject.CreateInstance<JumpBackIntoCardStep>();
```

## Troubleshooting

### Character Not Jumping Out of Card
- Ensure the character has a `CardCombatant` component
- Check that the card transform reference is set
- Verify the character is properly parented to the card

### Character Not Returning to Card
- Check that the `JumpBackIntoCardStep` is in the action sequence
- Ensure the card transform reference is still valid
- Verify the character's original local position is stored

### Stencil Masking Issues
- Make sure stencil IDs are properly set on materials
- Check that the card window shader is using the correct stencil reference
- Verify render queue settings for proper layering

## Performance Considerations

- The system uses DOTween for smooth animations
- Characters are temporarily unparented during combat, which may affect stencil rendering
- Consider using object pooling for multiple simultaneous attacks
- The jump animations are optimized with configurable duration and easing

## Future Enhancements

Potential improvements to consider:
- Card-specific attack animations
- Different exit/entry animations based on card type
- Card damage effects (cracks, breaks, etc.)
- Multi-character card attacks
- Card combination attacks
