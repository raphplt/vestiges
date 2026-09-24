"""
Génère les sprites d'un personnage (8 directions × actions) avec le pipeline procédural commun tools/sprites.

Usage :
    python3 tools/generate_character.py vagabond                  # remplace assets/characters/<id>/
    python3 tools/generate_character.py vagabond --sheet out.png  # planche de contrôle ×4 en plus
    python3 tools/generate_character.py vagabond --dry-run --sheet out.png

Les PNG du dossier qui ne sont pas réécrits (ancien nombre de frames) sont retirés avec leur .import :
CharacterSpriteLoader les chargerait à la suite des nouvelles frames.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from tools.sprites.models import character  # noqa: E402
from tools.sprites.render import screen_direction_to_yaw  # noqa: E402

DIRECTIONS = {
    "E": (1, 0), "SE": (1, 1), "S": (0, 1), "SW": (-1, 1),
    "W": (-1, 0), "NW": (-1, -1), "N": (0, -1), "NE": (1, -1),
}
ACTIONS = ("idle", "walk", "dash", "hurt", "death")


def generate(character_id: str, output: Path | None, sheet: Path | None, scale: int) -> None:
    model = character(character_id)
    written: set[Path] = set()
    frames: dict[tuple[str, str], list[Image.Image]] = {}
    for direction, (dx, dy) in DIRECTIONS.items():
        yaw = screen_direction_to_yaw(dx, dy)
        for action in ACTIONS:
            images = [model.render(pose, yaw) for pose in model.animations[action]]
            frames[(direction, action)] = images
            if output is not None:
                output.mkdir(parents=True, exist_ok=True)
                for index, image in enumerate(images, start=1):
                    path = output / f"char_{character_id}_{direction}_{action}_{index:02d}.png"
                    image.save(path)
                    written.add(path)
        print(f"[generate_character] {character_id} {direction} : ok", flush=True)

    if output is not None:
        _remove_orphans(output, written)
    if sheet is not None:
        write_sheet(frames, sheet, scale, ACTIONS, model.frame_size, model.pivot)


def _remove_orphans(folder: Path, written: set[Path]) -> None:
    """Retire les PNG d'un ancien nombre de frames : le chargeur les jouerait à la suite des nouvelles.
    Les fichiers réécrits gardent leur .import, donc leur uid."""
    for path in folder.glob("*.png"):
        if path not in written:
            path.unlink()
            path.with_name(path.name + ".import").unlink(missing_ok=True)
            print(f"[generate_character] orphelin retiré : {path.name}")


def write_sheet(frames: dict[tuple[str, str], list[Image.Image]], path: Path, scale: int, actions: tuple[str, ...],
                size: tuple[int, int], pivot: tuple[float, float]) -> None:
    columns = sum(len(frames[("E", action)]) for action in actions)
    cell_w, cell_h = size[0] * scale, size[1] * scale
    label = 28
    sheet = Image.new("RGBA", (label + columns * cell_w, label + len(DIRECTIONS) * cell_h), (58, 66, 48, 255))
    draw = ImageDraw.Draw(sheet)
    column = 0
    for action in actions:
        draw.text((label + column * cell_w + 4, 6), action, fill=(232, 224, 212, 255))
        column += len(frames[("E", action)])
    for row, direction in enumerate(DIRECTIONS):
        draw.text((4, label + row * cell_h + cell_h // 2), direction, fill=(232, 224, 212, 255))
        column = 0
        for action in actions:
            for image in frames[(direction, action)]:
                x, y = label + column * cell_w, label + row * cell_h
                # Losange de sol sous le pivot : vérifie l'ancrage des pieds sur toutes les frames.
                cx, cy = x + int(pivot[0] * scale), y + int(pivot[1] * scale)
                draw.polygon([(cx - 8 * scale, cy), (cx, cy - 4 * scale), (cx + 8 * scale, cy), (cx, cy + 4 * scale)],
                             fill=(70, 82, 56, 255))
                big = image.resize((cell_w, cell_h), Image.NEAREST)
                sheet.alpha_composite(big, (x, y))
                column += 1
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)
    print(f"[sprites] planche : {path}")


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
