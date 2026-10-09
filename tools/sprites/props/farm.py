"""
Fermes des Champs Sauvages (plan 08, lot P4b-1) : de quoi composer une ferme lisible d'un coup d'œil.

Corps de ferme (maison en pierre au toit de tuiles, grange en planches à pignon, hangar ouvert, silo intact),
petits éléments de cour (abreuvoir, remorque, portail, poteau électrique), clôtures et haies dans les deux axes de
la grille pour border les enclos et les parcelles. Les bâtiments sont dimensionnés en cellules comme les immeubles
(tools/sprites/props/buildings.py) et vus presque de face ; leur hauteur tient dans ~190 px à l'écran.
"""
from __future__ import annotations

from dataclasses import replace

import numpy as np

from ..palette import make_material
from ..render import Part
from ..sdf import capsule, cylinder, ellipsoid, rotation_x, rotation_y, rotation_z, rounded_box, sphere
from ._flora import _clumps, _union
from ._kit import AXIS_X_YAW, AXIS_Y_YAW, M, PropModel, Weathering, box_footprint
from .buildings import BUILDING_YAW, CELL_WIDTH, DEPTH_ROWS, ROW_DEPTH, BuildingSpec, building
from .fields import FENCE, GRASS, GRASS_DARK, RUST, STRAW, stone_wall, tractor, wooden_fence
from .forest import tree

BARN_RED = "#7E4032"
BARN_GREY = "#7A6E62"
PLANK_SHADOW = "#4E3A2E"
TIN = "#8E8A84"
TIN_DARK = "#6A6660"
STONE = "#8A8272"
FARM_STONE = "#A89A82"
WATER = "#2F4652"
HEDGE_DARK = "#2E5A28"
HEDGE = "#3F7534"
WHITE_TRIM = "#C8C0AE"


def _box(p: np.ndarray, center, half) -> np.ndarray:
    return rounded_box(p, center, half, 0.0)


def _nothing(p: np.ndarray) -> np.ndarray:
    return np.full(len(p), np.inf)


def _building_model(stem: str, parts, materials, half_w: float, half_d: float, top: float, mirrored: bool) -> PropModel:
    """Cadrage commun des bâtiments de ferme, comme building() : lacet ±12°, emprise en boîte, suréchantillonnage 3×3."""
    extent_x = half_w + 1.4 * M
    extent_z = half_d + 1.6 * M
    width_px = int((extent_x * 2) * 0.62 * 1.05) + 24
    height_px = int(top * 0.62 * 0.87 + extent_z * 2 * 0.31) + 30
    return PropModel(stem, parts, materials, -BUILDING_YAW if mirrored else BUILDING_YAW,
                     ray_range=float(2.5 * max(extent_x, extent_z, top)),
                     canvas=(width_px, height_px),
                     footprint=box_footprint(half_w, half_d),
                     bounds=((-extent_x, 0.0, -extent_z), (extent_x, top, extent_z)),
                     supersample=3)


# ---------------------------------------------------------------------------------------------------------------------
# Bâtiments
# ---------------------------------------------------------------------------------------------------------------------

