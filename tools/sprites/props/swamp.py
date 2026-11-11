"""
Décors des Marécages (plan 08, lot P5 ; repris au plan 31, lot M1) : une réalité fragile, eau laiteuse, bois blanchi,
barbes de lichen et lueurs de spores.

Échelle réelle du personnage (1 m ≈ 27,5 unités), mêmes noms de fichiers que les anciens décors (mêmes uid).
Palette « Marécages » de la charte (§3) : bois blanchi pour les arbres morts, mousse humide et lichen jaune-vert,
violet profond pour ce qui est vénéneux, spore verte pour les rares lueurs, reflet argenté sur l'eau. Le sol du marais
est sombre et moucheté : un décor s'y lit par sa valeur (bois blanchi, os, lichen clair) et par un pied mouillé plus
sombre, jamais par un contour seul. Ce qui est « noyé » est coupé à un niveau d'eau sous le point au sol et cerné d'un
liseré de clapot argenté, pour se lire posé dans l'eau plutôt que sur elle.
Le marais est devenu le Bas-Port (plan 58 T2) : chenaux, roselières, vasières et prés salés se règlent dans le rythme de
`data/props/swamp.json`, et les décors du port rejoignent ceux du marais.
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
from .buildings import BuildingSpec, building

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

# ---------------------------------------------------------------------------------------------------------------------
# Le Bas-Port (plan 58 T2a) : passerelles des chenaux, roselières, vasières
# ---------------------------------------------------------------------------------------------------------------------

NET = "#8E9A88"
NET_DARK = "#4E5A50"
CORK = "#B07A4A"
FLOAT_RED = "#B04A34"
PLUME = "#7E6656"


def footbridge(stem: str, seed: int, angle: float, half_length: float) -> PropModel:
    """
    Passerelle d'un chenal, posée là où passe un chemin : un caillebotis de planches clouées en travers sur deux
    longerons, quelques planches manquantes ou de biais. Pas de pieux : vus de haut, ils en faisaient une clôture. Un
    module par cellule de chemin : les modules voisins se touchent et font la passerelle. Quatre sens (le long de x,
    de y, et les deux diagonales de l'écran), tournés de `angle` au sol. À plat, il se range dans les décalques au sol.
    """
    PLANK, PLANK_LIGHT, GRAIN, BEAM = range(4)
    materials = [make_material("plank", WOOD_OLD), make_material("plank_light", BLEACHED),
                 make_material("grain", WOOD_DARK, contrast=0.7), make_material("beam", WOOD_DARK, contrast=0.8)]
    length = half_length
    half_width = 0.7 * M
    deck = 0.16 * M
    # Le module se construit le long de x, puis se tourne au sol vers son sens.
    turn = rotation_y(angle)
    frame = lambda p: p @ turn

    def parts() -> list[Part]:
        w = Weathering(seed)
        count = int(round(2 * length / (0.34 * M)))
        planks = [(np.array([x, deck + w.uniform(-0.01, 0.01) * M, w.uniform(-0.05, 0.05) * M]),
                   rotation_y(w.uniform(-0.06, 0.06)) @ rotation_z(w.uniform(-0.03, 0.03)), k % 2)
                  for k, x in enumerate(np.linspace(-length + 0.16 * M, length - 0.16 * M, count)) if k not in (2, count - 3)]

        def plank_shape(kind: int):
            return lambda p: _union(*(rounded_box(frame(p), c, (0.15 * M, 0.03 * M, half_width), 0.012 * M, r)
                                      for c, r, k in planks if k == kind))

        everything = lambda p: np.minimum(plank_shape(0)(p), plank_shape(1)(p))
        grain = noise_mask(0.07 * M, seed + 1, 0.2, (1.0, 1.0, 7.0), frame=frame)
        beams = lambda p: _union(*(rounded_box(frame(p), (0.0, deck - 0.07 * M, z * M), (length, 0.04 * M, 0.06 * M), 0.01 * M)
                                   for z in (-0.5, 0.5)))
        return [
            Part(plank_shape(0), PLANK),
            Part(plank_shape(1), PLANK_LIGHT),
            Part(painted(everything, grain), GRAIN, relief=False),
            Part(beams, BEAM),
        ]

    # Plancher plat vu à 30° : sans lissage des pentes, chaque planche se couvre de lignes internes et noircit.
    return PropModel(stem, parts, materials, FRONT, canvas=(96, 64), smooth_slopes=True)


def tall_reeds(stem: str, seed: int) -> PropModel:
    """
    Roseaux de la roselière : une touffe haute de deux mètres, tiges fines et droites, panaches gris-brun qui penchent
    tous du même côté, quelques feuilles en lame à mi-hauteur. Plus haut et plus pâle que les massettes.
    """
    STEM, LEAF, PLUME_M = range(3)
    materials = [make_material("stem", MOSS_LIGHT, contrast=0.6), make_material("leaf", LICHEN, contrast=0.6),
                 make_material("plume", PLUME, contrast=0.7)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        stems = []
        for k in range(11):
            x, z = w.uniform(-0.55, 0.55) * M, w.uniform(-0.35, 0.35) * M
            h = w.uniform(1.6, 2.3) * M
            lean = w.uniform(0.05, 0.22)
            base = np.array([x, 0.0, z])
            stems.append((base, base + np.array([lean * h, h, w.uniform(-0.05, 0.05) * M])))
        blades = []
        for base, top in stems[::2]:
            mid = base + (top - base) * w.uniform(0.35, 0.55)
            blades.append((mid, mid + np.array([w.choice((-1.0, 1.0)) * 0.35 * M, -0.12 * M, w.uniform(-0.1, 0.1) * M])))
        return [
            Part(lambda p: _union(*(capsule(p, a, b, 0.022 * M, 0.014 * M) for a, b in stems)), STEM),
            Part(lambda p: _union(*(capsule(p, a, b, 0.03 * M, 0.005 * M) for a, b in blades)), LEAF),
            Part(lambda p: _union(*(ellipsoid(p, b + np.array([0.09 * M, -0.05 * M, 0.0]), (0.15 * M, 0.06 * M, 0.05 * M), rotation_z(-0.5))
                                    for _, b in stems[::2])), PLUME_M),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(72, 96))


def lobster_pots(stem: str, seed: int) -> PropModel:
    """
    Casiers de pêcheur empilés : des cages carrées, cadre de bois sombre et filet tendu, deux au sol, un dessus, un
    tombé sur le flanc ; un bout de cordage et un flotteur rouge passé. Ce que le port laissait sur les quais.
    """
    FRAME, NET_M, MESH, ROPE, FLOAT = range(5)
    materials = [make_material("frame", WOOD_DARK, contrast=0.8), make_material("net", NET, contrast=0.6),
                 make_material("mesh", NET_DARK, contrast=0.7), make_material("rope", FUNGUS, contrast=0.7),
                 make_material("float", FLOAT_RED, contrast=0.7)]
    half = np.array([0.3, 0.2, 0.24]) * M
    pots = [(np.array([-0.34 * M, half[1], 0.0]), rotation_y(0.04)), (np.array([0.34 * M, half[1], 0.04 * M]), rotation_y(-0.06)),
            (np.array([0.02 * M, 3 * half[1] + 0.01 * M, 0.02 * M]), rotation_y(0.15)),
            (np.array([1.05 * M, half[2], 0.45 * M]), rotation_y(0.9) @ rotation_x(np.pi / 2))]

    def local(p: np.ndarray, origin, rotation) -> np.ndarray:
        return (p - origin) @ rotation

    def cage(p: np.ndarray) -> np.ndarray:
        return _union(*(rounded_box(local(p, o, r), (0.0, 0.0, 0.0), tuple(half), 0.015 * M) for o, r in pots))

    def frame(p: np.ndarray) -> np.ndarray:
        # Arêtes du cadre : le bord de la boîte, là où deux faces se rencontrent.
        result = []
        for o, r in pots:
            q = np.abs(local(p, o, r)) - half
            edge = np.sort(q, axis=1)[:, 1]
            result.append(np.maximum(rounded_box(local(p, o, r), (0.0, 0.0, 0.0), tuple(half + 0.012 * M), 0.015 * M), -edge - 0.035 * M))
        return _union(*result)

    def parts() -> list[Part]:
        mesh = lambda p: np.minimum(np.abs(np.mod(p[:, 0] + p[:, 2], 0.1 * M) - 0.05 * M) - 0.012 * M,
                                    np.abs(np.mod(p[:, 1], 0.1 * M) - 0.05 * M) - 0.012 * M)
        float_at = np.array([-0.95 * M, 0.13 * M, 0.5 * M])
        rope = [np.array([0.02 * M, 0.82 * M, 0.2 * M]), np.array([-0.45 * M, 0.45 * M, 0.4 * M]), np.array([-0.75 * M, 0.05 * M, 0.55 * M]), float_at]
        return [
            Part(cage, NET_M),
            Part(painted(cage, mesh), MESH, relief=False),
            Part(frame, FRAME),
            Part(_branch_chain(rope, 0.03 * M, 0.025 * M), ROPE),
            Part(lambda p: ellipsoid(p, float_at, (0.15 * M, 0.12 * M, 0.12 * M)), FLOAT),
        ]

    return PropModel(stem, parts, materials, AXIS_Y_YAW, canvas=(96, 80), footprint=box_footprint(0.75 * M, 0.4 * M))


def drying_net(stem: str, seed: int) -> PropModel:
    """
    Filet qui sèche, jeté sur une perche entre deux fourches : il pend des deux côtés en pans inégaux, effrangé en bas,
    maillage clair sur un filet sombre pour qu'on le lise ajouré ; flotteurs de liège le long de la ralingue.
    """
    POLE, NET_M, MESH, CORK_M = range(4)
    materials = [make_material("pole", BLEACHED), make_material("net", NET_DARK, contrast=0.6),
                 make_material("mesh", NET, contrast=0.6), make_material("cork", CORK, contrast=0.7)]
    span = 1.0 * M
    ridge = 1.35 * M

    def net(p: np.ndarray) -> np.ndarray:
        x, y, z = p[:, 0], p[:, 1], p[:, 2]
        # Deux pans qui tombent de la perche en s'écartant, plus bas d'un côté, bord effrangé par un bruit.
        flare = 0.32 * M * np.clip((ridge - y) / ridge, 0.0, 1.0)
        sheet = np.abs(np.abs(z) - flare) - 0.025 * M
        drop = np.where(z > 0, 0.25 * M, 0.55 * M) + 0.12 * M * value_noise(p, 0.2 * M, seed, (1.0, 100.0, 100.0))
        inside = np.maximum(np.abs(x) - span + 0.12 * M, np.maximum(y - ridge, drop - y))
        return np.maximum(sheet, inside)

    def parts() -> list[Part]:
        w = Weathering(seed)
        forks = lambda p: _union(*(np.minimum(capsule(p, (x * span, 0.0, 0.0), (x * span, ridge + 0.05 * M, 0.0), 0.05 * M, 0.04 * M),
                                              capsule(p, (x * span, ridge - 0.05 * M, 0.0), (x * span + 0.08 * M, ridge + 0.18 * M, 0.06 * M), 0.035 * M, 0.025 * M))
                                   for x in (-1.0, 1.0)))
        pole = lambda p: capsule(p, (-span - 0.15 * M, ridge + 0.04 * M, 0.0), (span + 0.15 * M, ridge + 0.04 * M + w.uniform(-0.03, 0.03) * M, 0.0), 0.04 * M)
        mesh = lambda p: np.minimum(np.abs(np.mod(p[:, 0] + p[:, 1], 0.12 * M) - 0.06 * M) - 0.014 * M,
                                    np.abs(np.mod(p[:, 0] - p[:, 1], 0.12 * M) - 0.06 * M) - 0.014 * M)
        corks = lambda p: _union(*(ellipsoid(p, (x, ridge - 0.12 * M, side * 0.06 * M), (0.06 * M, 0.045 * M, 0.04 * M))
                                   for x in np.linspace(-0.75 * span, 0.75 * span, 5) for side in (-1.0, 1.0)))
        return [
            Part(lambda p: np.minimum(forks(p), pole(p)), POLE),
            Part(net, NET_M),
            Part(painted(net, mesh), MESH, relief=False),
            Part(corks, CORK_M),
        ]

    return PropModel(stem, parts, materials, float(np.radians(-18.0)), canvas=(90, 90), footprint=box_footprint(1.0 * M, 0.3 * M))


def stake_row(stem: str, seed: int) -> PropModel:
    """
    Rangée de pieux dans la vase : ce qui reste d'un parc ou d'une clôture de bord d'eau, six bois de hauteurs inégales,
    deux cassés, un penché, le pied noirci et verdi par l'eau qui montait.
    """
    WOOD, GROOVE, WET, TOP = range(4)
    materials = [make_material("post", BLEACHED), make_material("bark_groove", BARK_GREY, contrast=0.8),
                 make_material("wet_wood", WET_WOOD, contrast=0.8), make_material("end_grain", FUNGUS, contrast=0.6)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        stakes = []
        for k, x in enumerate(np.linspace(-1.3, 1.3, 6)):
            h = (0.45 if k in (1, 4) else w.uniform(0.8, 1.25)) * M
            lean = rotation_z(w.uniform(-0.08, 0.08) + (0.3 if k == 3 else 0.0)) @ rotation_x(w.uniform(-0.06, 0.06))
            stakes.append((np.array([x * M, 0.0, w.uniform(-0.08, 0.08) * M]), h, w.uniform(0.07, 0.1) * M, lean))
        shape = lambda p: _union(*(cylinder((p - b) @ r, (0.0, h * 0.5, 0.0), rad, h * 0.5, 0.02 * M) for b, h, rad, r in stakes))
        tops = lambda p: _union(*(np.maximum(cylinder((p - b) @ r, (0.0, h * 0.5, 0.0), rad - 0.01 * M, h * 0.5, 0.02 * M),
                                             -(((p - b) @ r)[:, 1] - h + 0.04 * M)) for b, h, rad, r in stakes))
        return [
            Part(shape, WOOD),
            Part(_grooves(shape, seed + 1, period=0.07 * M), GROOVE),
            Part(_wet_foot(shape, seed + 2, 0.35 * M), WET),
            Part(tops, TOP),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(110, 64))


# Le chantier naval et les quais (plan 58 T2b) : ce que le port a laissé à terre. Peintures marines passées,
# antifouling rouille, granit des quais, jaune de grue écaillé.
HULL_BLUE = "#40586A"
ANTIFOUL = "#7A3A2C"
BOOT_WHITE = "#C6C2B0"
TARRED = "#3A3430"
CABIN_PAINT = "#B8B29E"
GLASS_DARK = "#2C3238"
SHED_PAINT = "#66705E"
TIN = "#7A7470"
GRANITE = "#7A7870"
CRANE_YELLOW = "#A8843A"
CRATE_BLUE = "#3E6884"
CRATE_RED = "#8A4A3C"
MUD = "#4A4234"


def trawler_hull(stem: str, seed: int) -> PropModel:
    """
    Le chalutier sur sa cale : coque bleue délavée, antifouling rouille sous la ligne de flottaison, liston clair,
    timonerie à l'arrière, potence de chalut au tableau, mât de charge à l'avant. Posé sur ses tins, tenu par trois
    paires d'étais. Coulures de rouille, bois nu là où la peinture est partie : le chantier s'est arrêté en plein carénage.
    """
    (HULL, ANTI, BOOT, STRAKE, BARE, RUST_M, DECK, CABIN, WINDOW, ROOF, STEEL, TIMBER, TIMBER_DARK) = range(13)
    materials = [make_material("hull", HULL_BLUE), make_material("antifouling", ANTIFOUL),
                 make_material("boot", BOOT_WHITE, contrast=0.6), make_material("strake", TARRED, contrast=0.7),
                 make_material("bare_wood", WOOD_OLD, contrast=0.7), make_material("rust", RUST_ORANGE, contrast=0.7),
                 make_material("deck", WOOD_OLD), make_material("cabin", CABIN_PAINT),
                 make_material("window", GLASS_DARK, contrast=0.5), make_material("roof", RUST, contrast=0.8),
                 make_material("steel", IRON, contrast=0.8), make_material("timber", WOOD_OLD, contrast=0.8),
                 make_material("timber_dark", WOOD_DARK, contrast=0.8)]
    half_length, beam, depth, keel_y = 4.2 * M, 1.5 * M, 2.4 * M, 0.9 * M
    deck_y = keel_y + depth - 0.35 * M

    def taper(x: np.ndarray) -> np.ndarray:
        # La coque s'effile vers l'étrave (+x) et garde du gras à l'arrière.
        return np.maximum(1.0 - np.clip(x / half_length, 0.0, 1.0) ** 2 * 0.72 - np.clip(-x / half_length, 0.0, 1.0) ** 2 * 0.2, 0.2)

    def sheer(x: np.ndarray) -> np.ndarray:
        return depth + 0.22 * M * (x / half_length) ** 2 + 0.35 * M * np.clip(x / half_length, 0.0, 1.0) ** 2

    def outer(p: np.ndarray) -> np.ndarray:
        q = p - np.array([0.0, keel_y, 0.0])
        t = taper(q[:, 0])
        scaled = np.stack([q[:, 0], q[:, 1], q[:, 2] / t], axis=1)
        body = ellipsoid(scaled, (0.0, depth, 0.0), (half_length, depth, beam)) * t
        body = np.maximum(body, q[:, 1] - sheer(q[:, 0]))
        return np.maximum(body, -q[:, 0] - 0.88 * half_length)

    def hull(p: np.ndarray) -> np.ndarray:
        o = outer(p)
        # Pavois : l'intérieur au-dessus du pont est creusé, il reste un bordé de 9 cm.
        well = np.maximum(o + 0.09 * M, deck_y - p[:, 1])
        shell = np.maximum(o, -well)
        keel = rounded_box(p, (0.05 * half_length, keel_y + 0.06 * M, 0.0), (0.68 * half_length, 0.12 * M, 0.07 * M), 0.02 * M)
        return np.minimum(shell, keel)

    def side_z(x: float, height: float) -> float:
        # Demi-largeur de la coque à l'abscisse x et à la hauteur donnée (au-dessus de la quille).
        ratio = 1.0 - ((height - depth) / depth) ** 2 - (x / half_length) ** 2
        return float(beam * taper(np.array([x]))[0] * np.sqrt(max(ratio, 0.0)))

    def parts() -> list[Part]:
        w = Weathering(seed)
        qy = lambda p: p[:, 1] - keel_y
        waterline_y = 0.42 * depth
        anti = lambda p: qy(p) - waterline_y - 0.08 * M * p[:, 0] / half_length
        boot = lambda p: np.abs(qy(p) - waterline_y - 0.08 * M * p[:, 0] / half_length - 0.11 * M) - 0.055 * M
        strake = lambda p: np.abs(qy(p) - sheer(p[:, 0]) + 0.22 * M) - 0.07 * M
        topsides = lambda p: waterline_y + 0.2 * M - qy(p)
        bare = both(noise_mask(0.4 * M, seed + 1, 0.1), topsides)
        streaks = both(noise_mask(0.12 * M, seed + 2, 0.14, (1.0, 9.0, 1.0)), lambda p: qy(p) - depth + 0.1 * M)
        deck_floor = both(lambda p: np.abs(p[:, 1] - deck_y) - 0.06 * M, lambda p: outer(p) + 0.05 * M)
        cabin_at = np.array([-0.3 * half_length, deck_y + 0.95 * M, 0.0])
        cabin = lambda p: rounded_box(p, cabin_at, (0.8 * M, 0.95 * M, 0.85 * M), 0.05 * M)
        roof = lambda p: rounded_box(p, cabin_at + np.array([0.0, 1.0 * M, 0.0]), (0.95 * M, 0.07 * M, 1.0 * M), 0.02 * M)
        windows = both(lambda p: np.abs(p[:, 1] - cabin_at[1] - 0.42 * M) - 0.22 * M,
                       lambda p: 0.045 * M - np.abs(np.mod(p[:, 0] + p[:, 2], 0.46 * M) - 0.23 * M))
        stern_x = -0.8 * half_length
        gallow_z = side_z(stern_x, depth) * 0.8
        gallows = [((stern_x, deck_y, s * gallow_z), (stern_x - 0.1 * M, deck_y + 1.8 * M, s * gallow_z * 0.8)) for s in (-1.0, 1.0)]
        gallows.append(((stern_x - 0.1 * M, deck_y + 1.8 * M, -gallow_z * 0.8), (stern_x - 0.1 * M, deck_y + 1.8 * M, gallow_z * 0.8)))
        mast_x = 0.3 * half_length
        rigging = [((mast_x, deck_y, 0.0), (mast_x, deck_y + 3.2 * M, 0.0)),
                   ((mast_x, deck_y + 0.6 * M, 0.0), (0.72 * half_length, deck_y + 1.7 * M, 0.25 * M))]
        shores = []
        for x in (-0.55 * half_length, -0.1 * half_length, 0.35 * half_length):
            touch = depth * 0.45
            z = side_z(x, touch)
            for s in (-1.0, 1.0):
                shores.append(((x + w.uniform(-0.2, 0.2) * M, 0.0, s * (z + 1.7 * M)), (x, keel_y + touch, s * (z - 0.05 * M))))
        blocks = [x * half_length for x in (-0.6, -0.2, 0.2, 0.55)]
        block_shape = lambda p: _union(*(rounded_box(p, (x, keel_y * 0.5 - 0.04 * M, 0.0), (0.22 * M, keel_y * 0.5 - 0.04 * M, 0.42 * M), 0.02 * M)
                                         for x in blocks))
        return [
            Part(hull, HULL),
            Part(painted(hull, anti), ANTI, relief=False),
            Part(painted(hull, boot), BOOT, relief=False),
            Part(painted(hull, strake, 0.02 * M), STRAKE),
            Part(painted(hull, bare, 0.016 * M), BARE, relief=False),
            Part(painted(hull, streaks, 0.02 * M), RUST_M, relief=False),
            Part(painted(hull, deck_floor), DECK, relief=False),
            Part(cabin, CABIN),
            Part(painted(cabin, windows), WINDOW, relief=False),
            Part(painted(cabin, streaks, 0.02 * M), RUST_M, relief=False),
            Part(roof, ROOF),
            Part(lambda p: _union(*(capsule(p, a, b, 0.07 * M) for a, b in gallows + rigging)), STEEL),
            Part(lambda p: _union(*(capsule(p, a, b, 0.085 * M) for a, b in shores)), TIMBER),
            Part(block_shape, TIMBER_DARK),
            Part(painted(block_shape, bands(1, 0.2 * M, 0.03 * M)), TIMBER, relief=False),
        ]

    top_y = deck_y + 3.4 * M
    return PropModel(stem, parts, materials, AXIS_Y_YAW, ray_range=float(2.5 * top_y), canvas=(260, 220),
                     footprint=box_footprint(0.85 * half_length, beam + 0.3 * M),
                     bounds=((-half_length - 0.4 * M, -0.2 * M, -beam - 1.5 * M), (half_length + 0.4 * M, top_y, beam + 1.5 * M)),
                     supersample=3)


def slipway(stem: str, seed: int) -> PropModel:
    """
    La cale de halage : deux longrines goudronnées sur des traverses, un rail de fer rouillé sur chacune, qui filent
    sous la coque et descendent vers l'eau qu'il n'y a plus. Des traverses manquent. À plat : on marche dessus.
    """
    WAY, SLEEPER, GRAIN, RAIL = range(4)
    materials = [make_material("way", TARRED, contrast=0.7), make_material("sleeper", WOOD_DARK, contrast=0.8),
                 make_material("grain", WOOD_OLD, contrast=0.6), make_material("rail", RUST, contrast=0.7)]
    start, end = -5.4 * M, 5.4 * M

    def parts() -> list[Part]:
        w = Weathering(seed)
        sleepers = [(x, rotation_y(w.uniform(-0.06, 0.06))) for k, x in enumerate(np.arange(start + 0.2 * M, end, 0.78 * M)) if k not in (3, 9)]
        ways = lambda p: _union(*(rounded_box(p, ((start + end) * 0.5, 0.1 * M, z * M), ((end - start) * 0.5, 0.07 * M, 0.14 * M), 0.02 * M)
                                  for z in (-0.62, 0.62)))
        sleeper_shape = lambda p: _union(*(rounded_box(p, (x, 0.03 * M, 0.0), (0.12 * M, 0.03 * M, 0.98 * M), 0.015 * M, r) for x, r in sleepers))
        return [
            Part(sleeper_shape, SLEEPER),
            Part(painted(sleeper_shape, noise_mask(0.06 * M, seed + 1, 0.25, (1.0, 1.0, 6.0))), GRAIN, relief=False),
            Part(ways, WAY),
            Part(lambda p: _union(*(capsule(p, (start + 0.1 * M, 0.2 * M, z * M), (end - 0.1 * M, 0.2 * M, z * M), 0.035 * M) for z in (-0.62, 0.62))), RAIL),
        ]

    return PropModel(stem, parts, materials, AXIS_Y_YAW, ray_range=float(end - start), canvas=(260, 110), smooth_slopes=True,
                     bounds=((start - 0.2 * M, -0.1 * M, -1.1 * M), (end + 0.2 * M, 0.4 * M, 1.1 * M)))


def winch(stem: str, seed: int) -> PropModel:
    """Le treuil de la cale : bâti de fonte en A sur un patin de bois, tambour où la chaîne est encore enroulée, grande roue dentée, manivelle ; la chaîne sort et court au sol."""
    IRON_M, RUST_M, SKID, CHAIN = range(4)
    materials = [make_material("iron", IRON, contrast=0.8), make_material("rust", RUST_ORANGE, contrast=0.7),
                 make_material("skid", WOOD_DARK, contrast=0.8), make_material("chain", RUST, contrast=0.8)]
    axle_y = 0.72 * M
    across = rotation_x(np.pi / 2)

    def parts() -> list[Part]:
        frames = lambda p: _union(*(np.minimum(np.minimum(capsule(p, (-0.42 * M, 0.2 * M, z), (0.0, axle_y + 0.12 * M, z), 0.06 * M),
                                                          capsule(p, (0.42 * M, 0.2 * M, z), (0.0, axle_y + 0.12 * M, z), 0.06 * M)),
                                               rounded_box(p, (0.0, 0.24 * M, z), (0.45 * M, 0.04 * M, 0.04 * M), 0.01 * M))
                                    for z in (-0.38 * M, 0.38 * M)))
        drum = lambda p: cylinder(p, (0.0, axle_y, 0.0), 0.22 * M, 0.34 * M, 0.02 * M, across)
        flanges = lambda p: _union(*(cylinder(p, (0.0, axle_y, z), 0.3 * M, 0.025 * M, 0.01 * M, across) for z in (-0.32 * M, 0.32 * M)))
        gear = lambda p: np.maximum(cylinder(p, (0.08 * M, axle_y - 0.05 * M, 0.5 * M), 0.4 * M, 0.035 * M, 0.01 * M, across),
                                    -cylinder(p, (0.08 * M, axle_y - 0.05 * M, 0.5 * M), 0.28 * M, 0.06 * M, 0.0, across))
        spokes = lambda p: _union(*(capsule(p, (0.08 * M, axle_y - 0.05 * M, 0.5 * M),
                                            (0.08 * M + 0.3 * M * np.cos(a), axle_y - 0.05 * M + 0.3 * M * np.sin(a), 0.5 * M), 0.03 * M)
                                    for a in np.linspace(0.3, 0.3 + 2 * np.pi, 5, endpoint=False)))
        crank = lambda p: np.minimum(capsule(p, (0.0, axle_y, -0.45 * M), (0.28 * M, axle_y + 0.22 * M, -0.5 * M), 0.035 * M),
                                     capsule(p, (0.28 * M, axle_y + 0.22 * M, -0.5 * M), (0.28 * M, axle_y + 0.22 * M, -0.72 * M), 0.035 * M))
        iron = lambda p: _union(frames(p), flanges(p), gear(p), spokes(p), crank(p))
        skid = lambda p: rounded_box(p, (0.0, 0.1 * M, 0.0), (0.75 * M, 0.1 * M, 0.52 * M), 0.02 * M)
        wound = painted(drum, bands(2, 0.075 * M, 0.035 * M), 0.025 * M)
        run = [np.array([0.2 * M, axle_y - 0.18 * M, 0.05 * M]), np.array([0.6 * M, 0.25 * M, 0.15 * M]),
               np.array([1.0 * M, 0.04 * M, 0.3 * M]), np.array([1.5 * M, 0.04 * M, 0.2 * M]), np.array([1.85 * M, 0.04 * M, 0.45 * M])]
        return [
            Part(skid, SKID),
            Part(iron, IRON_M),
            Part(painted(iron, noise_mask(0.12 * M, seed + 1, 0.35)), RUST_M, relief=False),
            Part(drum, IRON_M),
            Part(wound, CHAIN),
            Part(_branch_chain(run, 0.045 * M, 0.04 * M), CHAIN),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(110, 84), footprint=box_footprint(0.75 * M, 0.52 * M))


def plank_stack(stem: str, seed: int) -> PropModel:
    """Tas de planches du chantier : cinq lits séparés de liteaux, celui du dessus glissé de travers, une planche tombée devant."""
    PLANK, PLANK_LIGHT, GRAIN, BATTEN = range(4)
    materials = [make_material("plank", WOOD_OLD), make_material("plank_light", BLEACHED),
                 make_material("grain", WOOD_DARK, contrast=0.7), make_material("batten", WOOD_DARK, contrast=0.8)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        planks, battens = [], []
        for layer in range(5):
            y = 0.1 * M + layer * 0.11 * M
            slide = (w.uniform(0.2, 0.45) * M, rotation_y(w.uniform(-0.18, 0.18))) if layer == 4 else (w.uniform(-0.05, 0.05) * M, rotation_y(w.uniform(-0.02, 0.02)))
            for k, z in enumerate(np.linspace(-0.55, 0.55, 6)):
                if layer == 4 and k >= 4:
                    continue
                planks.append((np.array([slide[0], y + 0.04 * M, z * M]), slide[1], (layer + k) % 2))
            for x in (-0.95, 0.0, 0.95):
                battens.append(np.array([x * M, y - 0.025 * M, 0.0]))
        fallen = [(np.array([0.2 * M, 0.03 * M, 0.95 * M]), rotation_y(0.25))]

        def plank_shape(kind: int):
            return lambda p: _union(*(rounded_box(p, c, (1.3 * M, 0.03 * M, 0.11 * M), 0.01 * M, r) for c, r, k in planks if k == kind))

        lean_shape = lambda p: _union(*(rounded_box(p, c, (1.3 * M, 0.03 * M, 0.11 * M), 0.01 * M, r) for c, r in fallen))
        everything = lambda p: _union(plank_shape(0)(p), plank_shape(1)(p), lean_shape(p))
        return [
            Part(plank_shape(0), PLANK),
            Part(plank_shape(1), PLANK_LIGHT),
            Part(lean_shape, PLANK_LIGHT),
            Part(painted(everything, noise_mask(0.07 * M, seed + 1, 0.22, (7.0, 1.0, 1.0))), GRAIN, relief=False),
            Part(lambda p: _union(*(rounded_box(p, c, (0.05 * M, 0.025 * M, 0.62 * M), 0.008 * M) for c in battens)), BATTEN),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(110, 84), smooth_slopes=True, footprint=box_footprint(1.3 * M, 0.7 * M))


def shipyard_shed(stem: str, seed: int) -> PropModel:
    """
    La cabane du chantier : planches verticales peintes d'un vert passé, toit à un pan de tôle ondulée galvanisée que la
    rouille gagne, porte entrouverte sur le noir, carreau cassé, l'enseigne au-dessus de la porte, un tuyau de poêle.
    """
    WALL, BOARD, BARE, DOOR, GLASS, ROOF, RUST_M, SIGN, PIPE = range(9)
    materials = [make_material("wall", SHED_PAINT), make_material("board_joint", WOOD_DARK, contrast=0.6),
                 make_material("bare", WOOD_OLD, contrast=0.6), make_material("doorway", GLASS_DARK, contrast=0.4),
                 make_material("glass", "#4A5A64", contrast=0.5), make_material("roof", TIN, contrast=0.8),
                 make_material("rust", RUST_ORANGE, contrast=0.7), make_material("sign", PAINT_FADED, contrast=0.6),
                 make_material("pipe", IRON, contrast=0.8)]
    half = (1.45 * M, 1.2 * M, 1.1 * M)

    def parts() -> list[Part]:
        walls = lambda p: rounded_box(p, (0.0, half[1], 0.0), half, 0.03 * M)
        roof = lambda p: rounded_box(p, (0.0, 2.55 * M, 0.0), (1.72 * M, 0.05 * M, 1.4 * M), 0.02 * M, rotation_x(-0.2))
        joints = lambda p: np.minimum(bands(0, 0.25 * M, 0.03 * M)(p), bands(2, 0.25 * M, 0.03 * M)(p))
        front = lambda p: half[2] - 0.08 * M - p[:, 2]
        doorway = both(lambda p: np.abs(p[:, 0] - 0.55 * M) - 0.4 * M, lambda p: p[:, 1] - 1.95 * M, front)
        window = both(lambda p: np.abs(p[:, 0] + 0.7 * M) - 0.33 * M, lambda p: np.abs(p[:, 1] - 1.4 * M) - 0.28 * M, front)
        cracked = both(window, lambda p: np.abs((p[:, 0] + 0.7 * M) - (p[:, 1] - 1.4 * M) * 0.8) - 0.03 * M)
        leaf = lambda p: rounded_box(p, (1.2 * M, 0.98 * M, half[2] + 0.3 * M), (0.4 * M, 0.97 * M, 0.035 * M), 0.01 * M, rotation_y(0.9))
        sign = lambda p: rounded_box(p, (0.4 * M, 2.18 * M, half[2] + 0.02 * M), (0.7 * M, 0.13 * M, 0.025 * M), 0.01 * M)
        letters = both(lambda p: np.abs(p[:, 1] - 2.18 * M) - 0.05 * M, noise_mask(0.05 * M, seed + 3, 0.5, (1.0, 4.0, 1.0)))
        pipe = lambda p: cylinder(p, (-1.0 * M, 2.95 * M, -0.5 * M), 0.07 * M, 0.45 * M, 0.01 * M)
        corrugation = bands(2, 0.12 * M, 0.04 * M, frame=lambda p: p @ rotation_x(-0.2))
        return [
            Part(walls, WALL),
            Part(painted(walls, joints), BOARD, relief=False),
            Part(painted(walls, noise_mask(0.3 * M, seed + 1, 0.22)), BARE, relief=False),
            Part(painted(walls, doorway, 0.02 * M), DOOR, relief=False),
            Part(painted(walls, window, 0.02 * M), GLASS, relief=False),
            Part(painted(walls, cracked, 0.025 * M), DOOR, relief=False),
            Part(leaf, BARE),
            Part(roof, ROOF),
            Part(painted(roof, corrugation), PIPE, relief=False),
            Part(painted(roof, noise_mask(0.25 * M, seed + 2, 0.4), 0.02 * M), RUST_M, relief=False),
            Part(sign, SIGN),
            Part(painted(sign, letters), BOARD, relief=False),
            Part(pipe, PIPE),
        ]

    return PropModel(stem, parts, materials, AXIS_Y_YAW, ray_range=180.0, canvas=(150, 150), footprint=box_footprint(half[0], half[2]))


def quay_edge(stem: str, seed: int) -> PropModel:
    """
    Bordure du quai, au bord de la mer oubliée : quatre blocs de granit taillé, un peu déjointés, un épaufré, lichen et
    taches d'humidité, un anneau d'amarrage scellé. Bas : on le longe et on y pousse la horde dans le vide.
    """
    STONE_M, LICHEN_M, WET, RING = range(4)
    materials = [make_material("granite", GRANITE), make_material("lichen", "#6E7A4A", contrast=0.6),
                 make_material("wet", "#5E5E5A", contrast=0.6), make_material("ring", RUST, contrast=0.8)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        blocks = [((x * M, 0.22 * M + w.uniform(-0.03, 0.02) * M, w.uniform(-0.04, 0.04) * M), rotation_y(w.uniform(-0.035, 0.035)))
                  for x in (-1.68, -0.56, 0.56, 1.68)]
        stone = lambda p: np.maximum(_union(*(rounded_box(p, c, (0.54 * M, 0.22 * M, 0.4 * M), 0.04 * M, r) for c, r in blocks)),
                                     -sphere(p, (0.95 * M, 0.5 * M, 0.38 * M), 0.22 * M))
        ring = lambda p: np.abs(cylinder(p, (-0.6 * M, 0.46 * M, 0.05 * M), 0.12 * M, 0.015 * M)) - 0.025 * M
        return [
            Part(stone, STONE_M),
            Part(painted(stone, noise_mask(0.2 * M, seed + 1, 0.12)), LICHEN_M, relief=False),
            Part(painted(stone, both(noise_mask(0.3 * M, seed + 2, 0.4), lambda p: p[:, 1] - 0.25 * M)), WET, relief=False),
            Part(ring, RING),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(140, 64), smooth_slopes=True)


def quay_paving(stem: str, seed: int) -> PropModel:
    """Pavés du quai : un pan de granit en rangées décalées, joints sombres, pavés arrachés laissant la vase, herbe dans les joints. À plat."""
    STONE_M, JOINT, HOLE, WEED = range(4)
    materials = [make_material("sett", GRANITE, contrast=0.6), make_material("joint", "#55524C", contrast=0.5),
                 make_material("mud", MUD, contrast=0.5), make_material("weed", MOSS_LIGHT, contrast=0.6)]
    half = (2.2 * M, 1.1 * M)

    def slab(p: np.ndarray) -> np.ndarray:
        box = rounded_box(p, (0.0, 0.03 * M, 0.0), (half[0], 0.03 * M, half[1]), 0.01 * M)
        reach = np.maximum(np.abs(p[:, 0]) / half[0], np.abs(p[:, 2]) / half[1])
        return np.maximum(box, (reach - 0.86 - 0.3 * (value_noise(p, 0.45 * M, seed) - 0.5)) * half[1])

    def parts() -> list[Part]:
        row = lambda p: np.floor(p[:, 2] / (0.22 * M))
        cross = lambda p: (lambda phase: np.minimum(phase, 0.3 * M - phase) - 0.015 * M)(np.mod(p[:, 0] + row(p) * 0.15 * M, 0.3 * M))
        joints = lambda p: np.minimum(bands(2, 0.22 * M, 0.03 * M)(p), cross(p))
        w = Weathering(seed)
        tufts = [(w.uniform(-1.8, 1.8) * M, w.uniform(-0.8, 0.8) * M) for _ in range(7)]
        return [
            Part(slab, STONE_M),
            Part(painted(slab, joints), JOINT, relief=False),
            Part(painted(slab, noise_mask(0.35 * M, seed + 1, 0.16), 0.016 * M), HOLE, relief=False),
            Part(lambda p: _union(*(capsule(p, (x, 0.04 * M, z), (x + 0.04 * M, 0.16 * M, z), 0.035 * M, 0.01 * M) for x, z in tufts)), WEED),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(160, 64), smooth_slopes=True)


def bollard(stem: str, seed: int) -> PropModel:
    """Bitte d'amarrage en fonte : socle, fût à taille, chapeau débordant ; un bout de cordage encore tourné autour, qui retombe au sol."""
    IRON_M, RUST_M, ROPE = range(3)
    materials = [make_material("iron", "#4A4848", contrast=0.8), make_material("rust", RUST, contrast=0.7),
                 make_material("rope", FUNGUS, contrast=0.7)]

    def parts() -> list[Part]:
        body = lambda p: _union(cylinder(p, (0.0, 0.07 * M, 0.0), 0.36 * M, 0.07 * M, 0.02 * M),
                                cylinder(p, (0.0, 0.5 * M, 0.0), 0.22 * M, 0.4 * M, 0.02 * M),
                                ellipsoid(p, (0.05 * M, 0.92 * M, 0.0), (0.33 * M, 0.11 * M, 0.3 * M)))
        loop = lambda p: np.abs(cylinder(p, (0.0, 0.56 * M, 0.0), 0.27 * M, 0.035 * M)) - 0.035 * M
        tail = [np.array([0.26 * M, 0.54 * M, 0.1 * M]), np.array([0.45 * M, 0.12 * M, 0.3 * M]), np.array([0.8 * M, 0.03 * M, 0.35 * M]),
                np.array([1.15 * M, 0.03 * M, 0.15 * M])]
        return [
            Part(body, IRON_M),
            Part(painted(body, noise_mask(0.1 * M, seed + 1, 0.12, (1.0, 4.0, 1.0))), RUST_M, relief=False),
            Part(lambda p: np.minimum(loop(p), _branch_chain(tail, 0.04 * M, 0.035 * M)(p)), ROPE),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(72, 64), footprint=box_footprint(0.36 * M, 0.36 * M))


def harbor_crane(stem: str, seed: int) -> PropModel:
    """
    La grue du port : portique de quatre jambes contreventées, cabine tournante aux vitres crevées, contrepoids de béton,
    flèche en treillis tendue vers le large (le vide), le crochet encore pendu au bout de son câble. Jaune écaillé, rouille.
    """
    PAINT, RUST_M, STEEL, CABIN, WINDOW, WEIGHT, CABLE = range(7)
    materials = [make_material("paint", CRANE_YELLOW), make_material("rust", RUST_ORANGE, contrast=0.7),
                 make_material("steel", IRON, contrast=0.8), make_material("cabin", CRANE_YELLOW),
                 make_material("window", GLASS_DARK, contrast=0.4), make_material("counterweight", STONE, contrast=0.7),
                 make_material("cable", "#2E2C2C", contrast=0.6)]
    top = 3.1 * M
    root = np.array([0.0, 3.7 * M, -0.8 * M])
    tip = np.array([0.0, 8.6 * M, -5.6 * M])

    def parts() -> list[Part]:
        legs = [(x * M, z * M) for x in (-0.9, 0.9) for z in (-0.9, 0.9)]
        leg_shape = lambda p: _union(*(rounded_box(p, (x, top * 0.5, z), (0.12 * M, top * 0.5, 0.12 * M), 0.02 * M) for x, z in legs))
        braces = [((-0.9 * M, 0.3 * M, z * M), (0.9 * M, top - 0.3 * M, z * M)) for z in (-0.9, 0.9)]
        braces += [((x * M, 0.3 * M, -0.9 * M), (x * M, top - 0.3 * M, 0.9 * M)) for x in (-0.9, 0.9)]
        platform = lambda p: rounded_box(p, (0.0, top + 0.1 * M, 0.0), (1.1 * M, 0.12 * M, 1.1 * M), 0.02 * M)
        cabin = lambda p: rounded_box(p, (0.1 * M, top + 0.95 * M, 0.2 * M), (0.85 * M, 0.75 * M, 0.95 * M), 0.05 * M)
        windows = both(lambda p: np.abs(p[:, 1] - top - 1.2 * M) - 0.26 * M, lambda p: 0.04 * M - np.abs(np.mod(p[:, 0], 0.5 * M) - 0.25 * M))
        weight = lambda p: rounded_box(p, (0.1 * M, top + 0.7 * M, 1.4 * M), (0.7 * M, 0.45 * M, 0.3 * M), 0.03 * M)
        chords, lattice = [], []
        for side in (-1.0, 1.0):
            a = root + np.array([side * 0.32 * M, 0.0, 0.0])
            b = tip + np.array([side * 0.08 * M, 0.0, 0.0])
            chords.append((a, b))
        for k in range(9):
            t0, t1 = k / 9.0, (k + 1) / 9.0
            side = 1.0 if k % 2 == 0 else -1.0
            p0 = root + (tip - root) * t0 + np.array([side * (0.32 - 0.24 * t0) * M, 0.0, 0.0])
            p1 = root + (tip - root) * t1 + np.array([-side * (0.32 - 0.24 * t1) * M, 0.0, 0.0])
            lattice.append((p0, p1))
        hook_at = np.array([0.0, 4.4 * M, tip[2]])
        steelwork = lambda p: _union(leg_shape(p), platform(p), *(capsule(p, a, b, 0.06 * M) for a, b in braces + chords),
                                     *(capsule(p, a, b, 0.035 * M) for a, b in lattice))
        return [
            Part(steelwork, PAINT),
            Part(painted(steelwork, noise_mask(0.18 * M, seed + 1, 0.18, (1.0, 4.0, 1.0)), 0.02 * M), RUST_M, relief=False),
            Part(cabin, CABIN),
            Part(painted(cabin, windows), WINDOW, relief=False),
            Part(painted(cabin, noise_mask(0.2 * M, seed + 2, 0.15, (1.0, 6.0, 1.0)), 0.02 * M), RUST_M, relief=False),
            Part(weight, WEIGHT),
            Part(lambda p: capsule(p, tip, hook_at + np.array([0.0, 0.25 * M, 0.0]), 0.03 * M), CABLE),
            Part(lambda p: np.minimum(rounded_box(p, hook_at + np.array([0.0, 0.18 * M, 0.0]), (0.14 * M, 0.12 * M, 0.08 * M), 0.02 * M),
                                      np.abs(cylinder(p, hook_at - np.array([0.0, 0.05 * M, 0.0]), 0.1 * M, 0.03 * M, 0.0, rotation_x(np.pi / 2))) - 0.03 * M), STEEL),
        ]

    return PropModel(stem, parts, materials, float(np.radians(20.0)), ray_range=float(2.5 * tip[1]), canvas=(200, 290),
                     footprint=box_footprint(1.0 * M, 1.0 * M), supersample=3,
                     bounds=((-1.6 * M, -0.1 * M, -6.2 * M), (1.6 * M, tip[1] + 0.3 * M, 2.0 * M)))


def upturned_boat(stem: str, seed: int) -> PropModel:
    """Barque retournée sur deux chevalets, quille en l'air : bordé à clins, peinture bleu-gris passée qui s'écaille sur le bois, un trou dans les fonds."""
    WOOD, PAINT, STRAKE, KEEL, TRESTLE, HOLE = range(6)
    materials = [make_material("wood", WOOD_DARK, contrast=0.7), make_material("paint", "#7E949C", contrast=0.7),
                 make_material("strake", WOOD_DARK, contrast=0.6), make_material("keel", WOOD_OLD, contrast=0.8),
                 make_material("trestle", WOOD_DARK, contrast=0.8), make_material("hole", GLASS_DARK, contrast=0.4)]
    gunwale = 0.42 * M

    def hull(p: np.ndarray) -> np.ndarray:
        q = p - np.array([0.0, gunwale, 0.0])
        t = np.maximum(1.0 - np.clip(q[:, 0] / (1.9 * M), 0.0, 1.0) ** 2 * 0.6, 0.3)
        scaled = np.stack([q[:, 0], q[:, 1], q[:, 2] / t], axis=1)
        return np.maximum(ellipsoid(scaled, (0.0, 0.0, 0.0), (1.9 * M, 0.55 * M, 0.72 * M)) * t, -q[:, 1])

    def parts() -> list[Part]:
        trestles = lambda p: _union(*(rounded_box(p, (x * M, gunwale * 0.5, 0.0), (0.1 * M, gunwale * 0.5, 0.82 * M), 0.02 * M) for x in (-0.95, 0.85)))
        keel = lambda p: capsule(p, (-1.75 * M, gunwale + 0.56 * M, 0.0), (1.8 * M, gunwale + 0.5 * M, 0.0), 0.05 * M)
        return [
            Part(hull, WOOD),
            Part(painted(hull, noise_mask(0.22 * M, seed + 1, 0.82)), PAINT, relief=False),
            Part(painted(hull, bands(1, 0.13 * M, 0.025 * M), 0.016 * M), STRAKE, relief=False),
            Part(painted(hull, both(lambda p: np.linalg.norm(p[:, [0, 2]] - np.array([0.5 * M, 0.25 * M]), axis=1) - 0.16 * M,
                                    lambda p: gunwale + 0.25 * M - p[:, 1]), 0.02 * M), HOLE, relief=False),
            Part(keel, KEEL),
            Part(trestles, TRESTLE),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(120, 80), footprint=box_footprint(1.8 * M, 0.72 * M))


def fish_crates(stem: str, seed: int) -> PropModel:
    """Caisses à poisson en plastique : une pile de trois, deux à côté, une renversée ; bleu de criée et rouge passé, poignées ajourées, crasse au fond."""
    BLUE, RED, SLOT, GRIME = range(4)
    materials = [make_material("crate_blue", CRATE_BLUE), make_material("crate_red", CRATE_RED),
                 make_material("slot", GLASS_DARK, contrast=0.4), make_material("grime", MUD, contrast=0.6)]
    half = np.array([0.32, 0.13, 0.22]) * M

    def parts() -> list[Part]:
        w = Weathering(seed)
        crates = [(np.array([-0.38 * M, half[1] + k * 2 * half[1], 0.0]), rotation_y(w.uniform(-0.08, 0.08)), k % 2) for k in range(3)]
        crates += [(np.array([0.36 * M, half[1], 0.06 * M]), rotation_y(0.1), 1), (np.array([0.4 * M, 3 * half[1], 0.02 * M]), rotation_y(-0.12), 0)]
        crates.append((np.array([1.05 * M, half[2], 0.45 * M]), rotation_y(0.8) @ rotation_x(np.pi / 2 + 0.1), 0))

        def crate_shape(kind: int):
            def shape(p: np.ndarray) -> np.ndarray:
                result = []
                for c, r, k in crates:
                    if k != kind:
                        continue
                    q = (p - c) @ r
                    box = rounded_box(q, (0.0, 0.0, 0.0), tuple(half), 0.02 * M)
                    inner = rounded_box(q, (0.0, 0.05 * M, 0.0), tuple(half - np.array([0.03, 0.0, 0.03]) * M), 0.01 * M)
                    result.append(np.maximum(box, -inner))
                return _union(*result)
            return shape

        slots = both(lambda p: np.abs(np.mod(p[:, 1], 2 * half[1]) - 1.45 * half[1]) - 0.035 * M,
                     lambda p: np.abs(np.mod(p[:, 0] + p[:, 2], 0.3 * M) - 0.15 * M) - 0.06 * M)
        everything = lambda p: np.minimum(crate_shape(0)(p), crate_shape(1)(p))
        return [
            Part(crate_shape(0), BLUE),
            Part(crate_shape(1), RED),
            Part(painted(everything, slots), SLOT, relief=False),
            Part(painted(everything, noise_mask(0.15 * M, seed + 1, 0.25)), GRIME, relief=False),
        ]

    return PropModel(stem, parts, materials, AXIS_Y_YAW, canvas=(100, 76), footprint=box_footprint(0.7 * M, 0.35 * M))


# Les marais salants et la digue (plan 58 T2c) : le sel que plus personne ne récolte a grisé, la digue a cédé.
SALT_GREY = "#9C9A90"
SALT_DIRTY = "#7E786C"
LIME_WASH = "#C4BCA8"
ROOF_TILE = "#8A5C46"
DRY_STONE = "#7E7A6C"
SACK = "#8E7E5C"
GRASS_TOP = "#5A7A40"


def salt_pile(stem: str, seed: int) -> PropModel:
    """Tas de sel laissé au bord des bassins : un cône tassé, gris, sali à la base, croûté et raviné par les pluies, quelques cristaux clairs."""
    SALT, DIRT, CRUST, BRIGHT = range(4)
    materials = [make_material("salt", SALT_GREY, contrast=0.6), make_material("dirt", SALT_DIRTY, contrast=0.6),
                 make_material("crust", "#8E8A7E", contrast=0.5), make_material("bright", "#D4D2C8", contrast=0.4)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        lean = w.uniform(-0.12, 0.12) * M
        heap = lambda p: np.maximum(capsule(p, (0.0, 0.25 * M, 0.0), (lean, 1.3 * M, 0.0), 1.2 * M, 0.12 * M), -p[:, 1])
        gullies = bands(0, 0.22 * M, 0.025 * M, frame=lambda p: p * np.array([1.0, 0.3, 1.0]))
        return [
            Part(heap, SALT),
            Part(painted(heap, both(lambda p: p[:, 1] - 0.4 * M, noise_mask(0.2 * M, seed + 1, 0.6))), DIRT, relief=False),
            Part(painted(heap, noise_mask(0.3 * M, seed + 3, 0.35)), CRUST, relief=False),
            Part(painted(heap, both(gullies, lambda p: p[:, 1] - 0.9 * M, noise_mask(0.25 * M, seed + 4, 0.5))), DIRT, relief=False),
            Part(painted(heap, noise_mask(0.06 * M, seed + 2, 0.1)), BRIGHT, relief=False),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(100, 84), footprint=box_footprint(1.1 * M, 1.1 * M))


def salt_pan(stem: str, seed: int) -> PropModel:
    """
    Une saline : bassin rectangulaire peu profond bordé d'une diguette d'argile tassée, l'eau laiteuse et immobile, une
    croûte de sel gris prise sur les bords et en plaques. À plat sur le sol : l'eau du terrain dessous ralentit, elle
    donne la forme.
    """
    CLAY, CLAY_DARK, WATER, CRUST, STREAK = range(5)
    materials = [make_material("clay", "#7A6E58", contrast=0.6), make_material("clay_dark", "#5A5042", contrast=0.5),
                 make_material("brine", "#5E7470", contrast=0.4), make_material("crust", SALT_GREY, contrast=0.4),
                 make_material("streak", WATER_MILK, contrast=0.3)]
    half = (6.45 * M, 5.4 * M)
    rim = 0.6 * M

    def parts() -> list[Part]:
        outer = lambda p: rounded_box(p, (0.0, 0.06 * M, 0.0), (half[0], 0.06 * M, half[1]), 0.04 * M)
        inner = lambda p: rounded_box(p, (0.0, 0.06 * M, 0.0), (half[0] - rim, 0.1 * M, half[1] - rim), 0.1 * M)
        bank = lambda p: np.maximum(outer(p), -inner(p))
        brine = lambda p: rounded_box(p, (0.0, 0.02 * M, 0.0), (half[0] - rim, 0.02 * M, half[1] - rim), 0.05 * M)
        edge = lambda p: -np.minimum(half[0] - rim - np.abs(p[:, 0]), half[1] - rim - np.abs(p[:, 2])) - 0.0 * M
        crust_edge = lambda p: -edge(p) - 0.45 * M - 1.0 * M * (value_noise(p, 0.9 * M, seed) - 0.5)
        return [
            Part(bank, CLAY),
            Part(painted(bank, noise_mask(0.4 * M, seed + 1, 0.35)), CLAY_DARK, relief=False),
            Part(brine, WATER),
            Part(painted(brine, crust_edge), CRUST, relief=False),
            Part(painted(brine, noise_mask(1.1 * M, seed + 2, 0.1)), CRUST, relief=False),
            Part(painted(brine, both(noise_mask(0.7 * M, seed + 3, 0.05, (3.0, 1.0, 0.6)), lambda p: -edge(p) - 1.0 * M)), STREAK,
                 relief=False),
        ]

    return PropModel(stem, parts, materials, FRONT, ray_range=float(3.0 * half[1]), canvas=(340, 220), smooth_slopes=True)


def salt_rake(stem: str, seed: int) -> PropModel:
    """Le las du saunier, couché au bord d'un bassin : un long manche, la planchette qui ramène le sel, du sel gris resté collé."""
    WOOD, SALT = range(2)
    materials = [make_material("wood", WOOD_OLD, contrast=0.7), make_material("salt", SALT_GREY, contrast=0.5)]

    def parts() -> list[Part]:
        handle = lambda p: capsule(p, (-1.6 * M, 0.05 * M, 0.0), (1.1 * M, 0.08 * M, 0.0), 0.035 * M)
        blade = lambda p: rounded_box(p, (1.18 * M, 0.1 * M, 0.0), (0.05 * M, 0.08 * M, 0.42 * M), 0.015 * M)
        crust = lambda p: rounded_box(p, (1.26 * M, 0.04 * M, 0.0), (0.06 * M, 0.04 * M, 0.38 * M), 0.03 * M)
        return [Part(lambda p: np.minimum(handle(p), blade(p)), WOOD), Part(crust, SALT)]

    return PropModel(stem, parts, materials, float(np.radians(-28.0)), canvas=(110, 56))


def wheelbarrow(stem: str, seed: int) -> PropModel:
    """Brouette de bois du saunier, une roue cerclée de fer, les brancards au sol, pleine d'un sel gris qui a pris en bloc."""
    WOOD, IRON_M, SALT, RUST_M = range(4)
    materials = [make_material("wood", WOOD_OLD, contrast=0.7), make_material("iron", IRON, contrast=0.8),
                 make_material("salt", SALT_GREY, contrast=0.5), make_material("rust", RUST, contrast=0.7)]

    def parts() -> list[Part]:
        tilt = rotation_z(-0.12)
        box = lambda p: rounded_box(p, (0.1 * M, 0.55 * M, 0.0), (0.62 * M, 0.2 * M, 0.42 * M), 0.03 * M, tilt)
        hollow = lambda p: rounded_box(p, (0.1 * M, 0.66 * M, 0.0), (0.56 * M, 0.2 * M, 0.36 * M), 0.02 * M, tilt)
        tub = lambda p: np.maximum(box(p), -hollow(p))
        load = lambda p: np.maximum(ellipsoid(p, (0.1 * M, 0.62 * M, 0.0), (0.56 * M, 0.2 * M, 0.36 * M), tilt), -box(p) - 0.2 * M)
        shafts = lambda p: _union(*(capsule(p, (0.75 * M, 0.38 * M, z), (-1.0 * M, 0.06 * M, z), 0.035 * M) for z in (-0.3 * M, 0.3 * M)))
        legs = lambda p: _union(*(capsule(p, (-0.35 * M, 0.4 * M, z), (-0.42 * M, 0.02 * M, z), 0.03 * M) for z in (-0.3 * M, 0.3 * M)))
        wheel = lambda p: cylinder(p, (0.9 * M, 0.25 * M, 0.0), 0.25 * M, 0.05 * M, 0.01 * M, rotation_x(np.pi / 2))
        rim = lambda p: np.maximum(wheel(p) - 0.005 * M, 0.2 * M - np.hypot(p[:, 0] - 0.9 * M, p[:, 1] - 0.25 * M))
        return [
            Part(tub, WOOD),
            Part(painted(tub, noise_mask(0.15 * M, seed + 1, 0.15)), RUST_M, relief=False),
            Part(load, SALT),
            Part(lambda p: np.minimum(shafts(p), legs(p)), WOOD),
            Part(wheel, WOOD),
            Part(rim, IRON_M),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(100, 76), footprint=box_footprint(0.95 * M, 0.45 * M))


def saltworker_hut(stem: str, seed: int) -> PropModel:
    """
    La cabane du saunier : murs de moellons passés à la chaux qui s'écaille sur la pierre, toit de tuiles creuses à deux
    pans dont il manque des tuiles, porte basse fermée d'un cadenas, un banc de pierre contre le mur.
    """
    WALL, STONE_M, ROOF, GAP, DOOR, SILL = range(6)
    materials = [make_material("lime", LIME_WASH, contrast=0.6), make_material("stone", DRY_STONE, contrast=0.7),
                 make_material("tiles", ROOF_TILE, contrast=0.8), make_material("gap", "#3E3430", contrast=0.4),
                 make_material("door", WOOD_DARK, contrast=0.6), make_material("sill", STONE, contrast=0.6)]
    half = (1.3 * M, 1.0 * M, 1.0 * M)

    def parts() -> list[Part]:
        walls = lambda p: rounded_box(p, (0.0, half[1], 0.0), half, 0.06 * M)
        slope_l = lambda p: rounded_box(p, (0.0, 2.38 * M, -0.6 * M), (1.5 * M, 0.06 * M, 0.78 * M), 0.02 * M, rotation_x(0.58))
        slope_r = lambda p: rounded_box(p, (0.0, 2.38 * M, 0.6 * M), (1.5 * M, 0.06 * M, 0.78 * M), 0.02 * M, rotation_x(-0.58))
        roof = lambda p: np.minimum(slope_l(p), slope_r(p))
        gable = lambda p: np.maximum(rounded_box(p, (0.0, 2.3 * M, 0.0), (1.28 * M, 0.4 * M, 0.98 * M), 0.02 * M),
                                     p[:, 1] - 2.75 * M + np.abs(p[:, 2]) * 0.65)
        channels = bands(0, 0.2 * M, 0.06 * M)
        front = lambda p: half[2] - 0.08 * M - p[:, 2]
        door = both(lambda p: np.abs(p[:, 0] - 0.35 * M) - 0.32 * M, lambda p: p[:, 1] - 1.45 * M, front)
        bench = lambda p: rounded_box(p, (-0.65 * M, 0.25 * M, half[2] + 0.2 * M), (0.45 * M, 0.08 * M, 0.17 * M), 0.02 * M)
        bench_feet = lambda p: _union(*(rounded_box(p, (x * M, 0.1 * M, half[2] + 0.2 * M), (0.07 * M, 0.1 * M, 0.13 * M), 0.01 * M)
                                        for x in (-0.95, -0.35)))
        return [
            Part(lambda p: np.minimum(walls(p), gable(p)), WALL),
            Part(painted(lambda p: np.minimum(walls(p), gable(p)), both(noise_mask(0.18 * M, seed + 1, 0.3),
                                                                        lambda p: p[:, 1] - 0.9 * M)), STONE_M, relief=False),
            Part(painted(walls, noise_mask(0.1 * M, seed + 3, 0.06)), STONE_M, relief=False),
            Part(roof, ROOF),
            Part(painted(roof, channels), GAP, relief=False),
            Part(painted(roof, noise_mask(0.18 * M, seed + 2, 0.08), 0.03 * M), GAP, relief=False),
            Part(painted(walls, door, 0.02 * M), DOOR, relief=False),
            Part(lambda p: np.minimum(bench(p), bench_feet(p)), SILL),
        ]

    return PropModel(stem, parts, materials, AXIS_Y_YAW, ray_range=160.0, canvas=(130, 130), footprint=box_footprint(half[0], half[2]))


def sluice_gate(stem: str, seed: int) -> PropModel:
    """Vanne entre deux bassins : deux poteaux, la planche coulissante relevée, sa crémaillère et la manivelle rouillée."""
    WOOD, IRON_M, RUST_M = range(3)
    materials = [make_material("wood", WET_WOOD, contrast=0.7), make_material("iron", IRON, contrast=0.8),
                 make_material("rust", RUST_ORANGE, contrast=0.7)]

    def parts() -> list[Part]:
        posts = lambda p: _union(*(rounded_box(p, (x * M, 0.6 * M, 0.0), (0.08 * M, 0.6 * M, 0.08 * M), 0.015 * M) for x in (-0.5, 0.5)))
        beam = lambda p: rounded_box(p, (0.0, 1.2 * M, 0.0), (0.62 * M, 0.07 * M, 0.09 * M), 0.015 * M)
        board = lambda p: rounded_box(p, (0.0, 0.62 * M, 0.0), (0.42 * M, 0.26 * M, 0.04 * M), 0.01 * M)
        rack = lambda p: rounded_box(p, (0.0, 1.32 * M, 0.0), (0.03 * M, 0.42 * M, 0.03 * M), 0.008 * M)
        crank = lambda p: _union(cylinder(p, (0.12 * M, 1.32 * M, 0.0), 0.1 * M, 0.02 * M, 0.005 * M, rotation_z(np.pi / 2)),
                                 capsule(p, (0.14 * M, 1.32 * M, 0.0), (0.14 * M, 1.44 * M, 0.1 * M), 0.02 * M))
        iron = lambda p: np.minimum(rack(p), crank(p))
        return [
            Part(lambda p: _union(posts(p), beam(p), board(p)), WOOD),
            Part(iron, IRON_M),
            Part(painted(iron, noise_mask(0.06 * M, seed + 1, 0.4)), RUST_M, relief=False),
        ]

    return PropModel(stem, parts, materials, THREE_QUARTER, canvas=(72, 90), footprint=box_footprint(0.6 * M, 0.12 * M))


def dike_wall(stem: str, seed: int) -> PropModel:
    """
    Un tronçon de la digue : un talus de pierres sèches à fruit, assises irrégulières, joints vides, le dessus enherbé ;
    des pierres manquent ici et là. Bloquant : on passe par la brèche.
    """
    STONE_M, JOINT, GRASS, MOSS = range(4)
    materials = [make_material("stone", DRY_STONE, contrast=0.7), make_material("joint", "#4A463E", contrast=0.5),
                 make_material("grass", GRASS_TOP, contrast=0.7), make_material("moss", "#6A7448", contrast=0.6)]
    length, height, base = 2.1 * M, 1.1 * M, 0.95 * M

    def bank(p: np.ndarray) -> np.ndarray:
        taper = base - (base - 0.45 * M) * np.clip(p[:, 1] / height, 0.0, 1.0)
        body = np.maximum(np.maximum(np.abs(p[:, 0]) - length, np.abs(p[:, 2]) - taper), np.maximum(-p[:, 1], p[:, 1] - height))
        bumps = 0.08 * M * (value_noise(p, 0.3 * M, seed) - 0.5)
        return body + bumps

    def parts() -> list[Part]:
        courses = bands(1, 0.22 * M, 0.035 * M)
        heads = lambda p: (lambda row: (lambda phase: np.minimum(phase, 0.38 * M - phase) - 0.025 * M)(
            np.mod(p[:, 0] + row * 0.17 * M, 0.38 * M)))(np.floor(p[:, 1] / (0.22 * M)))
        joints = lambda p: np.minimum(courses(p), heads(p))
        top = lambda p: np.maximum(ellipsoid(p, (0.0, height, 0.0), (length + 0.05 * M, 0.16 * M, 0.5 * M)), -bank(p) - 0.12 * M)
        return [
            Part(bank, STONE_M),
            Part(painted(bank, joints), JOINT, relief=False),
            Part(painted(bank, noise_mask(0.3 * M, seed + 1, 0.06), 0.04 * M), JOINT, relief=False),
            Part(painted(bank, both(noise_mask(0.25 * M, seed + 2, 0.4), lambda p: p[:, 1] - 0.4 * M)), MOSS, relief=False),
            Part(top, GRASS),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(150, 90), footprint=box_footprint(length, base))


def dike_rubble(stem: str, seed: int) -> PropModel:
    """Les pierres de la brèche, roulées par l'eau : des blocs épars et basculés, un pan de talus effondré, de la vase entre eux."""
    STONE_M, MOSS, MUD_M = range(3)
    materials = [make_material("stone", DRY_STONE, contrast=0.7), make_material("moss", "#6A7448", contrast=0.6),
                 make_material("mud", MUD, contrast=0.5)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        blocks = [((x * M + w.uniform(-0.12, 0.12) * M, w.uniform(0.2, 0.32) * M, w.uniform(-0.45, 0.45) * M),
                   (w.uniform(0.24, 0.36) * M, w.uniform(0.2, 0.28) * M, w.uniform(0.2, 0.3) * M),
                   rotation_y(w.uniform(0, 3.0)) @ rotation_z(w.uniform(-0.6, 0.6)))
                  for x in (-1.0, -0.6, -0.2, 0.25, 0.7, 1.05, -0.4, 0.45)]
        blocks += [((x * M, 0.6 * M, w.uniform(-0.15, 0.15) * M), (0.28 * M, 0.2 * M, 0.24 * M), rotation_y(w.uniform(0, 3.0)) @ rotation_x(0.4))
                   for x in (-0.55, 0.3)]
        stones = lambda p: _union(*(rounded_box(p, c, h, 0.05 * M, r) for c, h, r in blocks))
        slump = lambda p: np.maximum(ellipsoid(p, (-0.2 * M, 0.0, 0.0), (1.0 * M, 0.25 * M, 0.5 * M)), -p[:, 1])
        mud = lambda p: np.maximum(ellipsoid(p, (0.2 * M, 0.0, 0.1 * M), (1.4 * M, 0.06 * M, 0.75 * M)), -p[:, 1])
        return [
            Part(mud, MUD_M),
            Part(lambda p: np.minimum(stones(p), slump(p)), STONE_M),
            Part(painted(lambda p: np.minimum(stones(p), slump(p)), both(noise_mask(0.2 * M, seed + 1, 0.3), lambda p: p[:, 1] - 0.25 * M)),
                 MOSS, relief=False),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(120, 84), footprint=box_footprint(1.2 * M, 0.6 * M))


def sandbags(stem: str, seed: int) -> PropModel:
    """Sacs de sable entassés à la hâte au bord de la brèche, deux rangs décalés, toile pourrie, un sac crevé qui a coulé."""
    CLOTH, DARK, SAND = range(3)
    materials = [make_material("sack", SACK, contrast=0.6), make_material("damp", "#5E5442", contrast=0.5),
                 make_material("sand", "#A08E66", contrast=0.5)]

    def parts() -> list[Part]:
        w = Weathering(seed)
        bags = [((x * M + w.uniform(-0.04, 0.04) * M, 0.14 * M, w.uniform(-0.05, 0.05) * M), rotation_y(w.uniform(-0.15, 0.15)))
                for x in (-0.84, -0.28, 0.28, 0.84)]
        bags += [(((x + 0.03) * M, 0.38 * M, w.uniform(-0.04, 0.04) * M), rotation_y(w.uniform(-0.2, 0.2))) for x in (-0.56, 0.0, 0.56)]
        sacks = lambda p: _union(*(ellipsoid(p, c, (0.3 * M, 0.13 * M, 0.2 * M), r) for c, r in bags))
        spill = lambda p: np.maximum(ellipsoid(p, (1.25 * M, 0.0, 0.15 * M), (0.35 * M, 0.08 * M, 0.25 * M)), -p[:, 1])
        burst = lambda p: ellipsoid(p, (1.12 * M, 0.08 * M, 0.0), (0.24 * M, 0.08 * M, 0.18 * M))
        return [
            Part(lambda p: np.minimum(sacks(p), burst(p)), CLOTH),
            Part(painted(lambda p: np.minimum(sacks(p), burst(p)), lambda p: p[:, 1] - 0.12 * M), DARK, relief=False),
            Part(spill, SAND),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(110, 60), footprint=box_footprint(1.15 * M, 0.25 * M))


# Les maisons du port (plan 58 T2d) : du courrier que plus personne ne relève.
FISHER_PLASTER = "#CEC8B6"
POST_YELLOW = "#B89A42"
LETTER = "#D8D2C0"
STAMP_RED = "#9A3A30"
BEACON_RED = "#8E4436"


def fisher_house(stem: str, seed: int, width_cells: int, damage: float, mirrored: bool) -> PropModel:
    """Maison de pêcheur basse : plain-pied, crépi blanc sali par les embruns, tuiles, volets peints qui s'écaillent."""
    return building(BuildingSpec(stem, width_cells, 3, 1, "house", FISHER_PLASTER, damage, seed, mirrored, window_spacing=2.0 * M))


def port_mailbox(stem: str, seed: int) -> PropModel:
    """
    Boîte aux lettres d'une maison du port : tôle jaune passé sur un poteau, la fente bourrée de lettres qui débordent,
    un tas d'enveloppes à ses pieds, tamponnées en rouge « retour à l'envoyeur », que la pluie a collées entre elles.
    """
    POST, BOX, RUST_M, PAPER, STAMP = range(5)
    materials = [make_material("post", "#6E5E4A"), make_material("box", POST_YELLOW, contrast=0.5), make_material("rust", RUST, contrast=0.7),
                 make_material("paper", LETTER, contrast=0.5), make_material("stamp", STAMP_RED, contrast=0.6)]
    w = Weathering(seed)
    lean = rotation_z(w.uniform(-0.1, -0.05))
    letters = [((w.uniform(-0.6, 0.65) * M, 0.02 * M + i * 0.01 * M, w.uniform(-0.1, 0.6) * M), rotation_y(w.uniform(0, 3.1)))
               for i in range(22)]
    stuck = [((w.uniform(-0.14, 0.14) * M, 1.24 * M, 0.38 * M), rotation_x(w.uniform(-0.5, 0.2)) @ rotation_y(w.uniform(-0.3, 0.3)))
             for _ in range(4)]

    def parts() -> list[Part]:
        post = lambda p: rounded_box(p @ lean, (0.0, 0.55 * M, 0.0), (0.055 * M, 0.55 * M, 0.055 * M), 0.012 * M)
        box = lambda p: rounded_box(p @ lean, (0.0, 1.22 * M, 0.06 * M), (0.3 * M, 0.24 * M, 0.3 * M), 0.03 * M)
        pile = lambda p: _union(*(rounded_box(p, c, (0.16 * M, 0.008 * M, 0.11 * M), 0.003 * M, r) for c, r in letters))
        out = lambda p: _union(*(rounded_box(p @ lean, c, (0.14 * M, 0.1 * M, 0.008 * M), 0.003 * M, r) for c, r in stuck))
        paper = lambda p: np.minimum(pile(p), out(p))
        return [
            Part(post, POST),
            Part(box, BOX),
            Part(painted(box, noise_mask(0.08 * M, seed + 1, 0.12)), RUST_M, relief=False),
            Part(paper, PAPER),
            Part(painted(paper, noise_mask(0.06 * M, seed + 2, 0.12)), STAMP, relief=False),
        ]

    return PropModel(stem, parts, materials, THREE_QUARTER, canvas=(72, 84), footprint=box_footprint(0.3 * M, 0.3 * M))


def harbor_beacon(stem: str, seed: int) -> PropModel:
    """
    Feu de port au bout d'une jetée qui n'existe plus : un socle de béton, une tourelle en treillis peinte en rouge
    que la rouille délave, la lanterne vitrée au sommet, éteinte, et sa petite échelle.
    """
    CONCRETE, FRAME, RUST_M, LAMP, GLASS = range(5)
    materials = [make_material("concrete", "#8A887E", contrast=0.6), make_material("frame", BEACON_RED),
                 make_material("rust", RUST_ORANGE, contrast=0.7), make_material("lamp", IRON, contrast=0.8),
                 make_material("glass", "#5A6A6E", contrast=0.5)]
    top = 3.4 * M

    def parts() -> list[Part]:
        base = lambda p: rounded_box(p, (0.0, 0.25 * M, 0.0), (0.75 * M, 0.25 * M, 0.75 * M), 0.05 * M)
        feet = [(-0.5, -0.5), (0.5, -0.5), (-0.5, 0.5), (0.5, 0.5)]
        legs = lambda p: _union(*(capsule(p, (x * M, 0.5 * M, z * M), (x * 0.4 * M, top, z * 0.4 * M), 0.05 * M) for x, z in feet))
        rings = lambda p: _union(*(np.maximum(rounded_box(p, (0.0, y * M, 0.0), (r * M, 0.03 * M, r * M), 0.01 * M),
                                              -rounded_box(p, (0.0, y * M, 0.0), ((r - 0.06) * M, 0.1 * M, (r - 0.06) * M), 0.0))
                                   for y, r in ((1.3, 0.44), (2.2, 0.34), (3.0, 0.26))))
        braces = lambda p: _union(*(capsule(p, (a * M, ya * M, 0.5 * M * s), (b * M, yb * M, 0.42 * M * s), 0.025 * M)
                                    for s in (-1, 1) for a, ya, b, yb in ((-0.47, 0.6, 0.38, 1.3), (0.42, 1.3, -0.33, 2.2))))
        frame = lambda p: _union(legs(p), rings(p), braces(p))
        deck = lambda p: rounded_box(p, (0.0, top, 0.0), (0.38 * M, 0.04 * M, 0.38 * M), 0.01 * M)
        lantern = lambda p: cylinder(p, (0.0, top + 0.32 * M, 0.0), 0.2 * M, 0.26 * M, 0.02 * M)
        cap = lambda p: capsule(p, (0.0, top + 0.6 * M, 0.0), (0.0, top + 0.78 * M, 0.0), 0.24 * M, 0.04 * M)
        panes = both(bands(1, 0.5 * M, 0.38 * M, top + 0.32 * M), lambda p: np.abs(p[:, 1] - top - 0.32 * M) - 0.18 * M)
        return [
            Part(base, CONCRETE),
            Part(frame, FRAME),
            Part(painted(frame, noise_mask(0.15 * M, seed + 1, 0.3)), RUST_M, relief=False),
            Part(lambda p: _union(deck(p), cap(p)), LAMP),
            Part(lantern, LAMP),
            Part(painted(lantern, panes, 0.02 * M), GLASS, relief=False),
        ]

    return PropModel(stem, parts, materials, THREE_QUARTER, ray_range=float(2.5 * top), canvas=(80, 150),
                     footprint=box_footprint(0.75 * M, 0.75 * M))


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
        footbridge("prop_footbridge_x", 551, 0.0, 1.9 * M),
        footbridge("prop_footbridge_y", 552, np.pi / 2, 1.5 * M),
        footbridge("prop_footbridge_falling", 557, -np.pi / 4, 1.45 * M),
        footbridge("prop_footbridge_rising", 558, np.pi / 4, 1.45 * M),
        tall_reeds("prop_tall_reeds", 553),
        lobster_pots("prop_lobster_pots", 554),
        drying_net("prop_drying_net", 555),
        stake_row("prop_stake_row", 556),
        trawler_hull("prop_trawler_hull", 561),
        slipway("prop_slipway", 562),
        winch("prop_winch", 563),
        plank_stack("prop_plank_stack", 564),
        shipyard_shed("prop_shipyard_shed", 565),
        quay_edge("prop_quay_edge", 566),
        quay_paving("prop_quay_paving", 567),
        bollard("prop_bollard", 568),
        harbor_crane("prop_harbor_crane", 569),
        upturned_boat("prop_upturned_boat", 570),
        fish_crates("prop_fish_crates", 571),
        salt_pile("prop_salt_pile", 581),
        salt_rake("prop_salt_rake", 582),
        salt_pan("prop_salt_pan", 589),
        wheelbarrow("prop_wheelbarrow", 583),
        saltworker_hut("prop_saltworker_hut", 584),
        sluice_gate("prop_sluice_gate", 585),
        dike_wall("prop_dike_wall", 586),
        dike_rubble("prop_dike_rubble", 587),
        sandbags("prop_sandbags", 588),
        fisher_house("prop_fisher_house_a", 591, 2, 0.25, False),
        fisher_house("prop_fisher_house_b", 592, 3, 0.35, True),
        fisher_house("prop_fisher_house_c", 593, 2, 0.5, True),
        port_mailbox("prop_port_mailbox", 594),
        harbor_beacon("prop_harbor_beacon", 595),
    ]
    return models
