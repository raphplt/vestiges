"""
Génère les sols en tuiles de Wang (plan 10 T1) : 16 tuiles par matière, nommées <dossier>/tile_<matière>_w<NN>.png.

Usage : python3 tools/generate_ground.py <matière|all> [--sheet planche.png]
La planche pave une zone avec les tuiles choisies comme en jeu (arêtes hachées), pour juger raccords et répétitions.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "tools"))

from sprites.ground import TILE_H, TILE_W, Detail, GroundMaterial, Stipple, render_tiles  # noqa: E402
from sprites.ground_motifs import (calm, cracks, crystals, flowers, glints, grass, litter, moss, mud, reeds,  # noqa: E402
                                   rubble, stains, stones, stubble, water, weeds, wheat)
from tile_preview import cell_hash  # noqa: E402

# Sels des arêtes : identiques à WangTiles.cs.
SALT_NE = 0x51ED
SALT_NW = 0xA7E3

# Quelques motifs posés au centre des tuiles, en plus du semis.
FERN = Detail(("0.0", ".1.", "..."), ("#4a8c3f", "#2d5a27"), weight=0.8)
ROOT_LINE = Detail(("00.", ".00"), ("#2a1e16",), weight=0.8)
CRYSTAL_CLUSTER = Detail((".0.", "010", "11."), ("#7ae0f0", "#4abae0"), weight=1.0)
GLOWING_SPORE = Detail(("0",), ("#6aca5a",), weight=0.3)
LILY_PAD = Detail((".0.", "010", ".0."), ("#3a6a38", "#6aca5a"), weight=0.8)

# Palettes des motifs (plan 37) : ombre portée, ton moyen, clair, reflet.
GRASS_FIELD = ("#335f2a", "#3f7533", "#6aa94c", "#8cc663")
GRASS_SHADE = ("#13270f", "#234620", "#386a2e", "#4f8644")
GRASS_SWAMP = ("#2e4a3e", "#3f6454", "#5a8670", "#78a48a")
LEAF = ("#16290f", "#5a4528", "#7c6036", "#9a7a44")
LEAF_SOIL = ("#140d08", "#4e3a24", "#664c2e", "#80603a")
MOSS = ("#18301a", "#355e2b", "#4c7d3a", "#6a9a4a")
STONE_WARM = ("#4e3f2f", "#8a7a64", "#ab9a7e", "#cbbd9e")
STONE_FIELD = ("#3e4a28", "#8a8a70", "#aaa88a", "#cac6a6")
STONE_GREY = ("#2a2a2a", "#5a5a56", "#72726c", "#8c8c86")
STONE_QUARRY = ("#171214", "#4a4244", "#625a5a", "#7e7676")
STONE_RUST = ("#1c1210", "#5a3a2a", "#7a4e34", "#9a6a48")
WHEAT = ("#5e6a2a", "#a8a050", "#d0c46a", "#ece2a0")
WHEAT_DENSE = ("#525e24", "#9a9446", "#c4b85e", "#e4d894")
STUBBLE = ("#5a4a36", "#8a7656", "#a08a66", "#b49e7a")
MUD = ("#18190f", "#2a2c22", "#3c3e30", "#5a5c4a")
REEDS = ("#1a261a", "#3a5a34", "#5a7a44", "#8a9a5a")
CRYSTAL = ("#1a1220", "#3a8aa8", "#6ad0e8", "#c8f4ff")
RUST = ("#3a1c12", "#5e3220", "#7e4a2c", "#96603a")
OIL = ("#141418", "#24242a", "#34343a", "#44444a")
URBAN_WEED = ("#26321f", "#3e5a30", "#56763c", "#6e8c4a")


def material(name: str, tones: tuple[str, str, str, str], shares: tuple[float, float, float, float], feature_px: float,
             stipples: tuple, seed: int, details: tuple = (), details_per_tile: float = 0.0, **extra) -> GroundMaterial:
    """Fond resserré (calm), grandes formes lentes, et les motifs qui disent la matière."""
    return GroundMaterial(name=name, tones=calm(tones), shares=shares, feature_px=feature_px, stipples=stipples,
                          seed=seed, details=details, details_per_tile=details_per_tile, **extra)


MATERIALS: dict[str, tuple[str, GroundMaterial]] = {
    "foret_sol": ("foret", material(
        "foret_sol", ("#1c3719", "#21421d", "#28502a", "#365f2f"), (0.28, 0.44, 0.23, 0.05), 30.0,
        grass(GRASS_SHADE, 0.8, 0.5) + litter(LEAF, 0.4) + moss(MOSS, 0.6, 0.68), 4127, (FERN,), 0.3)),
    "foret_terre": ("foret", material(
        "foret_terre", ("#2a1e16", "#382719", "#4a3728", "#5c4431"), (0.18, 0.42, 0.30, 0.10), 26.0,
        litter(LEAF_SOIL, 0.8, 0.45) + stones(STONE_WARM, 0.5, 0.45) + moss(MOSS, 0.5, 0.72), 5231,
        (ROOT_LINE,), 0.5)),
    "foret_sousbois": ("foret", material(
        "foret_sousbois", ("#1c3719", "#27491f", "#2f5a26", "#3f7432"), (0.30, 0.38, 0.24, 0.08), 26.0,
        litter(LEAF) + moss(MOSS) + grass(GRASS_SHADE, 0.4, 0.6)
        + (Stipple((".3.", "322", ".1.", ".0."), ("#16290f", "#c8bca0", "#a0896a", "#e8dcc4"), 0.0008, (0.4, 1.0)),),
        6311, (FERN,), 0.4)),
    "carriere_sol": ("carriere", material(
        "carriere_sol", ("#2a2222", "#342b2b", "#3e3434", "#4f4545"), (0.25, 0.4, 0.25, 0.1), 24.0,
        stones(STONE_QUARRY, 1.2) + cracks("#1a1416", 1.0) + crystals(CRYSTAL, 0.25), 7757)),
    "carriere_roche": ("carriere", material(
        "carriere_roche", ("#302a2c", "#3b3537", "#474042", "#554e4f"), (0.25, 0.4, 0.25, 0.1), 34.0,
        stones(STONE_QUARRY, 1.6, 0.7) + cracks("#1c1618", 1.5, 0.6), 7759)),
    "carriere_industriel": ("carriere", material(
        "carriere_industriel", ("#302220", "#3b2922", "#473126", "#553a2c"), (0.25, 0.4, 0.25, 0.1), 28.0,
        stones(STONE_RUST, 0.8) + stains(RUST, 0.6) + stains(OIL, 0.6) + glints("#7a6a5a", "#2a201c", 0.5), 7761)),
    "carriere_cristal": ("carriere", material(
        "carriere_cristal", ("#140e14", "#1e181e", "#2a2222", "#383030"), (0.25, 0.4, 0.25, 0.1), 22.0,
        crystals(CRYSTAL, 0.45) + stones(STONE_QUARRY, 0.6) + glints("#7ae0f0", "#1a1220", 0.4), 7763,
        (CRYSTAL_CLUSTER,), 0.5)),
    "champs_herbe": ("champs", material(
        "champs_herbe", ("#3f7334", "#4a8a3c", "#56a045", "#6fb453"), (0.16, 0.36, 0.36, 0.12), 30.0,
        grass(GRASS_FIELD) + flowers("#3c7031", "#e8e4d8", "#e0c050"), 8101)),
    "champs_fleurs": ("champs", material(
        "champs_fleurs", ("#3f7334", "#4a8a3c", "#56a045", "#6fb453"), (0.16, 0.36, 0.36, 0.12), 30.0,
        grass(GRASS_FIELD, 0.7) + flowers("#3c7031", "#c44a3a", "#e8cf52", 3.0)
        + flowers("#3c7031", "#6a8ad8", "#e8e4d8", 3.0) + flowers("#3c7031", "#e8e4d8", "#e0c050", 2.0), 8117)),
    "champs_sol": ("champs", material(
        "champs_sol", ("#557f3c", "#63953f", "#7aa447", "#98b252"), (0.18, 0.38, 0.32, 0.12), 30.0,
        grass(("#3e5e28", "#55803a", "#86ac52", "#a8c46a"), 0.6, 0.55) + stones(STONE_FIELD, 0.4, 0.4), 8123)),
    "champs_ble": ("champs", material(
        "champs_ble", ("#8a9a48", "#a2ae55", "#b6be64", "#cbd283"), (0.18, 0.36, 0.32, 0.14), 9.0,
        wheat(WHEAT), 8147, stretch=(5.0, 1.0))),
    "champs_ble_dense": ("champs", material(
        "champs_ble_dense", ("#7e8c40", "#96a14c", "#aeb65b", "#c9cf82"), (0.22, 0.34, 0.30, 0.14), 8.0,
        wheat(WHEAT_DENSE, 1.5), 8161, stretch=(6.0, 1.0))),
    "champs_chaume": ("champs", material(
        "champs_chaume", ("#6a5a42", "#78664c", "#8a7058", "#a08a6a"), (0.18, 0.38, 0.32, 0.12), 9.0,
        stubble(STUBBLE, 0.6) + stones(STONE_WARM, 0.3, 0.4), 8171, stretch=(5.0, 1.0))),
    "champs_chemin": ("champs", material(
        "champs_chemin", ("#6a5a42", "#7a6650", "#8a7058", "#a88f6c"), (0.16, 0.38, 0.34, 0.12), 22.0,
        cracks("#55452f") + stones(STONE_WARM)
        + (Stipple(("3.", "23", "02"), ("#5a4a34", "#6a7a3a", "#8aa04a", "#a8b85a"), 0.004, (0.7, 1.0)),), 8179)),
    "champs_bosquet": ("champs", material(
        "champs_bosquet", ("#2e5a28", "#386a30", "#44803a", "#56a045"), (0.22, 0.38, 0.28, 0.12), 26.0,
        grass(("#22441e", "#33612b", "#4f8a3c", "#68a44c"), 0.9, 0.45) + litter(LEAF, 0.6) + moss(MOSS, 0.5), 8191, (FERN,), 0.4)),
    "marecages_sol": ("marecages", material(
        "marecages_sol", ("#3a574d", "#446458", "#527266", "#6c8a7c"), (0.18, 0.40, 0.30, 0.12), 26.0,
        grass(GRASS_SWAMP, 0.8, 0.5) + reeds(REEDS, 0.6) + mud(("#2a3a32", "#3a4a40", "#4e5e52", "#6a7a72"), "#7a8a8a", 0.5), 9011,
        (GLOWING_SPORE,), 0.4)),
    "marecages_vase": ("marecages", material(
        "marecages_vase", ("#2c2d24", "#36372b", "#424233", "#525240"), (0.20, 0.40, 0.30, 0.10), 24.0,
        mud(MUD, "#8a9a96", 1.4) + reeds(REEDS, 0.6), 9029, (GLOWING_SPORE,), 0.3)),
    "marecages_eau": ("marecages", material(
        "marecages_eau", ("#203840", "#28444c", "#325058", "#40606a"), (0.20, 0.42, 0.30, 0.08), 30.0,
        water("#7a9aa6", "#1a3038"), 9043, (LILY_PAD,), 0.5, stretch=(2.0, 1.0))),
    "ruines_sol": ("ruines", material(
        "ruines_sol", ("#3a3a3a", "#444444", "#505050", "#626262"), (0.20, 0.40, 0.30, 0.10), 26.0,
        cracks("#262626", 1.2) + rubble(STONE_GREY, 0.6) + weeds(URBAN_WEED) + stains(OIL, 0.6)
        + glints("#7a92a6", "#26262a", 0.4), 9511)),
    "ruines_trottoir": ("ruines", material(
        "ruines_trottoir", ("#5e5e5a", "#686864", "#73736d", "#838379"), (0.18, 0.40, 0.32, 0.10), 28.0,
        cracks("#4a4a44") + rubble(STONE_GREY, 0.4) + weeds(URBAN_WEED, 1.2) + stains(RUST, 0.3), 9533,
        slab_px=16, joint="#56564f", joint_wear=0.35)),
    "ruines_carrelage": ("ruines", material(
        "ruines_carrelage", ("#5a5652", "#666058", "#726b62", "#857c70"), (0.18, 0.40, 0.32, 0.10), 22.0,
        cracks("#423d38") + rubble(("#2a2622", "#7a5040", "#9a6a52", "#b8866a"), 0.6) + weeds(URBAN_WEED, 0.6),
        9551, slab_px=8, joint="#524d47", joint_wear=0.3)),
    "ruines_place": ("ruines", material(
        "ruines_place", ("#5c5a56", "#67645f", "#736f68", "#86817a"), (0.18, 0.40, 0.32, 0.10), 32.0,
        cracks("#44413c", 1.2) + rubble(STONE_GREY, 0.5) + weeds(URBAN_WEED), 9573,
        slab_px=32, joint="#55524c", joint_wear=0.3, running_bond=True)),
}


def wang_index(x: int, y: int) -> int:
    """Même calcul que WangTiles.Index : couleur de chaque arête par hachage de l'arête (cellule de référence + sel)."""
    odd = y & 1
    ne = cell_hash(x, y, SALT_NE) & 1
    nw = cell_hash(x, y, SALT_NW) & 1
    se = cell_hash(x + odd, y + 1, SALT_NW) & 1
    sw = cell_hash(x - 1 + odd, y + 1, SALT_NE) & 1
    return ne | nw << 1 | se << 2 | sw << 3


