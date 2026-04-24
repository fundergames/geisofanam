# Funder Core Reorganization Summary

## Menu Structure Changes

I've successfully consolidated all menu items under a unified `Funder` menu structure:

### **Funder/Core** - Generic Systems (Reusable)
```
Funder/
├── Core/
│   ├── Bootstrap/
│   │   ├── Create Bootstrap Config
│   │   └── Validate Bootstrap Config
│   ├── EventBus/
│   │   └── Run Smoke Test
│   ├── Feature Flags/
│   │   └── Create Or Select Config
│   ├── Logging/
│   │   ├── Create Default Log Config
│   │   ├── Open Log Directory
│   │   ├── Show Log Path in Console
│   │   └── Clear Log Files
│   ├── Random/
│   │   ├── Create Default Config
│   │   ├── Run Determinism Test
│   │   ├── Run Stream Independence Test
│   │   └── Run Weighted Pick Test
│   ├── Time/
│   │   ├── Toggle Pause Gameplay
│   │   ├── Slow Motion (0.5x)
│   │   └── Reset Time Scale
│   ├── UI/
│   │   └── Create Diagnostic HUD Prefab
│   ├── Game Flow/
│   │   ├── Flow Tester
│   │   ├── Create All Scenes
│   │   ├── Create Entry Scene
│   │   ├── Create Splash Scene
│   │   ├── Create Login Scene
│   │   ├── Create Main Menu Scene
│   │   └── Create Game Lobby Scene
│   └── Build Settings Helper
```

### **Funder/Rogue Deal** - Game-Specific
```
Funder/
└── Rogue Deal/
    ├── Create Example Data
    ├── Analytics/
    │   └── Update Analytics Listener in Bootstrap
    └── Migration/
        ├── 1. Update EventBus to EventBusService
        ├── 2. Verify EventBus Configuration
        ├── 3. Open Migration Guide
        ├── 4. Remove Legacy EventBus Code
        ├── 5. Verify All Files
        └── 6. Remove Example Service from Config
```

## Current Project Structure

### Generic Funder Core Systems (Can be packaged)
Located in `/Assets/FunderCore/`:

```
/Assets/FunderCore/
├── Editor/
│   ├── Systems/FeatureFlags/      # Feature flag utilities
│   └── UI/                         # Diagnostic HUD builder
├── Scripts/Systems/
│   ├── Analytics/                  # Analytics service interfaces
│   ├── Audio/                      # Audio system
│   ├── Core/                       # Core service locator & bootstrap
│   ├── FeatureFlags/               # Feature toggle system
│   ├── Input/                      # Input handling
│   ├── Logging/                    # Logging system
│   ├── Random/                     # Deterministic random system
│   └── Time/                       # Time management
├── Systems/
│   ├── EventBus/                   # Modern event bus system
│   ├── FSM/                        # Finite state machine
│   ├── Logging/                    # Structured logging
│   ├── Random/                     # Random number generation
│   └── Time/                       # Time utilities
├── Prefabs/
└── Resources/
```

### Rogue Deal Specific (Game implementation)
Located in `/Assets/RogueDeal/`:

```
/Assets/RogueDeal/
├── Scripts/
│   ├── Analytics/                  # Game-specific analytics listener
│   ├── Combat/                     # Combat system implementation
│   ├── Crafting/                   # Crafting system
│   ├── Editor/                     # Game-specific editor tools
│   ├── Enemies/                    # Enemy definitions
│   ├── Events/                     # Game-specific events
│   ├── Flow/                       # Game flow controllers
│   ├── Items/                      # Item system
│   ├── Levels/                     # Level definitions
│   ├── Player/                     # Player character
│   └── UI/                         # UI components
├── Prefabs/
├── Resources/
├── Scenes/
└── Sprites/
```

## Recommended Next Steps for Package Conversion

To convert FunderCore into a Unity Package Manager package, you'll need to:

