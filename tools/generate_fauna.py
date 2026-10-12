"""
Génère les sprites de la faune qui fuit (plan 39 B2) avec le pipeline procédural commun : 8 directions × actions.

Usage :
    python3 tools/generate_fauna.py corbeau                   # écrit assets/fauna/<id>/
    python3 tools/generate_fauna.py corbeau --sheet out.png   # planche de contrôle ×4 en plus
    python3 tools/generate_fauna.py --all --dry-run --sheet-dir dossier/

Nommage : fauna_<id>_<DIR>_<action>_<NN>.png. Les PNG réécrits gardent leur .import (et leur uid).
"""
from __future__ import annotations

import argparse
import importlib
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from tools.generate_character import DIRECTIONS, write_sheet  # noqa: E402
from tools.sprites.models import SpriteModel  # noqa: E402
from tools.sprites.render import MODEL_SCALE, flatten, screen_direction_to_yaw  # noqa: E402

FAUNA = ("corbeau", "pigeon", "rat", "papillon", "grenouille")


def model(fauna_id: str) -> SpriteModel:
    module = importlib.import_module(f"tools.sprites.fauna.{fauna_id}")
    # Une bête trop petite se perd à côté d'un personnage de ~30 px : chacune porte son agrandissement.
    return SpriteModel(fauna_id, module.FRAME_SIZE, module.FRAME_PIVOT, MODEL_SCALE * module.SCALE, module.MATERIALS,
                       module.ANIMATIONS, module.parts)


def generate(fauna_id: str, output: Path | None, sheet: Path | None, scale: int) -> None:
    sprite = model(fauna_id)
    actions = tuple(sprite.animations)
    frames = {}
    for direction, (dx, dy) in DIRECTIONS.items():
        yaw = screen_direction_to_yaw(dx, dy)
        for action in actions:
            images = [flatten(sprite.render_layers(state, yaw)) for state in sprite.animations[action]]
            frames[(direction, action)] = images
            if output is not None:
                output.mkdir(parents=True, exist_ok=True)
                for index, image in enumerate(images, start=1):
                    image.save(output / f"fauna_{fauna_id}_{direction}_{action}_{index:02d}.png")
    if sheet is not None:
        write_sheet(frames, sheet, scale, actions, sprite.frame_size, sprite.pivot)
    print(f"[generate_fauna] {fauna_id} : {len(actions)} actions × {len(DIRECTIONS)} directions", flush=True)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("fauna", nargs="?", choices=FAUNA)
    parser.add_argument("--all", action="store_true")
    parser.add_argument("--sheet", type=Path)
    parser.add_argument("--sheet-dir", type=Path)
    parser.add_argument("--scale", type=int, default=4)
    parser.add_argument("--dry-run", action="store_true", help="n'écrit pas dans assets/")
    args = parser.parse_args()
    ids = FAUNA if args.all else (args.fauna,)
    for fauna_id in ids:
        output = None if args.dry_run else Path("assets/fauna") / fauna_id
        sheet = args.sheet if args.sheet else (args.sheet_dir / f"{fauna_id}.png" if args.sheet_dir else None)
        generate(fauna_id, output, sheet, args.scale)
    if not args.dry_run:
        write_manifest()


def write_manifest() -> None:
    """Pied et cadre de chaque espèce, lus par World/FaunaDirector : le point au sol tombe sur le pied des poses."""
    manifest = {}
    for fauna_id in FAUNA:
        sprite = model(fauna_id)
        manifest[fauna_id] = {"frame_size": list(sprite.frame_size), "pivot": list(sprite.pivot)}
    Path("assets/fauna/fauna_manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")


if __name__ == "__main__":
    main()
