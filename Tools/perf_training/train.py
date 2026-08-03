#!/usr/bin/env python3
"""
Performance Intelligence — FPS prediction training pipeline (v2).

Trains a model to predict average FPS from SCENE COMPOSITION — i.e. from
data you can measure before running the scene (object counts, lights,
materials, etc.) rather than from runtime measurements like CPU frame time.

Each session folder must contain both capture.json (scene census) and
capture.csv (per-frame metrics). One training row is built per session.

Usage:
    cd Tools/perf_training
    pip install -r requirements.txt

    python train.py                          # auto-detect captures
    python train.py --captures <path>        # override capture directory
    python train.py --export model.onnx      # also write ONNX model
"""

import argparse
import glob
import json
import os
import sys
from typing import Optional

import numpy as np
import pandas as pd
from sklearn.linear_model import Ridge
from sklearn.ensemble import GradientBoostingRegressor
from sklearn.model_selection import LeaveOneOut, cross_val_score
from sklearn.metrics import mean_absolute_error, r2_score
from sklearn.preprocessing import RobustScaler, FunctionTransformer
from sklearn.pipeline import Pipeline

# ---------------------------------------------------------------------------
# Schema
# ---------------------------------------------------------------------------

# Scene census fields extracted from capture.json → sceneCensus.
# These are the pre-run predictors: you can measure them with SceneCensus.Capture()
# without ever entering Play Mode. A useful model takes these as input.
SCENE_CENSUS_FEATURES = [
    "activeGameObjects",
    "activeRenderers",
    "meshRenderers",
    "skinnedMeshRenderers",
    "particleSystems",
    "lights",
    "realtimeLights",
    "shadowCastingLights",
    "cameras",
    "canvases",
    "rigidbodies",
    "colliders",
    "animators",
    "uniqueMaterials",
    "uniqueShaders",
    "estimatedTriangleCount",
]

# Runtime render stats averaged across the session.
# These are NOT used as predictors (you'd need to run the scene to get them),
# but they're included in the output table for reference.
RUNTIME_STAT_COLS = [
    "drawCalls", "batches", "triangles", "setPassCalls",
    "vertices", "cpuMainThreadMs",
]

TARGET_COL = "avgFPS"

_SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
DEFAULT_CAPTURES_DIR = os.path.normpath(
    os.path.join(_SCRIPT_DIR, "..", "..",
                 "Assets", "PerformanceIntelligence", "Data", "Captures")
)


def _log1p_nonnegative(X):
    """
    Stabilize heavy-tailed count features before scaling.
    Scene census metrics are counts/estimates and should be >= 0, but we clip
    defensively to avoid invalid values if malformed input slips in.
    """
    X = np.asarray(X, dtype=np.float64)
    return np.log1p(np.clip(X, a_min=0.0, a_max=None))

# ---------------------------------------------------------------------------
# Loading
# ---------------------------------------------------------------------------

def load_session(session_dir: str) -> Optional[dict]:
    """
    Load one session directory.
    Returns a dict with scene census fields + avg FPS, or None if unusable.
    """
    json_path = os.path.join(session_dir, "capture.json")
    csv_path  = os.path.join(session_dir, "capture.csv")

    if not os.path.exists(json_path) or not os.path.exists(csv_path):
        return None

    # ── Scene census from JSON ────────────────────────────────────────────
    with open(json_path, encoding="utf-8-sig") as f:
        raw = json.load(f)

    sc = raw.get("sceneCensus") or {}

    # ── Frame averages from CSV ───────────────────────────────────────────
    df = pd.read_csv(csv_path)
    df = df.replace(-1, np.nan).replace(-1.0, np.nan)
    df = df[(df["estimatedFPS"] > 0) & (df["estimatedFPS"] < 500)]

    if len(df) < 10:
        return None

    row = {
        "sessionId":  raw.get("sessionId", "")[:8],
        "sceneName":  sc.get("sceneName", "unknown"),
        "frameCount": len(df),
        TARGET_COL:   float(df["estimatedFPS"].mean()),
    }

    # Scene census fields
    for field in SCENE_CENSUS_FEATURES:
        row[field] = sc.get(field, np.nan)

    # Runtime averages (reference only, not used as features)
    for col in RUNTIME_STAT_COLS:
        if col in df.columns:
            vals = df[col].dropna()
            row[f"avg_{col}"] = float(vals.mean()) if len(vals) > 0 else np.nan

    return row


