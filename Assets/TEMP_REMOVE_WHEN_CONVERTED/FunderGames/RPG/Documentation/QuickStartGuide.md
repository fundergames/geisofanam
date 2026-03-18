# Quick Start Guide - RPG Animation System

## Setup Steps

### 1. Create Your Player GameObject
1. Create an empty GameObject in your scene
2. Name it "Player"
3. Add a **CharacterController** component
4. Add a **Capsule** or **Character Model** as a child (this should have the Animator component)

### 2. Set Up the Animator
1. Select your character model (the one with the Animator component)
2. In the Animator component, assign `NoWeaponAnimatorController_New.controller`
3. Make sure "Apply Root Motion" is checked for root motion animations

### 3. Add the Scripts
1. Add `PlayerAnimationController.cs` to your **Player** GameObject
2. Add `PlayerController.cs` to your **Player** GameObject

### 4. Configure the Animator Controller
1. Double-click the animator controller to open the Animator window
2. Create states for your basic animations:
   - **Idle** → Assign `Idle_Normal_NoWeapon.fbx`
   - **Walk** → Assign `WalkFWD_RM_NoWeapon.fbx`
   - **Run** → Assign `SprintFWD_Battle_RM_NoWeapon.fbx`
   - **Jump** → Assign `JumpFull_RM_NoWeapon.fbx`
   - **Attack** → Assign `Attack01_NoWeapon.fbx`
   - **Roll** → Assign `RollFWD_Battle_RM_NoWeapon.fbx`
   - **Dash** → Assign `DashFWD_Battle_RM_NoWeapon.fbx`

### 5. Set Up Basic Transitions
```
Idle → Walk (Speed > 0.1)
Walk → Run (Speed > 6.0)
Run → Walk (Speed < 6.0)
Walk → Idle (Speed < 0.1)
Any State → Jump (Jump trigger)
Any State → Attack (Attack trigger)
Any State → Roll (IsRolling = true)
Any State → Dash (IsDashing = true)
```

### 6. Test Basic Movement
- **WASD** - Move
- **Left Shift** - Run
- **Space** - Jump
- **Q** - Roll
- **E** - Dash
- **Left Mouse** - Attack
- **Right Mouse** - Defend

## Common Issues & Solutions

### Animations Not Playing
- Check if the Animator component is assigned
- Verify the animator controller is assigned
- Ensure animation clips are properly imported

### Root Motion Not Working
- Check "Apply Root Motion" in the Animator component
- Verify animations have root motion data
- Make sure CharacterController is on the parent GameObject

### Script Errors
- Ensure both scripts are in the same namespace (`FunderGames.RPG`)
- Check that all required components are present
- Verify script compilation

## Next Steps

1. **Add More Animation States** - Gradually add all the animations you want
2. **Fine-tune Transitions** - Adjust transition durations and conditions
3. **Add Animation Events** - Use the provided callback methods
4. **Customize Controls** - Modify the input keys in PlayerController
5. **Add Sound Effects** - Integrate with your audio system

## File Structure
```
Assets/FunderGames/RPG/
├── Scripts/
│   ├── PlayerController.cs
│   └── PlayerAnimationController.cs
├── Assets/Animators/
│   └── NoWeaponAnimatorController_New.controller
└── Documentation/
    ├── AnimationSetupGuide.md
    └── QuickStartGuide.md
```

## Support
If you encounter issues:
1. Check the Unity Console for errors
2. Verify all components are properly assigned
3. Test with simple animations first
4. Refer to the detailed AnimationSetupGuide.md
