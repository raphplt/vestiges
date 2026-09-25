"""
Génère les sprites d'une créature (8 directions × actions) avec le pipeline procédural commun tools/sprites.

Usage :
    python3 tools/generate_enemy.py rodeur                  # remplace assets/enemies/<id>/
    python3 tools/generate_enemy.py rodeur --sheet out.png  # planche de contrôle ×4 en plus
    python3 tools/generate_enemy.py rodeur --dry-run --sheet out.png

Les PNG réécrits gardent leur .import (et leur uid). Les anciens sprites qui ne sont pas réécrits (quatre directions,
autres dimensions) sont retirés : ils se mélangeraient aux nouveaux dans le chargeur. Une retouche Aseprite
(art/retouches/, voir tools/sprites/retouch.py) n'est jamais écrasée ; --editable la crée.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from tools.generate_character import DIRECTIONS, write_sheet  # noqa: E402
from tools.sprites.models import creature  # noqa: E402
from tools.sprites.render import flatten, screen_direction_to_yaw  # noqa: E402
from tools.sprites.retouch import create_source, is_locked, save_unless_locked  # noqa: E402

ACTIONS = ("idle", "walk", "attack", "death")


def generate(enemy_id: str, output: Path | None, sheet: Path | None, scale: int, editable: bool = False) -> None:
    model = creature(enemy_id)
    written: set[Path] = set()
    frames: dict[tuple[str, str], list[Image.Image]] = {}
    for direction, (dx, dy) in DIRECTIONS.items():
        yaw = screen_direction_to_yaw(dx, dy)
        for action in ACTIONS:
            layered = [model.render_layers(state, yaw) for state in model.animations[action]]
            images = [flatten(layers) for layers in layered]
            frames[(direction, action)] = images
            if output is not None:
                paths = [output / f"enemy_{enemy_id}_{direction}_{action}_{index:02d}.png"
                         for index in range(1, len(images) + 1)]
                output.mkdir(parents=True, exist_ok=True)
                for image, path in zip(images, paths):
                    save_unless_locked(image, path, "generate_enemy")
                    written.add(path)
                if editable:
                    create_source(f"{output.as_posix()}/enemy_{enemy_id}_{direction}_{action}", layered, paths,
                                  "generate_enemy")
        print(f"[generate_enemy] {enemy_id} {direction} : ok", flush=True)

    if output is not None:
        _remove_orphans(output, written)
    if sheet is not None:
        write_sheet(frames, sheet, scale, ACTIONS, model.frame_size, model.pivot)


def _remove_orphans(folder: Path, written: set[Path]) -> None:
    """Retire les anciens sprites (autre nombre de directions ou de frames) : le chargeur les mélangerait aux
    nouveaux. Les fichiers réécrits et les retouches gardent leur .import, donc leur uid."""
    for path in folder.glob("*.png"):
        if path not in written and not is_locked(path):
            path.unlink()
            path.with_name(path.name + ".import").unlink(missing_ok=True)
            print(f"[generate_enemy] orphelin retiré : {path.name}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("enemy")
    parser.add_argument("--sheet", type=Path)
    parser.add_argument("--scale", type=int, default=4)
    parser.add_argument("--dry-run", action="store_true", help="n'écrit pas dans assets/")
    parser.add_argument("--editable", action="store_true",
                        help="crée les retouches Aseprite (une par direction et action) dans art/retouches/")
    args = parser.parse_args()
    output = None if args.dry_run else Path("assets/enemies") / args.enemy
    generate(args.enemy, output, args.sheet, args.scale, args.editable)


if __name__ == "__main__":
    main()