def load_all_sessions(captures_dir: str) -> pd.DataFrame:
    """Load every session directory under captures_dir."""
    # A session dir is any direct subdirectory that contains capture.json
    session_dirs = [
        d for d in glob.glob(os.path.join(captures_dir, "*"))
        if os.path.isdir(d) and os.path.exists(os.path.join(d, "capture.json"))
    ]

    if not session_dirs:
        print(f"\nNo session folders found in:\n  {os.path.abspath(captures_dir)}")
        print("\nEach session needs both capture.json and capture.csv.")
        print("Click 'Export Latest Capture' in the Performance Intelligence window.")
        sys.exit(1)

    rows = []
    skipped = 0
    for d in sorted(session_dirs):
        row = load_session(d)
        if row:
            rows.append(row)
        else:
            skipped += 1

    if not rows:
        print("All sessions were unusable (missing JSON, CSV, or too few frames).")
        sys.exit(1)

    if skipped:
        print(f"Skipped {skipped} incomplete session(s).")

    return pd.DataFrame(rows)

# ---------------------------------------------------------------------------
# Summary table
# ---------------------------------------------------------------------------

def print_session_table(df: pd.DataFrame):
    """Print a readable per-session overview."""
    print(f"\n{'Scene':<45s} {'Frames':>7} {'AvgFPS':>8} {'Tris':>10} {'Lights':>7} {'Mats':>6}")
    print("─" * 85)
    for _, r in df.iterrows():
        scene = str(r["sceneName"])[:44]
        tris  = int(r.get("estimatedTriangleCount", 0) or 0)
        lights = int(r.get("lights", 0) or 0)
        mats  = int(r.get("uniqueMaterials", 0) or 0)
        print(f"  {scene:<43s} {int(r['frameCount']):>7} {r[TARGET_COL]:>8.1f} {tris:>10,} {lights:>7} {mats:>6}")
    print()

# ---------------------------------------------------------------------------
# Feature selection
# ---------------------------------------------------------------------------

def select_features(df: pd.DataFrame, corr_threshold: float = 0.90) -> tuple[list[str], pd.DataFrame]:
    """
    Return scene census columns suitable for Ridge regression:
    - no NaNs
    - non-zero variance
    - no pair with Pearson |r| > corr_threshold (prevents near-singular matrix / matmul warning)
    """
    candidates = [c for c in SCENE_CENSUS_FEATURES if c in df.columns]

    # Drop columns with any NaN
    complete = [c for c in candidates if df[c].notna().all()]

    # Drop zero-variance columns (same value in every session = no signal)
    varied = [c for c in complete if df[c].nunique() > 1]

    dropped_basic = set(candidates) - set(varied)
    if dropped_basic:
        print(f"Dropped {len(dropped_basic)} constant/incomplete feature(s): {sorted(dropped_basic)}")

    # Drop highly correlated features — keeps the first of each correlated pair.
    # e.g. activeGameObjects ≈ activeRenderers ≈ meshRenderers at small N.
    corr_matrix = df[varied].corr().abs()
    upper = corr_matrix.where(np.triu(np.ones(corr_matrix.shape, dtype=bool), k=1))
    drop_collinear = [c for c in upper.columns if (upper[c] > corr_threshold).any()]
    kept = [c for c in varied if c not in drop_collinear]
    if drop_collinear:
        print(f"Dropped {len(drop_collinear)} collinear feature(s) (|r|>{corr_threshold}): {drop_collinear}")

    keep_cols = kept + [TARGET_COL]
    if "sceneName" in df.columns:
        keep_cols.append("sceneName")
    return kept, df[keep_cols].dropna()

# ---------------------------------------------------------------------------
# Training
# ---------------------------------------------------------------------------

def choose_model(n_samples: int):
    """Ridge for small N, gradient boosting for larger datasets."""
    if n_samples < 40:
        return Pipeline([
            ("log1p", FunctionTransformer(_log1p_nonnegative, validate=False)),
            ("scaler", RobustScaler(quantile_range=(10.0, 90.0))),
            ("model", Ridge(alpha=1.0, solver="svd")),
        ])
    return GradientBoostingRegressor(n_estimators=100, max_depth=3, random_state=42)


