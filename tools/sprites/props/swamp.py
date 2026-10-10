"""
Décors des Marécages (plan 08, lot P5 ; repris au plan 31, lot M1) : une réalité fragile, eau laiteuse, bois blanchi,
barbes de lichen et lueurs de spores.

Échelle réelle du personnage (1 m ≈ 27,5 unités), mêmes noms de fichiers que les anciens décors (mêmes uid).
Palette « Marécages » de la charte (§3) : bois blanchi pour les arbres morts, mousse humide et lichen jaune-vert,
violet profond pour ce qui est vénéneux, spore verte pour les rares lueurs, reflet argenté sur l'eau. Le sol du marais
est sombre et moucheté : un décor s'y lit par sa valeur (bois blanchi, os, lichen clair) et par un pied mouillé plus
sombre, jamais par un contour seul. Ce qui est « noyé » est coupé à un niveau d'eau sous le point au sol et cerné d'un
liseré de clapot argenté, pour se lire posé dans l'eau plutôt que sur elle.
Le placement reste celui de `SwampPropPlacer` (zones d'eau, lisières, bosquets morts, poches fongiques).
"""
from __future__ import annotations

from typing import Callable

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, cylinder, ellipsoid, rotation_x, rotation_y, rotation_z, rounded_box, sphere
from ._flora import _align_y, _branch_chain, _clumps, _gnarled, _grooves, _sag, _shelves, _union
from ._kit import AXIS_X_YAW, AXIS_Y_YAW, M, PropModel, Weathering, box_footprint
from ._surface import bands, both, noise_mask, painted, value_noise, waterline

# Palette Marécages (charte §3).
WATER_DARK = "#4A6A5E"
WATER_MILK = "#B8C8BE"
MOSS_WET = "#3A6A38"
PEAT = "#3A2E22"
BLEACHED = "#9A8E7A"
LICHEN = "#8A9A4A"
SPORE = "#6ACA5A"
MIST = "#7A8A9A"
VIOLET = "#4A2A5A"
SILVER = "#C4D0D4"
# Tons dérivés de la palette du biome : écorce grise des rainures, bois mouillé du pied, barbe de lichen gris-vert.
BARK_GREY = "#655A4B"
WET_WOOD = "#4C4236"
BEARD = "#8E9C84"
MOSS_LIGHT = "#5E8A44"
# Orientation des décors composés pour l'écran : x vers la droite, +z face à la caméra. Un léger trois-quarts garde
# deux faces visibles aux objets construits (boîtes, oratoire) ; les touffes et les arbres se composent de face.
FRONT = 0.0
THREE_QUARTER = float(np.radians(24.0))
# Hors palette de biome, repris de la palette maîtresse : rouille, fer, pierre, os et bois des vestiges humains.
RUST = "#6A4430"
RUST_ORANGE = "#A85C30"
IRON = "#4A4648"
STONE = "#8A887C"
BONE = "#D8CFB8"
WOOD_OLD = "#7A6248"
WOOD_DARK = "#5A4632"
FUNGUS = "#C8B07E"
CATTAIL = "#7A5234"
PETAL = "#E2D4DA"
POLLEN = "#D6B44A"
LILAC = "#8A5A9A"
CLOTH_RED = "#8E3A34"
PAINT_FADED = "#A68E58"


def _sunk(distance: Callable[[np.ndarray], np.ndarray], level: float) -> Callable[[np.ndarray], np.ndarray]:
    """Garde ce qui dépasse de l'eau : le reste est sous la surface de la tuile."""
    return lambda p: np.maximum(distance(p), level - p[:, 1])


def _beards(w: Weathering, anchors: list[np.ndarray], length: tuple[float, float], strands: int = 5,
            spread: float = 0.1 * M, radius: float = 0.034 * M) -> Callable[[np.ndarray], np.ndarray]:
    """
    Barbes de lichen (usnée) : sous chaque attache, une touffe plate d'où pendent des brins d'un pixel, de longueurs
    très inégales, qui ondulent un peu. Une frange effilochée, jamais des stalactites de même taille.
    """
    tufts = []
    pieces = []
    for anchor in anchors:
        tufts.append(anchor + np.array([0.0, -0.04 * M, 0.0]))
        for index in range(strands):
            x = (index / max(strands - 1, 1) - 0.5) * 2 * spread + w.uniform(-0.02, 0.02) * M
            top = anchor + np.array([x * 0.6, -0.03 * M, w.uniform(-0.03, 0.03) * M])
            # Les brins du milieu tombent plus bas : la touffe s'effile en pointe.
            centrality = 1.0 - abs(index / max(strands - 1, 1) - 0.5) * 1.4
            drop = w.uniform(*length) * (0.55 + 0.45 * centrality) * M
            sway = np.array([w.uniform(-0.05, 0.05) * M, 0.0, w.uniform(-0.02, 0.02) * M])
            middle = top + np.array([x * 0.25, -drop * 0.5, 0.0]) + sway
            bottom = top + np.array([x * 0.4, -drop, 0.0]) - sway * 0.5
            pieces.append((top, middle, bottom, radius * w.uniform(0.85, 1.15)))
    return lambda p: _union(*(ellipsoid(p, t, (spread * 0.55, 0.04 * M, 0.05 * M)) for t in tufts),
                            *(np.minimum(capsule(p, t, m, r, r * 0.85), capsule(p, m, b, r * 0.85, r * 0.45)) for t, m, b, r in pieces))


def _wet_foot(shape, seed: int, height: float = 0.32 * M):
    """Pied mouillé : la vase et l'eau assombrissent le bas du bois sur une hauteur irrégulière."""
    def mask(p: np.ndarray) -> np.ndarray:
        wobble = noise_mask(0.3 * M, seed, 0.5)(p)
        return p[:, 1] - height + wobble * 0.8

    return painted(shape, mask)


# ---------------------------------------------------------------------------------------------------------------------
# Arbres morts
# ---------------------------------------------------------------------------------------------------------------------

class _DeadTree:
    """Arbre mort à branches nues, partagé par le tronc et la « canopée » (branches hautes et barbes de lichen)."""

    def __init__(self, seed: int, height: float, trunk_radius: float, limbs: int):
        w = Weathering(seed)
        self.radius = trunk_radius
        lean = np.array([w.uniform(-0.3, 0.3) * M, 0.0, w.uniform(-0.2, 0.2) * M])
        self.trunk = _gnarled(w, np.zeros(3), np.array([0.0, 1.0, 0.0]) + lean / M * 0.3, height * 0.62, 5, wobble=0.08)
        # Contreforts : racines épaisses qui partent du fût et plongent dans la vase en s'évasant.
        self.buttresses = []
        for angle in np.linspace(0, 2 * np.pi, 6, endpoint=False) + w.uniform(0, 1.0):
            reach = trunk_radius * w.uniform(2.6, 3.4)
            knee = np.array([np.sin(angle) * trunk_radius * 1.4, 0.35 * M, np.cos(angle) * trunk_radius * 1.4])
            foot = np.array([np.sin(angle) * reach, -0.05 * M, np.cos(angle) * reach])
            self.buttresses.append((np.array([0.0, 0.9 * M, 0.0]), knee, foot))
        self.low_limbs: list[list[np.ndarray]] = []
        self.high_limbs: list[list[np.ndarray]] = []
        self.twigs: list[list[np.ndarray]] = []
        for index in range(limbs):
            angle = index * 2.39996 + w.uniform(-0.3, 0.3)
            along = 0.45 + 0.55 * (index + 1) / limbs
            anchor = self.trunk[min(len(self.trunk) - 1, int(along * (len(self.trunk) - 1)))]
            direction = np.array([np.cos(angle), w.uniform(0.25, 0.7), np.sin(angle) * 0.8])
            limb = _gnarled(w, anchor, direction, w.uniform(1.5, 2.3) * M * height / (5.5 * M), 3, wobble=0.25)
            (self.high_limbs if along > 0.7 else self.low_limbs).append(limb)
            # Rameaux nus au bout et au milieu de chaque branche : la silhouette hérissée de l'arbre mort.
            for start in (limb[-1], limb[-1], limb[2], limb[1]):
                twig_dir = np.array([w.uniform(-1, 1), w.uniform(0.2, 0.9), w.uniform(-1, 1)])
                self.twigs.append(_gnarled(w, start, twig_dir, w.uniform(0.35, 0.7) * M, 2))
        # Barbes de lichen sous les branches hautes, plus longues au bout qu'à l'attache.
        self.beard_anchors = []
        for limb in self.high_limbs + self.low_limbs[-1:]:
            for point in limb[1:]:
                self.beard_anchors.append(point + np.array([0.0, -0.05 * M, 0.0]))
        self.beard_seed = seed + 7

    def trunk_shape(self, top_ratio: float = 0.55):
        return _branch_chain(self.trunk, self.radius, self.radius * top_ratio)

    def buttress_shape(self):
        r = self.radius
        return lambda p: _union(*(np.minimum(capsule(p, a, k, r * 0.62, r * 0.42), capsule(p, k, f, r * 0.42, r * 0.12))
                                  for a, k, f in self.buttresses))


def _trunk_details(tree: _DeadTree, seed: int, wood: Callable[[np.ndarray], np.ndarray], hole_height: float):
    """Fût creusé d'une loge sombre côté caméra ; renvoie (bois creusé, intérieur de la loge)."""
    r = tree.radius
    axis = tree.trunk[0] + (tree.trunk[-1] - tree.trunk[0]) * (hole_height / np.linalg.norm(tree.trunk[-1] - tree.trunk[0]))
    hole = axis + np.array([r * 0.35, 0.0, r * 0.95])
    cavity = lambda p: ellipsoid(p, hole, (r * 0.42, r * 0.8, r * 0.6))
    carved = lambda p: np.maximum(wood(p), -cavity(p))
    inside = lambda p: np.maximum(wood(p) + 0.01 * M, np.abs(cavity(p)) - 0.03 * M)
    return carved, inside


def dead_tree_large(stem: str, seed: int) -> list[PropModel]:
    shape = _DeadTree(seed, 6.2 * M, 0.38 * M, 6)
    WOOD, GROOVE, WET, HOLLOW, LICHEN_M, SHELF = range(6)
    base_materials = [make_material("bleached", BLEACHED), make_material("bark_groove", BARK_GREY, contrast=0.8),
                      make_material("wet_wood", WET_WOOD, contrast=0.8), make_material("hollow", PEAT, contrast=0.5),
                      make_material("lichen", LICHEN, contrast=0.8), make_material("shelf", FUNGUS, contrast=0.8)]
    r = shape.radius

    def base_parts() -> list[Part]:
        trunk = shape.trunk_shape()
        roots = shape.buttress_shape()
        wood = lambda p: np.minimum(trunk(p), roots(p))
        carved, inside = _trunk_details(shape, seed, wood, 1.9 * M)
        low = lambda p: _union(*(_branch_chain(limb, r * 0.42, r * 0.18)(p) for limb in shape.low_limbs))
        foot = shape.trunk[0]
        shelves = [(foot + np.array([np.sin(a) * r * 1.0, h * M, np.cos(a) * r * 1.0]), s * M, a)
                   for a, h, s in ((-1.1, 1.1, 0.2), (-1.0, 1.32, 0.15), (-1.25, 0.9, 0.13))]
        lichen = both(noise_mask(0.22 * M, seed + 3, 0.28), lambda p: np.abs(p[:, 1] - 1.6 * M) - 1.1 * M)
        return [
            Part(carved, WOOD),
            Part(_grooves(carved, seed), GROOVE),
            Part(painted(carved, lichen), LICHEN_M),
            Part(_wet_foot(carved, seed + 5, 0.45 * M), WET),
            Part(inside, HOLLOW),
            Part(low, WOOD),
            Part(_grooves(low, seed + 1, period=0.1 * M), GROOVE),
            Part(_shelves(shelves), SHELF),
        ]

    BEARD_M, WOOD_HIGH, TWIG, GROOVE_HIGH = range(4)
    canopy_materials = [make_material("beard", BEARD, contrast=0.9), make_material("bleached", BLEACHED),
                        make_material("twig", BARK_GREY, contrast=0.8), make_material("bark_groove", BARK_GREY, contrast=0.8)]

    def canopy_parts() -> list[Part]:
        w = Weathering(shape.beard_seed)
        high = lambda p: _union(*(_branch_chain(limb, r * 0.4, r * 0.16)(p) for limb in shape.high_limbs))
        return [
            Part(high, WOOD_HIGH),
            Part(_grooves(high, seed + 2, period=0.1 * M), GROOVE_HIGH),
            Part(lambda p: _union(*(_branch_chain(twig, r * 0.16, r * 0.07)(p) for twig in shape.twigs)), TWIG),
            Part(_beards(w, shape.beard_anchors, (0.45, 1.25)), BEARD_M),
        ]

    canvas = (180, 220)
    return [PropModel(f"{stem}_base", base_parts, base_materials, FRONT, canvas=canvas,
                      footprint=box_footprint(r * 1.4, r * 1.4)),
            PropModel(f"{stem}_canopy", canopy_parts, canopy_materials, FRONT, canvas=canvas, supersample=3)]


