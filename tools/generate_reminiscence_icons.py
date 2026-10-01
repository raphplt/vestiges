"""Plan 25 S8 : quatorze fragments teintés, planche 32/16 px ; --export après validation."""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from tools.sprites.perks.icons import build
from tools.sprites.perks.plan25 import ALL_ICONS, FAMILY_NAMES, NAMES, PROPOSED
from tools.sprites.ui.kit import board, export
from tools.sprites.weapons.icons import render_icon


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sheet", type=Path, default=Path("doc/plans/planches/25-s8-reminiscences.png"))
    parser.add_argument("--export", action="store_true")
    args = parser.parse_args()
    rows, images, entries = [], {}, []
    proposed = {icon.perk_id for icon in PROPOSED}
    for icon in ALL_ICONS:
        model = build(icon)
        large = render_icon(model)
        small = render_icon(model, 16, 14, (.50, .46, .42, .38))
        images[model.stem], images[model.stem + "_16"] = large, small
        rows.append((f"{NAMES[icon.perk_id]} / {FAMILY_NAMES[icon.family]}", [large, small]))
        entries.append({"id": icon.perk_id, "name": NAMES[icon.perk_id], "family": icon.family,
                        "icon": model.stem + ".png", "icon_16": model.stem + "_16.png",
                        "proposed_id": icon.perk_id in proposed})
        print(f"[generate_reminiscence_icons] {icon.perk_id}", flush=True)
    board(rows, args.sheet, columns=3)
    if args.export:
        output = Path("assets/perks/icons")
        export(images, output, "generate_reminiscence_icons")
        (output / "reminiscences_manifest.json").write_text(json.dumps({"icons": entries}, ensure_ascii=False, indent=2) + "\n")
    print(f"[generate_reminiscence_icons] planche : {args.sheet}")


if __name__ == "__main__":
    main()
