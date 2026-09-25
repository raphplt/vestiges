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


def car(stem: str, paint: str, yaw: float, seed: int) -> PropModel:
    PAINT, GLASS, TYRE, METAL, RUST, MOSS, LAMP, TAIL = range(8)
    materials = [
        make_material("paint", CAR_PAINTS[paint]),
        make_material("glass", "#2A3644", contrast=0.6),
        make_material("tyre", "#221F24", contrast=0.5),
        make_material("metal", "#86847C"),
        make_material("rust", "#6A4430"),
        make_material("moss", "#5C7A3A"),
        make_material("lamp", "#D8D0A8", contrast=0.4),
        make_material("tail", "#8E2F2A", contrast=0.6),
    ]

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
        result.append(Part(_patches(w.surface_points(body_c, body_h, 10), 0.18 * M, 0.3 * M, w), RUST))
        moss = w.points_on_box((0, 1.47 * M, -0.32 * M), (0.55 * M, 0.0, 0.6 * M), 1, top_only=True)
        moss += w.points_on_box((0, 0.96 * M, 1.45 * M), (0.55 * M, 0.0, 0.4 * M), 1, top_only=True)
        result.append(Part(lambda p: _union(*(ellipsoid(p, c, (0.24 * M, 0.06 * M, 0.22 * M)) for c in moss)), MOSS))
        return result

    return PropModel(stem, parts, materials, yaw, canvas=(110, 96), footprint=box_footprint(0.9 * M, 2.12 * M))


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

def concrete_debris(stem: str, seed: int) -> PropModel:
    CONCRETE, DARK, REBAR = range(3)
    materials = [
        make_material("concrete", "#9A958A"),
        make_material("dark", "#6B665E"),
        make_material("rebar", "#7C4526"),
    ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        chunks = []
        for index in range(4):
            center = (w.uniform(-0.5, 0.5) * M, w.uniform(0.1, 0.25) * M, w.uniform(-0.5, 0.5) * M)
            half = (w.uniform(0.18, 0.4) * M, w.uniform(0.1, 0.22) * M, w.uniform(0.16, 0.36) * M)
            rot = rotation_y(w.uniform(0, np.pi)) @ rotation_z(w.uniform(-0.4, 0.4))
            chunks.append((center, half, rot, index % 2))
        bars = [((w.uniform(-0.3, 0.3) * M, 0.2 * M, w.uniform(-0.3, 0.3) * M),
                 (w.uniform(-0.6, 0.6) * M, w.uniform(0.4, 0.7) * M, w.uniform(-0.6, 0.6) * M)) for _ in range(2)]
        return [
            Part(lambda p: _union(*(rounded_box(p, c, h, 0.05 * M, r) for c, h, r, k in chunks if k == 0)), CONCRETE),
            Part(lambda p: _union(*(rounded_box(p, c, h, 0.05 * M, r) for c, h, r, k in chunks if k == 1)), DARK),
            Part(lambda p: _union(*(capsule(p, a, b, 0.03 * M) for a, b in bars)), REBAR),
        ]

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(64, 48))


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

        spots = [np.asarray(center) + rot @ np.array([0, w.uniform(-0.15, 0.2) * M, w.uniform(-1.3, 1.3) * M]) for _ in range(5)]
        result = [Part(beam, STEEL), Part(_spots(spots, 0.07 * M, 0.13 * M, w), RUST)]
        if diagonal:
            result.append(Part(lambda p: rounded_box(p, (0, 0.33 * M, 1.3 * M), (0.45 * M, 0.33 * M, 0.35 * M), 0.06 * M,
                                                      rotation_y(0.3)), CONCRETE))
        return result

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(80, 64))


# ---------------------------------------------------------------------------------------------------------------------
# Catalogue : nom de fichier → modèle. Les noms historiques sont conservés (JSON et placeurs y font référence).
# ---------------------------------------------------------------------------------------------------------------------

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
        chain_link_fence("prop_chain_link_fence", 81),
        torn_billboard("prop_torn_billboard", 91),
        concrete_debris("prop_concrete_debris", 101),
        concrete_debris("prop_concrete_debris_v2", 102),
        concrete_debris("prop_concrete_debris_v3", 103),
        steel_beam("prop_steel_beam", False, 111),
        steel_beam("prop_steel_beam_diagonal", True, 112),
    ]
    return models
