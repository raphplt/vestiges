"""
Décors de la Forêt Reconquise (plan 08, lot P3) : une nature lumineuse qui a dévoré la civilisation.

Échelle réelle du personnage (1 m ≈ 27,5 unités). Les arbres sont rendus en deux fichiers alignés sur le même point
au sol : le tronc (trié avec les entités) et la canopée (couche des canopées, transparente quand le joueur passe
dessous). Couleurs de la palette forêt de la charte (§3) ; le feuillage reste plus clair que le sous-bois pour se
détacher du sol, et les vestiges humains gardent leur gris et leur rouille sous le lierre.
"""
from __future__ import annotations

from typing import Callable

import numpy as np

from ..palette import make_material
from ..render import Part
from ..sdf import capsule, cylinder, ellipsoid, rotation_x, rotation_y, rotation_z, rounded_box, sphere
from ._kit import AXIS_X_YAW, AXIS_Y_YAW, M, PropModel, Weathering, box_footprint
from .urban import car

# Palette forêt (charte §3).
CANOPY_DARK = "#2D5A27"
MOSS = "#4A8C3F"
LEAF_LIGHT = "#7BC558"
LEAF_YELLOW = "#A4D65E"
TRUNK = "#4A3728"
EARTH = "#7A5C42"
WOOD_LIGHT = "#A68B6B"
OCHRE = "#C49B3E"
CONCRETE = "#7A7A70"
IVY = "#1E3A1A"
FLOWER_VIOLET = "#8B6BAE"
FLOWER_YELLOW = "#E0C84A"


def _union(*distances: np.ndarray) -> np.ndarray:
    result = distances[0]
    for d in distances[1:]:
        result = np.minimum(result, d)
    return result


def _leafy(distance: Callable[[np.ndarray], np.ndarray], amplitude: float, period: float):
    """Surface feuillue : bosses régulières qui cassent la boule lisse, sans coût de géométrie."""
    k = 2 * np.pi / period

    def field(p: np.ndarray) -> np.ndarray:
        bumps = np.sin(p[:, 0] * k) * np.sin(p[:, 1] * k * 1.3 + 1.1) * np.sin(p[:, 2] * k + 0.7)
        return distance(p) + amplitude * bumps

    return field


def _clumps(centers, radii, amplitude: float = 0.07 * M, period: float = 0.75 * M):
    return _leafy(lambda p: _union(*(ellipsoid(p, c, r) for c, r in zip(centers, radii))), amplitude, period)


# ---------------------------------------------------------------------------------------------------------------------
# Arbres (tronc + canopée)
# ---------------------------------------------------------------------------------------------------------------------