def barn(stem: str, seed: int, paint: str, damage: float, mirrored: bool) -> PropModel:
    """Grange à pignon face à la caméra : bardage de planches, grande porte à croix, fenil, toit de tôle."""
    PLANK, PLANK_DARK, ROOF, ROOF_DARK, DOOR, TRIM, BASE, INTERIOR, MOSS, HAY, RUST_M = range(11)
    materials = [make_material("plank", paint), make_material("plank_dark", PLANK_SHADOW),
                 make_material("roof", TIN), make_material("roof_dark", TIN_DARK),
                 make_material("door", "#3E2E26", contrast=0.6), make_material("trim", WHITE_TRIM, contrast=0.7),
                 make_material("base", STONE), make_material("interior", "#1E1A1D", contrast=0.4),
                 make_material("moss", "#5A7A38"), make_material("hay", STRAW), make_material("rust", RUST)]
    w = Weathering(seed)
    half_w = 3 * CELL_WIDTH * 0.46
    half_d = DEPTH_ROWS * ROW_DEPTH * 0.46
    wall_h = 3.4 * M
    ridge = 2.3 * M
    slope_norm = float(np.sqrt(1.0 + (ridge / half_w) ** 2))
    holes = [(np.array([w.uniform(-0.6, 0.6) * half_w, wall_h + ridge * w.uniform(0.3, 0.6), w.uniform(-0.5, 0.6) * half_d]),
              w.uniform(0.7, 1.1) * M) for _ in range(int(damage * 3))]
    gaps = [(np.array([half_w * w.choice([-1.0, 1.0]), w.uniform(0.8, 2.6) * M, w.uniform(-0.6, 0.6) * half_d]),
             w.uniform(0.3, 0.5) * M) for _ in range(int(damage * 4))]
    rust = [np.array([w.uniform(-0.8, 0.8) * half_w, 0.0, w.uniform(-0.8, 0.9) * half_d]) for _ in range(3)]
    moss = [np.array([w.uniform(-0.7, 0.7) * half_w, 0.0, w.uniform(-0.6, 0.8) * half_d]) for _ in range(2)]

    def gable(p: np.ndarray) -> np.ndarray:
        # Distance (signée, approchée) au plan des deux pans : négative sous le toit.
        return (p[:, 1] - wall_h - ridge + ridge * np.abs(p[:, 0]) / half_w) / slope_norm

    def roof_height(x: np.ndarray) -> np.ndarray:
        return wall_h + ridge * (1.0 - np.abs(x) / half_w)

    def cuts(p: np.ndarray) -> np.ndarray:
        if not holes and not gaps:
            return np.full(len(p), np.inf)
        return _union(*(sphere(p, c, r) for c, r in holes + gaps))

    def body(p: np.ndarray) -> np.ndarray:
        box = _box(p, (0, (wall_h + ridge) / 2, 0), (half_w, (wall_h + ridge) / 2, half_d))
        shell = np.maximum(box, gable(p))
        loft = _box(p, (0, wall_h + 0.55 * M, half_d), (0.55 * M, 0.5 * M, 0.3 * M))
        return np.maximum(shell, -np.minimum(loft, cuts(p)))

    def planks(p: np.ndarray, stripe: int) -> np.ndarray:
        band = np.floor(p[:, 0] / (0.36 * M)).astype(np.int64) % 3
        return np.where((band == 0) == (stripe == 1), body(p), np.inf)

    def interior(p: np.ndarray) -> np.ndarray:
        inner = _box(p, (0, (wall_h + ridge) / 2, 0), (half_w - 0.15 * M, (wall_h + ridge) / 2, half_d - 0.15 * M))
        return np.maximum(inner, gable(p) + 0.15 * M)

    def roof(p: np.ndarray, stripe: int) -> np.ndarray:
        sheet = np.maximum(np.abs(gable(p) - 0.14 * M) - 0.08 * M, np.abs(p[:, 2]) - (half_d + 0.35 * M))
        sheet = np.maximum(sheet, np.abs(p[:, 0]) - (half_w + 0.3 * M))
        # Tôle ondulée : une onde sur deux dans chaque teinte.
        band = np.floor(p[:, 2] / (0.28 * M)).astype(np.int64) % 2
        sheet = np.where(band == stripe, sheet, np.inf)
        return np.maximum(sheet, -cuts(p))

    def roof_patches(p: np.ndarray, spots, radius: float) -> np.ndarray:
        sheet = np.maximum(np.abs(gable(p) - 0.2 * M) - 0.06 * M, np.abs(p[:, 2]) - (half_d + 0.3 * M))
        blobs = _union(*(ellipsoid(p, (s[0], roof_height(np.array([s[0]]))[0], s[2]), (radius, 0.6 * M, radius * 0.8))
                         for s in spots))
        return np.maximum(np.maximum(sheet, blobs), -cuts(p))

    def door(p: np.ndarray) -> np.ndarray:
        return _box(p, (0, 1.45 * M, half_d), (1.35 * M, 1.45 * M, 0.08 * M))

    def trim(p: np.ndarray) -> np.ndarray:
        z = half_d + 0.1 * M
        frame = np.maximum(_box(p, (0, 1.45 * M, z), (1.45 * M, 1.55 * M, 0.05 * M)),
                           -_box(p, (0, 1.45 * M, z), (1.3 * M, 1.4 * M, 0.2 * M)))
        braces = _union(capsule(p, (-1.25 * M, 0.15 * M, z), (1.25 * M, 2.75 * M, z), 0.07 * M),
                        capsule(p, (1.25 * M, 0.15 * M, z), (-1.25 * M, 2.75 * M, z), 0.07 * M),
                        capsule(p, (0, 0.1 * M, z), (0, 2.8 * M, z), 0.05 * M))
        return _union(frame, braces)

    def base(p: np.ndarray) -> np.ndarray:
        return _box(p, (0, 0.22 * M, 0), (half_w + 0.06 * M, 0.22 * M, half_d + 0.06 * M))

    def hay(p: np.ndarray) -> np.ndarray:
        return _clumps([(0, wall_h + 0.2 * M, half_d - 0.1 * M)], [(0.5 * M, 0.35 * M, 0.3 * M)], 0.04 * M, 0.2 * M)(p)

    def parts() -> list[Part]:
        return [
            Part(interior, INTERIOR), Part(lambda p: planks(p, 0), PLANK), Part(lambda p: planks(p, 1), PLANK_DARK),
            Part(lambda p: roof(p, 0), ROOF), Part(lambda p: roof(p, 1), ROOF_DARK),
            Part(lambda p: roof_patches(p, rust, 0.32 * M), RUST_M), Part(lambda p: roof_patches(p, moss, 0.3 * M), MOSS),
            Part(door, DOOR), Part(trim, TRIM), Part(base, BASE), Part(hay, HAY),
        ]

    return _building_model(stem, parts, materials, half_w, half_d, wall_h + ridge + 1.2 * M, mirrored)


