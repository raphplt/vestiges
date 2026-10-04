"""
Icônes d'armes 32×32 du pipeline procédural (plan 17, lot 2B) : une par arme, écrite dans assets/weapons/icons/.

Usage :
    python3 tools/generate_weapon_icons.py                        # écrit les 24 icônes
    python3 tools/generate_weapon_icons.py --sheet planche.png    # et une planche de contrôle (taille réelle et ×6)

Chaque modèle de tools/sprites/weapons/icons.py nomme l'arme de data/weapons/weapons.json dont il est l'icône ;
le champ "sprite" de l'arme pointe sur le fichier écrit ici.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from tools.sprites.weapons.icons import ICON_SIZE, catalog, render_icon  # noqa: E402

OUTPUT = Path("assets/weapons/icons")


def write_icons() -> list[tuple[str, Image.Image]]:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    icons = []
    for model in catalog():
        icon = render_icon(model)
        icon.save(OUTPUT / f"{model.stem}.png")
        icons.append((model.stem, icon))
    print(f"[generate_weapon_icons] {len(icons)} icônes dans {OUTPUT}/")
    return icons


def write_sheet(path: Path, icons: list[tuple[str, Image.Image]], scale: int) -> None:
    """Six colonnes : chaque icône agrandie sans lissage, et à taille réelle en dessous, sur fond de HUD."""
    columns = 6
    size = ICON_SIZE
    cell = size * scale + 24
    rows = (len(icons) + columns - 1) // columns
    sheet = Image.new("RGBA", (cell * columns, (cell + size + 12) * rows), (26, 26, 46, 255))
    for index, (_, icon) in enumerate(icons):
        x = (index % columns) * cell + 12
        y = (index // columns) * (cell + size + 12) + 12
        sheet.alpha_composite(icon.resize((size * scale, size * scale), Image.NEAREST), (x, y))
        sheet.alpha_composite(icon, (x, y + size * scale + 6))
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)
    print(f"[generate_weapon_icons] planche : {path}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--sheet", type=Path)
    parser.add_argument("--scale", type=int, default=6)
    args = parser.parse_args()
    icons = write_icons()
    if args.sheet:
        write_sheet(args.sheet, icons, args.scale)


if __name__ == "__main__":
    main()