def dead_tree_mossy(stem: str, seed: int) -> PropModel:
    """Grand arbre mort étêté : fût brisé net en échardes, gaine de mousse côté nord, loge au pied, barbes basses."""
    shape = _DeadTree(seed, 5.0 * M, 0.44 * M, 4)
    WOOD, GROOVE, MOSS_M, MOSS_TOP, BEARD_M, WET, HOLLOW, SPLINTER, SHELF = range(9)
    materials = [make_material("bleached", BLEACHED), make_material("bark_groove", BARK_GREY, contrast=0.8),
                 make_material("moss", MOSS_WET), make_material("moss_light", MOSS_LIGHT, contrast=0.9),
                 make_material("beard", BEARD, contrast=0.9), make_material("wet_wood", WET_WOOD, contrast=0.8),
                 make_material("hollow", PEAT, contrast=0.5), make_material("splinter", SILVER, contrast=0.6),
                 make_material("shelf", FUNGUS, contrast=0.8)]
    r = shape.radius

    def parts() -> list[Part]:
        w = Weathering(seed + 2)
        trunk = shape.trunk_shape(0.8)
        top = shape.trunk[-1]
        # Cassure : le haut du fût est coupé par des plans penchés, d'où sortent de longues échardes.
        cuts = [(rotation_y(w.uniform(0, 2 * np.pi)) @ rotation_x(w.uniform(0.35, 0.7)), top[1] - w.uniform(0.0, 0.5) * M) for _ in range(3)]
        broken = lambda p: np.maximum(trunk(p), _union(*((p @ c)[:, 1] - h for c, h in cuts)))
        splinters = [(top + np.array([np.sin(a) * r * 0.55, -0.6 * M, np.cos(a) * r * 0.55]),
                      top + np.array([np.sin(a) * r * 0.7, w.uniform(0.0, 0.5) * M, np.cos(a) * r * 0.7])) for a in np.linspace(0.3, 5.5, 5)]
        roots = shape.buttress_shape()
        wood = lambda p: np.minimum(broken(p), roots(p))
        foot = shape.trunk[0]
        hole_c = foot + np.array([r * 0.2, 0.45 * M, r * 1.0])
        cavity = lambda p: ellipsoid(p, hole_c, (r * 0.55, 0.42 * M, r * 0.7))
        carved = lambda p: np.maximum(wood(p), -cavity(p))
        limbs = lambda p: _union(*(_branch_chain(limb, r * 0.4, r * 0.16)(p) for limb in shape.low_limbs + shape.high_limbs))
        # Gaine de mousse sur la moitié droite du fût (côté ombre, mais visible), au-dessus des contreforts.
        moss_side = lambda p: np.maximum(np.maximum(-p[:, 0] - r * 0.15, p[:, 1] - (shape.trunk[-1][1] * 0.8)), 0.55 * M - p[:, 1])
        moss = both(moss_side, noise_mask(0.3 * M, seed + 9, 0.7))
        moss_top = both(moss, lambda p: (0.3 - value_noise(p, 0.2 * M, seed + 11)) * M)
        shelves = [(foot + np.array([np.sin(a) * r * 1.0, h * M, np.cos(a) * r * 1.0]), s * M, a)
                   for a, h, s in ((1.2, 1.5, 0.22), (1.35, 1.78, 0.16), (0.9, 2.5, 0.14))]
        beards = _beards(w, [limb[i] + np.array([0.0, -0.05 * M, 0.0]) for limb in shape.low_limbs + shape.high_limbs for i in (1, 2, 3)],
                         (0.4, 1.0))
        return [
            Part(carved, WOOD),
            Part(_grooves(carved, seed), GROOVE),
            Part(_wet_foot(carved, seed + 4, 0.5 * M), WET),
            Part(painted(carved, moss, 0.02 * M), MOSS_M),
            Part(painted(carved, moss_top, 0.03 * M), MOSS_TOP),
            Part(lambda p: np.maximum(wood(p) + 0.01 * M, np.abs(cavity(p)) - 0.03 * M), HOLLOW),
            Part(lambda p: _union(*(capsule(p, a, b, r * 0.2, r * 0.04) for a, b in splinters)), SPLINTER),
            Part(limbs, WOOD),
            Part(_shelves(shelves), SHELF),
            Part(beards, BEARD_M),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(170, 200), footprint=box_footprint(r * 1.4, r * 1.4))


