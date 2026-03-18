# 3rd Person Combat Test Scene Setup

This editor tool creates a minimal test scene for 3rd person combat testing.

## How to Use

1. In Unity Editor, go to **Tools > Combat Setup > Create 3rd Person Combat Test Scene**
2. The tool will:
   - Create a new scene with all necessary components
   - Set up a player character with movement and combat
   - Add a 3rd person camera that follows the player
   - Place 3 training dummies for target practice
   - Create basic combat actions for testing
   - Save the scene to `Assets/RogueDeal/Scenes/ThirdPersonCombatTest.unity`

## What Gets Created

### Player
- **CharacterController** for movement
- **ThirdPersonCombatController** for input and combat
- **CombatEntity** with stats
- **CombatExecutor** for action execution
- **CombatEventReceiver** for animation events
- **Animator** (controller can be assigned later)

### Camera
- **Main Camera** with **CombatCameraController**
- Follows player at offset (0, 5, -8)
- Looks at player with offset (0, 1, 0)

### Training Dummies
- 3 red capsule dummies positioned in front of player
- Each has **CombatEntity** and **TrainingDummy** components
- Infinite health enabled by default
- Tagged as "Enemy"

### Combat Actions
- **Basic Attack** - 50 damage melee attack
- **Combo Attack** - 60 damage follow-up attack
- Saved to `Assets/RogueDeal/Resources/Combat/TestActions/`

## Controls

- **WASD** - Move
- **Left Shift** - Run
- **Space** - Dash
- **Left Click** - Attack

## Next Steps

1. **Assign Animator Controller**: 
   - Select the PlayerVisual GameObject
   - Assign your animator controller in the Animator component
   - Make sure it has states: `Idle`, `Walk`, `Run`, `Action_1`, `Action_2`, `Dash`

2. **Set Up Animation Events** (optional):
   - Add `OnCombatEvent("ApplyEffects")` to attack animations at hit frame
   - See `ANIMATION_EVENTS_SETUP.md` for details

3. **Test**:
   - Press Play
   - Move around with WASD
   - Attack dummies with Left Click
   - Check console for damage logs

## Troubleshooting

- **Player doesn't move**: Check that CharacterController is enabled and Animator has a controller assigned
- **Attacks don't work**: Make sure dummies are on the correct layer and have CombatEntity components
- **Camera doesn't follow**: Check that CombatCameraController has the player assigned as target
- **No damage**: Ensure animation events are set up, or the system will auto-apply when no animator controller exists

