"""
Décors de la Forêt Reconquise (plan 08, lot P3 ; repris au plan 31, lot F1) : une nature lumineuse qui a dévoré la
civilisation.

Échelle réelle du personnage (1 m ≈ 27,5 unités). Les arbres sont rendus en deux fichiers alignés sur le même point
au sol : le tronc et ses maîtresses branches (triés avec les entités) et la canopée (couche des canopées, transparente
quand le joueur passe dessous) ; les deux viennent du même tirage, et la canopée laisse voir les branches là où elle
s'ouvre. Les champs et la ferme reprennent ces arbres (`tree`). Couleurs de la palette forêt de la charte (§3) : le
feuillage reste plus clair que le sous-bois pour se détacher du sol, et les vestiges humains gardent leur gris et
leur rouille sous le lierre. Le lierre pousse en tiges chargées de feuilles plaquées, la mousse se peint sur les
volumes : plus de boules vertes posées sur les objets.
"""
from __future__ import annotations

from typing import Callable

import numpy as np

from ..palette import make_material
from ..render import Part
from ..sdf import capsule, cylinder, ellipsoid, rotation_x, rotation_y, rotation_z, rounded_box, sphere
from ._flora import _align_y, _bounded, _branch_chain, _gnarled, _grooves, _leaf_mass, _shelves, _top_skin, _union
from ._kit import AXIS_X_YAW, AXIS_Y_YAW, M, PropModel, Weathering, box_footprint
from ._surface import bands, both, bricks, cracks, either, noise_mask, painted, value_noise
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
# Tons dérivés de la palette du biome : rainure d'écorce, marques du bouleau, feuille de lierre, lichen, pierre
# humide, cœur des fleurs, chapeau de cèpe, bois mort gris.
BARK_GROOVE = "#2E2219"
BIRCH_MARK = "#3A3330"
IVY_LEAF = "#2E5A28"
LICHEN = "#A9AE7A"
STONE_DARK = "#5E5E57"
PETAL_WHITE = "#E6E1D2"
HEART = "#C9862E"
CEP = "#7B4B2A"
DEAD_WOOD = "#6B6161"
# Hors palette de biome, repris de la palette maîtresse : fonte peinte, rouille, brique, panneau.
IRON_GREEN = "#3D4A42"
RUST = "#6A4430"
RUST_ORANGE = "#A85C30"
BRICK = "#8A5A42"
MORTAR = "#A49C8A"
SIGN_RED = "#9E3B33"
SIGN_WHITE = "#D9D3C3"
# Arbres et touffes se composent de face (x vers la droite, +z face à la caméra) : la lumière vient du haut à gauche,
# l'ombre est à droite.
FRONT = 0.0


def _point_at_height(chain: list[np.ndarray], y: float) -> np.ndarray:
    """Point d'une chaîne à peu près verticale (fût) à la hauteur y, par interpolation entre ses nœuds."""
    heights = np.array([point[1] for point in chain])
    return np.array([np.interp(y, heights, [point[k] for point in chain]) for k in range(3)])


def _grass(w: Weathering, spots: list[tuple[float, float]], blades: int = 5, height: float = 0.3 * M):
    """Touffes d'herbe au pied d'un objet : brins fins qui s'écartent en éventail, (x, z) de chaque touffe."""
    pieces = []
    for x, z in spots:
        for index in range(blades):
            angle = w.uniform(0, 2 * np.pi)
            lean = w.uniform(0.25, 0.6)
            tip = np.array([x + np.sin(angle) * lean * height * 0.6, height * w.uniform(0.6, 1.0), z + np.cos(angle) * lean * height * 0.5])
            pieces.append((np.array([x, 0.0, z]), tip))
    return lambda p: _union(*(capsule(p, a, b, 0.03 * M, 0.012 * M) for a, b in pieces))


def _leaf_cluster(centers: list[np.ndarray], normals: list[np.ndarray], sizes: list[float]):
    """Feuilles plaquées (lierre) : disques minces couchés contre leur support, mince le long de la normale."""
    frames = [_align_y(n) for n in normals]
    return lambda p: _union(*(ellipsoid(p, c, (s, 0.045 * M, s * 0.85), f) for c, f, s in zip(centers, frames, sizes)))


# ---------------------------------------------------------------------------------------------------------------------
# Arbres (tronc + canopée)
# ---------------------------------------------------------------------------------------------------------------------

