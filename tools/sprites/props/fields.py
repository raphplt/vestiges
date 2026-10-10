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
from ._flora import _align_y, _grass, _grooves, _top_skin, _union
from ._kit import AXIS_X_YAW, AXIS_Y_YAW, M, PropModel, Weathering, box_footprint
from ._surface import bands, both, bricks, cracks, either, noise_mask, painted, value_noise
from .forest import tree

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
# Tons dérivés : pied des herbes à l'ombre, cœur des fleurs.
GRASS_SHADOW = "#2E5226"
GOLD_SHADOW = "#7E8640"
POPPY_HEART = "#2A2226"
CORNFLOWER_HEART = "#2E3E7A"
DAISY = "#E8E4D0"
DAISY_HEART = "#D8A830"
# Touffes et fleurs se composent de face (x vers la droite, +z face à la caméra).
FRONT = 0.0


# ---------------------------------------------------------------------------------------------------------------------
# Végétation basse
# ---------------------------------------------------------------------------------------------------------------------

def tall_grass(stem: str, seed: int, blade: str, base: str, tip: str, ears: bool) -> PropModel:
    """
    Touffe d'herbes hautes (ou de blé retourné à l'état sauvage) : une quinzaine de brins fins qui partent d'un même
    pied et s'arquent en éventail sous le vent, sombres au pied, clairs à la pointe ; épis pour le blé, panicules
    pour l'herbe. Peu de brins, espacés : dense, la touffe se lisait comme un pavé.
    """
    BASE, BLADE, TIP = range(3)
    materials = [make_material("blade_base", base, contrast=0.8), make_material("blade", blade, contrast=0.8),
                 make_material("tip", tip, contrast=0.7)]
    w = Weathering(seed)
    wind = np.array([0.3, 0.0, 0.08])
    chains, heads = [], []
    for index in range(15):
        angle = index * 2.39996 + w.uniform(-0.3, 0.3)
        out = np.array([np.sin(angle), 0.0, np.cos(angle) * 0.7])
        root = out * w.uniform(0.05, 0.32) * M
        height = w.uniform(0.65, 1.2) * M
        lean = out * w.uniform(0.2, 0.5) + wind
        middle = root + np.array([0.0, height * 0.55, 0.0]) + lean * height * 0.25
        top = root + np.array([0.0, height, 0.0]) + lean * height * 0.75
        chains.append((root, middle, top))
        heads.append((top, top - middle))

    def blades(p: np.ndarray) -> np.ndarray:
        return _union(*(np.minimum(capsule(p, a, b, 0.04 * M, 0.026 * M), capsule(p, b, c, 0.026 * M, 0.01 * M)) for a, b, c in chains))

    def parts() -> list[Part]:
        result = [
            Part(blades, BLADE),
            Part(painted(blades, lambda p: p[:, 1] - 0.3 * M), BASE, relief=False),
        ]
        if ears:
            result.append(Part(lambda p: _union(*(ellipsoid(p, h + d / np.linalg.norm(d) * 0.08 * M, (0.045 * M, 0.12 * M, 0.045 * M),
                                                            _align_y(d)) for h, d in heads[::2])), TIP))
        else:
            result.append(Part(painted(blades, lambda p: 0.75 * M - p[:, 1]), TIP, relief=False))
            result.append(Part(lambda p: _union(*(sphere(p, h + d / np.linalg.norm(d) * 0.05 * M, 0.04 * M) for h, d in heads[::3])), TIP))
        return result

    return PropModel(stem, parts, materials, FRONT, canvas=(56, 56))


def wildflowers(stem: str, seed: int, colours: tuple[tuple[str, str], ...]) -> PropModel:
    """
    Touffe de fleurs des champs : rosette de feuilles, tiges de hauteurs inégales, corolles tournées vers la caméra
    et leur cœur sombre (coquelicot) ou clair (marguerite), quelques boutons fermés.
    """
    LEAF_M, STEM_M = range(2)
    materials = [make_material("leaf", GRASS_DARK), make_material("stem", GRASS_DARK, contrast=0.6)]
    for index, (petal, heart) in enumerate(colours):
        materials += [make_material(f"petal{index}", petal, contrast=0.7), make_material(f"heart{index}", heart, contrast=0.5)]
    w = Weathering(seed)
    heads = [np.array([w.uniform(-0.6, 0.6) * M, w.uniform(0.25, 0.55) * M, w.uniform(-0.38, 0.38) * M]) for _ in range(11)]
    buds = [np.array([w.uniform(-0.5, 0.5) * M, w.uniform(0.2, 0.45) * M, w.uniform(-0.3, 0.3) * M]) for _ in range(3)]
    blades = [(rotation_y(w.uniform(0, 2 * np.pi)) @ rotation_x(w.uniform(0.2, 0.5)), np.array([w.uniform(-0.3, 0.3) * M, 0.04 * M, w.uniform(-0.2, 0.2) * M]))
              for _ in range(7)]
    # Corolles tournées vers la caméra, pour qu'elles se lisent rondes et non en tirets.
    tilt = rotation_x(0.6)

    def parts() -> list[Part]:
        result = [
            Part(lambda p: _union(*(ellipsoid(p, c + q @ np.array([0.0, 0.0, 0.18 * M]), (0.06 * M, 0.018 * M, 0.2 * M), q.T)
                                    for q, c in blades)), LEAF_M),
            Part(lambda p: _union(*(capsule(p, (h[0] * 0.6, 0, h[2] * 0.6), h, 0.024 * M) for h in heads + buds)), STEM_M),
        ]
        for index in range(len(colours)):
            mine = heads[index::len(colours)]
            closed = buds[index::len(colours)]
            result.append(Part(lambda p, mine=mine, closed=closed: _union(
                *(ellipsoid(p, h, (0.11 * M, 0.03 * M, 0.11 * M), tilt) for h in mine),
                *(ellipsoid(p, b, (0.035 * M, 0.05 * M, 0.035 * M)) for b in closed)) if mine or closed else np.full(len(p), np.inf),
                2 + index * 2))
            result.append(Part(lambda p, mine=mine: _union(*(sphere(p, h + np.array([0.0, 0.02 * M, 0.012 * M]), 0.04 * M) for h in mine)),
                               3 + index * 2))
        return result

    return PropModel(stem, parts, materials, FRONT, canvas=(64, 44))


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

