"""Plan 25 S6 : cadre et animation d'XP, crâne, sceaux, pictogrammes de minimap."""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from tools.sprites.ui.hud import assets
from tools.sprites.ui.kit import board, export, nine_patch


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sheet", type=Path, default=Path("doc/plans/planches/25-s6-hud.png"))
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    images = assets()
    bar = nine_patch(images["xp_frame"], (128, 9), 4)
    for x in range(4, 82, 16):
        bar.alpha_composite(images["xp_fill_00"], (x, 2))
    bar.alpha_composite(images["xp_tip_00"], (80, 0))
    rows = [("XP : métal patiné et mémoire dorée", [bar]),
            ("XP : 4 poses de remplissage, pointe, éclat", [images[f"xp_fill_{f:02}"] for f in range(4)] + [images["xp_tip_00"], images["xp_burst_01"]]),
            ("Éliminations 10 px / minimap 5 px", [images["kill_skull"]] + [images[f"minimap_{name}"] for name in ("chest", "memorial", "rift", "place", "player")])]
    board(rows, args.sheet)
    if not args.dry_run:
        output = Path("assets/ui/hud/plan25")
        export(images, output, "generate_hud_sprites")
        manifest = {"xp": {"frame": "xp_frame.png", "margins": [4, 2, 4, 1], "fill_size": [16, 6], "frames": 4, "fps": 8},
                    "kills": {"file": "kill_skull.png", "size": [10, 10]},
                    "minimap": {"size": [5, 5], "ids": ["chest", "memorial", "rift", "place", "player", "workshop", "peril_stele"]}}
        (output / "hud_manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n")
    print(f"[generate_hud_sprites] {len(images)} textures ; planche : {args.sheet}")


if __name__ == "__main__":
    main()
