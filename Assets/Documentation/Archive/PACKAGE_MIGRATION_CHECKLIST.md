# Funder Core Package Migration Checklist

## Overview
This checklist helps you migrate FunderCore from `/Assets/FunderCore/` to a proper Unity Package Manager package at `/Packages/com.funder.core/`.

## Phase 1: Preparation ✓ (COMPLETED)

- [x] Consolidated menu items under `Funder/Core/` and `Funder/Rogue Deal/`
- [x] Identified generic vs game-specific code
- [x] Documented folder structure and organization principles

## Phase 2: Package Structure Setup

### 2.1 Create Package Directories
```bash
# In /Packages/com.funder.core/
mkdir -p Runtime/Analytics
mkdir -p Runtime/Bootstrap
mkdir -p Runtime/EventBus
mkdir -p Runtime/FSM
mkdir -p Runtime/Logging
mkdir -p Runtime/Random
mkdir -p Runtime/Time
mkdir -p Runtime/Audio
mkdir -p Runtime/FeatureFlags
mkdir -p Editor/Analytics
mkdir -p Editor/Bootstrap
mkdir -p Editor/EventBus
mkdir -p Editor/FSM
mkdir -p Editor/Logging
mkdir -p Editor/Random
mkdir -p Editor/Time
mkdir -p Editor/UI
mkdir -p Tests/Editor
mkdir -p Tests/Runtime
mkdir -p Samples~/Analytics
mkdir -p Samples~/Flow
```

### 2.2 Create Assembly Definition Files

