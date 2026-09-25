"""
Génère les décors d'un biome avec le pipeline procédural commun (plan 08, lots P0–P6).

Usage :
    python3 tools/generate_props.py urban                        # écrit assets/props/urban_ruins/
    python3 tools/generate_props.py urban --sheet out.png        # planche de contrôle ×3 sur le sol du biome
    python3 tools/generate_props.py urban --only prop_dumpster --dry-run --sheet out.png

Seuls les fichiers du catalogue sont réécrits ; les autres décors du dossier (immeubles, lot P2) restent en place.
Godot réimporte de lui-même un PNG modifié : les .import existants (et leurs uid) sont conservés.
"""
from __future__ import annotations

import argparse
import importlib
import random
import sys
import time
from pathlib import Path

from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from tools.sprites.props._kit import render_prop  # noqa: E402

BIOMES = {
    "urban": ("tools.sprites.props.urban", "assets/props/urban_ruins",
              ["assets/tiles/ruines/tile_ruines_sol_base.png", "assets/tiles/ruines/tile_ruines_sol_v2.png",
               "assets/tiles/ruines/tile_ruines_carrelage_base.png"]),
}
SCALE_REFERENCE = "assets/characters/vagabond/char_vagabond_SE_idle_01.png"


def generate(biome: str, only: set[str], output: Path | None, sheet: Path | None, scale: int) -> None:
    module_name, folder, tiles = BIOMES[biome]
    models = [m for m in importlib.import_module(module_name).catalog() if not only or m.stem in only]
    images: list[tuple[str, Image.Image]] = []
    for model in models:
        started = time.time()
        image = render_prop(model)
        images.append((model.stem, image))
        if output is not None:
            output.mkdir(parents=True, exist_ok=True)
            target = output / f"{model.stem}.png"
            image.save(target)
        print(f"[generate_props] {model.stem} {image.width}×{image.height} ({time.time() - started:.1f} s)", flush=True)
    if sheet is not None:
        write_sheet(images, tiles, sheet, scale)


def write_sheet(images: list[tuple[str, Image.Image]], tiles: list[str], path: Path, scale: int) -> None:
    """Décors posés sur le sol réel du biome, à côté du personnage, puis agrandis sans lissage."""
    columns = 5
    cell = (128, 112)
    rows = (len(images) + columns - 1) // columns
    width, height = columns * cell[0], rows * cell[1]
    ground = Image.new("RGBA", (width, height), (0, 0, 0, 255))
    tile_images = [Image.open(t).convert("RGBA") for t in tiles]
    rng = random.Random(7)
    # Tuiles iso 64×32 en quinconce ; coller hors du cadre est permis par paste (alpha_composite l'interdit).
    for row in range(-2, height // 16 + 2):
        for col in range(-1, width // 64 + 2):
            tile = rng.choice(tile_images)
            ground.paste(tile, (col * 64 + (32 if row % 2 else 0) - 32, row * 16 - 16), tile)
    reference = Image.open(SCALE_REFERENCE).convert("RGBA")
    draw = ImageDraw.Draw(ground)
    for index, (stem, image) in enumerate(images):
        cx = (index % columns) * cell[0] + cell[0] // 2
        cy = (index // columns) * cell[1] + cell[1] - 18
        ground.alpha_composite(image, (cx - image.width // 2 + 8, cy + 4 - image.height))
        ground.alpha_composite(reference, (cx - image.width // 2 - reference.width + 4, cy + 4 - reference.height))
        draw.text((index % columns * cell[0] + 3, index // columns * cell[1] + cell[1] - 12), stem.replace("prop_", ""),
                  fill=(236, 228, 210, 255))
    big = ground.resize((width * scale, height * scale), Image.NEAREST)
    path.parent.mkdir(parents=True, exist_ok=True)
    big.save(path)
    print(f"[generate_props] planche : {path}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("biome", choices=sorted(BIOMES))
    parser.add_argument("--only", nargs="*", default=[])
    parser.add_argument("--sheet", type=Path)
    parser.add_argument("--scale", type=int, default=3)
    parser.add_argument("--dry-run", action="store_true", help="n'écrit pas dans assets/")
    args = parser.parse_args()
    output = None if args.dry_run else Path(BIOMES[args.biome][1])
    generate(args.biome, set(args.only), output, args.sheet, args.scale)


if __name__ == "__main__":
    main()
