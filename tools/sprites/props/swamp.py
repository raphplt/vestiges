"""
Décors des Marécages (plan 08, lot P5) : une réalité fragile, eau laiteuse, bois blanchi et lueurs de spores.

Échelle réelle du personnage (1 m ≈ 27,5 unités), mêmes noms de fichiers que les anciens décors (mêmes uid).
Palette « Marécages » de la charte (§3) : bois blanchi et racines sombres pour les arbres morts, mousse humide et
lichen jaune-vert, violet profond pour ce qui est vénéneux, spore verte pour les rares lueurs. Ce qui est « noyé »
est coupé à un niveau d'eau sous le point au sol : l'objet s'enfonce dans la tuile d'eau au lieu d'y être posé.
Le placement reste celui de `SwampPropPlacer` (zones d'eau, lisières, bosquets morts, poches fongiques).
"""
from __future__ import annotations

from typing import Callable

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, cylinder, ellipsoid, rotation_x, rotation_y, rotation_z, rounded_box, sphere
from ._kit import AXIS_X_YAW, AXIS_Y_YAW, M, PropModel, Weathering, box_footprint
from .forest import _clumps, _union

# Palette Marécages (charte §3).
WATER_LIGHT = "#8AA89A"
WATER_DARK = "#4A6A5E"
WATER_MILK = "#B8C8BE"
MOSS_WET = "#3A6A38"
PEAT = "#3A2E22"
BLEACHED = "#9A8E7A"
LICHEN = "#8A9A4A"
SPORE = "#6ACA5A"
MIST = "#7A8A9A"
VIOLET = "#4A2A5A"
ROOT = "#2A1E16"
SILVER = "#C4D0D4"
# Hors palette de biome, repris de la palette maîtresse : rouille, fer et pierre des vestiges humains.
RUST = "#6A4430"
RUST_ORANGE = "#A85C30"
IRON = "#4A4648"
STONE = "#7A7A70"
BONE = "#D8CFB8"
WOOD_OLD = "#6E5A44"


def _sunk(distance: Callable[[np.ndarray], np.ndarray], level: float) -> Callable[[np.ndarray], np.ndarray]:
    """Garde ce qui dépasse de l'eau : le reste est sous la surface de la tuile."""
    return lambda p: np.maximum(distance(p), level - p[:, 1])


def _branch_chain(points: list[np.ndarray], r0: float, r1: float):
    """Branche torse : capsules enchaînées dont le rayon décroît de r0 à r1."""
    radii = np.linspace(r0, r1, len(points))
    return lambda p: _union(*(capsule(p, a, b, ra, rb) for a, b, ra, rb in zip(points, points[1:], radii, radii[1:])))


def _gnarled(w: Weathering, start: np.ndarray, direction: np.ndarray, length: float, segments: int,
             wobble: float = 0.35) -> list[np.ndarray]:
    """Points d'une branche qui se tord un peu à chaque segment ; un fût se tord moins qu'une branche."""
    points = [start]
    heading = direction / np.linalg.norm(direction)
    step = length / segments
    for _ in range(segments):
        heading = heading + np.array([w.uniform(-wobble, wobble), w.uniform(-0.1, 0.25) * wobble / 0.35, w.uniform(-wobble, wobble)])
        heading /= np.linalg.norm(heading)
        points.append(points[-1] + heading * step)
    return points


# ---------------------------------------------------------------------------------------------------------------------
# Arbres morts
# ---------------------------------------------------------------------------------------------------------------------

class _DeadTree:
    """Arbre mort à branches nues, partagé par le tronc et la « canopée » (branches hautes et mousse pendante)."""

    def __init__(self, seed: int, height: float, trunk_radius: float, limbs: int):
        w = Weathering(seed)
        self.radius = trunk_radius
        lean = np.array([w.uniform(-0.3, 0.3) * M, 0.0, w.uniform(-0.2, 0.2) * M])
        self.trunk = _gnarled(w, np.zeros(3), np.array([0.0, 1.0, 0.0]) + lean / M * 0.3, height * 0.62, 4, wobble=0.1)
        self.roots = [np.array([np.cos(a) * trunk_radius * 2.8, 0.02 * M, np.sin(a) * trunk_radius * 2.8])
                      for a in np.linspace(0, 2 * np.pi, 5, endpoint=False) + w.uniform(0, 1.2)]
        self.low_limbs: list[list[np.ndarray]] = []
        self.high_limbs: list[list[np.ndarray]] = []
        self.twigs: list[list[np.ndarray]] = []
        for index in range(limbs):
            angle = index * 2.39996 + w.uniform(-0.3, 0.3)
            along = 0.45 + 0.55 * (index + 1) / limbs
            anchor = self.trunk[min(len(self.trunk) - 1, int(along * (len(self.trunk) - 1)))]
            direction = np.array([np.cos(angle), w.uniform(0.2, 0.65), np.sin(angle) * 0.8])
            limb = _gnarled(w, anchor, direction, w.uniform(1.5, 2.3) * M * height / (5.5 * M), 3, wobble=0.25)
            (self.high_limbs if along > 0.7 else self.low_limbs).append(limb)
            # Rameaux nus au bout et au milieu de chaque branche : la silhouette hérissée de l'arbre mort.
            for start in (limb[-1], limb[-1], limb[2], limb[1]):
                twig_dir = np.array([w.uniform(-1, 1), w.uniform(0.1, 0.9), w.uniform(-1, 1)])
                self.twigs.append(_gnarled(w, start, twig_dir, w.uniform(0.35, 0.7) * M, 2))
        # Mousse pendante (barbe de vieillard) sous les branches hautes : des touffes de trois brins inégaux.
        self.moss = []
        for limb in self.high_limbs + self.low_limbs[-1:]:
            for point in limb[1:]:
                for dx in (-0.07, 0.0, 0.07):
                    top = point + np.array([dx * M, -0.04 * M, w.uniform(-0.05, 0.05) * M])
                    self.moss.append((top, top + np.array([w.uniform(-0.06, 0.06) * M, -w.uniform(0.3, 0.9) * M, 0.0])))