def train(df: pd.DataFrame, feature_cols: list[str]):
    """Train and evaluate a FPS predictor. Returns (model, feature_cols)."""
    X = df[feature_cols].values
    y = df[TARGET_COL].values
    n = len(X)

    print(f"Training on {n} sessions × {len(feature_cols)} scene features")

    if n < 5:
        print(f"\nOnly {n} sessions — need at least 5 for meaningful evaluation.")
        print("Collect more captures across different scenes and run again.")
        sys.exit(1)

    model = choose_model(n)
    model_name = "Ridge regression" if n < 40 else "Gradient Boosting"
    print(f"Model: {model_name} (auto-selected for n={n})\n")

    # Cross-validation: LOOCV for small N, 5-fold for larger.
    # R² is undefined for single-sample test folds (LOOCV), so use MAE there.
    if n < 20:
        cv = LeaveOneOut()
        cv_label = "leave-one-out MAE"
        cv_metric = "neg_mean_absolute_error"
    else:
        cv = min(5, n // 4)
        cv_label = f"{cv}-fold R²"
        cv_metric = "r2"

    cv_scores = cross_val_score(model, X, y, cv=cv, scoring=cv_metric)

    # Final fit on all data for feature importances
    model.fit(X, y)
    y_pred = model.predict(X)

    print("── Evaluation ─────────────────────────────────────────────")
    if n < 20:
        cv_mae = -cv_scores
        print(f"  CV MAE ({cv_label}):   {cv_mae.mean():.1f} ± {cv_mae.std():.1f} fps")
    else:
        print(f"  CV R² ({cv_label}):    {cv_scores.mean():.3f} ± {cv_scores.std():.3f}")
    print(f"  Train R²:              {r2_score(y, y_pred):.3f}  (in-sample, informational)")
    print(f"  Train MAE:             {mean_absolute_error(y, y_pred):.1f} fps")

    if n < 20:
        print(f"\n  Note: {n} sessions is too few for a reliable model.")
        print("  CV R² will be noisy. Collect 40+ sessions for stable results.")

    # Per-session predictions
    print("\n── Per-Session Predictions ────────────────────────────────")
    print(f"  {'Scene':<40s} {'Actual':>8} {'Predicted':>10} {'Error':>8}")
    print("  " + "─" * 68)
    for i, (_, row) in enumerate(df.iterrows()):
        err = y_pred[i] - y[i]
        scene = str(row["sceneName"])[:39]
        print(f"  {scene:<40s} {y[i]:>8.1f} {y_pred[i]:>10.1f} {err:>+8.1f}")

    # Feature correlations (more meaningful than importances at small N)
    print("\n── Feature Correlations with FPS ──────────────────────────")
    corrs = df[feature_cols + [TARGET_COL]].corr()[TARGET_COL].drop(TARGET_COL)
    for feat, corr in corrs.abs().sort_values(ascending=False).items():
        sign = "+" if corrs[feat] >= 0 else "-"
        bar  = "█" * max(1, int(abs(corr) * 30))
        print(f"  {feat:<30s}  {sign}{bar} {corrs[feat]:+.3f}")

    return model, feature_cols

# ---------------------------------------------------------------------------
# ONNX export
# ---------------------------------------------------------------------------

def export_onnx(model, feature_cols: list[str], output_path: str):
    try:
        from skl2onnx import convert_sklearn
        from skl2onnx.common.data_types import FloatTensorType
    except ImportError:
        print("\nskl2onnx / onnx not installed — skipping ONNX export.")
        print("Run: pip install skl2onnx onnx")
        return

    n = len(feature_cols)
    # unwrap Pipeline if needed
    export_model = model.named_steps["model"] if hasattr(model, "named_steps") else model
    proto = convert_sklearn(
        export_model,
        name="perf_fps_predictor",
        initial_types=[("input", FloatTensorType([None, n]))],
    )
    with open(output_path, "wb") as f:
        f.write(proto.SerializeToString())

    print(f"\n── ONNX Export ─────────────────────────────────────────────")
    print(f"  Saved:    {os.path.abspath(output_path)}")
    print(f"  Input:    float32[N, {n}]  — {feature_cols}")
    print(f"  Output:   float32[N, 1]   — predicted avg FPS")

# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(
        description="Train a scene-composition → FPS predictor from Unity captures."
    )
    parser.add_argument("--captures", default=DEFAULT_CAPTURES_DIR, metavar="DIR")
    parser.add_argument("--export", default=None, metavar="FILE.onnx")
    args = parser.parse_args()

    print("Performance Intelligence — Training Pipeline v2")
    print("=" * 55)
    print("Goal: predict FPS from scene composition (pre-run features)\n")

    df              = load_all_sessions(args.captures)
    print(f"Loaded {len(df)} session(s)\n")
    print_session_table(df)

    feature_cols, clean_df = select_features(df)
    model, feature_cols    = train(clean_df, feature_cols)

    if args.export:
        export_onnx(model, feature_cols, args.export)

    print("\nDone.")
    if not args.export:
        print("Tip: --export model.onnx to write an ONNX file for Unity Sentis.")


if __name__ == "__main__":
    main()
