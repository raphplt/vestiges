"""
Génère les sprites d'un personnage (8 directions × actions) avec le pipeline procédural commun tools/sprites.

Usage :
    python3 tools/generate_character.py vagabond                  # écrit assets/characters/<id>/
    python3 tools/generate_character.py vagabond --sheet out.png  # planche de contrôle ×4 en plus
    python3 tools/generate_character.py vagabond --dry-run --sheet out.png
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

DIRECTIONS = {
    "E": (1, 0), "SE": (1, 1), "S": (0, 1), "SW": (-1, 1),
    "W": (-1, 0), "NW": (-1, -1), "N": (0, -1), "NE": (1, -1),
}
ACTIONS = ("idle", "walk", "dash", "hurt", "death")
SIZE = (48, 64)
PIVOT = (24.0, 60.0)


def generate(character_id: str, output: Path | None, sheet: Path | None, scale: int) -> None:
    module = importlib.import_module(f"tools.sprites.characters.{character_id}")
    frames: dict[tuple[str, str], list[Image.Image]] = {}
    for direction, (dx, dy) in DIRECTIONS.items():
        yaw = screen_direction_to_yaw(dx, dy)
        for action in ACTIONS:
            images = []
            for pose in module.ANIMATIONS[action]:
                skeleton = build_skeleton(pose, module.DIMENSIONS)
                images.append(render(module.build(skeleton), module.MATERIALS, yaw, SIZE, PIVOT))
            frames[(direction, action)] = images
            if output is not None:
                output.mkdir(parents=True, exist_ok=True)
                for index, image in enumerate(images, start=1):
                    image.save(output / f"char_{character_id}_{direction}_{action}_{index:02d}.png")
        print(f"[generate_character] {character_id} {direction} : ok", flush=True)

    if sheet is not None:
        _write_sheet(frames, sheet, scale)


def _write_sheet(frames: dict[tuple[str, str], list[Image.Image]], path: Path, scale: int) -> None:
    columns = sum(len(frames[("E", action)]) for action in ACTIONS)
    cell_w, cell_h = SIZE[0] * scale, SIZE[1] * scale
    label = 28
    sheet = Image.new("RGBA", (label + columns * cell_w, label + len(DIRECTIONS) * cell_h), (58, 66, 48, 255))
    draw = ImageDraw.Draw(sheet)
    column = 0
    for action in ACTIONS:
        draw.text((label + column * cell_w + 4, 6), action, fill=(232, 224, 212, 255))
        column += len(frames[("E", action)])
    for row, direction in enumerate(DIRECTIONS):
        draw.text((4, label + row * cell_h + cell_h // 2), direction, fill=(232, 224, 212, 255))
        column = 0
        for action in ACTIONS:
            for image in frames[(direction, action)]:
                x, y = label + column * cell_w, label + row * cell_h
                # Losange de sol sous le pivot : vérifie l'ancrage des pieds sur toutes les frames.
                cx, cy = x + int(PIVOT[0] * scale), y + int(PIVOT[1] * scale)
                draw.polygon([(cx - 12 * scale, cy), (cx, cy - 6 * scale), (cx + 12 * scale, cy), (cx, cy + 6 * scale)],
                             fill=(70, 82, 56, 255))
                big = image.resize(SIZE if scale == 1 else (cell_w, cell_h), Image.NEAREST)
                sheet.alpha_composite(big, (x, y))
                column += 1
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)
    print(f"[generate_character] planche : {path}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("character")
    parser.add_argument("--sheet", type=Path)
    parser.add_argument("--scale", type=int, default=4)
    parser.add_argument("--dry-run", action="store_true", help="n'écrit pas dans assets/")
    args = parser.parse_args()
    output = None if args.dry_run else Path("assets/characters") / args.character
    generate(args.character, output, args.sheet, args.scale)


if __name__ == "__main__":
    main()
