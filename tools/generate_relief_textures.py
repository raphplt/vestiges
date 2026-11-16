"""
Textures pixel art du relief 3D (plan 60, §235) : le sol texturé du bac à sable de la verticalité, à la façon de
Minecraft et de Megabonk, des pixels francs (filtre au plus proche) plaqués sur la géométrie.

Chaque texture se répète sans couture et couvre 4 × 4 cellules, à 16 px par cellule (§236 : à 32, la densité des tuiles
du jeu, le sol était trop détaillé ; le détail vient plutôt des touffes d'herbe en relief posées dessus) : bruit périodique ramené à quatre tons, puis les motifs des sols du jeu (touffes, feuilles, cailloux, fissures,
tools/sprites/ground_motifs.py) semés en plaques et repliés sur les bords. Les tons partent des couleurs du relief low
poly (data/sandbox/relief.json, bloc lowpoly.colors), en rampe à ombres froides et lumières chaudes (charte §4).

Dessus : sol (herbe sombre), terrasse (herbe claire), butte (roche), marches (terre battue). Flancs : terre en strates,
roche en blocs. Franges : le bord d'herbe qui déborde sur le haut des falaises, transparent dessous (comme le côté d'un
bloc d'herbe de Minecraft), une par herbe.

Usage : python3 tools/generate_relief_textures.py [--sheet planche.png]
Sortie : assets/models/textures/relief_<nom>.png (64 px, 16 px par cellule)
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "tools"))

from sprites.ground import Stipple  # noqa: E402
from sprites.ground_motifs import cracks, grass, litter, stones  # noqa: E402
from sprites.palette import _shift, hex_to_rgb  # noqa: E402

CELLS = 4
PX_PER_CELL = 16
SIZE = CELLS * PX_PER_CELL
# La frange pend sur une demi-cellule.
LIP_HEIGHT = PX_PER_CELL // 2
OUT = ROOT / "assets" / "models" / "textures"

Rgb = tuple[int, int, int]


def to_hex(rgb: Rgb) -> str:
    return "#" + "".join(f"{c:02x}" for c in rgb)


def ramp(base: str, spread: float = 1.0) -> tuple[str, str, str, str]:
    """Quatre tons autour d'une couleur : ombre froide vers le violet, lumière chaude vers le jaune."""
    rgb = hex_to_rgb(base)
    return (
        to_hex(_shift(rgb, 1 - 0.30 * spread, 0.95, -0.025 * spread)),
        to_hex(_shift(rgb, 1 - 0.13 * spread, 1.0, -0.01 * spread)),
        to_hex(rgb),
        to_hex(_shift(rgb, 1 + 0.16 * spread, 1.05, 0.015 * spread)),
    )


def periodic_noise(rng: np.random.Generator, period: tuple[int, int]) -> np.ndarray:
    """Bruit de valeur lissé qui se répète exactement sur la texture : `period` mailles en x et en y."""
    px, py = period
    grid = rng.random((py, px))
    ys, xs = np.mgrid[0:SIZE, 0:SIZE].astype(float)
    fx = (xs + 0.5) * px / SIZE
    fy = (ys + 0.5) * py / SIZE
    ix = np.floor(fx).astype(int)
    iy = np.floor(fy).astype(int)
    tx = fx - ix
    ty = fy - iy
    tx = tx * tx * (3 - 2 * tx)
    ty = ty * ty * (3 - 2 * ty)
    x0, x1 = ix % px, (ix + 1) % px
    y0, y1 = iy % py, (iy + 1) % py
    top = grid[y0, x0] * (1 - tx) + grid[y0, x1] * tx
    bottom = grid[y1, x0] * (1 - tx) + grid[y1, x1] * tx
    return top * (1 - ty) + bottom * ty


def field(rng: np.random.Generator, feature_px: float, stretch: tuple[float, float] = (1.0, 1.0)) -> np.ndarray:
    """Champ de fond dans [0, 1] : grandes formes et un octave plus fin."""
    def period(scale: float) -> tuple[int, int]:
        return (max(1, round(SIZE / (feature_px * scale * stretch[0]))), max(1, round(SIZE / (feature_px * scale * stretch[1]))))
    value = periodic_noise(rng, period(1.0)) * 0.7 + periodic_noise(rng, period(0.45)) * 0.3
    low, high = value.min(), value.max()
    return (value - low) / max(high - low, 1e-6)


