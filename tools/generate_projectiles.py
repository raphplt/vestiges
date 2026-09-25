"""
Génère les sprites des projectiles du joueur avec le pipeline procédural commun tools/sprites.

Usage :
    python3 tools/generate_projectiles.py                    # tous, dans assets/vfx/projectiles/
    python3 tools/generate_projectiles.py arrow axe --sheet planche.png
    python3 tools/generate_projectiles.py --dry-run --sheet planche.png

Chaque projectile devient une planche `proj_<id>.png` : une colonne par direction d'écran (16, en partant de
l'est dans le sens horaire de l'écran), une ligne par frame. `projectiles_manifest.json` décrit le découpage
pour le jeu (Combat/ProjectileSprites.cs). Les .import existants sont conservés.
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from tools.sprites.projectiles import MODELS, ProjectileModel  # noqa: E402
from tools.sprites.render import LAYER_NAMES, flatten, render_layers, screen_direction_to_yaw  # noqa: E402
from tools.sprites.retouch import create_source, save_unless_locked  # noqa: E402

OUTPUT = Path("assets/vfx/projectiles")


def render_model(model: ProjectileModel) -> list[Image.Image]:
    """Planche du projectile, un calque par entrée de LAYER_NAMES."""
    width, height = model.frame_size
    pivot = (width / 2, height / 2)
    sheets = [Image.new("RGBA", (width * model.directions, height * model.frames), (0, 0, 0, 0)) for _ in LAYER_NAMES]
    for direction in range(model.directions):
        angle = direction * math.tau / model.directions
        yaw = screen_direction_to_yaw(math.cos(angle), math.sin(angle)) if model.directions > 1 else 0.6
        for frame in range(model.frames):
            layers = render_layers(model.parts(frame), model.materials, yaw, model.frame_size, pivot, ray_range=40.0)
            for sheet, layer in zip(sheets, layers):
                sheet.alpha_composite(layer, (direction * width, frame * height))
    return sheets


def write_contact_sheet(sheets: dict[str, Image.Image], path: Path, scale: int) -> None:
    label = 60
    rows = sum(sheet.height for sheet in sheets.values()) * scale + 8 * len(sheets)
    columns = max(sheet.width for sheet in sheets.values()) * scale
    contact = Image.new("RGBA", (label + columns, rows), (74, 92, 56, 255))
    draw = ImageDraw.Draw(contact)
    y = 0
    for name, sheet in sheets.items():
        draw.text((4, y + 4), name, fill=(232, 224, 212, 255))
        contact.alpha_composite(sheet.resize((sheet.width * scale, sheet.height * scale), Image.NEAREST), (label, y))
        y += sheet.height * scale + 8
    path.parent.mkdir(parents=True, exist_ok=True)
    contact.save(path)
    print(f"[generate_projectiles] planche : {path}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("ids", nargs="*", help="projectiles à générer (tous par défaut)")
    parser.add_argument("--sheet", type=Path)
    parser.add_argument("--scale", type=int, default=4)
    parser.add_argument("--dry-run", action="store_true", help="n'écrit pas dans assets/")
    parser.add_argument("--editable", action="store_true", help="crée la retouche Aseprite de la planche (art/retouches/)")
    args = parser.parse_args()

    ids = args.ids or list(MODELS)
    manifest_path = OUTPUT / "projectiles_manifest.json"
    manifest = json.loads(manifest_path.read_text()) if manifest_path.exists() else {}
    sheets = {}
    for projectile_id in ids:
        model = MODELS[projectile_id]()
        layers = render_model(model)
        sheet = flatten(layers)
        sheets[projectile_id] = sheet
        manifest[projectile_id] = {
            "frame": list(model.frame_size),
            "directions": model.directions,
            "frames": model.frames,
            "fps": model.fps,
        }
        if not args.dry_run:
            target = OUTPUT / f"proj_{projectile_id}.png"
            save_unless_locked(sheet, target, "generate_projectiles")
            if args.editable:
                create_source(target.with_suffix("").as_posix(), [layers], [target], "generate_projectiles")
        print(f"[generate_projectiles] {projectile_id} : {model.directions} directions × {model.frames} frames", flush=True)

    if not args.dry_run:
        manifest_path.write_text(json.dumps(dict(sorted(manifest.items())), indent=2) + "\n")
    if args.sheet is not None:
        write_contact_sheet(sheets, args.sheet, args.scale)


if __name__ == "__main__":
    main()
