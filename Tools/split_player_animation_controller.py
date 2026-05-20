#!/usr/bin/env python3
"""Split GeisPlayerAnimationController into partial class files."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MAIN = ROOT / "Assets/Geis/Scripts/Locomotion/GeisPlayerAnimationController.cs"
LOC = ROOT / "Assets/Geis/Scripts/Locomotion"

HEADER = """\
/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using Geis.Combat;
using Geis.InputSystem;
using Geis.InteractInput;
using Geis.Attributes;
using Geis.Animation;
using Geis.SoulRealm;
using RogueDeal.Combat;
using RogueDeal.Combat.Targeting;

namespace Geis.Locomotion
{
    public partial class GeisPlayerAnimationController
    {
"""

FOOTER = """\
    }
}
"""


def slice_lines(lines: list[str], start: int, end: int) -> list[str]:
    return lines[start - 1 : end]


def write_partial(name: str, ranges: list[tuple[int, int]]) -> None:
    body = MAIN.read_text(encoding="utf-8")
    lines = body.splitlines(keepends=True)
    chunks: list[str] = []
    for start, end in ranges:
        chunks.extend(slice_lines(lines, start, end))
    (LOC / f"GeisPlayerAnimationController.{name}.cs").write_text(
        HEADER + "".join(chunks) + FOOTER, encoding="utf-8"
    )


def main() -> None:
    body = MAIN.read_text(encoding="utf-8")
    lines = body.splitlines(keepends=True)

    write_partial("LockOn", [(1047, 1165), (3230, 3441)])
    write_partial("Combat", [(1288, 2313)])
    write_partial("LocomotionStates", [(2317, 3229), (3442, 3706)])

    main_lines = (
        slice_lines(lines, 1, 1046)
        + slice_lines(lines, 1167, 1286)
        + ["\n", "        #endregion\n", "\n"]
        + FOOTER.splitlines(keepends=True)
    )
    MAIN.write_text("".join(main_lines), encoding="utf-8")
    print(f"Main: {len(main_lines)} lines")
    for name in ("LockOn", "Combat", "LocomotionStates"):
        p = LOC / f"GeisPlayerAnimationController.{name}.cs"
        print(f"{name}: {len(p.read_text(encoding='utf-8').splitlines())} lines")


if __name__ == "__main__":
    main()
