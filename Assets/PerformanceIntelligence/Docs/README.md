# Performance Intelligence

A self-contained Unity 6 package for runtime performance capture, scene analysis,
budget reporting, and ML-ready dataset export.

---

## Quick Start

1. **Open the window** — `Window > Performance Intelligence`
2. *(Optional)* Create a budget: right-click in the Project panel →
   `Create > Performance Intelligence > Performance Budget`
3. Assign the budget asset in the window under **Platform & Budget**
4. **Enter Play Mode** in Unity
5. Click **Start Capture** — frame data records for the configured duration
6. Click **Export Latest Capture** → writes JSON + CSV to
   `Assets/PerformanceIntelligence/Data/Captures/<sessionId>/`
7. Click **Generate Report** → writes a Markdown report to
   `Assets/PerformanceIntelligence/Data/Reports/`

---

## Scene Census (No PlayMode Required)

Click **Run Scene Census** at any time (edit or play mode) to get an instant
snapshot of scene composition: object counts, renderer types, lights, materials,
shaders, and estimated triangle count.

---

## Installation

Drop the `Assets/PerformanceIntelligence/` folder into any Unity 6 project.
Unity will pick up the two `.asmdef` files automatically.

**Minimum Unity version:** 6000.0 (Unity 6)
**Render Pipeline:** URP or built-in (package has no URP dependency)
**Packages required:** none — only built-in Unity modules

---

## Performance Budget

Create one `PerformanceBudget` ScriptableObject per target platform:

| Field | Description |
|-------|-------------|
| Platform Name | Label for the profile (e.g. "iOS Mid-Range") |
| Target FPS | Minimum acceptable frame rate |
| Frame Budget Ms | Maximum allowed frame time (1000 / targetFPS) |
| Max Draw Calls | Draw call ceiling per frame |
| Max Batches | Render batch ceiling per frame |
| Max Triangles | Triangle ceiling per frame |
| Max Memory MB | Total reserved memory ceiling |
| Max GC Alloc Bytes/Frame | Per-frame GC allocation ceiling |
| Notes | Free-text notes |

---

## Export Formats

### JSON (`capture.json`)
Full session including scene census snapshot and all frame samples.
Produced by `JsonUtility.ToJson` — readable by any JSON library.

### CSV (`capture.csv`)
Flat, one-row-per-frame format. 19 columns:

```
sessionId, platform, sceneName, frameIndex, timestamp, deltaTime, estimatedFPS,
cpuMainThreadMs, renderThreadMs, gpuFrameMs,
drawCalls, batches, setPassCalls, triangles, vertices,
gcAllocBytes, totalReservedMemory, monoHeapSize, monoUsedSize
```

Unavailable metrics appear as `-1` (not zero) so training pipelines can mask them.

---

## ML Feature Vector

`PerformanceFeatureVector.FromSession(session)` flattens a session into a
fixed-size 24-element float array for model inference:

- Features 0–5: averaged frame performance (FPS, frame ms, worst frame, memory, GC)
- Features 6–21: scene composition counts (GameObjects, renderers, lights, etc.)
- Features 22–23: session metadata (frame count, duration)

Implement `IPerformancePredictor` to plug in any backend:
```csharp
public class MyPredictor : IPerformancePredictor
{
    public bool IsAvailable => true;
    public PerformancePrediction Predict(PerformanceFeatureVector features) { ... }
}
```

---

## Known Limitations

| Metric | Notes |
|--------|-------|
| `cpuMainThreadMs` / `renderThreadMs` | Requires Profiler enabled; may be 0 in some builds |
| `gpuFrameMs` | Requires GPU profiling support; not available on all APIs (Metal, Vulkan vary) |
| `drawCalls`, `batches`, etc. | Sampler names are Unity-version-sensitive; appear as -1 if the sampler is absent |
| Scene census triangle count | Allocates a managed array copy per mesh — fine for one-shot use, not per-frame |
| Frame capture | Requires Play Mode — `ProfilerRecorder` runs only when the player loop is active |

---

## Roadmap

| Phase | Status | Description |
|-------|--------|-------------|
| 1 | ✓ Done | Editor window, frame capture, scene census, reports |
| 2 | Planned | Automated benchmark scenes + camera-path playback |
| 3 | Planned | Python training pipeline from exported CSV |
| 4 | Planned | ONNX export + Unity Sentis inference via `IPerformancePredictor` |
| 5 | Planned | CI regression checks + GitHub/Slack reporting |

---

## File Layout

```
Assets/PerformanceIntelligence/
├── Runtime/
│   ├── PerformanceIntelligence.Runtime.asmdef
│   ├── FrameCensus.cs              Per-frame metric data class
│   ├── SceneCensus.cs              Scene composition snapshot
│   ├── PerformanceBudget.cs        ScriptableObject budget profile
│   ├── PerformanceCapture.cs       MonoBehaviour sampler + CaptureSession + CaptureStats
│   └── PerformanceFeatureSchema.cs FeatureVector, Prediction, IPerformancePredictor
├── Editor/
│   ├── PerformanceIntelligence.Editor.asmdef
│   ├── PerformanceIntelligenceWindow.cs  EditorWindow (UI Toolkit)
│   ├── PerformanceCaptureRunner.cs       Editor capture orchestration
│   └── PerformanceReportGenerator.cs     Markdown report generator
├── Data/
│   ├── Captures/   JSON + CSV exports (one folder per sessionId)
│   └── Reports/    Markdown reports
└── Docs/
    ├── README.md        (this file)
    └── Architecture.md  System design and data flow
```
