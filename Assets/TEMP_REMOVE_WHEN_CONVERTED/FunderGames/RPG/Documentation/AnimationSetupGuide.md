# RPG Animation Setup Guide

## Overview
This guide will help you set up the comprehensive animation system for your RPG character using all the available animations from the `RootMotion` and `NoWeapon` folders.

## Files Created
1. **NoWeaponAnimatorController_New.controller** - New animator controller with all parameters
2. **PlayerAnimationController.cs** - C# script to control animations
3. **AnimationSetupGuide.md** - This setup guide

## Step-by-Step Setup

### 1. Set Up the Animator Controller

1. **Open the new animator controller** in Unity:
   - Navigate to `Assets/FunderGames/RPG/Assets/Animators/NoWeaponAnimatorController_New.controller`
   - Double-click to open the Animator window

2. **Add Animation States**:
   - Right-click in the Animator window → Create State → Empty
   - Name each state according to the animation (e.g., "Idle_Normal", "Walk", "Run")
   - Assign the corresponding animation clip to each state

3. **Set up the State Machine**:
   - Create transitions between states based on your game logic
   - Use the parameters defined in the controller for transitions

### 2. Assign Animation Clips

#### Basic Movement Animations
- **Idle_Normal** → `Idle_Normal_NoWeapon.fbx`
- **Idle_Battle** → `Idle_Battle_NoWeapon.fbx`
- **Walk** → `WalkFWD_RM_NoWeapon.fbx`
- **Run** → `SprintFWD_Battle_RM_NoWeapon.fbx`

#### Combat Animations
- **Attack01** → `Attack01_NoWeapon.fbx`
- **Attack02** → `Attack02_NoWeapon.fbx`
- **Attack03** → `Attack03_NoWeapon.fbx`
- **Attack04** → `Attack04_NoWeapon.fbx`
- **Attack05** → `Attack05_NoWeapon.fbx`
- **Combo01** → `Combo01_RM_NoWeapon.fbx`
- **Combo02** → `Combo02_RM_NoWeapon.fbx`
- **Combo03** → `Combo03_RM_NoWeapon.fbx`
- **Combo04** → `Combo04_RM_NoWeapon.fbx`
- **Combo05** → `Combo05_RM_NoWeapon.fbx`

#### Movement Animations (Root Motion)
- **MoveFWD_Normal** → `MoveFWD_Normal_RM_NoWeapon.fbx`
- **MoveFWD_Battle** → `MoveFWD_Battle_RM_NoWeapon.fbx`
- **MoveBWD_Battle** → `MoveBWD_Battle_RM_NoWeapon.fbx`
- **MoveLFT_Battle** → `MoveLFT_Battle_RM_NoWeapon.fbx`
- **MoveRGT_Battle** → `MoveRGT_Battle_RM_NoWeapon.fbx`

#### Special Movement
- **RollFWD** → `RollFWD_Battle_RM_NoWeapon.fbx`
- **RollBWD** → `RollBWD_Battle_RM_NoWeapon.fbx`
- **RollLFT** → `RollLFT_Battle_RM_NoWeapon.fbx`
- **RollRGT** → `RollRGT_Battle_RM_NoWeapon.fbx`
- **DashFWD** → `DashFWD_Battle_RM_NoWeapon.fbx`
- **DashBWD** → `DashBWD_Battle_RM_NoWeapon.fbx`
- **DashLFT** → `DashLFT_Battle_RM_NoWeapon.fbx`
- **DashRGT** → `DashRGT_Battle_RM_NoWeapon.fbx`

#### Jump Animations
- **JumpFull** → `JumpFull_RM_NoWeapon.fbx`
- **JumpFullSpin** → `JumpFullSpin_RM_NoWeapon.fbx`

#### Swimming
- **Swimming** → `Swimming_RM_NoWeapon.fbx`
- **Swimming_Floating** → `Swimming_Floating_NoWeapon.fbx`

#### Climbing
- **ClimbUp** → `ClimbUp_RM_NoWeapon.fbx`
- **ClimbDown** → `ClimbDown_RM_NoWeapon.fbx`

