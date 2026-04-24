# Funder Games Menu Structure - Quick Reference

## Before Reorganization ❌

Menu items were scattered across multiple root menus:

```
Unity Editor Menu Bar:
│
├── Tools/
│   ├── FunderCore/                    ❌ Inconsistent naming
│   │   ├── Create Bootstrap Config
│   │   ├── Validate Bootstrap Config
│   │   └── EventBus/
│   │       └── Run Smoke Test
│   ├── Funder/                        ❌ Different root
│   │   └── Random/
│   │       ├── Create Default Config
│   │       └── Run Tests...
│   └── FunderCore/                    ❌ Duplicate naming
│       └── Game Flow/
│           └── ...
│
├── Funder.Core/                       ❌ Mixed naming conventions
│   ├── Feature Flags/
│   │   └── Create Or Select Config
│   ├── Logging/
│   │   └── ...
│   └── UI/
│       └── Create Diagnostic HUD Prefab
│
├── Funder/                            ❌ Another root location
│   └── Time/
│       ├── Toggle Pause Gameplay
│       └── ...
│
├── RogueDeal/                         ❌ Separate from framework
│   ├── Create Example Data
│   ├── Analytics/
│   │   └── ...
│   └── Migration/
│       └── ...
│
└── FunderCore/                        ❌ Yet another location
    └── Build Settings Helper
```

**Problems:**
- 5+ different root menu locations
- Inconsistent naming (FunderCore vs Funder.Core vs Funder)
- No clear separation between generic and game-specific
- Hard to find related functionality
- Confusing for new team members

---

## After Reorganization ✅

All menu items now under one unified `Funder Games` menu:

```
Unity Editor Menu Bar:
│
└── Funder Games/                      ✅ Single unified root
    │
    ├── Core/                          ✅ Generic framework systems
    │   │
    │   ├── Analytics/                 # Analytics validation
    │   │   └── Validate Events
    │   │
    │   ├── Bootstrap/                 # Service initialization
    │   │   ├── Create Bootstrap Config
    │   │   └── Validate Bootstrap Config
    │   │
    │   ├── EventBus/                  # Event system
    │   │   ├── Run Smoke Test
    │   │   ├── Diagnostics Window
    │   │   └── Run Simple Smoke Test
    │   │
    │   ├── Feature Flags/             # Feature toggles
    │   │   └── Create Or Select Config
    │   │
    │   ├── Flow/                      # Application flow
    │   │   ├── Create AppConfig
    │   │   └── Open Entry Scene
    │   │
    │   ├── FSM/                       # Finite State Machine
    │   │   └── Run Smoke Test
    │   │
    │   ├── Game Flow/                 # Scene flow system
    │   │   ├── Flow Tester
    │   │   ├── Create All Scenes
    │   │   ├── Create Entry Scene
    │   │   ├── Create Splash Scene
    │   │   ├── Create Login Scene
    │   │   ├── Create Main Menu Scene
    │   │   └── Create Game Lobby Scene
    │   │
    │   ├── Logging/                   # Logging system
    │   │   ├── Create Default Log Config
    │   │   ├── Open Log Directory
    │   │   ├── Show Log Path in Console
    │   │   ├── Clear Log Files
    │   │   └── Log Viewer
    │   │
    │   ├── Random/                    # Random number generation
    │   │   ├── Create Default Config
    │   │   ├── Run Determinism Test
    │   │   ├── Run Stream Independence Test
    │   │   └── Run Weighted Pick Test
    │   │
    │   ├── Time/                      # Time management
    │   │   ├── Toggle Pause Gameplay (%&p)
    │   │   ├── Slow Motion (0.5x) (%&s)
    │   │   └── Reset Time Scale (%&r)
    │   │
    │   ├── Tools/                     # Development tools
    │   │   └── Fix Script Execution Order
    │   │
    │   ├── UI/                        # UI utilities
    │   │   └── Create Diagnostic HUD Prefab
    │   │
    │   └── Build Settings Helper      # Build configuration
    │
    └── Rogue Deal/                    ✅ Game-specific tools
        │
        ├── Create Example Data        # Game data creation
        ├── Setup Combat Scene         # Combat scene setup
        ├── Setup GameLobby Scene      # GameLobby scene setup
        │
        ├── Analytics/                 # Game analytics
        │   └── Update Analytics Listener in Bootstrap
        │
        └── Migration/                 # Migration utilities
            ├── 1. Update EventBus to EventBusService
            ├── 2. Verify EventBus Configuration
            ├── 3. Open Migration Guide
            ├── 4. Remove Legacy EventBus Code
            ├── 5. Verify All Files
            └── 6. Remove Example Service from Config
```

