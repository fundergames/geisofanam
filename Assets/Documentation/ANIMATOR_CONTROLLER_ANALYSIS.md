# Third Person Controller - Animator Analysis

Analysis of `Assets/RogueDeal/Combat/Animations/ThirdPerson_Controller.controller`

## Current States ✅

The controller has these states:
- ✅ **Idle** (Default State)
- ✅ **Run Forward**
- ✅ **Attack_1**
- ✅ **Attack_2**
- ✅ **Attack_3**
- ✅ **DashFWD_Battle_RM_SingleSword** (Dash forward)
- ✅ **DashBWD_Battle_RM_SingleSword** (Dash backward)
- ✅ **Take Damage**
- ✅ **Die**
- ✅ **Level Up** (Optional)
- ✅ **Taunt** (Optional)
- ✅ **Use Potion** (Optional)
- ✅ **Attack02_SingleSword** (Alternative attack state)

## Current Parameters ✅

All required parameters exist:
- ✅ Speed (Float)
- ✅ Run (Bool)
- ✅ IsGrounded (Bool)
- ✅ Dash (Trigger)
- ✅ Attack_1, Attack_2, Attack_3 (Triggers)
- ✅ TakeAction, ActionIndex, IsAction (New system)

## Missing/Incorrect Transitions ❌

### 1. **Idle → Run Forward** ❌
**Current:** Uses `Run` trigger (condition mode 1 = true/trigger)
**Problem:** The script sets `Run` as a **Bool**, not a trigger!
**Should be:** `Run = true` (condition mode 6 = bool true)
**Also missing:** Transition using `Speed > 0.1` parameter

### 2. **Idle → Dash** ❌
**Current:** Has transition to DashFWD on `Combo` trigger
**Missing:** Transition to DashFWD on `Dash` trigger
**Should have:** `Dash` trigger → DashFWD_Battle_RM_SingleSword

### 3. **Run Forward → Attack States** ❌
**Missing:** Transitions from Run Forward to Attack_1, Attack_2, Attack_3
**Should have:** Run Forward can be interrupted by attack triggers

### 4. **Speed-Based Transitions** ❌
**Missing:** No transitions using the `Speed` parameter
**The script sets Speed (0-1 range), but no transitions use it!**

## Current Transition Map

```
Idle:
  → Attack_1 (on Attack_1 trigger) ✅
  → Attack_2 (on Attack_2 trigger) ✅
  → Attack_3 (on Attack_3 trigger) ✅
  → Run Forward (on Run trigger) ❌ WRONG - should be Run bool = true
  → Use Potion (on UseItem trigger) ✅
  → DashFWD (on Combo trigger) ❌ WRONG - should be Dash trigger

Run Forward:
  → Idle (on Run = false) ✅

Attack_1:
  → Attack_2 (on Attack_2 trigger) ✅
  → Idle (on exit time) ✅

Attack_2:
  → Idle (on exit time) ✅

Attack_3:
  → Idle (on exit time) ✅
```

## Required Fixes

### Fix 1: Idle → Run Forward Transition
**Current transition uses:** `Run` trigger
**Should use:** `Run` bool = true
**Also add:** `Speed > 0.1` as alternative/additional condition

### Fix 2: Idle → Dash Transition
**Add new transition:** Idle → DashFWD_Battle_RM_SingleSword
**Condition:** `Dash` trigger
**Has Exit Time:** No
**Transition Duration:** 0.1s

### Fix 3: Run Forward → Attack Transitions
**Add transitions:**
- Run Forward → Attack_1 (on Attack_1 trigger)
- Run Forward → Attack_2 (on Attack_2 trigger)  
- Run Forward → Attack_3 (on Attack_3 trigger)
**Has Exit Time:** No (to allow interruptions)

### Fix 4: Speed Parameter Usage (Optional)
Consider adding transitions that use `Speed` parameter:
- Idle → Run Forward: `Speed > 0.1` AND `Run = true`
- Run Forward → Idle: `Speed < 0.1` OR `Run = false`

## Summary

**Critical Issues:**
1. ❌ Idle → Run uses wrong condition type (trigger vs bool)
2. ❌ Missing Idle → Dash transition (has Combo instead)
3. ❌ Run Forward cannot be interrupted by attacks

**Recommended Fixes:**
1. Change Idle → Run Forward to use `Run` bool = true
2. Add Idle → DashFWD transition using `Dash` trigger
3. Add Run Forward → Attack_1/2/3 transitions

