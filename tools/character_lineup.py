"""
Planche de casting : silhouettes noires puis couleurs des personnages à taille réelle et agrandie.
Contrôle de lisibilité (Bible §11.4 : deux silhouettes qui se confondent → l'une est redessinée).

Usage : python3 tools/character_lineup.py sortie.png [--scale 4] [ids...]
"""
from __future__ import annotations

import argparse
import importlib
import sys
from pathlib import Path

from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from tools.sprites.render import render, screen_direction_to_yaw  # noqa: E402
from tools.sprites.rig import build_skeleton  # noqa: E402

CAST = ["vagabond", "traqueur", "forgeuse", "eveillee", "facteur", "scaphandriere"]
VIEWS = [("S", (0, 1)), ("SE", (1, 1)), ("E", (1, 0))]
GROUND_ANCHORED = (74, 88, 60, 255)
GROUND_ERASED = (206, 204, 214, 255)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("output", type=Path)
    parser.add_argument("--scale", type=int, default=4)
    parser.add_argument("ids", nargs="*", default=CAST)
    args = parser.parse_args()

    scale = args.scale
    cell_w, cell_h = 48 * scale, 64 * scale
    rows = [("silhouette", GROUND_ANCHORED), ("ancré", GROUND_ANCHORED), ("effacé", GROUND_ERASED)]
    width = len(args.ids) * len(VIEWS) * cell_w
    sheet = Image.new("RGBA", (width, 30 + len(rows) * cell_h + 80), (40, 44, 36, 255))
    draw = ImageDraw.Draw(sheet)

    for index, character_id in enumerate(args.ids):
        module = importlib.import_module(f"tools.sprites.characters.{character_id}")
        pose = module.ANIMATIONS["idle"][0]
        parts = module.build(build_skeleton(pose, module.DIMENSIONS))
        x0 = index * len(VIEWS) * cell_w
        draw.text((x0 + 8, 8), character_id, fill=(232, 224, 212, 255))
        for view, (label, (dx, dy)) in enumerate(VIEWS):
            sprite = render(parts, module.MATERIALS, screen_direction_to_yaw(dx, dy))
            big = sprite.resize((cell_w, cell_h), Image.NEAREST)
            for row, (kind, ground) in enumerate(rows):
                x, y = x0 + view * cell_w, 30 + row * cell_h
                draw.rectangle([x, y, x + cell_w - 1, y + cell_h - 1], fill=ground)
                if kind == "silhouette":
                    alpha = big.getchannel("A")
                    black = Image.new("RGBA", big.size, (16, 16, 24, 255))
                    black.putalpha(alpha)
                    sheet.alpha_composite(black, (x, y))
                else:
                    sheet.alpha_composite(big, (x, y))
            # Taille réelle sous la planche, pour juger la lecture au zoom de jeu.
            sheet.alpha_composite(sprite, (x0 + view * cell_w + cell_w // 2 - 24, 30 + len(rows) * cell_h + 8))

    args.output.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(args.output)
    print(f"[character_lineup] {args.output}")


if __name__ == "__main__":
    main()
