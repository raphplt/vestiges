"""
Mobilier des Ruines Urbaines (plan 08, lot P1) : ce que la ville a laissé dans ses rues.

Tout est à l'échelle réelle du personnage (1 m ≈ 27,5 unités) : une voiture fait deux personnages et demi de long,
une benne arrive à l'épaule. Usure reproductible par graine : rouille, mousse, peinture passée. Les couleurs sont
tenues en valeur par rapport au sol gris-brun des ruines pour que chaque objet se lise d'un coup d'œil.
"""
from __future__ import annotations

import numpy as np

from ..palette import make_material
from ..render import Part
from ..sdf import capsule, cylinder, ellipsoid, rounded_box, rotation_x, rotation_y, rotation_z, sphere
from ._kit import AXIS_X_YAW, AXIS_Y_YAW, M, PropModel, Weathering, box_footprint
from ._surface import bands, both, noise_mask, painted, value_noise

ROT_WHEEL = rotation_z(np.pi / 2)
# Convention des primitives orientées : local = (p − centre) @ rotation, donc un point local va en monde par rotation @ local.


def _union(*distances: np.ndarray) -> np.ndarray:
    result = distances[0]
    for d in distances[1:]:
        result = np.minimum(result, d)
    return result


def _spots(points, radius_low: float, radius_high: float, w: Weathering):
    radii = [w.uniform(radius_low, radius_high) for _ in points]
    return lambda p: _union(*(sphere(p, c, r) for c, r in zip(points, radii)))


def _patches(surface, radius_low: float, radius_high: float, w: Weathering, flush: float = 0.8):
    """Taches affleurantes (rouille, peinture écaillée) : la sphère s'enfonce, seule une calotte dépasse."""
    spots = []
    for point, normal in surface:
        radius = w.uniform(radius_low, radius_high)
        spots.append((point - normal * radius * flush, radius))
    return lambda p: _union(*(sphere(p, c, r) for c, r in spots))


# ---------------------------------------------------------------------------------------------------------------------
# Voitures
# ---------------------------------------------------------------------------------------------------------------------

CAR_PAINTS = {"red": "#94503F", "teal": "#3F7379", "cream": "#BDB08A"}


def car(stem: str, paint: str, yaw: float, seed: int, open_doors: tuple[str, ...] = ()) -> PropModel:
    """
    `open_doors` (exode, plan 39 B3) : portières laissées ouvertes, parmi "front", "rear" (côté +x), "front_far",
    "rear_far" (côté −x) et "trunk". Vide, le modèle reste celui des voitures déjà validées.
    """
    PAINT, GLASS, TYRE, METAL, RUST, MOSS, LAMP, TAIL, GLINT, SEAM, MOSS_LIGHT, INTERIOR = range(12)
    materials = [
        make_material("paint", CAR_PAINTS[paint]),
        make_material("glass", "#2A3644", contrast=0.6),
        make_material("tyre", "#221F24", contrast=0.5),
        make_material("metal", "#86847C"),
        make_material("rust", "#6A4430"),
        make_material("moss", "#5C7A3A"),
        make_material("lamp", "#D8D0A8", contrast=0.4),
        make_material("tail", "#8E2F2A", contrast=0.6),
        make_material("glint", "#8EA4B0", contrast=0.4),
        make_material("seam", "#3A3236", contrast=0.5),
        make_material("moss_light", "#7E9A48", contrast=0.8),
    ]
    if open_doors:
        materials.append(make_material("interior", "#2B2522", contrast=0.5))

    def parts() -> list[Part]:
        w = Weathering(seed)
        # Carrosserie affaissée d'un côté : un pneu à plat.
        sag = rotation_z(w.uniform(-0.05, 0.05))
        body_c, body_h = (0.0, 0.62 * M, 0.0), (0.88 * M, 0.34 * M, 2.05 * M)
        result = [
            Part(lambda p: rounded_box(p, body_c, body_h, 0.22 * M, sag), PAINT),
            Part(lambda p: rounded_box(p, (0, 1.12 * M, -0.25 * M), (0.78 * M, 0.3 * M, 1.05 * M), 0.28 * M, sag), GLASS),
            Part(lambda p: rounded_box(p, (0, 1.4 * M, -0.32 * M), (0.75 * M, 0.07 * M, 0.82 * M), 0.06 * M, sag), PAINT),
            Part(lambda p: _union(*(rounded_box(p, (0, 0.44 * M, z * 2.08 * M), (0.86 * M, 0.1 * M, 0.07 * M), 0.05 * M)
                                    for z in (-1, 1))), METAL),
            Part(lambda p: _union(*(cylinder(p, (x * 0.8 * M, 0.32 * M, z * 1.3 * M), 0.32 * M, 0.13 * M, 0.05 * M, ROT_WHEEL)
                                    for x in (-1, 1) for z in (-1, 1))), TYRE),
            Part(lambda p: _union(*(sphere(p, (x * 0.55 * M, 0.7 * M, 2.04 * M), 0.11 * M) for x in (-1, 1))), LAMP),
            Part(lambda p: _union(*(sphere(p, (x * 0.6 * M, 0.72 * M, -2.04 * M), 0.1 * M) for x in (-1, 1))), TAIL),
        ]
        body = lambda p: rounded_box(p, body_c, body_h, 0.22 * M, sag)
        roof = lambda p: rounded_box(p, (0, 1.4 * M, -0.32 * M), (0.75 * M, 0.07 * M, 0.82 * M), 0.06 * M, sag)
        cabin = lambda p: rounded_box(p, (0, 1.12 * M, -0.25 * M), (0.78 * M, 0.3 * M, 1.05 * M), 0.28 * M, sag)
        shell = lambda p: np.minimum(body(p), roof(p))
        # Rouille peinte en coulures qui partent du bas de caisse ; mousse posée sur le toit et le capot, pas en boules.
        rust = both(noise_mask(0.22 * M, seed + 1, 0.35), lambda p: p[:, 1] - 0.85 * M)
        moss = both(noise_mask(0.35 * M, seed + 2, 0.28), lambda p: 0.9 * M - p[:, 1])
        doors = both(bands(2, 1.05 * M, 0.035 * M, offset=0.35 * M), lambda p: np.abs(p[:, 1] - 0.65 * M) - 0.25 * M)
        glint = lambda p: np.abs(p[:, 2] * 0.8 + p[:, 1] - 1.0 * M) - 0.08 * M
        result += [
            Part(painted(body, doors, 0.012 * M), SEAM),
            Part(painted(shell, rust, 0.015 * M), RUST),
            Part(painted(shell, moss, 0.02 * M), MOSS, relief=False),
            Part(painted(shell, both(moss, noise_mask(0.15 * M, seed + 3, 0.4)), 0.024 * M), MOSS_LIGHT, relief=False),
            Part(painted(cabin, glint, 0.012 * M), GLINT, relief=False),
            Part(lambda p: _union(*(cylinder(p, (x * 0.94 * M, 0.32 * M, z * 1.3 * M), 0.14 * M, 0.02 * M, 0.01 * M, ROT_WHEEL)
                                    for x in (-1, 1) for z in (-1, 1))), METAL),
        ]
        if open_doors:
            result += _open_doors(open_doors, body, w, PAINT, GLASS, SEAM, INTERIOR)
        return result

    # Portières ouvertes : le cadre s'élargit pour qu'elles tiennent dans le rendu.
    canvas = (130, 104) if open_doors else (110, 96)
    return PropModel(stem, parts, materials, yaw, canvas=canvas, footprint=box_footprint(0.9 * M, 2.12 * M))


# Portières : charnière à l'avant de l'ouverture (z), côté (signe de x).
DOOR_HINGES = {"front": (1.4, 1.0), "rear": (0.35, 1.0), "front_far": (1.4, -1.0), "rear_far": (0.35, -1.0)}
DOOR_LENGTH = 1.02


