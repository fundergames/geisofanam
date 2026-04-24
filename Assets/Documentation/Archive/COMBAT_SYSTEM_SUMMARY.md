# Combat System Proposal - Executive Summary

## Overview

This proposal outlines a comprehensive, modular combat system architecture that extends your existing RogueDeal combat infrastructure to support:

- **Animation-driven timing** (no hardcoded delays)
- **Fully data-driven effects** (designers create content without code)
- **Multiple game modes** (video poker, turn-based RPG, real-time, open-world)
- **Headless simulation** for Monte Carlo balance testing
- **Extensible architecture** for future game types

## Key Documents

1. **COMBAT_SYSTEM_PROPOSAL.md** - Full architecture proposal with analysis
2. **COMBAT_SYSTEM_IMPLEMENTATION_GUIDE.md** - Detailed code examples and integration guide
3. **This document** - Quick reference and next steps

## Architecture Highlights

### Three-Layer Design

```
Presentation Layer (Unity MonoBehaviour)
    ↓ Uses
Logic Layer (Pure C#)
    ↓ Uses
Data Layer (ScriptableObjects)
    ↓ Can run in
Simulation Layer (Headless)
```

### Core Innovations

1. **CombatEntityData**: Pure C# data class (no Unity dependencies) for simulation
2. **BaseEffect System**: Composable, extensible effects (Damage, StatModifier, Status, Conditional, Multi)
3. **Animation Event Parsing**: Generic string-based events ("EventType:Param1:Param2")
4. **TargetingStrategy Pattern**: Swappable targeting for different game modes
5. **ActionCooldownManager**: Unified cooldown system (turn-based, time-based, charges, GCD)
6. **CombatSimulator**: Headless combat execution for balance testing

## Current State vs. Proposed State

### What You Have ✅
- CombatEntity, CombatStats
- AbilityData, EffectData (basic)
- StatusEffect system
- CombatManager (turn-based)
- Animation event receiver (basic)
- Targeting interface

### What's Missing ❌
- Animation-driven timing (currently hardcoded delays)
- Generic, composable effect system
- Weapon configuration system
- Combat profile (engagement distance, range)
- Collision-based hit detection
- Projectile system
- Persistent AOE
- Advanced cooldown system
- Combo system (animation-driven)
- Simulation capability

### What We're Proposing 🎯
- **Build on existing**: Keep CombatEntity, enhance with CombatEntityData
- **Extend AbilityData**: Create CombatAction that extends/enhances it
- **Enhance effects**: Create BaseEffect hierarchy
- **Add new systems**: Weapon, CombatProfile, CooldownManager
- **Add presentation**: CombatExecutor, WeaponHitbox, Projectile, PersistentAOE
- **Add simulation**: CombatSimulator, MonteCarloBalancer

## Implementation Phases

### Phase 1: Foundation (Week 1-2)
- Create CombatEntityData
- Create BaseEffect hierarchy
- Create Weapon and CombatProfile ScriptableObjects
- Create ActionCooldownManager

**Deliverable**: Data structures ready, can create effects in editor

### Phase 2: Targeting & Effects (Week 2-3)
- Enhance TargetingStrategy system
- Implement all effect types
- Create CalculatedEffect system
- Integration with existing status effects

**Deliverable**: Full effect system working, designers can create new effects

### Phase 3: Animation & Presentation (Week 3-4)
- Enhance CombatEventReceiver
- Create CombatExecutor
- Create WeaponHitbox system
- Create Projectile system
- Create PersistentAOE system

**Deliverable**: Animation-driven combat working, all visual systems functional

### Phase 4: Integration (Week 4-5)
- Integrate with existing CombatEntity
- Update CombatAbilityExecutor
- Create combo system
- Add weapon configuration support

**Deliverable**: Full combat system integrated, works with existing code

### Phase 5: Simulation (Week 5-6)
- Create CombatSimulator
- Create MonteCarloBalancer
- Create editor tools
- Create balance report visualization

**Deliverable**: Can run headless simulations, generate balance reports

## Migration Strategy

### Backward Compatibility
- **Keep existing systems working** during transition
- **Gradual migration** - new features use new system
- **Adapter layer** if needed for compatibility

### Example Migration Path
1. Create new CombatAction for new abilities
2. Keep old AbilityData for existing abilities
3. Migrate one ability at a time
4. Remove old system once all migrated

## Key Design Decisions

### 1. Why CombatEntityData?
- **Simulation**: Pure C# = no Unity dependencies = headless execution
- **Cloning**: Easy to clone for independent simulations
- **Separation**: Logic separate from presentation

### 2. Why BaseEffect Hierarchy?
- **Extensibility**: Add new effects without touching existing code
- **Composability**: MultiEffect combines multiple effects
- **Data-Driven**: Designers create effects via ScriptableObjects