def hangar(stem: str, seed: int, mirrored: bool) -> PropModel:
    """Hangar ouvert : poteaux, toit de tôle en appentis, bottes de paille empilées à l'abri."""
    POST, ROOF, ROOF_DARK, BALE, BALE_DARK, RUST_M = range(6)
    materials = [make_material("post", FENCE), make_material("roof", TIN), make_material("roof_dark", TIN_DARK),
                 make_material("bale", STRAW), make_material("bale_dark", "#9A8448"), make_material("rust", RUST)]
    w = Weathering(seed)
    half_w = 3 * CELL_WIDTH * 0.46
    half_d = 3 * ROW_DEPTH * 0.46
    front_h, back_h = 3.8 * M, 3.1 * M
    pitch = float(np.arctan2(front_h - back_h, 2 * half_d))
    posts = [(x * half_w * 0.92, z * half_d * 0.9) for x in (-1, 0, 1) for z in (-1, 1)]
    stacks = []
    for column in range(3):
        for layer in range(1 if column == 2 else 2):
            x = (-0.6 + column * 0.5) * half_w
            stacks.append(((x, (0.35 + layer * 0.7) * M, -0.3 * half_d), (0.55 * M, 0.35 * M, 0.4 * M),
                           w.uniform(-0.1, 0.1)))
    rust = [(w.uniform(-0.8, 0.8) * half_w, w.uniform(-0.6, 0.6) * half_d) for _ in range(4)]

    def roof_y(z: float) -> float:
        return back_h + (front_h - back_h) * (z + half_d) / (2 * half_d)

    def sheet(p: np.ndarray) -> np.ndarray:
        return rounded_box(p, (0, (front_h + back_h) / 2 + 0.1 * M, 0), (half_w + 0.35 * M, 0.07 * M, half_d + 0.45 * M), 0.0,
                           rotation_x(-pitch))

    def roof(p: np.ndarray, stripe: int) -> np.ndarray:
        band = np.floor(p[:, 0] / (0.3 * M)).astype(np.int64) % 2
        return np.where(band == stripe, sheet(p), np.inf)

    def rust_part(p: np.ndarray) -> np.ndarray:
        blobs = _union(*(ellipsoid(p, (x, roof_y(z) + 0.1 * M, z), (0.35 * M, 0.3 * M, 0.28 * M)) for x, z in rust))
        return np.maximum(blobs, sheet(p) - 0.03 * M)

    def frame(p: np.ndarray) -> np.ndarray:
        uprights = [capsule(p, (x, 0, z), (x, roof_y(z), z), 0.11 * M) for x, z in posts]
        beams = [capsule(p, (-half_w, roof_y(z) - 0.1 * M, z), (half_w, roof_y(z) - 0.1 * M, z), 0.09 * M)
                 for z in (-half_d * 0.9, half_d * 0.9)]
        return _union(*uprights, *beams)

    def bales(p: np.ndarray, dark: bool) -> np.ndarray:
        blocks = _union(*(rounded_box(p, c, h, 0.08 * M, rotation_z(a)) for c, h, a in stacks))
        # Liens de ficelle : fines tranches plus sombres.
        ties = np.abs(np.mod(p[:, 0], 0.55 * M) - 0.27 * M) < 0.04 * M
        return np.where(ties == dark, blocks, np.inf)

    def parts() -> list[Part]:
        return [Part(frame, POST), Part(lambda p: roof(p, 0), ROOF), Part(lambda p: roof(p, 1), ROOF_DARK),
                Part(rust_part, RUST_M), Part(lambda p: bales(p, False), BALE), Part(lambda p: bales(p, True), BALE_DARK)]

    return _building_model(stem, parts, materials, half_w, half_d, front_h + 1.0 * M, mirrored)


def silo_intact(stem: str, seed: int) -> PropModel:
    """Silo à grain resté debout : tôle cerclée, toit conique, échelle, coulures de rouille."""
    SHEET, BAND, ROOF, LADDER, RUST_M, GRASS_M = range(6)
    materials = [make_material("sheet", "#A8A8A0"), make_material("band", "#6B6161"), make_material("roof", "#8E8A84"),
                 make_material("ladder", "#4A4440"), make_material("rust", RUST), make_material("grass", GRASS)]
    w = Weathering(seed)
    radius, height, cone = 1.15 * M, 4.3 * M, 1.0 * M
    rust = [(np.cos(a) * radius, y * M, np.sin(a) * radius) for a, y in zip(np.linspace(0.6, 2.6, 3), (3.4, 2.2, 3.4))]
    tufts = [(np.cos(a) * 1.25 * M, 0.12 * M, np.sin(a) * 1.25 * M) for a in np.linspace(0.3, 3.0, 4)]
    tilt = w.uniform(-0.02, 0.02)

    def roof(p: np.ndarray) -> np.ndarray:
        radial = np.linalg.norm(p[:, [0, 2]], axis=1)
        local_y = p[:, 1] - height
        slanted = (radial * cone + local_y * (radius + 0.1 * M) - (radius + 0.1 * M) * cone) / np.hypot(cone, radius)
        return np.maximum(slanted, -local_y)

    def ladder(p: np.ndarray) -> np.ndarray:
        z = radius + 0.12 * M
        rails = [capsule(p, (x, 0.2 * M, z), (x, height, z), 0.035 * M) for x in (-0.22 * M, 0.22 * M)]
        rungs = [capsule(p, (-0.22 * M, y * M, z), (0.22 * M, y * M, z), 0.025 * M) for y in np.arange(0.5, 4.2, 0.45)]
        return _union(*rails, *rungs)

    def parts() -> list[Part]:
        return [
            Part(lambda p: cylinder(p @ rotation_z(tilt), (0, height / 2, 0), radius, height / 2, 0.05 * M), SHEET),
            Part(lambda p: _union(*(cylinder(p, (0, y * M, 0), radius + 0.04 * M, 0.05 * M) for y in (0.8, 2.0, 3.2))), BAND),
            Part(roof, ROOF), Part(ladder, LADDER),
            Part(lambda p: _union(*(ellipsoid(p, c, (0.08 * M, 0.55 * M, 0.08 * M)) for c in rust)), RUST_M),
            Part(_clumps(tufts, [(0.3 * M, 0.2 * M, 0.3 * M)] * 4, 0.03 * M, 0.2 * M), GRASS_M),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(100, 170), footprint=box_footprint(1.15 * M, 1.15 * M),
                     bounds=((-1.6 * M, -0.1 * M, -1.6 * M), (1.6 * M, 5.6 * M, 1.6 * M)))


