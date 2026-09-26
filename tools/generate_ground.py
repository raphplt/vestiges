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

from sprites.ground import TILE_H, TILE_W, Detail, GroundMaterial, render_tiles  # noqa: E402
from tile_preview import cell_hash  # noqa: E402

# Sels des arêtes : identiques à WangTiles.cs.
SALT_NE = 0x51ED
SALT_NW = 0xA7E3

LEAF_LITTER = Detail(("01", "10"), ("#4a3728", "#7a5c42"))
GOLD_LEAF = Detail((".0", "01"), ("#c49b3e", "#7a5c42"), weight=0.6)
FERN = Detail(("0.0", ".1.", "..."), ("#4a8c3f", "#2d5a27"), weight=0.8)
MOSS_SPOT = Detail(("00",), ("#7bc558",), weight=0.5)

ROOT_LINE = Detail(("00.", ".00"), ("#2a1e16",), weight=0.8)
PEBBLE = Detail(("01",), ("#7a7a70", "#4a4a44"), weight=0.6)
VIOLET_FLOWER = Detail((".0.", "010", ".0."), ("#6a4a8a", "#c49b3e"), weight=0.25)

GRAVEL = Detail(("01", "10"), ("#8a7a6a", "#5a5050"))
PALE_STONE = Detail(("0",), ("#b4a080",), weight=0.7)
RUST_FLAKE = Detail(("01",), ("#6a3a28", "#5a2a18"), weight=0.6)
CRYSTAL_CHIP = Detail((".0", "01"), ("#7ae0f0", "#4abae0"), weight=0.25)
CRYSTAL_CLUSTER = Detail((".0.", "010", "11."), ("#7ae0f0", "#4abae0"), weight=1.0)
BOLT = Detail(("0",), ("#8a7a6a",), weight=0.5)
CRACK = Detail(("0..", ".00", "..0"), ("#1a1218",), weight=0.7)

RED_FLOWER = Detail((".0.", "010", ".0."), ("#c44a3a", "#c8d480"))
BLUE_FLOWER = Detail((".0.", "010", ".0."), ("#5a7aca", "#d0d8d0"))
WHITE_FLOWER = Detail(("0",), ("#d0d8d0",), weight=1.2)
YELLOW_FLOWER = Detail(("0",), ("#e0d070",), weight=1.2)
CLOVER = Detail(("01",), ("#7cba5a", "#3a6a30"), weight=0.8)
STRAW = Detail(("00",), ("#b8a080",), weight=1.0)
WHEAT_TIP = Detail(("0",), ("#e8e0d4",), weight=0.8)
FIELD_PEBBLE = Detail(("0",), ("#b8a080",), weight=0.7)

REED = Detail(("0.0", "0.0", "1.1"), ("#3a6a38", "#2a4430"), weight=1.0)
MUD_SPOT = Detail(("01",), ("#3a2e22", "#2a1e16"), weight=0.8)
PALE_MOSS = Detail(("0",), ("#b8c8be",), weight=0.6)
GLOWING_SPORE = Detail(("0",), ("#6aca5a",), weight=0.3)
PUDDLE_GLINT = Detail(("01",), ("#7a8a9a", "#5a6a72"), weight=1.0)
LILY_PAD = Detail((".0.", "010", ".0."), ("#3a6a38", "#6aca5a"), weight=0.8)
WATER_GLINT = Detail(("00",), ("#8a9ea8",), weight=1.0)

CONCRETE_CRACK = Detail(("0..", ".0.", ".00"), ("#2e2e2e",), weight=1.0)
WEED = Detail(("0.0", ".1."), ("#4a7a3a", "#3a5a2e"), weight=0.8)
RUST_STAIN = Detail(("01", "1."), ("#a85c30", "#8a5a42"), weight=0.5)
DIRT_HOLE = Detail(("00", "01"), ("#5a4a38", "#3e3226"), weight=0.8)
GLASS = Detail(("0",), ("#5a7a9a",), weight=0.4)