def quantize(values: np.ndarray, tones: tuple[str, ...], shares: tuple[float, ...]) -> np.ndarray:
    """Chaque pixel prend un des tons selon la part de surface visée pour chacun (quantiles du champ)."""
    cuts = np.quantile(values, np.cumsum(shares)[:-1])
    index = np.searchsorted(cuts, values)
    palette = np.array([hex_to_rgb(t) for t in tones], dtype=np.uint8)
    return palette[index]


def sow(image: np.ndarray, background: np.ndarray, stipples: tuple[Stipple, ...], rng: np.random.Generator) -> None:
    """Sème les motifs, repliés sur les bords : un motif qui déborde à droite reprend à gauche."""
    for stipple in stipples:
        count = rng.poisson(stipple.density * SIZE * SIZE)
        height = len(stipple.pattern)
        colors = [hex_to_rgb(c) for c in stipple.colors]
        low, high = stipple.field_range
        for _ in range(count):
            ax, ay = int(rng.integers(SIZE)), int(rng.integers(SIZE))
            if not low <= background[ay, ax] <= high:
                continue
            for row, line in enumerate(stipple.pattern):
                for col, char in enumerate(line):
                    if char == ".":
                        continue
                    y = (ay - (height - 1) + row) % SIZE
                    x = (ax + col) % SIZE
                    image[y, x] = colors[int(char)]


def speckled(rng: np.random.Generator, background: np.ndarray, amount: float) -> np.ndarray:
    """Grain au pixel mêlé aux grandes formes : les tons se mélangent en points, comme une texture de Minecraft."""
    return background * (1 - amount) + rng.random(background.shape) * amount


def top_texture(base: str, feature_px: float, shares: tuple[float, ...], stipples: tuple[Stipple, ...], seed: int,
                spread: float = 1.0, grain: float = 0.3) -> np.ndarray:
    rng = np.random.default_rng(seed)
    background = field(rng, feature_px)
    image = quantize(speckled(rng, background, grain), ramp(base, spread), shares)
    sow(image, background, stipples, rng)
    return image


def strata_texture(base: str, dark: str, stipples: tuple[Stipple, ...], seed: int) -> np.ndarray:
    """Flanc de terre : strates ondulées qui alternent deux matières tous les demi-étages, cailloux et racines."""
    rng = np.random.default_rng(seed)
    grain = field(rng, 5.0, (3.0, 0.5))
    wobble = field(rng, 14.0, (2.0, 1.0))
    ys = np.mgrid[0:SIZE, 0:SIZE][0].astype(float)
    # Six strates sur la hauteur : la texture couvre quatre cellules, soit environ six étages de 0,7 cellule.
    band = np.sin((ys / SIZE + (wobble - 0.5) * 0.06) * 2 * np.pi * 6)
    speck = speckled(rng, grain, 0.35)
    image = np.where((band > -0.25)[..., None], quantize(speck, ramp(base), (0.12, 0.4, 0.36, 0.12)),
                     quantize(speck, ramp(dark), (0.2, 0.42, 0.3, 0.08)))
    # Liseré sombre sous chaque strate claire : l'arête lit l'épaisseur.
    edge = (band > -0.25) & (np.roll(band, -1, axis=0) <= -0.25)
    image[edge] = hex_to_rgb(ramp(dark)[0])
    sow(image, grain, stipples, rng)
    return image


