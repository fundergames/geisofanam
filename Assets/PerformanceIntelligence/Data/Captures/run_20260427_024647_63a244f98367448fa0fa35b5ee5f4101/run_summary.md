# Performance Test Run Summary

Total captures: 18
Scenes tested: Demo_Layout_Houses_With_Interiors, Demo_ExteriorOnly_Optimized
Quality presets tested: High, Balanced, Low

## Frame Time by Scene
- Demo_ExteriorOnly_Optimized: avg 16.67 ms, p95 25.2 ms
- Demo_Layout_Houses_With_Interiors: avg 16.67 ms, p95 24.47 ms

## Worst Captures (Top 5 by P95)
- Demo_ExteriorOnly_Optimized / High / MainCameraStart / run 0: p95 30.67 ms
- Demo_ExteriorOnly_Optimized / High / MainCameraStart / run 2: p95 29.55 ms
- Demo_ExteriorOnly_Optimized / High / MainCameraStart / run 1: p95 29.31 ms
- Demo_ExteriorOnly_Optimized / Balanced / MainCameraStart / run 1: p95 28.99 ms
- Demo_ExteriorOnly_Optimized / Balanced / MainCameraStart / run 0: p95 28.99 ms

## Recommendations
- Reduce geometry complexity or improve LODs in: Demo_ExteriorOnly_Optimized, Demo_Layout_Houses_With_Interiors.
