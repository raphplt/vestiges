"""Plan 25 : modèles d'objets en 32/16 px ; --export après validation de la planche."""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from tools.sprites.items.icons import PROPERTY_ITEMS
from tools.sprites.items.triggers import TRIGGER_ITEMS, WORLD_ITEMS, WORLD_NAMES
from tools.sprites.ui.kit import board, export
from tools.sprites.weapons.icons import render_icon


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sheet", type=Path)
    parser.add_argument("--lot", choices=("s2", "s3", "all"), default="s2")
    parser.add_argument("--export", action="store_true")
    parser.add_argument("--ids", nargs="*")
    args = parser.parse_args()
    args.sheet = args.sheet or Path(f"doc/plans/planches/25-{args.lot}-objets.png")
    names = {entry["id"]: entry["name"] for entry in json.loads(Path("data/progression/passive_souvenirs.json").read_text())}
    names.update(WORLD_NAMES)
    factories = PROPERTY_ITEMS if args.lot == "s2" else TRIGGER_ITEMS + WORLD_ITEMS
    if args.lot == "all":
        factories = PROPERTY_ITEMS + TRIGGER_ITEMS + WORLD_ITEMS
    rows, images = [], {}
    for factory in factories:
        item = factory()
        if args.ids and item.item_id not in args.ids:
            continue
        model = item.model()
        icon = render_icon(model, 32, 30, (.9, .8, .7, .6))
        small = render_icon(model, 16, 14, (.42, .38, .34, .3))
        images[model.stem], images[model.stem + "_16"] = icon, small
        rows.append((names[item.item_id], [icon, small]))
        print(f"[generate_item_icons] {item.item_id}", flush=True)
    if not rows:
        parser.error("aucun objet ne correspond aux identifiants")
    # Armes réellement utilisées en jeu, pour comparer densité et lumière dans la même planche.
    refs = [Image.open(Path("assets/weapons/icons") / f"weapon_icon_{name}.png").convert("RGBA") for name in ("sickle", "nail_gun", "music_box")]
    rows.append(("Référence : armes validées", refs))
    board(rows, args.sheet, columns=3)
    if args.export:
        export(images, Path("assets/items/icons"), "generate_item_icons")
    print(f"[generate_item_icons] planche : {args.sheet}")


if __name__ == "__main__":
    main()