def _open_doors(doors: tuple[str, ...], body, w: Weathering, paint: int, glass: int, seam: int, interior: int) -> list[Part]:
    """Portières battantes ouvertes vers l'extérieur, habitacle sombre là où elles manquent ; coffre levé."""
    result = []
    for name in doors:
        if name == "trunk":
            hinge = np.array([0.0, 0.98 * M, -1.3 * M])
            lift = rotation_x(-w.uniform(1.0, 1.25))
            lid = hinge + lift @ np.array([0.0, 0.0, -0.42 * M])
            result += [
                Part(painted(body, lambda p: np.maximum(np.abs(p[:, 0]) - 0.72 * M, p[:, 2] + 1.32 * M), 0.02 * M), interior),
                Part(lambda p, c=lid, r=lift: rounded_box(p, c, (0.8 * M, 0.035 * M, 0.42 * M), 0.03 * M, r), paint),
            ]
            continue
        z, side = DOOR_HINGES[name]
        swing = rotation_y(-side * w.uniform(0.85, 1.15))
        hinge = np.array([side * 0.9 * M, 0.0, z * M])
        half = DOOR_LENGTH * 0.5 * M
        panel = hinge + swing @ np.array([side * 0.04 * M, 0.66 * M, -half])
        frame = hinge + swing @ np.array([side * 0.04 * M, 1.12 * M, -half * 0.9])
        z0, z1 = (z - DOOR_LENGTH) * M, z * M
        opening = lambda p, s=side, a=z0, b=z1: np.maximum.reduce([
            -s * p[:, 0], np.abs(p[:, 2] - (a + b) * 0.5) - (b - a) * 0.5 + 0.06 * M, np.abs(p[:, 1] - 0.68 * M) - 0.27 * M])
        result += [
            Part(painted(body, opening, 0.02 * M), interior),
            Part(lambda p, c=panel, r=swing: rounded_box(p, c, (0.035 * M, 0.3 * M, half), 0.03 * M, r), paint),
            # Vitre de portière, dans son cadre fin au-dessus du panneau.
            Part(lambda p, c=frame, r=swing: rounded_box(p, c, (0.03 * M, 0.16 * M, half * 0.85), 0.03 * M, r), glass),
            Part(painted(lambda p, c=panel, r=swing: rounded_box(p, c, (0.035 * M, 0.3 * M, half), 0.03 * M, r),
                         lambda p: np.abs(p[:, 1] - 0.62 * M) - 0.02 * M, 0.01 * M), seam),
        ]
    return result


# ---------------------------------------------------------------------------------------------------------------------
# Bennes
# ---------------------------------------------------------------------------------------------------------------------

