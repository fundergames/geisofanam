# ✅ Auto-Start Feature Added!

I've added a new feature to make level buttons **directly start combat** when clicked!

---

## 🎯 What Changed

**File:** `/Assets/RogueDeal/Scripts/UI/StageSelectController.cs`

**New Field Added:**
```csharp
[Header("Behavior")]
[SerializeField]
[Tooltip("If enabled, clicking a level button immediately starts combat")]
private bool autoStartOnLevelSelect = false;
```

**Updated Logic:**
```csharp
private void SelectLevel(LevelDefinition level)
{
    _selectedLevel = level;
    LevelManager.Instance.SelectLevel(level);
    UpdateLevelInfo();
    UpdateButtonStates();

    // NEW: Auto-start if enabled!
    if (autoStartOnLevelSelect && LevelManager.Instance.IsLevelUnlocked(level))
    {
        Debug.Log($"[StageSelect] Auto-starting level: {level.displayName}");
        StartLevel(level);  // ← Immediately loads combat!
    }
}
```

---

## 🎮 How to Use

### Enable Auto-Start (For Testing)

1. **Find the StageSelect object:**
   - In Play Mode, or
   - Find the prefab: `/Assets/RogueDeal/Resources/UI/RogueDeal_StageSelect.prefab`

2. **Select the root GameObject** with `StageSelectController` component

3. **In Inspector, find** the "Behavior" section

4. **Check the box:** "Auto Start On Level Select"

5. **Done!** Now clicking "1-1" will immediately start combat ✅

---

## 📊 Behavior Comparison

### Before (Default)
```
Click "1-1" button
  ↓
Level info updates
  ↓
Click "START" button
  ↓
Combat loads
```

### After (Auto-Start Enabled)
```
Click "1-1" button
  ↓
Combat loads immediately! ✅
```

---

## 🔍 Current Setting

**Default:** `autoStartOnLevelSelect = false`

**This means:**
- ✅ Original behavior preserved
- ✅ You still need to click "Start" button
- ✅ Can review level info before starting

**To enable auto-start:**
- Open the StageSelect prefab/object
- Check the "Auto Start On Level Select" box
- Save

---

## ⚡ Quick Enable Guide

### Option 1: Enable in Prefab (Permanent)

1. Navigate to: `/Assets/RogueDeal/Resources/UI/RogueDeal_StageSelect.prefab`
2. Select the root GameObject
3. Find `StageSelectController` component
4. Check "Auto Start On Level Select"
5. Save prefab

**Result:** Auto-start enabled for all scenes using this prefab

---

### Option 2: Enable in Scene (Temporary)

1. Enter Play Mode
2. Find "StageSelect" or similar in Hierarchy
3. Select it
4. In Inspector, check "Auto Start On Level Select"
5. Test by clicking a level

**Result:** Auto-start enabled for this play session only

---

## 🛡️ Safety Features

**The code includes safety checks:**

✅ **Locked levels won't auto-start**
```csharp
if (autoStartOnLevelSelect && LevelManager.Instance.IsLevelUnlocked(level))
```

✅ **Console logging**
```csharp
Debug.Log($"[StageSelect] Auto-starting level: {level.displayName}");
```

✅ **Null checks from original code**
```csharp
if (_selectedLevel == null) return;
```

---

## 🐛 Troubleshooting

### Issue: Still need to click Start button
**Cause:** Auto-start not enabled  
**Fix:** Check the checkbox in Inspector

### Issue: Level selected but nothing happens
**Cause:** Level might be locked  
**Fix:** Check Console for unlock status, or check LevelManager

### Issue: Can't find the checkbox
**Cause:** Looking at wrong GameObject  
**Fix:** Must be on GameObject with `StageSelectController` component

### Issue: Checkbox doesn't save
**Cause:** Changed in Play Mode (temporary)  
**Fix:** Edit the prefab directly, not during Play Mode

---

## 📝 Testing Steps

### Test 1: Verify Default Behavior (Auto-Start OFF)
1. Make sure `autoStartOnLevelSelect = false`
2. Enter Play Mode
3. Click "1-1"
4. ✅ Level info should update
5. ❌ Combat should NOT start yet
6. Click "Start" button
7. ✅ Combat should start now

### Test 2: Verify Auto-Start (Auto-Start ON)
1. Set `autoStartOnLevelSelect = true`
2. Enter Play Mode
3. Click "1-1"
4. ✅ Combat should start immediately!

### Test 3: Verify Locked Level Protection
1. Set `autoStartOnLevelSelect = true`
2. Lock a level (if possible)
3. Click locked level
4. ✅ Should select but NOT start
5. ❌ Should show locked overlay

---

## 🎯 Recommended Settings

**For Development/Testing:**
```
autoStartOnLevelSelect = true  ← Faster testing
```

**For Production/Release:**
```
autoStartOnLevelSelect = false  ← Players can review level info
```

---

## 🔧 Additional Notes

**Compatible with:**
- ✅ Level locking system
- ✅ Star tracking
- ✅ Energy costs
- ✅ Original Start button (still works!)

**Performance:**
- ✅ No overhead when disabled
- ✅ Same scene loading as manual start
- ✅ No additional allocations

**UI:**
- ✅ Doesn't break existing UI
- ✅ Start button still functional
- ✅ Level info still updates

---

## 📋 Summary

**What I did:**
1. ✅ Added `autoStartOnLevelSelect` boolean field
2. ✅ Updated `SelectLevel()` method with auto-start logic
3. ✅ Added safety check for locked levels
4. ✅ Added console logging
5. ✅ Made it Inspector-editable

**What you need to do:**
1. Find StageSelect prefab or scene object
2. Check "Auto Start On Level Select" checkbox
3. Test by clicking a level button

**Result:**
- One-click combat start for faster testing! 🚀
- Toggle-able for different workflows
- Safe with locked level protection

---

## ✅ Ready to Test!

Your game now has **both** workflows:

**Workflow 1: Original (Auto-Start OFF)**
- Click level → Review info → Click Start → Combat

**Workflow 2: Quick Test (Auto-Start ON)**
- Click level → Combat immediately!

**Choose based on your needs!** 🎮
