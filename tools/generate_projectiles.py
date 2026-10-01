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

from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from tools.sprites.projectiles import ENEMY_IDS, MODELS, ProjectileModel  # noqa: E402
from tools.sprites.render import LAYER_NAMES, flatten, render_layers, screen_direction_to_yaw  # noqa: E402
from tools.sprites.retouch import create_source, save_unless_locked  # noqa: E402
from tools.sprites.ui.kit import BIOMES, FONT, ground_patch  # noqa: E402

OUTPUT = Path("assets/vfx/projectiles")


def render_model(model: ProjectileModel) -> list[Image.Image]:
    """Planche du projectile, un calque par entrée de LAYER_NAMES."""
    width, height = model.frame_size
    pivot = (width / 2, height / 2)
    sheets = [Image.new("RGBA", (width * model.directions, height * model.frames), (0, 0, 0, 0)) for _ in LAYER_NAMES]
    for direction in range(model.directions):
        angle = direction * math.tau / model.directions
        yaw = screen_direction_to_yaw(math.cos(angle), math.sin(angle)) if model.directions > 1 else (0.0 if model.outline else 0.6)
        for frame in range(model.frames):
            layers = render_layers(model.parts(frame), model.materials, yaw, model.frame_size, pivot, ray_range=40.0)
            if model.outline:
                outline = Image.new("RGBA", model.frame_size, model.outline)
                outline.putalpha(layers[-1].getchannel("A"))
                layers[-1] = outline
            if model.opacity:
                for layer in layers:
                    layer.putalpha(layer.getchannel("A").point(lambda a: a * model.opacity[frame] // 255))
            if model.dissolve and frame >= 2:
                # Effilochage déterministe de fin d'impact ; ne touche pas la silhouette du projectile dangereux.
                for layer in layers:
                    for y in range(height):
                        for x in range(width):
                            if (x + y * 3) % 5 < frame - 1:
                                layer.putpixel((x, y), (0, 0, 0, 0))
            for sheet, layer in zip(sheets, layers):
                sheet.alpha_composite(layer, (direction * width, frame * height))
    return sheets


def write_biome_sheet(sheets: dict[str, Image.Image], path: Path) -> None:
    """Cinq sols réels ; apparition, vol, impact aux tailles natives et un vol ×4."""
    cell = (240, 178)
    contact = Image.new("RGBA", (cell[0] * 5, cell[1] * 5), (26, 26, 46, 255))
    font = ImageFont.truetype(FONT, 14)
    draw = ImageDraw.Draw(contact)
    for row, name in enumerate(ENEMY_IDS):
        for col, biome in enumerate(BIOMES):
            x, y = col * cell[0], row * cell[1]
            patch = ground_patch(biome, (240, 150))
            model = MODELS[name]()
            flight = sheets[name].crop((0, 0, 32, 24))
            ImageDraw.Draw(patch).ellipse((26, 69, 78, 95), fill=(26, 26, 46, 150))
            patch.alpha_composite(flight.resize((128, 96), Image.Resampling.NEAREST), (0, 0))
            for index, phase in enumerate((f"{name}_appear", name, f"{name}_impact")):
                phase_model = MODELS[phase]()
                for frame in range(phase_model.frames):
                    w, h = phase_model.frame_size
                    sprite = sheets[phase].crop((0, frame * h, w, (frame + 1) * h))
                    patch.alpha_composite(sprite, (102 + frame * 32, 10 + index * 38))
            contact.alpha_composite(patch, (x, y + 25))
            draw.text((x + 6, y + 4), f"{name} / {biome}", font=font, fill="#E8E0D4")
    path.parent.mkdir(parents=True, exist_ok=True)
    contact.save(path)


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
    parser.add_argument("--enemies", action="store_true", help="les cinq familles ennemies et leurs effets")
    parser.add_argument("--biome-sheet", type=Path)
    parser.add_argument("--editable", action="store_true", help="crée la retouche Aseprite de la planche (art/retouches/)")
    args = parser.parse_args()

    ids = ([key for name in ENEMY_IDS for key in (name, f"{name}_appear", f"{name}_impact")]
           if args.enemies else args.ids or list(MODELS))
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
        if projectile_id in ENEMY_IDS:
            manifest[projectile_id].update(appearance=f"{projectile_id}_appear", impact=f"{projectile_id}_impact",
                                           shadow_width=16, loop=projectile_id in ("spit", "bile"))
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
    if args.biome_sheet is not None:
        if not args.enemies:
            parser.error("--biome-sheet nécessite --enemies")
        write_biome_sheet(sheets, args.biome_sheet)


if __name__ == "__main__":
    main()