def _rough(shape, seed: int, amplitude: float = 0.035 * M, period: float = 0.22 * M):
    """Pierre bosselée : un bruit fin déforme la surface près d'elle (champ adouci pour ne pas sauter la surface)."""

    def field(p: np.ndarray) -> np.ndarray:
        d = shape(p) - amplitude
        near = d < 0.15 * M
        if near.any():
            d[near] = (d[near] + amplitude + (value_noise(p[near], period, seed) - 0.5) * 2.0 * amplitude) * 0.7
        return d

    return field


def stone_wall(stem: str, seed: int, broken: bool) -> PropModel:
    """
    Muret de pierres sèches : moellons bosselés de tailles inégales sur deux assises, couvertine de pierres posées de
    chant, lichen pâle et mousse sur le dessus, herbe au pied ; trouée et pierres roulées au sol s'il est écroulé.
    """
    STONE, STONE_DARK, LICHEN, MOSS, GRASS_M = range(5)
    materials = [make_material("stone", FIELD_STONE), make_material("stone_dark", "#5E5E52"),
                 make_material("lichen", "#B4B888", contrast=0.6), make_material("moss", GRASS_DARK),
                 make_material("grass", GRASS, contrast=0.8)]
    w = Weathering(seed)
    light, dark, fallen = [], [], []
    for row, y in enumerate((0.18, 0.5, 0.78)):
        x = -1.35 + w.uniform(0, 0.2)
        while x < 1.35:
            size = w.uniform(0.2, 0.32) if row < 2 else w.uniform(0.08, 0.12)
            height = 0.17 if row < 2 else 0.13
            gap = broken and -0.35 < x < 0.45 and (row >= 1 or w.uniform(0, 1) < 0.5)
            block = ((x * M, y * M, w.uniform(-0.03, 0.03) * M), (size * M, height * M, (0.26 if row < 2 else 0.2) * M),
                     rotation_y(w.uniform(-0.15, 0.15)) @ rotation_z(w.uniform(-0.12, 0.12) if row == 2 else 0.0))
            if gap:
                fallen.append(((x * M + w.uniform(-0.3, 0.3) * M, 0.1 * M, w.uniform(0.4, 0.75) * M),
                               (max(size, 0.14) * M, 0.11 * M, 0.17 * M), rotation_y(w.uniform(0, 3))))
            else:
                (light if w.uniform(0, 1) < 0.62 else dark).append(block)
            x += size * 2 + 0.03
    tufts = [(x * M, z * M) for x, z in ((-1.25, 0.32), (-0.4, 0.34), (0.6, 0.33), (1.3, 0.3))]

    def parts() -> list[Part]:
        bright = _rough(lambda p: _union(*(rounded_box(p, c, h, 0.06 * M, r) for c, h, r in light + fallen)), seed)
        shaded = _rough(lambda p: _union(*(rounded_box(p, c, h, 0.06 * M, r) for c, h, r in dark)), seed)
        both_stones = lambda p: np.minimum(bright(p), shaded(p))
        moss = both(_top_skin(both_stones, 0.08 * M), noise_mask(0.25 * M, seed + 2, 0.22))
        return [
            Part(bright, STONE),
            Part(shaded, STONE_DARK),
            Part(painted(both_stones, noise_mask(0.08 * M, seed + 1, 0.14)), LICHEN, relief=False),
            Part(painted(both_stones, moss, 0.02 * M), MOSS, relief=False),
            Part(_grass(w, tufts, 5, 0.32 * M), GRASS_M),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(100, 64), footprint=box_footprint(1.5 * M, 0.3 * M))


