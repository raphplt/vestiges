"""
La Barrière, boss intermédiaire (plan 07, lot B2a) : la barrière en haut de la Montée devenue chose.

Une grille municipale en fer forgé, entre des piliers de pierre ; ses battants sont cadenassés par la chaîne que la
Forgeuse a forgée. Les cadenas portent la lumière hostile des créatures (vert-acide) : ce sont les points à frapper.
Des ailes de chaînes basses la prolongent hors de l'écran ; des poings passent entre les barreaux.

Échelle de boss : un battant fait ~100 px de large à l'écran (5,8 m), la grille dépasse deux fois la taille d'un
personnage. Le mur est modélisé le long de l'axe X local (épaisseur en Z) : lacet 0 = face à la caméra, horizontal à
l'écran ; lacet ~90° = de profil, vertical à l'écran (DECISIONS §85 : l'orientation suit le cap du joueur).
"""
from __future__ import annotations

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, rounded_box, rotation_x, rotation_y, rotation_z, sphere
from ._kit import M, PropModel, Weathering, box_footprint, project_ground

# Lacets des deux orientations. De profil exact, les barreaux se recouvrent en une seule masse : un léger
# trois-quarts les sépare tout en gardant la grille presque verticale à l'écran.
YAW_HORIZONTAL = 0.0
YAW_VERTICAL = float(np.radians(72.0))

SPAN_WIDTH = 5.8 * M
BAR_TOP = 4.0 * M
BAR_SPACING = 0.42 * M
RAIL_LOW = 0.35 * M
RAIL_HIGH = 3.7 * M
RAIL_MID = 2.0 * M
PILLAR_HALF = 0.42 * M
PILLAR_HEIGHT = 4.8 * M
SPAN_CANVAS = (150, 170)
PILLAR_CANVAS = (70, 150)

IRON, RUST, STONE, CHAIN, BRASS, EYE = range(6)


def _materials() -> list:
    return [
        make_material("iron", "#3B4150", contrast=0.9),
        make_material("rust", "#6A4430"),
        make_material("stone", "#6B6161", contrast=0.8),
        make_material("chain", "#5E6168", contrast=0.9),
        make_material("brass", "#A8843A"),
        make_emissive("eye", "#7FFF00"),
    ]


def _union(*distances: np.ndarray) -> np.ndarray:
    result = distances[0]
    for d in distances[1:]:
        result = np.minimum(result, d)
    return result


def _ring(p: np.ndarray, center, major: float, minor: float, rotation: np.ndarray | None = None) -> np.ndarray:
    """Tore dans le plan XY local : un maillon de chaîne."""
    local = p - np.asarray(center)
    if rotation is not None:
        local = local @ rotation
    # Maillon allongé : on étire le tore en X en écrasant la coordonnée.
    radial = np.linalg.norm(np.stack([local[:, 0] * 0.7, local[:, 1]], axis=1), axis=1) - major
    return np.linalg.norm(np.stack([radial, local[:, 2]], axis=1), axis=1) - minor


def _chain(points: list[np.ndarray], link: float, minor: float):
    """Chaîne posée le long d'une polyligne : maillons alternés à plat et de chant, orientés selon le tracé."""
    links = []
    for a, b in zip(points[:-1], points[1:]):
        a, b = np.asarray(a, dtype=np.float64), np.asarray(b, dtype=np.float64)
        segment = b - a
        length = float(np.linalg.norm(segment))
        count = max(1, int(round(length / (link * 1.5))))
        heading = np.arctan2(segment[1], segment[0])
        tilt = rotation_z(heading)
        for i in range(count):
            center = a + segment * ((i + 0.5) / count)
            # Un maillon sur deux tourne d'un quart de tour autour de l'axe de la chaîne.
            rotation = tilt if (len(links) % 2 == 0) else tilt @ rotation_x(np.pi / 2)
            links.append((center, rotation))
    return lambda p: _union(*(_ring(p, c, link, minor, r) for c, r in links))


