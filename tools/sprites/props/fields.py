"""
Décors des Champs Sauvages (plan 08, lot P4) : espaces ouverts, vent, vestiges agricoles abandonnés.

Échelle réelle du personnage (1 m ≈ 27,5 unités), palette « Champs Sauvages » de la charte (§3). Les herbes et les
fleurs restent sous la hauteur du genou pour ne pas masquer les créatures ; les repères agricoles (éolienne, silo,
tracteur) sont rares et hauts, pour se lire de loin dans un paysage plat.
"""
from __future__ import annotations

import numpy as np

from ..palette import make_material
from ..render import Part
from ..sdf import capsule, cylinder, ellipsoid, rotation_x, rotation_y, rotation_z, rounded_box, sphere
from ._kit import AXIS_X_YAW, AXIS_Y_YAW, M, PropModel, Weathering, box_footprint
from .forest import _clumps, _union, tree

GRASS_DARK = "#3A6A30"
GRASS = "#5AA848"
GRASS_GOLD = "#A8B458"
GRASS_PALE = "#C8D480"
EARTH_DRY = "#8A7058"
EARTH_LIGHT = "#B8A080"
FIELD_STONE = "#7A7A6A"
FLOWER_RED = "#C44A3A"
FLOWER_BLUE = "#5A7ACA"
SKY = "#8AB0D0"
FENCE = "#6A5A42"
RUST = "#6A4430"
RUST_ORANGE = "#A85C30"
STRAW = "#C8B060"


# ---------------------------------------------------------------------------------------------------------------------
# Végétation basse
# ---------------------------------------------------------------------------------------------------------------------

def tall_grass(stem: str, seed: int, blade: str, tip: str, ears: bool) -> PropModel:
    """Touffe d'herbes hautes (ou de blé retourné à l'état sauvage), penchée par le vent."""
    BLADE, TIP = range(2)
    materials = [make_material("blade", blade), make_material("tip", tip, contrast=0.8)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        wind = np.array([0.25 * M, 0.0, 0.08 * M])
        blades = []
        for _ in range(26):
            base = np.array([w.uniform(-0.6, 0.6) * M, 0.0, w.uniform(-0.4, 0.4) * M])
            height = w.uniform(0.6, 1.1) * M
            top = base + np.array([w.uniform(-0.12, 0.12) * M, height, w.uniform(-0.1, 0.1) * M]) + wind * (height / M)
            blades.append((tuple(base), tuple(top)))
        result = [Part(lambda p: _union(*(capsule(p, a, b, 0.06 * M, 0.02 * M) for a, b in blades)), BLADE)]
        if ears:
            heads = [b for _, b in blades[::2]]
            result.append(Part(lambda p: _union(*(ellipsoid(p, h, (0.05 * M, 0.13 * M, 0.05 * M)) for h in heads)), TIP))
        else:
            tips = [b for _, b in blades[::3]]
            result.append(Part(lambda p: _union(*(capsule(p, h, (h[0] + 0.08 * M, h[1] + 0.12 * M, h[2]), 0.03 * M)
                                                  for h in tips)), TIP))
        return result

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(56, 56))


def wildflowers(stem: str, seed: int, colours: tuple[str, ...]) -> PropModel:
    STEM = 0
    materials = [make_material("stem", GRASS_DARK)] + [make_material(f"petal{i}", c, contrast=0.6) for i, c in enumerate(colours)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        heads = [(w.uniform(-0.7, 0.7) * M, w.uniform(0.25, 0.55) * M, w.uniform(-0.45, 0.45) * M) for _ in range(12)]
        result = [Part(lambda p: _union(*(capsule(p, (h[0], 0, h[2]), h, 0.035 * M) for h in heads)), STEM)]
        for index in range(len(colours)):
            mine = heads[index::len(colours)]
            result.append(Part(lambda p, mine=mine: _union(*(ellipsoid(p, h, (0.12 * M, 0.07 * M, 0.12 * M)) for h in mine)),
                               1 + index))
        return result

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(64, 44))


