# 🌍 Open World RPG System

A comprehensive open world RPG framework for Unity that includes player movement, enemy AI, combat, and world interaction systems.

## 🚀 **Features**

### **Player System**
- **Smooth Movement**: WASD/Arrow keys with mouse look
- **Jumping**: Spacebar for vertical movement
- **Running**: Left Shift for faster movement
- **Combat**: Left mouse button for attacks
- **Camera Control**: Third-person camera with mouse look
- **Health System**: Player health with respawn mechanics

### **Enemy AI System**
- **Patrol Behavior**: Enemies patrol designated areas
- **Detection**: Enemies detect and chase the player
- **Combat**: Enemies attack when in range
- **Return Behavior**: Enemies return to patrol when player escapes
- **State Machine**: Clean AI state management

### **Combat System**
- **Melee Combat**: Close-range attacks with damage
- **Health Management**: Both player and enemies have health
- **Invulnerability**: Temporary damage immunity after taking hits
- **Death Handling**: Proper death and respawn mechanics

### **World System**
- **Enemy Spawning**: Dynamic enemy spawning system
- **Spawn Points**: Configurable spawn locations
- **Population Control**: Maximum enemy limits
- **Distance Management**: Smart spawning based on player location

## 🛠️ **Setup Instructions**

### **Step 1: Project Setup**

1. **Ensure you have the Input System package**:
   - Go to `Window > Package Manager`
   - Install `Input System` package if not already installed

2. **Set up NavMesh for enemy AI**:
   - Select your terrain/ground objects
   - Go to `Window > AI > Navigation`
   - Click `Bake` to generate NavMesh

### **Step 2: Player Setup**

1. **Create Player GameObject**:
   ```
   - Create Empty GameObject → Name it "Player"
   - Add CharacterController component
   - Add Animator component (if you have animations)
   - Add PlayerController script
   - Add PlayerHealth script
   ```

2. **Configure PlayerController**:
   - Set `Walk Speed` (default: 6)
   - Set `Run Speed` (default: 12)
   - Set `Jump Height` (default: 2)
   - Set `Attack Range` (default: 2)
   - Set `Attack Damage` (default: 25)

3. **Set up Input Actions**:
   - Import the `PlayerInputActions.inputactions` file
   - In PlayerController, assign the Input Actions asset
   - Enable the Input System in Project Settings

### **Step 3: Enemy Setup**

1. **Create Enemy Prefab**:
   ```
   - Create 3D model (capsule, character, etc.)
   - Add NavMeshAgent component
   - Add Animator component
   - Add EnemyAI script
   - Add EnemyHealth script
   - Add Collider (for combat detection)
   ```

2. **Configure EnemyAI**:
   - Set `Detection Range` (default: 10)
   - Set `Attack Range` (default: 2)
   - Set `Patrol Radius` (default: 10)
   - Set `Attack Damage` (default: 20)

3. **Configure EnemyHealth**:
   - Set `Max Health` (default: 100)
   - Set `Death Delay` (default: 2)
   - Set `Experience Value` (default: 10)

### **Step 4: Enemy Spawner Setup**

1. **Create Enemy Spawner**:
   ```
   - Create Empty GameObject → Name it "EnemySpawner"
   - Add EnemySpawner script
   - Assign enemy prefabs to spawn
   ```

2. **Configure Spawner**:
   - Set `Max Total Enemies` (default: 20)
   - Set `Global Spawn Interval` (default: 10)
   - Set `Min/Max Spawn Distance` from player

3. **Set up Spawn Points**:
   - The system will auto-generate spawn points
   - Or manually create and assign custom spawn points

### **Step 5: Scene Setup**

1. **Tag your objects**:
   - Player: Tag as "Player"
   - Enemies: Tag as "Enemy"

2. **Set up layers**:
   - Create "Player" and "Enemy" layers
   - Assign appropriate layer masks in scripts

3. **Camera setup**:
   - Ensure you have a main camera
   - The PlayerController will automatically set up camera positioning

