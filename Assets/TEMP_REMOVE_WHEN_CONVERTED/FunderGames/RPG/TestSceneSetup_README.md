# 🎮 Test Scene Setup Guide

This guide will help you create a simple test scene to test your Open World RPG system with the player character and enemies.

## 🏗️ **Scene Setup**

### **Step 1: Create New Scene**
1. File → New Scene
2. Choose "3D" template
3. Save as "TestScene" in your Scenes folder

### **Step 2: Add Ground**
1. Create a Plane (GameObject → 3D Object → Plane)
2. Scale it to (10, 1, 10) for a large play area
3. Position at (0, 0, 0)
4. Add a material with a visible color/texture

### **Step 3: Add NavMesh**
1. Select the Plane
2. Window → AI → Navigation
3. In Navigation window, select "Object" tab
4. Check "Navigation Static"
5. Select "Bake" tab and click "Bake" button
6. This creates a navigation mesh for enemies

## 🎭 **Player Setup**

### **Step 1: Create Player**
1. Create empty GameObject named "Player"
2. Set Tag to "Player"
3. Position at (0, 1, 0)

### **Step 2: Add Character Model**
1. Create a Capsule as child of Player
2. Position at (0, 0, 0) relative to Player
3. Add `Animator` component
4. Assign `PlayerAnimatorController` to Animator

### **Step 3: Add Components**
1. **CharacterController** (on Player GameObject)
   - Height: 2, Radius: 0.5, Center: (0, 1, 0)

2. **PlayerController Script**
   - Assign the Capsule's Animator to `animator` field
   - Set Walk Speed: 5, Run Speed: 8

3. **PlayerHealth Script**
   - Max Health: 100, Respawn Delay: 3

### **Step 4: Add Camera**
1. Create Camera as child of Player
2. Position at (0, 2, -5)
3. Set as Main Camera

## 👹 **Enemy Setup**

### **Step 1: Create Enemy Prefab**
1. Create empty GameObject named "Enemy"
2. Add a Cube as child (for visibility)
3. Scale to (1, 2, 1)
4. Position at (0, 1, 0) relative to Enemy

### **Step 2: Add Enemy Components**
1. **NavMeshAgent** (on Enemy GameObject)
   - Speed: 3.5, Angular Speed: 120
   - Stopping Distance: 2, Attack Range: 2

2. **EnemyAI Script**
   - Detection Range: 8, Attack Range: 2
   - Patrol Radius: 5, Attack Cooldown: 1.5

3. **EnemyHealth Script**
   - Max Health: 50, Death Delay: 2

4. **Animator** (on Cube)
   - Assign `EnemyAnimatorController`

### **Step 3: Create Prefab**
1. Drag Enemy from Hierarchy to Project window
2. Delete from scene (keep prefab)

## 🚀 **Enemy Spawner Setup**

### **Step 1: Create Spawner**
1. Create empty GameObject named "EnemySpawner"
2. Position at (0, 0, 0)

### **Step 2: Add Spawner Script**
1. Add `EnemySpawner` script
2. Assign Enemy prefab to `enemyPrefab` field
3. Set Max Enemies: 5, Spawn Interval: 3
4. Add spawn points around the scene

### **Step 3: Add Spawn Points**
1. Create empty GameObjects as children of Spawner
2. Position them around the scene (e.g., at corners)
3. Assign them to the `spawnPoints` array

## 🎯 **Testing Checklist**

### **Player Testing**
- [ ] WASD movement works
- [ ] Mouse look works
- [ ] Space bar jumps
- [ ] Left click attacks
- [ ] Shift key runs
- [ ] Camera follows player
- [ ] Animations play correctly

### **Enemy Testing**
- [ ] Enemies spawn at spawn points
- [ ] Enemies patrol when player is far
- [ ] Enemies chase when player is detected
- [ ] Enemies attack when in range
- [ ] Enemies take damage and die
- [ ] Enemy animations work

### **Combat Testing**
- [ ] Player can attack enemies
- [ ] Enemies can attack player
- [ ] Health system works
- [ ] Player respawns when killed
- [ ] Enemies drop items (if configured)

## 🔧 **Quick Test Commands**

### **In Play Mode**
- **WASD** - Move around
- **Mouse** - Look around
- **Space** - Jump
- **Shift** - Run
- **Left Click** - Attack
- **Walk near enemies** - Test detection
- **Get close to enemies** - Test combat

### **Scene View**
- **F** key - Focus on selected object
- **Ctrl+Shift+F** - Focus on player
- **Scene Gizmo** - Switch between perspectives

## 📊 **Performance Monitoring**

### **Stats Window**
1. Window → General → Stats
2. Monitor FPS, Draw Calls, Memory
3. Look for performance issues

### **Profiler**
1. Window → General → Profiler
2. Record during gameplay
3. Analyze performance bottlenecks

## 🎨 **Visual Improvements**

### **Lighting**
1. Add Directional Light
2. Adjust intensity and rotation
3. Enable shadows

### **Materials**
1. Create materials for ground, player, enemies
2. Use different colors for visibility
3. Add normal maps for detail

### **Particles**
1. Add particle effects for attacks
2. Add impact effects for damage
3. Add spawn effects for enemies

---

## 🚀 **Ready to Test!**

Your test scene is now set up with:
- ✅ Ground with NavMesh
- ✅ Player character with full controls
- ✅ Enemy prefab with AI
- ✅ Enemy spawner system
- ✅ Basic combat system

**Press Play and start testing your Open World RPG!** 🎮⚔️

---

## 📚 **Next Steps After Testing**

1. **Refine Movement** - Adjust speeds and responsiveness
2. **Balance Combat** - Tune damage, health, and timing
3. **Add Content** - More enemy types, weapons, abilities
4. **Polish** - Effects, sounds, UI, and visual feedback
5. **Scale Up** - Larger world, more complex AI, quests
