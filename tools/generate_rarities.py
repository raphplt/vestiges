"""Plan 25 S1 : planche par défaut ; --export seulement après validation."""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from tools.sprites.ui.kit import board, export, nine_patch, strip
from tools.sprites.ui.rarities import PALETTE, assets, card, clover, jump, shard


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sheet", type=Path, default=Path("doc/plans/planches/25-s1-raretes.png"))
    parser.add_argument("--export", action="store_true")
    args = parser.parse_args()
    images = assets()
    rows = [(p["id"], [shard(rank, 12), shard(rank, 24), nine_patch(card(rank), (40, 40))])
            for rank, p in enumerate(PALETTE)]
    rows.append(("Reflet légendaire / trèfle 8 px", [shard(4, 24, f) for f in range(4)] + [clover()]))
    for rank in range(4):
        rows.append((f"Saut : {PALETTE[rank]['id']} → {PALETTE[rank + 1]['id']}", jump(rank)))
    board(rows, args.sheet)
    if args.export:
        output = Path("assets/ui/rarities")
        export(images, output, "generate_rarities")
        manifest = {"layout": "horizontal", "fps": 12, "card_margin": 3,
                    "rarities": [p["id"] for p in PALETTE], "legendary_frames": 4, "jump_frames": 6}
        (output / "rarities_manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    print(f"[generate_rarities] planche : {args.sheet}")


if __name__ == "__main__":
    main()
