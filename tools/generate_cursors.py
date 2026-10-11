"""Plan 37 H5 : curseurs de souris pixel art (flèche et main au survol), planche agrandie ; écrit les ressources du jeu."""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from tools.sprites.ui.cursor import HOTSPOTS, assets
from tools.sprites.ui.kit import board, export


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sheet", type=Path, default=Path("doc/plans/planches/37-h5-curseurs.png"))
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    images = assets()
    zoomed = [image.resize((image.width * 4, image.height * 4), 0) for image in images.values()]
    board([("Flèche et main, échelle 1 puis ×4", list(images.values()) + zoomed)], args.sheet)
    if not args.dry_run:
        output = Path("assets/ui/cursor")
        export(images, output, "generate_cursors")
        manifest = {name.removeprefix("cursor_"): {"file": f"{name}.png", "hotspot": list(HOTSPOTS[name.removeprefix("cursor_")])}
                    for name in images}
        (output / "cursor_manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n")
    print(f"[generate_cursors] {len(images)} curseurs ; planche : {args.sheet}")


if __name__ == "__main__":
    main()