def _catenary(x0: float, x1: float, y: float, sag: float, z: float, steps: int = 6) -> list[np.ndarray]:
    xs = np.linspace(x0, x1, steps + 1)
    t = (xs - x0) / max(x1 - x0, 1e-6)
    return [np.array([x, y - sag * 4 * u * (1 - u), z]) for x, u in zip(xs, t)]


def _hinged(distance, hinge_x: float, angle: float):
    """Un battant pivoté autour de son gond (axe vertical en x = hinge_x) : il s'ouvre vers la caméra."""
    rotation = rotation_y(angle)
    pivot = np.array([hinge_x, 0.0, 0.0])
    return lambda p: distance((p - pivot) @ rotation + pivot)


def _bars(w: Weathering, bent: int, missing_tips: int):
    """Barreaux à pointe de lance ; quelques-uns tordus vers la caméra (battant entamé)."""
    count = int(SPAN_WIDTH // BAR_SPACING)
    xs = [(-0.5 * count + 0.5 + i) * BAR_SPACING for i in range(count)]
    bent_set = set(int(w.uniform(0, count)) for _ in range(bent))
    tipless = set(int(w.uniform(0, count)) for _ in range(missing_tips))
    shafts, tips = [], []
    for i, x in enumerate(xs):
        if i in bent_set:
            kink_y = w.uniform(1.3, 2.6) * M
            kink = (x + w.uniform(-0.25, 0.25) * M, kink_y, w.uniform(0.35, 0.6) * M)
            shafts.append(((x, 0.1 * M, 0), kink))
            shafts.append((kink, (x, BAR_TOP, 0)))
        else:
            shafts.append(((x, 0.1 * M, 0), (x, BAR_TOP, 0)))
        if i not in tipless:
            tips.append(((x, BAR_TOP - 0.05 * M, 0), (x, BAR_TOP + 0.32 * M, 0)))
    return (lambda p: _union(*(capsule(p, a, b, 0.055 * M) for a, b in shafts)),
            lambda p: _union(*(capsule(p, a, b, 0.085 * M, 0.012 * M) for a, b in tips)))


def _frame(with_mid: bool):
    half = SPAN_WIDTH * 0.5
    rails = [((-half, RAIL_LOW, 0), (half, RAIL_LOW, 0)), ((-half, RAIL_HIGH, 0), (half, RAIL_HIGH, 0))]
    if with_mid:
        rails.append(((-half, RAIL_MID, 0), (half, RAIL_MID, 0)))
        # Montants épais du battant et écharpe en diagonale : on lit un vantail, pas une travée fixe.
        rails += [((-half + 0.1 * M, 0.1 * M, 0), (-half + 0.1 * M, BAR_TOP, 0)),
                  ((half - 0.1 * M, 0.1 * M, 0), (half - 0.1 * M, BAR_TOP, 0)),
                  ((-half + 0.2 * M, RAIL_LOW, 0), (half - 0.2 * M, RAIL_MID, 0))]
    radius = 0.09 * M if with_mid else 0.075 * M
    return lambda p: _union(*(capsule(p, a, b, radius) for a, b in rails))


def _padlock(center, eye: bool = True):
    """Gros cadenas de laiton ; son trou de serrure porte la lumière hostile : c'est le point à frapper."""
    cx, cy, cz = center
    body = lambda p: rounded_box(p, (cx, cy, cz), (0.34 * M, 0.38 * M, 0.14 * M), 0.08 * M)
    shackle = lambda p: _ring(p, (cx, cy + 0.42 * M, cz), 0.26 * M, 0.07 * M)
    keyhole = lambda p: sphere(p, (cx, cy, cz + 0.12 * M), 0.13 * M)
    return body, shackle, keyhole if eye else None


def _rust(w: Weathering, count: int):
    spots = [(w.uniform(-0.45, 0.45) * SPAN_WIDTH, w.uniform(0.2, 3.9) * M, 0.0) for _ in range(count)]
    return lambda p: _union(*(sphere(p, s, w.uniform(0.07, 0.11) * M) for s in spots))


def battant(stem: str, state: str, yaw: float, seed: int) -> PropModel:
    """Battant cadenassé ; state : « intact », « entame » (barreaux tordus, chaîne lâche) ou « brise » (ouvert)."""
    materials = _materials()

    def parts() -> list[Part]:
        w = Weathering(seed)
        bent = {"intact": 0, "entame": 4, "brise": 5}[state]
        shafts, tips = _bars(w, bent, 2 if state == "intact" else 4)
        frame = _frame(with_mid=True)
        rust = _rust(w, 6 if state == "intact" else 12)
        leaf = [Part(shafts, IRON), Part(tips, IRON), Part(frame, IRON), Part(rust, RUST)]
        if state == "brise":
            # Le vantail pend, ouvert vers le joueur ; la chaîne rompue et le cadenas gisent au sol.
            hinge = -SPAN_WIDTH * 0.5
            leaf = [Part(_hinged(part.distance, hinge, -1.15), part.material) for part in leaf]
            left = _chain(_catenary(-1.6 * M, -0.2 * M, 0.25 * M, 0.05 * M, 0.7 * M, 3), 0.17 * M, 0.05 * M)
            body, shackle, _ = _padlock((0.4 * M, 0.2 * M, 0.9 * M), eye=False)
            return leaf + [Part(left, CHAIN), Part(body, BRASS), Part(shackle, CHAIN)]
        sag = 0.35 * M if state == "intact" else 0.75 * M
        chain_z = 0.2 * M
        chain = _chain(_catenary(-SPAN_WIDTH * 0.5 + 0.1 * M, SPAN_WIDTH * 0.5 - 0.1 * M, RAIL_MID + 0.1 * M, sag, chain_z, 10), 0.17 * M, 0.05 * M)
        body, shackle, keyhole = _padlock((0.0, RAIL_MID + 0.1 * M - sag - 0.5 * M, chain_z + 0.06 * M))
        return leaf + [Part(chain, CHAIN), Part(body, BRASS), Part(shackle, CHAIN), Part(keyhole, EYE)]

    half = SPAN_WIDTH * 0.5
    bounds = ((-half - 0.3 * M, -0.1 * M, -3.5 * M), (half + 0.3 * M, BAR_TOP + 0.5 * M, 3.5 * M))
    return PropModel(stem, parts, materials, yaw, canvas=SPAN_CANVAS, bounds=bounds, ray_range=400.0,
                     footprint=box_footprint(half, 0.15 * M) if state != "brise" else None)


def fixed_span(stem: str, yaw: float, seed: int) -> PropModel:
    """Travée fixe : barreaux et deux traverses, sans cadenas ; elle ne se brise pas."""
    materials = _materials()

    def parts() -> list[Part]:
        w = Weathering(seed)
        shafts, tips = _bars(w, 0, 1)
        return [Part(shafts, IRON), Part(tips, IRON), Part(_frame(with_mid=False), IRON), Part(_rust(w, 5), RUST)]

    half = SPAN_WIDTH * 0.5
    bounds = ((-half - 0.3 * M, -0.1 * M, -0.5 * M), (half + 0.3 * M, BAR_TOP + 0.5 * M, 0.5 * M))
    return PropModel(stem, parts, materials, yaw, canvas=SPAN_CANVAS, bounds=bounds, ray_range=400.0,
                     footprint=box_footprint(half, 0.15 * M))


def pillar(stem: str, yaw: float, seed: int) -> PropModel:
    """Pilier de pierre à chapiteau, boule de fer au sommet ; il porte les gonds."""
    materials = _materials()

    def parts() -> list[Part]:
        w = Weathering(seed)
        shaft = lambda p: rounded_box(p, (0, PILLAR_HEIGHT * 0.5, 0), (PILLAR_HALF, PILLAR_HEIGHT * 0.5, PILLAR_HALF), 0.04 * M)
        cap = lambda p: rounded_box(p, (0, PILLAR_HEIGHT + 0.1 * M, 0), (PILLAR_HALF + 0.08 * M, 0.12 * M, PILLAR_HALF + 0.08 * M), 0.04 * M)
        base = lambda p: rounded_box(p, (0, 0.15 * M, 0), (PILLAR_HALF + 0.1 * M, 0.15 * M, PILLAR_HALF + 0.1 * M), 0.04 * M)
        ball = lambda p: sphere(p, (0, PILLAR_HEIGHT + 0.42 * M, 0), 0.22 * M)
        cracks = [w.uniform(0.6, 3.2) * M for _ in range(3)]
        seams = lambda p: _union(*(rounded_box(p, (0, y, PILLAR_HALF), (PILLAR_HALF * 0.9, 0.02 * M, 0.02 * M), 0.01 * M) for y in cracks))
        hinges = lambda p: _union(*(rounded_box(p, (side * PILLAR_HALF, y, 0), (0.06 * M, 0.08 * M, 0.08 * M), 0.02 * M)
                                    for side in (-1, 1) for y in (RAIL_LOW, RAIL_HIGH)))
        return [Part(shaft, STONE), Part(cap, STONE), Part(base, STONE), Part(ball, IRON), Part(seams, RUST), Part(hinges, IRON)]

    bounds = ((-0.6 * M, -0.1 * M, -0.6 * M), (0.6 * M, PILLAR_HEIGHT + 0.8 * M, 0.6 * M))
    return PropModel(stem, parts, materials, yaw, canvas=PILLAR_CANVAS, bounds=bounds,
                     footprint=box_footprint(PILLAR_HALF + 0.1 * M, PILLAR_HALF + 0.1 * M))


WING_LENGTH = 3.4 * M


def wing(stem: str, yaw: float, seed: int) -> PropModel:
    """Aile : borne de fer basse et chaîne lourde tendue vers la borne suivante (module répétable le long de X)."""
    materials = _materials()

    def parts() -> list[Part]:
        w = Weathering(seed)
        post = lambda p: rounded_box(p, (0, 0.75 * M, 0), (0.2 * M, 0.75 * M, 0.2 * M), 0.06 * M)
        knob = lambda p: sphere(p, (0, 1.6 * M, 0), 0.2 * M)
        chain = _chain(_catenary(0.15 * M, WING_LENGTH, 1.3 * M, w.uniform(0.4, 0.5) * M, 0.0, 8), 0.17 * M, 0.05 * M)
        return [Part(post, IRON), Part(knob, IRON), Part(chain, CHAIN)]

    bounds = ((-0.4 * M, -0.1 * M, -0.5 * M), (WING_LENGTH + 0.3 * M, 2.0 * M, 0.5 * M))
    return PropModel(stem, parts, materials, yaw, canvas=(150, 90), bounds=bounds, ray_range=300.0,
                     footprint=box_footprint(0.2 * M, 0.2 * M))


def fist(stem: str, variant: str, yaw: float, seed: int) -> PropModel:
    """
    Poing qui passe entre les barreaux et frappe vers le joueur : phalanges vers l'avant (+Z local), avant-bras tendu
    vers la grille (−Z), chaîne au poignet. variant : « gantelet » (fer riveté) ou « main » (chair grise de noyé).
    """
    skin = make_material("iron", "#66707E", contrast=0.9) if variant == "gantelet" else make_material("flesh", "#7E8680", contrast=0.8)
    materials = [skin, make_material("chain", "#5E6168", contrast=0.9), make_material("rust", "#6A4430")]
    SKIN, LINK, SPOT = range(3)
    k = 1.7 * M

    def parts() -> list[Part]:
        w = Weathering(seed)
        hand = lambda p: rounded_box(p, (0, 0.55 * k, -0.05 * k), (0.4 * k, 0.3 * k, 0.28 * k), 0.12 * k)
        xs = (-0.29 * k, -0.1 * k, 0.1 * k, 0.29 * k)
        # Rangée des jointures sur le haut-avant, phalanges repliées dessous : on lit un poing fermé.
        knuckles = lambda p: _union(*(sphere(p, (x, 0.72 * k, 0.22 * k), 0.11 * k) for x in xs))
        fingers = lambda p: _union(*(capsule(p, (x, 0.62 * k, 0.3 * k), (x, 0.38 * k, 0.28 * k), 0.095 * k) for x in xs))
        thumb = lambda p: capsule(p, (0.42 * k, 0.48 * k, 0.05 * k), (0.05 * k, 0.36 * k, 0.34 * k), 0.09 * k)
        forearm = lambda p: capsule(p, (0, 0.6 * k, -0.25 * k), (0, 0.85 * k, -1.5 * k), 0.26 * k, 0.24 * k)
        cuff = _chain([np.array([-0.3 * k, 0.88 * k, -0.6 * k]), np.array([0.0, 0.98 * k, -0.62 * k]), np.array([0.3 * k, 0.88 * k, -0.6 * k])],
                      0.15 * M, 0.045 * M)
        result = [Part(hand, SKIN), Part(knuckles, SKIN), Part(fingers, SKIN), Part(thumb, SKIN), Part(forearm, SKIN), Part(cuff, LINK)]
        if variant == "gantelet":
            rivets = lambda p: _union(*(sphere(p, (x, 0.86 * k, -0.15 * k), 0.045 * k) for x in (-0.22 * k, 0.0, 0.22 * k)))
            stains = lambda p: _union(*(sphere(p, (w.uniform(-0.25, 0.25) * k, w.uniform(0.55, 0.9) * k, -w.uniform(0.2, 1.2) * k), 0.07 * k)
                                       for _ in range(4)))
            result += [Part(rivets, SPOT), Part(stains, SPOT)]
        return result

    bounds = ((-0.7 * k, -0.1 * k, -1.9 * k), (0.7 * k, 1.3 * k, 0.6 * k))
    return PropModel(stem, parts, materials, yaw, canvas=(150, 100), bounds=bounds)


def heavy_link(stem: str, edge_on: bool, seed: int) -> PropModel:
    """Maillon de la chaîne qui balaie : le jeu l'aligne le long de l'arc, à plat et de chant en alternance."""
    materials = _materials()

    def parts() -> list[Part]:
        rotation = rotation_x(np.pi / 2) if edge_on else None
        return [Part(lambda p: _ring(p, (0, 0.5 * M, 0), 0.26 * M, 0.08 * M, rotation), CHAIN)]

    return PropModel(stem, parts, materials, 0.0, canvas=(40, 40))


def catalog() -> list[PropModel]:
    models: list[PropModel] = []
    for suffix, yaw in (("h", YAW_HORIZONTAL), ("v", YAW_VERTICAL)):
        models += [
            battant(f"barrier_leaf_intact_{suffix}", "intact", yaw, 701),
            battant(f"barrier_leaf_damaged_{suffix}", "entame", yaw, 701),
            battant(f"barrier_leaf_broken_{suffix}", "brise", yaw, 701),
            fixed_span(f"barrier_span_{suffix}", yaw, 702),
            pillar(f"barrier_pillar_{suffix}", yaw, 703),
            wing(f"barrier_wing_{suffix}", yaw, 704),
        ]
    for variant in ("gantelet", "main"):
        models.append(fist(f"barrier_fist_{variant}_s", variant, 0.0, 705))
        models.append(fist(f"barrier_fist_{variant}_e", variant, float(np.radians(90.0)), 705))
    models += [heavy_link("barrier_link_flat", False, 706), heavy_link("barrier_link_edge", True, 706)]
    return models


def layout() -> dict:
    """
    Disposition lue par le jeu (Combat/Barrier) : pas écran d'un pilier au suivant, d'une borne d'aile à la suivante,
    et du dernier pilier à la première borne, par orientation. Les sprites se posent à leur pivot le long de ces pas.
    """
    stride = SPAN_WIDTH + 2.0 * PILLAR_HALF
    first_wing = PILLAR_HALF + 0.5 * M
    result = {}
    for suffix, yaw in (("h", YAW_HORIZONTAL), ("v", YAW_VERTICAL)):
        result[suffix] = {
            "pillar_stride": list(project_ground(stride, 0.0, yaw)),
            "wing_stride": list(project_ground(WING_LENGTH, 0.0, yaw)),
            "first_wing": list(project_ground(first_wing, 0.0, yaw)),
        }
    return result