def wooden_fence(stem: str, seed: int, broken: bool) -> PropModel:
    """Clôture de piquets et de lisses : bois grisé veiné dans le sens du fil, lichen, herbe au pied des piquets."""
    WOOD, WOOD_DARK, GRAIN, LICHEN, GRASS_M = range(5)
    materials = [make_material("wood", FENCE), make_material("wood_dark", "#4A3E2E"), make_material("grain", "#3A3024", contrast=0.6),
                 make_material("lichen", "#A8B080", contrast=0.6), make_material("grass", GRASS, contrast=0.8)]
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
    tufts = [(x * M + w.uniform(-0.1, 0.1) * M, w.uniform(0.08, 0.2) * M) for x in (-1.3, 0.0, 1.3)]

    def parts() -> list[Part]:
        post_shape = lambda p: _union(*(rounded_box(p, c, (0.07 * M, 0.55 * M, 0.07 * M), 0.02 * M, r) for c, r in posts))
        rail_shape = lambda p: _union(*(capsule(p, a, b, 0.05 * M) for a, b in rails))
        return [
            Part(post_shape, WOOD_DARK),
            Part(rail_shape, WOOD),
            Part(_grooves(post_shape, seed + 1, axis=1, period=0.07 * M, coverage=0.25), GRAIN),
            Part(_grooves(rail_shape, seed + 2, axis=0, period=0.07 * M, coverage=0.25), GRAIN),
            Part(painted(lambda p: np.minimum(post_shape(p), rail_shape(p)), noise_mask(0.08 * M, seed + 3, 0.1)), LICHEN, relief=False),
            Part(_grass(w, tufts, 5, 0.3 * M), GRASS_M),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(100, 64), footprint=box_footprint(1.4 * M, 0.12 * M))


# ---------------------------------------------------------------------------------------------------------------------
# Vestiges agricoles
# ---------------------------------------------------------------------------------------------------------------------

def hay_bale(stem: str, seed: int, rotten: bool) -> PropModel:
    """
    Balle ronde oubliée : paille enroulée (spirale sur les flancs, fibres qui font le tour), filet de liage pâle, ombre
    dessous. Pourrie, elle s'affaisse, brunit, se couvre de mousse et l'herbe pousse dessus.
    """
    STRAW_M, FIBRE, NET, MOSS, GRASS_M, DAMP = range(6)
    materials = [make_material("straw", "#7A6A40" if rotten else STRAW), make_material("fibre", "#5E5236" if rotten else "#E0CE84", contrast=0.5),
                 make_material("net", "#8A8468" if rotten else "#8E7A40", contrast=0.6), make_material("moss", GRASS_DARK),
                 make_material("grass", GRASS, contrast=0.8), make_material("damp", "#4A4030", contrast=0.6)]
    w = Weathering(seed)
    sag = 0.72 if rotten else 1.0
    roll = rotation_z(np.pi / 2)
    # Herbe qui a pris racine sur la balle pourrie.
    tufts = [(w.uniform(-0.4, 0.4) * M, w.uniform(-0.3, 0.3) * M) for _ in range(3)]

    def parts() -> list[Part]:
        if rotten:
            bale = lambda p: ellipsoid(p, (0, 0.45 * M, 0), (0.75 * M, 0.45 * M, 0.66 * M))
        else:
            bale = lambda p: cylinder(p, (0, 0.62 * M, 0), 0.62 * M, 0.7 * M, 0.2 * M, roll)
        # Fibres : rayures courtes le long de x (elles font le tour du rouleau) ; spirale : cernes sur les flancs.
        fibres = noise_mask(0.05 * M, seed + 1, 0.2, (1.0, 6.0, 6.0))
        spiral = bands(0, 0.1 * M, 0.03 * M, frame=lambda q: np.stack([np.hypot(q[:, 1] - 0.62 * M * sag, q[:, 2]), q[:, 1], q[:, 0]], axis=1))
        flanks = lambda p: 0.6 * M - np.abs(p[:, 0])
        result = [
            Part(bale, STRAW_M),
            Part(painted(bale, either(both(fibres, lambda p: np.abs(p[:, 0]) - 0.62 * M), both(spiral, flanks) if not rotten else fibres)),
                 FIBRE, relief=False),
            Part(painted(bale, lambda p: p[:, 1] - 0.12 * M * (1.0 if not rotten else 2.5)), DAMP, relief=False),
        ]
        if not rotten:
            result.append(Part(lambda p: _union(*(cylinder(p, (x * M, 0.62 * M, 0), 0.632 * M, 0.025 * M, 0.01 * M, roll)
                                                  for x in (-0.38, 0.0, 0.38))), NET))
        else:
            moss = both(lambda p: 0.55 * M - p[:, 1], noise_mask(0.3 * M, seed + 2, 0.32))
            result.append(Part(painted(bale, moss, 0.03 * M), MOSS))
            result.append(Part(_grass(w, tufts, 6, 0.3 * M, ground=0.78 * M), GRASS_M))
        return result

    return PropModel(stem, parts, materials, AXIS_Y_YAW, canvas=(72, 60), footprint=box_footprint(0.75 * M, 0.66 * M))


