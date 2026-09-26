"""
Coffres du monde (plan 17, lot 0A) : quatre silhouettes lisibles d'un coup d'œil, fermées et ouvertes.

Ils sont à l'échelle des personnages et un peu plus grands que nature (≈ 30 px de large) pour qu'on ne les prenne
pas pour du décor : malle de bois, cantine de métal, reliquaire envahi de cristaux, châsse de pierre ancienne.
La couleur de rareté vient de la colonne de lumière du jeu ; le sprite porte la matière, pas la couleur.
"""
from __future__ import annotations

import numpy as np

from ..palette import make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, rounded_box, rotation_x, rotation_z, sphere
from ._kit import AXIS_X_YAW, M, PropModel, Weathering, box_footprint

# Caisse : demi-dimensions (longueur, hauteur, profondeur) en mètres.
BODY = (0.72, 0.33, 0.44)
LID_THICKNESS = 0.1
# Couvercle ouvert : l'avant se relève autour de la charnière arrière (angle négatif, voir sdf.rotation_x).
LID_OPEN_ANGLE = -1.95


def _union(*distances: np.ndarray) -> np.ndarray:
    result = distances[0]
    for d in distances[1:]:
        result = np.minimum(result, d)
    return result


def _hinged(center, open_lid: bool) -> tuple[np.ndarray, np.ndarray | None]:
    """Centre et rotation du couvercle ; un point local passe en monde par rotation @ local."""
    center = np.asarray(center, dtype=np.float64)
    if not open_lid:
        return center, None
    rotation = rotation_x(LID_OPEN_ANGLE)
    hinge = np.array([0.0, 2 * BODY[1] * M, -BODY[2] * M])
    return hinge + rotation @ (center - hinge), rotation


def _hollow(body, open_lid: bool, wall: float = 0.07):
    """Caisse évidée quand le couvercle est levé : l'intérieur sombre se lit comme « déjà ouvert »."""
    if not open_lid:
        return body
    inner = (BODY[0] - wall, BODY[1], BODY[2] - wall)
    return lambda p: np.maximum(body(p), -rounded_box(p, (0, (2 * BODY[1] - wall) * M + inner[1] * M, 0),
                                                      tuple(v * M for v in inner), 0.02 * M))


def _interior(open_lid: bool, material: int, wall: float = 0.07) -> list[Part]:
    if not open_lid:
        return []
    return [Part(lambda p: rounded_box(p, (0, 0.34 * M, 0), ((BODY[0] - wall) * M, 0.03 * M, (BODY[2] - wall) * M),
                                       0.01 * M), material)]


def wooden_chest(stem: str, open_lid: bool, seed: int) -> PropModel:
    WOOD, BAND, BRASS, DARK = range(4)
    materials = [
        make_material("wood", "#8A5A34"),
        make_material("band", "#5E5854"),
        make_material("brass", "#C49B3E", contrast=0.8),
        make_material("inside", "#2A1E1A", contrast=0.4),
    ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        lid_c, lid_r = _hinged((0, (2 * BODY[1] + LID_THICKNESS) * M, 0), open_lid)
        lid_h = (BODY[0] * M + 0.02 * M, LID_THICKNESS * M, BODY[2] * M + 0.02 * M)
        body = _hollow(lambda p: rounded_box(p, (0, BODY[1] * M, 0), tuple(v * M for v in BODY), 0.04 * M), open_lid)
        bands = [(x * 0.42 * M, BODY[1] * M, 0) for x in (-1, 1)]
        tilt = rotation_z(w.uniform(-0.03, 0.03))
        result = [
            Part(body, WOOD),
            Part(lambda p: rounded_box(p, lid_c, lid_h, 0.05 * M, lid_r), WOOD),
            Part(lambda p: _union(*(rounded_box(p, c, (0.07 * M, (BODY[1] + 0.02) * M, (BODY[2] + 0.03) * M), 0.01 * M, tilt)
                                    for c in bands)), BAND),
        ]
        if not open_lid:
            result.append(Part(lambda p: rounded_box(p, (0, 0.5 * M, (BODY[2] + 0.02) * M), (0.09 * M, 0.11 * M, 0.03 * M),
                                                     0.02 * M), BRASS))
        result += _interior(open_lid, DARK)
        return result

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(72, 72), footprint=box_footprint(0.76 * M, 0.48 * M))