def dead_tree_large(stem: str, seed: int) -> list[PropModel]:
    shape = _DeadTree(seed, 6.2 * M, 0.34 * M, 6)
    WOOD, ROOT_M, LICHEN_M = range(3)
    base_materials = [make_material("bleached", BLEACHED), make_material("root", ROOT, contrast=0.8),
                      make_material("lichen", LICHEN, contrast=0.8)]
    r = shape.radius

    def base_parts() -> list[Part]:
        w = Weathering(seed + 1)
        patches = [shape.trunk[i] + np.array([w.uniform(-1, 1) * r * 0.7, 0.0, r * 0.75]) for i in (1, 2)]
        return [
            Part(_branch_chain(shape.trunk, r, r * 0.55), WOOD),
            Part(lambda p: _union(*(capsule(p, (0, 0.4 * M, 0), root, r * 0.6, r * 0.18) for root in shape.roots)), ROOT_M),
            Part(lambda p: _union(*(_branch_chain(limb, r * 0.42, r * 0.18)(p) for limb in shape.low_limbs)), WOOD),
            Part(lambda p: _union(*(ellipsoid(p, c, (r * 0.6, r * 0.9, r * 0.35)) for c in patches)), LICHEN_M),
        ]

    MOSS_M, WOOD_HIGH, TWIG = range(3)
    canopy_materials = [make_material("hanging_moss", LICHEN, contrast=0.8), make_material("bleached", BLEACHED),
                        make_material("twig", ROOT, contrast=0.8)]

    def canopy_parts() -> list[Part]:
        return [
            Part(lambda p: _union(*(_branch_chain(limb, r * 0.38, r * 0.16)(p) for limb in shape.high_limbs)), WOOD_HIGH),
            Part(lambda p: _union(*(_branch_chain(twig, r * 0.16, r * 0.08)(p) for twig in shape.twigs)), TWIG),
            Part(lambda p: _union(*(capsule(p, a, b, 0.06 * M, 0.02 * M) for a, b in shape.moss)), MOSS_M),
        ]

    canvas = (170, 210)
    return [PropModel(f"{stem}_base", base_parts, base_materials, AXIS_X_YAW, canvas=canvas,
                      footprint=box_footprint(r * 1.4, r * 1.4)),
            PropModel(f"{stem}_canopy", canopy_parts, canopy_materials, AXIS_X_YAW, canvas=canvas, supersample=3)]


def dead_tree_mossy(stem: str, seed: int) -> PropModel:
    """Grand arbre mort sans canopée séparée : cime brisée, tronc gainé de mousse, barbes pendantes basses."""
    shape = _DeadTree(seed, 5.4 * M, 0.38 * M, 4)
    WOOD, MOSS_M, HANG, ROOT_M = range(4)
    materials = [make_material("bleached", BLEACHED), make_material("moss", MOSS_WET), make_material("hanging_moss", LICHEN, contrast=0.8),
                 make_material("root", ROOT, contrast=0.8)]
    r = shape.radius

    def parts() -> list[Part]:
        w = Weathering(seed + 2)
        sheath = [shape.trunk[0] + (shape.trunk[-1] - shape.trunk[0]) * t + np.array([w.uniform(-0.5, 0.5) * r, 0.0, r * 0.6])
                  for t in np.linspace(0.05, 0.8, 7)]
        return [
            Part(_branch_chain(shape.trunk, r, r * 0.7), WOOD),
            Part(lambda p: _union(*(_branch_chain(limb, r * 0.4, r * 0.16)(p) for limb in shape.low_limbs + shape.high_limbs)), WOOD),
            Part(_clumps(sheath, [(r * 0.75, r * 1.1, r * 0.55)] * len(sheath), 0.03 * M, 0.25 * M), MOSS_M),
            Part(lambda p: _union(*(capsule(p, a, b, 0.06 * M, 0.02 * M) for a, b in shape.moss)), HANG),
            Part(lambda p: _union(*(_branch_chain(twig, r * 0.14, r * 0.07)(p) for twig in shape.twigs)), WOOD),
            Part(lambda p: _union(*(capsule(p, (0, 0.4 * M, 0), root, r * 0.55, r * 0.16) for root in shape.roots)), ROOT_M),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(160, 190), footprint=box_footprint(r * 1.4, r * 1.4))