def scarecrow(stem: str, seed: int) -> PropModel:
    """
    Épouvantail cassé : poteau penché, un bras qui a lâché, veste rouge rapiécée, paille qui sort des manches et du
    col, tête en toile cousue, chapeau mou.
    """
    POLE, SACK, CLOTH, PATCH, HAT, STRAW_M, STITCH, GRASS_M = range(8)
    materials = [make_material("pole", FENCE), make_material("sack", EARTH_LIGHT), make_material("cloth", FLOWER_RED, contrast=0.8),
                 make_material("patch", "#7A6A50", contrast=0.7), make_material("hat", "#4A3E2E"), make_material("straw", STRAW),
                 make_material("stitch", "#3A2E26", contrast=0.4), make_material("grass", GRASS, contrast=0.8)]
    w = Weathering(seed)
    lean = rotation_z(0.18)
    straws = [((x * M, 1.38 * M, 0), (x * M + w.uniform(-0.12, 0.12) * M, w.uniform(1.1, 1.2) * M, 0.05 * M)) for x in (-0.68, -0.6, -0.72)]
    straws += [((0.45 * M, 1.0 * M, 0), (0.5 * M + w.uniform(-0.05, 0.1) * M, w.uniform(0.72, 0.8) * M, 0.05 * M)) for _ in range(3)]
    straws += [((x * M, 1.62 * M, 0.05 * M), (x * 1.5 * M, w.uniform(1.68, 1.75) * M, 0.12 * M)) for x in (-0.1, 0.0, 0.1)]

    def parts() -> list[Part]:
        coat = lambda p: _union(rounded_box(p @ lean, (0, 1.2 * M, 0.02 * M), (0.3 * M, 0.34 * M, 0.14 * M), 0.08 * M),
                                capsule(p @ lean, (-0.25 * M, 1.42 * M, 0.02 * M), (-0.62 * M, 1.4 * M, 0.02 * M), 0.09 * M, 0.07 * M),
                                capsule(p @ lean, (0.25 * M, 1.42 * M, 0.02 * M), (0.42 * M, 1.05 * M, 0.06 * M), 0.09 * M, 0.07 * M))
        head = lambda p: sphere(p @ lean, (0, 1.88 * M, 0.02 * M), 0.2 * M)
        face = lambda p: np.minimum(_union(*(np.hypot(*(((p @ lean) - np.array([x * M, 1.92 * M, 0.0]))[:, [0, 1]].T)) - 0.035 * M
                                              for x in (-0.07, 0.08))),
                                    np.maximum(np.abs(((p @ lean)[:, 1]) - 1.8 * M) - 0.015 * M, np.abs((p @ lean)[:, 0]) - 0.09 * M))
        return [
            Part(lambda p: _union(capsule(p @ lean, (0, 0, 0), (0, 1.8 * M, 0), 0.06 * M),
                                  capsule(p @ lean, (-0.7 * M, 1.42 * M, -0.05 * M), (0.2 * M, 1.43 * M, -0.05 * M), 0.05 * M),
                                  capsule(p @ lean, (0.2 * M, 1.43 * M, -0.05 * M), (0.45 * M, 1.0 * M, -0.02 * M), 0.05 * M)), POLE),
            Part(head, SACK),
            Part(painted(head, face, 0.012 * M), STITCH, relief=False),
            Part(coat, CLOTH),
            Part(painted(coat, noise_mask(0.16 * M, seed + 1, 0.22), 0.015 * M), PATCH, relief=False),
            Part(lambda p: _union(cylinder(p @ lean, (0, 2.03 * M, 0), 0.3 * M, 0.02 * M, 0.01 * M, rotation_x(0.15)),
                                  cylinder(p @ lean, (0, 2.12 * M, 0), 0.15 * M, 0.09 * M, 0.03 * M)), HAT),
            Part(lambda p: _union(*(capsule(p @ lean, a, b, 0.03 * M, 0.015 * M) for a, b in straws)), STRAW_M),
            Part(_grass(w, [(0.0, 0.0), (0.2 * M, 0.15 * M)], 6, 0.35 * M), GRASS_M),
        ]

    return PropModel(stem, parts, materials, FRONT, canvas=(72, 110), footprint=box_footprint(0.12 * M, 0.12 * M))