class _Tree:
    """Tirage d'un arbre, partagé par ses deux fichiers pour que tronc et canopée correspondent."""

    def __init__(self, seed: int, height: float, crown_radius: float, trunk_radius: float, clumps: int):
        w = Weathering(seed)
        self.lean = np.array([w.uniform(-0.25, 0.25) * M, 0.0, w.uniform(-0.2, 0.2) * M])
        self.trunk_top = np.array([0.0, height * 0.55, 0.0]) + self.lean
        self.trunk_radius = trunk_radius
        self.roots = [(np.cos(a) * trunk_radius * 2.4, 0.05 * M, np.sin(a) * trunk_radius * 2.4)
                      for a in np.linspace(0, 2 * np.pi, 4, endpoint=False) + w.uniform(0, 1.5)]
        crown_center = np.array([0.0, height * 0.72, 0.0]) + self.lean * 1.3
        self.branches = []
        self.crown_centers = []
        self.crown_radii = []
        for index in range(clumps):
            angle = index * 2.39996 + w.uniform(-0.3, 0.3)
            ring = np.sqrt((index + 0.5) / clumps)
            offset = np.array([np.cos(angle) * ring * crown_radius * 0.75,
                               (1.0 - ring) * crown_radius * 0.55 + w.uniform(-0.2, 0.2) * crown_radius,
                               np.sin(angle) * ring * crown_radius * 0.6])
            center = crown_center + offset
            size = crown_radius * w.uniform(0.3, 0.42)
            self.crown_centers.append(tuple(center))
            self.crown_radii.append((size, size * 0.78, size))
            if index < 4:
                self.branches.append((tuple(self.trunk_top), tuple(center - offset * 0.35)))
        # Trois étages de feuillage : dessous à l'ombre, cœur, touffes du haut en pleine lumière.
        order = np.argsort([c[1] - c[2] * 0.3 for c in self.crown_centers])
        self.lower = set(int(i) for i in order[:len(order) // 4])
        self.upper = set(int(i) for i in order[len(order) * 2 // 3:])


def _tree_base(stem: str, tree: _Tree, bark: str, moss: bool, canvas: tuple[int, int]) -> PropModel:
    BARK, ROOT, MOSS_M = range(3)
    materials = [make_material("bark", bark), make_material("root", TRUNK), make_material("moss", MOSS)]

    def parts() -> list[Part]:
        r = tree.trunk_radius
        result = [
            Part(lambda p: capsule(p, (0, 0, 0), tuple(tree.trunk_top), r, r * 0.62), BARK),
            Part(lambda p: _union(*(capsule(p, (0, 0.35 * M, 0), root, r * 0.55, r * 0.2) for root in tree.roots)), ROOT),
            Part(lambda p: _union(*(capsule(p, a, b, r * 0.45, r * 0.18) for a, b in tree.branches)), BARK),
        ]
        if moss:
            result.append(Part(lambda p: ellipsoid(p, (r * 0.6, 0.45 * M, r * 0.5), (r * 0.7, 0.35 * M, r * 0.7)), MOSS_M))
        return result

    r = tree.trunk_radius
    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=canvas, footprint=box_footprint(r * 1.3, r * 1.3))


def _tree_canopy(stem: str, tree: _Tree, leaf: str, leaf_top: str, canvas: tuple[int, int]) -> PropModel:
    SHADE, LEAF, LEAF_TOP = range(3)
    materials = [make_material("leaf_shade", CANOPY_DARK, contrast=0.9), make_material("leaf", leaf),
                 make_material("leaf_top", leaf_top, contrast=0.9)]

    def tier(indices) -> Callable[[np.ndarray], np.ndarray]:
        chosen = sorted(indices)
        return _clumps([tree.crown_centers[i] for i in chosen], [tree.crown_radii[i] for i in chosen], 0.09 * M, 0.6 * M)

    def parts() -> list[Part]:
        middle = set(range(len(tree.crown_centers))) - tree.lower - tree.upper
        return [Part(tier(tree.lower), SHADE), Part(tier(middle), LEAF), Part(tier(tree.upper), LEAF_TOP)]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=canvas, supersample=3)


def tree(stem: str, seed: int, height: float, crown: float, trunk: float, clumps: int, bark: str, leaf: str,
         leaf_top: str, moss: bool, canvas: tuple[int, int]) -> list[PropModel]:
    shape = _Tree(seed, height, crown, trunk, clumps)
    return [_tree_base(f"{stem}_base", shape, bark, moss, canvas),
            _tree_canopy(f"{stem}_canopy", shape, leaf, leaf_top, canvas)]


def strangled_tree(stem: str, seed: int) -> PropModel:
    """Arbre mort étranglé par le lierre : pas de canopée, silhouette haute et sombre."""
    WOOD, IVY_M, LEAF = range(3)
    materials = [make_material("dead_wood", "#6B6161"), make_material("ivy", IVY, contrast=0.8), make_material("leaf", MOSS)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        top = (w.uniform(-0.2, 0.2) * M, 4.2 * M, 0.0)
        limbs = [((0, 2.6 * M, 0), (w.uniform(0.7, 1.2) * M * s, w.uniform(3.4, 4.0) * M, w.uniform(-0.4, 0.4) * M))
                 for s in (-1, 1)]
        spiral = [(np.cos(t) * 0.3 * M, t / (2 * np.pi) * 1.3 * M, np.sin(t) * 0.3 * M) for t in np.linspace(0, 6 * np.pi, 14)]
        leaves = [(np.cos(t) * 0.38 * M, t / (2 * np.pi) * 1.3 * M + 0.1 * M, np.sin(t) * 0.38 * M)
                  for t in np.linspace(0.5, 6 * np.pi, 7)]
        return [
            Part(lambda p: capsule(p, (0, 0, 0), top, 0.3 * M, 0.08 * M), WOOD),
            Part(lambda p: _union(*(capsule(p, a, b, 0.13 * M, 0.04 * M) for a, b in limbs)), WOOD),
            Part(lambda p: _union(*(capsule(p, a, b, 0.09 * M) for a, b in zip(spiral, spiral[1:]))), IVY_M),
            Part(_clumps(leaves, [(0.2 * M, 0.14 * M, 0.2 * M)] * len(leaves), 0.03 * M, 0.2 * M), LEAF),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(80, 150), footprint=box_footprint(0.35 * M, 0.35 * M))


# ---------------------------------------------------------------------------------------------------------------------
# Sous-bois
# ---------------------------------------------------------------------------------------------------------------------

def bush(stem: str, seed: int, blossom: str | None) -> PropModel:
    LEAF, LEAF_TOP, FLOWER = range(3)
    materials = [make_material("leaf", MOSS), make_material("leaf_top", LEAF_LIGHT, contrast=0.9),
                 make_material("flower", blossom or FLOWER_YELLOW, contrast=0.6)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        low = [(w.uniform(-0.9, 0.9) * M, w.uniform(0.3, 0.6) * M, w.uniform(-0.5, 0.5) * M) for _ in range(8)]
        high = [(w.uniform(-0.6, 0.6) * M, w.uniform(0.8, 1.2) * M, w.uniform(-0.3, 0.3) * M) for _ in range(4)]
        low_radii = [(r, r * 0.8, r) for r in (w.uniform(0.3, 0.45) * M for _ in low)]
        high_radii = [(r, r * 0.8, r) for r in (w.uniform(0.25, 0.38) * M for _ in high)]
        result = [
            Part(_clumps(low, low_radii, 0.08 * M, 0.38 * M), LEAF),
            Part(_clumps(high, high_radii, 0.07 * M, 0.34 * M), LEAF_TOP),
        ]
        if blossom:
            dots = [(w.uniform(-0.8, 0.8) * M, w.uniform(0.7, 1.4) * M, w.uniform(0.2, 0.6) * M) for _ in range(8)]
            result.append(Part(lambda p: _union(*(sphere(p, d, 0.11 * M) for d in dots)), FLOWER))
        return result

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(84, 72))


def fern(stem: str, seed: int) -> PropModel:
    LEAF, LEAF_TOP = range(2)
    materials = [make_material("frond", CANOPY_DARK, contrast=1.1), make_material("frond_light", LEAF_YELLOW, contrast=0.8)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        fronds = []
        for index in range(10):
            angle = index * 2 * np.pi / 10 + w.uniform(-0.2, 0.2)
            rise = w.uniform(0.15, 0.4)
            # Fronde : ellipsoïde allongé, incliné vers l'extérieur.
            rotation = rotation_y(angle) @ rotation_x(rise)
            center = (np.sin(angle) * 0.45 * M, 0.22 * M, np.cos(angle) * 0.45 * M)
            fronds.append((center, rotation, index % 3 == 0))
        return [
            Part(lambda p: _union(*(ellipsoid(p, c, (0.15 * M, 0.04 * M, 0.5 * M), r) for c, r, top in fronds if not top)), LEAF),
            Part(lambda p: _union(*(ellipsoid(p, c, (0.15 * M, 0.04 * M, 0.5 * M), r) for c, r, top in fronds if top)), LEAF_TOP),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(80, 60))


def flowers(stem: str, seed: int, petals: str) -> PropModel:
    STEM, PETAL = range(2)
    materials = [make_material("stem", MOSS), make_material("petal", petals, contrast=0.6)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        heads = [(w.uniform(-0.7, 0.7) * M, w.uniform(0.25, 0.5) * M, w.uniform(-0.45, 0.45) * M) for _ in range(11)]
        return [
            Part(lambda p: _union(*(capsule(p, (h[0], 0, h[2]), h, 0.04 * M) for h in heads)), STEM),
            Part(lambda p: _union(*(ellipsoid(p, h, (0.13 * M, 0.07 * M, 0.13 * M)) for h in heads)), PETAL),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(64, 44))


def mushrooms(stem: str, seed: int) -> PropModel:
    STALK, CAP = range(2)
    materials = [make_material("stalk", "#E8E0D4", contrast=0.6), make_material("cap", OCHRE)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        caps = [(w.uniform(-0.45, 0.45) * M, w.uniform(0.2, 0.45) * M, w.uniform(-0.3, 0.3) * M, w.uniform(0.14, 0.24) * M)
                for _ in range(5)]
        return [
            Part(lambda p: _union(*(capsule(p, (x, 0, z), (x, y, z), r * 0.35) for x, y, z, r in caps)), STALK),
            Part(lambda p: _union(*(ellipsoid(p, (x, y, z), (r, r * 0.55, r)) for x, y, z, r in caps)), CAP),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(52, 44))


def stump(stem: str, seed: int) -> PropModel:
    BARK, CUT, MOSS_M, SHROOM = range(4)
    materials = [make_material("bark", TRUNK), make_material("cut", WOOD_LIGHT, contrast=0.7), make_material("moss", MOSS),
                 make_material("shroom", OCHRE)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        roots = [(np.cos(a) * 0.7 * M, 0.04 * M, np.sin(a) * 0.7 * M) for a in np.linspace(0, 2 * np.pi, 5, endpoint=False) + w.uniform(0, 1)]
        return [
            Part(lambda p: cylinder(p, (0, 0.28 * M, 0), 0.42 * M, 0.28 * M, 0.06 * M), BARK),
            Part(lambda p: cylinder(p, (0, 0.57 * M, 0), 0.36 * M, 0.02 * M, 0.01 * M), CUT),
            Part(lambda p: _union(*(capsule(p, (0, 0.2 * M, 0), r, 0.16 * M, 0.06 * M) for r in roots)), BARK),
            Part(lambda p: ellipsoid(p, (0.3 * M, 0.35 * M, 0.2 * M), (0.2 * M, 0.2 * M, 0.2 * M)), MOSS_M),
            Part(lambda p: ellipsoid(p, (-0.38 * M, 0.3 * M, 0.2 * M), (0.12 * M, 0.05 * M, 0.1 * M)), SHROOM),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(56, 48), footprint=box_footprint(0.42 * M, 0.42 * M))


def mossy_rock(stem: str, seed: int, scale: float) -> PropModel:
    STONE, MOSS_M = range(2)
    materials = [make_material("stone", CONCRETE), make_material("moss", MOSS)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        blocks = [((w.uniform(-0.3, 0.3) * M * scale, w.uniform(0.3, 0.45) * M * scale, w.uniform(-0.2, 0.2) * M * scale),
                   (w.uniform(0.45, 0.65) * M * scale, w.uniform(0.35, 0.5) * M * scale, w.uniform(0.4, 0.55) * M * scale),
                   rotation_y(w.uniform(0, np.pi)))
                  for _ in range(3)]
        moss = [(c[0], c[1] + h[1] * 0.8, c[2]) for c, h, _ in blocks]
        return [
            Part(lambda p: _union(*(rounded_box(p, c, h, 0.15 * M * scale, r) for c, h, r in blocks)), STONE),
            Part(_clumps(moss, [(0.5 * M * scale, 0.2 * M * scale, 0.45 * M * scale)] * 3, 0.04 * M, 0.3 * M), MOSS_M),
        ]

    half = 0.75 * M * scale
    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(int(70 * scale) + 16, int(56 * scale) + 16),
                     footprint=box_footprint(half, half * 0.8))


def fallen_log(stem: str, seed: int, yaw: float) -> PropModel:
    BARK, CUT, MOSS_M, SHROOM = range(4)
    materials = [make_material("bark", TRUNK), make_material("cut", WOOD_LIGHT, contrast=0.7), make_material("moss", MOSS),
                 make_material("shroom", OCHRE)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        moss = [(w.uniform(-0.1, 0.1) * M, 0.62 * M, z * M) for z in (-0.9, 0.1, 0.8)]
        shrooms = [(0.36 * M, 0.3 * M, z * M) for z in (-0.4, -0.2)]
        return [
            Part(lambda p: capsule(p, (0, 0.35 * M, -1.5 * M), (0, 0.33 * M, 1.4 * M), 0.35 * M, 0.3 * M), BARK),
            Part(lambda p: cylinder(p, (0, 0.33 * M, 1.62 * M), 0.28 * M, 0.02 * M, 0.01 * M, rotation_x(np.pi / 2)), CUT),
            Part(_clumps(moss, [(0.28 * M, 0.1 * M, 0.35 * M)] * 3, 0.03 * M, 0.2 * M), MOSS_M),
            Part(lambda p: _union(*(ellipsoid(p, c, (0.1 * M, 0.04 * M, 0.08 * M)) for c in shrooms)), SHROOM),
        ]

    return PropModel(stem, parts, materials, yaw, canvas=(100, 64), footprint=box_footprint(0.36 * M, 1.55 * M))


# ---------------------------------------------------------------------------------------------------------------------
# Vestiges envahis
# ---------------------------------------------------------------------------------------------------------------------

def ruin_wall(stem: str, seed: int) -> PropModel:
    BRICK, MORTAR, IVY_M, LEAF = range(4)
    materials = [make_material("brick", "#8A5A42"), make_material("mortar", "#A0A0A0", contrast=0.6),
                 make_material("ivy", IVY, contrast=0.8), make_material("leaf", MOSS)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        # Mur en redans : le sommet s'est effondré brique par brique.
        steps = [(x * M, w.uniform(0.8, 2.1) * M) for x in np.linspace(-1.3, 1.3, 6)]
        ivy = [(w.uniform(-1.2, 1.2) * M, w.uniform(0.2, 1.6) * M, 0.22 * M) for _ in range(9)]
        return [
            Part(lambda p: _union(*(rounded_box(p, (x, h / 2, 0), (0.24 * M, h / 2, 0.2 * M), 0.03 * M) for x, h in steps)), BRICK),
            Part(lambda p: rounded_box(p, (0, 0.08 * M, 0), (1.6 * M, 0.08 * M, 0.26 * M), 0.03 * M), MORTAR),
            Part(_clumps(ivy, [(0.3 * M, 0.26 * M, 0.08 * M)] * len(ivy), 0.04 * M, 0.2 * M), IVY_M),
            Part(_clumps(ivy[:4], [(0.2 * M, 0.16 * M, 0.1 * M)] * 4, 0.03 * M, 0.2 * M), LEAF),
        ]

    return PropModel(stem, parts, materials, -AXIS_X_YAW, canvas=(100, 96), footprint=box_footprint(1.55 * M, 0.26 * M))


def ivy_lamppost(stem: str, seed: int) -> PropModel:
    METAL, RUST, IVY_M, GLASS = range(4)
    materials = [make_material("metal", "#5A5A5A"), make_material("rust", "#6A4430"), make_material("ivy", IVY, contrast=0.8),
                 make_material("glass", "#D8D0A8", contrast=0.4)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        bend = rotation_z(w.uniform(0.08, 0.16))
        head = bend @ np.array([0.45 * M, 3.55 * M, 0.0])
        spiral = [(np.cos(t) * 0.12 * M, t / (2 * np.pi) * 0.9 * M, np.sin(t) * 0.12 * M) for t in np.linspace(0, 5 * np.pi, 12)]
        return [
            Part(lambda p: capsule(p @ bend, (0, 0, 0), (0, 3.5 * M, 0), 0.09 * M, 0.06 * M), METAL),
            Part(lambda p: capsule(p @ bend, (0, 3.5 * M, 0), (0.45 * M, 3.55 * M, 0), 0.05 * M), METAL),
            Part(lambda p: ellipsoid(p, tuple(head), (0.2 * M, 0.1 * M, 0.14 * M)), GLASS),
            Part(lambda p: cylinder(p, (0, 0.12 * M, 0), 0.2 * M, 0.12 * M, 0.03 * M), RUST),
            Part(lambda p: _union(*(capsule(p, a, b, 0.07 * M) for a, b in zip(spiral, spiral[1:]))), IVY_M),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(56, 120), footprint=box_footprint(0.2 * M, 0.2 * M))


def rusty_sign(stem: str, seed: int) -> PropModel:
    POST, PLATE, RUST, MOSS_M = range(4)
    materials = [make_material("post", "#6B6161"), make_material("plate", "#C4A830"), make_material("rust", "#6A4430"),
                 make_material("moss", MOSS)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        tilt = rotation_z(w.uniform(-0.12, 0.12))
        return [
            Part(lambda p: capsule(p @ tilt, (0, 0, 0), (0, 2.1 * M, 0), 0.05 * M), POST),
            Part(lambda p: rounded_box(p @ tilt, (0, 1.85 * M, 0.08 * M), (0.42 * M, 0.42 * M, 0.03 * M), 0.02 * M,
                                       rotation_z(np.pi / 4)), PLATE),
            Part(lambda p: _union(*(sphere(p @ tilt, (x * M, y * M, 0.12 * M), 0.1 * M)
                                    for x, y in ((0.18, 1.75), (-0.2, 2.0), (0.05, 1.55)))), RUST),
            Part(lambda p: ellipsoid(p, (0, 0.12 * M, 0), (0.3 * M, 0.14 * M, 0.3 * M)), MOSS_M),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(48, 96))


def overgrown_car(stem: str, seed: int, yaw: float) -> PropModel:
    """Voiture de la ville, reprise par la forêt : mousse épaisse, un jeune arbre qui perce le toit."""
    base = car(stem, "teal", yaw, seed)
    offset = len(base.materials)
    materials = list(base.materials) + [make_material("moss", MOSS), make_material("leaf", LEAF_LIGHT, contrast=0.9),
                                        make_material("sapling", TRUNK)]
    MOSS_M, LEAF, SAPLING = offset, offset + 1, offset + 2

    def parts() -> list[Part]:
        w = Weathering(seed + 500)
        moss = [(w.uniform(-0.6, 0.6) * M, w.uniform(0.9, 1.45) * M, w.uniform(-1.6, 1.6) * M) for _ in range(6)]
        crown = [(w.uniform(-0.4, 0.4) * M, w.uniform(2.3, 2.8) * M, w.uniform(-0.6, 0.1) * M) for _ in range(4)]
        return base.parts() + [
            Part(_clumps(moss, [(0.34 * M, 0.12 * M, 0.34 * M)] * len(moss), 0.04 * M, 0.25 * M), MOSS_M),
            Part(lambda p: capsule(p, (0, 0.6 * M, -0.3 * M), (0.1 * M, 2.3 * M, -0.25 * M), 0.07 * M, 0.04 * M), SAPLING),
            Part(_clumps(crown, [(0.42 * M, 0.34 * M, 0.4 * M)] * len(crown), 0.06 * M, 0.3 * M), LEAF),
        ]

    return PropModel(stem, parts, materials, yaw, canvas=(110, 120), footprint=base.footprint)


# ---------------------------------------------------------------------------------------------------------------------
# Catalogue
# ---------------------------------------------------------------------------------------------------------------------

def catalog() -> list[PropModel]:
    models: list[PropModel] = []
    # Chêne : large couronne, tronc épais ; deux tirages.
    models += tree("prop_tree_large", 301, 7.0 * M, 2.7 * M, 0.34 * M, 24, TRUNK, MOSS, LEAF_LIGHT, True, (150, 190))
    models += tree("prop_tree_large_v2", 302, 6.6 * M, 2.5 * M, 0.32 * M, 22, TRUNK, CANOPY_DARK, MOSS, True, (150, 190))
    # Jeune arbre : couronne ronde et claire.
    models += tree("prop_tree_medium", 311, 5.0 * M, 1.8 * M, 0.2 * M, 16, EARTH, MOSS, LEAF_YELLOW, False, (110, 150))
    # Bouleau : tronc pâle, feuillage jaune-vert.
    models += tree("prop_tree_birch", 321, 5.4 * M, 1.6 * M, 0.16 * M, 14, "#D8D0C0", LEAF_LIGHT, LEAF_YELLOW, False, (100, 160))
    models += [
        strangled_tree("prop_tree_strangled", 331),
        bush("prop_bush", 341, None),
        bush("prop_bush_v2", 342, FLOWER_VIOLET),
        fern("prop_fern", 351),
        flowers("prop_flowers", 361, FLOWER_YELLOW),
        flowers("prop_flowers_v2", 362, FLOWER_VIOLET),
        mushrooms("prop_mushrooms", 371),
        stump("prop_stump", 381),
        mossy_rock("prop_rock_mossy", 391, 1.0),
        mossy_rock("prop_rock_mossy_v2", 392, 1.4),
        fallen_log("prop_fallen_log", 401, AXIS_X_YAW),
        ruin_wall("prop_ruin_wall", 411),
        ivy_lamppost("prop_lamppost", 421),
        rusty_sign("prop_sign_rusty", 431),
        overgrown_car("prop_car_overgrown", 441, AXIS_Y_YAW),
    ]
    return models
