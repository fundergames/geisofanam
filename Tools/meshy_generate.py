#!/usr/bin/env python3
"""
Meshy.ai Text-to-3D Generator for Geis of Annam

Generates 3D models from text prompts using the Meshy API. Supports both preview
(preview-only mesh) and full refine (textured model) workflows.

Usage:
    # Set API key (required for production; use test key for development)
    set MESHY_API_KEY=msy-your-key-here   # Windows
    export MESHY_API_KEY=msy-your-key-here   # Linux/macOS

    # Generate a model (preview + refine, outputs GLB)
    python Tools/meshy_generate.py "a forest guardian spirit, organic plant-fused creature"

    # Preview only (no texture)
    python Tools/meshy_generate.py --preview-only "a monster mask"

    # Custom output path (default: Assets/_Generated/Staging/<slug>.glb)
    python Tools/meshy_generate.py -o Assets/_Generated/Staging/ForestGuardian.glb "..."

Test mode: Use MESHY_API_KEY=msy_dummy_api_key_for_test_mode_12345678 to explore
without consuming credits (returns sample results).
"""

import argparse
import os
import sys
import time
from pathlib import Path

try:
    import requests
except ImportError:
    print("Error: 'requests' is required. Install with: pip install requests")
    sys.exit(1)

API_BASE = "https://api.meshy.ai/openapi/v2"
ENV_API_KEY = "MESHY_API_KEY"
TEST_MODE_KEY = "msy_dummy_api_key_for_test_mode_12345678"


def get_api_key() -> str:
    key = os.environ.get(ENV_API_KEY)
    if not key:
        print(f"Error: Set {ENV_API_KEY} environment variable.")
        print("  Example: export MESHY_API_KEY=msy-your-key")
        print("  Test mode: export MESHY_API_KEY=msy_dummy_api_key_for_test_mode_12345678")
        sys.exit(1)
    return key


def create_task(prompt: str, negative_prompt: str, should_remesh: bool, headers: dict) -> str:
    payload = {
        "mode": "preview",
        "prompt": prompt,
        "negative_prompt": negative_prompt or "low quality, low resolution, ugly, blurry",
        "should_remesh": should_remesh,
    }
    r = requests.post(f"{API_BASE}/text-to-3d", headers=headers, json=payload, timeout=30)
    r.raise_for_status()
    return r.json()["result"]


def poll_task(task_id: str, headers: dict) -> dict:
    while True:
        r = requests.get(f"{API_BASE}/text-to-3d/{task_id}", headers=headers, timeout=30)
        r.raise_for_status()
        data = r.json()
        status = data.get("status", "UNKNOWN")
        progress = data.get("progress", 0)

        if status == "SUCCEEDED":
            return data
        if status in ("FAILED", "CANCELLED"):
            raise RuntimeError(f"Task {task_id} ended with status {status}: {data.get('error', data)}")

        print(f"  Status: {status} | Progress: {progress} | Polling in 5s...")
        time.sleep(5)


def refine_task(preview_task_id: str, headers: dict) -> str:
    payload = {"mode": "refine", "preview_task_id": preview_task_id}
    r = requests.post(f"{API_BASE}/text-to-3d", headers=headers, json=payload, timeout=30)
    r.raise_for_status()
    return r.json()["result"]


def download_model(url: str, out_path: Path) -> None:
    r = requests.get(url, stream=True, timeout=60)
    r.raise_for_status()
    out_path.parent.mkdir(parents=True, exist_ok=True)
    with open(out_path, "wb") as f:
        for chunk in r.iter_content(chunk_size=8192):
            f.write(chunk)


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Generate 3D models from text using Meshy.ai",
        epilog="Set MESHY_API_KEY in your environment. See script docstring for details.",
    )
    parser.add_argument("prompt", help="Text description of the 3D model")
    parser.add_argument(
        "-n",
        "--negative-prompt",
        default="low quality, low resolution, low poly, ugly, blurry",
        help="Negative prompt (what to avoid)",
    )
    parser.add_argument(
        "-o",
        "--output",
        type=Path,
        default=None,
        help="Output path for GLB file (default: Assets/_Generated/Staging/<slug>.glb)",
    )
    parser.add_argument(
        "--preview-only",
        action="store_true",
        help="Skip refine stage; output untextured preview mesh only",
    )
    parser.add_argument(
        "--no-remesh",
        action="store_true",
        help="Disable remeshing",
    )
    args = parser.parse_args()

    api_key = get_api_key()
    headers = {"Authorization": f"Bearer {api_key}"}

    slug = "".join(c if c.isalnum() or c in "-_" else "_" for c in args.prompt[:40])
    slug = slug.strip("_") or "model"

    if args.output:
        out_path = Path(args.output)
    else:
        # Generate → Assets/_Generated/Staging/ (validate → promote → Assets/Art/)
        out_path = Path(__file__).resolve().parent.parent / "Assets" / "_Generated" / "Staging" / f"{slug}.glb"

    print("Meshy Text-to-3D Generator")
    print("=" * 40)
    print(f"Prompt: {args.prompt}")
    print(f"Output: {out_path}")
    print()

    try:
        # 1. Create preview task
        print("Creating preview task...")
        preview_task_id = create_task(
            args.prompt,
            args.negative_prompt,
            not args.no_remesh,
            headers,
        )
        print(f"  Task ID: {preview_task_id}")

        # 2. Poll preview
        print("Waiting for preview...")
        preview_task = poll_task(preview_task_id, headers)
        preview_url = preview_task.get("model_urls", {}).get("glb")
        if not preview_url:
            raise RuntimeError("Preview response missing model_urls.glb")

        if args.preview_only:
            print("Downloading preview model...")
            download_model(preview_url, out_path)
            print(f"Done: {out_path}")
            return 0

        # 3. Create refine task
        print("Creating refine task...")
        refined_task_id = refine_task(preview_task_id, headers)
        print(f"  Task ID: {refined_task_id}")

        # 4. Poll refine
        print("Waiting for refined model...")
        refined_task = poll_task(refined_task_id, headers)
        refined_url = refined_task.get("model_urls", {}).get("glb")
        if not refined_url:
            raise RuntimeError("Refined response missing model_urls.glb")

        # 5. Download
        print("Downloading refined model...")
        download_model(refined_url, out_path)
        print(f"Done: {out_path}")
        return 0

    except requests.exceptions.RequestException as e:
        print(f"API error: {e}")
        if hasattr(e, "response") and e.response is not None:
            try:
                print(e.response.text)
            except Exception:
                pass
        return 1
    except RuntimeError as e:
        print(f"Error: {e}")
        return 1


if __name__ == "__main__":
    sys.exit(main())