### 1. **Create Package Structure**
The existing package at `/Packages/com.funder.core/` should mirror the structure you want. Move content from `/Assets/FunderCore/` to this package:

```
/Packages/com.funder.core/
├── package.json                    # Package manifest
├── README.md                       # Package documentation
├── CHANGELOG.md                    # Version history
├── Editor/
│   ├── Systems/
│   └── UI/
├── Runtime/
│   ├── Analytics/
│   ├── Bootstrap/
│   ├── EventBus/
│   ├── FSM/
│   ├── Logging/
│   ├── Random/
│   └── Time/
├── Tests/
│   ├── Editor/
│   └── Runtime/
└── Samples~/                       # Optional samples
```

### 2. **Assembly Definitions**
Ensure proper `.asmdef` files exist for:
- `Funder.Core` (Runtime)
- `Funder.Core.Editor` (Editor)
- `Funder.Core.Tests` (Tests)

### 3. **Package Manifest (package.json)**
Update `/Packages/com.funder.core/package.json`:

```json
{
  "name": "com.funder.core",
  "version": "1.0.0",
  "displayName": "Funder Core",
  "description": "Core framework systems for game development including EventBus, FSM, Logging, Random, Time, and Bootstrap",
  "unity": "6000.0",
  "dependencies": {},
  "keywords": [
    "framework",
    "eventbus",
    "fsm",
    "logging",
    "bootstrap"
  ],
  "author": {
    "name": "Your Name",
    "email": "your@email.com"
  }
}
```

### 4. **Move Files to Package**
Since I cannot move/rename files, here's what you should do manually:

1. **Copy Runtime Systems** from `/Assets/FunderCore/` to `/Packages/com.funder.core/Runtime/`
2. **Copy Editor Tools** from `/Assets/FunderCore/Editor/` to `/Packages/com.funder.core/Editor/`
3. **Copy Tests** from `/Assets/FunderCore/Systems/*/Tests/` to `/Packages/com.funder.core/Tests/`
4. **Update References** in RogueDeal scripts to use the package namespace

### 5. **RogueDeal as Package Consumer**
After moving FunderCore to package:

- `/Assets/RogueDeal/` remains as game-specific implementation
- Implements interfaces from `com.funder.core`
- Uses services via dependency injection
- Follows same folder structure for implementations (e.g., Analytics listener in `/Assets/RogueDeal/Scripts/Analytics/`)

## Interface Implementation Pattern

When RogueDeal implements FunderCore interfaces, follow this pattern:

**FunderCore Interface Location:**
```
/Packages/com.funder.core/Runtime/Analytics/IAnalyticsEventListener.cs
```

**RogueDeal Implementation Location:**
```
/Assets/RogueDeal/Scripts/Analytics/RogueDealAnalyticsListener.cs
```

This maintains clear separation and makes it obvious which code is generic vs game-specific.

## Benefits of This Organization

1. **Clear Separation**: Generic framework code vs game-specific implementation
2. **Reusability**: FunderCore can be used across multiple projects
3. **Version Control**: Package can be versioned independently
4. **Team Collaboration**: Different teams can work on framework vs game
5. **Testing**: Easier to test framework in isolation
6. **Maintenance**: Updates to core systems don't require game code changes

## Updated Menu Items

All menu items have been updated to follow the new structure:
- **Before**: Scattered across `Tools/FunderCore/`, `Funder.Core/`, `RogueDeal/`, etc.
- **After**: Unified under `Funder/Core/` and `Funder/Rogue Deal/`

This makes it immediately clear which tools are generic framework features and which are game-specific.

## What Hasn't Changed (Yet)

The physical file locations remain the same for now. You'll need to manually:
1. Move files from `/Assets/FunderCore/` to `/Packages/com.funder.core/`
2. Update assembly references in both locations
3. Update namespace imports in RogueDeal scripts
4. Test thoroughly to ensure all references are correct

Once you're ready to make that move, the menu structure and organization principles are already in place to support it!
