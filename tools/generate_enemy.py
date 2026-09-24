"""
Génère les sprites d'une créature (8 directions × actions) avec le pipeline procédural commun tools/sprites.

Usage :
    python3 tools/generate_enemy.py rodeur                  # remplace assets/enemies/<id>/
    python3 tools/generate_enemy.py rodeur --sheet out.png  # planche de contrôle ×4 en plus
    python3 tools/generate_enemy.py rodeur --dry-run --sheet out.png

Le dossier de sortie est vidé de ses PNG (et de leurs .import) avant écriture : les anciens sprites à quatre
directions et d'autres dimensions ne doivent pas se mélanger aux nouveaux dans le chargeur.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from tools.generate_character import DIRECTIONS, write_sheet  # noqa: E402
from tools.sprites.models import creature  # noqa: E402
from tools.sprites.render import screen_direction_to_yaw  # noqa: E402

ACTIONS = ("idle", "walk", "attack", "death")


def generate(enemy_id: str, output: Path | None, sheet: Path | None, scale: int) -> None:
    model = creature(enemy_id)
    if output is not None:
        _clear_pngs(output)
    frames: dict[tuple[str, str], list[Image.Image]] = {}
    for direction, (dx, dy) in DIRECTIONS.items():
        yaw = screen_direction_to_yaw(dx, dy)
        for action in ACTIONS:
            images = [model.render(state, yaw) for state in model.animations[action]]
            frames[(direction, action)] = images
            if output is not None:
                for index, image in enumerate(images, start=1):
                    image.save(output / f"enemy_{enemy_id}_{direction}_{action}_{index:02d}.png")
        print(f"[generate_enemy] {enemy_id} {direction} : ok", flush=True)

    if sheet is not None:
        write_sheet(frames, sheet, scale, ACTIONS, model.frame_size, model.pivot)


def _clear_pngs(folder: Path) -> None:
    folder.mkdir(parents=True, exist_ok=True)
    removed = 0
    for path in list(folder.glob("*.png")) + list(folder.glob("*.png.import")):
        path.unlink()
        removed += 1
    if removed:
        print(f"[generate_enemy] {removed} fichiers remplacés dans {folder}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("enemy")
    parser.add_argument("--sheet", type=Path)
    parser.add_argument("--scale", type=int, default=4)
    parser.add_argument("--dry-run", action="store_true", help="n'écrit pas dans assets/")
    args = parser.parse_args()
    output = None if args.dry_run else Path("assets/enemies") / args.enemy
    generate(args.enemy, output, args.sheet, args.scale)


if __name__ == "__main__":
    main()