**Benefits:**
- ✅ Single `Funder Games` menu root - easy to find
- ✅ Clear separation: `Core` (framework) vs `Rogue Deal` (game)
- ✅ Consistent naming convention throughout
- ✅ Logical grouping by system/feature
- ✅ Alphabetically organized within categories
- ✅ Numbered migration steps for clarity

---

## Menu Usage Guide

### For Framework Development
Use `Funder Games/Core/` menus when working on:
- Generic systems that work in any game
- Framework features to be shared across projects
- Core infrastructure (Bootstrap, EventBus, etc.)
- Reusable utilities (Logging, Random, Time)

### For Game Development
Use `Funder Games/Rogue Deal/` menus when working on:
- Game-specific data and content
- RogueDeal implementations of Core interfaces
- Game analytics and telemetry
- Project-specific migration/cleanup tasks

---

## Keyboard Shortcuts

All time-related shortcuts remain active:
- `Ctrl+Alt+P` (⌘⌥P on Mac) - Toggle Pause Gameplay
- `Ctrl+Alt+S` (⌘⌥S on Mac) - Slow Motion (0.5x)
- `Ctrl+Alt+R` (⌘⌥R on Mac) - Reset Time Scale

---

## Quick Access Patterns

### Creating Configs
```
Funder Games/Core/Bootstrap/Create Bootstrap Config     # Service registration
Funder Games/Core/Feature Flags/Create Or Select Config # Feature toggles
Funder Games/Core/Logging/Create Default Log Config     # Logging setup
Funder Games/Core/Random/Create Default Config          # Random system
Funder Games/Core/Flow/Create AppConfig                 # App flow config
```

### Running Tests
```
Funder Games/Core/EventBus/Run Smoke Test               # Event system test
Funder Games/Core/Random/Run Determinism Test           # Random test
Funder Games/Core/Random/Run Stream Independence Test   # Random test
Funder Games/Core/Random/Run Weighted Pick Test         # Random test
Funder Games/Core/FSM/Run Smoke Test                    # FSM test
```

### Debugging Tools
```
Funder Games/Core/Logging/Open Log Directory            # View logs
Funder Games/Core/Logging/Log Viewer                    # View log window
Funder Games/Core/Logging/Show Log Path in Console      # Get log path
Funder Games/Core/UI/Create Diagnostic HUD Prefab       # Debug overlay
Funder Games/Core/EventBus/Diagnostics Window           # EventBus debug
```

### Game Flow Development
```
Funder Games/Core/Game Flow/Flow Tester                 # Test scene flow
Funder Games/Core/Game Flow/Create All Scenes           # Quick scene setup
Funder Games/Core/Build Settings Helper                 # Build configuration
```

### Game-Specific Tools
```
Funder Games/Rogue Deal/Create Example Data             # Generate test data
Funder Games/Rogue Deal/Analytics/...                   # Analytics config
Funder Games/Rogue Deal/Migration/...                   # Migration tools
```

---

## Migration from Old Menu Structure

If you have bookmarks or documentation referencing old menu paths:

| Old Path | New Path |
|----------|----------|
| `Tools/FunderCore/Create Bootstrap Config` | `Funder Games/Core/Bootstrap/Create Bootstrap Config` |
| `Tools/FunderCore/EventBus/Run Smoke Test` | `Funder Games/Core/EventBus/Run Smoke Test` |
| `Tools/Funder/Core/EventBus/Diagnostics Window` | `Funder Games/Core/EventBus/Diagnostics Window` |
| `Tools/Funder/Core/EventBus/Run Simple Smoke Test` | `Funder Games/Core/EventBus/Run Simple Smoke Test` |
| `Tools/Funder/Core/Fix Script Execution Order` | `Funder Games/Core/Tools/Fix Script Execution Order` |
| `Funder.Core/Feature Flags/...` | `Funder Games/Core/Feature Flags/...` |
| `Funder.Core/Logging/...` | `Funder Games/Core/Logging/...` |
| `Funder.Core/Logging/Log Viewer` | `Funder Games/Core/Logging/Log Viewer` |
| `Funder.Core/UI/...` | `Funder Games/Core/UI/...` |
| `Funder Core/FSM/...` | `Funder Games/Core/FSM/...` |
| `Funder/Time/...` | `Funder Games/Core/Time/...` |
| `Funder/Core/Analytics/...` | `Funder Games/Core/Analytics/...` |
| `Funder/Core/Flow/...` | `Funder Games/Core/Flow/...` |
| `Funder/Core/Random/...` | `Funder Games/Core/Random/...` |
| `Funder/Core/Game Flow/...` | `Funder Games/Core/Game Flow/...` |
| `Funder/Core/Build Settings Helper` | `Funder Games/Core/Build Settings Helper` |
| `RogueDeal/Create Example Data` | `Funder Games/Rogue Deal/Create Example Data` |
| `RogueDeal/Setup Combat Scene` | `Funder Games/Rogue Deal/Setup Combat Scene` |
| `RogueDeal/Setup GameLobby Scene` | `Funder Games/Rogue Deal/Setup GameLobby Scene` |
| `RogueDeal/Analytics/...` | `Funder Games/Rogue Deal/Analytics/...` |
| `RogueDeal/Migration/...` | `Funder Games/Rogue Deal/Migration/...` |