def well(stem: str, seed: int) -> PropModel:
    """
    Puits abandonné : margelle de moellons appareillés, eau noire, deux montants et leur treuil, seau pendu à la
    corde, petit toit de planches moussu, herbe au pied.
    """
    STONE, JOINT, WOOD, GRAIN, ROOF, WATER, MOSS, ROPE, BUCKET, GRASS_M = range(10)
    materials = [make_material("stone", FIELD_STONE), make_material("joint", "#4E4E44", contrast=0.5), make_material("wood", FENCE),
                 make_material("grain", "#3A3024", contrast=0.6), make_material("roof", "#7A5A40"),
                 make_material("water", "#2A3A4A", contrast=0.5), make_material("moss", GRASS_DARK),
                 make_material("rope", "#B8A880", contrast=0.5), make_material("bucket", "#6A6460"), make_material("grass", GRASS, contrast=0.8)]
    w = Weathering(seed)
    tilt = rotation_z(w.uniform(-0.1, 0.1))
    tufts = [(np.cos(a) * 0.72 * M, np.sin(a) * 0.72 * M) for a in (0.4, 1.4, 2.6)]

    def parts() -> list[Part]:
        ring = lambda p: np.maximum(cylinder(p, (0, 0.4 * M, 0), 0.65 * M, 0.4 * M, 0.05 * M), -cylinder(p, (0, 0.45 * M, 0), 0.47 * M, 0.45 * M))
        # Appareil en quinconce déroulé autour de la margelle : coordonnée le long du tour = angle × rayon.
        unrolled = lambda q: np.stack([np.arctan2(q[:, 2], q[:, 0]) * 0.65 * M, q[:, 1], np.zeros(len(q))], axis=1)
        roof = lambda p: _union(*(rounded_box(p @ tilt, (0, 2.0 * M, z * 0.25 * M), (0.8 * M, 0.035 * M, 0.27 * M), 0.01 * M, rotation_x(z * 0.45))
                                  for z in (-1, 1)))
        posts = lambda p: _union(*(rounded_box(p, (x * 0.58 * M, 1.3 * M, 0), (0.06 * M, 0.6 * M, 0.06 * M), 0.015 * M) for x in (-1, 1)),
                                 capsule(p, (-0.6 * M, 1.6 * M, 0), (0.6 * M, 1.6 * M, 0), 0.07 * M))
        return [
            Part(ring, STONE),
            Part(painted(ring, bricks(0.2 * M, 0.38 * M, 0.04 * M, frame=unrolled)), JOINT, relief=False),
            Part(painted(ring, both(_top_skin(ring, 0.08 * M), noise_mask(0.2 * M, seed + 1, 0.6)), 0.02 * M), MOSS, relief=False),
            Part(lambda p: cylinder(p, (0, 0.55 * M, 0), 0.46 * M, 0.02 * M), WATER),
            Part(posts, WOOD),
            Part(_grooves(posts, seed + 2, axis=1, period=0.07 * M, coverage=0.25), GRAIN),
            Part(roof, ROOF),
            Part(painted(roof, noise_mask(0.25 * M, seed + 3, 0.4), 0.02 * M), MOSS, relief=False),
            Part(lambda p: capsule(p, (0.1 * M, 1.6 * M, 0.05 * M), (0.1 * M, 1.05 * M, 0.05 * M), 0.015 * M), ROPE),
            Part(lambda p: np.maximum(cylinder(p, (0.1 * M, 0.92 * M, 0.05 * M), 0.12 * M, 0.12 * M, 0.02 * M), -cylinder(p, (0.1 * M, 1.04 * M, 0.05 * M), 0.09 * M, 0.04 * M)), BUCKET),
            Part(_grass(w, tufts, 5, 0.3 * M), GRASS_M),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(72, 100), footprint=box_footprint(0.65 * M, 0.65 * M))


def standing_stone(stem: str, seed: int) -> PropModel:
    """Menhir penché : pierre haute et bosselée, fendue, plaques de lichen jaune pâle et gris, mousse et herbe au pied."""
    STONE, CRACK, LICHEN, LICHEN_GREY, MOSS, GRASS_M = range(6)
    materials = [make_material("stone", FIELD_STONE), make_material("crack", "#454538", contrast=0.5),
                 make_material("lichen", "#B8BC90", contrast=0.5), make_material("lichen_grey", "#B8B6A8", contrast=0.5),
                 make_material("moss", GRASS_DARK), make_material("grass", GRASS, contrast=0.8)]
    w = Weathering(seed)
    tilt = rotation_z(w.uniform(-0.12, -0.06)) @ rotation_x(w.uniform(-0.05, 0.05))
    tufts = [(x * M, z * M) for x, z in ((-0.45, 0.2), (0.4, 0.25), (0.05, 0.4))]

    def parts() -> list[Part]:
        stone = _rough(lambda p: np.minimum(ellipsoid(p @ tilt, (0, 1.15 * M, 0), (0.4 * M, 1.25 * M, 0.3 * M)),
                                            ellipsoid(p @ tilt, (0.05 * M, 0.35 * M, 0), (0.46 * M, 0.45 * M, 0.34 * M))), seed, 0.05 * M, 0.3 * M)
        return [
            Part(stone, STONE),
            Part(painted(stone, cracks(0.6 * M, seed + 1, 0.035 * M, reach=0.5)), CRACK, relief=False),
            Part(painted(stone, noise_mask(0.12 * M, seed + 2, 0.07)), LICHEN, relief=False),
            Part(painted(stone, noise_mask(0.09 * M, seed + 3, 0.09)), LICHEN_GREY, relief=False),
            Part(painted(stone, both(lambda p: p[:, 1] - 0.3 * M, noise_mask(0.2 * M, seed + 4, 0.6))), MOSS, relief=False),
            Part(_grass(w, tufts, 6, 0.35 * M), GRASS_M),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(48, 100), footprint=box_footprint(0.36 * M, 0.26 * M))


