"""
Décors de la Carrière Effondrée (plan 08, lot P6) : une mine industrielle écroulée sur elle-même.

Échelle réelle du personnage (1 m ≈ 27,5 unités). Palette « Carrière » de la charte (§3) : roches sombre, grise et
claire, terre rouge du minerai, métal industriel et rouille profonde des machines, bois de mine des étais, et les
seules lumières du biome : les cristaux d'Essence bleus piégés dans la roche et l'orange des lampes de sécurité
qui clignotent encore (Bible §5.4). Les machines sont figées en plein mouvement, les ouvriers sont partis.
"""
from __future__ import annotations

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, cylinder, ellipsoid, rotation_x, rotation_y, rotation_z, rounded_box, sphere
from ._kit import AXIS_X_YAW, AXIS_Y_YAW, M, PropModel, Weathering, box_footprint
from .buildings import BUILDING_YAW
from .forest import _union

# Palette Carrière (charte §3).
ROCK_DARK = "#3A3030"
ROCK_GREY = "#5A5050"
ROCK_LIGHT = "#8A7A6A"
RED_EARTH = "#6A3A28"
METAL = "#5A6A7A"
RUST_DEEP = "#5A2A18"
CRYSTAL = "#4ABAE0"
CRYSTAL_BRIGHT = "#7AE0F0"
MINE_WOOD = "#6A5038"
COAL = "#2A2222"
OCHRE_DUST = "#B4A080"
# Palette maîtresse : orange des lumières de sécurité et jaune des machines de chantier.
SAFETY_ORANGE = "#E07B39"
MACHINE_YELLOW = "#C49B3E"
CLOTH = "#9E9494"
HUT_WALL = "#6E7A74"
PLANK_LIGHT = "#8A6A48"


def _rocks(w: Weathering, count: int, spread: tuple[float, float], size: tuple[float, float], height: float):
    """Blocs anguleux empilés : boîtes à peine arrondies, tournées au hasard, plus hautes au centre."""
    blocks = []
    for index in range(count):
        x, z = w.uniform(-spread[0], spread[0]) * M, w.uniform(-spread[1], spread[1]) * M
        centrality = 1.0 - min(1.0, (abs(x) / (spread[0] * M + 1e-6) + abs(z) / (spread[1] * M + 1e-6)) / 2)
        half = np.array([w.uniform(*size), w.uniform(*size) * (0.6 + height * centrality), w.uniform(*size)]) * M
        center = (x, half[1] * 0.85, z)
        rotation = rotation_y(w.uniform(0, np.pi)) @ rotation_z(w.uniform(-0.25, 0.25)) @ rotation_x(w.uniform(-0.2, 0.2))
        blocks.append((center, half, rotation))
    return blocks


def _align_y(direction: np.ndarray) -> np.ndarray:
    """Rotation qui porte l'axe Y local sur `direction` (local = p @ rotation), pour une poutre entre deux points."""
    y = direction / np.linalg.norm(direction)
    helper = np.array([0.0, 0.0, 1.0]) if abs(y[2]) < 0.9 else np.array([1.0, 0.0, 0.0])
    x = np.cross(y, helper)
    x /= np.linalg.norm(x)
    z = np.cross(x, y)
    return np.stack([x, y, z], axis=1)


def _block_union(blocks, rounding: float = 0.08 * M):
    return lambda p: _union(*(rounded_box(p, c, h, rounding, r) for c, h, r in blocks))


def _crystals(w: Weathering, anchors: list[np.ndarray], count: int, length: tuple[float, float], radius: float):
    """Prismes de cristal : capsules effilées qui jaillissent en gerbe d'un point d'ancrage."""
    shards = []
    for index in range(count):
        base = anchors[index % len(anchors)]
        direction = np.array([w.uniform(-0.7, 0.7), w.uniform(0.6, 1.0), w.uniform(-0.3, 0.7)])
        direction /= np.linalg.norm(direction)
        shards.append((base, base + direction * w.uniform(*length) * M, radius * w.uniform(0.7, 1.2)))
    return shards


# ---------------------------------------------------------------------------------------------------------------------
# Roche et Essence
# ---------------------------------------------------------------------------------------------------------------------

def rock_outcrop(stem: str, seed: int, scale: float) -> PropModel:
    """Affleurement : pile de blocs taillés, strate de terre rouge, poussière ocre au pied."""
    DARK, GREY, LIGHT, EARTH = range(4)
    materials = [make_material("rock_dark", ROCK_DARK), make_material("rock_grey", ROCK_GREY), make_material("rock_light", ROCK_LIGHT),
                 make_material("red_earth", RED_EARTH)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        blocks = _rocks(w, 7, (0.9 * scale, 0.6 * scale), (0.3 * scale, 0.55 * scale), 1.2)
        order = sorted(range(len(blocks)), key=lambda i: blocks[i][0][1])
        low, mid, high = order[:2], order[2:5], order[5:]
        seam = [(np.array(blocks[i][0]) + np.array([0.0, 0.0, blocks[i][1][2] * 0.9]), blocks[i][1]) for i in mid[:2]]
        return [
            Part(_block_union([blocks[i] for i in low]), DARK),
            Part(_block_union([blocks[i] for i in mid]), GREY),
            Part(_block_union([blocks[i] for i in high]), LIGHT),
            Part(lambda p: _union(*(ellipsoid(p, c, (h[0] * 0.8, h[1] * 0.18, 0.06 * M)) for c, h in seam)), EARTH),
        ]

    half = 1.0 * M * scale
    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(int(110 * scale) + 20, int(100 * scale) + 20),
                     footprint=box_footprint(half, half * 0.7))