class _Tree:
    """
    Tirage d'un arbre, partagé par ses deux fichiers pour que tronc et canopée correspondent : un fût un peu torse,
    évasé en contreforts, qui se divise en maîtresses branches (le bouleau garde une flèche jusqu'en haut) ; les
    rameaux vont chercher les touffes de la couronne.
    """

    def __init__(self, seed: int, height: float, crown_radius: float, trunk_radius: float, clumps: int, style: str):
        w = Weathering(seed)
        r, c = trunk_radius, crown_radius
        birch = style == "birch"
        self.radius = r
        self.trunk = _gnarled(w, np.zeros(3), np.array([w.uniform(-0.08, 0.08), 1.0, w.uniform(-0.06, 0.06)]),
                              height * (0.6 if birch else 0.34), 4, wobble=0.05)
        fork = self.trunk[-1]
        center = np.array([fork[0] * 1.4, height * 0.72, fork[2] * 1.4])
        self.crown_center = center
        self.roots = []
        for angle in np.linspace(0, 2 * np.pi, 5, endpoint=False) + w.uniform(0, 1.2):
            reach = r * w.uniform(2.2, 3.0)
            knee = np.array([np.sin(angle) * r * 1.1, r * 0.75, np.cos(angle) * r * 1.1])
            foot = np.array([np.sin(angle) * reach, -0.04 * M, np.cos(angle) * reach])
            self.roots.append((np.array([0.0, r * 2.2, 0.0]), knee, foot))
        self.centers: list[np.ndarray] = []
        self.radii: list[np.ndarray] = []
        for index in range(clumps):
            angle = index * 2.39996 + w.uniform(-0.3, 0.3)
            if birch:
                # Couronne haute et étroite qui s'affine vers la pointe.
                t = (index + 0.5) / clumps
                offset = np.array([np.cos(angle) * c * 0.75 * (1.0 - 0.5 * t), (t - 0.4) * c * 1.6,
                                   np.sin(angle) * c * 0.55 * (1.0 - 0.5 * t)])
                size = c * w.uniform(0.24, 0.32)
            else:
                ring = np.sqrt((index + 0.5) / clumps)
                offset = np.array([np.cos(angle) * ring * c * 0.8, (1.0 - ring) * c * 0.5 + w.uniform(-0.18, 0.18) * c,
                                   np.sin(angle) * ring * c * 0.62])
                size = c * w.uniform(0.3, 0.42)
            self.centers.append(center + offset)
            self.radii.append(np.array([size, size * 0.8, size]))
        # Trois étages de feuillage : dessous à l'ombre, cœur, touffes du haut en pleine lumière.
        order = np.argsort([cc[1] - cc[2] * 0.3 for cc in self.centers])
        self.lower = [int(i) for i in order[: len(order) // 4]]
        self.upper = [int(i) for i in order[len(order) * 2 // 3:]]
        self.middle = [i for i in range(clumps) if i not in self.lower and i not in self.upper]
        self.floor = min(cc[1] - rr[1] for cc, rr in zip(self.centers, self.radii))
        # Branches : (points, rayon au départ, rayon au bout).
        self.limbs: list[tuple[list[np.ndarray], float, float]] = []
        if birch:
            leader = _gnarled(w, fork, np.array([0.0, 1.0, 0.0]), height * 0.3, 3, wobble=0.06)
            self.limbs.append((leader, r * 0.78, r * 0.3))
            starts = [self.trunk[-2], fork, leader[1], leader[2], leader[1], leader[3], fork]
            for index, start in enumerate(starts):
                target = self.centers[(index * clumps) // len(starts)]
                d = target - start
                self.limbs.append((_gnarled(w, start, d, np.linalg.norm(d) * 0.9, 2, wobble=0.15), r * 0.34, r * 0.1))
        else:
            a0 = w.uniform(0, 2 * np.pi)
            for k in range(3):
                a = a0 + k * 2 * np.pi / 3 + w.uniform(-0.3, 0.3)
                target = center + np.array([np.cos(a) * c * 0.62, w.uniform(-0.3, -0.1) * c, np.sin(a) * c * 0.48])
                d = target - fork
                self.limbs.append((_gnarled(w, fork, d, np.linalg.norm(d), 3, wobble=0.12), r * 0.62, r * 0.28))
            d = center + np.array([0.0, 0.3 * c, 0.0]) - fork
            self.limbs.append((_gnarled(w, fork, d, np.linalg.norm(d), 3, wobble=0.1), r * 0.55, r * 0.22))
            # Rameaux : du bout et du milieu de chaque branche vers une touffe voisine.
            for limb, _, _ in list(self.limbs):
                for start in (limb[-1], limb[-2]):
                    nearest = np.argsort([np.linalg.norm(cc - start) for cc in self.centers])
                    target = self.centers[int(nearest[int(w.uniform(0, 2.99))])]
                    d = target - start
                    if np.linalg.norm(d) > 0.25 * M:
                        self.limbs.append((_gnarled(w, start, d, np.linalg.norm(d) * 0.85, 2, wobble=0.2), r * 0.28, r * 0.1))

    def trunk_shape(self):
        r = self.radius
        trunk = _branch_chain(self.trunk, r, r * 0.8)
        roots = lambda p: _union(*(np.minimum(capsule(p, a, k, r * 0.55, r * 0.4), capsule(p, k, f, r * 0.4, r * 0.08))
                                   for a, k, f in self.roots))
        return lambda p: np.minimum(trunk(p), roots(p))

    def limb_shape(self):
        return lambda p: _union(*(_branch_chain(points, r0, r1)(p) for points, r0, r1 in self.limbs))


def _tree_base(stem: str, tree: _Tree, seed: int, bark: str, moss: bool, style: str, canvas: tuple[int, int]) -> PropModel:
    BARK, GROOVE, MARK, MOSS_M, MOSS_TOP = range(5)
    materials = [make_material("bark", bark), make_material("bark_groove", BIRCH_MARK if style == "birch" else BARK_GROOVE, contrast=0.8),
                 make_material("birch_mark", BIRCH_MARK, contrast=0.6), make_material("moss", MOSS),
                 make_material("moss_light", LEAF_LIGHT, contrast=0.8)]
    r = tree.radius

    def parts() -> list[Part]:
        trunk, limbs = tree.trunk_shape(), tree.limb_shape()
        wood = lambda p: np.minimum(trunk(p), limbs(p))
        result = [Part(wood, BARK)]
        if style == "birch":
            # Écorce blanche barrée de lenticelles sombres, pied noirci et crevassé.
            dashes = both(bands(1, 0.19 * M, 0.07 * M), noise_mask(0.1 * M, seed + 4, 0.5, (1.0, 0.25, 1.0)))
            foot = lambda p: p[:, 1] - 0.4 * M - (value_noise(p, 0.2 * M, seed + 5) - 0.5) * 0.4 * M
            result.append(Part(painted(wood, either(dashes, foot)), MARK))
        else:
            result.append(Part(_grooves(wood, seed + 1, period=0.11 * M, coverage=0.3), GROOVE))
        if moss:
            # Mousse côté ombre, sur le pied et les contreforts.
            patch = both(noise_mask(0.16 * M, seed + 2, 0.4), lambda p: p[:, 1] - 0.9 * M, lambda p: -p[:, 0] - 0.3 * r)
            result.append(Part(painted(trunk, patch, 0.02 * M), MOSS_M))
            result.append(Part(painted(trunk, both(patch, noise_mask(0.08 * M, seed + 3, 0.2)), 0.03 * M), MOSS_TOP, relief=False))
        return result

    points = [q for chain, _, _ in tree.limbs for q in chain]
    reach = max(max(np.hypot(q[0], q[2]) for q in points), r * 3.2) + r + 0.3 * M
    top = max(q[1] for q in points) + r + 0.3 * M
    return PropModel(stem, parts, materials, FRONT, canvas=canvas, footprint=box_footprint(r * 1.3, r * 1.3),
                     bounds=((-reach, -0.3 * M, -reach), (reach, top, reach)))


def _tree_canopy(stem: str, tree: _Tree, seed: int, bark: str, leaf: str, leaf_top: str, shade: str,
                 canvas: tuple[int, int]) -> PropModel:
    SHADE, LEAF, LEAF_TOP, BRANCH = range(4)
    materials = [make_material("leaf_shade", shade, contrast=0.9), make_material("leaf", leaf),
                 make_material("leaf_top", leaf_top, contrast=0.9), make_material("branch", bark)]

    def mass(indices: list[int], tier: int) -> Callable[[np.ndarray], np.ndarray]:
        return _leaf_mass([tree.centers[i] for i in indices], [tree.radii[i] for i in indices], seed + 10 + tier)

    def parts() -> list[Part]:
        limbs = tree.limb_shape()
        return [
            Part(mass(tree.lower, 0), SHADE),
            Part(mass(tree.middle, 1), LEAF),
            Part(mass(tree.upper, 2), LEAF_TOP),
            Part(lambda p: np.maximum(limbs(p), tree.floor + 0.2 * M - p[:, 1]), BRANCH),
        ]

    top = max(c[1] + r[1] for c, r in zip(tree.centers, tree.radii)) + 0.6 * M
    reach = max(np.hypot(c[0], c[2]) + r[0] for c, r in zip(tree.centers, tree.radii)) + 0.6 * M
    return PropModel(stem, parts, materials, FRONT, canvas=canvas, supersample=3,
                     bounds=((-reach, tree.floor - 0.3 * M, -reach), (reach, top, reach)))


def tree(stem: str, seed: int, height: float, crown: float, trunk: float, clumps: int, bark: str, leaf: str,
         leaf_top: str, moss: bool, canvas: tuple[int, int], style: str = "oak", shade: str = CANOPY_DARK) -> list[PropModel]:
    shape = _Tree(seed, height, crown, trunk, clumps, style)
    return [_tree_base(f"{stem}_base", shape, seed, bark, moss, style, canvas),
            _tree_canopy(f"{stem}_canopy", shape, seed, bark, leaf, leaf_top, shade, canvas)]


def strangled_tree(stem: str, seed: int) -> PropModel:
    """
    Arbre mort étranglé par le lierre : fût gris étêté en échardes, deux branches mortes ; trois tiges de lierre
    s'enroulent en montant, chargées de feuilles plaquées à l'écorce, plus denses au pied, et des pans pendent des
    branches. Pas de canopée : la silhouette reste haute et sombre.
    """
    WOOD, GROOVE, SPLINTER, STEM_M, LEAF, LEAF_LIT = range(6)
    materials = [make_material("dead_wood", DEAD_WOOD), make_material("bark_groove", "#4A4545", contrast=0.8),
                 make_material("splinter", "#A39A90", contrast=0.6), make_material("ivy_stem", "#4A3A2C", contrast=0.6),
                 make_material("ivy", IVY_LEAF), make_material("ivy_light", MOSS, contrast=0.8)]
    w = Weathering(seed)
    r = 0.3 * M
    trunk = _gnarled(w, np.zeros(3), np.array([0.05, 1.0, 0.0]), 3.8 * M, 5, wobble=0.06)
    top = trunk[-1]
    limbs = [_gnarled(w, trunk[k], np.array([s, 0.75, w.uniform(-0.3, 0.3)]), w.uniform(1.0, 1.35) * M, 3, wobble=0.25)
             for k, s in ((3, -1.0), (4, 1.0))]
    twigs = [_gnarled(w, limb[j], np.array([w.uniform(-1, 1), 0.9, w.uniform(-0.4, 0.4)]), w.uniform(0.3, 0.5) * M, 2)
             for limb in limbs for j in (2, 3)]
    # Cassure : trois plans inclinés passant sous la cime, d'où sortent les échardes.
    jag = [(rotation_y(w.uniform(0, 2 * np.pi)) @ rotation_x(w.uniform(0.4, 0.7)), top - np.array([0.0, w.uniform(0.15, 0.45) * M, 0.0])) for _ in range(3)]
    splinters = [(top + np.array([np.sin(a) * r * 0.45, -0.35 * M, np.cos(a) * r * 0.45]),
                  top + np.array([np.sin(a) * r * 0.5, w.uniform(0.0, 0.25) * M, np.cos(a) * r * 0.5])) for a in (-0.6, 1.2, 2.6)]
    stems, centers, normals, sizes = [], [], [], []
    for k in range(3):
        a0 = k * 2 * np.pi / 3 + w.uniform(-0.4, 0.4)
        turns = w.uniform(0.9, 1.3)
        climb = w.uniform(2.5, 3.3) * M
        points = []
        for t in np.linspace(0.0, 1.0, 18):
            y = t * climb
            a = a0 + t * turns * 2 * np.pi
            radial = np.array([np.sin(a), 0.0, np.cos(a)])
            points.append(_point_at_height(trunk, y) + radial * (r * (1.0 - 0.4 * y / (3.8 * M)) + 0.02 * M))
            # Feuilles plus serrées et plus grandes au pied qu'en haut.
            if w.uniform(0, 1) < 1.0 - 0.45 * t:
                tilt = np.array([w.uniform(-0.3, 0.3), w.uniform(0.0, 0.5), w.uniform(-0.3, 0.3)])
                centers.append(points[-1] + radial * 0.05 * M)
                normals.append(radial + tilt)
                sizes.append(w.uniform(0.13, 0.19) * M * (1.0 - 0.35 * t))
        stems.append(points)
    for limb in limbs:
        for point in limb[1:]:
            for _ in range(2):
                centers.append(point + np.array([w.uniform(-0.12, 0.12) * M, -w.uniform(0.1, 0.45) * M, 0.06 * M]))
                normals.append(np.array([w.uniform(-0.3, 0.3), 0.4, 1.0]))
                sizes.append(w.uniform(0.1, 0.15) * M)

    def parts() -> list[Part]:
        stem_chain = lambda p: np.maximum(_branch_chain(trunk, r, r * 0.62)(p), _union(*(((p - anchor) @ q)[:, 1] for q, anchor in jag)))
        dead = lambda p: _union(stem_chain(p), *(_branch_chain(limb, r * 0.4, r * 0.14)(p) for limb in limbs),
                                *(_branch_chain(t, r * 0.14, r * 0.05)(p) for t in twigs))
        hull = lambda p: np.minimum(capsule(p, (0, 0, 0), top, r + 0.35 * M),
                                    _union(*(capsule(p, limb[0], limb[-1], 0.6 * M) for limb in limbs)))
        leaves = _bounded(_leaf_cluster(centers, normals, sizes), hull)
        lit = both(noise_mask(0.14 * M, seed + 3, 0.35), lambda p: -p[:, 0] - 0.05 * M)
        return [
            Part(dead, WOOD),
            Part(_grooves(dead, seed + 1, period=0.1 * M), GROOVE),
            Part(lambda p: _union(*(capsule(p, a, b, 0.07 * M, 0.015 * M) for a, b in splinters)), SPLINTER),
            Part(lambda p: _union(*(_branch_chain(s, 0.035 * M, 0.025 * M)(p) for s in stems)), STEM_M),
            Part(leaves, LEAF),
            Part(painted(leaves, lit, 0.015 * M), LEAF_LIT, relief=False),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(90, 150), footprint=box_footprint(0.35 * M, 0.35 * M))


# ---------------------------------------------------------------------------------------------------------------------
# Sous-bois
# ---------------------------------------------------------------------------------------------------------------------

def bush(stem: str, seed: int, blossom: str | None) -> PropModel:
    """
    Buisson : un pied de tiges sombres sous une masse de feuillage en touffes, ombrée par-dessous et piquée de lumière
    au sommet. La variante fleurie porte des grappes qui dépassent du feuillage.
    """
    SHADE, LEAF, LEAF_TOP, GLINT, TWIG, FLOWER, FLOWER_LIT = range(7)
    materials = [make_material("leaf_shade", CANOPY_DARK, contrast=0.9), make_material("leaf", MOSS),
                 make_material("leaf_top", LEAF_LIGHT, contrast=0.9), make_material("glint", LEAF_YELLOW, contrast=0.6),
                 make_material("twig", TRUNK, contrast=0.6), make_material("flower", blossom or FLOWER_YELLOW, contrast=0.8),
                 make_material("flower_light", PETAL_WHITE, contrast=0.5)]
    w = Weathering(seed)
    low = [np.array([w.uniform(-0.75, 0.75) * M, w.uniform(0.45, 0.6) * M, w.uniform(-0.45, 0.45) * M]) for _ in range(7)]
    high = [np.array([w.uniform(-0.45, 0.45) * M, w.uniform(0.85, 1.05) * M, w.uniform(-0.2, 0.3) * M]) for _ in range(4)]
    low_radii = [np.array([s, s * 0.75, s]) for s in (w.uniform(0.3, 0.42) * M for _ in low)]
    high_radii = [np.array([s, s * 0.8, s]) for s in (w.uniform(0.26, 0.36) * M for _ in high)]
    twigs = [(np.array([x * M, 0.0, z * M]), np.array([x * 1.6 * M, 0.55 * M, z * 1.4 * M])) for x, z in ((-0.3, 0.1), (0.1, 0.25), (0.35, -0.05), (-0.05, -0.2))]
    clusters = []
    if blossom:
        for index in range(11):
            k = index % (len(low) + len(high))
            center, radius = (high[k], high_radii[k]) if k < len(high) else (low[k - len(high)], low_radii[k - len(high)])
            direction = np.array([w.uniform(-1, 1), w.uniform(0.4, 1.2), w.uniform(0.0, 1.0)])
            direction /= np.linalg.norm(direction)
            clusters.append((center + direction * radius * 1.05, direction))

    def parts() -> list[Part]:
        lower = _leaf_mass(low, low_radii, seed, per=6, ratio=0.52)
        upper = _leaf_mass(high, high_radii, seed + 1, per=6, ratio=0.52)
        underside = lambda p: p[:, 1] - 0.42 * M + (value_noise(p, 0.25 * M, seed + 4) - 0.5) * 0.3 * M
        result = [
            Part(lower, LEAF),
            Part(upper, LEAF_TOP),
            Part(painted(lower, underside), SHADE, relief=False),
            Part(painted(upper, noise_mask(0.11 * M, seed + 5, 0.22)), GLINT, relief=False),
            Part(lambda p: _union(*(capsule(p, a, b, 0.05 * M, 0.025 * M) for a, b in twigs)), TWIG),
        ]
        if clusters:
            # Grappe : un cône de petites fleurs, plus pâle à la pointe.
            result.append(Part(lambda p: _union(*(ellipsoid(p, c, (0.07 * M, 0.11 * M, 0.07 * M), _align_y(d)) for c, d in clusters)), FLOWER))
            result.append(Part(lambda p: _union(*(sphere(p, c + d * 0.09 * M, 0.04 * M) for c, d in clusters)), FLOWER_LIT))
        return result

    return PropModel(stem, parts, materials, FRONT, canvas=(84, 72))


def fern(stem: str, seed: int) -> PropModel:
    """
    Fougère : une dizaine de frondes qui montent puis retombent en arc, chacune un peigne de folioles qui rétrécit vers
    la pointe ; les frondes de l'arrière à l'ombre, deux crosses encore enroulées au cœur.
    """
    FROND_DARK, FROND, FROND_LIT, CROSIER = range(4)
    materials = [make_material("frond_shade", CANOPY_DARK, contrast=0.9), make_material("frond", MOSS),
                 make_material("frond_light", LEAF_LIGHT, contrast=0.8), make_material("crosier", LEAF_YELLOW, contrast=0.7)]
    w = Weathering(seed)
    leaflets: list[list[tuple[np.ndarray, np.ndarray, np.ndarray]]] = [[], [], []]
    up = np.array([0.0, 1.0, 0.0])
    count = 11
    for index in range(count):
        angle = index * 2 * np.pi / count + w.uniform(-0.15, 0.15)
        direction = np.array([np.sin(angle), 0.0, np.cos(angle)])
        length, rise = w.uniform(0.75, 1.0) * M, w.uniform(0.45, 0.62) * M
        p0, p1, p2 = np.array([0.0, 0.05 * M, 0.0]), direction * length * 0.3 + up * rise * 1.35, direction * length + up * rise * 0.15
        tone = 0 if direction[2] < -0.35 else (2 if index % 3 == 0 else 1)
        for s in np.linspace(0.14, 0.96, 9):
            point = (1 - s) ** 2 * p0 + 2 * s * (1 - s) * p1 + s ** 2 * p2
            tangent = 2 * (1 - s) * (p1 - p0) + 2 * s * (p2 - p1)
            tangent /= np.linalg.norm(tangent)
            side = np.cross(tangent, up)
            side /= np.linalg.norm(side)
            normal = np.cross(side, tangent)
            frame = np.stack([side, normal, tangent], axis=1)
            leaflets[tone].append((point, frame, np.array([0.17 * M * (1.0 - 0.72 * s), 0.02 * M, 0.06 * M])))
    crosiers = [np.array([x * M, h * M, z * M]) for x, h, z in ((-0.06, 0.32, 0.04), (0.08, 0.26, 0.1))]

    def frond_part(tone: int) -> Callable[[np.ndarray], np.ndarray]:
        chosen = leaflets[tone]
        return lambda p: _union(*(ellipsoid(p, c, radii, f) for c, f, radii in chosen))

    def parts() -> list[Part]:
        return [
            Part(frond_part(0), FROND_DARK),
            Part(frond_part(1), FROND),
            Part(frond_part(2), FROND_LIT),
            Part(lambda p: _union(*(np.minimum(capsule(p, (c[0], 0, c[2]), c, 0.025 * M), sphere(p, c, 0.055 * M)) for c in crosiers)), CROSIER),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(80, 60))


def flowers(stem: str, seed: int, petals: str, heart: str) -> PropModel:
    """
    Touffe de fleurs sauvages : rosette de feuilles au sol, tiges de hauteurs inégales, corolles ouvertes vers le ciel
    avec leur cœur, quelques boutons encore fermés.
    """
    LEAF_M, STEM_M, PETAL, HEART_M = range(4)
    materials = [make_material("leaf", MOSS), make_material("stem", CANOPY_DARK, contrast=0.6),
                 make_material("petal", petals, contrast=0.7), make_material("heart", heart, contrast=0.5)]
    w = Weathering(seed)
    heads = [np.array([w.uniform(-0.65, 0.65) * M, w.uniform(0.24, 0.52) * M, w.uniform(-0.4, 0.4) * M]) for _ in range(10)]
    buds = [np.array([w.uniform(-0.45, 0.45) * M, w.uniform(0.18, 0.4) * M, w.uniform(-0.3, 0.3) * M]) for _ in range(3)]
    blades = [(rotation_y(w.uniform(0, 2 * np.pi)) @ rotation_x(w.uniform(0.2, 0.5)), np.array([w.uniform(-0.25, 0.25) * M, 0.04 * M, w.uniform(-0.2, 0.2) * M]))
              for _ in range(7)]
    # Corolles tournées vers la caméra, pour qu'elles se lisent rondes et non en tirets.
    tilt = rotation_x(0.6)

    def parts() -> list[Part]:
        return [
            Part(lambda p: _union(*(ellipsoid(p, c + q @ np.array([0.0, 0.0, 0.18 * M]), (0.06 * M, 0.018 * M, 0.2 * M), q.T)
                                    for q, c in blades)), LEAF_M),
            Part(lambda p: _union(*(capsule(p, (h[0] * 0.6, 0, h[2] * 0.6), h, 0.024 * M) for h in heads + buds)), STEM_M),
            Part(lambda p: _union(*(ellipsoid(p, h, (0.1 * M, 0.028 * M, 0.1 * M), tilt) for h in heads),
                                  *(ellipsoid(p, b, (0.035 * M, 0.05 * M, 0.035 * M)) for b in buds)), PETAL),
            Part(lambda p: _union(*(sphere(p, h + np.array([0.0, 0.02 * M, 0.01 * M]), 0.035 * M) for h in heads)), HEART_M),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(64, 44))


def mushrooms(stem: str, seed: int) -> PropModel:
    """
    Touffe de cèpes et de girolles sur un lit de mousse et de feuilles mortes : chapeaux bombés aux bords plus
    clairs, pieds ventrus, tailles franches pour que chacun se lise.
    """
    STALK, CAP, RIM, CHANTERELLE, MOSS_M, LITTER = range(6)
    materials = [make_material("stalk", "#E3D8C2", contrast=0.6), make_material("cap", CEP),
                 make_material("rim", "#A8784A", contrast=0.7), make_material("chanterelle", OCHRE, contrast=0.8),
                 make_material("moss", MOSS, contrast=0.8), make_material("litter", EARTH, contrast=0.7)]
    # (x, z, hauteur du chapeau, rayon, inclinaison, girolle)
    specs = ((-0.1, 0.0, 0.44, 0.2, 0.05, False), (0.27, 0.1, 0.3, 0.15, -0.25, False), (-0.42, 0.16, 0.2, 0.11, 0.3, False),
             (0.45, -0.12, 0.2, 0.1, -0.2, True), (0.1, 0.32, 0.13, 0.08, 0.15, True))

    def parts() -> list[Part]:
        shapes = [(np.array([x * M, 0.0, z * M]), np.array([x * M, h * M, z * M]), r * M, rotation_z(t), chanterelle)
                  for x, z, h, r, t, chanterelle in specs]
        ceps = [s for s in shapes if not s[4]]
        girolles = [s for s in shapes if s[4]]
        cap = lambda p: _union(*(np.maximum(ellipsoid(p, top, (r, r * 0.62, r), tilt), -((p - top) @ tilt)[:, 1] - r * 0.1)
                                 for _, top, r, tilt, _ in ceps))
        funnel = lambda p: _union(*(np.maximum(ellipsoid(p, top, (r, r * 0.45, r), tilt), ((p - top) @ tilt)[:, 1] - r * 0.15)
                                    for _, top, r, tilt, _ in girolles))
        return [
            Part(lambda p: _union(*(capsule(p, b, top, r * 0.42, r * 0.3) for b, top, r, _, _ in ceps),
                                  *(capsule(p, b, top, r * 0.25, r * 0.4) for b, top, r, _, _ in girolles)), STALK),
            Part(cap, CAP),
            Part(painted(cap, lambda p: _union(*(((p - top) @ tilt)[:, 1] - r * 0.02 for _, top, r, tilt, _ in ceps))), RIM, relief=False),
            Part(funnel, CHANTERELLE),
            Part(lambda p: ellipsoid(p, (0.0, 0.0, 0.1 * M), (0.65 * M, 0.06 * M, 0.4 * M)), MOSS_M),
            Part(lambda p: _union(*(ellipsoid(p, (x * M, 0.02 * M, z * M), (0.12 * M, 0.025 * M, 0.08 * M), rotation_y(a))
                                    for x, z, a in ((-0.5, 0.3, 0.4), (0.3, 0.4, -0.6), (0.55, 0.15, 1.1), (-0.2, -0.25, 0.2)))), LITTER),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(52, 44))


def stump(stem: str, seed: int) -> PropModel:
    """
    Souche sciée : fût évasé en racines, écorce rainurée, plateau clair aux cernes concentriques fendu d'une gerce,
    mousse au pied côté ombre, polypores en console, une pousse de chêne qui repart.
    """
    BARK_M, GROOVE, CUT, RING, CRACK, MOSS_M, MOSS_TOP, SHELF, SPROUT = range(9)
    materials = [make_material("bark", TRUNK), make_material("bark_groove", BARK_GROOVE, contrast=0.8),
                 make_material("cut", WOOD_LIGHT, contrast=0.6), make_material("ring", "#8A6E52", contrast=0.6),
                 make_material("crack", BARK_GROOVE, contrast=0.5), make_material("moss", MOSS),
                 make_material("moss_light", LEAF_LIGHT, contrast=0.8), make_material("shelf", OCHRE, contrast=0.8),
                 make_material("sprout", LEAF_LIGHT, contrast=0.7)]
    w = Weathering(seed)
    tilt = rotation_x(0.1) @ rotation_z(-0.08)
    level = 0.56 * M
    roots = [(np.array([0.0, 0.3 * M, 0.0]), np.array([np.sin(a) * 0.45 * M, 0.16 * M, np.cos(a) * 0.45 * M]),
              np.array([np.sin(a) * w.uniform(0.7, 0.85) * M, -0.03 * M, np.cos(a) * w.uniform(0.7, 0.85) * M]))
             for a in np.linspace(0, 2 * np.pi, 5, endpoint=False) + w.uniform(0, 1)]

    def parts() -> list[Part]:
        body = lambda p: np.maximum(cylinder(p, (0, 0.3 * M, 0), 0.42 * M, 0.4 * M, 0.04 * M), (p @ tilt)[:, 1] - level)
        root_shape = lambda p: _union(*(np.minimum(capsule(p, a, k, 0.2 * M, 0.15 * M), capsule(p, k, f, 0.15 * M, 0.04 * M)) for a, k, f in roots))
        wood = lambda p: np.minimum(body(p), root_shape(p))
        top = lambda p: (p @ tilt)[:, 1] - level + 0.03 * M
        face = lambda p: -top(p)
        radial = lambda q: np.stack([np.hypot(q[:, 0], q[:, 2]), q[:, 1], np.zeros(len(q))], axis=1)
        moss = both(noise_mask(0.22 * M, seed + 2, 0.6), lambda p: p[:, 1] - 0.3 * M, lambda p: -p[:, 0] + 0.05 * M)
        shelves = [(np.array([np.sin(a) * 0.42 * M, h * M, np.cos(a) * 0.42 * M]), s * M, a) for a, h, s in ((-0.9, 0.32, 0.13), (-0.7, 0.44, 0.1))]
        sprout = np.array([0.22 * M, level + 0.02 * M, 0.12 * M])
        return [
            Part(wood, BARK_M),
            Part(_grooves(wood, seed + 1, period=0.1 * M), GROOVE),
            Part(painted(wood, face), CUT),
            Part(painted(wood, both(face, bands(0, 0.075 * M, 0.025 * M, frame=radial))), RING, relief=False),
            Part(painted(wood, both(face, lambda p: np.abs(p[:, 0] * 0.6 - p[:, 2] * 0.8) - 0.018 * M, lambda p: -p[:, 0] - 0.02 * M)), CRACK, relief=False),
            Part(painted(wood, moss, 0.02 * M), MOSS_M),
            Part(painted(wood, both(moss, noise_mask(0.1 * M, seed + 3, 0.3)), 0.03 * M), MOSS_TOP, relief=False),
            Part(_shelves(shelves), SHELF),
            Part(lambda p: _union(capsule(p, sprout, sprout + np.array([0.02 * M, 0.2 * M, 0.0]), 0.02 * M),
                                  ellipsoid(p, sprout + np.array([-0.06 * M, 0.22 * M, 0.0]), (0.07 * M, 0.025 * M, 0.04 * M), rotation_z(-0.4)),
                                  ellipsoid(p, sprout + np.array([0.09 * M, 0.18 * M, 0.0]), (0.07 * M, 0.025 * M, 0.04 * M), rotation_z(0.4))), SPROUT),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(56, 48), footprint=box_footprint(0.42 * M, 0.42 * M))


def mossy_rock(stem: str, seed: int, scale: float) -> PropModel:
    """
    Rocher moussu : deux ou trois blocs arrondis, bosselés, fendus, piqués de lichen pâle ; une calotte de mousse qui
    coule par endroits sur les flancs, le pied humide plus sombre, des touffes d'herbe autour.
    """
    STONE, STONE_DAMP, CRACK, LICHEN_M, MOSS_M, MOSS_LIT, GRASS = range(7)
    materials = [make_material("stone", "#83837A"), make_material("stone_damp", STONE_DARK, contrast=0.8),
                 make_material("crack", "#3E3E3A", contrast=0.5), make_material("lichen", LICHEN, contrast=0.6),
                 make_material("moss", MOSS), make_material("moss_light", LEAF_LIGHT, contrast=0.8),
                 make_material("grass", MOSS, contrast=0.8)]
    w = Weathering(seed)
    s = scale
    blocks = [(np.array([0.0, 0.36 * M * s, 0.0]), np.array([0.6, 0.45, 0.5]) * M * s, rotation_y(w.uniform(0, np.pi))),
              (np.array([0.5 * M * s, 0.22 * M * s, 0.22 * M * s]), np.array([0.4, 0.3, 0.34]) * M * s, rotation_y(w.uniform(0, np.pi))),
              (np.array([-0.45 * M * s, 0.15 * M * s, 0.3 * M * s]), np.array([0.28, 0.2, 0.25]) * M * s, rotation_y(w.uniform(0, np.pi)))]
    tufts = [(x * M * s, z * M * s) for x, z in ((-0.7, 0.05), (0.15, 0.55), (0.85, -0.05), (-0.2, -0.5))]

    def parts() -> list[Part]:
        lumps = lambda p: _union(*(ellipsoid(p, c, h, r) for c, h, r in blocks))

        def rock(p: np.ndarray) -> np.ndarray:
            d = lumps(p) - 0.06 * M
            near = d < 0.2 * M
            if near.any():
                q = p[near]
                d[near] = (d[near] + 0.06 * M + (value_noise(q, 0.4 * M, seed + 1) - 0.5) * 0.2 * M
                           + (value_noise(q, 0.15 * M, seed + 2) - 0.5) * 0.05 * M) * 0.6
            return np.maximum(d, -p[:, 1] - 0.02 * M)

        cap = lambda p: 0.55 * M * s - p[:, 1] + (value_noise(p, 0.3 * M, seed + 3) - 0.5) * 0.5 * M * s
        moss = both(cap, noise_mask(0.35 * M, seed + 4, 0.75))
        damp = lambda p: p[:, 1] - 0.12 * M * s - (value_noise(p, 0.25 * M, seed + 5) - 0.5) * 0.15 * M
        return [
            Part(rock, STONE),
            Part(painted(rock, damp), STONE_DAMP, relief=False),
            Part(painted(rock, cracks(0.45 * M, seed + 6, 0.035 * M, reach=0.45)), CRACK, relief=False),
            Part(painted(rock, noise_mask(0.08 * M, seed + 7, 0.1)), LICHEN_M, relief=False),
            Part(painted(rock, moss, 0.03 * M), MOSS_M),
            Part(painted(rock, both(moss, noise_mask(0.1 * M, seed + 8, 0.3)), 0.04 * M), MOSS_LIT, relief=False),
            Part(_grass(w, tufts, 5, 0.28 * M), GRASS),
        ]

    half = 0.75 * M * scale
    return PropModel(stem, parts, materials, FRONT, canvas=(int(70 * scale) + 16, int(56 * scale) + 16),
                     footprint=box_footprint(half, half * 0.8))


def fallen_log(stem: str, seed: int, yaw: float) -> PropModel:
    """
    Tronc couché : écorce rainurée dans le sens du bois, partie par plaques vers le bout cassé où le bois nu
    blanchit ; bout scié aux cernes nets, mousse épaisse sur le dessus, deux moignons de branches, polypores.
    """
    BARK_M, GROOVE, BARE, CUT, RING, MOSS_M, MOSS_LIT, SHELF, SPLINTER = range(9)
    materials = [make_material("bark", TRUNK), make_material("bark_groove", BARK_GROOVE, contrast=0.8),
                 make_material("bare", WOOD_LIGHT, contrast=0.8), make_material("cut", "#B89A74", contrast=0.6),
                 make_material("ring", "#8A6E52", contrast=0.6), make_material("moss", MOSS),
                 make_material("moss_light", LEAF_LIGHT, contrast=0.8), make_material("shelf", OCHRE, contrast=0.8),
                 make_material("splinter", "#C8B69A", contrast=0.6)]
    w = Weathering(seed)
    a, b = np.array([0.0, 0.34 * M, -1.5 * M]), np.array([0.0, 0.32 * M, 1.45 * M])
    cuts = [(rotation_x(w.uniform(-0.9, -0.5)) @ rotation_y(w.uniform(-0.6, 0.6)), -1.3 * M + w.uniform(0.0, 0.25) * M) for _ in range(3)]
    splinters = [(np.array([np.sin(t) * 0.24 * M, 0.34 * M + np.cos(t) * 0.24 * M, -1.25 * M]),
                  np.array([np.sin(t) * 0.27 * M, 0.34 * M + np.cos(t) * 0.27 * M, -1.25 * M - w.uniform(0.25, 0.45) * M]))
                 for t in (-1.0, 0.0, 1.1)]
    stubs = [((0.0, 0.5 * M, z * M), (s * 0.4 * M, w.uniform(0.8, 1.0) * M, z * M + w.uniform(-0.2, 0.2) * M)) for z, s in ((-0.55, -1), (0.6, 1))]

    def parts() -> list[Part]:
        log = lambda p: capsule(p, a, b, 0.36 * M, 0.31 * M)
        sawn = lambda p: np.maximum(log(p), p[:, 2] - 1.5 * M)
        body = lambda p: _union(np.maximum(sawn(p), _union(*(-(p @ c)[:, 2] + h for c, h in cuts))),
                                *(capsule(p, s, e, 0.11 * M, 0.06 * M) for s, e in stubs))
        bare = both(noise_mask(0.22 * M, seed + 1, 0.4), lambda p: p[:, 2] + 0.85 * M)
        moss = both(lambda p: 0.6 * M - p[:, 1], noise_mask(0.4 * M, seed + 2, 0.6))
        face = lambda p: np.maximum(np.abs(p[:, 2] - 1.5 * M) - 0.02 * M, log(p) - 0.01 * M)
        radial = lambda q: np.stack([np.hypot(q[:, 0], q[:, 1] - b[1]), q[:, 1], q[:, 2]], axis=1)
        shelves = [(np.array([0.35 * M, 0.3 * M, z * M]), s * M, np.pi / 2) for z, s in ((-0.15, 0.14), (0.1, 0.1))]
        return [
            Part(body, BARK_M),
            Part(_grooves(body, seed + 3, axis=2, period=0.12 * M), GROOVE),
            Part(painted(body, bare, 0.03 * M), BARE),
            Part(painted(body, moss, 0.03 * M), MOSS_M),
            Part(painted(body, both(moss, noise_mask(0.12 * M, seed + 4, 0.3)), 0.04 * M), MOSS_LIT, relief=False),
            Part(face, CUT),
            Part(lambda p: np.maximum(face(p) - 0.005 * M, bands(0, 0.08 * M, 0.025 * M, frame=radial)(p)), RING),
            Part(lambda p: _union(*(capsule(p, s, e, 0.07 * M, 0.015 * M) for s, e in splinters)), SPLINTER),
            Part(_shelves(shelves), SHELF),
        ]

    return PropModel(stem, parts, materials, yaw, canvas=(100, 64), footprint=box_footprint(0.36 * M, 1.55 * M))


# ---------------------------------------------------------------------------------------------------------------------
# Vestiges envahis
# ---------------------------------------------------------------------------------------------------------------------

def _climbing_ivy(w: Weathering, x0: float, face_z: float, climb: float, spread: float = 0.3 * M, leaves: int = 2):
    """
    Lierre sur une face : deux ou trois tiges qui grimpent en zigzag depuis le pied et s'écartent en montant, chargées
    de feuilles plaquées ; renvoie (tiges, centres, normales, tailles).
    """
    stems, centers, normals, sizes = [], [], [], []
    for _ in range(int(w.uniform(2, 3.99))):
        x, y = x0 + w.uniform(-0.15, 0.15) * M, 0.05 * M
        points = [np.array([x, y, face_z + 0.02 * M])]
        while y < climb:
            x += w.uniform(-spread, spread)
            y += w.uniform(0.16, 0.26) * M
            points.append(np.array([x, min(y, climb), face_z + 0.02 * M]))
            for _ in range(leaves):
                centers.append(points[-1] + np.array([w.uniform(-0.1, 0.1) * M, w.uniform(-0.08, 0.08) * M, 0.04 * M]))
                normals.append(np.array([w.uniform(-0.4, 0.4), w.uniform(0.0, 0.6), 1.0]))
                sizes.append(w.uniform(0.11, 0.16) * M * (1.0 - 0.3 * y / climb))
        stems.append(points)
    return stems, centers, normals, sizes


def ruin_wall(stem: str, seed: int) -> PropModel:
    """
    Pan de mur de briques : sommet effondré en redans, appareil en quinconce, plaques d'enduit restées accrochées,
    soubassement de pierre ; mousse sur chaque redan, lierre qui grimpe en tiges chargées de feuilles, briques tombées
    et herbe au pied.
    """
    BRICK_M, JOINT, PLASTER, STONE, MOSS_M, MOSS_LIT, STEM_M, LEAF, LEAF_LIT, LOOSE, GRASS = range(11)
    materials = [make_material("brick", BRICK), make_material("mortar", MORTAR, contrast=0.6),
                 make_material("plaster", "#B9B09C", contrast=0.8), make_material("stone", "#7E7C72"),
                 make_material("moss", MOSS), make_material("moss_light", LEAF_LIGHT, contrast=0.8),
                 make_material("ivy_stem", "#4A3A2C", contrast=0.6), make_material("ivy", IVY_LEAF),
                 make_material("ivy_light", MOSS, contrast=0.8), make_material("loose_brick", "#9A6448"),
                 make_material("grass", MOSS, contrast=0.8)]
    w = Weathering(seed)
    course = 0.2 * M
    # Redans : colonnes de briques dont le sommet tombe sur une assise, plus haut d'un côté.
    columns = [(x * M, round(w.uniform(0.35, 1.0) * (1.2 + 0.9 * (1.0 - abs(x + 0.4))) * M / course) * course)
               for x in np.linspace(-1.2, 1.2, 7)]
    stems, centers, normals, sizes = _climbing_ivy(w, -0.75 * M, 0.2 * M, 1.25 * M, leaves=1)
    loose = [(np.array([w.uniform(-1.3, 1.3) * M, 0.06 * M, w.uniform(0.35, 0.7) * M]), rotation_y(w.uniform(0, np.pi)) @ rotation_z(w.uniform(-0.3, 0.3)))
             for _ in range(7)]
    tufts = [(x * M, z * M) for x, z in ((-1.35, 0.35), (0.4, 0.4), (1.2, 0.3))]

    def parts() -> list[Part]:
        wall = lambda p: _union(*(rounded_box(p, (x, h / 2 + 0.2 * M, 0), (0.2 * M, h / 2, 0.2 * M), 0.02 * M) for x, h in columns))
        plinth = lambda p: rounded_box(p, (0, 0.11 * M, 0), (1.45 * M, 0.11 * M, 0.25 * M), 0.03 * M)
        plaster = both(noise_mask(0.45 * M, seed + 1, 0.35), lambda p: 0.15 * M - p[:, 2], lambda p: 0.4 * M - p[:, 1])
        moss = both(_top_skin(wall, 0.07 * M), noise_mask(0.2 * M, seed + 2, 0.55))
        leaves = _bounded(_leaf_cluster(centers, normals, sizes), lambda p: rounded_box(p, (-0.6 * M, 0.9 * M, 0.25 * M), (1.0 * M, 0.95 * M, 0.2 * M), 0.0))
        return [
            Part(wall, BRICK_M),
            Part(painted(wall, bricks(course, 0.42 * M, 0.05 * M)), JOINT, relief=False),
            Part(painted(wall, plaster, 0.02 * M), PLASTER),
            Part(plinth, STONE),
            Part(painted(wall, moss, 0.02 * M), MOSS_M),
            Part(painted(wall, both(moss, noise_mask(0.1 * M, seed + 3, 0.3)), 0.03 * M), MOSS_LIT, relief=False),
            Part(lambda p: _union(*(_branch_chain(s, 0.03 * M, 0.02 * M)(p) for s in stems)), STEM_M),
            Part(leaves, LEAF),
            Part(painted(leaves, noise_mask(0.12 * M, seed + 4, 0.35), 0.015 * M), LEAF_LIT, relief=False),
            Part(lambda p: _union(*(rounded_box(p, c, (0.13 * M, 0.06 * M, 0.07 * M), 0.015 * M, r) for c, r in loose)), LOOSE),
            Part(_grass(w, tufts, 6, 0.3 * M), GRASS),
        ]

    return PropModel(stem, parts, materials, -AXIS_X_YAW, canvas=(100, 96), footprint=box_footprint(1.55 * M, 0.26 * M))


def ivy_lamppost(stem: str, seed: int) -> PropModel:
    """
    Candélabre de fonte vert sombre, penché : socle mouluré, fût, crosse et lanterne à quatre vitres (une brisée),
    coulures de rouille ; le lierre a pris le fût jusqu'à mi-hauteur et pend de la crosse.
    """
    IRON, RUST_M, RUST_LIT, GLASS, GLASS_BROKEN, STEM_M, LEAF, LEAF_LIT, GRASS = range(9)
    materials = [make_material("iron", IRON_GREEN), make_material("rust", RUST), make_material("rust_light", RUST_ORANGE, contrast=0.7),
                 make_material("glass", "#C8C6A8", contrast=0.5), make_material("glass_broken", "#2A2C2A", contrast=0.4),
                 make_material("ivy_stem", "#4A3A2C", contrast=0.6), make_material("ivy", IVY_LEAF),
                 make_material("ivy_light", MOSS, contrast=0.8), make_material("grass", MOSS, contrast=0.8)]
    w = Weathering(seed)
    bend = rotation_z(w.uniform(0.06, 0.12))
    pole_top = 3.3 * M
    lantern = np.array([0.55 * M, 3.05 * M, 0.0])
    stems, centers, normals, sizes = [], [], [], []
    for k in range(2):
        a0 = k * np.pi + w.uniform(-0.4, 0.4)
        points = []
        for t in np.linspace(0.0, 1.0, 12):
            a = a0 + t * 2.2 * np.pi
            radial = np.array([np.sin(a), 0.0, np.cos(a)])
            points.append(bend @ np.array([0.0, t * 1.5 * M, 0.0]) + radial * 0.11 * M)
            if w.uniform(0, 1) < 0.7 - 0.35 * t:
                centers.append(points[-1] + radial * 0.05 * M)
                normals.append(radial + np.array([0.0, 0.4, 0.0]))
                sizes.append(w.uniform(0.11, 0.15) * M)
        stems.append(points)
    for t in (0.15, 0.45):
        anchor = bend @ np.array([lantern[0] * t, pole_top + 0.15 * M * np.sin(np.pi * t), 0.0])
        for drop in (0.12, 0.3, 0.48):
            centers.append(anchor + np.array([w.uniform(-0.05, 0.05) * M, -drop * M, 0.05 * M]))
            normals.append(np.array([0.0, 0.3, 1.0]))
            sizes.append(0.1 * M)

    def parts() -> list[Part]:
        local = lambda p: p @ bend
        pole = lambda p: _union(capsule(local(p), (0, 0.4 * M, 0), (0, pole_top, 0), 0.085 * M, 0.06 * M),
                                cylinder(local(p), (0, 0.22 * M, 0), 0.15 * M, 0.22 * M, 0.04 * M),
                                cylinder(local(p), (0, 0.5 * M, 0), 0.11 * M, 0.05 * M, 0.02 * M),
                                cylinder(local(p), (0, 1.4 * M, 0), 0.085 * M, 0.04 * M, 0.02 * M))
        arm = lambda p: _union(*(capsule(local(p), a, c, 0.04 * M) for a, c in (
            ((0, pole_top - 0.05 * M, 0), (0.3 * M, pole_top + 0.12 * M, 0)), ((0.3 * M, pole_top + 0.12 * M, 0), (lantern[0], pole_top + 0.05 * M, 0)),
            ((lantern[0], pole_top + 0.05 * M, 0), (lantern[0], lantern[1] + 0.35 * M, 0)))))
        frame = lambda p: _union(rounded_box(local(p), lantern + np.array([0.0, 0.21 * M, 0.0]), (0.2 * M, 0.045 * M, 0.2 * M), 0.02 * M),
                                 rounded_box(local(p), lantern - np.array([0.0, 0.21 * M, 0.0]), (0.12 * M, 0.04 * M, 0.12 * M), 0.02 * M),
                                 capsule(local(p), lantern + np.array([0.0, 0.25 * M, 0.0]), lantern + np.array([0.0, 0.4 * M, 0.0]), 0.08 * M, 0.02 * M))
        panes = lambda p: rounded_box(local(p), lantern, (0.15 * M, 0.19 * M, 0.15 * M), 0.01 * M, rotation_y(np.pi / 4))
        iron = lambda p: _union(pole(p), arm(p), frame(p))
        streaks = both(noise_mask(0.16 * M, seed + 1, 0.3, (1.0, 4.0, 1.0)), lambda p: p[:, 1] - 2.6 * M)
        leaves = _bounded(_leaf_cluster(centers, normals, sizes), lambda p: capsule(p, (0, 0, 0), (0.3 * M, 3.4 * M, 0), 0.45 * M))
        return [
            Part(iron, IRON),
            Part(painted(iron, streaks, 0.015 * M), RUST_M),
            Part(painted(iron, both(streaks, noise_mask(0.08 * M, seed + 2, 0.3)), 0.02 * M), RUST_LIT, relief=False),
            Part(panes, GLASS),
            Part(painted(panes, lambda p: np.maximum(local(p)[:, 0] - lantern[0] + 0.02 * M, local(p)[:, 1] - lantern[1] - 0.04 * M), 0.012 * M), GLASS_BROKEN, relief=False),
            Part(lambda p: _union(*(_branch_chain(s, 0.03 * M, 0.02 * M)(p) for s in stems)), STEM_M),
            Part(leaves, LEAF),
            Part(painted(leaves, noise_mask(0.12 * M, seed + 3, 0.35), 0.015 * M), LEAF_LIT, relief=False),
            Part(_grass(w, [(-0.2 * M, 0.15 * M), (0.25 * M, 0.1 * M)], 5, 0.28 * M), GRASS),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(60, 120), footprint=box_footprint(0.2 * M, 0.2 * M))


def rusty_sign(stem: str, seed: int) -> PropModel:
    """
    Panneau de danger penché : triangle à bord rouge, fond blanc passé, pictogramme noir, mangé de rouille en plaques
    et en coulures ; poteau galvanisé, herbe au pied.
    """
    POST, BORDER, FIELD, SYMBOL, RUST_M, RUST_LIT, GRASS = range(7)
    materials = [make_material("post", "#8A8A84"), make_material("border", SIGN_RED, contrast=0.8),
                 make_material("field", SIGN_WHITE, contrast=0.6), make_material("symbol", "#2A2626", contrast=0.4),
                 make_material("rust", RUST), make_material("rust_light", RUST_ORANGE, contrast=0.7),
                 make_material("grass", MOSS, contrast=0.8)]
    w = Weathering(seed)
    tilt = rotation_z(w.uniform(-0.12, 0.12)) @ rotation_x(w.uniform(-0.05, 0.08))
    center = np.array([0.0, 1.8 * M, 0.06 * M])

    def triangle(p: np.ndarray, size: float) -> np.ndarray:
        """Triangle équilatéral pointe en haut, dans le plan de la plaque (distance 2D)."""
        q = (p @ tilt) - center
        x, y = np.abs(q[:, 0]), q[:, 1] + size * 0.29
        edge = np.maximum(x * 0.866 + y * 0.5 - size * 0.5, -y)
        return edge

    def parts() -> list[Part]:
        plate = lambda p: np.maximum(triangle(p, 0.78 * M) * 0.8, np.abs(((p @ tilt) - center)[:, 2]) - 0.025 * M) - 0.01 * M
        inner = lambda p: triangle(p, 0.55 * M)
        bar = lambda p: np.maximum(np.abs(((p @ tilt) - center)[:, 0]) - 0.035 * M, np.abs(((p @ tilt) - center)[:, 1] + 0.01 * M) - 0.11 * M)
        dot = lambda p: np.hypot(((p @ tilt) - center)[:, 0], ((p @ tilt) - center)[:, 1] + 0.2 * M) - 0.04 * M
        rust = either(noise_mask(0.18 * M, seed + 1, 0.3), both(noise_mask(0.1 * M, seed + 2, 0.4, (1.0, 4.0, 1.0)), lambda p: 1.75 * M - p[:, 1]))
        return [
            Part(lambda p: _union(capsule(p @ tilt, (0, 0, 0), (0, 2.05 * M, 0), 0.045 * M),
                                  rounded_box(p @ tilt, (0, 1.8 * M, 0.01 * M), (0.06 * M, 0.2 * M, 0.03 * M), 0.01 * M)), POST),
            Part(plate, BORDER),
            Part(painted(plate, inner), FIELD, relief=False),
            Part(painted(plate, both(inner, lambda p: np.minimum(bar(p), dot(p)))), SYMBOL, relief=False),
            Part(painted(plate, rust, 0.02 * M), RUST_M, relief=False),
            Part(painted(plate, both(rust, noise_mask(0.07 * M, seed + 3, 0.3)), 0.025 * M), RUST_LIT, relief=False),
            Part(_grass(w, [(-0.1 * M, 0.1 * M), (0.15 * M, -0.05 * M)], 6, 0.3 * M), GRASS),
        ]

    return PropModel(stem, parts, materials, float(np.radians(20.0)), canvas=(48, 96))


def overgrown_car(stem: str, seed: int, yaw: float) -> PropModel:
    """
    Voiture de la ville reprise par la forêt : mousse épaisse peinte sur le toit et le capot, lierre qui pend des
    vitres, herbes et fougères qui mangent les roues, un jeune frêne qui a percé le toit.
    """
    base = car(stem, "teal", yaw, seed)
    offset = len(base.materials)
    materials = list(base.materials) + [make_material("moss_thick", MOSS), make_material("moss_top", LEAF_LIGHT, contrast=0.8),
                                        make_material("sapling", TRUNK), make_material("leaf", MOSS),
                                        make_material("leaf_top", LEAF_LIGHT, contrast=0.9), make_material("ivy", IVY_LEAF),
                                        make_material("grass", MOSS, contrast=0.8)]
    MOSS_M, MOSS_TOP, SAPLING, LEAF, LEAF_TOP, IVY_M, GRASS = range(offset, offset + 7)
    w = Weathering(seed + 500)
    trunk = _gnarled(w, np.array([0.0, 1.2 * M, -0.3 * M]), np.array([0.1, 1.0, 0.05]), 1.2 * M, 3, wobble=0.12)
    crown = [trunk[-1] + np.array([w.uniform(-0.45, 0.45) * M, w.uniform(0.1, 0.55) * M, w.uniform(-0.35, 0.35) * M]) for _ in range(5)]
    twigs = [_gnarled(w, trunk[-2 + (k % 2)], c - trunk[-2 + (k % 2)], np.linalg.norm(c - trunk[-2 + (k % 2)]) * 0.8, 2, wobble=0.2)
             for k, c in enumerate(crown[:3])]
    # Lierre qui pend des vitres : de petites feuilles en chapelet le long des portières.
    drapes = [(np.array([x * 0.9 * M, (1.0 - k * 0.14 - w.uniform(0.0, 0.05)) * M, z * M + w.uniform(-0.06, 0.06) * M]), np.array([x, 0.3, 0.2]))
              for x in (-1, 1) for z, n in ((-0.7, 4), (0.25, 3)) for k in range(n)]
    tufts = [(x * 0.95 * M, z * 1.3 * M) for x in (-1, 1) for z in (-1, 1)] + [(0.0, 2.25 * M), (-1.05 * M, 0.0)]

    def parts() -> list[Part]:
        body = lambda p: np.minimum(rounded_box(p, (0.0, 0.62 * M, 0.0), (0.88 * M, 0.34 * M, 2.05 * M), 0.22 * M),
                                    rounded_box(p, (0, 1.12 * M, -0.25 * M), (0.78 * M, 0.3 * M, 1.05 * M), 0.28 * M))
        cover = both(_top_skin(body, 0.1 * M), noise_mask(0.4 * M, seed + 2, 0.38))
        leaves = _leaf_mass(crown, [np.array([0.34, 0.28, 0.32]) * M] * len(crown), seed + 6, per=6, ratio=0.5)
        return base.parts() + [
            Part(painted(body, cover, 0.04 * M), MOSS_M),
            Part(painted(body, both(cover, noise_mask(0.12 * M, seed + 3, 0.3)), 0.05 * M), MOSS_TOP, relief=False),
            Part(lambda p: np.minimum(_branch_chain(trunk, 0.08 * M, 0.05 * M)(p), _union(*(_branch_chain(t, 0.04 * M, 0.02 * M)(p) for t in twigs))), SAPLING),
            Part(leaves, LEAF),
            Part(painted(leaves, lambda p: trunk[-1][1] + 0.2 * M - p[:, 1] + (value_noise(p, 0.2 * M, seed + 7) - 0.5) * 0.3 * M), LEAF_TOP, relief=False),
            Part(_leaf_cluster([c for c, _ in drapes], [n for _, n in drapes], [0.1 * M] * len(drapes)), IVY_M),
            Part(_grass(w, tufts, 6, 0.4 * M), GRASS),
        ]

    return PropModel(stem, parts, materials, yaw, canvas=(110, 120), footprint=base.footprint)


# ---------------------------------------------------------------------------------------------------------------------
# Catalogue
# ---------------------------------------------------------------------------------------------------------------------

def catalog() -> list[PropModel]:
    models: list[PropModel] = []
    # Chêne : large couronne, tronc épais ; deux tirages.
    models += tree("prop_tree_large", 301, 7.0 * M, 2.7 * M, 0.34 * M, 24, TRUNK, MOSS, LEAF_LIGHT, True, (150, 190))
    models += tree("prop_tree_large_v2", 302, 6.6 * M, 2.5 * M, 0.32 * M, 22, TRUNK, CANOPY_DARK, MOSS, True, (150, 190),
                   shade="#22471F")
    # Jeune arbre : couronne ronde et claire.
    models += tree("prop_tree_medium", 311, 5.0 * M, 1.8 * M, 0.2 * M, 16, EARTH, MOSS, LEAF_YELLOW, False, (110, 150))
    # Bouleau : tronc pâle barré de noir, couronne haute et légère.
    models += tree("prop_tree_birch", 321, 5.4 * M, 1.6 * M, 0.16 * M, 16, "#D8D0C0", LEAF_LIGHT, LEAF_YELLOW, False, (100, 160),
                   style="birch", shade=MOSS)
    models += [
        strangled_tree("prop_tree_strangled", 331),
        bush("prop_bush", 341, None),
        bush("prop_bush_v2", 342, FLOWER_VIOLET),
        fern("prop_fern", 351),
        flowers("prop_flowers", 361, FLOWER_YELLOW, HEART),
        flowers("prop_flowers_v2", 362, FLOWER_VIOLET, FLOWER_YELLOW),
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