# ---------------------------------------------------------------------------------------------------------------------
# Cour
# ---------------------------------------------------------------------------------------------------------------------

def trough(stem: str, seed: int) -> PropModel:
    """Abreuvoir en pierre, eau sombre, mousse au pied."""
    STONE_M, WATER_M, MOSS = range(3)
    materials = [make_material("stone", STONE), make_material("water", WATER, contrast=0.5), make_material("moss", GRASS)]
    w = Weathering(seed)
    tufts = [(w.uniform(-1.0, 1.0) * M, 0.08 * M, 0.45 * M) for _ in range(3)]

    def basin(p: np.ndarray) -> np.ndarray:
        outer = rounded_box(p, (0, 0.35 * M, 0), (1.2 * M, 0.35 * M, 0.42 * M), 0.06 * M)
        return np.maximum(outer, -_box(p, (0, 0.55 * M, 0), (1.05 * M, 0.3 * M, 0.28 * M)))

    def parts() -> list[Part]:
        return [Part(basin, STONE_M), Part(lambda p: _box(p, (0, 0.5 * M, 0), (1.06 * M, 0.03 * M, 0.29 * M)), WATER_M),
                Part(_clumps(tufts, [(0.2 * M, 0.12 * M, 0.16 * M)] * 3, 0.02 * M, 0.2 * M), MOSS)]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(80, 50), footprint=box_footprint(1.2 * M, 0.42 * M))


def trailer(stem: str, seed: int) -> PropModel:
    """Remorque agricole à plateau, ridelles de bois, deux roues, chargée de bottes affaissées."""
    WOOD, WOOD_DARK, TYRE, METAL, HAY, HAY_DARK = range(6)
    materials = [make_material("wood", "#8A6A4A"), make_material("wood_dark", "#5A4432"),
                 make_material("tyre", "#221F24", contrast=0.5), make_material("metal", RUST),
                 make_material("hay", STRAW), make_material("hay_dark", "#9A8448")]
    w = Weathering(seed)
    bales = [((x * M, 1.05 * M, z * M), w.uniform(-0.15, 0.15)) for x, z in ((-0.5, -0.3), (0.5, -0.25), (0.0, 0.35))]

    def parts() -> list[Part]:
        return [
            Part(lambda p: rounded_box(p, (0, 0.72 * M, 0), (1.5 * M, 0.08 * M, 0.85 * M), 0.02 * M), WOOD),
            Part(lambda p: _union(*(rounded_box(p, (0, 0.95 * M, z * 0.85 * M), (1.5 * M, 0.18 * M, 0.04 * M), 0.02 * M)
                                    for z in (-1, 1)),
                                  rounded_box(p, (-1.5 * M, 0.95 * M, 0), (0.04 * M, 0.18 * M, 0.85 * M), 0.02 * M)), WOOD_DARK),
            Part(lambda p: _union(*(cylinder(p, (0, 0.42 * M, z * 0.95 * M), 0.42 * M, 0.12 * M, 0.05 * M, rotation_x(np.pi / 2))
                                    for z in (-1, 1))), TYRE),
            Part(lambda p: _union(capsule(p, (1.5 * M, 0.6 * M, 0), (2.4 * M, 0.35 * M, 0), 0.06 * M),
                                  capsule(p, (0, 0.42 * M, -0.95 * M), (0, 0.42 * M, 0.95 * M), 0.05 * M)), METAL),
            Part(lambda p: _union(*(rounded_box(p, c, (0.48 * M, 0.3 * M, 0.34 * M), 0.08 * M, rotation_z(a)) for c, a in bales)), HAY),
            Part(lambda p: _union(*(rounded_box(p, (c[0], c[1] + 0.31 * M, c[2]), (0.3 * M, 0.02 * M, 0.3 * M), 0.01 * M)
                                    for c, _ in bales)), HAY_DARK),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(110, 70), footprint=box_footprint(1.6 * M, 0.9 * M))