def rusted_plow(stem: str, seed: int) -> PropModel:
    """
    Charrue à trois socs laissée en bout de champ : une poutre, trois versoirs d'acier vrillés côté caméra dont le bord
    usé brille encore, roue de jauge, timon d'attelage ; rouille en plaques sur la poutre, en coulures sur l'acier,
    herbe qui pousse au travers. Les socs sont décalés hors de la poutre pour se lire chacun.
    """
    FRAME, BLADE, BLADE_EDGE, RUST_M, RUST_LIGHT, WHEEL, GRASS_M = range(7)
    materials = [make_material("frame", "#4E4440"), make_material("blade", "#8E887E"), make_material("blade_edge", "#D2CCBE", contrast=0.5),
                 make_material("rust", RUST), make_material("rust_light", RUST_ORANGE, contrast=0.7), make_material("wheel", "#3A3530"),
                 make_material("grass", GRASS, contrast=0.8)]
    w = Weathering(seed)
    shares = [np.array([0.28 * M, 0.3 * M, z * M]) for z in (-0.75, 0.0, 0.75)]
    hub = np.array([0.0, 0.38 * M, -1.35 * M])
    tufts = [(w.uniform(-0.5, 0.6) * M, z * M) for z in (-1.0, -0.35, 0.4, 1.0)]

    def parts() -> list[Part]:
        frame = lambda p: _union(capsule(p, (0.0, 0.72 * M, -1.3 * M), (0.0, 0.72 * M, 1.05 * M), 0.075 * M),
                                 capsule(p, (0.0, 0.72 * M, 1.05 * M), (0.0, 0.62 * M, 1.6 * M), 0.055 * M),
                                 *(capsule(p, (0.0, 0.72 * M, s[2]), s + np.array([-0.05 * M, 0.2 * M, 0.0]), 0.045 * M) for s in shares))
        blades = lambda p: _union(*(rounded_box(p, s, (0.04 * M, 0.24 * M, 0.3 * M), 0.03 * M, rotation_y(-0.5) @ rotation_x(0.35) @ rotation_z(-0.35))
                                    for s in shares))
        return [
            Part(frame, FRAME),
            Part(painted(frame, noise_mask(0.2 * M, seed + 1, 0.22)), RUST_M, relief=False),
            Part(blades, BLADE),
            Part(painted(blades, lambda p: p[:, 1] - 0.16 * M), BLADE_EDGE, relief=False),
            Part(painted(blades, both(noise_mask(0.08 * M, seed + 2, 0.35, (1.0, 3.0, 1.0)), lambda p: 0.18 * M - p[:, 1])), RUST_LIGHT, relief=False),
            Part(lambda p: _union(np.maximum(cylinder(p, hub, 0.36 * M, 0.05 * M, 0.02 * M, rotation_z(np.pi / 2)),
                                             -cylinder(p, hub, 0.26 * M, 0.2 * M, 0.0, rotation_z(np.pi / 2))),
                                  capsule(p, hub, (0.0, 0.72 * M, -1.25 * M), 0.05 * M),
                                  *(capsule(p, hub, hub + np.array([0.0, np.sin(a) * 0.3 * M, np.cos(a) * 0.3 * M]), 0.02 * M) for a in (0.3, 1.9, 3.5, 5.1))),
                 WHEEL),
            Part(_grass(w, tufts, 6, 0.4 * M), GRASS_M),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(80, 64), footprint=box_footprint(0.6 * M, 1.1 * M))


def tractor(stem: str, seed: int, husk: bool) -> PropModel:
    """
    Tracteur abandonné : capot à calandre, garde-boue sur les grandes roues, cabine vitrée, pot d'échappement ;
    peinture passée mangée de rouille en coulures. Brûlé, c'est une carcasse noire affaissée sur ses jantes.
    """
    PAINT, RUST_M, TYRE, METAL, GRASS_M, GLASS, GRILLE, TREAD = range(8)
    materials = [make_material("paint", "#3A3530" if husk else "#8E4A34"), make_material("rust", RUST),
                 make_material("tyre", "#221F24", contrast=0.5), make_material("metal", "#6B6161"), make_material("grass", GRASS, contrast=0.8),
                 make_material("glass", "#2A3644", contrast=0.6), make_material("grille", "#2E2A28", contrast=0.5),
                 make_material("tread", "#38343A", contrast=0.5)]
    w = Weathering(seed)
    drop = 0.18 * M if husk else 0.0
    tufts = [(w.uniform(-0.9, 0.9) * M, w.uniform(-1.4, 1.5) * M) for _ in range(5)]

    def parts() -> list[Part]:
        hood = lambda p: rounded_box(p, (0, 1.0 * M - drop, 0.6 * M), (0.45 * M, 0.4 * M, 0.95 * M), 0.12 * M)
        cab = lambda p: rounded_box(p, (0, 1.1 * M - drop, -0.75 * M), (0.6 * M, 0.5 * M, 0.45 * M), 0.08 * M)
        guards = lambda p: _union(*(np.maximum(np.abs(cylinder(p, (x * 0.72 * M, 0.72 * M - drop * 0.5, -0.75 * M), 0.82 * M, 0.2 * M, 0.04 * M,
                                                               rotation_z(np.pi / 2))) - 0.04 * M, 0.75 * M - drop - p[:, 1]) for x in (-1, 1)))
        body = lambda p: _union(hood(p), cab(p), guards(p))
        rear = lambda p: _union(*(cylinder(p, (x * 0.72 * M, 0.72 * M - drop * 0.5, -0.75 * M), 0.72 * M, 0.2 * M, 0.08 * M,
                                           rotation_z(np.pi / 2)) for x in (-1, 1)))
        front = lambda p: _union(*(cylinder(p, (x * 0.5 * M, 0.4 * M - drop * 0.5, 1.1 * M), 0.4 * M, 0.14 * M, 0.06 * M,
                                            rotation_z(np.pi / 2)) for x in (-1, 1)))
        tyres = lambda p: np.minimum(rear(p), front(p))
        rust = either(both(noise_mask(0.2 * M, seed + 1, 0.35, (1.0, 3.0, 1.0)), lambda p: p[:, 1] - 1.2 * M + drop),
                      noise_mask(0.25 * M, seed + 2, 0.12))
        result = [
            Part(body, PAINT),
            Part(painted(body, rust, 0.015 * M), RUST_M, relief=False),
            Part(painted(hood, both(lambda p: 1.5 * M - p[:, 2], bands(1, 0.12 * M, 0.04 * M)), 0.012 * M), GRILLE, relief=False),
            Part(lambda p: capsule(p, (0.2 * M, 1.4 * M - drop, 1.0 * M), (0.2 * M, 2.1 * M - drop, 1.0 * M), 0.07 * M), METAL),
            Part(tyres, TYRE),
            Part(painted(rear, noise_mask(0.06 * M, seed + 3, 0.3, (3.0, 1.0, 1.0))), TREAD, relief=False),
            Part(lambda p: _union(*(cylinder(p, (x * 0.94 * M, 0.72 * M - drop * 0.5, -0.75 * M), 0.3 * M, 0.02 * M, 0.01 * M, rotation_z(np.pi / 2))
                                    for x in (-1, 1))), METAL),
        ]
        if not husk:
            result.append(Part(lambda p: rounded_box(p, (0, 1.9 * M, -0.75 * M), (0.5 * M, 0.35 * M, 0.4 * M), 0.05 * M), GLASS))
            result.append(Part(lambda p: rounded_box(p, (0, 2.28 * M, -0.75 * M), (0.62 * M, 0.04 * M, 0.5 * M), 0.02 * M), PAINT))
        result.append(Part(_grass(w, tufts, 6, 0.4 * M), GRASS_M))
        return result

    return PropModel(stem, parts, materials, AXIS_Y_YAW, canvas=(110, 110), footprint=box_footprint(0.9 * M, 1.45 * M))


