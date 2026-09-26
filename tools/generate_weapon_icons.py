"""
Icônes d'armes 32×32 du pipeline procédural (plan 17, pilote de style de la vague 1).

Usage :
    python3 tools/generate_weapon_icons.py --sheet planche.png   # planche de contrôle, rien n'est écrit dans assets/

Le pilote ne branche rien dans le jeu : les icônes seront écrites dans assets/ au lot 2B, une fois le style validé.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from tools.sprites.weapons.icons import ICON_SIZE, catalog, render_icon  # noqa: E402

OLD_ICONS = {"weapon_icon_sickle": "assets/weapons/item_epee_bois.png",
             "weapon_icon_nail_gun": "assets/weapons/item_arbalete_bois.png",
             "weapon_icon_music_box": "assets/weapons/item_music_box.png"}


def write_sheet(path: Path, scale: int) -> None:
    """Par icône : ancienne icône (si trouvée) et nouvelle, à taille réelle puis agrandies sans lissage, sur fond de HUD."""
    icons = [(model.stem, render_icon(model)) for model in catalog()]
    cell = ICON_SIZE * scale + 24
    sheet = Image.new("RGBA", (cell * 2 + ICON_SIZE * 2 + 60, cell * len(icons) + 20), (26, 26, 46, 255))
    draw = ImageDraw.Draw(sheet)
    for row, (stem, icon) in enumerate(icons):
        y = 10 + row * cell
        old_path = Path(OLD_ICONS.get(stem, ""))
        if old_path.is_file():
            old = Image.open(old_path).convert("RGBA").resize((ICON_SIZE, ICON_SIZE), Image.NEAREST)
            sheet.alpha_composite(old.resize((ICON_SIZE * scale, ICON_SIZE * scale), Image.NEAREST), (10, y))
        sheet.alpha_composite(icon.resize((ICON_SIZE * scale, ICON_SIZE * scale), Image.NEAREST), (cell + 10, y))
        sheet.alpha_composite(icon, (cell * 2 + 20, y + 10))
        sheet.alpha_composite(icon, (cell * 2 + 20 + ICON_SIZE + 10, y + 10))
        draw.text((cell * 2 + 20, y + ICON_SIZE + 20), stem.replace("weapon_icon_", ""), fill=(232, 224, 212, 255))
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)
    print(f"[generate_weapon_icons] planche : {path}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--sheet", type=Path, required=True)
    parser.add_argument("--scale", type=int, default=6)
    args = parser.parse_args()
    write_sheet(args.sheet, args.scale)


if __name__ == "__main__":
    main()
