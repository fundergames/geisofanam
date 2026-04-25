#!/usr/bin/env python3
"""
Performance Intelligence — FPS prediction training pipeline.

Loads all capture CSVs exported from the Unity Performance Intelligence window,
trains a gradient-boosted regressor to predict estimatedFPS, and prints
evaluation metrics. Optionally exports the model to ONNX for Unity Sentis.

Usage:
    cd Tools/perf_training
    pip install -r requirements.txt

    python train.py                          # auto-detect captures
    python train.py --captures <path>        # override capture directory
    python train.py --export model.onnx      # also write ONNX model
"""

import argparse
import glob
import os
import sys

import numpy as np
import pandas as pd
from sklearn.ensemble import GradientBoostingRegressor
from sklearn.metrics import mean_absolute_error, r2_score
from sklearn.model_selection import cross_val_score, train_test_split

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

# Candidate feature columns in order of preference.
# The script uses whichever subset has data; columns that are all-NaN are dropped.
CANDIDATE_FEATURES = [
    "drawCalls",
    "batches",
    "triangles",
    "cpuMainThreadMs",
    "totalReservedMemory",
    "monoUsedSize",
    "gcAllocBytes",
    "setPassCalls",
    "vertices",
    "renderThreadMs",
]

TARGET_COL = "estimatedFPS"

# Path from this script to the Unity captures folder (two dirs up = project root)
_SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
DEFAULT_CAPTURES_DIR = os.path.normpath(
    os.path.join(_SCRIPT_DIR, "..", "..",
                 "Assets", "PerformanceIntelligence", "Data", "Captures")
)

# ---------------------------------------------------------------------------
# Data loading
# ---------------------------------------------------------------------------

def load_captures(captures_dir: str) -> pd.DataFrame:
    """Glob all capture.csv files under captures_dir and concat them."""
    pattern = os.path.join(captures_dir, "**", "capture.csv")
    csv_files = glob.glob(pattern, recursive=True)

    if not csv_files:
        print(f"\nNo capture CSVs found in:\n  {os.path.abspath(captures_dir)}")
        print("\nTo generate captures:")
        print("  1. Open Unity → Window > Performance Intelligence")
        print("  2. Enter Play Mode, click Start Capture")
        print("  3. After capture finishes, click Export Latest Capture")
        sys.exit(1)

    print(f"Found {len(csv_files)} capture session(s):\n")
    dfs = []
    for path in sorted(csv_files):
        df = pd.read_csv(path)
        session_id = os.path.basename(os.path.dirname(path))
        scene = df["sceneName"].iloc[0] if "sceneName" in df.columns else "unknown"
        print(f"  {session_id[:8]}…  scene={scene:<30s}  frames={len(df)}")
        dfs.append(df)

    combined = pd.concat(dfs, ignore_index=True)
    print(f"\nTotal frames loaded: {len(combined)}")
    return combined


# ---------------------------------------------------------------------------
# Cleaning
# ---------------------------------------------------------------------------

def clean(df: pd.DataFrame) -> pd.DataFrame:
    """Replace -1 sentinels with NaN and remove invalid FPS rows."""
    # -1 means 'metric unavailable on this platform' in the capture format
    df = df.replace(-1, np.nan).replace(-1.0, np.nan)

    before = len(df)
    df = df[df[TARGET_COL].notna() & (df[TARGET_COL] > 0) & (df[TARGET_COL] < 500)]
    dropped = before - len(df)
    if dropped:
        print(f"Dropped {dropped} rows with invalid FPS")

    return df


def report_availability(df: pd.DataFrame, candidates: list[str]) -> list[str]:
    """Print per-column availability and return columns with >50% valid data."""
    print("\nFeature availability (% rows with valid data):\n")
    usable = []
    for col in candidates:
        if col not in df.columns:
            continue
        pct = df[col].notna().mean() * 100
        bar = "█" * int(pct / 5)
        mark = "✓" if pct >= 50 else "✗"
        print(f"  {mark} {col:<30s} {bar:<20s} {pct:5.1f}%")
        if pct >= 50:
            usable.append(col)

    if not usable:
        print("\nNo features have >50% valid data.")
        print("Tip: drawCalls, batches, triangles are usually available on PC builds.")
        sys.exit(1)

    print(f"\nUsing {len(usable)} feature(s): {usable}")
    return usable


