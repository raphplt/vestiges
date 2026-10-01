"""Plan 25 S5 : sprites de bonus prêts pour le consommateur du plan 24 C4."""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from tools.sprites.items.pickups import COLORS, NAMES, models, render
from tools.sprites.ui.kit import board, export, strip


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sheet", type=Path, default=Path("doc/plans/planches/25-s5-bonus.png"))
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    images, entries, rows = {}, {}, []
    for index, item in enumerate(models()):
        idle, disappear, glow = render(item, 2505 + index)
        stem = f"pickup_{item.item_id}"
        images[stem] = strip(idle)
        images[f"{stem}_disappear"] = strip(disappear)
        images[f"{stem}_glow"] = glow
        entries[item.item_id] = {"name": NAMES[item.item_id], "idle": f"{stem}.png", "disappear": f"{stem}_disappear.png",
                                 "glow": f"{stem}_glow.png", "color": COLORS[item.item_id]}
        rows.append((NAMES[item.item_id] + " : flottement / effacement / lueur", idle + disappear + [glow]))
    board(rows, args.sheet)
    if not args.dry_run:
        output = Path("assets/vfx/pickups")
        export(images, output, "generate_pickups")
        manifest = {"frame_size": [16, 16], "layout": "horizontal", "idle_frames": 4, "idle_fps": 6,
                    "disappear_frames": 3, "disappear_fps": 12, "glow_size": [16, 8], "glow_z_index": -1, "pickups": entries}
        (output / "pickups_manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n")
    print(f"[generate_pickups] cinq bonus ; planche : {args.sheet}")


if __name__ == "__main__":
    main()