## 🎮 **Controls**

| Action | Input |
|--------|-------|
| **Move** | WASD or Arrow Keys |
| **Look** | Mouse Movement |
| **Jump** | Spacebar |
| **Attack** | Left Mouse Button |
| **Run** | Left Shift |
| **Toggle Cursor** | Call `ToggleCursorLock()` |

## 🔧 **Customization**

### **Player Customization**
- Adjust movement speeds in PlayerController
- Modify camera settings (distance, height, sensitivity)
- Customize attack range and damage
- Add custom animations

### **Enemy Customization**
- Modify AI behavior ranges
- Adjust patrol patterns
- Customize attack patterns
- Add different enemy types

### **Combat Customization**
- Modify damage values
- Adjust invulnerability timing
- Add armor/resistance systems
- Implement different weapon types

### **World Customization**
- Adjust spawn rates and locations
- Modify enemy population limits
- Add different spawn patterns
- Implement day/night cycles

## 📁 **File Structure**

```
Assets/FunderGames/RPG/Scripts/OpenWorld/
├── PlayerController.cs          # Main player movement and combat
├── EnemyAI.cs                   # Enemy AI behavior system
├── HealthSystem.cs              # Health system for all entities
├── EnemySpawner.cs              # Enemy spawning system
└── InputActions/
    └── PlayerInputActions.inputactions  # Input configuration
```

## 🐛 **Troubleshooting**

### **Common Issues**

1. **Player not moving**:
   - Check if Input System is enabled
   - Verify Input Actions asset is assigned
   - Ensure CharacterController is attached

2. **Enemies not moving**:
   - Check if NavMesh is baked
   - Verify NavMeshAgent component is attached
   - Check if enemy is on NavMesh

3. **Combat not working**:
   - Verify layer masks are set correctly
   - Check if attack point is positioned correctly
   - Ensure enemies have EnemyHealth component

4. **Camera issues**:
   - Check if main camera exists
   - Verify camera target is assigned
   - Adjust camera distance and height settings

### **Performance Tips**

1. **Limit enemy count** based on your target platform
2. **Use object pooling** for frequently spawned enemies
3. **Optimize NavMesh** by reducing unnecessary detail
4. **LOD systems** for distant enemies

## 🚀 **Next Steps**

### **Immediate Additions**
- **Inventory System**: Item collection and management
- **Quest System**: Mission objectives and rewards
- **Leveling System**: Experience and character progression
- **Save System**: Game state persistence

### **Advanced Features**
- **Magic System**: Ranged combat and spells
- **Crafting System**: Item creation and modification
- **Trading System**: NPC interactions and commerce
- **Multiplayer**: Cooperative or competitive gameplay

## 📚 **API Reference**

### **PlayerController Methods**
- `ToggleCursorLock()`: Toggle mouse cursor lock
- `OnMove(InputValue)`: Handle movement input
- `OnAttack(InputValue)`: Handle attack input

### **EnemyAI Methods**
- `GetCurrentState()`: Get current AI state
- `ForceChase()`: Force enemy to chase player
- `ResetToPatrol()`: Reset enemy to patrol state

### **HealthSystem Methods**
- `TakeDamage(float)`: Apply damage to entity
- `Heal(float)`: Heal entity
- `Revive(float)`: Revive dead entity
- `MakeInvulnerable(float)`: Make entity temporarily invulnerable

### **EnemySpawner Methods**
- `ForceSpawnAtPoint(int, int)`: Force spawn at specific point
- `ClearAllEnemies()`: Remove all enemies
- `GetEnemyCount()`: Get current enemy count
- `ToggleSpawnPoint(int)`: Enable/disable spawn point

## 🤝 **Contributing**

Feel free to extend this system with:
- New enemy types and behaviors
- Additional combat mechanics
- World interaction systems
- UI improvements
- Performance optimizations

## 📄 **License**

This system is part of the FunderGames RPG framework. Use it to create amazing open world RPG experiences!

---

**Happy Game Development! 🎮✨**
