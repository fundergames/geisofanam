# 🎮 How to Start Combat from Level Select

## Current UI Flow

When you click a world level button (e.g., "1-1"), here's what happens:

### Step 1: Click Level Button (1-1)
- ✅ Selects the level
- ✅ Shows level info (name, description, energy cost)
- ✅ Updates star display
- ⚠️ **Does NOT start combat yet**

### Step 2: Click "Start" Button
- ✅ Validates level is unlocked
- ✅ Hides the panel
- ✅ Loads Combat scene
- ✅ Starts the fight!

---

## 🔍 Current Behavior

**What You See:**
```
[UI] Click "1-1" button
  ↓
[Console] "[LevelManager] Selected level: Level 1 (1-1)"
  ↓
[UI] Level info appears on the right
  ↓
[UI] Click "START" button (separate button!)
  ↓
[Console] "[StageSelect] Starting level: Level 1"
[FLOW] Loading Combat scene...
  ↓
Combat scene loads!
```

---

## ❓ Is There a "Start" Button?

**Check your UI:**
1. Look for a button labeled **"Start"**, **"Begin"**, or **"Enter Combat"**
2. It should be separate from the level grid
3. Usually at the bottom or right side of the panel
4. Should light up/become clickable after selecting a level

**If you don't see it:**
- The button might be outside the visible area
- Check if the UI panel needs to be scrolled
- The button might be inactive (grayed out) if level is locked

---

## 🐛 Troubleshooting

### Issue: "I click 1-1 but nothing happens"
**Expected Behavior:** Level info should update, but combat doesn't start yet  
**Solution:** Look for and click the "Start" button

### Issue: "I don't see a Start button"
**Possible Causes:**
1. UI Canvas scale might be wrong
2. Button is outside camera view
3. Button is hidden/inactive

**Debug Steps:**
1. Enter Play Mode
2. Click a level button (1-1)
3. Check Console for: `"[LevelManager] Selected level: Level 1 (1-1)"`
4. If you see that, the selection works!
5. Now find the Start button in the UI

### Issue: "Start button is grayed out"
**Cause:** Level might be locked  
**Solution:** Check level unlock conditions in LevelManager

---

## 🎯 Quick Fix: Make Level Buttons Start Combat Directly

If you want clicking "1-1" to **immediately** start combat without a separate Start button:

### Option A: Modify StageSelectController (Quick)

I can update `StageSelectController.cs` to add a toggle that makes level buttons directly start combat when clicked.

**Pros:**
- ✅ Faster for testing
- ✅ Less clicks needed
- ✅ Can still keep original behavior

**Cons:**
- ⚠️ Skips level info review
- ⚠️ Might start locked levels by accident

### Option B: Modify LevelButtonUI (Direct)

I can update the individual level buttons to have an option to "auto-start" when clicked.

**Pros:**
- ✅ Per-button control
- ✅ More flexible

**Cons:**
- ⚠️ More complex

---

## 🛠️ Implementation: Direct Start on Click

Would you like me to add a feature where clicking a level button directly starts combat?

**What I'll do:**
1. Add a `bool autoStartOnSelect` option to `StageSelectController`
2. When `true`, selecting a level immediately calls `StartLevel()`
3. When `false`, keeps current behavior (select then click Start)
4. You can toggle it in Inspector

**Code changes:**
- `StageSelectController.cs` - Add auto-start logic
- Inspector checkbox to enable/disable

---

## 📋 Current Code Flow

**File:** `/Assets/RogueDeal/Scripts/UI/StageSelectController.cs`

**When you click a level button:**
```csharp
// Line 163 - Button callback
buttonUI.Initialize(level, isUnlocked, stars, () => SelectLevel(level));

// Line 186 - SelectLevel method
private void SelectLevel(LevelDefinition level)
{
    _selectedLevel = level;
    LevelManager.Instance.SelectLevel(level);  // ← Just selects!
    UpdateLevelInfo();  // ← Updates UI
    UpdateButtonStates();  // ← Highlights button
    // ❌ Does NOT call StartLevel() here
}
```

**When you click the Start button:**
```csharp
// Line 290 - Start button callback
private void OnStartLevelClicked()
{
    if (_selectedLevel == null) return;
    if (!LevelManager.Instance.IsLevelUnlocked(_selectedLevel)) return;
    
    Debug.Log($"[StageSelect] Starting level: {_selectedLevel.displayName}");
    StartLevel(_selectedLevel);  // ← This loads combat!
}

// Line 308 - StartLevel method
private async void StartLevel(LevelDefinition level)
{
    await PanelManager.Instance.HideCurrentPanel();
    FGFlowExtensions.StartCombatWithLoading();  // ← Loads scene!
}
```

---

## ✅ Quick Answer

**Q: Why doesn't clicking 1-1 start combat?**  
**A:** It's a two-step process:
1. Click level button to **select** it
2. Click separate "Start" button to **begin** combat

**Q: How do I fix this?**  
**A:** Either:
1. Find and click the "Start" button (check bottom/right of UI)
2. Let me add an "auto-start" feature so one click is enough

---

## 🎮 Recommended Next Steps

**For Testing:**
1. Enter Play Mode
2. Click a level button (1-1)
3. Look for "Start" button in UI
4. Click "Start" to begin combat

**For Development:**
1. Let me know if you want auto-start feature
2. I can add it in 2 minutes
3. You'll have a checkbox to toggle behavior

**Need help?** Let me know:
- Can't find Start button?
- Want direct-start feature?
- Something else not working?