def metal_chest(stem: str, open_lid: bool, seed: int) -> PropModel:
    STEEL, EDGE, HANDLE, DARK = range(4)
    materials = [
        make_material("steel", "#6C7C86"),
        make_material("edge", "#343C44"),
        make_material("handle", "#A8A49A", contrast=0.7),
        make_material("inside", "#1E2228", contrast=0.4),
    ]

    def parts() -> list[Part]:
        lid_c, lid_r = _hinged((0, (2 * BODY[1] + LID_THICKNESS) * M, 0), open_lid)
        lid_h = (BODY[0] * M + 0.01 * M, LID_THICKNESS * M, BODY[2] * M + 0.01 * M)
        body = _hollow(lambda p: rounded_box(p, (0, BODY[1] * M, 0), tuple(v * M for v in BODY), 0.03 * M), open_lid)
        corners = [(x * (BODY[0] - 0.04) * M, (0.05 + 0.5 * k) * M, z * (BODY[2] - 0.03) * M)
                   for x in (-1, 1) for z in (-1, 1) for k in (0, 1)]
        ridges = [np.array([0, (LID_THICKNESS + 0.02) * M, z * 0.16 * M]) for z in (-1, 1)]
        result = [
            Part(body, STEEL),
            Part(lambda p: rounded_box(p, lid_c, lid_h, 0.03 * M, lid_r), STEEL),
            Part(lambda p: _union(*(rounded_box(p, c, (0.07 * M, 0.07 * M, 0.07 * M), 0.02 * M) for c in corners)), EDGE),
            Part(lambda p: _union(*(rounded_box(p, lid_c + (r if lid_r is None else lid_r @ r),
                                                (BODY[0] * M, 0.025 * M, 0.035 * M), 0.015 * M, lid_r) for r in ridges)), EDGE),
            Part(lambda p: _union(*(capsule(p, (x * (BODY[0] + 0.04) * M, 0.42 * M, -0.12 * M),
                                            (x * (BODY[0] + 0.04) * M, 0.42 * M, 0.12 * M), 0.035 * M) for x in (-1, 1))), HANDLE),
        ]
        if not open_lid:
            result.append(Part(lambda p: rounded_box(p, (0, 0.5 * M, (BODY[2] + 0.02) * M), (0.12 * M, 0.08 * M, 0.03 * M),
                                                     0.02 * M), HANDLE))
        result += _interior(open_lid, DARK)
        return result

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(72, 72), footprint=box_footprint(0.78 * M, 0.48 * M))


def crystal_chest(stem: str, open_lid: bool, seed: int) -> PropModel:
    IRON, CRYSTAL, CORE, DARK = range(4)
    materials = [
        make_material("iron", "#3E3446"),
        make_material("crystal", "#A77BD6", contrast=1.2),
        make_material("core", "#E6D4FA", contrast=0.4),
        make_material("inside", "#1A1422", contrast=0.4),
    ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        lid_c, lid_r = _hinged((0, (2 * BODY[1] + LID_THICKNESS) * M, 0), open_lid)
        lid_h = (BODY[0] * M + 0.02 * M, LID_THICKNESS * M, BODY[2] * M + 0.02 * M)
        body = _hollow(lambda p: rounded_box(p, (0, BODY[1] * M, 0), tuple(v * M for v in BODY), 0.05 * M), open_lid)
        # Cristaux d'Essence qui ont poussé à travers la caisse : au coin et sur le flanc, jamais sous le couvercle.
        shards = []
        for base, direction, length in (((0.62, 0.5, 0.28), (0.3, 1.0, 0.2), 0.95),
                                        ((0.7, 0.3, -0.12), (0.9, 0.6, 0.1), 0.6),
                                        ((0.45, 0.62, 0.36), (0.1, 1.0, 0.5), 0.62),
                                        ((-0.66, 0.35, 0.3), (-0.7, 0.9, 0.4), 0.7),
                                        ((0.2, 0.12, 0.46), (0.25, 0.45, 1.0), 0.45)):
            d = np.asarray(direction) / np.linalg.norm(direction)
            start = np.asarray(base) * M
            shards.append((start, start + d * length * w.uniform(0.9, 1.1) * M, w.uniform(0.1, 0.13) * M))
        result = [
            Part(body, IRON),
            Part(lambda p: rounded_box(p, lid_c, lid_h, 0.05 * M, lid_r), IRON),
            Part(lambda p: _union(*(capsule(p, a, b, r, r * 0.25) for a, b, r in shards)), CRYSTAL),
            Part(lambda p: _union(*(sphere(p, a + (b - a) * 0.35, r * 0.55) for a, b, r in shards[:2])), CORE),
        ]
        if open_lid:
            result += _interior(open_lid, DARK)
            result.append(Part(lambda p: _union(*(capsule(p, (x * 0.25 * M, 0.34 * M, 0), (x * 0.3 * M, 0.62 * M, 0.05 * M),
                                                          0.07 * M, 0.02 * M) for x in (-1, 0.4))), CRYSTAL))
        return result

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(80, 88), footprint=box_footprint(0.78 * M, 0.5 * M))