def dumpster(stem: str, paint_hex: str, open_lid: bool, seed: int) -> PropModel:
    PAINT, RUST, TYRE, BAG, LID = range(5)
    materials = [
        make_material("paint", paint_hex),
        make_material("rust", "#6A4430"),
        make_material("tyre", "#221F24", contrast=0.5),
        make_material("bag", "#2C2B33", contrast=0.8),
        make_material("lid", "#3B3F3A"),
    ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        body_c, body_h = (0.0, 0.62 * M, 0.0), (0.62 * M, 0.5 * M, 0.95 * M)
        lid_rot = rotation_x(-0.9 if open_lid else -0.08)
        lid_c = (0.0, 1.2 * M, -0.3 * M) if open_lid else (0.0, 1.16 * M, 0.0)
        result = [
            Part(lambda p: rounded_box(p, body_c, body_h, 0.08 * M), PAINT),
            Part(lambda p: rounded_box(p, lid_c, (0.66 * M, 0.05 * M, 0.98 * M), 0.04 * M, lid_rot), LID),
            Part(lambda p: _union(*(cylinder(p, (x * 0.45 * M, 0.1 * M, z * 0.75 * M), 0.1 * M, 0.06 * M, 0.02 * M, ROT_WHEEL)
                                    for x in (-1, 1) for z in (-1, 1))), TYRE),
            Part(_patches(w.surface_points(body_c, body_h, 8), 0.14 * M, 0.24 * M, w), RUST),
        ]
        if open_lid:
            bags = [(w.uniform(-0.4, 0.4) * M, 1.15 * M, w.uniform(-0.6, 0.6) * M) for _ in range(3)]
            bags += [(0.95 * M, 0.25 * M, 0.2 * M), (1.2 * M, 0.2 * M, -0.5 * M)]
            result.append(Part(lambda p: _union(*(ellipsoid(p, c, (0.32 * M, 0.26 * M, 0.3 * M)) for c in bags)), BAG))
        return result

    return PropModel(stem, parts, materials, AXIS_Y_YAW, canvas=(80, 80), footprint=box_footprint(0.66 * M, 0.98 * M))


# ---------------------------------------------------------------------------------------------------------------------
# Mobilier de trottoir
# ---------------------------------------------------------------------------------------------------------------------

def traffic_light(stem: str, seed: int) -> PropModel:
    POLE, HEAD, RED, AMBER, GREEN, MOSS = range(6)
    materials = [
        make_material("pole", "#4A4E4C"),
        make_material("head", "#2E3230"),
        make_material("red", "#9A3A30", contrast=0.5),
        make_material("amber", "#B8862E", contrast=0.5),
        make_material("green", "#3E6A48", contrast=0.5),
        make_material("moss", "#5C7A3A"),
    ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        lean = rotation_z(w.uniform(0.08, 0.16)) @ rotation_x(w.uniform(-0.06, 0.06))
        top = lean @ np.array([0.0, 3.2 * M, 0.0])
        head = top + lean @ np.array([0.0, -0.45 * M, 0.12 * M])
        lamps = [head + lean @ np.array([0.0, dy * M, 0.16 * M]) for dy in (0.28, 0.0, -0.28)]
        return [
            Part(lambda p: capsule(p, (0, 0, 0), top, 0.07 * M), POLE),
            Part(lambda p: cylinder(p, (0, 0.05 * M, 0), 0.2 * M, 0.05 * M, 0.02 * M), POLE),
            Part(lambda p: rounded_box(p, head, (0.17 * M, 0.45 * M, 0.14 * M), 0.05 * M, lean), HEAD),
            Part(lambda p: sphere(p, lamps[0], 0.1 * M), RED),
            Part(lambda p: sphere(p, lamps[1], 0.1 * M), AMBER),
            Part(lambda p: sphere(p, lamps[2], 0.1 * M), GREEN),
            Part(lambda p: _union(*(ellipsoid(p, (x * M, 0.08 * M, z * M), (0.22 * M, 0.12 * M, 0.2 * M))
                                    for x, z in ((0.15, 0.1), (-0.12, -0.08)))), MOSS),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(56, 100), footprint=box_footprint(0.22 * M, 0.22 * M))


def phone_booth(stem: str, seed: int) -> PropModel:
    FRAME, GLASS, ROOF, SIGN, MOSS = range(5)
    materials = [
        make_material("frame", "#5B6770"),
        make_material("glass", "#6F8C94", contrast=0.5),
        make_material("roof", "#3A4248"),
        make_material("sign", "#C7BC94", contrast=0.5),
        make_material("moss", "#5C7A3A"),
    ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        posts = [(x * 0.48 * M, 1.15 * M, z * 0.48 * M) for x in (-1, 1) for z in (-1, 1)]
        return [
            Part(lambda p: _union(*(rounded_box(p, c, (0.05 * M, 1.15 * M, 0.05 * M), 0.02 * M) for c in posts)), FRAME),
            Part(lambda p: rounded_box(p, (0, 1.1 * M, 0), (0.44 * M, 0.9 * M, 0.44 * M), 0.02 * M), GLASS),
            Part(lambda p: rounded_box(p, (0, 0.12 * M, 0), (0.5 * M, 0.12 * M, 0.5 * M), 0.03 * M), FRAME),
            Part(lambda p: rounded_box(p, (0, 2.36 * M, 0), (0.55 * M, 0.1 * M, 0.55 * M), 0.04 * M), ROOF),
            Part(lambda p: rounded_box(p, (0, 2.15 * M, 0), (0.5 * M, 0.1 * M, 0.5 * M), 0.02 * M), SIGN),
            Part(lambda p: ellipsoid(p, (w.uniform(-0.2, 0.2) * M, 2.47 * M, 0.1 * M), (0.35 * M, 0.08 * M, 0.3 * M)), MOSS),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(56, 96), footprint=box_footprint(0.55 * M, 0.55 * M))


def mailbox(stem: str, seed: int) -> PropModel:
    POST, BOX, SLOT = range(3)
    materials = [
        make_material("post", "#4A4E4C"),
        make_material("box", "#C9A23A"),
        make_material("slot", "#2E2A26", contrast=0.5),
    ]

    def parts() -> list[Part]:
        tilt = rotation_z(Weathering(seed).uniform(-0.1, 0.1))
        return [
            Part(lambda p: capsule(p, (0, 0, 0), tilt @ np.array([0, 0.7 * M, 0]), 0.06 * M), POST),
            Part(lambda p: rounded_box(p, tilt @ np.array([0, 1.0 * M, 0]), (0.26 * M, 0.34 * M, 0.2 * M), 0.1 * M, tilt), BOX),
            Part(lambda p: rounded_box(p, tilt @ np.array([0, 1.15 * M, 0.2 * M]), (0.14 * M, 0.025 * M, 0.02 * M), 0.01 * M, tilt), SLOT),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(40, 56))


def bus_shelter(stem: str, seed: int) -> PropModel:
    """Abribus (plan 23 R7) : toit plat, vitre de fond fêlée, banc, panneau de ligne. On y attendait ; on repart."""
    FRAME, GLASS, ROOF, BENCH, SIGN, MOSS, RUST = range(7)
    materials = [
        make_material("frame", "#5B6770"),
        make_material("glass", "#6F8C94", contrast=0.5),
        make_material("roof", "#3A4248"),
        make_material("bench", "#7A5A3C"),
        make_material("sign", "#C7BC94", contrast=0.5),
        make_material("moss", "#5C7A3A"),
        make_material("rust", "#6A4430"),
    ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        half_len = 1.1 * M
        posts = [(x * 0.42 * M, 1.1 * M, z * half_len) for x in (-1, 1) for z in (-1, 1)]
        rust = [(w.uniform(-0.4, 0.4) * M, 2.2 * M, w.uniform(-1.0, 1.0) * M) for _ in range(3)]
        return [
            Part(lambda p: _union(*(rounded_box(p, c, (0.04 * M, 1.1 * M, 0.04 * M), 0.015 * M) for c in posts)), FRAME),
            Part(lambda p: rounded_box(p, (0.42 * M, 1.2 * M, 0), (0.02 * M, 0.8 * M, half_len * 0.95), 0.01 * M), GLASS),
            Part(lambda p: rounded_box(p, (0, 2.25 * M, 0), (0.55 * M, 0.06 * M, half_len + 0.12 * M), 0.03 * M,
                                       rotation_z(w.uniform(-0.04, 0.04))), ROOF),
            Part(lambda p: rounded_box(p, (0.2 * M, 0.5 * M, 0), (0.16 * M, 0.04 * M, half_len * 0.8), 0.02 * M), BENCH),
            Part(lambda p: _union(*(rounded_box(p, (0.2 * M, 0.25 * M, z * half_len * 0.7), (0.03 * M, 0.25 * M, 0.03 * M), 0.01 * M)
                                    for z in (-1, 1))), FRAME),
            Part(lambda p: rounded_box(p, (-0.46 * M, 1.95 * M, half_len + 0.05 * M), (0.03 * M, 0.2 * M, 0.2 * M), 0.02 * M), SIGN),
            Part(_spots(rust, 0.08 * M, 0.14 * M, w), RUST),
            Part(lambda p: ellipsoid(p, (w.uniform(-0.2, 0.2) * M, 2.32 * M, w.uniform(-0.6, 0.6) * M), (0.3 * M, 0.07 * M, 0.45 * M)), MOSS),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(80, 100), footprint=box_footprint(0.45 * M, 1.15 * M))


def chain_link_fence(stem: str, seed: int) -> PropModel:
    POST, WIRE, RUST = range(3)
    materials = [
        make_material("post", "#6E706B"),
        make_material("wire", "#8F928A", contrast=0.6),
        make_material("rust", "#6A4430"),
    ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        length = 1.4 * M
        posts = [(0, 0, z * length) for z in (-1, 0, 1)]
        wires = []
        for level in (0.45, 0.85, 1.25):
            sag = w.uniform(0.0, 0.12) * M
            wires.append(((0, level * M, -length), (0, level * M - sag, 0), (0, level * M, length)))
        # Treillis en diagonales : assez épais pour rester un pixel à l'échelle du jeu.
        diagonals = []
        for i in range(-3, 3):
            z0 = i * 0.46 * M
            diagonals.append(((0, 0.25 * M, z0), (0, 1.3 * M, z0 + 0.46 * M)))
        return [
            Part(lambda p: _union(*(capsule(p, c, (0, 1.45 * M, c[2]), 0.06 * M) for c in posts)), POST),
            Part(lambda p: _union(*(np.minimum(capsule(p, a, b, 0.035 * M), capsule(p, b, c, 0.035 * M)) for a, b, c in wires),
                                  *(capsule(p, a, b, 0.03 * M) for a, b in diagonals)), WIRE),
            Part(_spots([(0, w.uniform(0.3, 1.2) * M, z * length) for z in (-1, 1)], 0.07 * M, 0.1 * M, w), RUST),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(80, 80), footprint=box_footprint(0.1 * M, 1.45 * M))


def torn_billboard(stem: str, seed: int) -> PropModel:
    LEG, BOARD, PAPER_A, PAPER_B, PAPER_C = range(5)
    materials = [
        make_material("leg", "#4A4E4C"),
        make_material("board", "#5A5249"),
        make_material("paper_a", "#C7B58A", contrast=0.5),
        make_material("paper_b", "#8E4E40", contrast=0.5),
        make_material("paper_c", "#4F7A86", contrast=0.5),
    ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        board_c = (0, 2.2 * M, 0)
        strips = []
        for index in range(5):
            z = (-1.1 + index * 0.55) * M
            height = w.uniform(0.5, 1.05) * M
            strips.append(((0.08 * M, 2.2 * M + w.uniform(-0.2, 0.15) * M, z), height, index % 3))
        paper = {k: [s for s in strips if s[2] == k] for k in range(3)}

        def paper_part(k):
            items = paper[k] or [((0.08 * M, 2.2 * M, 0), 0.01, k)]
            return lambda p: _union(*(rounded_box(p, c, (0.03 * M, h * 0.5, 0.28 * M), 0.01 * M) for c, h, _ in items))

        return [
            Part(lambda p: _union(*(capsule(p, (0, 0, z * M), (0, 1.8 * M, z * M), 0.07 * M) for z in (-0.9, 0.9))), LEG),
            Part(lambda p: rounded_box(p, board_c, (0.06 * M, 0.7 * M, 1.4 * M), 0.03 * M), BOARD),
            Part(paper_part(0), PAPER_A),
            Part(paper_part(1), PAPER_B),
            Part(paper_part(2), PAPER_C),
        ]

    # Affiches côté +X : vues de trois-quarts face à la caméra à −62°.
    return PropModel(stem, parts, materials, -AXIS_X_YAW, canvas=(96, 100), footprint=box_footprint(0.12 * M, 1.0 * M))


# ---------------------------------------------------------------------------------------------------------------------
# Gravats
# ---------------------------------------------------------------------------------------------------------------------

def _broken_slab(center, half, rotation, seed: int):
    """Dalle de béton cassée : une boîte dont un bout est rongé par un bruit, bord déchiqueté au lieu d'une coupe nette."""
    center = np.asarray(center, dtype=np.float64)

    def field(p: np.ndarray) -> np.ndarray:
        q = (p - center) @ rotation
        slab = rounded_box(q, (0, 0, 0), half, 0.015 * M)
        edge = (value_noise(q, 0.18 * M, seed) - 0.5) * half[0] * 0.9
        return np.maximum(slab, (q[:, 0] - half[0] * 0.55 - edge) * 0.7)

    return field


def concrete_debris(stem: str, seed: int, kind: int) -> PropModel:
    """
    Gravats de béton, trois variantes : 0 deux pans de dalle cassés appuyés l'un sur l'autre, fers à béton tordus ;
    1 tas de briques et de moellons avec un morceau d'enduit peint ; 2 tronçon de poteau couché, cage d'armature à nu.
    Grain de gravillons peint, poussière claire au sol pour asseoir le tas.
    """
    CONCRETE, DARK, REBAR, AGGREGATE, BRICK, PLASTER, DUST = range(7)
    materials = [
        make_material("concrete", "#9A958A"),
        make_material("dark", "#76716A"),
        make_material("rebar", "#8A4A2A"),
        make_material("aggregate", "#6B665E", contrast=0.6),
        make_material("brick", "#8E5644"),
        make_material("plaster", "#B9AE96"),
        make_material("dust", "#8C877C", contrast=0.5),
    ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        light, dark, bricks, rebar = [], [], [], []
        if kind == 0:
            light.append(_broken_slab((-0.15 * M, 0.32 * M, 0.0), (0.62 * M, 0.08 * M, 0.42 * M), rotation_z(0.45) @ rotation_y(0.2), seed))
            dark.append(_broken_slab((0.35 * M, 0.18 * M, 0.1 * M), (0.5 * M, 0.08 * M, 0.38 * M), rotation_y(2.9) @ rotation_z(-0.25), seed + 1))
            rebar += [((0.3 * M, 0.55 * M, -0.2 * M), (0.55 * M, 0.85 * M, -0.3 * M), (0.75 * M, 0.82 * M, -0.1 * M)),
                      ((0.22 * M, 0.6 * M, 0.15 * M), (0.42 * M, 0.95 * M, 0.2 * M), (0.5 * M, 1.05 * M, 0.42 * M)),
                      ((-0.2 * M, 0.25 * M, 0.35 * M), (-0.35 * M, 0.3 * M, 0.6 * M), (-0.3 * M, 0.15 * M, 0.75 * M))]
        elif kind == 1:
            # Tas de démolition : une butte sombre couverte de briques rouges et de quelques moellons clairs.
            mound = lambda p: ellipsoid(p, (0.0, 0.0, 0.0), (0.6 * M, 0.42 * M, 0.45 * M)) \
                + (value_noise(p, 0.18 * M, seed + 6) - 0.5) * 0.12 * M
            dark.append(mound)
            for index in range(14):
                angle = index * 2.39996
                r = 0.85 * np.sqrt((index + 0.5) / 14)
                x, z = np.cos(angle) * r * 0.62 * M, np.sin(angle) * r * 0.45 * M
                y = 0.42 * M * np.sqrt(max(0.0, 1.0 - r * r)) + 0.03 * M
                rot = rotation_y(w.uniform(0, np.pi)) @ rotation_z(w.uniform(-0.5, 0.5)) @ rotation_x(w.uniform(-0.4, 0.4))
                if index % 4 == 3:
                    light.append(lambda p, c=(x, y, z), rot=rot: rounded_box(p, c, (0.15 * M, 0.08 * M, 0.12 * M), 0.03 * M, rot))
                else:
                    bricks.append(((x, y, z), (0.16 * M, 0.06 * M, 0.08 * M), rot))
            for _ in range(4):
                c = (w.uniform(-0.85, 0.85) * M, 0.04 * M, w.uniform(-0.55, 0.55) * M)
                bricks.append((c, (0.16 * M, 0.05 * M, 0.08 * M), rotation_y(w.uniform(0, np.pi))))
        else:
            axis = rotation_z(np.pi / 2) @ rotation_x(0.0)
            light.append(lambda p: np.maximum(cylinder((p - np.array([-0.1 * M, 0.26 * M, 0.0])) @ rotation_y(0.35) @ axis, (0, 0, 0), 0.24 * M,
                                                       0.65 * M, 0.03 * M),
                                              -sphere(p, (0.55 * M, 0.3 * M, 0.25 * M), 0.32 * M)))
            for k in range(4):
                t = k / 3
                rebar.append(((0.25 * M + np.cos(t * 6.2) * 0.04 * M, 0.26 * M + np.sin(t * 6.2) * 0.16 * M, 0.12 * M),
                              (0.55 * M, 0.35 * M + np.sin(t * 6.2) * 0.2 * M, 0.25 * M + np.cos(t * 6.2) * 0.1 * M),
                              (0.7 * M, 0.25 * M + k * 0.08 * M, 0.45 * M)))
            dark += [lambda p, c=c: rounded_box(p, c, (0.14 * M, 0.09 * M, 0.12 * M), 0.04 * M, rotation_y(0.7))
                     for c in ((0.7 * M, 0.08 * M, -0.2 * M), (-0.7 * M, 0.08 * M, 0.35 * M))]
        concrete = lambda p: _union(*(f(p) for f in light + dark)) if light or dark else np.full(len(p), np.inf)
        result = [
            Part(lambda p: _union(*(f(p) for f in light)) if light else np.full(len(p), np.inf), CONCRETE),
            Part(lambda p: _union(*(f(p) for f in dark)) if dark else np.full(len(p), np.inf), DARK),
            Part(painted(concrete, noise_mask(0.06 * M, seed + 4, 0.2), 0.01 * M), AGGREGATE, relief=False),
            Part(lambda p: np.maximum(ellipsoid(p, (0.0, 0.0, 0.05 * M), (0.95 * M, 0.03 * M, 0.6 * M)),
                                      noise_mask(0.25 * M, seed + 5, 0.7)(p)), DUST),
        ]
        if rebar:
            result.append(Part(lambda p: _union(*(np.minimum(capsule(p, a, b, 0.028 * M), capsule(p, b, c, 0.028 * M)) for a, b, c in rebar)), REBAR))
        if bricks:
            result.append(Part(lambda p: _union(*(rounded_box(p, c, h, 0.015 * M, r) for c, h, r in bricks)), BRICK))
        if kind == 1:
            result.append(Part(painted(lambda p: _union(*(rounded_box(p, c, h, 0.015 * M, r) for c, h, r in bricks)),
                                       noise_mask(0.1 * M, seed + 7, 0.25), 0.01 * M), PLASTER, relief=False))
        return result

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(72, 56))


def overturned_desk(stem: str, seed: int) -> PropModel:
    """Bureau d'école renversé sur le flanc : plateau dressé, pieds de tube, tiroir tombé devant, feuilles éparpillées."""
    WOOD, WOOD_DARK, TUBE, DRAWER, PAPER, PAPER_SHADE = range(6)
    materials = [
        make_material("wood", "#9A7450"), make_material("wood_dark", "#6E4E34", contrast=0.8),
        make_material("tube", "#6E726E"), make_material("drawer", "#86603E"),
        make_material("paper", "#D8D2C0", contrast=0.5), make_material("paper_shade", "#B4AC98", contrast=0.5),
    ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        # Plateau dressé à la verticale, tourné vers la caméra ; caisson dessous, pieds pointés vers l'arrière.
        top = lambda p: rounded_box(p, (0.0, 0.42 * M, 0.0), (0.62 * M, 0.4 * M, 0.035 * M), 0.015 * M, rotation_x(-0.12))
        caisson = lambda p: rounded_box(p, (0.25 * M, 0.25 * M, -0.28 * M), (0.3 * M, 0.2 * M, 0.25 * M), 0.02 * M)
        legs = [((x * 0.55 * M, y * M, -0.05 * M), (x * 0.55 * M, y * M, -0.62 * M)) for x in (-1, 1) for y in (0.12, 0.68)]
        drawer_c = (0.55 * M, 0.08 * M, 0.42 * M)
        drawer = lambda p: np.maximum(rounded_box(p, drawer_c, (0.25 * M, 0.08 * M, 0.2 * M), 0.015 * M, rotation_y(0.5)),
                                      -rounded_box(p, (drawer_c[0], drawer_c[1] + 0.05 * M, drawer_c[2]), (0.21 * M, 0.08 * M, 0.16 * M), 0.0, rotation_y(0.5)))
        sheets = [((w.uniform(-0.75, 0.2) * M, 0.012 * M, w.uniform(0.25, 0.7) * M), w.uniform(0, np.pi), k % 2) for k in range(5)]
        return [
            Part(top, WOOD),
            Part(painted(top, bands(0, 0.3 * M, 0.02 * M), 0.01 * M), WOOD_DARK),
            Part(caisson, WOOD_DARK),
            Part(lambda p: _union(*(capsule(p, a, b, 0.03 * M) for a, b in legs)), TUBE),
            Part(drawer, DRAWER),
            Part(lambda p: _union(*(rounded_box(p, c, (0.11 * M, 0.006 * M, 0.08 * M), 0.002 * M, rotation_y(a)) for c, a, k in sheets if k == 0)), PAPER),
            Part(lambda p: _union(*(rounded_box(p, c, (0.11 * M, 0.006 * M, 0.08 * M), 0.002 * M, rotation_y(a) @ rotation_x(0.2))
                                    for c, a, k in sheets if k == 1)), PAPER_SHADE),
        ]

    return PropModel(stem, parts, materials, float(np.radians(20.0)), canvas=(64, 52), footprint=box_footprint(0.62 * M, 0.35 * M))


def steel_beam(stem: str, diagonal: bool, seed: int) -> PropModel:
    STEEL, RUST, CONCRETE = range(3)
    materials = [
        make_material("steel", "#6B6E6C"),
        make_material("rust", "#8A4B28"),
        make_material("concrete", "#8F8A80"),
    ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        # Inclinée : un bout au sol, l'autre posé sur un bloc de béton.
        rot = rotation_x(-0.25) if diagonal else rotation_y(w.uniform(-0.2, 0.2))
        center = (0, 0.42 * M, 0) if diagonal else (0, 0.2 * M, 0)
        length = 1.5 * M

        def beam(p):
            web = rounded_box(p, center, (0.03 * M, 0.18 * M, length), 0.01 * M, rot)
            top = rounded_box(p, np.asarray(center) + rot @ np.array([0, 0.18 * M, 0]), (0.14 * M, 0.025 * M, length), 0.01 * M, rot)
            bottom = rounded_box(p, np.asarray(center) - rot @ np.array([0, 0.18 * M, 0]), (0.14 * M, 0.025 * M, length), 0.01 * M, rot)
            return _union(web, top, bottom)

        rust = noise_mask(0.2 * M, seed + 1, 0.4, (1.0, 1.0, 3.0), frame=lambda q: (q - np.asarray(center)) @ rot)
        result = [Part(beam, STEEL), Part(painted(beam, rust, 0.012 * M), RUST)]
        if diagonal:
            result.append(Part(lambda p: rounded_box(p, (0, 0.33 * M, 1.3 * M), (0.45 * M, 0.33 * M, 0.35 * M), 0.06 * M,
                                                      rotation_y(0.3)), CONCRETE))
        return result

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(80, 64))


# ---------------------------------------------------------------------------------------------------------------------
# Catalogue : nom de fichier → modèle. Les noms historiques sont conservés (JSON et placeurs y font référence).
# ---------------------------------------------------------------------------------------------------------------------

# ---------------------------------------------------------------------------------------------------------------------
# Places et avenues (plan 58 T3a)
# ---------------------------------------------------------------------------------------------------------------------

LIME_LEAF = "#4E7A34"
LIME_LEAF_TOP = "#7FA24A"
LIME_DRY = "#8A7A3A"
LIME_DRY_TOP = "#B49A4C"
LIME_DRY_SHADE = "#5A4A26"
COLUMN_GREEN = "#3B5246"


def lime_tree(stem: str, seed: int, dry: bool) -> list[PropModel]:
    """
    Tilleul taillé des places et des avenues : fût droit, couronne basse et ronde, à hauteur d'un premier étage.
    Le roussi est un tilleul que la sécheresse a pris : sans la mer, il ne pleut plus.
    """
    # forest importe déjà la voiture d'ici : l'import se fait à l'appel.
    from .forest import CANOPY_DARK, TRUNK, tree

    leaf, top, shade = (LIME_DRY, LIME_DRY_TOP, LIME_DRY_SHADE) if dry else (LIME_LEAF, LIME_LEAF_TOP, CANOPY_DARK)
    return tree(stem, seed, 4.6 * M, 1.55 * M, 0.17 * M, 14, TRUNK, leaf, top, False, (100, 140), shade=shade)


def morris_column(stem: str, seed: int) -> PropModel:
    """
    Colonne d'affichage vert bouteille : socle, fût couvert d'affiches superposées, déchirées par lés, et dôme à
    écailles coiffé d'un fleuron. Les affiches passées laissent voir celles d'avant.
    """
    IRON, PAPER_A, PAPER_B, PAPER_C, PAPER_OLD, RUST = range(6)
    materials = [
        make_material("iron", COLUMN_GREEN),
        make_material("paper_a", "#CBBF98", contrast=0.5),
        make_material("paper_b", "#9A5844", contrast=0.5),
        make_material("paper_c", "#5D7F86", contrast=0.5),
        make_material("paper_old", "#8C8470", contrast=0.5),
        make_material("rust", "#6A4430"),
    ]
    radius = 0.42 * M
    shaft_low, shaft_high = 0.35 * M, 2.45 * M

    def parts() -> list[Part]:
        w = Weathering(seed)
        shaft = lambda p: cylinder(p, (0, (shaft_low + shaft_high) / 2, 0), radius, (shaft_high - shaft_low) / 2, 0.02 * M)
        iron = lambda p: _union(
            cylinder(p, (0, 0.17 * M, 0), radius + 0.08 * M, 0.17 * M, 0.04 * M),
            cylinder(p, (0, shaft_low, 0), radius + 0.04 * M, 0.04 * M, 0.02 * M),
            cylinder(p, (0, shaft_high + 0.06 * M, 0), radius + 0.07 * M, 0.07 * M, 0.03 * M),
            ellipsoid(p, (0, shaft_high + 0.18 * M, 0), (radius + 0.04 * M, 0.42 * M, radius + 0.04 * M)),
            capsule(p, (0, shaft_high + 0.5 * M, 0), (0, shaft_high + 0.82 * M, 0), 0.07 * M, 0.03 * M),
            sphere(p, (0, shaft_high + 0.86 * M, 0), 0.07 * M),
        )
        # Lés d'affiches : bandes verticales par secteur d'angle, de hauteurs inégales, déchirées par le bas.
        sheets = []
        for k in range(9):
            a0 = k * 2 * np.pi / 9 + w.uniform(-0.1, 0.1)
            bottom = shaft_low + w.uniform(0.15, 0.9) * M
            sheets.append((a0, a0 + 2 * np.pi / 9 * w.uniform(0.7, 1.0), bottom, k % 3))

        def sheet_mask(kind: int):
            def mask(p: np.ndarray) -> np.ndarray:
                angle = np.mod(np.arctan2(p[:, 2], p[:, 0]), 2 * np.pi)
                result = np.full(len(p), 1.0)
                for a0, a1, bottom, k in sheets:
                    if k != kind:
                        continue
                    inside = (angle >= np.mod(a0, 2 * np.pi)) & (angle <= np.mod(a0, 2 * np.pi) + (a1 - a0))
                    torn = bottom + (value_noise(p, 0.12 * M, seed + 7 + k) - 0.5) * 0.3 * M
                    result = np.where(inside & (p[:, 1] > torn) & (p[:, 1] < shaft_high - 0.06 * M), -1.0, result)
                return result
            return mask

        old = lambda p: np.maximum(p[:, 1] - shaft_high + 0.08 * M, shaft_low + 0.1 * M - p[:, 1])
        streaks = both(noise_mask(0.14 * M, seed + 3, 0.3, (1.0, 4.0, 1.0)), lambda p: p[:, 1] - shaft_high)
        return [
            Part(iron, IRON),
            Part(shaft, PAPER_OLD),
            Part(painted(shaft, old, 0.01 * M), PAPER_OLD, relief=False),
            Part(painted(shaft, sheet_mask(0), 0.02 * M), PAPER_A, relief=False),
            Part(painted(shaft, sheet_mask(1), 0.02 * M), PAPER_B, relief=False),
            Part(painted(shaft, sheet_mask(2), 0.02 * M), PAPER_C, relief=False),
            Part(painted(iron, streaks, 0.015 * M), RUST, relief=False),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(56, 110), footprint=box_footprint(radius, radius))


def standpipe(stem: str, seed: int) -> PropModel:
    """Borne-fontaine de fonte : fût cannelé, bouton de laiton, bec, et une flaque sèche, grise, à ses pieds."""
    IRON, BRASS, STAIN, RUST = range(4)
    materials = [
        make_material("iron", "#3E4644"),
        make_material("brass", "#A2873E", contrast=0.6),
        make_material("stain", "#6E6A60", contrast=0.4),
        make_material("rust", "#6A4430"),
    ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        tilt = rotation_z(w.uniform(-0.05, 0.05))
        local = lambda p: p @ tilt
        body = lambda p: _union(
            cylinder(local(p), (0, 0.08 * M, 0), 0.24 * M, 0.08 * M, 0.03 * M),
            cylinder(local(p), (0, 0.6 * M, 0), 0.15 * M, 0.5 * M, 0.03 * M),
            ellipsoid(local(p), (0, 1.12 * M, 0), (0.18 * M, 0.12 * M, 0.18 * M)),
            capsule(local(p), (0.1 * M, 0.75 * M, 0), (0.3 * M, 0.68 * M, 0), 0.045 * M, 0.035 * M),
        )
        flutes = painted(body, bands(0, 0.09 * M, 0.025 * M), 0.01 * M)
        return [
            Part(lambda p: ellipsoid(p, (0.35 * M, 0.0, 0.05 * M), (0.42 * M, 0.02 * M, 0.3 * M)), STAIN),
            Part(body, IRON),
            Part(flutes, IRON),
            Part(lambda p: sphere(local(p), (0, 1.26 * M, 0), 0.06 * M), BRASS),
            Part(painted(body, noise_mask(0.1 * M, seed + 2, 0.25), 0.012 * M), RUST, relief=False),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(48, 64), footprint=box_footprint(0.2 * M, 0.2 * M))


# ---------------------------------------------------------------------------------------------------------------------
# Le parvis de Saint-Aubin (plan 58 T3b1)
# ---------------------------------------------------------------------------------------------------------------------


def war_memorial(stem: str, seed: int) -> PropModel:
    """
    Monument aux morts du parvis : socle à deux degrés, dé de pierre où une plaque de bronze aligne des noms qu'on ne
    lit plus, obélisque coiffé d'un pyramidion. Une gerbe fanée est restée appuyée contre le dé, son ruban délavé.
    """
    STONE, TRIM, BRONZE, NAMES, WREATH, RIBBON, GRIME, MOSS = range(8)
    materials = [
        make_material("stone", "#ABA391"), make_material("trim", "#C4BBA4"),
        make_material("bronze", "#4E5A48", contrast=0.6), make_material("names", "#8E9A7A", contrast=0.5),
        make_material("wreath", "#6A5A3A", contrast=0.8), make_material("ribbon", "#8E5A54", contrast=0.6),
        make_material("grime", "#7A7262", contrast=0.9), make_material("moss", "#5A7A38"),
    ]
    die_h = 1.5 * M
    shaft_low, shaft_high = 1.0 * M + die_h, 4.6 * M

    def parts() -> list[Part]:
        w = Weathering(seed)
        steps = lambda p: _union(
            rounded_box(p, (0, 0.15 * M, 0), (1.25 * M, 0.15 * M, 1.25 * M), 0.02 * M),
            rounded_box(p, (0, 0.45 * M, 0), (0.95 * M, 0.15 * M, 0.95 * M), 0.02 * M),
        )
        die = lambda p: rounded_box(p, (0, 0.6 * M + die_h / 2, 0), (0.62 * M, die_h / 2, 0.62 * M), 0.02 * M)
        cornice = lambda p: rounded_box(p, (0, 0.6 * M + die_h + 0.1 * M, 0), (0.72 * M, 0.1 * M, 0.72 * M), 0.02 * M)

        def shaft(p: np.ndarray) -> np.ndarray:
            # Obélisque : section carrée qui s'affine, pyramidion au sommet.
            t = np.clip((p[:, 1] - shaft_low) / (shaft_high - shaft_low), 0.0, 1.0)
            half = 0.42 * M - 0.16 * M * t
            body = np.maximum(np.maximum(np.abs(p[:, 0]), np.abs(p[:, 2])) - half,
                              np.maximum(shaft_low - p[:, 1], p[:, 1] - shaft_high))
            apex = shaft_high + 0.45 * M
            pyramid = (np.maximum(np.abs(p[:, 0]), np.abs(p[:, 2])) * 0.45 * M / (0.26 * M) + p[:, 1] - apex) / 1.9
            return np.minimum(body, np.maximum(pyramid, shaft_high - p[:, 1]))

        plaque = lambda p: rounded_box(p, (0, 0.6 * M + die_h * 0.52, 0.63 * M), (0.46 * M, 0.52 * M, 0.03 * M), 0.01 * M)
        # Noms en lignes : de courts traits plus clairs, quelques-uns manquants, illisibles par construction.
        lines = lambda p: np.maximum(np.abs(np.mod(p[:, 1] - 0.6 * M, 0.16 * M) - 0.08 * M) - 0.03 * M,
                                     np.abs(np.mod(p[:, 0] + 0.5 * M, 0.42 * M) - 0.21 * M) - 0.15 * M)
        tilt = rotation_x(-0.3)
        wreath_c = np.array([0.62 * M, 0.95 * M, 0.98 * M])
        twigs = [(np.array([np.cos(a) * 0.36, np.sin(a) * 0.36, 0.0]) * M, w.uniform(0.09, 0.14) * M)
                 for a in np.linspace(0, 2 * np.pi, 11, endpoint=False)]
        wreath = lambda p: _union(*(sphere((p - wreath_c) @ tilt, c, r) for c, r in twigs))
        ribbon = lambda p: _union(
            capsule((p - wreath_c) @ tilt, (0.05 * M, -0.3 * M, 0.05 * M), (0.12 * M, -0.62 * M, 0.08 * M), 0.06 * M, 0.04 * M),
            capsule((p - wreath_c) @ tilt, (-0.08 * M, -0.3 * M, 0.05 * M), (-0.18 * M, -0.6 * M, 0.08 * M), 0.06 * M, 0.04 * M),
        )
        stone = lambda p: _union(steps(p), die(p), shaft(p))
        streaks = both(noise_mask(0.18 * M, seed + 3, 0.35, (1.0, 5.0, 1.0)), lambda p: p[:, 1] - shaft_high)
        moss = both(noise_mask(0.2 * M, seed + 5, 0.4), lambda p: p[:, 1] - 0.75 * M)
        return [
            Part(stone, STONE),
            Part(cornice, TRIM),
            Part(painted(stone, streaks, 0.012 * M), GRIME, relief=False),
            Part(painted(stone, moss, 0.015 * M), MOSS, relief=False),
            Part(plaque, BRONZE),
            Part(painted(plaque, lines, 0.01 * M), NAMES, relief=False),
            Part(wreath, WREATH),
            Part(ribbon, RIBBON),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(80, 120), footprint=box_footprint(1.25 * M, 1.25 * M),
                     bounds=((-1.4 * M, 0.0, -1.4 * M), (1.4 * M, 5.2 * M, 1.6 * M)))


def bell_rope(stem: str, seed: int) -> PropModel:
    """
    Corde de cloche coupée, lovée sur les pavés au pied du clocher : la corde de chanvre en spirale lâche, la poignée
    de laine rayée (rouge, blanc, bleu) que tenait le sonneur, et le bout tranché net, effiloché. On a empêché Baptiste
    de sonner le tocsin. Plaquée au sol.
    """
    HEMP, RED, WHITE, BLUE, FRAY = range(5)
    materials = [
        make_material("hemp", "#A08A62", contrast=0.8), make_material("red", "#9A4038"),
        make_material("white", "#CFC6B0"), make_material("blue", "#3E5A7E"),
        make_material("fray", "#C2AE80", contrast=0.6),
    ]
    radius = 0.07 * M

    def parts() -> list[Part]:
        w = Weathering(seed)
        points = []
        for k in range(46):
            a = k * 0.36
            r = (0.18 + 0.022 * k) * M * w.uniform(0.92, 1.08)
            points.append(np.array([np.cos(a) * r, radius + (0.05 * M if k % 9 == 0 else 0.0), np.sin(a) * r * 0.9]))
        segments = list(zip(points, points[1:]))
        rope = lambda p: _union(*(capsule(p, a, b, radius) for a, b in segments))
        tail = points[-1]
        cut_dir = np.array([0.55, 0.0, 0.25]) * M
        cut = tail + cut_dir
        rope_end = lambda p: capsule(p, tail, cut, radius)
        fray = lambda p: _union(*(capsule(p, cut, cut + np.array([0.16, 0.0, (k - 1.5) * 0.06]) * M, 0.025 * M)
                                  for k in range(4)))
        # Poignée de laine (la « sallie ») sur le premier tour : bandes rouges, blanches et bleues.
        grip = lambda p: _union(*(capsule(p, a, b, radius * 1.9) for a, b in segments[2:8]))
        stripe = lambda k: lambda p: np.where(np.mod(np.floor((p[:, 0] + p[:, 2]) / (0.12 * M)), 3) == k, grip(p), np.inf)
        return [
            Part(rope, HEMP),
            Part(rope_end, HEMP),
            Part(fray, FRAY),
            Part(stripe(0), RED),
            Part(stripe(1), WHITE),
            Part(stripe(2), BLUE),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(72, 52),
                     bounds=((-1.4 * M, 0.0, -1.4 * M), (1.4 * M, 0.4 * M, 1.4 * M)))


# ---------------------------------------------------------------------------------------------------------------------
# La place de la mairie (plan 58 T3b2)
# ---------------------------------------------------------------------------------------------------------------------


def dry_fountain(stem: str, seed: int) -> PropModel:
    """
    Fontaine de place sèche et fendue (plan 58 T3b2) : marche basse, bassin octogonal en pierre dont la margelle et
    le fond sont entaillés d'une fissure nette, fût central mouluré portant une vasque et une pomme de pin en bronze
    vert-de-gris ; feuilles mortes et mousse au fond du bassin vide.
    """
    STONE, TRIM, CRACK, BRONZE, LEAF, MOSS, GRIME = range(7)
    materials = [
        make_material("stone", "#A69E8C"), make_material("trim", "#C2B9A2"),
        make_material("crack", "#282422", contrast=0.5), make_material("bronze", "#4E685C", contrast=0.7),
        make_material("leaf", "#7A5E3A", contrast=0.8), make_material("moss", "#5A7A38"),
        make_material("grime", "#756E60", contrast=0.8),
    ]
    w = Weathering(seed)
    oct_rot = rotation_y(np.pi / 4.0)

    def octagon(p: np.ndarray, radius: float, y0: float, y1: float) -> np.ndarray:
        cy, hy = (y0 + y1) * 0.5, (y1 - y0) * 0.5
        a = rounded_box(p, (0.0, cy, 0.0), (radius, hy, radius), 0.03 * M)
        q = p - np.array([0.0, cy, 0.0])
        b = rounded_box(q @ oct_rot, (0.0, 0.0, 0.0), (radius, hy, radius), 0.03 * M)
        return np.maximum(a, b)

    leaves = [(np.array([w.uniform(-0.95, 0.95), 0.24, w.uniform(-0.95, 0.95)]) * M,
               w.uniform(0.08, 0.14) * M) for _ in range(10)]

    def crack_cut(p: np.ndarray) -> np.ndarray:
        # Fissure en biais traversant la margelle sud et le fond du bassin.
        s1 = capsule(p, (-0.35 * M, 0.26 * M, -0.1 * M), (0.65 * M, 0.34 * M, 1.15 * M), 0.055 * M)
        s2 = capsule(p, (0.65 * M, 0.34 * M, 1.15 * M), (1.05 * M, 0.42 * M, 1.65 * M), 0.065 * M)
        return np.minimum(s1, s2)

    def parts() -> list[Part]:
        plinth = lambda p: octagon(p, 1.88 * M, 0.0, 0.14 * M)
        basin_outer = lambda p: octagon(p, 1.65 * M, 0.12 * M, 0.54 * M)
        basin_hollow = lambda p: cylinder(p, (0.0, 0.48 * M, 0.0), 1.32 * M, 0.28 * M, 0.02 * M)
        shaft = lambda p: _union(
            cylinder(p, (0.0, 0.85 * M, 0.0), 0.42 * M, 0.62 * M, 0.04 * M),
            ellipsoid(p, (0.0, 0.88 * M, 0.0), (0.52 * M, 0.22 * M, 0.52 * M)),
        )
        upper_outer = lambda p: cylinder(p, (0.0, 1.48 * M, 0.0), 0.85 * M, 0.2 * M, 0.04 * M)
        upper_hollow = lambda p: cylinder(p, (0.0, 1.62 * M, 0.0), 0.68 * M, 0.12 * M)
        stone = lambda p: np.maximum(
            _union(plinth(p), np.maximum(basin_outer(p), -basin_hollow(p)), shaft(p), np.maximum(upper_outer(p), -upper_hollow(p))),
            -crack_cut(p),
        )
        rim_trim = lambda p: np.maximum(
            _union(
                np.maximum(octagon(p, 1.72 * M, 0.50 * M, 0.58 * M), -octagon(p, 1.3 * M, 0.44 * M, 0.64 * M)),
                np.maximum(cylinder(p, (0.0, 1.64 * M, 0.0), 0.9 * M, 0.05 * M), -cylinder(p, (0.0, 1.64 * M, 0.0), 0.66 * M, 0.08 * M)),
            ),
            -crack_cut(p),
        )
        crack_floor = lambda p: np.maximum(crack_cut(p), -stone(p) - 0.04 * M)
        finial = lambda p: _union(
            cylinder(p, (0.0, 1.92 * M, 0.0), 0.22 * M, 0.28 * M, 0.03 * M),
            ellipsoid(p, (0.0, 2.26 * M, 0.0), (0.24 * M, 0.34 * M, 0.24 * M)),
            capsule(p, (0.2 * M, 1.95 * M, 0.0), (0.34 * M, 1.88 * M, 0.0), 0.04 * M),
            capsule(p, (-0.2 * M, 1.95 * M, 0.0), (-0.34 * M, 1.88 * M, 0.0), 0.04 * M),
        )
        dry_leaves = lambda p: _union(*(sphere(p, c, r) for c, r in leaves))
        streaks = both(noise_mask(0.2 * M, seed + 3, 0.38), lambda p: p[:, 1] - 1.7 * M)
        moss = both(noise_mask(0.18 * M, seed + 7, 0.42), lambda p: p[:, 1] - 0.6 * M)
        return [
            Part(stone, STONE),
            Part(rim_trim, TRIM),
            Part(crack_floor, CRACK),
            Part(painted(stone, streaks, 0.014 * M), GRIME, relief=False),
            Part(painted(stone, moss, 0.015 * M), MOSS, relief=False),
            Part(finial, BRONZE),
            Part(dry_leaves, LEAF),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, ray_range=240.0, canvas=(116, 108),
                     footprint=box_footprint(1.6 * M, 1.6 * M),
                     bounds=((-2.1 * M, 0.0, -2.1 * M), (2.1 * M, 2.8 * M, 2.1 * M)),
                     supersample=3, smooth_slopes=True)


def bandstand(stem: str, seed: int) -> PropModel:
    """
    Kiosque à musique octogonal (plan 58 T3b2) : soubassement de pierre et brique, escalier de trois marches au sud,
    garde-corps en fer forgé, huit colonnes de fonte à consoles portant un toit octogonal en zinc à côtes marquées,
    lambrequin et épi de faîtage.
    """
    STONE, TRIM, IRON, CAST, ZINC, ZINC_DARK, FLOOR, MOSS = range(8)
    materials = [
        make_material("stone", "#9E9584"), make_material("trim", "#C2B9A4"),
        make_material("iron", "#2C3034", contrast=0.6), make_material("cast", "#3B4844", contrast=0.7),
        make_material("zinc", "#646E74", contrast=0.7), make_material("zinc_dark", "#485056", contrast=0.7),
        make_material("floor", "#6A5A4A", contrast=0.7), make_material("moss", "#5A7A38"),
    ]
    r_base = 2.25 * M
    h_base = 0.65 * M
    r_cols = 1.98 * M
    h_eave = 3.05 * M
    r_roof = 2.58 * M
    h_roof = 1.45 * M
    oct_rot = rotation_y(np.pi / 4.0)
    angles = [k * np.pi / 4.0 + np.pi / 8.0 for k in range(8)]

    def oct_prism(p: np.ndarray, radius: float, y0: float, y1: float) -> np.ndarray:
        cy, hy = (y0 + y1) * 0.5, (y1 - y0) * 0.5
        a = rounded_box(p, (0.0, cy, 0.0), (radius, hy, radius), 0.02 * M)
        q = p - np.array([0.0, cy, 0.0])
        b = rounded_box(q @ oct_rot, (0.0, 0.0, 0.0), (radius, hy, radius), 0.02 * M)
        return np.maximum(a, b)

    def parts() -> list[Part]:
        socle = lambda p: oct_prism(p, r_base, 0.0, h_base)
        deck = lambda p: oct_prism(p, r_cols + 0.05 * M, h_base - 0.04 * M, h_base + 0.05 * M)
        cornice = lambda p: _union(
            np.maximum(oct_prism(p, r_base + 0.1 * M, h_base - 0.06 * M, h_base + 0.04 * M), -deck(p) + 0.05 * M),
            oct_prism(p, r_cols + 0.25 * M, h_eave - 0.1 * M, h_eave + 0.08 * M),
        )
        stairs = lambda p: _union(*(
            rounded_box(p, (0.0, (k + 0.5) * 0.19 * M, r_base + (2 - k) * 0.34 * M + 0.06 * M),
                        (0.68 * M, (k + 0.5) * 0.19 * M, 0.2 * M), 0.02 * M)
            for k in range(3)
        ))
        cols = lambda p: _union(*(
            capsule(p, (np.cos(a) * r_cols, h_base, np.sin(a) * r_cols),
                    (np.cos(a) * r_cols, h_eave, np.sin(a) * r_cols), 0.075 * M)
            for a in angles
        ))
        # Garde-corps ouvert devant l'escalier sud (arête entre angles 1 et 2, où sin > 0 et |cos| petit).
        verts = [(np.cos(a) * r_cols, np.sin(a) * r_cols) for a in angles]
        open_idx = max(range(8), key=lambda k: verts[k][1] + verts[(k + 1) % 8][1])
        rails = []
        balusters = []
        for k in range(8):
            if k == open_idx:
                continue
            x0, z0 = verts[k]
            x1, z1 = verts[(k + 1) % 8]
            rails.append(((x0, h_base + 0.66 * M, z0), (x1, h_base + 0.66 * M, z1)))
            rails.append(((x0, h_base + 0.16 * M, z0), (x1, h_base + 0.16 * M, z1)))
            for s in (0.25, 0.5, 0.75):
                bx, bz = x0 + (x1 - x0) * s, z0 + (z1 - z0) * s
                balusters.append(((bx, h_base + 0.05 * M, bz), (bx, h_base + 0.66 * M, bz)))

        railing = lambda p: _union(
            *(capsule(p, a, b, 0.045 * M) for a, b in rails),
            *(capsule(p, a, b, 0.03 * M) for a, b in balusters),
        )

        def roof_body(p: np.ndarray) -> np.ndarray:
            local = p - np.array([0.0, h_eave, 0.0])
            # Pyramide octogonale : intersection de deux pyramides carrées tournées de 45°.
            slope = h_roof / r_roof
            norm = float(np.sqrt(1.0 + slope * slope))
            s1 = (np.maximum(np.abs(local[:, 0]), np.abs(local[:, 2])) * slope + local[:, 1] - h_roof) / norm
            q = local @ oct_rot
            s2 = (np.maximum(np.abs(q[:, 0]), np.abs(q[:, 2])) * slope + local[:, 1] - h_roof) / norm
            pyra = np.maximum(np.maximum(s1, s2), -local[:, 1])
            finial = _union(
                capsule(p, (0.0, h_eave + h_roof - 0.1 * M, 0.0), (0.0, h_eave + h_roof + 0.58 * M, 0.0), 0.055 * M),
                sphere(p, (0.0, h_eave + h_roof + 0.15 * M, 0.0), 0.16 * M),
            )
            return np.minimum(pyra, finial)

        def ribs(p: np.ndarray) -> np.ndarray:
            # Côtes de zinc le long des huit arêtes du toit et lambrequin d'égout.
            rim = np.maximum(oct_prism(p, r_roof + 0.04 * M, h_eave - 0.12 * M, h_eave + 0.04 * M),
                             -oct_prism(p, r_roof - 0.08 * M, h_eave - 0.2 * M, h_eave + 0.1 * M))
            top = (0.0, h_eave + h_roof, 0.0)
            seams = _union(*(capsule(p, (np.cos(a) * r_roof, h_eave, np.sin(a) * r_roof), top, 0.05 * M) for a in angles))
            return np.minimum(rim, seams)

        moss = both(noise_mask(0.22 * M, seed + 5, 0.42), lambda p: p[:, 1] - h_base)
        return [
            Part(socle, STONE),
            Part(stairs, TRIM),
            Part(cornice, TRIM),
            Part(deck, FLOOR),
            Part(painted(socle, moss, 0.015 * M), MOSS, relief=False),
            Part(railing, IRON),
            Part(cols, CAST),
            Part(roof_body, ZINC),
            Part(ribs, ZINC_DARK),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, ray_range=300.0, canvas=(156, 152),
                     footprint=box_footprint(2.15 * M, 2.15 * M),
                     bounds=((-2.9 * M, 0.0, -2.9 * M), (2.9 * M, 5.2 * M, 3.2 * M)),
                     supersample=3, smooth_slopes=True)


def obituary_board(stem: str, seed: int) -> PropModel:
    """
    Panneau d'affichage municipal couvert de faire-part d'oubli (plan 58 T3b2) : cadre de bois sombre sur deux poteaux,
    auvent de zinc, et grille de feuillets blancs bordés de noir. Un ou deux feuillets décollés au pied.
    """
    WOOD, ZINC, BORDER, PAPER, GRIME, MOSS = range(6)
    materials = [
        make_material("wood", "#4A3C32", contrast=0.7), make_material("zinc", "#626C72", contrast=0.7),
        make_material("border", "#201E22", contrast=0.4), make_material("paper", "#DAD4C4", contrast=0.4),
        make_material("grime", "#362B24", contrast=0.8), make_material("moss", "#5A7A38"),
    ]
    w = Weathering(seed)
    notices = []
    for row in range(3):
        for col in range(4):
            if w.uniform(0.0, 1.0) < 0.08:
                continue
            cx = (-0.54 + col * 0.36 + w.uniform(-0.025, 0.025)) * M
            cy = (1.66 - row * 0.35 + w.uniform(-0.02, 0.02)) * M
            tilt = w.uniform(-0.09, 0.09)
            notices.append((cx, cy, tilt))

    def parts() -> list[Part]:
        posts = lambda p: _union(
            rounded_box(p, (-0.78 * M, 1.02 * M, 0.0), (0.07 * M, 1.02 * M, 0.07 * M), 0.02 * M),
            rounded_box(p, (0.78 * M, 1.02 * M, 0.0), (0.07 * M, 1.02 * M, 0.07 * M), 0.02 * M),
        )
        board = lambda p: rounded_box(p, (0.0, 1.32 * M, 0.0), (0.85 * M, 0.62 * M, 0.06 * M), 0.02 * M)
        frame = lambda p: np.minimum(posts(p), board(p))
        awning = lambda p: rounded_box(p, (0.0, 2.02 * M, 0.1 * M), (0.94 * M, 0.05 * M, 0.24 * M), 0.02 * M,
                                       rotation_x(0.24))
        flyer_borders = lambda p: _union(
            *(rounded_box(p, (cx, cy, 0.08 * M), (0.14 * M, 0.13 * M, 0.025 * M), 0.005 * M, rotation_z(tilt))
              for cx, cy, tilt in notices),
            rounded_box(p, (-0.28 * M, 0.03 * M, 0.28 * M), (0.14 * M, 0.02 * M, 0.12 * M), 0.005 * M, rotation_y(0.35)),
        )
        flyer_papers = lambda p: _union(
            *(rounded_box(p, (cx, cy, 0.105 * M), (0.105 * M, 0.095 * M, 0.02 * M), 0.004 * M, rotation_z(tilt))
              for cx, cy, tilt in notices),
            rounded_box(p, (-0.28 * M, 0.045 * M, 0.28 * M), (0.105 * M, 0.015 * M, 0.09 * M), 0.004 * M, rotation_y(0.35)),
        )
        grime = both(noise_mask(0.18 * M, seed + 2, 0.35), lambda p: p[:, 1] - 0.6 * M)
        moss = both(noise_mask(0.16 * M, seed + 4, 0.4), lambda p: p[:, 1] - 0.45 * M)
        return [
            Part(frame, WOOD),
            Part(painted(frame, grime, 0.012 * M), GRIME, relief=False),
            Part(painted(frame, moss, 0.014 * M), MOSS, relief=False),
            Part(awning, ZINC),
            Part(flyer_borders, BORDER),
            Part(flyer_papers, PAPER),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, ray_range=220.0, canvas=(84, 92),
                     footprint=box_footprint(0.9 * M, 0.35 * M),
                     bounds=((-1.15 * M, 0.0, -0.45 * M), (1.15 * M, 2.35 * M, 0.55 * M)),
                     supersample=3, smooth_slopes=True)


def catalog() -> list[PropModel]:
    models = [car("prop_urban_car", "red", AXIS_X_YAW, 11)]
    for paint, seed in (("red", 12), ("teal", 21), ("cream", 31)):
        models.append(car(f"prop_urban_car_{paint}_x", paint, AXIS_X_YAW, seed))
        models.append(car(f"prop_urban_car_{paint}_y", paint, AXIS_Y_YAW, seed + 1))
    models += [
        dumpster("prop_dumpster", "#3F5E48", False, 41),
        dumpster("prop_dumpster_v2", "#4B5C6E", True, 42),
        traffic_light("prop_traffic_light", 51),
        phone_booth("prop_phone_booth", 61),
        mailbox("prop_mailbox", 71),
        bus_shelter("prop_bus_shelter", 75),
        chain_link_fence("prop_chain_link_fence", 81),
        torn_billboard("prop_torn_billboard", 91),
        concrete_debris("prop_concrete_debris", 101, 0),
        concrete_debris("prop_concrete_debris_v2", 102, 1),
        concrete_debris("prop_concrete_debris_v3", 103, 2),
        overturned_desk("prop_overturned_desk", 121),
        steel_beam("prop_steel_beam", False, 111),
        steel_beam("prop_steel_beam_diagonal", True, 112),
        morris_column("prop_morris_column", 131),
        standpipe("prop_standpipe", 141),
    ]
    models += lime_tree("prop_lime_tree", 151, False)
    models += lime_tree("prop_lime_tree_dry", 152, True)
    # Banc et réverbère des places : ceux du belvédère et des bois, dessinés ici pour que les pièces de la ville les trouvent.
    from .forest import ivy_lamppost, park_bench

    models += [
        park_bench("prop_park_bench_x", 161, AXIS_X_YAW),
        park_bench("prop_park_bench_y", 162, AXIS_Y_YAW),
        ivy_lamppost("prop_street_lamp", 171),
        war_memorial("prop_war_memorial", 181),
        bell_rope("prop_bell_rope", 191),
        dry_fountain("prop_dry_fountain", 201),
        bandstand("prop_bandstand", 211),
        obituary_board("prop_obituary_board", 221),
    ]
    return models