### 3. Why String-Based Animation Events?
- **Flexibility**: Animators can trigger any event without code changes
- **Extensibility**: Add new event types by parsing strings
- **Designer-Friendly**: No code knowledge needed

### 4. Why TargetingStrategy Pattern?
- **Game Mode Support**: Different targeting for poker vs open-world
- **Swappable**: Change targeting per action
- **Testable**: Easy to test different strategies

## Example Workflows

### Designer Creates Fire Sword Attack
1. Create Weapon: Fire Sword (Physical 1.0x, Fire 1.2x)
2. Create Effects: Physical Damage (50), Fire Damage (20), Burn Status
3. Create MultiEffect combining all three
4. Create CombatAction: FireSlash
   - Assign MultiEffect
   - Assign SingleTargetSelector
   - Set animation trigger
   - Set cooldown (3 turns)
5. Animator: Add events at frames 20, 25, 30

**No code changes needed!**

### Designer Creates Meteor Shower
1. Create GroundTargetedAOE targeting strategy
2. Create Fire Damage Effect (30)
3. Create PersistentAOE prefab
4. Create CombatAction: MeteorShower
   - Assign GroundTargetedAOE
   - Assign Fire Damage
   - Set AOE prefab, pulse count (5), pulse duration (1s)
5. Animator: Add event at frame 30: "SpawnPersistentAOE"

**No code changes needed!**

## Testing Strategy

### Unit Tests
- All pure C# logic (CombatEntityData, BaseEffect, CooldownManager)
- Effect calculations
- Cooldown logic

### Integration Tests
- Combat flow end-to-end
- Animation event handling
- Hit detection

### Simulation Tests
- Monte Carlo runs produce consistent results
- Balance reports are accurate
- Deterministic RNG works correctly

## Risk Mitigation

### Risk: Breaking Existing Combat
**Mitigation**: Keep old system working, gradual migration

### Risk: Performance Issues
**Mitigation**: Object pooling, efficient AOE checks, dictionary lookups

### Risk: Complexity
**Mitigation**: Clear documentation, example workflows, extension points

### Risk: Simulation Accuracy
**Mitigation**: Deterministic RNG, fixed-point math if needed, validation tests

## Success Criteria

✅ Designers can create new actions/effects without code
✅ Animators control all timing via events
✅ System works in video poker mode
✅ System works in turn-based RPG mode
✅ System works in real-time action mode
✅ System works in open-world mode
✅ Can run headless simulations
✅ Can generate balance reports
✅ Performance is acceptable
✅ Code is maintainable and extensible

## Next Steps

1. **Review Proposal** - Review COMBAT_SYSTEM_PROPOSAL.md
2. **Review Implementation Guide** - Review COMBAT_SYSTEM_IMPLEMENTATION_GUIDE.md
3. **Clarify Requirements** - Answer questions in proposal
4. **Approve Architecture** - Sign off on design
5. **Begin Phase 1** - Start implementation

## Questions to Answer

1. **Backward Compatibility**: Full compatibility required, or can we migrate?
2. **Priority**: Which game mode first? (Video poker, turn-based, real-time, open-world)
3. **Timeline**: What's the deadline? Can we do phased rollout?
4. **Resources**: Who's implementing? (You, team, contractors)
5. **Testing**: What level of testing needed? (Unit, integration, simulation)
6. **Editor Tools**: What level of editor tooling needed?

## File Structure

```
Assets/RogueDeal/Scripts/Combat/
├── Core/                    # Pure C# logic
│   ├── Data/
│   ├── Effects/
│   ├── Targeting/
│   ├── Weapons/
│   └── Cooldowns/
├── Presentation/            # Unity MonoBehaviour
│   ├── CombatExecutor.cs
│   ├── CombatEventReceiver.cs
│   ├── WeaponHitbox.cs
│   ├── Projectile.cs
│   └── PersistentAOE.cs
└── Simulation/              # Headless simulation
    ├── CombatSimulator.cs
    ├── MonteCarloBalancer.cs
    └── BalanceReport.cs
```

## Quick Start

1. Read **COMBAT_SYSTEM_PROPOSAL.md** for full architecture
2. Read **COMBAT_SYSTEM_IMPLEMENTATION_GUIDE.md** for code examples
3. Review existing codebase (CombatEntity, AbilityData, etc.)
4. Answer questions above
5. Begin Phase 1 implementation

## Support

- Architecture questions → See COMBAT_SYSTEM_PROPOSAL.md
- Implementation questions → See COMBAT_SYSTEM_IMPLEMENTATION_GUIDE.md
- Code examples → See COMBAT_SYSTEM_IMPLEMENTATION_GUIDE.md
- Extension points → See COMBAT_SYSTEM_PROPOSAL.md

---

**Ready to proceed?** Review the documents, answer the questions, and we can begin implementation!


