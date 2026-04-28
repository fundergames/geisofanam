# Performance Testing (Phase 2)

This guide covers automated, repeatable benchmark captures for ML training and regression checks.

## 1) Create a test config

1. Create `PerformanceTestConfig` via:
   `Create > Performance Intelligence > Testing > Performance Test Config`
2. Fill:
   - `sceneTests`
   - `qualityPresets`
   - `runsPerConfiguration`
   - `warmupDurationSeconds`
   - `captureDurationSeconds`
   - `outputFolder`
   - export/report toggles

Recommended starter matrix:
- 8 scenes x 2 camera paths x 3 quality presets x 3 runs = 144 captures

## 2) Create camera paths

1. Create `CameraPathDefinition` via:
   `Create > Performance Intelligence > Testing > Camera Path Definition`
2. Add waypoints manually, or use:
   `Assets > Create > Performance Intelligence > Testing > Camera Path From SceneView`
3. In the `CameraPathDefinition` inspector:
   - use **Append SceneView Camera Waypoint**
   - use **Normalize Waypoint Times**
4. Scene view gizmos draw waypoints and connecting lines.

## 3) Create quality presets

Create `QualityPresetDefinition` assets and configure:
- quality level index
- target frame rate
- vSync
- render scale (if available on active render pipeline)
- shadows
- anti-aliasing
- resolution

Unavailable APIs are logged and skipped safely.

## 4) Run benchmark captures

Open:
`Window > Performance Intelligence > Performance Test Runner`

Then:
1. Select your `PerformanceTestConfig`
2. Validate the planned test matrix
3. Start run
4. Stop/cancel if needed
5. Open output folder after completion

Output includes:
- metadata-rich JSON per capture
- ML-flat CSV per capture
- merged dataset CSV
- summary JSON per capture
- run-level markdown summary

## Why warmup matters

Warmup reduces startup transients (shader compilation, streaming, caching, initial allocations).  
Capturing after warmup produces more stable and comparable data.

## Why repeated runs matter

Single runs are noisy. Repeated runs let you:
- estimate variance
- avoid overfitting to one-time spikes
- make regressions statistically more credible

## Why frame time beats FPS for ML targets

FPS is nonlinear and compresses meaningful differences at higher frame rates.  
Frame time in milliseconds is linear and better for modeling, trend analysis, and budget comparisons.

Primary target columns:
- `avgFrameTimeMs`
- `p95FrameTimeMs`
- `avgFps`

Use frame time as the main optimization target.
