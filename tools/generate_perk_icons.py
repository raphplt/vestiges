"""
Planche de proposition des icônes de perks (plan 05) : pin's émaillés, deux variantes d'émail.

Usage :
    python3 tools/generate_perk_icons.py --sheet planche.png [--scale 6]

Rien n'est écrit dans assets/ tant que la direction n'est pas validée par Raphaël.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from tools.sprites.perks.icons import CATALOG, catalog  # noqa: E402
from tools.sprites.weapons.icons import ICON_SIZE, render_icon  # noqa: E402

NAMES = {
    "priority_targeting": "Convergence", "overflow": "Débordement", "carry_control": "Propagation",
    "overheal_reserve": "Prévoyance", "rally": "Reprise", "xp_trail": "Sillage",
    "salvage_xp": "Délestage", "carried_choice": "Seconde lecture", "familiar_loot": "Habitude",
}


def write_sheet(path: Path, scale: int) -> None:
    """Une rangée par variante : chaque pin's agrandi sans lissage, à taille réelle ×1 et ×2 dessous, sur fond de HUD."""
    variants = ("family", "uniform")
    cell = ICON_SIZE * scale + 24
    row = cell + ICON_SIZE * 2 + 40
    sheet = Image.new("RGBA", (cell * len(CATALOG), row * len(variants) + 10), (26, 26, 46, 255))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.truetype("assets/fonts/saira/SairaSemiCondensed-SemiBold.ttf", 14)
    for v, variant in enumerate(variants):
        for index, model in enumerate(catalog(variant)):
            icon = render_icon(model)
            x = index * cell + 12
            y = v * row + 22
            if index == 0:
                draw.text((12, v * row + 4), f"Variante : émail {'par famille' if variant == 'family' else 'commun'}", fill=(220, 210, 190, 255), font=font)
            sheet.alpha_composite(icon.resize((ICON_SIZE * scale, ICON_SIZE * scale), Image.NEAREST), (x, y))
            sheet.alpha_composite(icon, (x, y + ICON_SIZE * scale + 6))
            sheet.alpha_composite(icon.resize((ICON_SIZE * 2, ICON_SIZE * 2), Image.NEAREST), (x + ICON_SIZE + 8, y + ICON_SIZE * scale + 6))
            draw.text((x, y + ICON_SIZE * scale + ICON_SIZE * 2 + 10), NAMES[CATALOG[index].perk_id], fill=(200, 200, 210, 255), font=font)
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)
    print(f"[generate_perk_icons] planche : {path}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--sheet", type=Path, required=True)
    parser.add_argument("--scale", type=int, default=6)
    args = parser.parse_args()
    write_sheet(args.sheet, args.scale)


if __name__ == "__main__":
    main()