def power_pole(stem: str, seed: int) -> PropModel:
    """Poteau électrique en bois, penché, traverse à trois isolateurs, un câble rompu qui pend."""
    WOOD, INSULATOR, WIRE = range(3)
    materials = [make_material("wood", "#5E4A38"), make_material("insulator", "#9AB0B8", contrast=0.6),
                 make_material("wire", "#2A2628", contrast=0.4)]
    w = Weathering(seed)
    lean = rotation_z(w.uniform(0.04, 0.09))
    top = 5.0 * M
    insulators = [(x * M, top - 0.3 * M, 0) for x in (-0.8, 0.0, 0.8)]
    sag = [(0.8 * M + t * 0.9 * M, top - 0.35 * M - np.sin(t * 2.4) * 1.6 * M * t, 0.1 * M) for t in np.linspace(0, 1, 6)]

    def parts() -> list[Part]:
        return [
            Part(lambda p: _union(capsule(p @ lean, (0, 0, 0), (0, top, 0), 0.13 * M, 0.1 * M),
                                  capsule(p @ lean, (-1.0 * M, top - 0.45 * M, 0), (1.0 * M, top - 0.45 * M, 0), 0.07 * M)), WOOD),
            Part(lambda p: _union(*(cylinder(p @ lean, c, 0.07 * M, 0.12 * M) for c in insulators)), INSULATOR),
            Part(lambda p: _union(*(capsule(p @ lean, a, b, 0.025 * M) for a, b in zip(sag, sag[1:]))), WIRE),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(80, 150), footprint=box_footprint(0.14 * M, 0.14 * M),
                     bounds=((-2.2 * M, -0.1 * M, -0.6 * M), (2.4 * M, 5.6 * M, 0.6 * M)))


def farm_gate(stem: str, seed: int, yaw: float) -> PropModel:
    """Portail de ferme en tubes, entre deux piliers de bois, entrouvert."""
    POST, BAR = range(2)
    materials = [make_material("post", FENCE), make_material("bar", "#8A8478")]
    w = Weathering(seed)
    opening = w.uniform(0.35, 0.55)
    hinge = np.array([-1.5 * M, 0.0, 0.0])
    rails = []
    for y in (0.25, 0.5, 0.75, 1.0):
        rails.append(((0, y * M, 0), (2.6 * M, y * M, 0)))
    rails.append(((0, 0.25 * M, 0), (2.6 * M, 1.0 * M, 0)))

    def gate(p: np.ndarray) -> np.ndarray:
        # Le battant tourne autour du gond : local = (p − gond) @ rotation.
        c, s = np.cos(opening), np.sin(opening)
        rotation = np.array([[c, 0, -s], [0, 1, 0], [s, 0, c]])
        local = (p - hinge) @ rotation
        return _union(*(capsule(local, a, b, 0.045 * M) for a, b in rails))

    def parts() -> list[Part]:
        return [
            Part(lambda p: _union(*(rounded_box(p, (x * M, 0.65 * M, 0), (0.11 * M, 0.65 * M, 0.11 * M), 0.02 * M)
                                    for x in (-1.62, 1.62))), POST),
            Part(gate, BAR),
        ]

    return PropModel(stem, parts, materials, yaw, canvas=(100, 64), footprint=box_footprint(0.12 * M, 0.12 * M))


# ---------------------------------------------------------------------------------------------------------------------
# Limites de parcelles
# ---------------------------------------------------------------------------------------------------------------------

def hedge(stem: str, seed: int, yaw: float, flowering: bool) -> PropModel:
    """Haie bocagère d'une cellule de long : touffes serrées, un ou deux arbustes plus hauts, quelques fleurs."""
    LEAF_DARK, LEAF, FLOWER = range(3)
    materials = [make_material("leaf_dark", HEDGE_DARK), make_material("leaf", HEDGE),
                 make_material("flower", "#D8D0B8", contrast=0.6)]
    w = Weathering(seed)
    lumps = []
    x = -1.75
    while x < 1.75:
        height = w.uniform(0.5, 0.75) * (1.35 if w.uniform(0, 1) < 0.2 else 1.0)
        lumps.append(((x * M, height * M, w.uniform(-0.12, 0.12) * M), (w.uniform(0.4, 0.55) * M, height * M, 0.45 * M)))
        x += w.uniform(0.35, 0.55)
    tops = [(c[0], c[1] + r[1] * 0.75, c[2] + 0.2 * M) for c, r in lumps[::2]]
    blossoms = [(c[0] + w.uniform(-0.2, 0.2) * M, c[1] + w.uniform(-0.3, 0.2) * M, c[2] + 0.4 * M) for c, _ in lumps] \
        if flowering else []

    def parts() -> list[Part]:
        result = [
            Part(_clumps([c for c, _ in lumps], [r for _, r in lumps], 0.06 * M, 0.5 * M), LEAF_DARK),
            Part(_clumps(tops, [(0.35 * M, 0.2 * M, 0.3 * M)] * len(tops), 0.04 * M, 0.4 * M), LEAF),
        ]
        if blossoms:
            result.append(Part(lambda p: _union(*(sphere(p, b, 0.07 * M) for b in blossoms)), FLOWER))
        return result

    return PropModel(stem, parts, materials, yaw, canvas=(120, 70), footprint=box_footprint(1.8 * M, 0.45 * M))


# ---------------------------------------------------------------------------------------------------------------------
# Scènes-récits (P4b-4) : des gens étaient là, puis plus personne
# ---------------------------------------------------------------------------------------------------------------------