def dead_tree_small(stem: str, seed: int) -> PropModel:
    """Chicot : fût brisé net, deux moignons de branches, pied dans la tourbe."""
    WOOD, CUT, PEAT_M = range(3)
    materials = [make_material("bleached", BLEACHED), make_material("splinter", SILVER, contrast=0.6), make_material("peat", PEAT, contrast=0.7)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        trunk = _gnarled(w, np.zeros(3), np.array([0.1, 1.0, 0.0]), 2.6 * M, 3, wobble=0.15)
        stubs = [_gnarled(w, trunk[i], np.array([s, 0.6, w.uniform(-0.4, 0.4)]), w.uniform(0.4, 0.7) * M, 2) for i, s in ((1, -1), (2, 1))]
        top = trunk[-1]
        return [
            Part(_branch_chain(trunk, 0.2 * M, 0.13 * M), WOOD),
            Part(lambda p: _union(*(_branch_chain(s, 0.08 * M, 0.04 * M)(p) for s in stubs)), WOOD),
            Part(lambda p: _union(*(capsule(p, top, top + np.array([dx, 0.22 * M, dz]), 0.05 * M, 0.01 * M)
                                    for dx, dz in ((0.05 * M, 0.0), (-0.06 * M, 0.04 * M), (0.0, -0.06 * M)))), CUT),
            Part(lambda p: ellipsoid(p, (0, 0.02 * M, 0), (0.45 * M, 0.1 * M, 0.4 * M)), PEAT_M),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(70, 110), footprint=box_footprint(0.22 * M, 0.22 * M))


def aerial_roots(stem: str, seed: int, twist: float) -> PropModel:
    """Palétuvier : un fût court perché sur des racines en arceaux qui plongent dans la vase."""
    ROOT_M, WOOD, MOSS_M, LEAF = range(4)
    materials = [make_material("root", ROOT, contrast=0.9), make_material("bark", WOOD_OLD), make_material("moss", MOSS_WET),
                 make_material("leaf", LICHEN, contrast=0.9)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        hub = np.array([0.0, 1.5 * M, 0.0])
        arches = []
        for index in range(7):
            angle = index * 2 * np.pi / 7 + w.uniform(-0.25, 0.25) + twist * 0.3
            reach = w.uniform(0.9, 1.35) * M
            foot = np.array([np.cos(angle) * reach, -0.05 * M, np.sin(angle) * reach * 0.8])
            knee = hub * 0.55 + foot * 0.75 + np.array([np.sin(angle) * twist, 0.55 * M, -np.cos(angle) * twist]) * 0.4
            arches.append([hub + foot * 0.12, knee, foot])
        crown = [hub + np.array([w.uniform(-0.8, 0.8) * M, w.uniform(1.5, 2.3) * M, w.uniform(-0.5, 0.5) * M]) for _ in range(6)]
        return [
            Part(lambda p: _union(*(_branch_chain(a, 0.13 * M, 0.08 * M)(p) for a in arches)), ROOT_M),
            Part(lambda p: capsule(p, hub, hub + np.array([0.1 * M, 1.5 * M, 0.0]), 0.3 * M, 0.2 * M), WOOD),
            Part(lambda p: ellipsoid(p, hub + np.array([0.0, 0.25 * M, 0.2 * M]), (0.3 * M, 0.25 * M, 0.15 * M)), MOSS_M),
            Part(_clumps(crown, [(0.45 * M, 0.3 * M, 0.4 * M)] * len(crown), 0.07 * M, 0.35 * M), LEAF),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(120, 150), footprint=box_footprint(0.7 * M, 0.6 * M))


def root_mass(stem: str, seed: int) -> PropModel:
    """Souche renversée : galette de racines dressée, terre encore accrochée, radicelles pendantes."""
    ROOT_M, PEAT_M, HANG, WOOD = range(4)
    materials = [make_material("root", WOOD_OLD, contrast=0.9), make_material("peat", PEAT, contrast=0.8),
                 make_material("rootlet", BLEACHED, contrast=0.7), make_material("bleached", BLEACHED)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        plate_center = np.array([0.0, 1.4 * M, 0.0])
        spokes = []
        for index in range(11):
            angle = index * 2 * np.pi / 11 + w.uniform(-0.15, 0.15)
            tip = plate_center + np.array([np.cos(angle) * w.uniform(1.4, 1.9) * M, np.sin(angle) * w.uniform(1.2, 1.5) * M, w.uniform(0.0, 0.5) * M])
            start = plate_center + np.array([0.0, 0.0, 0.15 * M])
            spokes.append(_gnarled(w, start, tip - start, np.linalg.norm(tip - start), 3))
        rootlets = [(s[-2], s[-2] + np.array([0.0, -w.uniform(0.3, 0.7) * M, 0.05 * M])) for s in spokes if s[-2][1] > 0.8 * M]
        trunk_end = plate_center + np.array([0.0, -0.5 * M, -1.4 * M])
        return [
            Part(lambda p: _union(*(_branch_chain(s, 0.16 * M, 0.05 * M)(p) for s in spokes)), ROOT_M),
            Part(lambda p: ellipsoid(p, plate_center - np.array([0.0, 0.0, 0.2 * M]), (0.8 * M, 0.75 * M, 0.25 * M)), PEAT_M),
            Part(lambda p: _union(*(capsule(p, a, b, 0.04 * M, 0.015 * M) for a, b in rootlets)), HANG),
            Part(lambda p: capsule(p, plate_center + np.array([0.0, 0.0, -0.3 * M]), trunk_end, 0.45 * M, 0.38 * M), WOOD),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(130, 130), footprint=box_footprint(1.2 * M, 0.5 * M))


def bound_tree(stem: str, seed: int) -> PropModel:
    """Arbre lié : un tronc mort ceint de chaînes rouillées et de lambeaux de tissu, comme pour le retenir."""
    WOOD, CHAIN, CLOTH, ROOT_M = range(4)
    materials = [make_material("bleached", BLEACHED), make_material("chain", RUST_ORANGE, contrast=0.8),
                 make_material("cloth", SILVER, contrast=0.6), make_material("root", ROOT, contrast=0.8)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        trunk = _gnarled(w, np.zeros(3), np.array([0.0, 1.0, 0.0]), 3.6 * M, 4, wobble=0.1)
        limbs = [_gnarled(w, trunk[3], np.array([s, 0.8, 0.2]), 1.0 * M, 3) for s in (-1, 1)]
        rings = []
        for height in (0.9, 1.5, 2.1):
            center = np.array([0.0, height * M, 0.0]) + (trunk[2] - trunk[0]) * (height / 3.6) * 0.3
            tilt = rotation_z(w.uniform(-0.25, 0.25)) @ rotation_x(w.uniform(-0.15, 0.15))
            rings.append((center, tilt))
        strips = [(np.array([w.uniform(-0.2, 0.2) * M, h * M, 0.3 * M]), w.uniform(0.35, 0.6) * M) for h in (1.5, 2.1, 1.0)]
        roots = [np.array([np.cos(a) * 0.8 * M, 0.02 * M, np.sin(a) * 0.8 * M]) for a in np.linspace(0, 2 * np.pi, 4, endpoint=False) + 0.4]
        return [
            Part(_branch_chain(trunk, 0.3 * M, 0.16 * M), WOOD),
            Part(lambda p: _union(*(_branch_chain(l, 0.12 * M, 0.05 * M)(p) for l in limbs)), WOOD),
            Part(lambda p: _union(*(np.abs(cylinder(p, c, 0.34 * M, 0.04 * M, 0.02 * M, t)) - 0.03 * M for c, t in rings)), CHAIN),
            Part(lambda p: _union(*(rounded_box(p, c - np.array([0.0, l / 2, 0.0]), (0.06 * M, l / 2, 0.015 * M), 0.01 * M, rotation_z(0.1))
                                    for c, l in strips)), CLOTH),
            Part(lambda p: _union(*(capsule(p, (0, 0.35 * M, 0), root, 0.16 * M, 0.05 * M) for root in roots)), ROOT_M),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(100, 160), footprint=box_footprint(0.34 * M, 0.34 * M))


# ---------------------------------------------------------------------------------------------------------------------
# Végétation basse
# ---------------------------------------------------------------------------------------------------------------------

def vine_curtain(stem: str, seed: int) -> PropModel:
    """Rideau de lianes : une branche morte en travers, d'où pendent des lianes jusqu'au sol."""
    WOOD, VINE, LEAF = range(3)
    materials = [make_material("bleached", BLEACHED), make_material("vine", MOSS_WET, contrast=0.9), make_material("leaf", LICHEN, contrast=0.9)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        left, right = np.array([-1.1 * M, 2.0 * M, 0.0]), np.array([1.2 * M, 2.4 * M, 0.2 * M])
        vines = []
        for t in np.linspace(0.1, 0.9, 5):
            top = left + (right - left) * t
            bottom = top + np.array([w.uniform(-0.1, 0.1) * M, -w.uniform(1.1, 1.9) * M, w.uniform(-0.05, 0.1) * M])
            vines.append((top, bottom))
        leaves = [a + (b - a) * w.uniform(0.15, 0.95) for a, b in vines for _ in range(3)]
        return [
            Part(lambda p: _union(capsule(p, left, right, 0.09 * M, 0.07 * M),
                                  capsule(p, left, left + np.array([-0.15 * M, -1.9 * M, 0.0]), 0.1 * M, 0.12 * M)), WOOD),
            Part(lambda p: _union(*(capsule(p, a, b, 0.035 * M, 0.02 * M) for a, b in vines)), VINE),
            Part(lambda p: _union(*(ellipsoid(p, c, (0.11 * M, 0.07 * M, 0.06 * M)) for c in leaves)), LEAF),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(110, 110))


def hanging_moss(stem: str, seed: int) -> PropModel:
    """Chicot penché drapé de mousse pendante : une silhouette grise qu'on prend de loin pour quelqu'un."""
    WOOD, HANG = range(2)
    materials = [make_material("root", ROOT, contrast=0.8), make_material("hanging_moss", MIST, contrast=0.8)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        trunk = _gnarled(w, np.zeros(3), np.array([0.35, 1.0, 0.0]), 2.1 * M, 3)
        arm = _gnarled(w, trunk[2], np.array([-1.0, 0.4, 0.2]), 0.8 * M, 2)
        drapes = [(q + np.array([0.0, -0.05 * M, 0.0]), q + np.array([w.uniform(-0.08, 0.08) * M, -w.uniform(0.6, 1.2) * M, 0.0]))
                  for q in trunk[2:] + arm[1:]]
        return [
            Part(lambda p: _union(_branch_chain(trunk, 0.12 * M, 0.06 * M)(p), _branch_chain(arm, 0.06 * M, 0.03 * M)(p)), WOOD),
            Part(lambda p: _union(*(capsule(p, a, b, 0.12 * M, 0.03 * M) for a, b in drapes)), HANG),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(80, 100))


def reeds(stem: str, seed: int) -> PropModel:
    """Massettes : tiges fines et épis bruns, à hauteur de hanche."""
    STEM, LEAF, HEAD = range(3)
    materials = [make_material("stem", MOSS_WET, contrast=0.9), make_material("leaf", LICHEN, contrast=0.9), make_material("cattail", PEAT, contrast=0.7)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        stems = []
        for _ in range(12):
            base = np.array([w.uniform(-0.45, 0.45) * M, 0.0, w.uniform(-0.3, 0.3) * M])
            top = base + np.array([w.uniform(-0.15, 0.25) * M, w.uniform(0.9, 1.35) * M, w.uniform(-0.1, 0.1) * M])
            stems.append((base, top))
        blades = [(b, b + np.array([w.uniform(-0.35, 0.35) * M, w.uniform(0.5, 0.8) * M, w.uniform(-0.1, 0.1) * M])) for b, _ in stems]
        heads = [t for _, t in stems[::2]]
        return [
            Part(lambda p: _union(*(capsule(p, a, b, 0.025 * M, 0.015 * M) for a, b in stems)), STEM),
            Part(lambda p: _union(*(capsule(p, a, b, 0.045 * M, 0.01 * M) for a, b in blades)), LEAF),
            Part(lambda p: _union(*(capsule(p, h - np.array([0.0, 0.28 * M, 0.0]), h - np.array([0.0, 0.05 * M, 0.0]), 0.055 * M)
                                    for h in heads)), HEAD),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(64, 70))


def lily_pads(stem: str, seed: int) -> PropModel:
    """Nénuphars : feuilles rondes entaillées à fleur d'eau, une fleur pâle."""
    PAD, PAD_LIGHT, FLOWER = range(3)
    materials = [make_material("pad", MOSS_WET), make_material("pad_light", LICHEN, contrast=0.8), make_material("flower", WATER_MILK, contrast=0.6)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        pads = [(np.array([w.uniform(-0.7, 0.7) * M, 0.02 * M, w.uniform(-0.5, 0.5) * M]), w.uniform(0.18, 0.3) * M, w.uniform(0, 2 * np.pi))
                for _ in range(7)]

        def pad(p: np.ndarray, c: np.ndarray, r: float, notch: float) -> np.ndarray:
            disc = cylinder(p, c, r, 0.02 * M, 0.01 * M)
            local = (p - c) @ rotation_y(notch)
            wedge = np.maximum(np.abs(local[:, 0]) - 0.04 * M - local[:, 2] * 0.35, -local[:, 2])
            return np.maximum(disc, -wedge)

        flower_at = pads[0][0] + np.array([0.0, 0.08 * M, 0.0])
        return [
            Part(lambda p: _union(*(pad(p, c, r, n) for i, (c, r, n) in enumerate(pads) if i % 2)), PAD),
            Part(lambda p: _union(*(pad(p, c, r, n) for i, (c, r, n) in enumerate(pads) if not i % 2)), PAD_LIGHT),
            Part(lambda p: _union(*(ellipsoid(p, flower_at + np.array([np.cos(a) * 0.07 * M, 0.02 * M, np.sin(a) * 0.07 * M]),
                                              (0.06 * M, 0.05 * M, 0.04 * M), rotation_y(a)) for a in np.linspace(0, 2 * np.pi, 5, endpoint=False))), FLOWER),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(70, 44))


def toxic_mushrooms(stem: str, seed: int) -> PropModel:
    """Champignons vénéneux : chapeaux violet profond piqués de points de spore lumineux."""
    STALK, CAP, DOT = range(3)
    materials = [make_material("stalk", SILVER, contrast=0.6), make_material("cap", VIOLET), make_emissive("spore", SPORE)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        caps = [(w.uniform(-0.45, 0.45) * M, w.uniform(0.2, 0.5) * M, w.uniform(-0.3, 0.3) * M, w.uniform(0.13, 0.24) * M) for _ in range(6)]
        dots = [(x + w.uniform(-0.5, 0.5) * r, y + r * 0.45, z + r * 0.5) for x, y, z, r in caps for _ in range(2)]
        return [
            Part(lambda p: _union(*(capsule(p, (x, 0, z), (x, y, z), r * 0.3) for x, y, z, r in caps)), STALK),
            Part(lambda p: _union(*(ellipsoid(p, (x, y, z), (r, r * 0.6, r)) for x, y, z, r in caps)), CAP),
            Part(lambda p: _union(*(sphere(p, d, 0.035 * M) for d in dots)), DOT),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(56, 48))


def spore_patch(stem: str, seed: int) -> PropModel:
    """Tapis de mousse d'où montent des vesses-de-loup mûres, qui luisent avant d'éclater."""
    MOSS_M, PUFF, GLOW = range(3)
    materials = [make_material("moss", MOSS_WET), make_material("puff", LICHEN, contrast=0.8), make_emissive("glow", SPORE)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        mat = [(w.uniform(-0.6, 0.6) * M, 0.04 * M, w.uniform(-0.4, 0.4) * M) for _ in range(6)]
        puffs = [(w.uniform(-0.5, 0.5) * M, w.uniform(0.1, 0.16) * M, w.uniform(-0.35, 0.35) * M, w.uniform(0.08, 0.14) * M) for _ in range(6)]
        glows = [(x, y + r * 0.8, z) for x, y, z, r in puffs[:3]]
        return [
            Part(_clumps(mat, [(0.35 * M, 0.06 * M, 0.3 * M)] * len(mat), 0.02 * M, 0.2 * M), MOSS_M),
            Part(lambda p: _union(*(sphere(p, (x, y, z), r) for x, y, z, r in puffs)), PUFF),
            Part(lambda p: _union(*(sphere(p, g, 0.05 * M) for g in glows)), GLOW),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(64, 40))


# ---------------------------------------------------------------------------------------------------------------------
# Bois noyé
# ---------------------------------------------------------------------------------------------------------------------

def fallen_log(stem: str, seed: int) -> PropModel:
    """Tronc blanchi couché dans la vase, écorce partie par plaques, lichen sur le dessus."""
    WOOD, BARK, LICHEN_M, CUT = range(4)
    materials = [make_material("bleached", BLEACHED), make_material("bark", ROOT, contrast=0.8), make_material("lichen", LICHEN, contrast=0.8),
                 make_material("cut", SILVER, contrast=0.6)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        bark = [(w.uniform(-0.2, 0.2) * M, w.uniform(0.2, 0.45) * M, z * M) for z in (-1.1, -0.3, 0.6)]
        lichen = [(w.uniform(-0.1, 0.1) * M, 0.6 * M, z * M) for z in (-0.7, 0.2, 1.0)]
        log = lambda p: capsule(p, (0, 0.32 * M, -1.5 * M), (0, 0.3 * M, 1.4 * M), 0.34 * M, 0.3 * M)
        return [
            Part(_sunk(log, 0.08 * M), WOOD),
            Part(lambda p: np.maximum(_union(*(ellipsoid(p, c, (0.36 * M, 0.2 * M, 0.35 * M)) for c in bark)), log(p) - 0.02 * M), BARK),
            Part(_clumps(lichen, [(0.24 * M, 0.08 * M, 0.3 * M)] * 3, 0.03 * M, 0.2 * M), LICHEN_M),
            Part(lambda p: cylinder(p, (0, 0.3 * M, 1.6 * M), 0.27 * M, 0.02 * M, 0.01 * M, rotation_x(np.pi / 2)), CUT),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(100, 64), footprint=box_footprint(0.35 * M, 1.5 * M))


def rotten_stump(stem: str, seed: int) -> PropModel:
    """Souche creuse, pourrie de l'intérieur, collerette de champignons pâles."""
    WOOD, HOLLOW, SHROOM, MOSS_M = range(4)
    materials = [make_material("bleached", BLEACHED), make_material("hollow", ROOT, contrast=0.6), make_material("shroom", WATER_MILK, contrast=0.6),
                 make_material("moss", MOSS_WET)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        # Bord déchiqueté : le sommet est coupé par des plans penchés tirés au hasard.
        jag = [(rotation_y(w.uniform(0, 2 * np.pi)) @ rotation_x(w.uniform(0.3, 0.6)), w.uniform(0.55, 0.8) * M) for _ in range(4)]

        def shell(p: np.ndarray) -> np.ndarray:
            body = cylinder(p, (0, 0.4 * M, 0), 0.44 * M, 0.4 * M, 0.05 * M)
            cut = _union(*((p @ r)[:, 1] - h for r, h in jag))
            return np.maximum(np.maximum(body, cut), -cylinder(p, (0, 0.6 * M, 0), 0.3 * M, 0.4 * M))

        shelves = [(np.cos(a) * 0.45 * M, w.uniform(0.2, 0.45) * M, np.sin(a) * 0.45 * M) for a in (0.4, 1.0, 1.6)]
        return [
            Part(shell, WOOD),
            Part(lambda p: cylinder(p, (0, 0.35 * M, 0), 0.3 * M, 0.3 * M), HOLLOW),
            Part(lambda p: _union(*(ellipsoid(p, s, (0.14 * M, 0.04 * M, 0.12 * M)) for s in shelves)), SHROOM),
            Part(lambda p: ellipsoid(p, (-0.3 * M, 0.08 * M, 0.25 * M), (0.3 * M, 0.1 * M, 0.25 * M)), MOSS_M),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(56, 52), footprint=box_footprint(0.44 * M, 0.44 * M))


def sunken_trunk(stem: str, seed: int) -> PropModel:
    """Énorme tronc à moitié englouti : une échine de bois mort qui affleure, branches cassées dressées."""
    WOOD, ROOT_M, MOSS_M = range(3)
    materials = [make_material("bleached", BLEACHED), make_material("root", ROOT, contrast=0.8), make_material("moss", MOSS_WET)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        spine = lambda p: capsule(p, (0, 0.1 * M, -2.4 * M), (0, 0.05 * M, 2.3 * M), 0.6 * M, 0.45 * M)
        stubs = [((0.0, 0.5 * M, z * M), (w.uniform(-0.4, 0.4) * M, w.uniform(1.0, 1.5) * M, z * M + w.uniform(-0.3, 0.3) * M)) for z in (-1.2, 0.8)]
        roots = [np.array([np.cos(a) * 0.9 * M, np.sin(a) * 0.7 * M + 0.2 * M, -2.6 * M]) for a in np.linspace(0.2, np.pi - 0.2, 5)]
        moss = [(w.uniform(-0.1, 0.1) * M, 0.5 * M, z * M) for z in (-1.6, -0.2, 1.2)]
        return [
            Part(_sunk(spine, 0.05 * M), WOOD),
            Part(lambda p: _union(*(capsule(p, a, b, 0.14 * M, 0.06 * M) for a, b in stubs)), WOOD),
            Part(_sunk(lambda p: _union(*(capsule(p, (0, 0.2 * M, -2.3 * M), r, 0.14 * M, 0.05 * M) for r in roots)), 0.05 * M), ROOT_M),
            Part(_clumps(moss, [(0.28 * M, 0.08 * M, 0.7 * M)] * 3, 0.03 * M, 0.25 * M), MOSS_M),
        ]

    return PropModel(stem, parts, materials, AXIS_Y_YAW, canvas=(150, 90), footprint=box_footprint(0.55 * M, 2.4 * M))


# ---------------------------------------------------------------------------------------------------------------------
# Vestiges noyés
# ---------------------------------------------------------------------------------------------------------------------

def broken_walkway(stem: str, seed: int) -> PropModel:
    """Caillebotis sur pilotis : planches disjointes, une travée effondrée dans l'eau."""
    PLANK, POST, ROPE = range(3)
    materials = [make_material("plank", WOOD_OLD), make_material("post", ROOT, contrast=0.8), make_material("rope", BLEACHED, contrast=0.6)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        planks = []
        for index, x in enumerate(np.linspace(-1.5, 1.5, 11)):
            if index in (6, 7):
                continue
            tilt = rotation_z(w.uniform(-0.06, 0.06)) @ rotation_y(w.uniform(-0.08, 0.08))
            planks.append(((x * M, 0.32 * M + w.uniform(-0.02, 0.02) * M, 0.0), tilt))
        fallen = [((0.45 * M, 0.08 * M, 0.1 * M), rotation_z(0.5)), ((0.72 * M, 0.05 * M, -0.1 * M), rotation_z(-0.4) @ rotation_y(0.3))]
        posts = [(x * M, z * M) for x in (-1.5, -0.3, 1.0) for z in (-0.4, 0.4)]
        return [
            Part(lambda p: _union(*(rounded_box(p, c, (0.12 * M, 0.03 * M, 0.45 * M), 0.01 * M, r) for c, r in planks + fallen)), PLANK),
            Part(lambda p: _union(*(cylinder(p, (x, 0.2 * M, z), 0.07 * M, 0.24 * M, 0.02 * M) for x, z in posts)), POST),
            Part(lambda p: _union(*(np.abs(cylinder(p, (x, 0.3 * M, z), 0.1 * M, 0.03 * M)) - 0.015 * M for x, z in posts[::3])), ROPE),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(130, 64))


def collapsed_pontoon(stem: str, seed: int) -> PropModel:
    """Ponton effondré : un plancher carré basculé dans l'eau, pieux arrachés, un bidon d'amarrage."""
    PLANK, POST, DRUM, RUST_M = range(4)
    materials = [make_material("plank", WOOD_OLD), make_material("post", ROOT, contrast=0.8), make_material("drum", IRON), make_material("rust", RUST)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        tilt = rotation_x(0.28) @ rotation_z(-0.12)
        deck = lambda p: _union(*(rounded_box((p - np.array([0.0, 0.25 * M, 0.0])) @ tilt, (x * M, 0.0, 0.0), (0.13 * M, 0.04 * M, 1.1 * M), 0.01 * M)
                                  for x in np.linspace(-1.0, 1.0, 8)))
        posts = [((x * M, 0.0, z * M), rotation_z(w.uniform(-0.3, 0.3)) @ rotation_x(w.uniform(-0.2, 0.2))) for x, z in ((-1.1, -1.0), (1.1, -1.1), (-1.0, 1.0))]
        return [
            Part(_sunk(deck, 0.02 * M), PLANK),
            Part(lambda p: _union(*(cylinder(p, np.array(c) + np.array([0.0, 0.55 * M, 0.0]), 0.1 * M, 0.55 * M, 0.03 * M, r) for c, r in posts)), POST),
            Part(_sunk(lambda p: cylinder(p, (1.4 * M, 0.2 * M, 0.7 * M), 0.3 * M, 0.42 * M, 0.05 * M, rotation_x(np.pi / 2) @ rotation_y(0.6)), 0.03 * M), DRUM),
            Part(lambda p: sphere(p, (1.5 * M, 0.45 * M, 0.9 * M), 0.12 * M), RUST_M),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(140, 100), footprint=box_footprint(1.15 * M, 1.1 * M))


def sunken_boat(stem: str, seed: int) -> PropModel:
    """Barque noyée : coque de bois penchée, la proue hors de l'eau, un aviron resté en travers."""
    HULL, INSIDE, OAR, WATER = range(4)
    materials = [make_material("hull", WOOD_OLD), make_material("inside", PEAT, contrast=0.6), make_material("oar", BLEACHED),
                 make_material("water", WATER_DARK, contrast=0.5)]
    tilt = rotation_x(-0.22) @ rotation_z(0.18)

    def hull(p: np.ndarray) -> np.ndarray:
        local = (p - np.array([0.0, 0.2 * M, 0.0])) @ tilt
        outer = ellipsoid(local, (0, 0, 0), (0.62 * M, 0.42 * M, 1.5 * M))
        inner = ellipsoid(local, (0, 0.08 * M, 0), (0.52 * M, 0.38 * M, 1.38 * M))
        return np.maximum(np.maximum(outer, -inner), local[:, 1] - 0.18 * M)

    def parts() -> list[Part]:
        return [
            Part(_sunk(hull, 0.0), HULL),
            Part(_sunk(lambda p: ellipsoid((p - np.array([0.0, 0.2 * M, 0.0])) @ tilt, (0, 0.02 * M, 0), (0.5 * M, 0.2 * M, 1.3 * M)), 0.0), WATER),
            Part(lambda p: _sunk(lambda q: capsule(q, (-0.9 * M, 0.35 * M, -0.2 * M), (0.8 * M, 0.3 * M, 0.5 * M), 0.05 * M), 0.0)(p), OAR),
        ]

    return PropModel(stem, parts, materials, AXIS_Y_YAW, canvas=(100, 80), footprint=box_footprint(0.6 * M, 1.4 * M))


def drowned_cart(stem: str, seed: int) -> PropModel:
    """Charrette embourbée : plateau penché, une roue enfoncée jusqu'au moyeu, brancards levés vers le ciel."""
    WOOD, WHEEL, IRON_M, MUD = range(4)
    materials = [make_material("wood", WOOD_OLD), make_material("wheel", ROOT, contrast=0.8), make_material("iron", RUST), make_material("mud", PEAT, contrast=0.7)]
    tilt = rotation_z(0.2) @ rotation_x(-0.25)

    def parts() -> list[Part]:
        bed = lambda p: rounded_box((p - np.array([0.0, 0.55 * M, 0.0])) @ tilt, (0, 0, 0), (0.6 * M, 0.06 * M, 0.9 * M), 0.02 * M)
        sides = lambda p: _union(*(rounded_box((p - np.array([0.0, 0.55 * M, 0.0])) @ tilt, (x * M, 0.18 * M, 0), (0.04 * M, 0.14 * M, 0.9 * M), 0.01 * M)
                                   for x in (-0.58, 0.58)))
        shafts = lambda p: _union(*(capsule(p, (x * M, 0.7 * M, 0.9 * M), (x * M * 0.6, 1.6 * M, 1.9 * M), 0.05 * M) for x in (-0.4, 0.4)))
        wheels = lambda p: _union(*(np.abs(cylinder(p, c, 0.5 * M, 0.05 * M, 0.02 * M, rotation_z(np.pi / 2) @ r)) - 0.035 * M
                                    for c, r in (((-0.7 * M, 0.2 * M, -0.2 * M), rotation_x(0.1)), ((0.72 * M, 0.45 * M, -0.1 * M), rotation_x(-0.15)))))
        return [
            Part(lambda p: _union(bed(p), sides(p)), WOOD),
            Part(lambda p: shafts(p), WOOD),
            Part(_sunk(wheels, 0.02 * M), WHEEL),
            Part(lambda p: _union(*(cylinder(p, c, 0.1 * M, 0.08 * M, 0.02 * M, rotation_z(np.pi / 2)) for c in ((-0.7 * M, 0.2 * M, -0.2 * M), (0.72 * M, 0.45 * M, -0.1 * M)))), IRON_M),
            Part(lambda p: ellipsoid(p, (-0.6 * M, 0.02 * M, -0.1 * M), (0.6 * M, 0.12 * M, 0.5 * M)), MUD),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(100, 100), footprint=box_footprint(0.7 * M, 0.9 * M))


def rusted_barrel(stem: str, seed: int) -> PropModel:
    """Fût d'huile rouillé, penché dans la vase, une coulée irisée sur le flanc."""
    STEEL, RUST_M, RIB, SLICK = range(4)
    materials = [make_material("steel", "#5A6A5E"), make_material("rust", RUST_ORANGE), make_material("rib", IRON), make_material("slick", VIOLET, contrast=0.6)]
    tilt = rotation_z(0.22) @ rotation_x(0.1)

    def parts() -> list[Part]:
        w = Weathering(seed)
        local = lambda p: (p - np.array([0.0, 0.45 * M, 0.0])) @ tilt
        spots = [(w.uniform(-0.25, 0.25) * M, w.uniform(-0.3, 0.3) * M, 0.3 * M) for _ in range(4)]
        return [
            Part(_sunk(lambda p: cylinder(local(p), (0, 0, 0), 0.3 * M, 0.45 * M, 0.04 * M), 0.06 * M), STEEL),
            Part(lambda p: _union(*(sphere(local(p), s, 0.11 * M) for s in spots)), RUST_M),
            Part(lambda p: _union(*(np.abs(cylinder(local(p), (0, y * M, 0), 0.31 * M, 0.02 * M)) - 0.01 * M for y in (-0.15, 0.15))), RIB),
            Part(lambda p: ellipsoid(p, (0.35 * M, 0.02 * M, 0.2 * M), (0.45 * M, 0.03 * M, 0.3 * M)), SLICK),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(56, 60), footprint=box_footprint(0.3 * M, 0.3 * M))


def old_post(stem: str, seed: int) -> PropModel:
    """Pieu d'amarrage : bois fendu, une corde encore nouée qui file vers l'eau."""
    WOOD, ROPE, LICHEN_M = range(3)
    materials = [make_material("post", WOOD_OLD), make_material("rope", BLEACHED, contrast=0.6), make_material("lichen", LICHEN, contrast=0.8)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        lean = rotation_z(w.uniform(-0.15, 0.15))
        rope = [np.array([0.0, 1.1 * M, 0.0]), np.array([0.3 * M, 0.7 * M, 0.3 * M]), np.array([0.6 * M, 0.2 * M, 0.5 * M]), np.array([0.9 * M, 0.02 * M, 0.6 * M])]
        return [
            Part(lambda p: cylinder(p @ lean, (0, 0.7 * M, 0), 0.11 * M, 0.7 * M, 0.04 * M), WOOD),
            Part(lambda p: _union(np.abs(cylinder(p @ lean, (0, 1.1 * M, 0), 0.13 * M, 0.04 * M)) - 0.02 * M,
                                  _branch_chain(rope, 0.025 * M, 0.02 * M)(p)), ROPE),
            Part(lambda p: ellipsoid(p @ lean, (0.05 * M, 0.5 * M, 0.08 * M), (0.08 * M, 0.2 * M, 0.06 * M)), LICHEN_M),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(52, 70))


def bone_pile(stem: str, seed: int) -> PropModel:
    """Os de bête blanchis au bord de l'eau : un crâne cornu, des côtes, un fémur."""
    BONE_M, SHADE = range(2)
    materials = [make_material("bone", BONE, contrast=0.7), make_material("socket", PEAT, contrast=0.6)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        skull = np.array([0.2 * M, 0.12 * M, 0.1 * M])
        ribs = [(np.array([-0.4 * M + i * 0.12 * M, 0.02 * M, -0.2 * M]), rotation_y(w.uniform(-0.2, 0.2))) for i in range(4)]
        return [
            Part(lambda p: _union(
                ellipsoid(p, skull, (0.16 * M, 0.11 * M, 0.2 * M)),
                capsule(p, skull + np.array([0.0, 0.0, 0.18 * M]), skull + np.array([0.0, -0.03 * M, 0.34 * M]), 0.07 * M, 0.05 * M),
                capsule(p, skull + np.array([-0.1 * M, 0.08 * M, -0.05 * M]), skull + np.array([-0.3 * M, 0.2 * M, -0.1 * M]), 0.035 * M, 0.015 * M),
                capsule(p, skull + np.array([0.1 * M, 0.08 * M, -0.05 * M]), skull + np.array([0.3 * M, 0.2 * M, -0.12 * M]), 0.035 * M, 0.015 * M),
                *(np.abs(ellipsoid(p, c, (0.05 * M, 0.2 * M, 0.25 * M), r)) - 0.018 * M for c, r in ribs),
                capsule(p, (-0.5 * M, 0.04 * M, 0.3 * M), (0.0, 0.04 * M, 0.45 * M), 0.035 * M),
            ), BONE_M),
            Part(lambda p: _union(*(sphere(p, skull + np.array([s * 0.07 * M, 0.02 * M, 0.14 * M]), 0.035 * M) for s in (-1, 1))), SHADE),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(56, 36))


def sunken_shrine(stem: str, seed: int) -> PropModel:
    """Oratoire de pierre enfoncé de biais : niche vide, toit à deux pans couvert de lichen, bougie verte."""
    STONE_M, ROOF, LICHEN_M, NICHE, FLAME = range(5)
    materials = [make_material("stone", STONE), make_material("roof", "#5A5A58"), make_material("lichen", LICHEN, contrast=0.8),
                 make_material("niche", PEAT, contrast=0.6), make_emissive("flame", SPORE)]
    tilt = rotation_z(0.12) @ rotation_x(-0.08)

    def parts() -> list[Part]:
        w = Weathering(seed)
        local = lambda p: (p - np.array([0.0, 0.0, 0.0])) @ tilt
        body = lambda p: rounded_box(local(p), (0, 0.8 * M, 0), (0.42 * M, 0.8 * M, 0.35 * M), 0.04 * M)
        niche = lambda p: rounded_box(local(p), (0, 1.1 * M, 0.3 * M), (0.22 * M, 0.3 * M, 0.12 * M), 0.08 * M)

        def roof(p: np.ndarray) -> np.ndarray:
            q = local(p) - np.array([0.0, 1.62 * M, 0.0])
            slope = (np.abs(q[:, 0]) * 0.8 + q[:, 1] - 0.35 * M) / np.sqrt(1.64)
            return np.maximum(np.maximum(slope, -q[:, 1]), np.abs(q[:, 2]) - 0.45 * M)

        lichen = [np.array([w.uniform(-0.3, 0.3) * M, 1.75 * M, w.uniform(-0.3, 0.3) * M]) @ tilt.T for _ in range(3)]
        candle = np.array([0.0, 0.9 * M, 0.25 * M]) @ tilt.T
        return [
            Part(_sunk(lambda p: np.maximum(body(p), -niche(p)), 0.25 * M), STONE_M),
            Part(roof, ROOF),
            Part(_clumps(lichen, [(0.25 * M, 0.08 * M, 0.25 * M)] * 3, 0.02 * M, 0.2 * M), LICHEN_M),
            Part(lambda p: rounded_box(local(p), (0, 1.1 * M, 0.2 * M), (0.2 * M, 0.28 * M, 0.02 * M), 0.06 * M), NICHE),
            Part(lambda p: ellipsoid(p, candle, (0.05 * M, 0.09 * M, 0.05 * M)), FLAME),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(70, 100), footprint=box_footprint(0.42 * M, 0.35 * M))


def swamp_lantern(stem: str, seed: int) -> PropModel:
    """Lanterne de passeur : perche plantée dans la vase, lanterne pendue au crochet, lueur de spore."""
    POLE, IRON_M, GLASS = range(3)
    materials = [make_material("pole", WOOD_OLD), make_material("iron", IRON), make_emissive("glow", SPORE)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        lean = rotation_z(w.uniform(0.05, 0.12))
        hook = np.array([0.35 * M, 1.95 * M, 0.0])
        lantern = hook + np.array([0.0, -0.35 * M, 0.0])
        return [
            Part(lambda p: _union(capsule(p @ lean, (0, 0, 0), (0, 2.1 * M, 0), 0.06 * M, 0.045 * M),
                                  capsule(p @ lean, (0, 2.0 * M, 0), hook, 0.035 * M)), POLE),
            Part(lambda p: _union(capsule(p @ lean, hook, lantern + np.array([0.0, 0.14 * M, 0.0]), 0.015 * M),
                                  cylinder(p @ lean, lantern + np.array([0.0, 0.14 * M, 0.0]), 0.1 * M, 0.03 * M, 0.01 * M),
                                  cylinder(p @ lean, lantern - np.array([0.0, 0.14 * M, 0.0]), 0.1 * M, 0.025 * M, 0.01 * M)), IRON_M),
            Part(lambda p: cylinder(p @ lean, lantern, 0.08 * M, 0.12 * M, 0.02 * M), GLASS),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(52, 90))


# ---------------------------------------------------------------------------------------------------------------------
# Catalogue
# ---------------------------------------------------------------------------------------------------------------------

def catalog() -> list[PropModel]:
    models: list[PropModel] = []
    models += dead_tree_large("prop_dead_tree_large", 501)
    models += [
        dead_tree_mossy("prop_dead_tree_mossy_large", 502),
        dead_tree_small("prop_dead_tree_small", 503),
        aerial_roots("prop_aerial_roots", 511, 0.0),
        aerial_roots("prop_aerial_roots_twisted", 512, 0.6 * M),
        root_mass("prop_root_mass_large", 513),
        bound_tree("prop_bound_tree", 514),
        vine_curtain("prop_vine_curtain", 521),
        hanging_moss("prop_hanging_moss", 522),
        reeds("prop_reeds", 523),
        lily_pads("prop_lily_pads", 524),
        toxic_mushrooms("prop_toxic_mushrooms", 525),
        spore_patch("prop_spore_patch", 526),
        fallen_log("prop_fallen_log", 531),
        rotten_stump("prop_rotten_stump", 532),
        sunken_trunk("prop_sunken_trunk_large", 533),
        broken_walkway("prop_broken_walkway", 541),
        collapsed_pontoon("prop_collapsed_pontoon", 542),
        sunken_boat("prop_sunken_boat", 543),
        drowned_cart("prop_drowned_cart", 544),
        rusted_barrel("prop_rusted_barrel", 545),
        old_post("prop_old_post", 546),
        bone_pile("prop_bone_pile", 547),
        sunken_shrine("prop_sunken_shrine", 548),
        swamp_lantern("prop_swamp_lantern", 549),
    ]
    return models