def dead_tree_small(stem: str, seed: int) -> PropModel:
    """Chicot : fût brisé en échardes, un moignon de branche, écorce qui part par plaques, pied dans la tourbe."""
    WOOD, BARK, GROOVE, SPLINTER, WET, MOSS_M, SHELF, LICHEN_M = range(8)
    materials = [make_material("bleached", BLEACHED), make_material("bark", WOOD_DARK, contrast=0.8),
                 make_material("bark_groove", BARK_GREY, contrast=0.8), make_material("splinter", SILVER, contrast=0.6),
                 make_material("wet_wood", WET_WOOD, contrast=0.8), make_material("moss", MOSS_WET, contrast=0.8),
                 make_material("shelf", FUNGUS, contrast=0.8), make_material("lichen", LICHEN, contrast=0.8)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        trunk = _gnarled(w, np.zeros(3), np.array([0.12, 1.0, 0.05]), 2.5 * M, 4, wobble=0.12)
        stub = _gnarled(w, trunk[2], np.array([-1.0, 0.7, 0.3]), 0.75 * M, 2)
        top = trunk[-1]
        fibre = _branch_chain(trunk, 0.27 * M, 0.17 * M)
        cuts = [(rotation_y(w.uniform(0, 2 * np.pi)) @ rotation_x(w.uniform(0.4, 0.8)), top[1] - w.uniform(0.0, 0.35) * M) for _ in range(3)]
        trunk_shape = lambda p: np.maximum(fibre(p), _union(*((p @ c)[:, 1] - h for c, h in cuts)))
        flare = lambda p: _union(*(capsule(p, (0, 0.5 * M, 0), (np.sin(a) * 0.55 * M, -0.03 * M, np.cos(a) * 0.55 * M), 0.17 * M, 0.06 * M)
                                   for a in np.linspace(0.4, 6.0, 4)))
        wood = lambda p: _union(trunk_shape(p), flare(p), _branch_chain(stub, 0.11 * M, 0.06 * M)(p))
        splinters = [(top - np.array([0.0, 0.35 * M, 0.0]) + np.array([np.sin(a) * 0.13 * M, 0.0, np.cos(a) * 0.13 * M]),
                      top + np.array([np.sin(a) * 0.17 * M, w.uniform(0.05, 0.4) * M, np.cos(a) * 0.17 * M])) for a in np.linspace(0.2, 5.6, 4)]
        # Écorce restante : plaques sombres sur le bas du fût, que le bois blanchi perce par endroits.
        bark = both(noise_mask(0.28 * M, seed + 1, 0.5), lambda p: p[:, 1] - 1.6 * M)
        shelves = [(np.array([0.27 * M * np.sin(-1.0), h * M, 0.27 * M * np.cos(-1.0)]), s * M, -1.0) for h, s in ((0.85, 0.15), (1.05, 0.11))]
        lichen = both(noise_mask(0.18 * M, seed + 4, 0.25), lambda p: np.abs(p[:, 1] - 1.4 * M) - 0.8 * M)
        return [
            Part(wood, WOOD),
            Part(painted(wood, bark, 0.025 * M), BARK),
            Part(_grooves(wood, seed + 2, period=0.11 * M), GROOVE),
            Part(painted(wood, lichen), LICHEN_M),
            Part(_wet_foot(wood, seed + 3, 0.3 * M), WET),
            Part(lambda p: _union(*(capsule(p, a, b, 0.07 * M, 0.015 * M) for a, b in splinters)), SPLINTER),
            Part(_clumps([(-0.3 * M, 0.02 * M, 0.25 * M), (0.35 * M, 0.02 * M, 0.1 * M)], [(0.2 * M, 0.08 * M, 0.16 * M)] * 2, 0.02 * M, 0.2 * M), MOSS_M),
            Part(_shelves(shelves), SHELF),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(80, 110), footprint=box_footprint(0.26 * M, 0.26 * M))


def aerial_roots(stem: str, seed: int, twist: float) -> PropModel:
    """
    Palétuvier : un fût court perché sur des racines en arceaux qui plongent dans la vase, des racines-échasses
    qui pendent encore de la couronne. La variante torse penche, ses arceaux s'enroulent et la moitié de sa couronne est morte.
    """
    ROOT_M, ROOT_GROOVE, WOOD, WET, LEAF, LEAF_TOP, DRIP = range(7)
    materials = [make_material("root", WOOD_OLD), make_material("root_groove", WOOD_DARK, contrast=0.8),
                 make_material("bark", BLEACHED), make_material("wet_wood", WET_WOOD, contrast=0.8),
                 make_material("leaf", MOSS_LIGHT), make_material("leaf_top", LICHEN, contrast=0.9),
                 make_material("aerial_root", WOOD_DARK, contrast=0.8)]
    torsion = twist / M

    def parts() -> list[Part]:
        w = Weathering(seed)
        lean = np.array([torsion * 0.55 * M, 0.0, torsion * 0.2 * M])
        hub = np.array([0.0, 1.35 * M, 0.0]) + lean * 0.5
        arches = []
        for index in range(8):
            angle = index * 2 * np.pi / 8 + w.uniform(-0.2, 0.2)
            reach = w.uniform(0.85, 1.5) * M
            foot = np.array([np.cos(angle) * reach, -0.05 * M, np.sin(angle) * reach * 0.85])
            swirl = np.array([np.sin(angle), 0.0, -np.cos(angle)]) * torsion * 0.5 * M
            knee = hub * 0.6 + foot * 0.62 + np.array([0.0, 0.5 * M, 0.0]) + swirl
            arches.append(([hub + (foot - hub) * 0.08, knee, foot + swirl * 0.3], 0.16 * M if index % 3 == 0 else 0.1 * M))
        crown_base = hub + np.array([0.0, 1.4 * M, 0.0]) + lean
        crown = []
        for index in range(9):
            angle = index * 2.39996
            ring = np.sqrt((index + 0.5) / 9)
            crown.append(crown_base + np.array([np.cos(angle) * ring * 1.0 * M, (1 - ring) * 0.45 * M + w.uniform(-0.1, 0.15) * M,
                                                np.sin(angle) * ring * 0.8 * M]))
        dead_side = torsion > 0.0
        # La variante torse perd la moitié de sa couronne (côté droit) : des rameaux nus à la place.
        living = [c for c in crown if not (dead_side and c[0] > crown_base[0])]
        bare = [_gnarled(w, crown_base, c - crown_base + np.array([0.0, 0.3 * M, 0.0]), 1.2 * M, 3) for c in crown if dead_side and c[0] > crown_base[0]]
        order = sorted(range(len(living)), key=lambda i: -(living[i][1] - living[i][2] * 0.3))
        top_set = set(order[: len(order) // 3])
        drips = [(living[i] + np.array([0.0, -0.25 * M, 0.0]), living[i] + np.array([w.uniform(-0.1, 0.1) * M, -w.uniform(0.9, 1.6) * M, 0.0]))
                 for i in range(0, len(living), 2)]
        trunk = lambda p: capsule(p, hub, crown_base, 0.3 * M, 0.2 * M)
        roots = lambda p: _union(*(_branch_chain(a, r, r * 0.5)(p) for a, r in arches))
        result = [
            Part(roots, ROOT_M),
            Part(_grooves(roots, seed + 1, period=0.09 * M, coverage=0.25), ROOT_GROOVE),
            Part(_wet_foot(roots, seed + 2, 0.28 * M), WET),
            Part(trunk, WOOD),
            Part(_grooves(trunk, seed + 3, period=0.1 * M), ROOT_GROOVE),
            Part(_clumps([c for i, c in enumerate(living) if i not in top_set], [(0.48 * M, 0.32 * M, 0.42 * M)] * (len(living) - len(top_set)),
                         0.07 * M, 0.35 * M), LEAF),
            Part(_clumps([c for i, c in enumerate(living) if i in top_set], [(0.4 * M, 0.28 * M, 0.36 * M)] * len(top_set), 0.07 * M, 0.35 * M),
                 LEAF_TOP),
            Part(lambda p: _union(*(capsule(p, a, b, 0.035 * M, 0.02 * M) for a, b in drips)), DRIP),
        ]
        if bare:
            result.append(Part(lambda p: _union(*(_branch_chain(b, 0.07 * M, 0.03 * M)(p) for b in bare)), WOOD))
        return result

    return PropModel(stem, parts, materials, FRONT, canvas=(130, 160), footprint=box_footprint(0.7 * M, 0.6 * M))


def root_mass(stem: str, seed: int) -> PropModel:
    """
    Chablis : un arbre arraché dont la galette de racines s'est dressée face au chemin. Terre encore accrochée,
    pierres prises dedans, racines claires qui rayonnent et dépassent du bord, radicelles qui pendent ; le tronc
    couché file en arrière et le trou laissé devant s'est rempli d'eau noire.
    """
    EARTH, EARTH_DARK, ROOT_M, ROOT_LIGHT, RADICLE, STONE_M, MOSS_M, WOOD, GROOVE, WATER, RIPPLE = range(11)
    materials = [make_material("earth", "#6A5440", contrast=0.8), make_material("earth_dark", "#4A3A2A", contrast=0.7),
                 make_material("root", WOOD_OLD), make_material("root_light", BLEACHED),
                 make_material("rootlet", BARK_GREY, contrast=0.6), make_material("stone", STONE, contrast=0.8),
                 make_material("moss", MOSS_LIGHT, contrast=0.8), make_material("bleached", BLEACHED),
                 make_material("bark_groove", BARK_GREY, contrast=0.8), make_material("water", WATER_DARK, contrast=0.5),
                 make_material("ripple", WATER_MILK, contrast=0.4)]
    center = np.array([0.0, 0.95 * M, -0.1 * M])
    lean = rotation_x(-0.18)
    face = 0.2 * M

    def plate_local(p: np.ndarray) -> np.ndarray:
        return (p - center) @ lean

    def plate(p: np.ndarray) -> np.ndarray:
        q = plate_local(p)
        angle = np.arctan2(q[:, 1], q[:, 0])
        # Bord déchiré : le rayon varie avec l'angle ; la face avant est bosselée.
        radius = (1.0 + 0.12 * np.sin(angle * 5.0 + 0.7) + 0.06 * np.sin(angle * 11.0)) * M
        radial = np.hypot(q[:, 0], q[:, 1]) - radius
        slab = np.abs(q[:, 2]) - face + 0.04 * M * np.sin(q[:, 0] * 0.4) * np.sin(q[:, 1] * 0.37)
        return np.maximum(np.maximum(radial, slab) * 0.8, -p[:, 1] - 0.05 * M)

    def parts() -> list[Part]:
        w = Weathering(seed)
        spokes = []
        for index in range(11):
            angle = index * 2 * np.pi / 11 + w.uniform(-0.15, 0.15)
            if np.sin(angle) < -0.55:
                continue
            reach = w.uniform(1.15, 1.45) * M
            points = [center + lean @ np.array([np.cos(angle) * r, np.sin(angle) * r, face + 0.03 * M - max(0.0, r - 0.95 * M) * 0.9])
                      for r in np.linspace(0.12 * M, reach, 4)]
            points = [q + np.array([w.uniform(-0.04, 0.04) * M, w.uniform(-0.04, 0.04) * M, 0.0]) for q in points]
            spokes.append((points, index % 3 == 0))
        rootlets = []
        for points, _ in spokes:
            tip = points[-1]
            if tip[1] > 0.5 * M:
                rootlets.append((tip, tip + np.array([w.uniform(-0.05, 0.05) * M, -w.uniform(0.3, 0.6) * M, 0.0])))
        for x in (-0.55, -0.15, 0.3, 0.6):
            edge = center + lean @ np.array([x * M, -0.75 * M, face])
            rootlets.append((edge, edge + np.array([0.0, -w.uniform(0.12, 0.22) * M, 0.05 * M])))
        stones = [center + lean @ np.array([x * M, y * M, face]) for x, y in ((-0.5, -0.35), (0.42, 0.25), (-0.15, 0.55), (0.25, -0.5))]
        earth_dark = noise_mask(0.28 * M, seed + 1, 0.4)
        moss_top = lambda p: 0.72 * M - plate_local(p)[:, 1] + noise_mask(0.2 * M, seed + 2, 0.5)(p)
        trunk_start = center + np.array([0.15 * M, -0.5 * M, -0.3 * M])
        trunk_end = np.array([1.5 * M, 0.38 * M, -2.4 * M])
        trunk = lambda p: capsule(p, trunk_start, trunk_end, 0.46 * M, 0.38 * M)
        pit = np.array([0.0, 0.0, 0.75 * M])
        pool = lambda p: np.maximum(ellipsoid(p, pit, (0.95 * M, 0.2 * M, 0.42 * M)), p[:, 1] - 0.015 * M)
        rim = lambda p: ellipsoid(p, pit, (0.95 * M, 0.3 * M, 0.42 * M))
        return [
            Part(plate, EARTH),
            Part(painted(plate, earth_dark), EARTH_DARK),
            Part(painted(plate, moss_top, 0.03 * M), MOSS_M),
            Part(lambda p: _union(*(_branch_chain(s_, 0.15 * M, 0.07 * M)(p) for s_, thick in spokes if thick)), ROOT_LIGHT),
            Part(lambda p: _union(*(_branch_chain(s_, 0.1 * M, 0.04 * M)(p) for s_, thick in spokes if not thick)), ROOT_M),
            Part(lambda p: _union(*(capsule(p, a_, b_, 0.032 * M, 0.012 * M) for a_, b_ in rootlets)), RADICLE),
            Part(lambda p: _union(*(ellipsoid(p, c, (0.13 * M, 0.1 * M, 0.08 * M)) for c in stones)), STONE_M),
            Part(trunk, WOOD),
            Part(_grooves(trunk, seed + 3, axis=2), GROOVE),
            Part(pool, WATER),
            Part(lambda p: np.maximum(np.abs(p[:, 1] - 0.015 * M) - 0.015 * M, np.maximum(-rim(p), rim(p) - 0.07 * M)), RIPPLE),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(130, 120), footprint=box_footprint(1.0 * M, 0.3 * M))


def bound_tree(stem: str, seed: int) -> PropModel:
    """
    Arbre lié : un tronc mort ceint de chaînes, haubané par trois chaînes jusqu'à des pieux de fer, comme pour
    l'empêcher de partir ; des rubans délavés et un os pendu aux branches, offrandes ou mises en garde.
    """
    WOOD, GROOVE, WET, CHAIN, STAKE, CLOTH, CLOTH_LIGHT, BONE_M, GAP = range(9)
    materials = [make_material("bleached", BLEACHED), make_material("bark_groove", BARK_GREY, contrast=0.8),
                 make_material("wet_wood", WET_WOOD, contrast=0.8), make_material("chain", "#9A6242"),
                 make_material("stake", IRON), make_material("cloth", CLOTH_RED, contrast=0.8),
                 make_material("cloth_light", PETAL, contrast=0.6), make_material("bone", BONE, contrast=0.7),
                 make_material("chain_gap", IRON, contrast=0.6)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        trunk = _gnarled(w, np.zeros(3), np.array([0.0, 1.0, 0.0]), 3.5 * M, 5, wobble=0.07)
        limbs = [_gnarled(w, trunk[4], np.array([s, 0.9, 0.25]), 1.1 * M, 3) for s in (-1.0, 0.8)]
        twigs = [_gnarled(w, l[-1], np.array([w.uniform(-1, 1), 0.8, w.uniform(-1, 1)]), 0.45 * M, 2) for l in limbs for _ in range(2)]
        roots = lambda p: _union(*(capsule(p, (0, 0.5 * M, 0), (np.sin(a) * 0.75 * M, -0.03 * M, np.cos(a) * 0.75 * M), 0.17 * M, 0.06 * M)
                                   for a in np.linspace(0.3, 6.0, 5)))
        fibre = _branch_chain(trunk, 0.3 * M, 0.18 * M)
        wood = lambda p: _union(fibre(p), roots(p), *(_branch_chain(l, 0.13 * M, 0.06 * M)(p) for l in limbs))
        # Ceintures : trois tours de chaîne serrés autour du fût, un peu de travers. À l'écran, une chaîne se lit
        # comme un gros fil de fer rouillé coupé de maillons sombres : les maillons dessinés un à un faisaient du bruit.
        belts = []
        for height in (0.8, 1.5, 2.2):
            axis = trunk[0] + (trunk[-1] - trunk[0]) * (height * M / np.linalg.norm(trunk[-1] - trunk[0]))
            radius = (0.3 - (0.12 * height / 3.5)) * M + 0.035 * M
            belts.append((axis, radius, rotation_z(w.uniform(-0.2, 0.2)) @ rotation_x(w.uniform(-0.15, 0.15))))
        stakes = [np.array([np.sin(a) * 1.45 * M, 0.0, np.cos(a) * 1.45 * M]) for a in (0.5, 2.6, 4.4)]
        guys = [_sag(np.array([np.sin(a) * 0.3 * M, 1.25 * M, np.cos(a) * 0.3 * M]), s + np.array([0.0, 0.28 * M, 0.0]), 0.18 * M, 5)
                for a, s in zip((0.5, 2.6, 4.4), stakes)]

        def chains(p: np.ndarray) -> np.ndarray:
            rings = []
            for center, radius, tilt in belts:
                q = (p - center) @ tilt
                rings.append(np.hypot(np.hypot(q[:, 0], q[:, 2]) - radius, q[:, 1]) - 0.05 * M)
            lines = [capsule(p, a, b, 0.042 * M) for points in guys for a, b in zip(points, points[1:])]
            return _union(*rings, *lines)

        gaps = lambda p: np.abs(np.mod(p[:, 0] + p[:, 1] * 0.7 + p[:, 2], 0.15 * M) - 0.075 * M) - 0.025 * M
        ribbons = []
        for limb in limbs:
            top = limb[2]
            for k in range(2):
                bottom = top + np.array([w.uniform(-0.15, 0.15) * M, -w.uniform(0.45, 0.7) * M, w.uniform(0.0, 0.08) * M])
                ribbons.append((top + np.array([k * 0.07 * M, 0.0, 0.0]), bottom, k))
        bone_top = limbs[1][1]
        bone = bone_top + np.array([0.0, -0.45 * M, 0.0])
        return [
            Part(wood, WOOD),
            Part(_grooves(wood, seed + 1), GROOVE),
            Part(_wet_foot(wood, seed + 2, 0.35 * M), WET),
            Part(lambda p: _union(*(_branch_chain(t, 0.05 * M, 0.025 * M)(p) for t in twigs)), WOOD),
            Part(chains, CHAIN),
            Part(painted(chains, gaps, 0.01 * M), GAP),
            Part(lambda p: _union(*(np.minimum(capsule(p, s + np.array([0.0, -0.1 * M, 0.0]), s + np.array([0.0, 0.32 * M, 0.0]), 0.05 * M, 0.04 * M),
                                               np.abs(cylinder(p, s + np.array([0.0, 0.3 * M, 0.0]), 0.07 * M, 0.02 * M)) - 0.015 * M) for s in stakes)), STAKE),
            Part(lambda p: _union(*(rounded_box(p, (a + b) / 2, (0.06 * M, float(np.linalg.norm(b - a)) / 2, 0.012 * M), 0.01 * M,
                                                _align_y(b - a)) for a, b, k in ribbons if k == 0)), CLOTH),
            Part(lambda p: _union(*(rounded_box(p, (a + b) / 2, (0.05 * M, float(np.linalg.norm(b - a)) / 2, 0.012 * M), 0.01 * M,
                                                _align_y(b - a)) for a, b, k in ribbons if k == 1)), CLOTH_LIGHT),
            Part(lambda p: _union(capsule(p, bone_top, bone + np.array([0.0, 0.15 * M, 0.0]), 0.012 * M),
                                  capsule(p, bone + np.array([-0.12 * M, 0.0, 0.0]), bone + np.array([0.12 * M, 0.05 * M, 0.0]), 0.04 * M),
                                  sphere(p, bone + np.array([-0.13 * M, 0.0, 0.0]), 0.055 * M),
                                  sphere(p, bone + np.array([0.13 * M, 0.05 * M, 0.0]), 0.055 * M)), BONE_M),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(130, 170), footprint=box_footprint(0.36 * M, 0.36 * M))


# ---------------------------------------------------------------------------------------------------------------------
# Végétation basse
# ---------------------------------------------------------------------------------------------------------------------

def vine_curtain(stem: str, seed: int) -> PropModel:
    """Rideau de lianes : une branche morte tombée entre deux chicots, d'où pend un rideau de lianes feuillues."""
    WOOD, GROOVE, WET, VINE, LEAF, LEAF_TOP, FLOWER = range(7)
    materials = [make_material("bleached", BLEACHED), make_material("bark_groove", BARK_GREY, contrast=0.8),
                 make_material("wet_wood", WET_WOOD, contrast=0.8), make_material("vine", WOOD_DARK, contrast=0.8),
                 make_material("leaf", MOSS_WET), make_material("leaf_top", MOSS_LIGHT, contrast=0.9),
                 make_material("flower", LILAC, contrast=0.7)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        left_post = _gnarled(w, np.array([-1.2 * M, 0.0, 0.0]), np.array([0.05, 1.0, 0.0]), 2.5 * M, 3, wobble=0.1)
        right_post = _gnarled(w, np.array([1.25 * M, 0.0, 0.15 * M]), np.array([-0.08, 1.0, 0.0]), 2.1 * M, 3, wobble=0.1)
        beam = _sag(left_post[-1] + np.array([0.1 * M, -0.1 * M, 0.0]), right_post[-1] + np.array([0.0, -0.05 * M, 0.0]), 0.25 * M, 6)
        vines = []
        for index, t in enumerate(np.linspace(0.05, 0.95, 13)):
            k = min(len(beam) - 2, int(t * (len(beam) - 1)))
            top = beam[k] + (beam[k + 1] - beam[k]) * (t * (len(beam) - 1) - k)
            length = w.uniform(0.9, top[1] / M - 0.05) * M
            bottom = top + np.array([w.uniform(-0.12, 0.12) * M, -length, w.uniform(-0.04, 0.12) * M])
            vines.append((top, bottom))
        leaves = []
        for a, b in vines:
            for k in range(3):
                t = w.uniform(0.1, 0.95)
                leaves.append((a + (b - a) * t + np.array([w.uniform(-0.06, 0.06) * M, 0.0, 0.05 * M]), w.uniform(0.09, 0.14) * M, k % 3 == 0))
        # Masse feuillue sur la branche, d'où partent les lianes.
        crown = [beam[i] + np.array([w.uniform(-0.1, 0.1) * M, 0.08 * M, 0.04 * M]) for i in range(1, len(beam) - 1, 2)]
        flowers = [leaves[i][0] + np.array([0.0, 0.0, 0.07 * M]) for i in range(3, len(leaves), 11)]
        posts = lambda p: _union(_branch_chain(left_post, 0.16 * M, 0.11 * M)(p), _branch_chain(right_post, 0.15 * M, 0.1 * M)(p))
        timber = lambda p: _union(posts(p), _branch_chain(beam, 0.1 * M, 0.08 * M)(p))
        return [
            Part(timber, WOOD),
            Part(_grooves(timber, seed + 1, period=0.1 * M), GROOVE),
            Part(_wet_foot(posts, seed + 2, 0.3 * M), WET),
            Part(lambda p: _union(*(capsule(p, a, b, 0.036 * M, 0.026 * M) for a, b in vines)), VINE),
            Part(lambda p: _union(*(ellipsoid(p, c, (s, s * 0.7, s * 0.55)) for c, s, top in leaves if not top)), LEAF),
            Part(lambda p: _union(*(ellipsoid(p, c, (s, s * 0.7, s * 0.55)) for c, s, top in leaves if top)), LEAF_TOP),
            Part(_clumps(crown, [(0.2 * M, 0.12 * M, 0.16 * M)] * len(crown), 0.03 * M, 0.25 * M), LEAF_TOP),
            Part(lambda p: _union(*(sphere(p, f, 0.06 * M) for f in flowers)), FLOWER),
        ]

    return PropModel(stem, parts, materials, float(np.radians(15.0)), canvas=(130, 110))


def hanging_moss(stem: str, seed: int) -> PropModel:
    """Chicot penché drapé de barbes de lichen : une silhouette grise qu'on prend de loin pour quelqu'un."""
    WOOD, GROOVE, WET, BEARD_BACK, BEARD_M = range(5)
    materials = [make_material("bleached", BLEACHED), make_material("bark_groove", BARK_GREY, contrast=0.8),
                 make_material("wet_wood", WET_WOOD, contrast=0.8), make_material("beard_back", MIST, contrast=0.55),
                 make_material("beard", BEARD, contrast=0.55)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        trunk = _gnarled(w, np.zeros(3), np.array([0.28, 1.0, 0.0]), 2.4 * M, 4, wobble=0.1)
        arms = [_gnarled(w, trunk[2], np.array([-1.0, 0.5, 0.15]), 0.95 * M, 3, wobble=0.2),
                _gnarled(w, trunk[3], np.array([1.0, 0.35, -0.1]), 0.7 * M, 2, wobble=0.2)]
        wood = lambda p: _union(_branch_chain(trunk, 0.15 * M, 0.08 * M)(p), *(_branch_chain(a, 0.08 * M, 0.035 * M)(p) for a in arms))
        front = [trunk[3], trunk[4]] + [arms[0][i] for i in (1, 2, 3)] + [arms[1][i] for i in (1, 2)]
        back = [q + np.array([0.04 * M, 0.02 * M, -0.12 * M]) for q in front[::2]]
        return [
            Part(wood, WOOD),
            Part(_grooves(wood, seed + 1, period=0.09 * M), GROOVE),
            Part(_wet_foot(wood, seed + 2, 0.3 * M), WET),
            Part(_beards(w, back, (0.7, 1.3), strands=3, spread=0.1 * M, radius=0.05 * M), BEARD_BACK),
            Part(_beards(w, front, (0.4, 1.1), strands=3, spread=0.1 * M, radius=0.045 * M), BEARD_M),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(90, 100))


def reeds(stem: str, seed: int) -> PropModel:
    """
    Massettes : cinq hampes aux épis bruns, bien séparées, et des feuilles en lame qui s'arquent vers l'extérieur.
    Peu d'éléments, espacés d'au moins deux pixels : à 25 px de haut, une touffe trop dense devient un pavé vert.
    """
    STEM, LEAF, LEAF_TOP, HEAD, SPIKE = range(5)
    materials = [make_material("stem", MOSS_WET, contrast=0.6), make_material("leaf", MOSS_LIGHT, contrast=0.6),
                 make_material("leaf_top", LICHEN, contrast=0.6), make_material("cattail", CATTAIL, contrast=0.8),
                 make_material("spike", BLEACHED, contrast=0.5)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        stems = []
        for x, z, h in ((-0.3, 0.05, 1.2), (-0.1, -0.12, 1.5), (0.08, 0.1, 1.35), (0.27, -0.05, 1.05), (0.42, 0.12, 1.25)):
            base = np.array([x * M, 0.0, z * M])
            stems.append((base, base + np.array([x * 0.25 * M + w.uniform(-0.03, 0.03) * M, h * M, 0.0])))
        blades = []
        for x, z, h, lean, top in ((-0.2, 0.15, 0.95, -0.55, False), (-0.05, 0.2, 0.75, -0.3, True), (0.15, 0.18, 0.85, 0.45, False),
                                   (0.3, 0.05, 0.7, 0.6, True), (-0.35, -0.1, 0.65, -0.65, True), (0.0, -0.15, 1.05, 0.2, False),
                                   (0.45, -0.1, 0.6, 0.75, False)):
            base = np.array([x * M, 0.0, z * M])
            mid = base + np.array([lean * 0.25 * M, h * 0.62 * M, 0.0])
            tip = base + np.array([lean * 0.8 * M, h * M, w.uniform(-0.05, 0.05) * M])
            blades.append((base, mid, tip, top))
        heads = stems[:4]

        def blade_set(top: bool):
            return lambda p: _union(*(np.minimum(capsule(p, a, m, 0.045 * M, 0.035 * M), capsule(p, m, t, 0.035 * M, 0.006 * M))
                                      for a, m, t, kind in blades if kind == top))

        return [
            Part(lambda p: _union(*(capsule(p, a, b, 0.026 * M, 0.02 * M) for a, b in stems)), STEM),
            Part(blade_set(False), LEAF),
            Part(blade_set(True), LEAF_TOP),
            Part(lambda p: _union(*(capsule(p, b - np.array([0.0, 0.36 * M, 0.0]), b - np.array([0.0, 0.1 * M, 0.0]), 0.07 * M)
                                    for _, b in heads)), HEAD),
            Part(lambda p: _union(*(capsule(p, b - np.array([0.0, 0.06 * M, 0.0]), b + np.array([0.0, 0.08 * M, 0.0]), 0.02 * M, 0.01 * M)
                                    for _, b in heads)), SPIKE),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(72, 72))


def lily_pads(stem: str, seed: int) -> PropModel:
    """Nénuphars : sept feuilles rondes entaillées, espacées sur l'eau, une fleur pâle au cœur doré et un bouton mauve."""
    PAD, PAD_LIGHT, PETAL_M, BUD, HEART = range(5)
    materials = [make_material("pad", MOSS_WET, contrast=0.7), make_material("pad_light", MOSS_LIGHT, contrast=0.7),
                 make_material("petal", PETAL, contrast=0.6), make_material("bud", LILAC, contrast=0.6),
                 make_emissive("heart", POLLEN)]
    # Feuilles disjointes (centre, rayon, entaille, ton) : un écart d'au moins un pixel garde leurs contours.
    pads = [((-0.78, -0.02), 0.24, 0.3, 0), ((-0.3, -0.3), 0.22, 2.2, 1), ((0.22, -0.32), 0.26, 4.0, 0),
            ((0.0, 0.18), 0.3, 1.2, 1), ((-0.55, 0.38), 0.18, 5.1, 1), ((0.66, 0.1), 0.24, 3.0, 1), ((0.45, 0.48), 0.16, 0.9, 0)]

    def pad(p: np.ndarray, c, r: float, notch: float) -> np.ndarray:
        center = np.array([c[0] * M, 0.02 * M, c[1] * 0.85 * M])
        disc = cylinder(p, center, r * M, 0.014 * M, 0.006 * M)
        local = (p - center) @ rotation_y(notch)
        wedge = np.maximum(np.abs(local[:, 0]) - 0.03 * M - local[:, 2] * 0.45, -local[:, 2])
        return np.maximum(disc, -wedge)

    def parts() -> list[Part]:
        bloom = np.array([0.0, 0.06 * M, 0.14 * M])
        petals = [(bloom + np.array([np.cos(a) * 0.11 * M, 0.02 * M + (k % 2) * 0.035 * M, np.sin(a) * 0.11 * M]), a, k)
                  for k, a in enumerate(np.linspace(0, 2 * np.pi, 8, endpoint=False))]
        # Bouton couché sur sa feuille : dressé, il ferait passer le décor au-dessus de la hauteur d'un décalque au sol.
        bud = np.array([-0.55 * M, 0.06 * M, 0.32 * M])
        return [
            Part(lambda p: _union(*(pad(p, c, r, n) for c, r, n, k in pads if k == 0)), PAD),
            Part(lambda p: _union(*(pad(p, c, r, n) for c, r, n, k in pads if k == 1)), PAD_LIGHT),
            Part(lambda p: _union(*(ellipsoid(p, c, (0.12 * M, 0.05 * M, 0.06 * M), rotation_y(-a) @ rotation_z(0.45 + 0.3 * (k % 2)))
                                    for c, a, k in petals)), PETAL_M),
            Part(lambda p: ellipsoid(p, bud, (0.11 * M, 0.06 * M, 0.07 * M), rotation_z(0.5)), BUD),
            Part(lambda p: sphere(p, bloom + np.array([0.0, 0.06 * M, 0.0]), 0.05 * M), HEART),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(90, 44))


def toxic_mushrooms(stem: str, seed: int) -> PropModel:
    """Champignons vénéneux : quatre chapeaux violets de tailles franches, piqués de spores lumineuses, sur une motte."""
    STALK, CAP, RIM, DOT, PEAT_M = range(5)
    materials = [make_material("stalk", SILVER, contrast=0.6), make_material("cap", VIOLET, contrast=1.1),
                 make_material("rim", LILAC, contrast=0.7), make_emissive("spore", SPORE),
                 make_material("peat", PEAT, contrast=0.7)]
    # (x, z, hauteur du chapeau, rayon, inclinaison) : un grand, deux moyens, un petit, sans chevauchement à l'écran.
    caps = ((-0.05, 0.0, 0.62, 0.27, 0.0), (-0.42, 0.12, 0.38, 0.18, 0.35), (0.36, 0.1, 0.42, 0.19, -0.3), (0.12, 0.32, 0.2, 0.12, -0.15))

    def parts() -> list[Part]:
        specs = [(np.array([x * M, 0.0, z * M]), np.array([x * M, h * M, z * M]), r * M, rotation_z(t)) for x, z, h, r, t in caps]
        cap_shape = lambda p: _union(*(np.maximum(ellipsoid(p, top, (r, r * 0.68, r), tilt), -((p - top) @ tilt)[:, 1] - r * 0.12)
                                       for _, top, r, tilt in specs))
        dots = []
        for _, top, r, tilt in specs:
            for a in ((-0.9, 0.5), (0.6, 0.2), (0.1, -0.6))[: 3 if r > 0.15 * M else 1]:
                dots.append(top + tilt @ np.array([a[0] * r * 0.6, r * 0.45, a[1] * r * 0.6 + r * 0.2]))
        return [
            Part(lambda p: _union(*(capsule(p, b, b + (t - b) * 0.95, r * 0.2, r * 0.16) for b, t, r, _ in specs)), STALK),
            Part(cap_shape, CAP),
            Part(lambda p: _union(*(cylinder(p, top - tilt @ np.array([0.0, r * 0.1, 0.0]), r * 0.97, r * 0.06, 0.0, tilt)
                                    for _, top, r, tilt in specs)), RIM),
            Part(lambda p: _union(*(sphere(p, d, 0.042 * M) for d in dots)), DOT),
            Part(lambda p: ellipsoid(p, (0.0, 0.0, 0.1 * M), (0.62 * M, 0.08 * M, 0.38 * M)), PEAT_M),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(60, 52))


def spore_patch(stem: str, seed: int) -> PropModel:
    """Tapis de mousse d'où montent des vesses-de-loup mûres et trois hampes à spores qui luisent, quelques spores en l'air."""
    MOSS_M, MOSS_TOP, PUFF, STALK, GLOW = range(5)
    materials = [make_material("moss", MOSS_WET, contrast=0.8), make_material("moss_light", MOSS_LIGHT, contrast=0.7),
                 make_material("puff", BONE, contrast=0.8), make_material("stalk", BLEACHED, contrast=0.5),
                 make_emissive("glow", SPORE)]

    def parts() -> list[Part]:
        mat = [(-0.45 * M, 0.02 * M, 0.0), (0.1 * M, 0.02 * M, -0.15 * M), (0.5 * M, 0.02 * M, 0.15 * M), (-0.05 * M, 0.02 * M, 0.3 * M)]
        puffs = [(-0.35, 0.1, 0.15), (0.2, 0.2, 0.12), (0.5, -0.05, 0.1), (-0.05, -0.18, 0.09), (-0.55, -0.15, 0.08)]
        stalks = [(np.array([x * M, 0.0, z * M]), h * M) for x, z, h in ((0.02, 0.05, 0.5), (-0.2, -0.2, 0.36), (0.38, 0.0, 0.42))]
        floating = [np.array([x * M, y * M, 0.0]) for x, y in ((-0.3, 0.75), (0.25, 0.92), (0.6, 0.65))]
        return [
            Part(_clumps(mat, [(0.42 * M, 0.06 * M, 0.3 * M)] * len(mat), 0.02 * M, 0.25 * M), MOSS_M),
            Part(_clumps(mat[1:3], [(0.25 * M, 0.08 * M, 0.18 * M)] * 2, 0.02 * M, 0.25 * M), MOSS_TOP),
            Part(lambda p: _union(*(ellipsoid(p, (x * M, r * 0.7 * M, z * M), (r * M, r * 0.8 * M, r * M)) for x, z, r in puffs)), PUFF),
            Part(lambda p: _union(*(capsule(p, b, b + np.array([0.0, h, 0.0]), 0.024 * M, 0.016 * M) for b, h in stalks)), STALK),
            Part(lambda p: _union(*(sphere(p, b + np.array([0.0, h + 0.05 * M, 0.0]), 0.07 * M) for b, h in stalks),
                                  *(sphere(p, (x * M, r * 1.45 * M, z * M), 0.04 * M) for x, z, r in puffs[:3]),
                                  *(sphere(p, f, 0.032 * M) for f in floating)), GLOW),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(72, 56))


def fallen_log(stem: str, seed: int) -> PropModel:
    """
    Tronc blanchi couché dans l'eau : écorce partie par plaques, bout cassé en échardes, autre bout scié et creux,
    mousse et lichen sur le dessus, deux moignons de branches, polypores sur le flanc.
    """
    WOOD, BARK, GROOVE, LICHEN_M, MOSS_M, CUT, RING, HOLLOW, SPLINTER, SHELF, RIPPLE = range(11)
    materials = [make_material("bleached", BLEACHED), make_material("bark", WOOD_DARK, contrast=0.8),
                 make_material("bark_groove", BARK_GREY, contrast=0.8), make_material("lichen", LICHEN, contrast=0.8),
                 make_material("moss", MOSS_WET), make_material("cut", FUNGUS, contrast=0.6), make_material("ring", WOOD_OLD, contrast=0.6),
                 make_material("hollow", PEAT, contrast=0.5), make_material("splinter", SILVER, contrast=0.6),
                 make_material("shelf", FUNGUS, contrast=0.8), make_material("ripple", WATER_MILK, contrast=0.4)]
    level = 0.1 * M

    def parts() -> list[Part]:
        w = Weathering(seed)
        a, b = np.array([0.0, 0.3 * M, -1.55 * M]), np.array([0.0, 0.27 * M, 1.45 * M])
        log = lambda p: capsule(p, a, b, 0.36 * M, 0.31 * M)
        # Bout scié côté caméra (plan net), bout cassé en échardes à l'arrière.
        sawn = lambda p: np.maximum(log(p), p[:, 2] - 1.5 * M)
        hollowed = lambda p: np.maximum(sawn(p), -capsule(p, b + np.array([0.0, 0.0, -0.8 * M]), b + np.array([0.0, 0.0, 0.3 * M]), 0.18 * M))
        cuts = [(rotation_x(w.uniform(-0.9, -0.5)) @ rotation_y(w.uniform(-0.6, 0.6)), -1.35 * M + w.uniform(0.0, 0.25) * M) for _ in range(3)]
        broken = lambda p: np.maximum(hollowed(p), _union(*(-(p @ c)[:, 2] + h for c, h in cuts)))
        body = _sunk(broken, level)
        splinters = [(np.array([np.sin(t) * 0.24 * M, 0.3 * M + np.cos(t) * 0.24 * M, -1.25 * M]),
                      np.array([np.sin(t) * 0.28 * M, 0.3 * M + np.cos(t) * 0.28 * M, -1.25 * M - w.uniform(0.25, 0.5) * M]))
                     for t in (-1.2, -0.3, 0.6, 1.4)]
        stubs = [((0.0, 0.5 * M, z * M), (s * 0.35 * M, w.uniform(0.85, 1.05) * M, z * M + w.uniform(-0.2, 0.2) * M)) for z, s in ((-0.6, -1), (0.55, 1))]
        bark = both(noise_mask(0.32 * M, seed + 1, 0.42, frame=None), lambda p: p[:, 2] - 1.3 * M)
        # Mousse et lichen sur le dessus seulement, là où la pluie et la lumière tombent.
        moss = both(lambda p: 0.55 * M - p[:, 1], noise_mask(0.4 * M, seed + 2, 0.45))
        lichen = both(lambda p: 0.45 * M - p[:, 1], noise_mask(0.14 * M, seed + 3, 0.18))
        face = lambda p: np.maximum(np.abs(p[:, 2] - 1.5 * M) - 0.02 * M, log(p) - 0.01 * M)
        shelves = [(np.array([0.36 * M, 0.3 * M, z * M]), s * M, np.pi / 2) for z, s in ((-0.2, 0.15), (0.05, 0.11))]
        return [
            Part(body, WOOD),
            Part(painted(body, bark, 0.03 * M), BARK),
            Part(_grooves(body, seed + 4, axis=2), GROOVE),
            Part(painted(body, moss, 0.025 * M), MOSS_M),
            Part(painted(body, lichen), LICHEN_M),
            Part(lambda p: np.maximum(face(p), -capsule(p, b + np.array([0.0, 0.0, -0.8 * M]), b + np.array([0.0, 0.0, 0.3 * M]), 0.21 * M)), CUT),
            Part(lambda p: np.maximum(face(p) - 0.005 * M, bands(0, 0.11 * M, 0.025 * M, frame=lambda q: np.stack(
                [np.hypot(q[:, 0], q[:, 1] - b[1]), q[:, 1], q[:, 2]], axis=1))(p)), RING),
            Part(lambda p: np.maximum(sawn(p) + 0.01 * M, np.abs(capsule(p, b + np.array([0.0, 0.0, -0.8 * M]), b + np.array([0.0, 0.0, 0.3 * M]),
                                                                       0.18 * M)) - 0.03 * M), HOLLOW),
            Part(_sunk(lambda p: _union(*(capsule(p, s, e, 0.07 * M, 0.015 * M) for s, e in splinters)), level), SPLINTER),
            Part(lambda p: _union(*(capsule(p, s, e, 0.12 * M, 0.06 * M) for s, e in stubs)), WOOD),
            Part(_shelves(shelves), SHELF),
            Part(waterline(broken, level, 0.12 * M, seed + 5, 0.35), RIPPLE),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(110, 76), footprint=box_footprint(0.36 * M, 1.5 * M))


def rotten_stump(stem: str, seed: int) -> PropModel:
    """Souche creuse, pourrie de l'intérieur et pleine d'eau noire, couronne déchiquetée, polypores en escalier."""
    WOOD, GROOVE, WET, HOLLOW, WATER, SHELF, MOSS_M, MOSS_TOP, SPLINTER = range(9)
    materials = [make_material("bleached", BLEACHED), make_material("bark_groove", BARK_GREY, contrast=0.8),
                 make_material("wet_wood", WET_WOOD, contrast=0.8), make_material("hollow", PEAT, contrast=0.5),
                 make_material("water", WATER_DARK, contrast=0.4), make_material("shelf", FUNGUS, contrast=0.8),
                 make_material("moss", MOSS_WET), make_material("moss_light", MOSS_LIGHT, contrast=0.9),
                 make_material("splinter", SILVER, contrast=0.6)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        jag = [(rotation_y(w.uniform(0, 2 * np.pi)) @ rotation_x(w.uniform(0.35, 0.7)), w.uniform(0.7, 1.0) * M) for _ in range(5)]
        body = lambda p: cylinder(p, (0, 0.5 * M, 0), 0.52 * M, 0.5 * M, 0.06 * M)
        roots = lambda p: _union(*(capsule(p, (0, 0.35 * M, 0), (np.sin(a) * 0.85 * M, -0.03 * M, np.cos(a) * 0.85 * M), 0.18 * M, 0.06 * M)
                                   for a in np.linspace(0.6, 6.0, 5)))
        bore = lambda p: cylinder(p, (0, 0.8 * M, 0), 0.36 * M, 0.55 * M, 0.04 * M)
        shell = lambda p: np.maximum(np.minimum(np.maximum(body(p), _union(*((p @ r)[:, 1] - h for r, h in jag))), roots(p)), -bore(p))
        shelves = [(np.array([np.sin(a) * 0.52 * M, h * M, np.cos(a) * 0.52 * M]), s * M, a) for a, h, s in
                   ((0.5, 0.35, 0.17), (0.75, 0.55, 0.14), (0.3, 0.7, 0.11), (-1.3, 0.45, 0.12))]
        moss = [(-0.45 * M, 0.05 * M, 0.35 * M), (-0.2 * M, 0.05 * M, 0.55 * M), (0.5 * M, 0.04 * M, -0.3 * M)]
        spikes = [(np.array([np.sin(a) * 0.44 * M, 0.75 * M, np.cos(a) * 0.44 * M]), np.array([np.sin(a) * 0.46 * M, w.uniform(1.0, 1.25) * M, np.cos(a) * 0.46 * M]))
                  for a in (-0.9, 1.9, 3.4)]
        return [
            Part(shell, WOOD),
            Part(_grooves(shell, seed + 1), GROOVE),
            Part(_wet_foot(shell, seed + 2, 0.3 * M), WET),
            Part(lambda p: np.maximum(shell(p) + 0.01 * M, np.abs(bore(p)) - 0.03 * M), HOLLOW),
            Part(lambda p: cylinder(p, (0, 0.55 * M, 0), 0.37 * M, 0.02 * M), WATER),
            Part(_shelves(shelves), SHELF),
            Part(_clumps(moss, [(0.3 * M, 0.1 * M, 0.25 * M)] * 3, 0.02 * M, 0.2 * M), MOSS_M),
            Part(_clumps(moss[:1], [(0.2 * M, 0.12 * M, 0.17 * M)], 0.02 * M, 0.2 * M), MOSS_TOP),
            Part(lambda p: _union(*(capsule(p, a, b, 0.06 * M, 0.015 * M) for a, b in spikes)), SPLINTER),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(72, 64), footprint=box_footprint(0.5 * M, 0.5 * M))


def sunken_trunk(stem: str, seed: int) -> PropModel:
    """Énorme tronc à moitié englouti : une échine de bois mort, deux branches cassées dressées, la souche et ses racines au bout."""
    WOOD, GROOVE, BARK, MOSS_M, MOSS_TOP, ROOT_M, ROOT_LIGHT, RIPPLE = range(8)
    materials = [make_material("bleached", BLEACHED), make_material("bark_groove", BARK_GREY, contrast=0.8),
                 make_material("bark", WOOD_DARK, contrast=0.8), make_material("moss", MOSS_WET),
                 make_material("moss_light", MOSS_LIGHT, contrast=0.9), make_material("root", WOOD_OLD),
                 make_material("root_light", BLEACHED), make_material("ripple", WATER_MILK, contrast=0.4)]
    level = 0.12 * M

    def parts() -> list[Part]:
        w = Weathering(seed)
        spine = lambda p: capsule(p, (0, 0.18 * M, -2.3 * M), (0, 0.1 * M, 2.3 * M), 0.62 * M, 0.45 * M)
        stubs = [((0.0, 0.5 * M, z * M), (w.uniform(-0.4, 0.4) * M, w.uniform(1.2, 1.7) * M, z * M + w.uniform(-0.3, 0.3) * M)) for z in (-1.0, 0.9)]
        twigs = [(e, e + np.array([w.uniform(-0.4, 0.4) * M, w.uniform(0.2, 0.45) * M, w.uniform(-0.3, 0.3) * M])) for _, e in stubs for _ in range(2)]
        bowl = np.array([0.0, 0.35 * M, -2.45 * M])
        roots = []
        for index in range(9):
            angle = index * 2 * np.pi / 9 + w.uniform(-0.15, 0.15)
            tip = bowl + np.array([np.cos(angle) * w.uniform(0.9, 1.2) * M, np.sin(angle) * w.uniform(0.8, 1.1) * M, -w.uniform(0.1, 0.4) * M])
            roots.append(_gnarled(w, bowl, tip - bowl, float(np.linalg.norm(tip - bowl)), 3, wobble=0.2))
        root_shape = lambda p: _union(*(_branch_chain(r, 0.14 * M, 0.05 * M)(p) for r in roots))
        wood = lambda p: _union(spine(p), *(capsule(p, a, b, 0.15 * M, 0.07 * M) for a, b in stubs))
        body = _sunk(wood, level)
        bark = both(noise_mask(0.35 * M, seed + 1, 0.16), lambda p: -p[:, 2] - 1.4 * M)
        # Mousse sur l'échine seulement : à 0,12 m sous le dessus du tronc, qui s'abaisse vers l'avant.
        ridge = lambda p: (0.675 * M - p[:, 2] / (4.6 * M) * 0.25 * M - 0.1 * M) - p[:, 1]
        moss = both(ridge, noise_mask(0.45 * M, seed + 2, 0.32))
        moss_top = both(moss, noise_mask(0.2 * M, seed + 3, 0.3))
        return [
            Part(body, WOOD),
            Part(_grooves(body, seed + 4, axis=2, period=0.16 * M), GROOVE),
            Part(painted(body, bark, 0.03 * M), BARK),
            Part(painted(body, moss, 0.025 * M), MOSS_M),
            Part(painted(body, moss_top, 0.035 * M), MOSS_TOP),
            Part(lambda p: _union(*(capsule(p, a, b, 0.06 * M, 0.025 * M) for a, b in twigs)), WOOD),
            Part(_sunk(lambda p: _union(*(_branch_chain(r, 0.14 * M, 0.05 * M)(p) for i, r in enumerate(roots) if i % 3)), level), ROOT_M),
            Part(_sunk(lambda p: _union(*(_branch_chain(r, 0.15 * M, 0.06 * M)(p) for i, r in enumerate(roots) if not i % 3)), level), ROOT_LIGHT),
            Part(waterline(lambda p: np.minimum(wood(p), root_shape(p)), level, 0.14 * M, seed + 5, 0.3), RIPPLE),
        ]

    return PropModel(stem, parts, materials, AXIS_Y_YAW, canvas=(150, 100), footprint=box_footprint(0.55 * M, 2.3 * M))


# ---------------------------------------------------------------------------------------------------------------------
# Vestiges noyés
# ---------------------------------------------------------------------------------------------------------------------

def _planks(count: int, span: tuple[float, float], w: Weathering, missing: tuple[int, ...] = (), jitter: float = 0.04):
    """Planches jointives le long de x (chacune un peu de travers), avec trouées : liste (centre, rotation, ton)."""
    result = []
    for index, x in enumerate(np.linspace(span[0], span[1], count)):
        if index in missing:
            continue
        tilt = rotation_z(w.uniform(-jitter, jitter)) @ rotation_y(w.uniform(-jitter * 1.5, jitter * 1.5))
        result.append((np.array([x, w.uniform(-0.015, 0.015) * M, w.uniform(-0.04, 0.04) * M]), tilt, index % 2))
    return result


def broken_walkway(stem: str, seed: int) -> PropModel:
    """Caillebotis sur pilotis : planches disjointes sur deux longerons, une travée affaissée dans l'eau, cordes aux pieux."""
    PLANK, PLANK_LIGHT, GRAIN, BEAM, POST, ROPE, RIPPLE = range(7)
    materials = [make_material("plank", WOOD_OLD), make_material("plank_light", BLEACHED),
                 make_material("grain", WOOD_DARK, contrast=0.7), make_material("beam", WOOD_DARK, contrast=0.8),
                 make_material("post", WET_WOOD, contrast=0.8), make_material("rope", FUNGUS, contrast=0.6),
                 make_material("ripple", WATER_MILK, contrast=0.4)]
    deck = 0.42 * M

    def parts() -> list[Part]:
        w = Weathering(seed)
        planks = _planks(12, (-1.65 * M, 1.65 * M), w, missing=(7, 8))
        fallen = [((0.62 * M, 0.12 * M, 0.05 * M), rotation_z(0.55) @ rotation_y(0.15)), ((0.88 * M, 0.05 * M, -0.12 * M), rotation_z(-0.35) @ rotation_y(0.4))]
        rails = [((-0.45 * M, deck - 0.06 * M, z * M), (0.34 * M, deck - 0.08 * M, z * M)) for z in (-0.32, 0.32)]
        broken_rail = [((0.62 * M, deck - 0.1 * M, z * M), (1.1 * M, 0.0, z * M)) for z in (-0.32, 0.32)]
        far_rails = [((1.15 * M, deck - 0.06 * M, z * M), (1.75 * M, deck - 0.06 * M, z * M)) for z in (-0.32, 0.32)]
        posts = [(x * M, z * M, h * M) for x, h in ((-1.6, 0.62), (-0.4, 0.55), (1.2, 0.6), (1.7, 0.75)) for z in (-0.36, 0.36)]
        plank_shape = lambda kind: (lambda p: _union(*(rounded_box(p, c + np.array([0.0, deck, 0.0]), (0.115 * M, 0.03 * M, 0.47 * M), 0.012 * M, r)
                                                      for c, r, k in planks if k == kind)))
        everything = lambda p: _union(plank_shape(0)(p), plank_shape(1)(p))
        grain = noise_mask(0.07 * M, seed + 1, 0.22, (1.0, 1.0, 7.0))
        post_shape = lambda p: _union(*(cylinder(p, (x, h * 0.5 - 0.1 * M, z), 0.075 * M, h * 0.5 + 0.1 * M, 0.025 * M) for x, z, h in posts))
        return [
            Part(plank_shape(0), PLANK),
            Part(plank_shape(1), PLANK_LIGHT),
            Part(painted(everything, grain), GRAIN),
            Part(lambda p: _union(*(rounded_box(p, c, (0.115 * M, 0.03 * M, 0.47 * M), 0.012 * M, r) for c, r in fallen)), PLANK),
            Part(lambda p: _union(*(capsule(p, a, b, 0.05 * M) for a, b in rails + broken_rail + far_rails)), BEAM),
            Part(_sunk(post_shape, 0.0), POST),
            Part(lambda p: _union(*(np.abs(cylinder(p, (x, h - 0.12 * M, z), 0.085 * M, 0.035 * M)) - 0.015 * M for x, z, h in posts[::3])), ROPE),
            Part(waterline(post_shape, 0.02 * M, 0.1 * M), RIPPLE),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(140, 72))


def collapsed_pontoon(stem: str, seed: int) -> PropModel:
    """Ponton effondré : un plancher carré basculé dans l'eau, pieux arrachés à des hauteurs inégales, bidon d'amarrage, cordage."""
    PLANK, PLANK_LIGHT, GRAIN, POST, DRUM, DRUM_RUST, ROPE, RIPPLE = range(8)
    materials = [make_material("plank", WOOD_OLD), make_material("plank_light", BLEACHED),
                 make_material("grain", WOOD_DARK, contrast=0.7), make_material("post", WET_WOOD, contrast=0.8),
                 make_material("drum", PAINT_FADED), make_material("rust", RUST_ORANGE, contrast=0.8),
                 make_material("rope", FUNGUS, contrast=0.6), make_material("ripple", WATER_MILK, contrast=0.4)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        tilt = rotation_x(0.3) @ rotation_z(-0.14)
        origin = np.array([0.0, 0.32 * M, 0.0])
        local = lambda p: (p - origin) @ tilt
        boards = [(x * M, k) for k, x in enumerate(np.linspace(-1.05, 1.05, 9))]

        def deck_of(kind: int):
            return lambda p: _union(*(rounded_box(local(p), (x, 0.0, w_z), (0.115 * M, 0.04 * M, 1.12 * M), 0.012 * M)
                                      for (x, k), w_z in zip(boards, (0.0, 0.04 * M, -0.03 * M, 0.02 * M, 0.0, -0.05 * M, 0.03 * M, 0.0, 0.05 * M))
                                      if k % 2 == kind and k != 6))

        deck = lambda p: np.minimum(deck_of(0)(p), deck_of(1)(p))
        joists = lambda p: _union(*(rounded_box(local(p), (0.0, -0.08 * M, z * M), (1.12 * M, 0.05 * M, 0.06 * M), 0.01 * M) for z in (-0.85, 0.85)))
        posts = [((x * M, 0.0, z * M), h * M, rotation_z(w.uniform(-0.25, 0.25)) @ rotation_x(w.uniform(-0.15, 0.15)))
                 for x, z, h in ((-1.15, -1.05, 1.25), (1.15, -1.15, 0.9), (-1.1, 1.05, 0.6), (1.2, 1.0, 0.35))]
        post_shape = lambda p: _union(*(cylinder(p @ r, np.array(c) + np.array([0.0, h * 0.5, 0.0]), 0.095 * M, h * 0.5 + 0.1 * M, 0.03 * M) for c, h, r in posts))
        drum_at = np.array([1.55 * M, 0.25 * M, 0.75 * M])
        drum_rot = rotation_x(np.pi / 2) @ rotation_y(0.6)
        drum = lambda p: cylinder(p, drum_at, 0.3 * M, 0.42 * M, 0.05 * M, drum_rot)
        rust = noise_mask(0.22 * M, seed + 1, 0.3)
        rope_path = [np.array([-1.15 * M, 1.05 * M, -1.05 * M]), np.array([-0.5 * M, 0.45 * M, -0.6 * M]), np.array([0.6 * M, 0.35 * M, 0.2 * M]),
                     drum_at + np.array([-0.2 * M, 0.25 * M, 0.0])]
        return [
            Part(_sunk(deck_of(0), 0.04 * M), PLANK),
            Part(_sunk(deck_of(1), 0.04 * M), PLANK_LIGHT),
            Part(painted(_sunk(deck, 0.04 * M), noise_mask(0.07 * M, seed + 2, 0.22, (1.0, 1.0, 7.0), frame=local)), GRAIN),
            Part(_sunk(joists, 0.04 * M), POST),
            Part(_sunk(post_shape, 0.0), POST),
            Part(_sunk(drum, 0.05 * M), DRUM),
            Part(painted(_sunk(drum, 0.05 * M), rust), DRUM_RUST),
            Part(lambda p: _union(*(capsule(p, a, b, 0.03 * M) for a, b in zip(rope_path, rope_path[1:])),
                                  np.abs(cylinder(p, (-1.15 * M, 1.05 * M, -1.05 * M), 0.11 * M, 0.04 * M)) - 0.015 * M), ROPE),
            Part(waterline(lambda p: _union(deck(p), post_shape(p), drum(p)), 0.04 * M, 0.12 * M, seed + 3, 0.3), RIPPLE),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(150, 100), footprint=box_footprint(1.15 * M, 1.1 * M))


def sunken_boat(stem: str, seed: int) -> PropModel:
    """Barque noyée : coque à clins penchée, la proue hors de l'eau, banc de nage, aviron resté en travers, eau noire au fond."""
    HULL, STRAKE, RIM, SEAT, WATER, GLINT, OAR, RIPPLE = range(8)
    materials = [make_material("hull", WOOD_OLD), make_material("strake", WOOD_DARK, contrast=0.7),
                 make_material("rim", BLEACHED), make_material("seat", BLEACHED),
                 make_material("water", WATER_DARK, contrast=0.5), make_material("glint", SILVER, contrast=0.4),
                 make_material("oar", FUNGUS, contrast=0.8), make_material("ripple", WATER_MILK, contrast=0.4)]
    tilt = rotation_x(-0.2) @ rotation_z(0.16)
    origin = np.array([0.0, 0.22 * M, 0.0])
    local = lambda p: (p - origin) @ tilt

    def outer(p: np.ndarray) -> np.ndarray:
        q = local(p)
        # Coque effilée : l'ellipse se resserre vers la proue (+z) et garde un tableau plat à la poupe.
        taper = 1.0 - np.clip(q[:, 2] / (1.7 * M), 0.0, 1.0) ** 2 * 0.55
        scaled = np.stack([q[:, 0] / np.maximum(taper, 0.3), q[:, 1], q[:, 2]], axis=1)
        return np.maximum(ellipsoid(scaled, (0, 0, 0), (0.7 * M, 0.48 * M, 1.7 * M)) * np.minimum(taper, 1.0), -q[:, 2] - 1.45 * M)

    def hull(p: np.ndarray) -> np.ndarray:
        q = local(p)
        inner = lambda r: outer(r) + 0.07 * M
        return np.maximum(np.maximum(outer(p), -inner(p)), q[:, 1] - 0.2 * M)

    def parts() -> list[Part]:
        rim = lambda p: np.maximum(hull(p), 0.12 * M - local(p)[:, 1])
        seat = lambda p: rounded_box(local(p), (0.0, 0.08 * M, -0.15 * M), (0.6 * M, 0.025 * M, 0.13 * M), 0.01 * M)
        water = lambda p: np.maximum(outer(p) + 0.05 * M, np.abs(local(p)[:, 1] + 0.12 * M) - 0.02 * M)
        glint = both(noise_mask(0.25 * M, seed + 1, 0.2, (3.0, 1.0, 1.0)), lambda p: np.full(len(p), -1.0))
        oar = lambda p: _union(capsule(p, (-1.0 * M, 0.42 * M, -0.3 * M), (0.75 * M, 0.3 * M, 0.55 * M), 0.045 * M),
                               rounded_box(p, (0.95 * M, 0.28 * M, 0.66 * M), (0.22 * M, 0.02 * M, 0.08 * M), 0.02 * M, rotation_y(-0.45)))
        return [
            Part(_sunk(hull, 0.0), HULL),
            Part(painted(_sunk(hull, 0.0), bands(1, 0.13 * M, 0.025 * M, frame=local)), STRAKE),
            Part(_sunk(rim, 0.0), RIM),
            Part(_sunk(seat, 0.0), SEAT),
            Part(_sunk(water, 0.0), WATER),
            Part(painted(_sunk(water, 0.0), glint), GLINT),
            Part(_sunk(oar, 0.0), OAR),
            Part(waterline(outer, 0.0, 0.13 * M, seed + 2, 0.25), RIPPLE),
        ]

    return PropModel(stem, parts, materials, AXIS_Y_YAW, canvas=(110, 90), footprint=box_footprint(0.65 * M, 1.5 * M))


def drowned_cart(stem: str, seed: int) -> PropModel:
    """Charrette embourbée : plateau de planches penché, une roue à rayons enfoncée jusqu'au moyeu, brancards levés, un sac resté à bord."""
    WOOD, WOOD_LIGHT, GRAIN, WHEEL, IRON_M, MUD, SACK, RIPPLE = range(8)
    materials = [make_material("wood", WOOD_OLD), make_material("wood_light", BLEACHED), make_material("grain", WOOD_DARK, contrast=0.7),
                 make_material("wheel", WOOD_DARK, contrast=0.8), make_material("iron", RUST), make_material("mud", PEAT, contrast=0.7),
                 make_material("sack", FUNGUS, contrast=0.7), make_material("ripple", WATER_MILK, contrast=0.4)]
    tilt = rotation_z(0.2) @ rotation_x(-0.22)
    origin = np.array([0.0, 0.62 * M, 0.0])
    local = lambda p: (p - origin) @ tilt

    def wheel(p: np.ndarray, center, rotation) -> np.ndarray:
        q = (p - np.asarray(center)) @ (rotation_z(np.pi / 2) @ rotation)
        rim = np.maximum(np.abs(np.hypot(q[:, 0], q[:, 2]) - 0.5 * M) - 0.045 * M, np.abs(q[:, 1]) - 0.05 * M)
        spokes = _union(*(np.maximum(np.abs(q[:, 0] * np.cos(a) - q[:, 2] * np.sin(a)) - 0.025 * M,
                                     np.maximum(np.hypot(q[:, 0], q[:, 2]) - 0.5 * M, np.abs(q[:, 1]) - 0.03 * M)) for a in np.linspace(0, np.pi, 4, endpoint=False)))
        hub = np.maximum(np.hypot(q[:, 0], q[:, 2]) - 0.1 * M, np.abs(q[:, 1]) - 0.08 * M)
        return _union(rim, spokes, hub)

    wheels = (((-0.72 * M, 0.25 * M, -0.15 * M), rotation_x(0.1)), ((0.74 * M, 0.5 * M, -0.05 * M), rotation_x(-0.12)))

    def parts() -> list[Part]:
        boards = [(z, k) for k, z in enumerate(np.linspace(-0.82, 0.82, 7))]
        bed = lambda kind: (lambda p: _union(*(rounded_box(local(p), (0.0, 0.0, z * M), (0.62 * M, 0.04 * M, 0.11 * M), 0.012 * M)
                                               for z, k in boards if k % 2 == kind)))
        sides = lambda p: _union(*(rounded_box(local(p), (x * M, 0.17 * M, 0.0), (0.035 * M, 0.13 * M, 0.92 * M), 0.012 * M) for x in (-0.6, 0.6)),
                                 rounded_box(local(p), (0.0, 0.17 * M, -0.9 * M), (0.62 * M, 0.13 * M, 0.035 * M), 0.012 * M))
        shafts = lambda p: _union(*(capsule(p, (x * M, 0.72 * M, 0.85 * M), (x * M * 0.55, 1.75 * M, 1.85 * M), 0.055 * M, 0.045 * M) for x in (-0.42, 0.42)),
                                  capsule(p, (-0.3 * M, 1.5 * M, 1.6 * M), (0.3 * M, 1.5 * M, 1.6 * M), 0.035 * M))
        sack_at = origin + tilt @ np.array([-0.2 * M, 0.2 * M, -0.35 * M])
        all_wood = lambda p: _union(bed(0)(p), bed(1)(p), sides(p))
        wheel_shape = lambda p: _union(*(wheel(p, c, r) for c, r in wheels))
        return [
            Part(bed(0), WOOD),
            Part(bed(1), WOOD_LIGHT),
            Part(sides, WOOD),
            Part(painted(all_wood, noise_mask(0.07 * M, seed + 1, 0.22, (7.0, 1.0, 1.0), frame=local)), GRAIN),
            Part(shafts, WOOD_LIGHT),
            Part(_sunk(wheel_shape, 0.05 * M), WHEEL),
            Part(lambda p: _union(*(np.maximum(np.abs(np.hypot(((p - np.asarray(c)) @ (rotation_z(np.pi / 2) @ r))[:, 0],
                                                                ((p - np.asarray(c)) @ (rotation_z(np.pi / 2) @ r))[:, 2]) - 0.5 * M) - 0.05 * M,
                                               np.abs(((p - np.asarray(c)) @ (rotation_z(np.pi / 2) @ r))[:, 1]) - 0.025 * M) for c, r in wheels)), IRON_M),
            Part(lambda p: ellipsoid(p, sack_at, (0.26 * M, 0.2 * M, 0.22 * M), tilt), SACK),
            Part(lambda p: ellipsoid(p, (-0.68 * M, 0.0, -0.1 * M), (0.42 * M, 0.08 * M, 0.32 * M)), MUD),
            Part(waterline(wheel_shape, 0.05 * M, 0.1 * M), RIPPLE),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(110, 110), footprint=box_footprint(0.7 * M, 0.9 * M))


def rusted_barrel(stem: str, seed: int) -> PropModel:
    """
    Deux fûts d'huile à la peinture passée : l'un debout, penché dans la vase, l'autre couché devant, son fond cerclé
    tourné vers la lumière. La rouille coule des cerclages ; une nappe sombre et irisée s'échappe du fût couché.
    """
    PAINT, RUST_M, RUST_DARK, RIB, LID, SLICK, SHEEN = range(7)
    # Ocre passé : sur le sol vert-bleu du marais, un bidon teal disparaissait ; l'ocre s'en détache sans crier.
    materials = [make_material("paint", PAINT_FADED), make_material("rust", RUST_ORANGE, contrast=0.8),
                 make_material("rust_dark", RUST, contrast=0.8), make_material("rib", "#5E5246", contrast=0.8),
                 make_material("lid", "#B8A274", contrast=0.6), make_material("slick", VIOLET, contrast=0.5),
                 make_material("sheen", SILVER, contrast=0.4)]
    standing = (np.array([0.22 * M, 0.38 * M, -0.2 * M]), rotation_z(0.14) @ rotation_x(0.1))
    # Couché, fond vers l'avant-gauche : face à la lumière, son couvercle cerclé se lit d'un coup d'œil.
    lying = (np.array([-0.3 * M, 0.28 * M, 0.22 * M]), _align_y(np.array([-0.55, 0.0, 0.83])))

    def drum(p: np.ndarray, center, rotation) -> np.ndarray:
        return cylinder((p - center) @ rotation, (0, 0, 0), 0.29 * M, 0.42 * M, 0.03 * M)

    def ribs(p: np.ndarray, center, rotation) -> np.ndarray:
        q = (p - center) @ rotation
        return _union(*(np.abs(cylinder(q, (0, y * M, 0), 0.305 * M, 0.024 * M)) - 0.008 * M for y in (-0.15, 0.15)),
                      np.abs(cylinder(q, (0, 0.4 * M, 0), 0.3 * M, 0.025 * M)) - 0.01 * M)

    def lid(p: np.ndarray, center, rotation) -> np.ndarray:
        q = (p - center) @ rotation
        plate = cylinder(q, (0, 0.425 * M, 0), 0.23 * M, 0.012 * M)
        bung = cylinder(q, (0.12 * M, 0.44 * M, 0.06 * M), 0.035 * M, 0.02 * M)
        return np.minimum(plate, bung)

    def parts() -> list[Part]:
        body = lambda p: np.minimum(drum(p, *standing), drum(p, *lying))
        sunk_body = _sunk(body, 0.05 * M)
        # Coulures : un bruit étiré le long de l'axe de chaque fût (vertical pour le fût debout).
        streaks = lambda p: np.minimum(noise_mask(0.12 * M, seed + 1, 0.3, (1.0, 4.0, 1.0), frame=lambda q: (q - standing[0]) @ standing[1])(p),
                                       noise_mask(0.12 * M, seed + 2, 0.3, (1.0, 4.0, 1.0), frame=lambda q: (q - lying[0]) @ lying[1])(p))
        pits = noise_mask(0.08 * M, seed + 3, 0.14)
        return [
            Part(sunk_body, PAINT),
            Part(painted(sunk_body, streaks), RUST_M),
            Part(painted(sunk_body, pits, 0.02 * M), RUST_DARK),
            Part(_sunk(lambda p: np.minimum(ribs(p, *standing), ribs(p, *lying)), 0.05 * M), RIB),
            Part(lambda p: np.minimum(lid(p, *standing), lid(p, *lying)), LID),
            Part(lambda p: np.maximum(ellipsoid(p, (-0.55 * M, 0.0, 0.62 * M), (0.55 * M, 0.03 * M, 0.3 * M)), -body(p)), SLICK),
            Part(lambda p: np.maximum(ellipsoid(p, (-0.5 * M, 0.01 * M, 0.66 * M), (0.3 * M, 0.03 * M, 0.06 * M), rotation_y(0.25)), -body(p)), SHEEN),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(70, 64), footprint=box_footprint(0.55 * M, 0.45 * M))


def old_post(stem: str, seed: int) -> PropModel:
    """Pieux d'amarrage : deux bois fendus de hauteurs inégales, une corde nouée qui file de l'un à l'autre puis vers l'eau."""
    WOOD, GROOVE, WET, TOP, ROPE, LICHEN_M, RIPPLE = range(7)
    materials = [make_material("post", BLEACHED), make_material("bark_groove", BARK_GREY, contrast=0.8),
                 make_material("wet_wood", WET_WOOD, contrast=0.8), make_material("end_grain", FUNGUS, contrast=0.6),
                 make_material("rope", FUNGUS, contrast=0.7), make_material("lichen", LICHEN, contrast=0.8),
                 make_material("ripple", WATER_MILK, contrast=0.4)]

    def parts() -> list[Part]:
        posts = [(np.array([-0.25 * M, 0.0, -0.1 * M]), 1.55 * M, 0.17 * M, rotation_z(0.08)), (np.array([0.4 * M, 0.0, 0.2 * M]), 1.0 * M, 0.15 * M, rotation_z(-0.14))]

        def post(p: np.ndarray, base, height, radius, lean) -> np.ndarray:
            q = (p - base) @ lean
            body = cylinder(q, (0, height * 0.5 - 0.1 * M, 0), radius, height * 0.5 + 0.1 * M, 0.04 * M)
            # Tête fendue : une entaille en V sur le dessus.
            notch = ((q[:, 1] - height + 0.12 * M) - np.abs(q[:, 0]) * 1.6) / 1.9
            return np.maximum(body, notch)

        shape = lambda p: _union(*(post(p, *spec) for spec in posts))
        tops = lambda p: _union(*(np.maximum(post(p, *spec) - 0.01 * M, -(((p - spec[0]) @ spec[3])[:, 1] - spec[1] + 0.16 * M)) for spec in posts))
        coil_a = posts[0][0] + np.array([0.0, 1.15 * M, 0.0])
        coil_b = posts[1][0] + np.array([-0.08 * M, 0.75 * M, 0.0])
        rope = _sag(coil_a, coil_b, 0.18 * M, 6) + [coil_b + np.array([0.3 * M, -0.4 * M, 0.25 * M]), coil_b + np.array([0.6 * M, -0.73 * M, 0.45 * M])]
        lichen = both(noise_mask(0.14 * M, seed + 1, 0.25), lambda p: np.abs(p[:, 1] - 0.7 * M) - 0.4 * M)
        return [
            Part(_sunk(shape, 0.0), WOOD),
            Part(_grooves(_sunk(shape, 0.0), seed + 2, period=0.08 * M), GROOVE),
            Part(_wet_foot(_sunk(shape, 0.0), seed + 3, 0.3 * M), WET),
            Part(painted(_sunk(shape, 0.0), lichen), LICHEN_M),
            Part(tops, TOP),
            Part(lambda p: _union(_branch_chain(rope, 0.035 * M, 0.03 * M)(p),
                                  np.abs(cylinder(p, coil_a, 0.19 * M, 0.06 * M)) - 0.025 * M,
                                  np.abs(cylinder(p, coil_b, 0.17 * M, 0.05 * M)) - 0.025 * M), ROPE),
            Part(waterline(shape, 0.02 * M, 0.1 * M), RIPPLE),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(64, 72))


def bone_pile(stem: str, seed: int) -> PropModel:
    """Carcasse de bête à demi enfouie au bord de l'eau : cage de côtes dressée, échine, crâne cornu tombé devant."""
    BONE_M, BONE_SHADE, SOCKET, MUD = range(4)
    materials = [make_material("bone", BONE, contrast=0.7), make_material("bone_shade", FUNGUS, contrast=0.7),
                 make_material("socket", PEAT, contrast=0.6), make_material("mud", PEAT, contrast=0.7)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        spine_points = [np.array([x * M, 0.08 * M + 0.06 * M * np.sin(x * 2.0), -0.15 * M]) for x in np.linspace(-0.85, 0.45, 7)]
        ribs = []
        for index, x in enumerate(np.linspace(-0.7, 0.3, 6)):
            height = (0.62 - abs(x + 0.2) * 0.35) * M
            for side in (-1.0, 1.0):
                if index == 4 and side > 0:
                    continue
                arc = [np.array([x * M, 0.12 * M, -0.15 * M]), np.array([x * M + 0.03 * M, height, -0.15 * M + side * 0.2 * M]),
                       np.array([x * M + 0.06 * M, height * 0.65, -0.15 * M + side * 0.42 * M]), np.array([x * M + 0.08 * M, 0.02 * M, -0.15 * M + side * 0.5 * M])]
                ribs.append(arc)
        skull = np.array([0.82 * M, 0.17 * M, 0.25 * M])
        heading = rotation_y(-0.6)
        horn = [skull + heading @ np.array([s * 0.12 * M, 0.08 * M, -0.08 * M]) for s in (-1, 1)]
        horn_tips = [h + heading @ np.array([s * 0.28 * M, 0.22 * M, -0.12 * M]) for h, s in zip(horn, (-1, 1))]
        snout = skull + heading @ np.array([0.0, -0.05 * M, 0.3 * M])
        vertebrae = [np.array([w.uniform(0.2, 0.6) * M, 0.04 * M, w.uniform(0.3, 0.55) * M]) for _ in range(3)]
        femur = ((-0.3 * M, 0.05 * M, 0.45 * M), (0.25 * M, 0.05 * M, 0.55 * M))
        return [
            Part(lambda p: _union(_branch_chain(spine_points, 0.07 * M, 0.05 * M)(p),
                                  *(_branch_chain(r, 0.045 * M, 0.03 * M)(p) for r in ribs[::2])), BONE_M),
            Part(lambda p: _union(*(_branch_chain(r, 0.045 * M, 0.03 * M)(p) for r in ribs[1::2])), BONE_SHADE),
            Part(lambda p: _union(ellipsoid(p, skull, (0.17 * M, 0.14 * M, 0.2 * M), heading),
                                  capsule(p, skull, snout, 0.12 * M, 0.07 * M),
                                  *(capsule(p, h, t, 0.05 * M, 0.015 * M) for h, t in zip(horn, horn_tips)),
                                  *(sphere(p, v, 0.06 * M) for v in vertebrae),
                                  capsule(p, *femur, 0.045 * M), sphere(p, femur[0], 0.07 * M), sphere(p, femur[1], 0.07 * M)), BONE_M),
            Part(lambda p: _union(*(sphere(p, skull + heading @ np.array([s * 0.08 * M, 0.04 * M, 0.15 * M]), 0.045 * M) for s in (-1, 1))), SOCKET),
            Part(lambda p: ellipsoid(p, (-0.25 * M, -0.02 * M, -0.15 * M), (0.8 * M, 0.08 * M, 0.45 * M)), MUD),
        ]

    return PropModel(stem, parts, materials, THREE_QUARTER, canvas=(90, 56))


def sunken_shrine(stem: str, seed: int) -> PropModel:
    """
    Oratoire de chemin enfoncé de biais : socle en assises, fût de pierre, niche voûtée où veille une petite figure
    pâle, toit à deux pans couvert de lichen ; une bougie verte brûle encore, des rubans et des fioles au pied.
    """
    STONE_M, JOINT, ROOF, LICHEN_M, NICHE, FIGURE, FLAME, CLOTH, GLASS, RIPPLE = range(10)
    materials = [make_material("stone", STONE), make_material("joint", "#5E5C54", contrast=0.6), make_material("roof", "#5E6466"),
                 make_material("lichen", LICHEN, contrast=0.8), make_material("niche", PEAT, contrast=0.5),
                 make_material("figure", BONE, contrast=0.6), make_emissive("flame", SPORE), make_material("cloth", CLOTH_RED, contrast=0.8),
                 make_material("glass", WATER_MILK, contrast=0.5), make_material("ripple", WATER_MILK, contrast=0.4)]
    tilt = rotation_z(0.12) @ rotation_x(-0.06)
    level = 0.16 * M
    local = lambda p: p @ tilt

    def niche(p: np.ndarray) -> np.ndarray:
        # Ouverture voûtée sur la face avant de la chapelle : un rectangle surmonté d'un demi-disque.
        q = local(p) - np.array([0.0, 1.5 * M, 0.3 * M])
        box = np.maximum(np.maximum(np.abs(q[:, 0]) - 0.23 * M, np.abs(q[:, 1] + 0.08 * M) - 0.16 * M), np.abs(q[:, 2]) - 0.22 * M)
        arch = np.maximum(np.hypot(q[:, 0], q[:, 1] - 0.08 * M) - 0.23 * M, np.abs(q[:, 2]) - 0.22 * M)
        return np.minimum(box, arch)

    def roof(p: np.ndarray) -> np.ndarray:
        # Faîtage de gauche à droite : le pan avant, face à la caméra, porte le lichen ; les pignons sont sur les côtés.
        q = local(p) - np.array([0.0, 1.86 * M, 0.0])
        slope = (np.abs(q[:, 2]) * 0.9 + q[:, 1] - 0.36 * M) / 1.35
        return np.maximum(np.maximum(slope, -q[:, 1]), np.abs(q[:, 0]) - 0.48 * M)

    def parts() -> list[Part]:
        plinth = lambda p: rounded_box(local(p), (0, 0.25 * M, 0), (0.5 * M, 0.25 * M, 0.42 * M), 0.03 * M)
        shaft = lambda p: rounded_box(local(p), (0, 0.85 * M, 0), (0.22 * M, 0.38 * M, 0.2 * M), 0.025 * M)
        chapel = lambda p: rounded_box(local(p), (0, 1.52 * M, 0), (0.36 * M, 0.34 * M, 0.32 * M), 0.03 * M)
        stone = lambda p: np.maximum(_union(plinth(p), shaft(p), chapel(p)), -niche(p))
        figure_at = np.array([0.0, 1.42 * M, 0.2 * M])
        figure = lambda p: _union(ellipsoid(local(p), figure_at, (0.075 * M, 0.15 * M, 0.06 * M)),
                                  sphere(local(p), figure_at + np.array([0.0, 0.19 * M, 0.0]), 0.055 * M))
        candle = tilt @ np.array([0.13 * M, 1.26 * M, 0.3 * M])
        lichen = noise_mask(0.16 * M, seed + 1, 0.35)
        joints = bands(1, 0.17 * M, 0.024 * M, offset=0.085 * M, frame=local)
        ribbons = [(tilt @ np.array([0.2 * M, 1.05 * M, 0.21 * M]), tilt @ np.array([0.28 * M, 0.7 * M, 0.3 * M])),
                   (tilt @ np.array([0.16 * M, 1.05 * M, 0.21 * M]), tilt @ np.array([0.12 * M, 0.75 * M, 0.34 * M]))]
        vials = [tilt @ np.array([x * M, 0.5 * M, z * M]) for x, z in ((-0.32, 0.3), (-0.2, 0.34), (0.3, 0.32))]
        return [
            Part(_sunk(stone, level), STONE_M),
            Part(painted(_sunk(stone, level), joints), JOINT),
            Part(roof, ROOF),
            Part(painted(roof, lichen, 0.02 * M), LICHEN_M),
            Part(lambda p: np.maximum(stone(p) + 0.01 * M, np.abs(niche(p)) - 0.03 * M), NICHE),
            Part(figure, FIGURE),
            Part(lambda p: _union(capsule(p, candle - np.array([0.0, 0.05 * M, 0.0]), candle + np.array([0.0, 0.03 * M, 0.0]), 0.032 * M),
                                  ellipsoid(p, candle + np.array([0.0, 0.1 * M, 0.0]), (0.04 * M, 0.075 * M, 0.04 * M))), FLAME),
            Part(lambda p: _union(*(rounded_box(p, (a + b) / 2, (0.045 * M, float(np.linalg.norm(b - a)) / 2, 0.012 * M), 0.01 * M, _align_y(b - a))
                                    for a, b in ribbons)), CLOTH),
            Part(lambda p: _union(*(capsule(p, v, v + np.array([0.0, 0.12 * M, 0.0]), 0.045 * M, 0.025 * M) for v in vials)), GLASS),
            Part(waterline(plinth, level, 0.14 * M, seed + 2, 0.25), RIPPLE),
        ]

    return PropModel(stem, parts, materials, THREE_QUARTER, canvas=(80, 110), footprint=box_footprint(0.5 * M, 0.42 * M))


def swamp_lantern(stem: str, seed: int) -> PropModel:
    """Lanterne de passeur : perche tordue plantée dans la vase, potence, lanterne à cage de fer qui luit de spores."""
    POLE, GROOVE, WET, IRON_M, GLASS, ROPE = range(6)
    materials = [make_material("pole", BLEACHED), make_material("bark_groove", BARK_GREY, contrast=0.8),
                 make_material("wet_wood", WET_WOOD, contrast=0.8), make_material("iron", IRON), make_emissive("glow", SPORE),
                 make_material("rope", FUNGUS, contrast=0.7)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        pole = _gnarled(w, np.zeros(3), np.array([0.05, 1.0, 0.0]), 2.3 * M, 4, wobble=0.06)
        top = pole[-1]
        arm_end = top + np.array([0.5 * M, -0.02 * M, 0.1 * M])
        brace = (pole[-2] + np.array([0.0, 0.05 * M, 0.0]), top + (arm_end - top) * 0.55)
        lantern = arm_end + np.array([0.0, -0.5 * M, 0.0])
        wood = lambda p: _union(_branch_chain(pole, 0.075 * M, 0.055 * M)(p), capsule(p, top, arm_end, 0.045 * M, 0.04 * M),
                                capsule(p, *brace, 0.03 * M))

        # Lanterne grossie d'un tiers : à l'échelle réelle, sa lueur ne fait que deux pixels.
        size = 1.35

        def cage(p: np.ndarray) -> np.ndarray:
            q = (p - lantern) / size
            cap = np.minimum(cylinder(q, (0, 0.18 * M, 0), 0.13 * M, 0.03 * M, 0.01 * M),
                             capsule(q, (0, 0.2 * M, 0), (0, 0.3 * M, 0), 0.05 * M, 0.02 * M))
            base = cylinder(q, (0, -0.17 * M, 0), 0.12 * M, 0.03 * M, 0.01 * M)
            bars = _union(*(capsule(q, (np.sin(a) * 0.11 * M, -0.17 * M, np.cos(a) * 0.11 * M), (np.sin(a) * 0.11 * M, 0.18 * M, np.cos(a) * 0.11 * M), 0.017 * M)
                            for a in np.linspace(np.pi / 4, 2 * np.pi + np.pi / 4, 4, endpoint=False)))
            hook = capsule(q, (0, 0.3 * M, 0), (0, 0.42 * M, 0), 0.015 * M)
            return _union(cap, base, bars, hook) * size

        wraps = [top + np.array([0.0, -y * M, 0.0]) for y in (0.35, 0.45)]
        return [
            Part(wood, POLE),
            Part(_grooves(wood, seed + 1, period=0.07 * M), GROOVE),
            Part(_wet_foot(wood, seed + 2, 0.32 * M), WET),
            Part(cage, IRON_M),
            Part(lambda p: cylinder(p, lantern, 0.095 * M * size, 0.15 * M * size, 0.02 * M), GLASS),
            Part(lambda p: _union(*(np.abs(cylinder(p, c, 0.085 * M, 0.03 * M)) - 0.015 * M for c in wraps)), ROPE),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(64, 100))


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