def picnic(stem: str, seed: int) -> PropModel:
    """Pique-nique abandonné : nappe à carreaux froissée, panier ouvert, bouteille couchée, deux assiettes."""
    CLOTH_A, CLOTH_B, WICKER, BOTTLE, PLATE, GRASS_M = range(6)
    materials = [make_material("cloth_a", "#B84A3E", contrast=0.8), make_material("cloth_b", "#D8CFC0", contrast=0.6),
                 make_material("wicker", "#9A7A4A"), make_material("bottle", "#3E6A4E", contrast=0.6),
                 make_material("plate", "#C8C4BA", contrast=0.6), make_material("grass", GRASS)]
    w = Weathering(seed)
    tufts = [(w.uniform(-1.2, 1.2) * M, 0.08 * M, w.uniform(-0.9, 0.9) * M) for _ in range(4)]

    def cloth(p: np.ndarray, check: int) -> np.ndarray:
        # Nappe posée au sol, un coin relevé par le vent ; carreaux en damier.
        sheet = rounded_box(p, (0, 0.04 * M, 0), (1.0 * M, 0.03 * M, 0.8 * M), 0.02 * M, rotation_z(0.03))
        corner = rounded_box(p, (0.85 * M, 0.18 * M, 0.65 * M), (0.25 * M, 0.02 * M, 0.2 * M), 0.02 * M, rotation_x(0.6))
        d = np.minimum(sheet, corner)
        squares = (np.floor(p[:, 0] / (0.25 * M)) + np.floor(p[:, 2] / (0.25 * M))).astype(np.int64) % 2
        return np.where(squares == check, d, np.inf)

    def basket(p: np.ndarray) -> np.ndarray:
        body = np.maximum(rounded_box(p, (-0.45 * M, 0.25 * M, -0.2 * M), (0.3 * M, 0.2 * M, 0.22 * M), 0.05 * M),
                          -rounded_box(p, (-0.45 * M, 0.35 * M, -0.2 * M), (0.25 * M, 0.2 * M, 0.17 * M), 0.03 * M))
        handle = capsule(p, (-0.7 * M, 0.42 * M, -0.2 * M), (-0.45 * M, 0.7 * M, -0.2 * M), 0.03 * M)
        return np.minimum(body, np.minimum(handle, capsule(p, (-0.45 * M, 0.7 * M, -0.2 * M), (-0.2 * M, 0.42 * M, -0.2 * M), 0.03 * M)))

    def parts() -> list[Part]:
        return [
            Part(lambda p: cloth(p, 0), CLOTH_A), Part(lambda p: cloth(p, 1), CLOTH_B),
            Part(basket, WICKER),
            Part(lambda p: capsule(p, (0.2 * M, 0.12 * M, 0.3 * M), (0.65 * M, 0.1 * M, 0.1 * M), 0.08 * M, 0.04 * M), BOTTLE),
            Part(lambda p: _union(cylinder(p, (0.35 * M, 0.08 * M, -0.35 * M), 0.2 * M, 0.02 * M),
                                  cylinder(p, (-0.1 * M, 0.08 * M, 0.45 * M), 0.2 * M, 0.02 * M)), PLATE),
            Part(_clumps(tufts, [(0.2 * M, 0.14 * M, 0.2 * M)] * 4, 0.03 * M, 0.2 * M), GRASS_M),
        ]

    return PropModel(stem, parts, materials, AXIS_Y_YAW, canvas=(90, 60))


def clothesline(stem: str, seed: int) -> PropModel:
    """Linge encore étendu : deux poteaux, une corde qui ploie, draps et chemises décolorés qui battent au vent."""
    POST, ROPE, SHEET, SHIRT, PIN = range(5)
    materials = [make_material("post", FENCE), make_material("rope", "#8A8478", contrast=0.5),
                 make_material("sheet", "#D8D4C8", contrast=0.7), make_material("shirt", "#6A88A8", contrast=0.8),
                 make_material("pin", "#9A7A4A", contrast=0.5)]
    w = Weathering(seed)
    span = 1.7 * M
    top = 1.9 * M

    def rope_y(x: float) -> float:
        return top - 0.25 * M * (1.0 - (x / span) ** 2)

    laundry = []
    x = -1.35
    while x < 1.3:
        width = w.uniform(0.25, 0.45)
        laundry.append((x * M + width * M, width * M, w.uniform(0.45, 0.8) * M, w.uniform(0.15, 0.4), SHIRT if len(laundry) % 2 else SHEET))
        x += width * 2 + w.uniform(0.05, 0.15)

    def cloths(p: np.ndarray, material: int) -> np.ndarray:
        pieces = [rounded_box(p, (cx, rope_y(cx) - h, 0.05 * M), (hw, h, 0.02 * M), 0.02 * M, rotation_x(-sway))
                  for cx, hw, h, sway, m in laundry if m == material]
        return _union(*pieces) if pieces else np.full(len(p), np.inf)

    def rope(p: np.ndarray) -> np.ndarray:
        points = [(t * span, rope_y(t * span), 0.0) for t in np.linspace(-1, 1, 7)]
        return _union(*(capsule(p, a, b, 0.02 * M) for a, b in zip(points, points[1:])))

    def parts() -> list[Part]:
        return [
            Part(lambda p: _union(*(capsule(p, (x, 0, 0), (x, top + 0.1 * M, 0), 0.07 * M) for x in (-span, span)),
                                  *(capsule(p, (x - 0.25 * M, top, 0), (x + 0.25 * M, top, 0), 0.05 * M) for x in (-span, span))), POST),
            Part(rope, ROPE), Part(lambda p: cloths(p, SHEET), SHEET), Part(lambda p: cloths(p, SHIRT), SHIRT),
            Part(lambda p: _union(*(sphere(p, (cx, rope_y(cx), 0.05 * M), 0.05 * M) for cx, _, _, _, _ in laundry)), PIN),
        ]

    return PropModel(stem, parts, materials, AXIS_Y_YAW, canvas=(110, 90), footprint=box_footprint(0.1 * M, 0.1 * M))


