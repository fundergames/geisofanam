# Architecture

## Assembly Graph

```
PerformanceIntelligence.Runtime   (autoReferenced=true, all platforms)
           ↑
PerformanceIntelligence.Editor    (autoReferenced=false, Editor only)
```

No dependency on any game assembly (`Geis`, `Funder.Core`, etc.).
No dependency on URP or any optional Unity package.

---

## Data Flow

```
[Unity Player Loop — LateUpdate]
          │
          ▼
PerformanceCapture (MonoBehaviour)
   ├── ProfilerRecorder × 9  ──────────► FrameCensus (per frame)
   │      Main Thread (Internal)
   │      Render Thread (Internal)
   │      GPU Frame Time (Render)
   │      Draw Calls Count (Render)
   │      Batches Count (Render)
   │      SetPass Calls Count (Render)
   │      Triangles Count (Render)
   │      Vertices Count (Render)
   │      GC.Alloc (Memory)
   │
   └── Profiler.Get*Long() ────────────► memory fields in FrameCensus

[After captureDuration seconds]
StopCapture()
   └── CaptureSession { sessionId, platform, sceneCensus, frames[] }
              │
              ├── ToCsv()    ──► flat CSV (19 columns, one row/frame)
              ├── ToJson()   ──► pretty-printed JSON
              └── ComputeStats() ──► CaptureStats (averages + peaks)
                         │
                         └── PerformanceFeatureVector.FromSession()
                                       │
                                       └── IPerformancePredictor.Predict()
                                                  (NullPerformancePredictor by default)
```

---

## Editor Orchestration

```
PerformanceIntelligenceWindow  (EditorWindow, UI Toolkit, CreateGUI)
   ├── PerformanceCaptureRunner  (plain C# IDisposable, not MonoBehaviour)
   │      ├── Creates "_PerformanceCaptureTemp" GameObject (HideFlags.DontSave)
   │      ├── Adds PerformanceCapture component, sets duration
   │      ├── Subscribes EditorApplication.playModeStateChanged for cleanup
   │      └── On complete → fires OnCaptureComplete → window updates UI
   │
   ├── PerformanceReportGenerator  (static)
   │      └── GenerateMarkdown(session, budget) → SaveReport(md, dir, id)
   │
   └── SceneCensus.Capture()  (called directly, no runner needed)
```

---

## Key Design Decisions

### 1. Sentinel -1 for unavailable metrics
Unavailable metrics store `-1` rather than `0`. This lets training pipelines
distinguish "zero GC allocations this frame" from "GC metric not available on
this platform." `BudgetEvaluation` passes any -1 metric automatically.

### 2. DateTime as ISO 8601 strings
`JsonUtility` cannot serialise `DateTime`. `CaptureSession` stores
`startTimeUtc` / `endTimeUtc` as `string` fields formatted with `"o"` (round-trip
ISO 8601), then exposes them directly. No Newtonsoft.Json dependency.

### 3. PerformanceBudget is not sealed
Unity's serialisation system requires ScriptableObject subclasses to be
non-sealed so the editor can instantiate them via reflection. All other
final classes are `sealed`.

### 4. Runner is not a MonoBehaviour
`PerformanceCaptureRunner` is a plain C# `IDisposable`. The window creates it
in `OnEnable`, disposes in `OnDisable`. This avoids polluting the scene hierarchy
with a persistent editor object. The temporary capture `GameObject` is marked
`HideFlags.DontSave` so it never appears in the hierarchy or gets saved.

### 5. ProfilerRecorder.StartNew (not constructor)
`ProfilerRecorder.StartNew()` creates and immediately starts the recorder in
one call. The recorders are created in `OnEnable` and disposed in `OnDisable`,
matching Unity's component lifecycle. `IsValid` is checked before reading
`LastValue` to handle samplers that are absent on the current platform.

### 6. FindObjectsByType (Unity 6 API)
The project targets Unity 6000.0+. `FindObjectsByType<T>(FindObjectsSortMode.None)`
is used throughout — the deprecated `FindObjectsOfType<T>` is never used.

### 7. NullPerformancePredictor — Null Object pattern
The default predictor returns `IsAvailable = false` and `Predict() => null`.
Future Sentis/ONNX backends implement `IPerformancePredictor` without touching
call sites. The interface is in the Runtime assembly so predictor implementations
can ship in separate Editor or Runtime assemblies.

---

## Adding a Sentis Backend (Phase 4 guide)

1. Create `SentisPerformancePredictor : IPerformancePredictor` in a new assembly
   that references both `PerformanceIntelligence.Runtime` and `Unity.Sentis`.
2. Load your ONNX model file via `ModelLoader.Load(modelAsset)`.
3. In `Predict(PerformanceFeatureVector features)`:
   - Copy `features.values` into a `Tensor<float>`.
   - Run the worker: `worker.Schedule(inputTensor)`.
   - Read output: `worker.PeekOutput()`.
   - Return a `PerformancePrediction` with `predictedFPS` and `confidence`.
4. Wire the predictor into the window's report or add a separate inference panel.

The 24-feature layout in `PerformanceFeatureVector` is the stable contract between
the data pipeline and any model. Changing the layout requires retraining.

---

## CSV → Python Training Pipeline (Phase 3 guide)

```python
import pandas as pd
from sklearn.ensemble import GradientBoostingRegressor
from sklearn.model_selection import train_test_split

df = pd.read_csv("capture.csv")

# Drop sentinel -1 rows for metrics used as features
df = df[df["cpuMainThreadMs"] >= 0]

feature_cols = [
    "cpuMainThreadMs", "drawCalls", "batches", "triangles",
    "meshRenderers", "skinnedMeshRenderers", "lights",
    # ... add scene census columns from a joined SceneCensus snapshot
]
X = df[feature_cols]
y = df["estimatedFPS"]

X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2)
model = GradientBoostingRegressor()
model.fit(X_train, y_train)

# Export to ONNX for Unity Sentis
from skl2onnx import convert_sklearn
from skl2onnx.common.data_types import FloatTensorType
onnx_model = convert_sklearn(model, "perf_predictor",
                              [("input", FloatTensorType([None, len(feature_cols)]))])
with open("perf_predictor.onnx", "wb") as f:
    f.write(onnx_model.SerializeToString())
```

The 24-column `PerformanceFeatureVector` layout maps directly to a fixed-width
input tensor for any sklearn → ONNX pipeline.
