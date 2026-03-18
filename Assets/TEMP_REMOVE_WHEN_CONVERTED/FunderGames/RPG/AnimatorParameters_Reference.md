# 🎭 Animator Parameters Quick Reference

## 🎮 **Player Animator Controller**

### **Float Parameters**
| Parameter | Type | Usage | Values |
|-----------|------|-------|---------|
| `Speed` | Float | Movement speed | 0 = Idle, 0.1-6 = Walk, 6+ = Run |

### **Bool Parameters**
| Parameter | Type | Usage | Default |
|-----------|------|-------|---------|
| `IsGrounded` | Bool | Ground detection | false |
| `IsRunning` | Bool | Running state | false |
| `IsAttacking` | Bool | Attack state | false |

### **Trigger Parameters**
| Parameter | Type | Usage | When to Call |
|-----------|------|-------|--------------|
| `Attack` | Trigger | Start attack | Left Click / Attack Input |
| `Jump` | Trigger | Start jump | Space / Jump Input |
| `TakeDamage` | Trigger | Hit reaction | When damaged |
| `Die` | Trigger | Death animation | When health reaches 0 |

---

## 👹 **Enemy Animator Controller**

### **Float Parameters**
| Parameter | Type | Usage | Values |
|-----------|------|-------|---------|
| `Speed` | Float | Movement speed | 0 = Idle, 0.1+ = Moving |

### **Bool Parameters**
| Parameter | Type | Usage | Default |
|-----------|------|-------|---------|
| `IsAttacking` | Bool | Attack state | false |
| `IsChasing` | Bool | Chase state | false |

### **Trigger Parameters**
| Parameter | Type | Usage | When to Call |
|-----------|------|-------|--------------|
| `Attack` | Trigger | Start attack | When in attack range |
| `Die` | Trigger | Death animation | When health reaches 0 |
| `TakeDamage` | Trigger | Hit reaction | When damaged |

---

## 🔄 **State Transitions**

### **Player States**
```
Idle ←→ Walk ←→ Run
  ↓      ↓      ↓
Jump   Jump   Jump
  ↓      ↓      ↓
Fall   Fall   Fall

Attack → Idle (when IsAttacking = false)
TakeDamage → Idle
Die → (no exit)
```

### **Enemy States**
```
Idle ←→ Patrol ←→ Chase
  ↓       ↓       ↓
Attack  Attack  Attack
  ↓       ↓       ↓
TakeDamage → Idle
Die → (no exit)
```

---

## 💻 **Code Usage Examples**

### **Setting Parameters in PlayerController**
```csharp
// Movement speed
animator.SetFloat("Speed", currentSpeed);

// Ground detection
animator.SetBool("IsGrounded", isGrounded);

// Running state
animator.SetBool("IsRunning", isRunning);

// Attack state
animator.SetBool("IsAttacking", isAttacking);

// Triggers
animator.SetTrigger("Attack");
animator.SetTrigger("Jump");
animator.SetTrigger("TakeDamage");
animator.SetTrigger("Die");
```

### **Setting Parameters in EnemyAI**
```csharp
// Movement speed
animator.SetFloat("Speed", agent.velocity.magnitude);

// Chase state
animator.SetBool("IsChasing", currentState == AIState.Chase);

// Attack state
animator.SetBool("IsAttacking", isAttacking);

// Triggers
animator.SetTrigger("Attack");
animator.SetTrigger("TakeDamage");
animator.SetTrigger("Die");
```

---

## ⚠️ **Important Notes**

1. **Parameter Names Must Match Exactly** - Case sensitive!
2. **Triggers Auto-Reset** - Don't manually reset trigger parameters
3. **Bool Parameters** - Set to false when exiting states
4. **Float Parameters** - Update every frame for smooth transitions
5. **State Names** - Must match exactly in the animator controller

---

## 🎯 **Quick Setup Checklist**

- [ ] Animator component attached to character model
- [ ] Correct animator controller assigned
- [ ] All required parameters exist in controller
- [ ] Animation clips assigned to states
- [ ] Transitions configured properly
- [ ] Parameters being set in scripts
- [ ] Test all animations in Play mode

**Remember: The animator controller handles the logic, your scripts just set the parameters!** 🎭✨