**Runtime Assembly** (`Runtime/Funder.Core.asmdef`):
```json
{
    "name": "Funder.Core",
    "rootNamespace": "Funder.Core",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

**Editor Assembly** (`Editor/Funder.Core.Editor.asmdef`):
```json
{
    "name": "Funder.Core.Editor",
    "rootNamespace": "Funder.Core.Editor",
    "references": ["Funder.Core"],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

**Tests Assembly** (`Tests/Funder.Core.Tests.asmdef`):
```json
{
    "name": "Funder.Core.Tests",
    "rootNamespace": "Funder.Core.Tests",
    "references": [
        "Funder.Core",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

### 2.3 Update package.json

Located at `/Packages/com.funder.core/package.json`:
```json
{
  "name": "com.funder.core",
  "version": "1.0.0",
  "displayName": "Funder Core",
  "description": "Core framework systems including EventBus, FSM, Logging, Random, Time, and Bootstrap for Unity game development",
  "unity": "6000.0",
  "unityRelease": "0f1",
  "keywords": [
    "framework",
    "eventbus",
    "fsm",
    "state machine",
    "logging",
    "bootstrap",
    "service locator",
    "random",
    "time",
    "analytics"
  ],
  "author": {
    "name": "Your Name",
    "email": "your@email.com",
    "url": "https://your-website.com"
  },
  "dependencies": {},
  "samples": [
    {
      "displayName": "Analytics Sample",
      "description": "Example implementation of analytics system",
      "path": "Samples~/Analytics"
    },
    {
      "displayName": "Flow Sample",
      "description": "Example game flow implementation",
      "path": "Samples~/Flow"
    }
  ]
}
```

## Phase 3: File Migration

### 3.1 Runtime Scripts Migration

Move from `/Assets/FunderCore/` to `/Packages/com.funder.core/Runtime/`:

- [x] **Analytics System**
  - [x] `/Scripts/Systems/Analytics/` → `Runtime/Analytics/`
  - [x] Interfaces: `IAnalyticsService`, `IAnalyticsEventListener`
  - [x] Implementation: `AnalyticsService`

- [x] **Bootstrap System**
  - [x] `/Scripts/Systems/Core/Runtime/` → `Runtime/Bootstrap/`
  - [x] `GameBootstrap.cs`
  - [x] `BootstrapConfig.cs`
  - [x] `ServiceLocator.cs`
  - [x] `ServiceRegistryEntry.cs`

- [x] **EventBus System**
  - [x] `/Systems/EventBus/Runtime/` → `Runtime/EventBus/`
  - [x] `IEventBus.cs`
  - [x] `EventBusService.cs`
  - [x] All event examples

- [x] **FSM System**
  - [x] `/Systems/FSM/Runtime/` → `Runtime/FSM/`
  - [x] Complete state machine framework

- [x] **Logging System**
  - [x] `/Systems/Logging/Runtime/` → `Runtime/Logging/`
  - [x] Interfaces, filters, sinks

- [x] **Random System**
  - [x] `/Systems/Random/Runtime/` → `Runtime/Random/`
  - [x] `RandomHub`, `RandomStream`, `WeightedTable`

- [x] **Time System**
  - [x] `/Systems/Time/Runtime/` → `Runtime/Time/`
  - [x] Timers, coroutines, time management

- [x] **Audio System**
  - [x] `/Scripts/Systems/Audio/` → `Runtime/Audio/`

- [x] **Feature Flags**
  - [x] `/Scripts/Systems/FeatureFlags/` → `Runtime/FeatureFlags/`

### 3.2 Editor Scripts Migration

Move from `/Assets/FunderCore/Editor/` to `/Packages/com.funder.core/Editor/`:

- [x] **Core Editors**
  - [x] `/Scripts/Systems/Core/Editor/CoreMenuItems.cs` → `Editor/Bootstrap/`

- [x] **System Editors**
  - [x] `/Systems/EventBus/Editor/` → `Editor/EventBus/`
  - [x] `/Systems/FSM/Editor/` → `Editor/FSM/`
  - [x] `/Systems/Logging/Editor/` → `Editor/Logging/`
  - [x] `/Systems/Random/Editor/` → `Editor/Random/`
  - [x] `/Systems/Time/Editor/` → `Editor/Time/`

- [x] **UI Builders**
  - [x] `/Editor/UI/DiagnosticHudBuilder.cs` → `Editor/UI/`

- [x] **Feature Flags**
  - [x] `/Editor/Systems/FeatureFlags/` → `Editor/FeatureFlags/`

### 3.3 Tests Migration

Move from `/Assets/FunderCore/Systems/*/Tests/` to `/Packages/com.funder.core/Tests/`:

- [x] `/Systems/EventBus/Tests/EditMode/` → `Tests/Editor/EventBus/`
- [x] `/Systems/FSM/Tests/EditMode/` → `Tests/Editor/FSM/`
- [x] `/Systems/Logging/Tests/EditMode/` → `Tests/Editor/Logging/`
- [x] `/Systems/Random/Tests/EditMode/` → `Tests/Editor/Random/`
- [x] `/Systems/Time/Tests/EditMode/` → `Tests/Editor/Time/`
- [x] `/Scripts/Systems/Core/Tests/EditMode/` → `Tests/Editor/Bootstrap/`

### 3.4 Resources Migration

- [x] Move `/Assets/FunderCore/Resources/` → `/Packages/com.funder.core/Runtime/Resources/`
- [x] Move `/Assets/FunderCore/Prefabs/` → `/Packages/com.funder.core/Runtime/Prefabs/`

### 3.5 Samples Migration

- [x] Move `/Assets/Samples/Funder Core/` → `/Packages/com.funder.core/Samples~/`

## Phase 4: Update References in RogueDeal

### 4.1 Update Assembly References

- [x] `RogueDeal.asmdef` references updated
- [x] `RogueDeal.Editor.asmdef` references updated

In `/Assets/RogueDeal/Scripts/RogueDeal.asmdef`:
```json
{
    "name": "RogueDeal",
    "references": [
        "Funder.Core",
        "Unity.InputSystem"
    ],
    ...
}
```

In `/Assets/RogueDeal/Scripts/Editor/RogueDeal.Editor.asmdef`:
```json
{
    "name": "RogueDeal.Editor",
    "references": [
        "RogueDeal",
        "Funder.Core",
        "Funder.Core.Editor"
    ],
    ...
}
```

### 4.2 Update Using Statements

- [x] Namespaces updated in RogueDeal scripts

Search and verify all files in `/Assets/RogueDeal/Scripts/` have correct namespaces:
```csharp
using Funder.Core;
using Funder.Core.Services;
using Funder.Core.Events;
using Funder.Core.Analytics;
using Funder.Core.Logging;
using Funder.Core.Random;
using Funder.Core.Time;
```

### 4.3 Update Resource Paths

- [x] Resource paths validated for package layout

Any code loading resources from FunderCore:
```csharp
// Before
var config = Resources.Load<BootstrapConfig>("Configs/BootstrapConfig");

// After (if using package resources)
var config = Resources.Load<BootstrapConfig>("FunderCore/Configs/BootstrapConfig");
```

## Phase 5: Testing

### 5.1 Compilation Test
- [ ] Open Unity project
- [ ] Wait for compilation
- [ ] Verify no errors in Console
- [ ] Check all assembly references resolved

### 5.2 Runtime Test
- [ ] Open Entry scene
- [ ] Enter Play Mode
- [ ] Verify Bootstrap initializes correctly
- [ ] Check all services register properly
- [ ] Test EventBus functionality
- [ ] Verify no runtime errors

### 5.3 Editor Tools Test
- [ ] Test all `Funder/Core/` menu items
- [ ] Verify scene creators work
- [ ] Check config validators
- [ ] Test diagnostic tools

### 5.4 RogueDeal Integration Test
- [ ] Test all `Funder/Rogue Deal/` menu items
- [ ] Verify analytics integration
- [ ] Test combat system
- [ ] Check level loading
- [ ] Verify all game systems work

## Phase 6: Cleanup

### 6.1 Remove Old Files
After confirming everything works:
- [x] Delete `/Assets/FunderCore/` directory
- [x] Remove any `.meta` files
- [x] Clean up old assembly definitions

### 6.2 Update Documentation
- [ ] Update README in package
- [ ] Create CHANGELOG.md
- [ ] Document API changes
- [ ] Update wiki/documentation

## Phase 7: Version Control

### 7.1 Package Repository
Consider making the package a separate git repository:
```bash
cd /Packages/com.funder.core/
git init
git add .
git commit -m "Initial package commit"
```

### 7.2 Package Distribution
Options for distributing the package:
1. **Git URL**: Add via Package Manager using git URL
2. **Local Package**: Reference via `file:../` path
3. **Scoped Registry**: Publish to your own registry
4. **Unity Asset Store**: Publish as an asset

## Common Issues & Solutions

### Issue: "Assembly has reference to non-existent assembly"
**Solution**: Check assembly definition files have correct references

### Issue: "The type or namespace name could not be found"
**Solution**: Verify using statements and assembly references in `.asmdef` files

### Issue: "Resources not found"
**Solution**: Check resource paths, may need `FunderCore/` prefix

### Issue: "Menu items not appearing"
**Solution**: Reimport scripts or restart Unity

## Post-Migration Benefits

✅ **Clean Separation**: Framework code isolated from game code
✅ **Reusability**: Use Funder Core in multiple projects
✅ **Version Control**: Independent versioning
✅ **Team Workflow**: Separate teams can work on framework vs game
✅ **Faster Iteration**: Core updates don't require game recompilation
✅ **Professional Structure**: Industry-standard package organization

## Support

If you encounter issues during migration:
1. Check Unity Console for specific error messages
2. Verify all assembly references are correct
3. Ensure namespaces match the new structure
4. Test incrementally - migrate one system at a time
5. Keep backups of working code before major changes

---

**Remember**: The menu consolidation is complete! The physical file migration is the next step when you're ready.