def crow_scarecrow(stem: str, seed: int) -> PropModel:
    """Épouvantail couronné de corbeaux : ils ont gagné, ils se posent sur lui."""
    POLE, SACK, COAT, HAT, CROW, BEAK = range(6)
    materials = [make_material("pole", FENCE), make_material("sack", "#B8A080"), make_material("coat", "#4E5A6A"),
                 make_material("hat", "#3E342A"), make_material("crow", "#1E1C24", contrast=0.5),
                 make_material("beak", "#C8A040", contrast=0.5)]
    w = Weathering(seed)
    lean = rotation_z(-0.12)
    perches = [(-0.65 * M, 1.48 * M), (0.5 * M, 1.5 * M), (0.0, 2.35 * M)]
    crows = [(x + w.uniform(-0.08, 0.08) * M, y) for x, y in perches]

    def crow_body(p: np.ndarray) -> np.ndarray:
        birds = []
        for x, y in crows:
            birds.append(ellipsoid(p @ lean, (x, y + 0.14 * M, 0), (0.16 * M, 0.12 * M, 0.1 * M)))
            birds.append(sphere(p @ lean, (x + 0.13 * M, y + 0.25 * M, 0), 0.07 * M))
            birds.append(capsule(p @ lean, (x - 0.12 * M, y + 0.12 * M, 0), (x - 0.3 * M, y + 0.05 * M, 0), 0.04 * M))
        return _union(*birds)

    def parts() -> list[Part]:
        return [
            Part(lambda p: _union(capsule(p @ lean, (0, 0, 0), (0, 1.9 * M, 0), 0.06 * M),
                                  capsule(p @ lean, (-0.75 * M, 1.4 * M, 0), (0.6 * M, 1.42 * M, 0), 0.05 * M)), POLE),
            Part(lambda p: sphere(p @ lean, (0, 1.95 * M, 0.02 * M), 0.2 * M), SACK),
            Part(lambda p: rounded_box(p @ lean, (0, 1.15 * M, 0.02 * M), (0.34 * M, 0.38 * M, 0.15 * M), 0.08 * M), COAT),
            Part(lambda p: _union(cylinder(p @ lean, (0, 2.12 * M, 0), 0.3 * M, 0.02 * M, 0.01 * M),
                                  cylinder(p @ lean, (0, 2.2 * M, 0), 0.15 * M, 0.08 * M, 0.03 * M)), HAT),
            Part(crow_body, CROW),
            Part(lambda p: _union(*(capsule(p @ lean, (x + 0.18 * M, y + 0.25 * M, 0), (x + 0.27 * M, y + 0.23 * M, 0), 0.02 * M)
                                    for x, y in crows)), BEAK),
        ]

    # Bras à l'horizontale de l'écran : les corbeaux perchés restent visibles.
    return PropModel(stem, parts, materials, AXIS_Y_YAW, canvas=(80, 120), footprint=box_footprint(0.12 * M, 0.12 * M))


# ---------------------------------------------------------------------------------------------------------------------
# Catalogue
# ---------------------------------------------------------------------------------------------------------------------