def puddle(stem: str, seed: int) -> PropModel:
    """Flaque où se reflète le ciel : presque plate, bord de terre humide."""
    WATER, MUD = range(2)
    materials = [make_material("water", SKY, contrast=0.6), make_material("mud", EARTH_DRY)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        lobes = [(w.uniform(-0.5, 0.5) * M, 0.0, w.uniform(-0.3, 0.3) * M) for _ in range(3)]
        return [
            Part(lambda p: _union(*(ellipsoid(p, c, (0.6 * M, 0.03 * M, 0.42 * M)) for c in lobes)), WATER),
            Part(lambda p: _union(*(ellipsoid(p, (c[0], -0.01 * M, c[2]), (0.72 * M, 0.025 * M, 0.52 * M)) for c in lobes)), MUD),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(110, 64))


# ---------------------------------------------------------------------------------------------------------------------
# Clôtures et murets
# ---------------------------------------------------------------------------------------------------------------------

def stone_wall(stem: str, seed: int, broken: bool) -> PropModel:
    """Muret de pierres sèches : blocs irréguliers sur deux assises, lichen, trouée s'il est écroulé."""
    STONE, STONE_DARK, MOSS = range(3)
    materials = [make_material("stone", FIELD_STONE), make_material("stone_dark", "#5E5E52"), make_material("moss", GRASS)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        light, dark, fallen = [], [], []
        for row, y in enumerate((0.18, 0.5)):
            x = -1.35 + w.uniform(0, 0.2)
            while x < 1.35:
                size = w.uniform(0.22, 0.34)
                gap = broken and -0.35 < x < 0.45 and (row == 1 or w.uniform(0, 1) < 0.5)
                block = ((x * M, y * M, w.uniform(-0.03, 0.03) * M), (size * M, 0.17 * M, 0.26 * M),
                         rotation_y(w.uniform(-0.15, 0.15)))
                if gap:
                    fallen.append(((x * M + w.uniform(-0.2, 0.2) * M, 0.12 * M, w.uniform(0.4, 0.7) * M),
                                   (size * M, 0.13 * M, 0.2 * M), rotation_y(w.uniform(0, 3))))
                else:
                    (light if w.uniform(0, 1) < 0.65 else dark).append(block)
                x += size * 2 + 0.02
        moss = [(b[0][0], 0.68 * M, b[0][2]) for b in light[-5::2]]
        return [
            Part(lambda p: _union(*(rounded_box(p, c, h, 0.06 * M, r) for c, h, r in light + fallen)), STONE),
            Part(lambda p: _union(*(rounded_box(p, c, h, 0.06 * M, r) for c, h, r in dark)), STONE_DARK),
            Part(_clumps(moss, [(0.2 * M, 0.06 * M, 0.18 * M)] * len(moss), 0.02 * M, 0.2 * M), MOSS),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(100, 64), footprint=box_footprint(1.5 * M, 0.3 * M))


def wooden_fence(stem: str, seed: int, broken: bool) -> PropModel:
    WOOD, WOOD_DARK = range(2)
    materials = [make_material("wood", FENCE), make_material("wood_dark", "#4A3E2E")]

    def parts() -> list[Part]:
        w = Weathering(seed)
        posts = []
        for x in (-1.3, 0.0, 1.3):
            tilt = rotation_z(w.uniform(-0.08, 0.08) + (0.35 if broken and x > 0 else 0.0))
            posts.append(((x * M, 0.55 * M, 0), tilt))
        rails = [((-1.3 * M, y * M, 0.06 * M), (1.3 * M, y * M + w.uniform(-0.05, 0.05) * M, 0.06 * M)) for y in (0.45, 0.85)]
        if broken:
            # La travée de droite est tombée : un rail au sol, l'autre pend.
            rails = [((-1.3 * M, y * M, 0.06 * M), (0.0, y * M, 0.06 * M)) for y in (0.45, 0.85)]
            rails += [((0.1 * M, 0.06 * M, 0.35 * M), (1.4 * M, 0.06 * M, 0.55 * M)),
                      ((0.0, 0.85 * M, 0.06 * M), (0.9 * M, 0.3 * M, 0.2 * M))]
        return [
            Part(lambda p: _union(*(rounded_box(p, c, (0.07 * M, 0.55 * M, 0.07 * M), 0.02 * M, r) for c, r in posts)), WOOD_DARK),
            Part(lambda p: _union(*(capsule(p, a, b, 0.05 * M) for a, b in rails)), WOOD),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(100, 64), footprint=box_footprint(1.4 * M, 0.12 * M))


# ---------------------------------------------------------------------------------------------------------------------
# Vestiges agricoles
# ---------------------------------------------------------------------------------------------------------------------

def hay_bale(stem: str, seed: int, rotten: bool) -> PropModel:
    """Balle ronde oubliée : dorée, ou affaissée et moussue quand elle a pourri."""
    STRAW_M, STRAW_DARK, MOSS = range(3)
    materials = [make_material("straw", "#7A6A40" if rotten else STRAW),
                 make_material("straw_dark", "#5A4E34" if rotten else GRASS_GOLD), make_material("moss", GRASS)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        sag = 0.72 if rotten else 1.0
        roll = rotation_z(np.pi / 2)
        result = [
            Part(lambda p: cylinder(p, (0, 0.62 * M * sag, 0), 0.62 * M, 0.7 * M, 0.2 * M, roll @ rotation_x(0.0))
                 if not rotten else ellipsoid(p, (0, 0.45 * M, 0), (0.75 * M, 0.45 * M, 0.66 * M)), STRAW_M),
            # Bandes du filet de liage.
            Part(lambda p: _union(*(cylinder(p, (x * M, 0.62 * M * sag, 0), 0.635 * M * sag, 0.03 * M, 0.01 * M, roll)
                                    for x in (-0.35, 0.35))), STRAW_DARK),
        ]
        if rotten:
            moss = [(w.uniform(-0.4, 0.4) * M, 0.82 * M, w.uniform(-0.3, 0.3) * M) for _ in range(3)]
            result.append(Part(_clumps(moss, [(0.26 * M, 0.08 * M, 0.24 * M)] * 3, 0.03 * M, 0.2 * M), MOSS))
        return result

    return PropModel(stem, parts, materials, AXIS_Y_YAW, canvas=(72, 60), footprint=box_footprint(0.75 * M, 0.66 * M))


def scarecrow(stem: str, seed: int) -> PropModel:
    POLE, SACK, CLOTH, HAT, STRAW_M = range(5)
    materials = [make_material("pole", FENCE), make_material("sack", EARTH_LIGHT), make_material("cloth", FLOWER_RED, contrast=0.8),
                 make_material("hat", "#4A3E2E"), make_material("straw", STRAW)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        lean = rotation_z(0.18)
        # Convention des primitives : local = (p − centre) @ rotation ; ici tout le mannequin penche d'un bloc.
        return [
            Part(lambda p: capsule(p @ lean, (0, 0, 0), (0, 1.9 * M, 0), 0.06 * M), POLE),
            Part(lambda p: capsule(p @ lean, (-0.7 * M, 1.35 * M, 0), (0.55 * M, 1.4 * M, 0), 0.05 * M), POLE),
            Part(lambda p: sphere(p @ lean, (0, 1.95 * M, 0.02 * M), 0.2 * M), SACK),
            Part(lambda p: rounded_box(p @ lean, (0, 1.2 * M, 0.02 * M), (0.3 * M, 0.34 * M, 0.14 * M), 0.08 * M), CLOTH),
            Part(lambda p: _union(cylinder(p @ lean, (0, 2.12 * M, 0), 0.3 * M, 0.02 * M, 0.01 * M),
                                  cylinder(p @ lean, (0, 2.22 * M, 0), 0.15 * M, 0.1 * M, 0.03 * M)), HAT),
            Part(lambda p: _union(*(capsule(p @ lean, (x * M, 1.38 * M, 0), (x * M + w.uniform(-0.1, 0.1) * M, 1.15 * M, 0.05 * M),
                                            0.035 * M) for x in (-0.7, -0.62, 0.55, 0.48))), STRAW_M),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(72, 110), footprint=box_footprint(0.12 * M, 0.12 * M))


def well(stem: str, seed: int) -> PropModel:
    STONE, WOOD, ROOF, WATER, MOSS = range(5)
    materials = [make_material("stone", FIELD_STONE), make_material("wood", FENCE), make_material("roof", RUST),
                 make_material("water", "#2A3A4A", contrast=0.5), make_material("moss", GRASS)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        ring = lambda p: np.maximum(cylinder(p, (0, 0.4 * M, 0), 0.65 * M, 0.4 * M, 0.05 * M),  # noqa: E731
                                    -cylinder(p, (0, 0.45 * M, 0), 0.47 * M, 0.45 * M))
        moss = [(np.cos(a) * 0.6 * M, 0.8 * M, np.sin(a) * 0.6 * M) for a in (0.5, 2.2, 4.0)]
        return [
            Part(ring, STONE),
            Part(lambda p: cylinder(p, (0, 0.55 * M, 0), 0.46 * M, 0.02 * M), WATER),
            Part(lambda p: _union(*(capsule(p, (x * 0.58 * M, 0.7 * M, 0), (x * 0.58 * M, 1.9 * M, 0), 0.05 * M) for x in (-1, 1)),
                                  capsule(p, (-0.6 * M, 1.75 * M, 0), (0.6 * M, 1.75 * M, 0), 0.04 * M)), WOOD),
            Part(lambda p: rounded_box(p, (0, 2.0 * M, 0), (0.78 * M, 0.04 * M, 0.5 * M), 0.02 * M, rotation_z(w.uniform(-0.12, 0.12))), ROOF),
            Part(_clumps(moss, [(0.18 * M, 0.08 * M, 0.18 * M)] * 3, 0.02 * M, 0.2 * M), MOSS),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(72, 100), footprint=box_footprint(0.65 * M, 0.65 * M))


def standing_stone(stem: str, seed: int) -> PropModel:
    STONE, LICHEN = range(2)
    materials = [make_material("stone", FIELD_STONE), make_material("lichen", GRASS_PALE, contrast=0.6)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        tilt = rotation_z(w.uniform(-0.1, 0.1))
        spots = [(w.uniform(-0.2, 0.2) * M, w.uniform(0.6, 1.9) * M, 0.26 * M) for _ in range(4)]
        return [
            Part(lambda p: ellipsoid(p @ tilt, (0, 1.2 * M, 0), (0.38 * M, 1.25 * M, 0.28 * M)), STONE),
            Part(lambda p: _union(*(ellipsoid(p, s, (0.1 * M, 0.08 * M, 0.04 * M)) for s in spots)), LICHEN),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(48, 100), footprint=box_footprint(0.36 * M, 0.26 * M))


def rusted_plow(stem: str, seed: int) -> PropModel:
    FRAME, BLADE, WHEEL, GRASS_M = range(4)
    materials = [make_material("frame", RUST), make_material("blade", RUST_ORANGE), make_material("wheel", "#3A3530"),
                 make_material("grass", GRASS)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        blades = [(x * M, 0.2 * M, z * M) for x, z in ((-0.3, -0.4), (0.0, 0.0), (0.3, 0.4))]
        tufts = [(w.uniform(-0.6, 0.6) * M, 0.1 * M, w.uniform(-0.7, 0.7) * M) for _ in range(3)]
        return [
            Part(lambda p: _union(capsule(p, (-0.5 * M, 0.55 * M, -0.8 * M), (0.5 * M, 0.55 * M, 0.8 * M), 0.07 * M),
                                  capsule(p, (0.5 * M, 0.55 * M, 0.8 * M), (0.6 * M, 0.7 * M, 1.4 * M), 0.06 * M)), FRAME),
            Part(lambda p: _union(*(rounded_box(p, b, (0.05 * M, 0.25 * M, 0.2 * M), 0.02 * M, rotation_y(0.6)) for b in blades)), BLADE),
            Part(lambda p: cylinder(p, (-0.55 * M, 0.35 * M, -0.8 * M), 0.35 * M, 0.06 * M, 0.02 * M, rotation_z(np.pi / 2)), WHEEL),
            Part(_clumps(tufts, [(0.2 * M, 0.14 * M, 0.2 * M)] * 3, 0.03 * M, 0.2 * M), GRASS_M),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(80, 64), footprint=box_footprint(0.6 * M, 1.1 * M))


def tractor(stem: str, seed: int, husk: bool) -> PropModel:
    """Tracteur abandonné : peinture passée et rouille, ou carcasse brûlée affaissée sur ses jantes."""
    PAINT, RUST_M, TYRE, METAL, GRASS_M, GLASS = range(6)
    materials = [make_material("paint", "#3A3530" if husk else "#8E4A34"), make_material("rust", RUST),
                 make_material("tyre", "#221F24", contrast=0.5), make_material("metal", "#6B6161"), make_material("grass", GRASS),
                 make_material("glass", "#2A3644", contrast=0.6)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        drop = 0.18 * M if husk else 0.0
        result = [
            Part(lambda p: rounded_box(p, (0, 1.0 * M - drop, 0.6 * M), (0.45 * M, 0.4 * M, 0.95 * M), 0.12 * M), PAINT),
            Part(lambda p: rounded_box(p, (0, 1.1 * M - drop, -0.75 * M), (0.6 * M, 0.5 * M, 0.45 * M), 0.08 * M), PAINT),
            Part(lambda p: capsule(p, (0.2 * M, 1.4 * M - drop, 1.0 * M), (0.2 * M, 2.1 * M - drop, 1.0 * M), 0.07 * M), METAL),
            Part(lambda p: _union(*(cylinder(p, (x * 0.72 * M, 0.72 * M - drop * 0.5, -0.75 * M), 0.72 * M, 0.2 * M, 0.08 * M,
                                             rotation_z(np.pi / 2)) for x in (-1, 1))), TYRE),
            Part(lambda p: _union(*(cylinder(p, (x * 0.5 * M, 0.4 * M - drop * 0.5, 1.1 * M), 0.4 * M, 0.14 * M, 0.06 * M,
                                             rotation_z(np.pi / 2)) for x in (-1, 1))), TYRE),
            Part(lambda p: _patch(p, w_points), RUST_M),
        ]
        if not husk:
            result.append(Part(lambda p: rounded_box(p, (0, 1.9 * M, -0.75 * M), (0.5 * M, 0.35 * M, 0.4 * M), 0.05 * M), GLASS))
        tufts = [(w.uniform(-0.9, 0.9) * M, 0.12 * M, w.uniform(-1.4, 1.5) * M) for _ in range(5)]
        result.append(Part(_clumps(tufts, [(0.25 * M, 0.18 * M, 0.25 * M)] * 5, 0.03 * M, 0.2 * M), GRASS_M))
        return result

    rng = Weathering(seed + 7)
    w_points = [(rng.uniform(-0.45, 0.45) * M, rng.uniform(0.7, 1.4) * M, rng.uniform(-1.0, 1.4) * M) for _ in range(9)]
    return PropModel(stem, parts, materials, AXIS_Y_YAW, canvas=(110, 110), footprint=box_footprint(0.9 * M, 1.45 * M))


def _patch(p: np.ndarray, points) -> np.ndarray:
    return _union(*(sphere(p, c, 0.2 * M) for c in points))


def wind_pump(stem: str, seed: int) -> PropModel:
    """Éolienne de pompage : pylône en treillis et roue à pales, dont une partie a cédé. Repère visible de loin."""
    STEEL, VANE, RUST_M = range(3)
    materials = [make_material("steel", "#8A8478"), make_material("vane", "#B8B0A0", contrast=0.8), make_material("rust", RUST)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        top = 5.6 * M
        feet = [(x * 0.9 * M, 0, z * 0.9 * M) for x, z in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
        head = [(x * 0.18 * M, top, z * 0.18 * M) for x, z in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
        legs = list(zip(feet, head))
        braces = []
        for level in (0.3, 0.6):
            ring = [tuple(np.array(a) + (np.array(b) - np.array(a)) * level) for a, b in legs]
            braces += list(zip(ring, ring[1:] + ring[:1]))
        hub = (0, top + 0.3 * M, 0.35 * M)
        vanes = []
        for index in range(12):
            if index in (3, 4, 9):
                continue  # pales arrachées
            angle = index * np.pi / 6
            tip = (np.cos(angle) * 1.2 * M, top + 0.3 * M + np.sin(angle) * 1.2 * M, 0.45 * M)
            vanes.append(tip)
        return [
            Part(lambda p: _union(*(capsule(p, a, b, 0.06 * M) for a, b in legs + braces)), STEEL),
            Part(lambda p: _union(*(capsule(p, hub, t, 0.12 * M, 0.2 * M) for t in vanes)), VANE),
            Part(lambda p: _union(sphere(p, hub, 0.2 * M),
                                  capsule(p, (0, top + 0.3 * M, 0.3 * M), (0, top + 0.3 * M, -1.1 * M), 0.08 * M),
                                  rounded_box(p, (0, top + 0.3 * M, -1.1 * M), (0.04 * M, 0.35 * M, 0.3 * M), 0.02 * M)), RUST_M),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(90, 170), footprint=box_footprint(0.9 * M, 0.9 * M),
                     bounds=((-1.6 * M, -0.1 * M, -1.5 * M), (1.6 * M, 7.4 * M, 1.5 * M)))


def silo(stem: str, seed: int) -> PropModel:
    """Silo à grain effondré : tôle ondulée cerclée, toit crevé, rouille qui coule."""
    SHEET, BAND, RUST_M, GRASS_M = range(4)
    materials = [make_material("sheet", "#A0A098"), make_material("band", "#6B6161"), make_material("rust", RUST),
                 make_material("grass", GRASS)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        height = 4.6 * M
        # Toit crevé : le cylindre est entaillé en biais au sommet.
        cut = lambda p: np.maximum(cylinder(p, (0, height / 2, 0), 1.2 * M, height / 2, 0.05 * M),  # noqa: E731
                                   (p[:, 1] - height + 0.25 * p[:, 0] + 0.5 * M))
        # Coulures de rouille : longues traînées verticales qui partent des cerclages.
        rust = [(np.cos(a) * 1.2 * M, y * M - 0.5 * M, np.sin(a) * 1.2 * M)
                for a, y in zip(np.linspace(0.4, 2.8, 4), (2.1, 3.3, 0.9, 3.3))]
        tufts = [(np.cos(a) * 1.3 * M, 0.12 * M, np.sin(a) * 1.3 * M) for a in np.linspace(0.2, 3.3, 5)]
        return [
            Part(cut, SHEET),
            Part(lambda p: _union(*(np.maximum(cylinder(p, (0, y * M, 0), 1.24 * M, 0.05 * M),
                                               p[:, 1] - height + 0.25 * p[:, 0] + 0.5 * M) for y in (0.9, 2.1, 3.3))), BAND),
            Part(lambda p: _union(*(ellipsoid(p, c, (0.09 * M, 0.55 * M, 0.09 * M)) for c in rust)), RUST_M),
            Part(_clumps(tufts, [(0.3 * M, 0.2 * M, 0.3 * M)] * 5, 0.03 * M, 0.2 * M), GRASS_M),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(110, 170), footprint=box_footprint(1.2 * M, 1.2 * M),
                     bounds=((-1.6 * M, -0.1 * M, -1.6 * M), (1.6 * M, 5.2 * M, 1.6 * M)))


# ---------------------------------------------------------------------------------------------------------------------
# Catalogue
# ---------------------------------------------------------------------------------------------------------------------

def catalog() -> list[PropModel]:
    models: list[PropModel] = []
    # Arbre isolé des champs : chêne large et bas, feuillage de la palette des champs.
    models += tree("prop_solitary_tree", 501, 6.2 * M, 2.9 * M, 0.36 * M, 24, "#4A3728", GRASS_DARK, GRASS, False, (160, 190))
    models += [
        tall_grass("prop_tall_grass", 511, GRASS, GRASS_PALE, False),
        tall_grass("prop_tall_grass_v2", 512, GRASS_DARK, GRASS, False),
        tall_grass("prop_tall_grass_v3", 513, GRASS_GOLD, GRASS_PALE, True),
        wildflowers("prop_poppies_red", 521, (FLOWER_RED,)),
        wildflowers("prop_poppies_blue", 522, (FLOWER_BLUE,)),
        wildflowers("prop_poppies_mixed", 523, (FLOWER_RED, FLOWER_BLUE, GRASS_PALE)),
        stone_wall("prop_low_stone_wall", 531, False),
        stone_wall("prop_low_stone_wall_broken", 532, True),
        wooden_fence("prop_wooden_fence", 541, False),
        wooden_fence("prop_wooden_fence_broken", 542, True),
        hay_bale("prop_hay_bale", 551, False),
        hay_bale("prop_hay_bale_rotten", 552, True),
        puddle("prop_puddle", 561),
        puddle("prop_puddle_v2", 562),
        scarecrow("prop_scarecrow_broken", 571),
        well("prop_abandoned_well", 581),
        standing_stone("prop_standing_stone", 591),
        rusted_plow("prop_rusted_plow", 601),
        tractor("prop_tractor_remains", 611, False),
        tractor("prop_tractor_husk", 612, True),
        wind_pump("prop_windmill_ruin", 621),
        silo("prop_silo_ruin", 631),
    ]
    return models