# ---------------------------------------------------------------------------
# Training
# ---------------------------------------------------------------------------

def train(df: pd.DataFrame, feature_cols: list[str]):
    """Build feature matrix, train model, print metrics. Returns (model, features)."""
    subset = df[feature_cols + [TARGET_COL]].dropna()

    if len(subset) < 20:
        print(f"\nOnly {len(subset)} complete rows after dropping NaN — need at least 20.")
        print("Collect more captures or check that profiler metrics are enabled.")
        sys.exit(1)

    X = subset[feature_cols].values
    y = subset[TARGET_COL].values

    print(f"\nTraining on {len(X)} samples × {len(feature_cols)} features")

    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=0.2, random_state=42
    )

    model = GradientBoostingRegressor(
        n_estimators=100, max_depth=4, learning_rate=0.1, random_state=42
    )
    model.fit(X_train, y_train)

    y_pred = model.predict(X_test)

    print("\n── Evaluation ─────────────────────────────────────────────")
    print(f"  R²   (1.0 = perfect fit):      {r2_score(y_test, y_pred):.3f}")
    print(f"  MAE  (avg FPS error):           {mean_absolute_error(y_test, y_pred):.2f} fps")

    k = min(5, max(2, len(X) // 10))
    cv = cross_val_score(model, X, y, cv=k, scoring="r2")
    print(f"  CV R² ({k}-fold):                {cv.mean():.3f} ± {cv.std():.3f}")

    print("\n── Feature Importances ────────────────────────────────────")
    ranked = sorted(zip(feature_cols, model.feature_importances_), key=lambda x: -x[1])
    for name, imp in ranked:
        bar = "█" * max(1, int(imp * 50))
        print(f"  {name:<30s} {bar} {imp:.3f}")

    return model, feature_cols


# ---------------------------------------------------------------------------
# ONNX export
# ---------------------------------------------------------------------------

def export_onnx(model, feature_cols: list[str], output_path: str):
    """Export the sklearn model to ONNX. Requires skl2onnx + onnx packages."""
    try:
        from skl2onnx import convert_sklearn
        from skl2onnx.common.data_types import FloatTensorType
    except ImportError:
        print("\nskl2onnx / onnx not installed — skipping ONNX export.")
        print("Run: pip install skl2onnx onnx")
        return

    n = len(feature_cols)
    proto = convert_sklearn(
        model,
        name="perf_fps_predictor",
        initial_types=[("input", FloatTensorType([None, n]))],
    )
    with open(output_path, "wb") as f:
        f.write(proto.SerializeToString())

    print(f"\n── ONNX Export ────────────────────────────────────────────")
    print(f"  Saved to:  {os.path.abspath(output_path)}")
    print(f"  Input:     float32[N, {n}]")
    print(f"  Features:  {feature_cols}")
    print(f"  Output:    float32[N, 1]  (predicted FPS)")
    print("\n  Load in Unity via Unity Sentis (ModelLoader.Load) to use")
    print("  with IPerformancePredictor.")


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(
        description="Train an FPS predictor from Unity Performance Intelligence captures."
    )
    parser.add_argument(
        "--captures",
        default=DEFAULT_CAPTURES_DIR,
        metavar="DIR",
        help=f"Path to Captures directory (default: {DEFAULT_CAPTURES_DIR})",
    )
    parser.add_argument(
        "--export",
        default=None,
        metavar="FILE.onnx",
        help="Export trained model to ONNX for Unity Sentis",
    )
    args = parser.parse_args()

    print("Performance Intelligence — Training Pipeline")
    print("=" * 55)

    df      = load_captures(args.captures)
    df      = clean(df)
    usable  = report_availability(df, CANDIDATE_FEATURES)
    model, feature_cols = train(df, usable)

    if args.export:
        export_onnx(model, feature_cols, args.export)

    print("\nDone.")
    if not args.export:
        print("Tip: add --export model.onnx to write an ONNX file for Unity Sentis.")


if __name__ == "__main__":
    main()