MATERIALS: dict[str, tuple[str, GroundMaterial]] = {
    "foret_sol": ("foret", GroundMaterial(
        name="foret_sol",
        tones=("#1c3719", "#21421d", "#28502a", "#365f2f"),
        shares=(0.28, 0.44, 0.23, 0.05),
        feature_px=22.0,
        details=(LEAF_LITTER, GOLD_LEAF, FERN, MOSS_SPOT),
        details_per_tile=0.7,
        seed=4127,
    )),
    "foret_terre": ("foret", GroundMaterial(
        name="foret_terre",
        tones=("#2a1e16", "#382719", "#4a3728", "#5c4431"),
        shares=(0.18, 0.42, 0.30, 0.10),
        feature_px=18.0,
        details=(ROOT_LINE, PEBBLE, LEAF_LITTER, GOLD_LEAF, MOSS_SPOT),
        details_per_tile=1.1,
        seed=5231,
    )),
    "foret_sousbois": ("foret", GroundMaterial(
        name="foret_sousbois",
        tones=("#1c3719", "#27491f", "#2f5a26", "#3f7432"),
        shares=(0.30, 0.38, 0.24, 0.08),
        feature_px=12.0,
        details=(FERN, GOLD_LEAF, LEAF_LITTER, VIOLET_FLOWER, MOSS_SPOT),
        details_per_tile=1.4,
        seed=6311,
    )),
    "carriere_sol": ("carriere", GroundMaterial(
        name="carriere_sol",
        tones=("#2a2222", "#342b2b", "#3e3434", "#4f4545"),
        shares=(0.22, 0.42, 0.28, 0.08),
        feature_px=16.0,
        details=(GRAVEL, PALE_STONE, RUST_FLAKE, CRYSTAL_CHIP),
        details_per_tile=1.2,
        seed=7411,
    )),
    "carriere_roche": ("carriere", GroundMaterial(
        name="carriere_roche",
        tones=("#302a2c", "#3b3537", "#474042", "#554e4f"),
        shares=(0.18, 0.42, 0.32, 0.08),
        feature_px=26.0,
        details=(CRACK, GRAVEL, PALE_STONE),
        details_per_tile=1.0,
        seed=7523,
    )),
    "carriere_industriel": ("carriere", GroundMaterial(
        name="carriere_industriel",
        tones=("#302220", "#3b2922", "#473126", "#553a2c"),
        shares=(0.20, 0.42, 0.30, 0.08),
        feature_px=20.0,
        details=(BOLT, RUST_FLAKE, CRACK),
        details_per_tile=1.0,
        seed=7639,
    )),
    "carriere_cristal": ("carriere", GroundMaterial(
        name="carriere_cristal",
        tones=("#140e14", "#1e181e", "#2a2222", "#383030"),
        shares=(0.26, 0.38, 0.26, 0.10),
        feature_px=14.0,
        details=(CRYSTAL_CLUSTER, CRYSTAL_CHIP, CRACK),
        details_per_tile=1.1,
        seed=7757,
    )),
    "champs_herbe": ("champs", GroundMaterial(
        name="champs_herbe",
        tones=("#3f7334", "#4a8a3c", "#56a045", "#6fb453"),
        shares=(0.16, 0.36, 0.36, 0.12),
        feature_px=18.0,
        details=(CLOVER, WHITE_FLOWER, YELLOW_FLOWER),
        details_per_tile=0.8,
        seed=8101,
    )),
    "champs_fleurs": ("champs", GroundMaterial(
        name="champs_fleurs",
        tones=("#3f7334", "#4a8a3c", "#56a045", "#6fb453"),
        shares=(0.16, 0.36, 0.36, 0.12),
        feature_px=18.0,
        details=(RED_FLOWER, BLUE_FLOWER, WHITE_FLOWER, YELLOW_FLOWER),
        details_per_tile=3.0,
        seed=8117,
    )),
    "champs_sol": ("champs", GroundMaterial(
        name="champs_sol",
        tones=("#557f3c", "#63953f", "#7aa447", "#98b252"),
        shares=(0.18, 0.38, 0.32, 0.12),
        feature_px=20.0,
        details=(CLOVER, FIELD_PEBBLE, YELLOW_FLOWER),
        details_per_tile=0.6,
        seed=8123,
    )),
    "champs_ble": ("champs", GroundMaterial(
        name="champs_ble",
        tones=("#8a9a48", "#a2ae55", "#b6be64", "#cbd283"),
        shares=(0.18, 0.36, 0.32, 0.14),
        feature_px=7.0,
        stretch=(5.0, 1.0),
        details=(WHEAT_TIP, STRAW),
        details_per_tile=0.8,
        seed=8147,
    )),
    "champs_ble_dense": ("champs", GroundMaterial(
        name="champs_ble_dense",
        tones=("#7e8c40", "#96a14c", "#aeb65b", "#c9cf82"),
        shares=(0.22, 0.34, 0.30, 0.14),
        feature_px=6.0,
        stretch=(6.0, 1.0),
        details=(WHEAT_TIP, WHEAT_TIP, STRAW),
        details_per_tile=1.4,
        seed=8161,
    )),
    "champs_chaume": ("champs", GroundMaterial(
        name="champs_chaume",
        tones=("#6a5a42", "#78664c", "#8a7058", "#a08a6a"),
        shares=(0.18, 0.38, 0.32, 0.12),
        feature_px=6.0,
        stretch=(5.0, 1.0),
        details=(STRAW, FIELD_PEBBLE),
        details_per_tile=1.0,
        seed=8171,
    )),
    "champs_chemin": ("champs", GroundMaterial(
        name="champs_chemin",
        tones=("#6a5a42", "#7a6650", "#8a7058", "#a88f6c"),
        shares=(0.16, 0.38, 0.34, 0.12),
        feature_px=14.0,
        details=(FIELD_PEBBLE, STRAW),
        details_per_tile=0.9,
        seed=8179,
    )),
    "champs_bosquet": ("champs", GroundMaterial(
        name="champs_bosquet",
        tones=("#2e5a28", "#386a30", "#44803a", "#56a045"),
        shares=(0.22, 0.38, 0.28, 0.12),
        feature_px=12.0,
        details=(FERN, LEAF_LITTER, CLOVER),
        details_per_tile=1.2,
        seed=8191,
    )),
    "marecages_sol": ("marecages", GroundMaterial(
        name="marecages_sol",
        tones=("#3a574d", "#446458", "#527266", "#6c8a7c"),
        shares=(0.18, 0.40, 0.30, 0.12),
        feature_px=17.0,
        details=(REED, MUD_SPOT, PALE_MOSS, GLOWING_SPORE),
        details_per_tile=1.0,
        seed=9011,
    )),
    "marecages_vase": ("marecages", GroundMaterial(
        name="marecages_vase",
        tones=("#2c2d24", "#36372b", "#424233", "#525240"),
        shares=(0.20, 0.40, 0.30, 0.10),
        feature_px=15.0,
        details=(PUDDLE_GLINT, REED, GLOWING_SPORE),
        details_per_tile=1.0,
        seed=9029,
    )),
    "marecages_eau": ("marecages", GroundMaterial(
        name="marecages_eau",
        tones=("#203840", "#28444c", "#325058", "#40606a"),
        shares=(0.20, 0.42, 0.30, 0.08),
        feature_px=24.0,
        stretch=(2.0, 1.0),
        details=(WATER_GLINT, LILY_PAD),
        details_per_tile=0.7,
        seed=9043,
    )),
    "ruines_sol": ("ruines", GroundMaterial(
        name="ruines_sol",
        tones=("#3a3a3a", "#444444", "#505050", "#626262"),
        shares=(0.20, 0.40, 0.30, 0.10),
        feature_px=18.0,
        details=(CONCRETE_CRACK, WEED, RUST_STAIN, GLASS),
        details_per_tile=1.1,
        seed=9511,
    )),
    "ruines_trottoir": ("ruines", GroundMaterial(
        name="ruines_trottoir",
        tones=("#5e5e5a", "#686864", "#73736d", "#838379"),
        shares=(0.18, 0.40, 0.32, 0.10),
        feature_px=20.0,
        slab_px=16,
        joint="#5a5a56",
        joint_wear=0.35,
        details=(CONCRETE_CRACK, WEED, RUST_STAIN),
        details_per_tile=0.8,
        seed=9533,
    )),
    "ruines_carrelage": ("ruines", GroundMaterial(
        name="ruines_carrelage",
        tones=("#5a5652", "#666058", "#726b62", "#857c70"),
        shares=(0.18, 0.40, 0.32, 0.10),
        feature_px=16.0,
        slab_px=8,
        joint="#56514b",
        joint_wear=0.3,
        details=(DIRT_HOLE, CONCRETE_CRACK, RUST_STAIN),
        details_per_tile=1.2,
        seed=9551,
    )),
    "ruines_place": ("ruines", GroundMaterial(
        name="ruines_place",
        tones=("#5c5a56", "#67645f", "#736f68", "#86817a"),
        shares=(0.18, 0.40, 0.32, 0.10),
        feature_px=24.0,
        slab_px=32,
        joint="#58554f",
        joint_wear=0.3,
        running_bond=True,
        details=(CONCRETE_CRACK, WEED, DIRT_HOLE),
        details_per_tile=0.9,
        seed=9573,
    )),
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