def blocks_texture(base: str, seed: int) -> np.ndarray:
    """Flanc de roche : blocs en appareil irrégulier, joints sombres, arête haute éclairée, quelques fissures."""
    rng = np.random.default_rng(seed)
    grain = field(rng, 5.0)
    tones = ramp(base, 1.1)
    image = quantize(speckled(rng, grain, 0.3), tones, (0.12, 0.45, 0.33, 0.10))
    joint = np.array(hex_to_rgb(_shift_hex(tones[0], 0.7)), dtype=np.uint8)
    light = np.array(hex_to_rgb(tones[3]), dtype=np.uint8)
    course = PX_PER_CELL // 2
    for row in range(SIZE // course):
        y0 = row * course
        image[y0, :] = joint
        image[(y0 + 1) % SIZE, :] = light
        x = int(rng.integers(0, 12))
        while x < SIZE + 12:
            image[y0:y0 + course, x % SIZE] = joint
            image[(y0 + 2):(y0 + course), (x + 1) % SIZE] = light
            x += int(rng.integers(9, 20))
    sow(image, grain, cracks(tones[0], 0.8, 0.7), rng)
    return image


def _shift_hex(value: str, lightness: float) -> str:
    return to_hex(_shift(hex_to_rgb(value), lightness, 1.0, 0.0))


def lip_texture(base: str, seed: int) -> np.ndarray:
    """Frange d'herbe : bande pleine en haut, mèches qui pendent, liseré d'ombre dessous, transparent ailleurs."""
    rng = np.random.default_rng(seed)
    tones = ramp(base)
    image = np.zeros((LIP_HEIGHT, SIZE, 4), dtype=np.uint8)
    hang = periodic_noise(rng, (8, 1))[0]
    for x in range(SIZE):
        length = 2 + int(hang[x] * 3)
        if rng.random() < 0.12:
            length += int(rng.integers(1, 3))
        length = min(length, LIP_HEIGHT - 1)
        for y in range(length + 1):
            tone = tones[3] if y == 0 else tones[2] if y < length - 1 else tones[1]
            if y == length:
                tone = tones[0]
            image[y, x, :3] = hex_to_rgb(tone)
            image[y, x, 3] = 255
        if rng.random() < 0.18:
            image[1, x, :3] = hex_to_rgb(tones[1])
    return image


def build(colors: dict[str, str]) -> dict[str, np.ndarray]:
    grass_shade = ramp(colors["ground"], 1.4)
    grass_field = ramp(colors["terrace"], 1.4)
    leaf = ("#2a2014", "#5a4528", "#7c6036", "#9a7a44")
    stone_warm = ramp(colors["butte"], 1.5)
    stone_soil = ramp("8a7a64", 1.5)
    root = ("#2a1e16", "#3e2c1e", "#4e3a26", "#5e4630")
    return {
        "sol": top_texture(colors["ground"], 13.0, (0.14, 0.40, 0.34, 0.12),
                           grass(grass_shade, 0.6, 0.4) + litter(leaf, 0.3), 6001),
        "terrasse": top_texture(colors["terrace"], 15.0, (0.12, 0.38, 0.36, 0.14),
                                grass(grass_field, 0.6, 0.35), 6002),
        "butte": top_texture(colors["butte"], 10.0, (0.14, 0.42, 0.32, 0.12),
                             stones(stone_warm, 0.8, 0.7) + cracks(ramp(colors["butte"])[0], 0.8, 0.6), 6003, 1.2),
        "marches": top_texture(colors["steps"], 9.0, (0.14, 0.4, 0.34, 0.12),
                               stones(stone_soil, 0.6, 0.6), 6004, 1.1),
        "flanc_terre": strata_texture(colors["cliff"], colors["cliff_dark"],
                                      stones(stone_soil, 0.6, 1.0) + litter(root, 0.2, 1.0), 6005),
        "flanc_roche": blocks_texture(colors["butte"], 6006),
        "frange_sol": lip_texture(colors["ground"], 6007),
        "frange_terrasse": lip_texture(colors["terrace"], 6008),
    }


def sheet(textures: dict[str, np.ndarray], path: Path) -> None:
    """Planche : chaque texture pavée 2 × 2 (les raccords se voient), agrandie six fois."""
    tiles = []
    for name, image in textures.items():
        rgb = image[..., :3] if image.shape[-1] == 4 else image
        tiled = np.tile(rgb, (2, 2, 1))
        tiles.append(Image.fromarray(tiled).resize((tiled.shape[1] * 6, tiled.shape[0] * 6), Image.NEAREST))
    width = sum(t.width for t in tiles) + 8 * len(tiles)
    height = max(t.height for t in tiles)
    out = Image.new("RGB", (width, height), (11, 8, 18))
    x = 0
    for tile in tiles:
        out.paste(tile, (x, 0))
        x += tile.width + 8
    out.save(path)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--sheet", type=Path, help="planche de contrôle des raccords")
    args = parser.parse_args()
    config = json.loads((ROOT / "data" / "sandbox" / "relief.json").read_text(encoding="utf-8"))
    textures = build(config["lowpoly"]["colors"])
    OUT.mkdir(parents=True, exist_ok=True)
    for name, image in textures.items():
        mode = "RGBA" if image.shape[-1] == 4 else "RGB"
        Image.fromarray(image, mode).save(OUT / f"relief_{name}.png")
        print(f"{OUT.relative_to(ROOT)}/relief_{name}.png {image.shape[1]}×{image.shape[0]}")
    if args.sheet:
        sheet(textures, args.sheet)
        print(f"planche : {args.sheet}")


if __name__ == "__main__":
    main()