def mired_tractor(stem: str, seed: int) -> PropModel:
    """Tracteur embourbé : le tracteur des champs piqué du nez dans une mare de boue, deux ornières derrière lui,
    les roues arrière à demi englouties. On devine qu'il a tenté de fuir à travers champs."""
    base = tractor(stem, seed, False)
    MUD, MUD_WET, GRASS_M = range(len(base.materials), len(base.materials) + 3)
    materials = list(base.materials) + [make_material("mud", "#4E3E2E"), make_material("mud_wet", "#35302E", contrast=0.6),
                                        make_material("grass", GRASS)]
    # Nez vers l'avant (+Z) enfoncé, léger roulis : local = (p − pivot) @ rotation, comme les primitives.
    tilt = rotation_x(-0.22) @ rotation_z(0.08)
    pivot = np.array([0.0, 0.0, 0.0])
    sink = 0.32 * M
    w = Weathering(seed + 3)
    splashes = [(w.uniform(-0.6, 0.6) * M, w.uniform(0.25, 0.8) * M, w.uniform(-1.0, 1.2) * M) for _ in range(7)]
    tufts = [(w.uniform(-1.6, 1.6) * M, 0.08 * M, w.uniform(-2.6, 2.0) * M) for _ in range(5)]

    def sunk(distance):
        def evaluate(p: np.ndarray) -> np.ndarray:
            local = (p - pivot) @ tilt + pivot + np.array([0.0, sink, 0.0])
            # Rien sous la surface de la boue : le sol n'est pas rendu, il ne cacherait pas les roues.
            return np.maximum(distance(local), -p[:, 1])
        return evaluate

    def pool(p: np.ndarray) -> np.ndarray:
        return np.minimum(ellipsoid(p, (0, 0.0, 0.2 * M), (1.5 * M, 0.08 * M, 1.9 * M)),
                          ellipsoid(p, (0.5 * M, 0.0, 1.6 * M), (0.9 * M, 0.07 * M, 0.8 * M)))

    def puddles(p: np.ndarray) -> np.ndarray:
        return _union(ellipsoid(p, (-0.7 * M, 0.03 * M, 1.2 * M), (0.45 * M, 0.07 * M, 0.35 * M)),
                      ellipsoid(p, (0.8 * M, 0.03 * M, -0.4 * M), (0.35 * M, 0.07 * M, 0.5 * M)))

    def ruts(p: np.ndarray) -> np.ndarray:
        # Deux bandes plates de boue humide qui s'amincissent en s'éloignant.
        return _union(*(rounded_box(p, (x * 0.74 * M, 0.02 * M, -2.2 * M), (0.2 * M, 0.03 * M, 1.0 * M), 0.02 * M, rotation_y(x * 0.04))
                        for x in (-1, 1)))

    def parts() -> list[Part]:
        result = [Part(sunk(part.distance), part.material) for part in base.parts()]
        result += [
            Part(pool, MUD), Part(puddles, MUD_WET), Part(ruts, MUD_WET),
            Part(sunk(lambda q: _union(*(sphere(q, c, 0.14 * M) for c in splashes))), MUD),
            Part(_clumps(tufts, [(0.22 * M, 0.16 * M, 0.22 * M)] * 5, 0.03 * M, 0.2 * M), GRASS_M),
        ]
        return result

    return PropModel(stem, parts, materials, AXIS_Y_YAW, canvas=(160, 130), footprint=box_footprint(0.9 * M, 1.4 * M))


def catalog() -> list[PropModel]:
    """Nom : prop_farm_<élément>[_damaged]_<a|b> pour les bâtiments (b = lacet opposé) ; _h / _v pour les limites qui filent
    à l'horizontale de l'écran ou vers la profondeur (un modèle long en x prend AXIS_Y_YAW pour rester horizontal)."""
    models: list[PropModel] = []
    for index, (damage, mirrored) in enumerate(((0.12, False), (0.12, True), (0.5, False))):
        suffix = f"{'_damaged' if damage > 0.4 else ''}_{'b' if mirrored else 'a'}"
        models.append(building(BuildingSpec(f"prop_farm_house{suffix}", 3, DEPTH_ROWS, 2, "house", FARM_STONE, damage,
                                            701 + index, mirrored, window_spacing=2.5 * M)))
        models.append(barn(f"prop_farm_barn{suffix}", 711 + index, BARN_RED if index != 1 else BARN_GREY, damage, mirrored))
    models += [
        hangar("prop_farm_hangar_a", 721, False),
        hangar("prop_farm_hangar_b", 722, True),
        silo_intact("prop_farm_silo", 731),
        trough("prop_farm_trough", 741),
        trailer("prop_farm_trailer", 751),
        power_pole("prop_farm_power_pole", 761),
        farm_gate("prop_farm_gate_h", 771, AXIS_Y_YAW),
        farm_gate("prop_farm_gate_v", 772, AXIS_X_YAW),
        hedge("prop_hedge_h", 781, AXIS_Y_YAW, False),
        hedge("prop_hedge_h_v2", 782, AXIS_Y_YAW, True),
        hedge("prop_hedge_v", 783, AXIS_X_YAW, False),
        hedge("prop_hedge_v_v2", 784, AXIS_X_YAW, True),
        # Les clôtures des champs (prop_wooden_fence, prop_low_stone_wall) filent vers la profondeur : variantes horizontales.
        replace(wooden_fence("prop_wooden_fence_h", 541, False), yaw=AXIS_Y_YAW),
        replace(wooden_fence("prop_wooden_fence_broken_h", 542, True), yaw=AXIS_Y_YAW),
        replace(stone_wall("prop_low_stone_wall_h", 531, False), yaw=AXIS_Y_YAW),
    ]
    models += [
        picnic("prop_scene_picnic", 801),
        clothesline("prop_scene_clothesline", 802),
        crow_scarecrow("prop_scene_crow_scarecrow", 803),
        mired_tractor("prop_scene_mired_tractor", 804),
    ]
    # Arbres de verger, plantés en rangs : bas et ronds, l'un en feuilles, l'autre encore en fleurs.
    models += tree("prop_orchard_tree", 791, 3.4 * M, 1.25 * M, 0.13 * M, 12, "#5E4A38", GRASS, "#8AC060", False, (80, 110))
    models += tree("prop_orchard_tree_blossom", 792, 3.3 * M, 1.2 * M, 0.13 * M, 12, "#5E4A38", GRASS, "#EAD8D2", False, (80, 110))
    return models