---

## Files Updated

The following files have been modified to implement this menu structure:

### Core Menu Items (Package)
- `/Packages/com.funder.core/Editor/Analytics/FGAnalyticsValidator.cs`
- `/Packages/com.funder.core/Editor/Bootstrap/CoreMenuItems.cs`
- `/Packages/com.funder.core/Editor/Bootstrap/GameBootstrapExecutionOrder.cs`
- `/Packages/com.funder.core/Editor/EventBus/EventBusMenuItems.cs`
- `/Packages/com.funder.core/Editor/EventBus/EventBusDiagnosticsWindow.cs`
- `/Packages/com.funder.core/Editor/EventBus/EventBusSmokeTest.cs`
- `/Packages/com.funder.core/Editor/FeatureFlags/FeatureFlagsAssetUtility.cs`
- `/Packages/com.funder.core/Editor/Flow/FGAppConfigMenu.cs`
- `/Packages/com.funder.core/Editor/FSM/FSMMenus.cs`
- `/Packages/com.funder.core/Editor/Logging/LoggingMenuItems.cs`
- `/Packages/com.funder.core/Editor/Logging/LoggingWindow.cs`
- `/Packages/com.funder.core/Editor/Random/RandomMenuItems.cs`
- `/Packages/com.funder.core/Editor/Time/TimeMenuItems.cs`
- `/Packages/com.funder.core/Editor/UI/DiagnosticHudBuilder.cs`

### Core Menu Items (Assets)
- `/Assets/RogueDeal/Scripts/Flow/Editor/BuildSettingsHelper.cs`
- `/Assets/RogueDeal/Scripts/Flow/Editor/GameFlowSceneCreator.cs`
- `/Assets/RogueDeal/Scripts/Flow/Editor/GameFlowTester.cs`

### Rogue Deal Menu Items
- `/Assets/RogueDeal/Scripts/Editor/CleanupBootstrapConfig.cs`
- `/Assets/RogueDeal/Scripts/Editor/CleanupLegacyEventBus.cs`
- `/Assets/RogueDeal/Scripts/Editor/CombatSceneSetupHelper.cs`
- `/Assets/RogueDeal/Scripts/Editor/GameDataCreator.cs`
- `/Assets/RogueDeal/Scripts/Editor/GameLobbySceneSetupHelper.cs`
- `/Assets/RogueDeal/Scripts/Editor/MigrateToEventBusService.cs`
- `/Assets/RogueDeal/Scripts/Editor/UpdateAnalyticsListenerInBootstrap.cs`
- `/Assets/RogueDeal/Scripts/Editor/VerifyEventBusMigration.cs`

---

## Consistency Rules Applied

1. **Single Root Menu**: Everything under `Funder Games/`
2. **Two Main Categories**: `Core/` (framework) and `Rogue Deal/` (game-specific)
3. **System Grouping**: Related features grouped in submenus (Analytics, Bootstrap, EventBus, etc.)
4. **Verb-Noun Pattern**: Menu items use clear action names (Create, Run, Open, Validate, etc.)
5. **Descriptive Names**: Full descriptive text instead of abbreviations
6. **Priority Order**: Related items grouped with priority values for consistent ordering
7. **No Duplication**: Each menu path is unique and unambiguous
8. **Alphabetical Organization**: Subsystems organized alphabetically within Core

---

## Next Steps

1. ✅ **Menu Consolidation** - COMPLETE (Changed from `Funder` to `Funder Games`)
2. ✅ **All MenuItem Attributes Updated** - 30+ menu items updated across packages and assets
3. ⏳ **File Organization** - Ready to migrate to package structure
4. ⏳ **Package Creation** - Use `PACKAGE_MIGRATION_CHECKLIST.md`
5. ⏳ **Testing** - Verify all functionality after migration
6. ⏳ **Documentation** - Update team documentation with new menu paths

---

**Last Updated**: December 2024
**Unity Version**: 6000.0
**Project**: Rogue Deal
**Framework**: Funder Core
**Menu Root**: Funder Games (updated from Funder)