def sheet(tiles: list[Image.Image], size: int = 16, scale: int = 3) -> Image.Image:
    width = size * TILE_W + TILE_W // 2
    height = size * TILE_H // 2 + TILE_H
    image = Image.new("RGBA", (width, height), (20, 20, 30, 255))
    for y in range(size * 2):
        for x in range(size):
            left = x * TILE_W + (TILE_W // 2 if y & 1 else 0)
            image.alpha_composite(tiles[wang_index(x, y)], (left, y * TILE_H // 2))
    image = image.crop((TILE_W // 2, TILE_H // 2, width - TILE_W // 2, height - TILE_H))
    return image.resize((image.width * scale, image.height * scale), Image.NEAREST)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("material", choices=[*MATERIALS, "all"])
    parser.add_argument("--sheet", type=Path)
    args = parser.parse_args()

    names = list(MATERIALS) if args.material == "all" else [args.material]
    for name in names:
        folder, material = MATERIALS[name]
        tiles = render_tiles(material)
        out_dir = ROOT / "assets" / "tiles" / folder
        for index, tile in enumerate(tiles):
            tile.save(out_dir / f"tile_{name}_w{index:02d}.png")
        print(f"{name} : 16 tuiles dans assets/tiles/{folder}/")
        if args.sheet:
            path = args.sheet if len(names) == 1 else args.sheet.with_stem(f"{args.sheet.stem}-{name}")
            sheet(tiles).save(path)
            print(f"Planche : {path}")


if __name__ == "__main__":
    main()
