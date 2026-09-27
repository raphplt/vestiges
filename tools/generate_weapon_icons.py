"""
Icônes d'armes 32×32 du pipeline procédural (plan 17, lot 2B) : une par arme, écrite dans assets/weapons/icons/.
Chaque arme reçoit aussi sa version 16×16 portée en main (lot 2C, essai), dans assets/weapons/held/.

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

from tools.sprites.weapons.icons import HELD_SIZE, ICON_SIZE, catalog, render_held, render_icon  # noqa: E402

OUTPUT = Path("assets/weapons/icons")
HELD_OUTPUT = Path("assets/weapons/held")


def write_icons() -> list[tuple[str, Image.Image]]:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    icons = []
    for model in catalog():
        icon = render_icon(model)
        icon.save(OUTPUT / f"{model.stem}.png")
        icons.append((model.stem, icon))
    print(f"[generate_weapon_icons] {len(icons)} icônes dans {OUTPUT}/")
    return icons


def write_held() -> list[tuple[str, Image.Image]]:
    HELD_OUTPUT.mkdir(parents=True, exist_ok=True)
    sprites = []
    for model in catalog():
        stem = model.stem.replace("weapon_icon_", "weapon_held_")
        sprite = render_held(model)
        sprite.save(HELD_OUTPUT / f"{stem}.png")
        sprites.append((stem, sprite))
    print(f"[generate_weapon_icons] {len(sprites)} armes en main dans {HELD_OUTPUT}/")
    return sprites


def write_sheet(path: Path, icons: list[tuple[str, Image.Image]], scale: int, size: int = ICON_SIZE) -> None:
    """Six colonnes : chaque icône agrandie sans lissage, et à taille réelle en dessous, sur fond de HUD."""
    columns = 6
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
    held = write_held()
    if args.sheet:
        write_sheet(args.sheet, icons, args.scale)
        write_sheet(args.sheet.with_name(f"{args.sheet.stem}-main{args.sheet.suffix}"), held, args.scale * 2, HELD_SIZE)


if __name__ == "__main__":
    main()