def ancient_chest(stem: str, open_lid: bool, seed: int) -> PropModel:
    STONE, GOLD, MOSS, DARK = range(4)
    materials = [
        make_material("stone", "#A69C88"),
        make_material("gold", "#D4A843", contrast=0.8),
        make_material("moss", "#5C7A3A"),
        make_material("inside", "#241E1C", contrast=0.4),
    ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        top = 2 * BODY[1]
        # Couvercle en voûte : moitié haute d'un ellipsoïde posée sur la caisse.
        dome_c, dome_r = _hinged((0, top * M, 0), open_lid)

        def dome(p):
            local = p - dome_c
            if dome_r is not None:
                local = local @ dome_r
            shell = ellipsoid(local, (0, 0, 0), ((BODY[0] + 0.03) * M, 0.26 * M, (BODY[2] + 0.03) * M))
            return np.maximum(shell, -local[:, 1])

        def dome_band(p):
            local = p - dome_c
            if dome_r is not None:
                local = local @ dome_r
            return np.maximum(dome(p) - 0.015 * M, np.abs(local[:, 0]) - 0.06 * M)

        body = _hollow(lambda p: rounded_box(p, (0, BODY[1] * M, 0), tuple(v * M for v in BODY), 0.03 * M), open_lid)
        moss = w.points_on_box((0, 0.1 * M, 0), ((BODY[0] + 0.06) * M, 0.0, (BODY[2] + 0.08) * M), 3, top_only=True)
        result = [
            Part(lambda p: rounded_box(p, (0, 0.05 * M, 0), ((BODY[0] + 0.08) * M, 0.05 * M, (BODY[2] + 0.08) * M), 0.02 * M), STONE),
            Part(body, STONE),
            Part(dome, STONE),
            Part(dome_band, GOLD),
            Part(lambda p: rounded_box(p, (0, (top - 0.05) * M, 0), ((BODY[0] + 0.015) * M, 0.035 * M, (BODY[2] + 0.015) * M),
                                       0.01 * M), GOLD),
            Part(lambda p: _union(*(ellipsoid(p, c, (0.16 * M, 0.05 * M, 0.12 * M)) for c in moss)), MOSS),
        ]
        result += _interior(open_lid, DARK)
        return result

    return PropModel(stem, parts, materials, AXIS_X_YAW, canvas=(72, 72), footprint=box_footprint(0.82 * M, 0.54 * M))


def catalog() -> list[PropModel]:
    models = []
    for rarity, build, seed in (("common", wooden_chest, 11), ("rare", metal_chest, 23),
                                ("epic", crystal_chest, 37), ("lore", ancient_chest, 41)):
        for open_lid in (False, True):
            models.append(build(f"chest_{rarity}_{'open' if open_lid else 'closed'}", open_lid, seed))
    return models