def boulder(stem: str, seed: int) -> PropModel:
    """Bloc éboulé seul, fendu, avec ses éclats au pied."""
    GREY, LIGHT, DARK = range(3)
    materials = [make_material("rock_grey", ROCK_GREY), make_material("rock_light", ROCK_LIGHT), make_material("rock_dark", ROCK_DARK)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        rot = rotation_y(w.uniform(0, np.pi)) @ rotation_z(0.12)
        chips = [((w.uniform(-0.9, 0.9) * M, 0.08 * M, w.uniform(0.3, 0.8) * M), w.uniform(0.08, 0.15) * M) for _ in range(5)]

        def body(p: np.ndarray) -> np.ndarray:
            block = rounded_box(p, (0, 0.55 * M, 0), (0.7 * M, 0.55 * M, 0.55 * M), 0.2 * M, rot)
            crack = np.abs(((p - np.array([0.0, 0.55 * M, 0.0])) @ rotation_z(0.5))[:, 0]) - 0.03 * M
            return np.maximum(block, -crack)

        return [
            Part(body, GREY),
            Part(lambda p: np.maximum(body(p), 0.8 * M - p[:, 1]) - 0.005 * M, LIGHT),
            Part(lambda p: _union(*(sphere(p, c, r) for c, r in chips)), DARK),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(80, 70), footprint=box_footprint(0.7 * M, 0.55 * M))


def gravel_pile(stem: str, seed: int, tone: str) -> PropModel:
    """Tas de déblais : cône de gravier, quelques pierres roulées au pied. Ne bloque pas."""
    GRAVEL, STONES = range(2)
    materials = [make_material("gravel", tone), make_material("stones", ROCK_GREY)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        stones = [((w.uniform(-0.9, 0.9) * M, 0.07 * M, w.uniform(-0.6, 0.7) * M), w.uniform(0.06, 0.12) * M) for _ in range(9)]
        k = 2 * np.pi / (0.18 * M)

        def cone(p: np.ndarray) -> np.ndarray:
            radial = np.sqrt(p[:, 0] ** 2 + (p[:, 2] * 1.3) ** 2)
            grain = 0.02 * M * np.sin(p[:, 0] * k) * np.sin(p[:, 2] * k + 0.6)
            return np.maximum((radial * 0.55 + p[:, 1] - 0.5 * M) / 1.15, -p[:, 1]) + grain

        return [Part(cone, GRAVEL), Part(lambda p: _union(*(sphere(p, c, r) for c, r in stones)), STONES)]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(80, 50))


def crystal_vein(stem: str, seed: int) -> PropModel:
    """Veine d'Essence : un bloc de roche sombre fendu, d'où jaillissent des prismes bleus lumineux."""
    DARK, GREY, GLOW, CORE = range(4)
    materials = [make_material("rock_dark", ROCK_DARK), make_material("rock_grey", ROCK_GREY), make_emissive("crystal", CRYSTAL),
                 make_emissive("crystal_core", CRYSTAL_BRIGHT)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        blocks = _rocks(w, 5, (0.6, 0.4), (0.25, 0.42), 0.7)
        anchors = [np.array([w.uniform(-0.35, 0.35) * M, w.uniform(0.5, 0.8) * M, w.uniform(0.15, 0.4) * M]) for _ in range(3)]
        shards = _crystals(w, anchors, 11, (0.7, 1.4), 0.16 * M)
        return [
            Part(_block_union(blocks[:3]), DARK),
            Part(_block_union(blocks[3:]), GREY),
            Part(lambda p: _union(*(capsule(p, a, b, r, r * 0.2) for a, b, r in shards[3:])), GLOW),
            Part(lambda p: _union(*(capsule(p, a, b, r, r * 0.2) for a, b, r in shards[:3])), CORE),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(110, 120), footprint=box_footprint(0.7 * M, 0.5 * M))


def crystal_cluster(stem: str, seed: int) -> PropModel:
    """Petite gerbe de cristaux à fleur de sol, dans une poche de charbon. Ne bloque pas."""
    COAL_M, GLOW, CORE = range(3)
    materials = [make_material("coal", COAL, contrast=0.7), make_emissive("crystal", CRYSTAL), make_emissive("crystal_core", CRYSTAL_BRIGHT)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        anchors = [np.array([w.uniform(-0.2, 0.2) * M, 0.05 * M, w.uniform(-0.15, 0.15) * M]) for _ in range(2)]
        shards = _crystals(w, anchors, 7, (0.45, 0.9), 0.12 * M)
        return [
            Part(lambda p: ellipsoid(p, (0, 0.03 * M, 0), (0.45 * M, 0.08 * M, 0.35 * M)), COAL_M),
            Part(lambda p: _union(*(capsule(p, a, b, r, r * 0.2) for a, b, r in shards[2:])), GLOW),
            Part(lambda p: _union(*(capsule(p, a, b, r, r * 0.2) for a, b, r in shards[:2])), CORE),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(70, 70))


# ---------------------------------------------------------------------------------------------------------------------
# Machines figées
# ---------------------------------------------------------------------------------------------------------------------

def mine_cart(stem: str, seed: int) -> PropModel:
    """Wagonnet sur son bout de rail, renversé à moitié, minerai rouge répandu."""
    RUST_M, METAL_M, ORE, RAIL, SLEEPER = range(5)
    materials = [make_material("rust", RUST_DEEP), make_material("metal", METAL), make_material("ore", RED_EARTH),
                 make_material("rail", METAL, contrast=0.7), make_material("sleeper", MINE_WOOD)]
    tilt = rotation_z(0.35)

    def parts() -> list[Part]:
        w = Weathering(seed)
        local = lambda p: (p - np.array([0.1 * M, 0.55 * M, 0.0])) @ tilt

        def tub(p: np.ndarray) -> np.ndarray:
            q = local(p)
            outer = rounded_box(q, (0, 0, 0), (0.55 * M, 0.35 * M, 0.75 * M), 0.06 * M)
            inner = rounded_box(q, (0, 0.12 * M, 0), (0.47 * M, 0.35 * M, 0.67 * M), 0.04 * M)
            return np.maximum(outer, -inner)

        ore = [((w.uniform(-1.1, -0.3) * M, 0.08 * M, w.uniform(-0.5, 0.5) * M), w.uniform(0.07, 0.14) * M) for _ in range(8)]
        return [
            Part(tub, RUST_M),
            Part(lambda p: _union(*(cylinder(local(p), (x * M, -0.4 * M, z * M), 0.16 * M, 0.04 * M, 0.02 * M, rotation_z(np.pi / 2))
                                    for x in (-0.5, 0.5) for z in (-0.45, 0.45))), METAL_M),
            Part(lambda p: _union(*(sphere(p, c, r) for c, r in ore)), ORE),
            Part(lambda p: _union(*(rounded_box(p, (x * M, 0.04 * M, 0), (0.035 * M, 0.035 * M, 1.4 * M), 0.01 * M) for x in (-0.35, 0.35))), RAIL),
            Part(lambda p: _union(*(rounded_box(p, (0, 0.02 * M, z * M), (0.6 * M, 0.025 * M, 0.1 * M), 0.01 * M) for z in (-1.1, -0.4, 0.3, 1.0))), SLEEPER),
        ]

    return PropModel(stem, parts, materials, AXIS_Y_YAW, canvas=(90, 80), footprint=box_footprint(0.6 * M, 0.8 * M))


def rails(stem: str, seed: int) -> PropModel:
    """Voie étroite qui s'arrête dans le vide : deux rails tordus sur leurs traverses. Décalque au sol."""
    RAIL, SLEEPER = range(2)
    materials = [make_material("rail", METAL, contrast=0.7), make_material("sleeper", MINE_WOOD)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        sleepers = [((w.uniform(-0.05, 0.05) * M, 0.02 * M, z * M), rotation_y(w.uniform(-0.12, 0.12))) for z in np.linspace(-1.6, 1.6, 7)]
        bend = [np.array([0.0, 0.05 * M, z * M]) for z in np.linspace(-1.8, 1.8, 6)]
        bend[-1] = bend[-1] + np.array([0.25 * M, 0.25 * M, 0.0])
        return [
            Part(lambda p: _union(*(capsule(p, a + np.array([x * M, 0, 0]), b + np.array([x * M, 0, 0]), 0.035 * M)
                                    for x in (-0.35, 0.35) for a, b in zip(bend, bend[1:]))), RAIL),
            Part(lambda p: _union(*(rounded_box(p, c, (0.6 * M, 0.025 * M, 0.1 * M), 0.01 * M, r) for c, r in sleepers)), SLEEPER),
        ]

    return PropModel(stem, parts, materials, AXIS_Y_YAW, canvas=(80, 70))


def excavator_bucket(stem: str, seed: int) -> PropModel:
    """Godet géant de pelleteuse, dents plantées dans la roche, bras encore dressé : repère vu de loin."""
    YELLOW, RUST_M, METAL_M, ROCK = range(4)
    materials = [make_material("machine", MACHINE_YELLOW), make_material("rust", RUST_DEEP), make_material("metal", METAL),
                 make_material("rock", ROCK_GREY)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        rot = rotation_x(0.35)
        local = lambda p: (p - np.array([0.0, 0.7 * M, 0.2 * M])) @ rot

        def bucket(p: np.ndarray) -> np.ndarray:
            q = local(p)
            outer = rounded_box(q, (0, 0, 0), (1.0 * M, 0.7 * M, 0.7 * M), 0.25 * M)
            inner = rounded_box(q, (0, 0.2 * M, 0.25 * M), (0.9 * M, 0.7 * M, 0.7 * M), 0.2 * M)
            return np.maximum(outer, -inner)

        teeth = [np.array([x * M, 0.05 * M, 0.95 * M]) for x in np.linspace(-0.8, 0.8, 5)]
        arm = [np.array([0.0, 1.3 * M, -0.3 * M]), np.array([0.2 * M, 3.0 * M, -1.1 * M]), np.array([0.3 * M, 4.1 * M, -0.2 * M])]
        rust = [np.array([w.uniform(-0.8, 0.8) * M, w.uniform(0.5, 1.3) * M, w.uniform(-0.4, 0.4) * M]) for _ in range(5)]
        rocks = [((w.uniform(-1.3, 1.3) * M, 0.12 * M, w.uniform(0.6, 1.3) * M), w.uniform(0.12, 0.25) * M) for _ in range(6)]
        return [
            Part(bucket, YELLOW),
            Part(lambda p: np.maximum(_union(*(sphere(p, c, 0.28 * M) for c in rust)), bucket(p) - 0.03 * M), RUST_M),
            Part(lambda p: _union(*(capsule(p, t, t + np.array([0.0, -0.1 * M, 0.3 * M]), 0.08 * M, 0.03 * M) for t in teeth),
                                  *(sphere(p, joint, 0.3 * M) for joint in arm)), METAL_M),
            Part(lambda p: _union(*(rounded_box(p, (a + b) / 2, (0.24 * M, np.linalg.norm(b - a) / 2, 0.2 * M), 0.08 * M,
                                                _align_y(b - a)) for a, b in zip(arm, arm[1:]))), YELLOW),
            Part(lambda p: _union(*(sphere(p, c, r) for c, r in rocks)), ROCK),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(130, 170), footprint=box_footprint(1.1 * M, 0.9 * M))


def conveyor(stem: str, seed: int) -> PropModel:
    """Tronçon de convoyeur incliné, tapis déchiré qui pend, pieds rouillés, minerai resté dessus."""
    FRAME, BELT, RUST_M, ORE = range(4)
    materials = [make_material("frame", METAL), make_material("belt", COAL, contrast=0.7), make_material("rust", RUST_DEEP),
                 make_material("ore", RED_EARTH)]
    incline = rotation_x(-0.35)

    def parts() -> list[Part]:
        w = Weathering(seed)
        local = lambda p: (p - np.array([0.0, 1.2 * M, 0.0])) @ incline
        legs = [(np.array([x * M, 0.0, z * M]), np.array([x * M, (1.2 - z * 0.35) * M, z * M])) for x in (-0.4, 0.4) for z in (-1.3, 1.1)]
        ore = [np.array([w.uniform(-0.25, 0.25) * M, 0.12 * M, w.uniform(-1.4, 0.9) * M]) for _ in range(6)]
        droop = [np.array([0.0, -0.05 * M, 1.7 * M]), np.array([0.05 * M, -0.5 * M, 2.0 * M]), np.array([0.0, -1.0 * M, 2.1 * M])]
        return [
            Part(lambda p: _union(*(rounded_box(local(p), (x * M, 0, 0), (0.05 * M, 0.1 * M, 1.8 * M), 0.02 * M) for x in (-0.42, 0.42))), FRAME),
            Part(lambda p: _union(rounded_box(local(p), (0, 0.05 * M, -0.1 * M), (0.36 * M, 0.03 * M, 1.7 * M), 0.02 * M),
                                  *(capsule(local(p), a, b, 0.05 * M, 0.03 * M) for a, b in zip(droop, droop[1:]))), BELT),
            Part(lambda p: _union(*(capsule(p, a, b, 0.06 * M) for a, b in legs)), RUST_M),
            Part(lambda p: _union(*(sphere(local(p), c, 0.09 * M) for c in ore)), ORE),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(110, 110), footprint=box_footprint(0.45 * M, 1.4 * M))


def cable_drum(stem: str, seed: int) -> PropModel:
    """Touret de câble couché sur la tranche, câble d'acier déroulé qui serpente au sol."""
    WOOD, CABLE, METAL_M = range(3)
    materials = [make_material("drum", MINE_WOOD), make_material("cable", COAL, contrast=0.8), make_material("hub", METAL)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        axis = rotation_z(np.pi / 2)
        trail = [np.array([0.3 * M, 0.35 * M, 0.35 * M])] + [np.array([0.5 * M + i * 0.35 * M, 0.04 * M, 0.5 * M + np.sin(i) * 0.3 * M]) for i in range(5)]
        return [
            Part(lambda p: _union(cylinder(p, (-0.3 * M, 0.6 * M, 0), 0.6 * M, 0.05 * M, 0.02 * M, axis),
                                  cylinder(p, (0.3 * M, 0.6 * M, 0), 0.6 * M, 0.05 * M, 0.02 * M, axis)), WOOD),
            Part(lambda p: _union(cylinder(p, (0, 0.6 * M, 0), 0.42 * M, 0.26 * M, 0.08 * M, axis),
                                  *(capsule(p, a, b, 0.04 * M) for a, b in zip(trail, trail[1:]))), CABLE),
            Part(lambda p: cylinder(p, (0, 0.6 * M, 0), 0.12 * M, 0.38 * M, 0.02 * M, axis), METAL_M),
        ]

    return PropModel(stem, parts, materials, AXIS_Y_YAW, canvas=(80, 70), footprint=box_footprint(0.4 * M, 0.6 * M))


def safety_lamp(stem: str, seed: int) -> PropModel:
    """Gyrophare de chantier sur son poteau : l'orange qui clignote encore, alimenté par on ne sait quoi."""
    POLE, BOX, GLOW = range(3)
    materials = [make_material("pole", METAL), make_material("box", RUST_DEEP), make_emissive("safety", SAFETY_ORANGE)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        lean = rotation_z(w.uniform(-0.08, 0.08))
        return [
            Part(lambda p: _union(capsule(p @ lean, (0, 0, 0), (0, 2.2 * M, 0), 0.06 * M),
                                  cylinder(p, (0, 0.05 * M, 0), 0.25 * M, 0.05 * M, 0.02 * M)), POLE),
            Part(lambda p: rounded_box(p @ lean, (0.12 * M, 1.1 * M, 0.0), (0.12 * M, 0.18 * M, 0.1 * M), 0.02 * M), BOX),
            Part(lambda p: ellipsoid(p @ lean, (0, 2.35 * M, 0), (0.14 * M, 0.17 * M, 0.14 * M)), GLOW),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(44, 90))


# ---------------------------------------------------------------------------------------------------------------------
# Soutènements et traces des ouvriers
# ---------------------------------------------------------------------------------------------------------------------

def mine_supports(stem: str, seed: int) -> PropModel:
    """Cadre de soutènement : deux étais et un chapeau, l'un cassé, des planches de coffrage derrière."""
    WOOD, PLANK, ROCK = range(3)
    materials = [make_material("timber", MINE_WOOD), make_material("plank", "#8A6A48"), make_material("rock", ROCK_DARK)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        broken = rotation_z(-0.4)
        planks = [(np.array([x * M, w.uniform(0.6, 1.3) * M, -0.2 * M]), rotation_z(w.uniform(-0.1, 0.1))) for x in (-0.6, -0.2, 0.3)]
        rubble = [((w.uniform(-0.9, 0.9) * M, 0.12 * M, w.uniform(-0.3, 0.4) * M), w.uniform(0.12, 0.22) * M) for _ in range(5)]
        return [
            Part(lambda p: _union(cylinder(p, (-0.8 * M, 1.1 * M, 0), 0.12 * M, 1.1 * M, 0.03 * M),
                                  cylinder((p - np.array([0.8 * M, 0.0, 0.0])) @ broken, (0, 0.9 * M, 0), 0.12 * M, 0.9 * M, 0.03 * M),
                                  rounded_box(p @ rotation_z(-0.12), (0, 2.2 * M, 0), (1.05 * M, 0.12 * M, 0.14 * M), 0.03 * M)), WOOD),
            Part(lambda p: _union(*(rounded_box(p, c, (0.18 * M, 0.7 * M, 0.03 * M), 0.01 * M, r) for c, r in planks)), PLANK),
            Part(lambda p: _union(*(sphere(p, c, r) for c, r in rubble)), ROCK),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(90, 110), footprint=box_footprint(0.95 * M, 0.2 * M))


def worker_locker(stem: str, seed: int) -> PropModel:
    """Vestiaire renversé ouvert : un casque, un thermos, une photo punaisée. Les ouvriers sont partis."""
    STEEL, INSIDE, HELMET, THERMOS, PHOTO = range(5)
    materials = [make_material("locker", "#5A6A5A"), make_material("inside", COAL, contrast=0.6), make_material("helmet", MACHINE_YELLOW),
                 make_material("thermos", "#8A3A2A"), make_material("photo", "#E8E0D4", contrast=0.5)]
    tilt = rotation_z(-0.18)

    def parts() -> list[Part]:
        local = lambda p: p @ tilt

        def shell(p: np.ndarray) -> np.ndarray:
            q = local(p)
            outer = rounded_box(q, (0, 0.95 * M, 0), (0.32 * M, 0.95 * M, 0.26 * M), 0.02 * M)
            inner = rounded_box(q, (0, 0.95 * M, 0.08 * M), (0.27 * M, 0.9 * M, 0.26 * M), 0.02 * M)
            return np.maximum(outer, -inner)

        door = lambda p: rounded_box(local(p) @ rotation_y(-1.1), (0.3 * M, 0.95 * M, 0.02 * M), (0.3 * M, 0.9 * M, 0.02 * M), 0.01 * M)
        return [
            Part(lambda p: _union(shell(p), door(p)), STEEL),
            Part(lambda p: rounded_box(local(p), (0, 0.95 * M, -0.2 * M), (0.27 * M, 0.9 * M, 0.02 * M), 0.01 * M), INSIDE),
            Part(lambda p: np.maximum(sphere(p, (0.55 * M, 0.1 * M, 0.45 * M), 0.2 * M), 0.08 * M - p[:, 1]), HELMET),
            Part(lambda p: cylinder(p, (-0.45 * M, 0.1 * M, 0.5 * M), 0.08 * M, 0.2 * M, 0.03 * M, rotation_x(np.pi / 2) @ rotation_y(0.7)), THERMOS),
            Part(lambda p: rounded_box(local(p), (0.05 * M, 1.4 * M, -0.17 * M), (0.1 * M, 0.08 * M, 0.01 * M), 0.005 * M), PHOTO),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(70, 90), footprint=box_footprint(0.35 * M, 0.3 * M))


def explosives_crate(stem: str, seed: int) -> PropModel:
    """Caisse d'explosifs éventrée, couvercle de travers, bâtons rouges épars."""
    WOOD, LID, STICK, BAND = range(4)
    materials = [make_material("crate", MINE_WOOD), make_material("lid", "#8A6A48"), make_material("dynamite", "#A83A2A"),
                 make_material("band", OCHRE_DUST, contrast=0.6)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        sticks = [((w.uniform(-0.6, 0.6) * M, 0.05 * M, w.uniform(0.35, 0.7) * M), rotation_y(w.uniform(0, np.pi))) for _ in range(4)]

        def crate(p: np.ndarray) -> np.ndarray:
            outer = rounded_box(p, (0, 0.3 * M, 0), (0.45 * M, 0.3 * M, 0.32 * M), 0.02 * M)
            inner = rounded_box(p, (0, 0.4 * M, 0), (0.4 * M, 0.3 * M, 0.27 * M), 0.02 * M)
            return np.maximum(outer, -inner)

        return [
            Part(crate, WOOD),
            Part(lambda p: rounded_box(p @ rotation_z(0.5), (0.1 * M, 0.75 * M, 0.05 * M), (0.48 * M, 0.025 * M, 0.34 * M), 0.01 * M), LID),
            Part(lambda p: _union(*(cylinder(p, c, 0.05 * M, 0.2 * M, 0.02 * M, r @ rotation_x(np.pi / 2)) for c, r in sticks),
                                  *(cylinder(p, (x * M, 0.55 * M, 0.0), 0.05 * M, 0.12 * M, 0.02 * M) for x in (-0.2, -0.05, 0.1))), STICK),
            Part(lambda p: rounded_box(p, (0, 0.3 * M, 0.325 * M), (0.46 * M, 0.06 * M, 0.01 * M), 0.005 * M), BAND),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(64, 56), footprint=box_footprint(0.45 * M, 0.32 * M))


def danger_sign(stem: str, seed: int) -> PropModel:
    """Panneau « danger » rouillé sur son piquet, losange jaune, tordu par l'éboulement."""
    POST, PLATE, MARK, RUST_M = range(4)
    materials = [make_material("post", METAL), make_material("plate", MACHINE_YELLOW), make_material("mark", COAL, contrast=0.6),
                 make_material("rust", RUST_DEEP)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        bend = rotation_z(w.uniform(0.12, 0.25))
        plate_center = np.array([0.0, 1.55 * M, 0.06 * M])

        return [
            Part(lambda p: capsule(p @ bend, (0, 0, 0), (0, 1.5 * M, 0), 0.04 * M), POST),
            Part(lambda p: rounded_box(p @ bend, plate_center, (0.24 * M, 0.24 * M, 0.02 * M), 0.02 * M, rotation_z(np.pi / 4)), PLATE),
            Part(lambda p: capsule(p @ bend, plate_center + np.array([0.0, 0.12 * M, 0.03 * M]), plate_center + np.array([0.0, -0.02 * M, 0.03 * M]), 0.025 * M), MARK),
            Part(lambda p: _union(*(sphere(p @ bend, plate_center + np.array([x * M, y * M, 0.02 * M]), 0.05 * M) for x, y in ((0.12, -0.06), (-0.1, -0.08)))), RUST_M),
        ]

    # Plaque tournée vers la caméra : de trois-quarts sur l'axe X, elle se lisait par la tranche.
    return PropModel(stem, parts, materials, AXIS_Y_YAW, canvas=(44, 70))


def pickaxe_rock(stem: str, seed: int) -> PropModel:
    """Pioche plantée dans un bloc, lanterne éteinte posée à côté : le dernier coup avant de partir."""
    ROCK, HANDLE, HEAD, LAMP = range(4)
    materials = [make_material("rock", ROCK_GREY), make_material("handle", MINE_WOOD), make_material("head", METAL),
                 make_material("lamp", RUST_DEEP)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        blocks = _rocks(w, 3, (0.35, 0.3), (0.25, 0.4), 0.6)
        head = np.array([0.05 * M, 0.85 * M, 0.0])
        return [
            Part(_block_union(blocks), ROCK),
            Part(lambda p: capsule(p, head, head + np.array([0.55 * M, 0.75 * M, 0.1 * M]), 0.045 * M), HANDLE),
            Part(lambda p: capsule(p, head + np.array([-0.35 * M, -0.2 * M, 0.0]), head + np.array([0.35 * M, 0.15 * M, 0.0]), 0.05 * M, 0.02 * M), HEAD),
            Part(lambda p: _union(cylinder(p, (0.6 * M, 0.18 * M, 0.45 * M), 0.1 * M, 0.18 * M, 0.03 * M),
                                  np.abs(cylinder(p @ rotation_x(np.pi / 2), (0.6 * M, 0.45 * M, -0.38 * M), 0.08 * M, 0.01 * M)) - 0.01 * M), LAMP),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(64, 76), footprint=box_footprint(0.4 * M, 0.35 * M))


# ---------------------------------------------------------------------------------------------------------------------
# Chantier composé (plan 08, composition de la carrière) : galerie, voie droite, wagonnets, baraque
# ---------------------------------------------------------------------------------------------------------------------

def _site_model(stem: str, parts, materials, half_w: float, half_d: float, extent_x: float, extent_z: float, top: float,
                mirrored: bool) -> PropModel:
    """Cadrage des grands éléments du chantier, vus presque de face comme les bâtiments de ferme (lacet ±12°)."""
    width_px = int((extent_x * 2) * 0.62 * 1.05) + 24
    height_px = int(top * 0.62 * 0.87 + extent_z * 2 * 0.31) + 30
    return PropModel(stem, parts, materials, -BUILDING_YAW if mirrored else BUILDING_YAW,
                     ray_range=float(2.5 * max(extent_x, extent_z, top)), canvas=(width_px, height_px),
                     footprint=box_footprint(half_w, half_d),
                     bounds=((-extent_x, 0.0, -extent_z), (extent_x, top, extent_z)), supersample=3)


def gallery_entrance(stem: str, seed: int, mirrored: bool) -> PropModel:
    """Entrée de galerie : front de taille, bouche noire étayée de bois, lampe de sécurité, voie qui en sort."""
    DARK, GREY, LIGHT, VOID, TIMBER, PLANK, RAIL, SLEEPER, LAMP, EARTH = range(10)
    materials = [make_material("rock_dark", ROCK_DARK), make_material("rock_grey", ROCK_GREY), make_material("rock_light", ROCK_LIGHT),
                 make_material("void", COAL, contrast=0.4), make_material("timber", MINE_WOOD), make_material("plank", PLANK_LIGHT),
                 make_material("rail", METAL, contrast=0.7), make_material("sleeper", MINE_WOOD), make_emissive("safety", SAFETY_ORANGE),
                 make_material("red_earth", RED_EARTH)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        # Front de taille en gradins : une assise de gros blocs, des blocs plus petits posés dessus, en retrait.
        blocks = []
        for tier, (count, base, size, depth) in enumerate(((11, 0.0, (0.55, 0.9), (-0.7, 0.1)), (10, 1.5, (0.4, 0.75), (-1.0, -0.3)),
                                                           (7, 2.6, (0.35, 0.6), (-1.3, -0.6)))):
            reach = 3.5 - tier * 0.8
            for index in range(count):
                x = w.uniform(-reach, reach)
                half = np.array([w.uniform(*size), w.uniform(*size) * (1.1 if tier == 0 else 0.85), w.uniform(0.5, 0.8)]) * M
                center = np.array([x * M, base * M + half[1] * (0.9 - 0.25 * abs(x) / 3.5), w.uniform(*depth) * M])
                rotation = rotation_y(w.uniform(-0.5, 0.5)) @ rotation_z(w.uniform(-0.2, 0.2)) @ rotation_x(w.uniform(-0.15, 0.15))
                blocks.append((center, half, rotation))
        tone = [w.uniform(0, 1) for _ in blocks]
        groups = [[i for i in range(len(blocks)) if tone[i] < 0.4], [i for i in range(len(blocks)) if 0.4 <= tone[i] < 0.8],
                  [i for i in range(len(blocks)) if tone[i] >= 0.8]]
        mouth = lambda p: rounded_box(p, (0, 1.15 * M, 0.2 * M), (0.95 * M, 1.15 * M, 1.6 * M), 0.1 * M)
        rock = lambda members: (lambda p: np.maximum(_block_union([blocks[i] for i in members], 0.14 * M)(p), -mouth(p)))
        seam = [(blocks[i][0] + np.array([0.0, blocks[i][1][1] * 0.2, blocks[i][1][2] * 0.9]), blocks[i][1]) for i in groups[1][:3]]
        posts = [np.array([x * M, 0.0, 0.8 * M]) for x in (-1.05, 1.05)]
        planks = [np.array([x * M, 2.75 * M, 0.72 * M]) for x in (-0.7, 0.0, 0.65)]
        sleepers = [np.array([w.uniform(-0.04, 0.04) * M, 0.02 * M, z * M]) for z in np.arange(-0.6, 2.4, 0.55)]
        rubble = [((w.uniform(-3.2, 3.2) * M, 0.1 * M, w.uniform(0.3, 1.3) * M), w.uniform(0.1, 0.22) * M) for _ in range(10)]
        rubble = [(c, r) for c, r in rubble if abs(c[0]) > 1.3 * M]
        return [
            Part(rock(groups[0]), DARK),
            Part(rock(groups[1]), GREY),
            Part(rock(groups[2]), LIGHT),
            Part(lambda p: np.maximum(_union(*(ellipsoid(p, c, (h[0] * 0.85, h[1] * 0.12, 0.08 * M)) for c, h in seam)), -mouth(p)), EARTH),
            Part(lambda p: rounded_box(p, (0, 1.15 * M, -0.9 * M), (1.0 * M, 1.2 * M, 0.25 * M), 0.02 * M), VOID),
            Part(lambda p: _union(*(cylinder(p, post + np.array([0.0, 1.2 * M, 0.0]), 0.13 * M, 1.2 * M, 0.03 * M) for post in posts),
                                  rounded_box(p, (0, 2.45 * M, 0.8 * M), (1.3 * M, 0.15 * M, 0.17 * M), 0.03 * M),
                                  *(cylinder(p, post + np.array([0.0, 1.2 * M, -0.9 * M]), 0.11 * M, 1.2 * M, 0.03 * M) for post in posts)), TIMBER),
            Part(lambda p: _union(*(rounded_box(p, c, (0.32 * M, 0.2 * M, 0.03 * M), 0.01 * M, rotation_z(tilt))
                                    for c, tilt in zip(planks, (0.06, -0.04, 0.1)))), PLANK),
            Part(lambda p: _union(*(rounded_box(p, (x * M, 0.05 * M, 0.9 * M), (0.035 * M, 0.035 * M, 1.6 * M), 0.01 * M) for x in (-0.35, 0.35))), RAIL),
            Part(lambda p: _union(*(rounded_box(p, c, (0.55 * M, 0.025 * M, 0.1 * M), 0.01 * M) for c in sleepers)), SLEEPER),
            Part(lambda p: ellipsoid(p, (1.25 * M, 1.95 * M, 1.0 * M), (0.12 * M, 0.16 * M, 0.12 * M)), LAMP),
            Part(lambda p: _union(*(sphere(p, c, r) for c, r in rubble)), GREY),
        ]

    return _site_model(stem, parts, materials, 3.3 * M, 0.9 * M, 4.2 * M, 2.6 * M, 4.4 * M, mirrored)


def track(stem: str, seed: int) -> PropModel:
    """Voie étroite droite de 3,9 m, vue de profil : posée tous les 64 px, les tronçons se raccordent. Décalque au sol."""
    RAIL, SLEEPER, RUST_M = range(3)
    materials = [make_material("rail", METAL, contrast=0.7), make_material("sleeper", MINE_WOOD), make_material("rust", RUST_DEEP)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        sleepers = [(np.array([x * M, 0.02 * M, w.uniform(-0.05, 0.05) * M]), rotation_y(w.uniform(-0.1, 0.1))) for x in np.arange(-1.65, 1.8, 0.55)]
        rust = [np.array([w.uniform(-1.8, 1.8) * M, 0.06 * M, z * M]) for z in (-0.35, 0.35) for _ in range(2)]
        return [
            Part(lambda p: _union(*(rounded_box(p, (0, 0.06 * M, z * M), (1.95 * M, 0.035 * M, 0.035 * M), 0.01 * M) for z in (-0.35, 0.35))), RAIL),
            Part(lambda p: _union(*(rounded_box(p, c, (0.1 * M, 0.025 * M, 0.55 * M), 0.01 * M, r) for c, r in sleepers)), SLEEPER),
            Part(lambda p: _union(*(sphere(p, c, 0.07 * M) for c in rust)), RUST_M),
        ]

    return PropModel(stem, parts, materials, 0.0, canvas=(90, 40))


def ore_wagon(stem: str, seed: int, crystals: bool) -> PropModel:
    """Wagonnet resté debout sur la voie, chargé de minerai rouge ou de cristaux d'Essence qui luisent encore."""
    RUST_M, METAL_M, WHEEL, LOAD, GLOW = range(5)
    materials = [make_material("rust", RUST_DEEP), make_material("metal", METAL), make_material("wheel", COAL, contrast=0.7),
                 make_material("ore", RED_EARTH), make_emissive("crystal", CRYSTAL)]

    def parts() -> list[Part]:
        w = Weathering(seed)

        def tub(p: np.ndarray) -> np.ndarray:
            outer = rounded_box(p, (0, 0.72 * M, 0), (0.8 * M, 0.38 * M, 0.5 * M), 0.06 * M)
            inner = rounded_box(p, (0, 0.85 * M, 0), (0.72 * M, 0.38 * M, 0.42 * M), 0.04 * M)
            return np.maximum(outer, -inner)

        load = [((w.uniform(-0.6, 0.6) * M, w.uniform(1.0, 1.2) * M, w.uniform(-0.3, 0.3) * M), w.uniform(0.14, 0.24) * M) for _ in range(9)]
        anchors = [np.array([w.uniform(-0.4, 0.4) * M, 1.0 * M, w.uniform(-0.2, 0.2) * M]) for _ in range(2)]
        shards = _crystals(w, anchors, 6, (0.4, 0.75), 0.11 * M) if crystals else []
        patches = [np.array([w.uniform(-0.7, 0.7) * M, w.uniform(0.45, 1.0) * M, 0.5 * M]) for _ in range(3)]
        parts_list = [
            Part(tub, METAL_M),
            Part(lambda p: np.maximum(_union(*(sphere(p, c, 0.22 * M) for c in patches)), tub(p) - 0.03 * M), RUST_M),
            Part(lambda p: _union(*(cylinder(p, (x * M, 0.22 * M, z * M), 0.2 * M, 0.05 * M, 0.02 * M, rotation_x(np.pi / 2))
                                    for x in (-0.48, 0.48) for z in (-0.42, 0.42)),
                                  rounded_box(p, (0, 0.28 * M, 0), (0.75 * M, 0.05 * M, 0.3 * M), 0.02 * M)), WHEEL),
            Part(lambda p: _union(*(sphere(p, c, r) for c, r in load)), LOAD),
        ]
        if crystals:
            parts_list.append(Part(lambda p: _union(*(capsule(p, a, b, r, r * 0.2) for a, b, r in shards)), GLOW))
        return parts_list

    return PropModel(stem, parts, materials, 0.0, canvas=(70, 64), footprint=box_footprint(0.8 * M, 0.5 * M))


def site_hut(stem: str, seed: int, mirrored: bool) -> PropModel:
    """Baraque de chantier en tôle ondulée sur parpaings : porte entrouverte, vitre sale, gyrophare sur le toit."""
    WALL, ROOF, DARK, BLOCK, TRIM, LAMP, RUST_M = range(7)
    materials = [make_material("hut", HUT_WALL), make_material("roof", METAL), make_material("dark", COAL, contrast=0.5),
                 make_material("block", ROCK_GREY), make_material("trim", MACHINE_YELLOW), make_emissive("safety", SAFETY_ORANGE),
                 make_material("rust", RUST_DEEP)]
    k = 2 * np.pi / (0.16 * M)

    def parts() -> list[Part]:
        w = Weathering(seed)

        def walls(p: np.ndarray) -> np.ndarray:
            corrugation = 0.012 * M * np.sin(p[:, 0] * k)
            return rounded_box(p, (0, 1.45 * M, 0), (1.8 * M, 1.2 * M, 1.15 * M), 0.03 * M) + corrugation

        roof = lambda p: rounded_box(p, (0, 2.72 * M, 0), (1.98 * M, 0.07 * M, 1.32 * M), 0.02 * M, rotation_x(-0.07))
        streaks = [np.array([w.uniform(-1.6, 1.6) * M, w.uniform(0.6, 2.3) * M, 1.15 * M]) for _ in range(4)]
        return [
            Part(walls, WALL),
            Part(roof, ROOF),
            Part(lambda p: _union(rounded_box(p, (-0.95 * M, 1.2 * M, 1.14 * M), (0.42 * M, 0.95 * M, 0.04 * M), 0.01 * M),
                                  rounded_box(p, (0.75 * M, 1.6 * M, 1.14 * M), (0.5 * M, 0.32 * M, 0.04 * M), 0.01 * M)), DARK),
            Part(lambda p: _union(*(rounded_box(p, (x * M, 0.12 * M, z * M), (0.2 * M, 0.12 * M, 0.2 * M), 0.02 * M)
                                    for x in (-1.6, 1.6) for z in (-0.95, 0.95))), BLOCK),
            Part(lambda p: _union(rounded_box(p, (0, 2.5 * M, 1.17 * M), (1.8 * M, 0.06 * M, 0.02 * M), 0.01 * M),
                                  rounded_box(p, (0.75 * M, 1.6 * M, 1.18 * M), (0.03 * M, 0.32 * M, 0.02 * M), 0.005 * M)), TRIM),
            Part(lambda p: ellipsoid(p, (1.6 * M, 2.95 * M, 0.9 * M), (0.12 * M, 0.15 * M, 0.12 * M)), LAMP),
            Part(lambda p: np.maximum(_union(*(capsule(p, c, c - np.array([0.0, 0.6 * M, 0.0]), 0.1 * M) for c in streaks)),
                                      walls(p) - 0.02 * M), RUST_M),
        ]

    return _site_model(stem, parts, materials, 1.8 * M, 1.15 * M, 2.4 * M, 1.8 * M, 3.3 * M, mirrored)


# ---------------------------------------------------------------------------------------------------------------------
# Catalogue
# ---------------------------------------------------------------------------------------------------------------------

def catalog() -> list[PropModel]:
    return [
        rock_outcrop("prop_rock_outcrop", 601, 1.0),
        rock_outcrop("prop_rock_outcrop_v2", 602, 1.5),
        boulder("prop_boulder", 603),
        gravel_pile("prop_gravel_pile", 604, ROCK_LIGHT),
        gravel_pile("prop_ore_pile", 605, RED_EARTH),
        crystal_vein("prop_crystal_vein", 611),
        crystal_cluster("prop_crystal_cluster", 612),
        mine_cart("prop_mine_cart", 621),
        rails("prop_rails", 622),
        excavator_bucket("prop_excavator_bucket", 623),
        conveyor("prop_conveyor", 624),
        cable_drum("prop_cable_drum", 625),
        safety_lamp("prop_safety_lamp", 626),
        mine_supports("prop_mine_supports", 631),
        worker_locker("prop_worker_locker", 632),
        explosives_crate("prop_explosives_crate", 633),
        danger_sign("prop_danger_sign", 634),
        pickaxe_rock("prop_pickaxe_rock", 635),
        gallery_entrance("prop_quarry_gallery_a", 641, False),
        gallery_entrance("prop_quarry_gallery_b", 642, True),
        track("prop_quarry_track", 643),
        track("prop_quarry_track_v2", 644),
        ore_wagon("prop_quarry_wagon_ore", 645, False),
        ore_wagon("prop_quarry_wagon_crystal", 646, True),
        site_hut("prop_quarry_hut_a", 647, False),
        site_hut("prop_quarry_hut_b", 648, True),
    ]