def wind_pump(stem: str, seed: int) -> PropModel:
    """
    Éolienne de pompage : pylône en treillis rouillé par endroits, roue de lames minces dont trois ont été arrachées,
    gouvernail de queue. Repère visible de loin.
    """
    STEEL, VANE, RUST_M, GRASS_M = range(4)
    materials = [make_material("steel", "#8A8478"), make_material("vane", "#B8B0A0", contrast=0.8), make_material("rust", RUST),
                 make_material("grass", GRASS, contrast=0.8)]
    w = Weathering(seed)
    top = 5.6 * M
    feet = [(x * 0.9 * M, 0, z * 0.9 * M) for x, z in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
    head = [(x * 0.18 * M, top, z * 0.18 * M) for x, z in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
    legs = list(zip(feet, head))
    braces = []
    for level in (0.3, 0.6):
        ring = [tuple(np.array(a) + (np.array(b) - np.array(a)) * level) for a, b in legs]
        braces += list(zip(ring, ring[1:] + ring[:1]))
        braces += [(ring[k], tuple(np.array(feet[(k + 1) % 4]) + (np.array(head[(k + 1) % 4]) - np.array(feet[(k + 1) % 4])) * (level - 0.3)))
                   for k in range(4)]
    hub = np.array([0.0, top + 0.3 * M, 0.35 * M])
    vanes = []
    for index in range(12):
        if index in (3, 4, 9):
            continue  # lames arrachées
        angle = index * np.pi / 6
        vanes.append((angle, rotation_z(-angle)))
    tufts = [(x * 0.9 * M, z * 0.9 * M) for x, z in ((-1, -1), (1, -1), (1, 1), (-1, 1))]

    def parts() -> list[Part]:
        lattice = lambda p: _union(*(capsule(p, a, b, 0.06 * M) for a, b in legs + braces))
        # Lame : plaque mince qui s'élargit vers l'extérieur, un peu vrillée comme une hélice.
        blades = lambda p: _union(*(rounded_box(p, hub + np.array([np.cos(a) * 0.75 * M, np.sin(a) * 0.75 * M, 0.1 * M]),
                                                (0.48 * M, 0.11 * M, 0.02 * M), 0.01 * M, r.T @ rotation_x(0.35)) for a, r in vanes))
        return [
            Part(lattice, STEEL),
            Part(painted(lattice, noise_mask(0.5 * M, seed + 1, 0.16)), RUST_M, relief=False),
            Part(blades, VANE),
            Part(lambda p: _union(sphere(p, hub, 0.18 * M), cylinder(p, hub, 0.3 * M, 0.03 * M, 0.0, rotation_x(np.pi / 2)),
                                  capsule(p, (0, top + 0.3 * M, 0.3 * M), (0, top + 0.3 * M, -1.1 * M), 0.07 * M),
                                  rounded_box(p, (0, top + 0.35 * M, -1.15 * M), (0.03 * M, 0.38 * M, 0.34 * M), 0.02 * M)), RUST_M),
            Part(_grass(w, tufts, 5, 0.35 * M), GRASS_M),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(90, 170), footprint=box_footprint(0.9 * M, 0.9 * M),
                     bounds=((-1.6 * M, -0.1 * M, -1.5 * M), (1.6 * M, 7.4 * M, 1.5 * M)))


def silo(stem: str, seed: int) -> PropModel:
    """
    Silo à grain effondré : tôle ondulée cerclée, sommet arraché en dents de scie, une déchirure qui montre l'intérieur
    noir, longues coulures de rouille sous les cerclages, herbe au pied.
    """
    SHEET, RIDGE, BAND, RUST_M, INTERIOR, GRASS_M = range(6)
    materials = [make_material("sheet", "#A0A098"), make_material("ridge", "#86867E", contrast=0.6), make_material("band", "#6B6161"),
                 make_material("rust", RUST), make_material("interior", "#36312C", contrast=0.5), make_material("grass", GRASS, contrast=0.8)]
    w = Weathering(seed)
    height = 4.6 * M
    tufts = [(np.cos(a) * 1.32 * M, np.sin(a) * 1.32 * M) for a in np.linspace(0.2, 3.3, 5)]
    tear = np.array([np.cos(1.3) * 1.2 * M, 2.6 * M, np.sin(1.3) * 1.2 * M])

    def parts() -> list[Part]:
        def jagged(p: np.ndarray) -> np.ndarray:
            # Sommet arraché : un plan en biais rongé par un bruit, les tôles s'arrêtent à des hauteurs inégales.
            return p[:, 1] - height + 0.25 * p[:, 0] + 0.5 * M - (value_noise(p, 0.35 * M, seed + 1) - 0.5) * 0.9 * M

        hull = lambda p: np.maximum(np.abs(cylinder(p, (0, height / 2, 0), 1.17 * M, height / 2, 0.02 * M)) - 0.04 * M, jagged(p))
        sheet = lambda p: np.maximum(hull(p), -ellipsoid(p, tear, (0.45 * M, 0.7 * M, 0.45 * M)))
        flutes = bands(0, 0.16 * M, 0.06 * M, frame=lambda q: np.stack([np.arctan2(q[:, 2], q[:, 0]) * 1.2 * M, q[:, 1], q[:, 2]], axis=1))
        def under_bands(p: np.ndarray) -> np.ndarray:
            # Négatif sur 1,6 m sous chaque cerclage : les coulures partent des cerclages.
            d = np.full(len(p), 1.0 * M)
            for y in (0.9, 2.1, 3.3):
                drop = y * M - p[:, 1]
                d = np.minimum(d, np.where(drop > 0, drop - 1.6 * M, 1.0 * M))
            return d

        rust = both(noise_mask(0.18 * M, seed + 2, 0.4, (1.0, 5.0, 1.0)), under_bands)
        return [
            Part(sheet, SHEET),
            Part(painted(sheet, flutes), RIDGE, relief=False),
            Part(painted(sheet, rust, 0.015 * M), RUST_M, relief=False),
            Part(lambda p: _union(*(np.maximum(np.maximum(cylinder(p, (0, y * M, 0), 1.23 * M, 0.05 * M), jagged(p)),
                                               -ellipsoid(p, tear, (0.5 * M, 0.75 * M, 0.5 * M))) for y in (0.9, 2.1, 3.3))), BAND),
            Part(lambda p: cylinder(p, (0, height / 2, 0), 1.08 * M, height / 2 - 0.05 * M), INTERIOR),
            Part(_grass(w, tufts, 6, 0.4 * M), GRASS_M),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(110, 170), footprint=box_footprint(1.2 * M, 1.2 * M),
                     bounds=((-1.6 * M, -0.1 * M, -1.6 * M), (1.6 * M, 5.2 * M, 1.6 * M)))


# ---------------------------------------------------------------------------------------------------------------------
# Catalogue
# ---------------------------------------------------------------------------------------------------------------------

def catalog() -> list[PropModel]:
    models: list[PropModel] = []
    # Arbre isolé des champs : chêne large et bas, feuillage de la palette des champs.
    models += tree("prop_solitary_tree", 501, 6.2 * M, 2.9 * M, 0.36 * M, 24, "#4A3728", GRASS_DARK, GRASS, False, (160, 190),
                   shade="#284A22", cluster=0.36 * M)
    models += [
        tall_grass("prop_tall_grass", 511, GRASS, GRASS_DARK, GRASS_PALE, False),
        tall_grass("prop_tall_grass_v2", 512, GRASS_DARK, GRASS_SHADOW, GRASS, False),
        tall_grass("prop_tall_grass_v3", 513, GRASS_GOLD, GOLD_SHADOW, GRASS_PALE, True),
        wildflowers("prop_poppies_red", 521, ((FLOWER_RED, POPPY_HEART),)),
        wildflowers("prop_poppies_blue", 522, ((FLOWER_BLUE, CORNFLOWER_HEART),)),
        wildflowers("prop_poppies_mixed", 523, ((FLOWER_RED, POPPY_HEART), (FLOWER_BLUE, CORNFLOWER_HEART), (DAISY, DAISY_HEART))),
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
