"""
Aperçu d'un sol : pave une zone avec les tuiles d'un biome comme en jeu (grille isométrique « stacked »,
variante choisie par hachage de la cellule), pour juger les répétitions et les raccords sans lancer Godot.

Usage : python3 tools/tile_preview.py <biome> [groupe=grass] [sortie.png] [--scale 2] [--size 18]
Exemple : python3 tools/tile_preview.py forest_reclaimed grass /tmp/foret.png
"""

import argparse
import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
TILE_W, TILE_H = 64, 32


def cell_hash(x: int, y: int, salt: int = 0) -> int:
    """Même finaliseur SplitMix64 que CellHash.Of côté C#, pour retrouver les variantes du jeu."""
    mask = (1 << 64) - 1
    h = ((x & 0xFFFFFFFF) * 0x9E3779B97F4A7C15) & mask
    h ^= ((y & 0xFFFFFFFF) * 0xC2B2AE3D27D4EB4F) & mask
    h ^= (salt * 0x165667B19E3779F9) & mask
    h ^= h >> 30
    h = (h * 0xBF58476D1CE4E5B9) & mask
    h ^= h >> 27
    h = (h * 0x94D049BB133111EB) & mask
    h ^= h >> 31
    return h & 0x7FFFFFFF


def load_group(biome: str, group: str) -> list[Image.Image]:
    data = json.loads((ROOT / "data" / "biomes" / f"{biome}.json").read_text())
    paths = data["tile_sources"][group]
    return [Image.open(ROOT / "assets" / "tiles" / f"{p}.png").convert("RGBA") for p in paths]


def render(tiles: list[Image.Image], size: int) -> Image.Image:
    width = size * TILE_W + TILE_W // 2
    height = size * TILE_H // 2 + TILE_H
    image = Image.new("RGBA", (width, height), (20, 20, 30, 255))
    for y in range(size * 2):
        for x in range(size):
            tile = tiles[cell_hash(x, y) % len(tiles)]
            left = x * TILE_W + (TILE_W // 2 if y & 1 else 0)
            top = y * TILE_H // 2
            image.alpha_composite(tile, (left, top))
    return image.crop((TILE_W // 2, TILE_H // 2, width - TILE_W // 2, height - TILE_H))


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("biome")
    parser.add_argument("group", nargs="?", default="grass")
    parser.add_argument("output", nargs="?", default=None)
    parser.add_argument("--scale", type=int, default=2)
    parser.add_argument("--size", type=int, default=18)
    args = parser.parse_args()

    image = render(load_group(args.biome, args.group), args.size)
    if args.scale > 1:
        image = image.resize((image.width * args.scale, image.height * args.scale), Image.NEAREST)
    output = args.output or f"/tmp/tiles-{args.biome}-{args.group}.png"
    image.save(output)
    print(output)


if __name__ == "__main__":
    main()