#### Interaction & Social
- **CarryStart** → `CarryStart_NoWeapon.fbx`
- **CarryMoveIdle** → `CarryMoveIdle_NoWeapon.fbx`
- **CarryThrow** → `CarryThrow_NoWeapon.fbx`
- **InteractGate** → `InteractWithGateObject_NoWeapon.fbx`
- **InteractPeople** → `InteractWithPeople_NoWeapon.fbx`
- **Defend** → `Defend_NoWeapon.fbx`
- **DefendHit** → `DefendHit_NoWeapon.fbx`
- **Greeting01** → `Greeting01_NoWeapon.fbx`
- **Greeting02** → `Greeting02_NoWeapon.fbx`
- **Dance** → `Dance_NoWeapon.fbx`
- **Challenging** → `Challenging_NoWeapon.fbx`
- **Victory** → `Victory_NoWeapon.fbx`

#### Status Effects
- **TakeDamage01** → `GetHit01_NoWeapon.fbx`
- **TakeDamage02** → `GetHit02_NoWeapon.fbx`
- **Die01** → `Die01_NoWeapon.fbx`
- **Die02** → `Die02_NoWeapon.fbx`
- **Die01Stay** → `Die01Stay_NoWeapon.fbx`
- **Dizzy** → `Dizzy_NoWeapon.fbx`
- **Sleeping** → `Sleeping_NoWeapon.fbx`
- **GetUp** → `GetUp_NoWeapon.fbx`

#### Special Actions
- **Push** → `Push_RM_NoWeapon.fbx`
- **DrinkPotion** → `DrinkPotion_NoWeapon.fbx`
- **LevelUp** → `LevelUp_NoWeapon.fbx`
- **SenseStart** → `SenseSomethingStart_NoWeapon.fbx`
- **SenseSearching** → `SenseSomethingSearching_NoWeapon.fbx`
- **FoundSomething** → `FoundSomething_NoWeapon.fbx`

### 3. Set Up the Player Controller

1. **Add the PlayerAnimationController script** to your player GameObject
2. **Assign the new animator controller** to the Animator component
3. **Assign animation clips** in the inspector (optional, for reference)

### 4. Configure Transitions

#### Basic Movement Flow
```
Idle → Walk (Speed > 0.1)
Walk → Run (Speed > 6.0)
Run → Walk (Speed < 6.0)
Walk → Idle (Speed < 0.1)
```

#### Combat Flow
```
Any State → Attack (Attack trigger)
Attack → Idle (Attack animation ends)
Any State → TakeDamage (TakeDamage trigger)
TakeDamage → Idle (TakeDamage animation ends)
```

#### Special Movement Flow
```
Any State → Roll (IsRolling = true)
Roll → Idle (Roll animation ends)
Any State → Dash (IsDashing = true)
Dash → Idle (Dash animation ends)
```

### 5. Integration with Player Controller

The `PlayerAnimationController` script provides methods for your player controller to trigger animations:

```csharp
// Get reference to animation controller
PlayerAnimationController animController = GetComponent<PlayerAnimationController>();

// Trigger animations
animController.TriggerJump();
animController.TriggerAttack(1); // Combo number
animController.TriggerRoll(1f); // Direction
animController.SetSwimming(true);
animController.SetInBattle(true);
```

### 6. Animation Events

Add animation events to your animation clips to call methods like:
- `OnAttackStart()` - Called when attack animation starts
- `OnAttackEnd()` - Called when attack animation ends
- `OnRollStart()` - Called when roll animation starts
- `OnRollEnd()` - Called when roll animation ends

### 7. Performance Optimization

- Use parameter hashes (already implemented in the script)
- Keep transitions simple and logical
- Use exit time for smooth transitions
- Consider using blend trees for movement variations

## Troubleshooting

### Common Issues
1. **Animations not playing**: Check if the animator controller is assigned
2. **Transitions not working**: Verify parameter names match exactly
3. **Root motion issues**: Ensure animations have root motion enabled
4. **Performance issues**: Check for unnecessary parameter updates

### Debug Tips
- Use the Animator window to monitor parameter values
- Enable "Show Sample Rate" in the Animator window
- Check the Console for animation-related errors
- Use the Animation window to preview clips

## Next Steps

1. Set up the basic movement states first
2. Add combat animations
3. Implement special movement (roll, dash)
4. Add interaction and social animations
5. Fine-tune transitions and timing
6. Test with your player controller
7. Optimize performance

## Support

If you encounter issues:
1. Check the Unity Console for errors
2. Verify all animation clips are properly imported
3. Ensure the animator controller is correctly configured
4. Test with simple animations first
