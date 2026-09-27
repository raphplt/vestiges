"""
Coffres du monde (plan 17, lot 0A) : quatre silhouettes lisibles d'un coup d'œil, fermées et ouvertes.

Ils sont à l'échelle des personnages et un peu plus grands que nature (≈ 30 px de large) pour qu'on ne les prenne
pas pour du décor : malle de bois, cantine de métal, reliquaire envahi de cristaux, châsse de pierre ancienne.
La couleur de rareté vient de la colonne de lumière du jeu ; le sprite porte la matière, pas la couleur.
"""
from __future__ import annotations

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, cylinder, ellipsoid, rounded_box, rotation_x, rotation_z, sphere
from ._kit import AXIS_Y_YAW, M, PropModel, Weathering, box_footprint

# Caisse : demi-dimensions (longueur, hauteur, profondeur) en mètres.
BODY = (0.72, 0.33, 0.44)
LID_THICKNESS = 0.1
# Couvercle ouvert : l'avant se relève autour de la charnière arrière (angle négatif, voir sdf.rotation_x).
LID_OPEN_ANGLE = -1.95
# Face avant (serrure, frise, médaillon) tournée vers la caméra, le flanc en léger trois-quarts.
CHEST_YAW = AXIS_Y_YAW


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


def _local_frame(center, rotation):
    """Passage monde → repère local d'une pièce mobile (couvercle) : les détails suivent son ouverture."""
    center = np.asarray(center, dtype=np.float64)

    def to_local(p):
        local = p - center
        return local @ rotation if rotation is not None else local
    return to_local


def _shell_grooves(half, rounding, slabs):
    """
    Rainures peu profondes à la surface d'une boîte (repère local centré) : chaque dalle (axe, position,
    demi-épaisseur) n'entaille que la peau, sur 0,03 m. Rend planches, frises et joints visibles à 30 px.
    """
    inner = tuple(max(h - 0.03 * M, 0.01) for h in half)

    def grooves(local):
        slab = np.full(len(local), np.inf)
        for axis, position, half_width in slabs:
            slab = np.minimum(slab, np.abs(local[:, axis] - position) - half_width)
        return np.maximum(slab, -rounded_box(local, (0, 0, 0), inner, rounding))
    return grooves


def _carved_box(center, half, rounding, slabs, rotation=None):
    to_local = _local_frame(center, rotation)
    grooves = _shell_grooves(half, rounding, slabs)
    return lambda p: np.maximum(rounded_box(to_local(p), (0, 0, 0), half, rounding), -grooves(to_local(p)))


def _seams(y_levels, half_x, half_z, thickness=0.018):
    """Filets sombres qui ceinturent la caisse (joints de planches, frise) : lisibles à 30 px, où une entaille ne l'est pas."""
    return lambda p: _union(*(rounded_box(p, (0, y * M, 0), ((half_x + 0.006) * M, thickness * M, (half_z + 0.006) * M), 0.004 * M)
                              for y in y_levels))


def _studs(points, radius):
    return lambda p: _union(*(sphere(p, c, radius) for c in points))


def wooden_chest(stem: str, open_lid: bool, seed: int) -> PropModel:
    """Malle de voyage : planches jointives, cerclages cloutés, coins ferrés, serrure de laiton."""
    WOOD, BAND, BRASS, DARK, STUD, SEAM = range(6)
    materials = [
        make_material("wood", "#8A5A34"),
        make_material("band", "#4A4644"),
        make_material("brass", "#C49B3E", contrast=0.8),
        make_material("inside", "#2A1E1A", contrast=0.4),
        make_material("stud", "#A8A29A", contrast=0.6),
        make_material("seam", "#4E2E1C", contrast=0.5),
    ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        lid_c, lid_r = _hinged((0, (2 * BODY[1] + LID_THICKNESS) * M, 0), open_lid)
        lid_half = (BODY[0] * M + 0.02 * M, LID_THICKNESS * M, BODY[2] * M + 0.02 * M)
        lid_local = _local_frame(lid_c, lid_r)
        body_half = tuple(v * M for v in BODY)
        # Trois planches par face, joints horizontaux.
        planks = [(1, (y - BODY[1]) * M, 0.012 * M) for y in (0.22, 0.44)]
        body = _hollow(_carved_box((0, BODY[1] * M, 0), body_half, 0.04 * M, planks), open_lid)
        lid = _carved_box(lid_c, lid_half, 0.05 * M, [(2, z * M, 0.012 * M) for z in (-0.15, 0.15)], lid_r)
        band_x = (-0.42, 0.42)
        tilt = rotation_z(w.uniform(-0.02, 0.02))
        front = (BODY[2] + 0.035) * M
        rivets = [(x * M + dx * 0.035 * M, y * M, front) for x in band_x for y in (0.12, 0.33, 0.54) for dx in (0,)]
        corners = [(x * (BODY[0] - 0.02) * M, 0.07 * M, z * (BODY[2] - 0.02) * M) for x in (-1, 1) for z in (-1, 1)]
        result = [
            Part(body, WOOD),
            Part(lid, WOOD),
            Part(lambda p: _union(*(rounded_box(p, (x * M, BODY[1] * M, 0), (0.06 * M, (BODY[1] + 0.02) * M, (BODY[2] + 0.03) * M),
                                                0.01 * M, tilt) for x in band_x)), BAND),
            Part(lambda p: _union(*(rounded_box(lid_local(p), (x * M, 0.02 * M, 0), (0.06 * M, LID_THICKNESS * M + 0.03 * M,
                                                                                     BODY[2] * M + 0.05 * M), 0.01 * M)
                                    for x in band_x)), BAND),
            Part(lambda p: _union(*(rounded_box(p, c, (0.07 * M, 0.07 * M, 0.07 * M), 0.015 * M) for c in corners)), BAND),
            Part(_studs(rivets, 0.028 * M), STUD),
            Part(_seams((0.22, 0.44), BODY[0], BODY[2]), SEAM),
        ]
        if not open_lid:
            lock = (0, 0.54 * M, (BODY[2] + 0.03) * M)
            result.append(Part(lambda p: rounded_box(p, lock, (0.09 * M, 0.1 * M, 0.03 * M), 0.02 * M), BRASS))
            result.append(Part(lambda p: capsule(p, (0, 0.5 * M, (BODY[2] + 0.065) * M), (0, 0.56 * M, (BODY[2] + 0.065) * M),
                                                 0.018 * M), DARK))
        result += _interior(open_lid, DARK)
        return result

    return PropModel(stem, parts, materials, CHEST_YAW, canvas=(72, 72), footprint=box_footprint(0.76 * M, 0.48 * M))


def metal_chest(stem: str, open_lid: bool, seed: int) -> PropModel:
    """Cantine militaire : nervures embouties, coins rivetés, deux fermoirs, étiquette au pochoir."""
    STEEL, EDGE, HANDLE, DARK, LABEL = range(5)
    materials = [
        make_material("steel", "#62757E"),
        make_material("edge", "#2F363C"),
        make_material("handle", "#B0AC9E", contrast=0.7),
        make_material("inside", "#1E2228", contrast=0.4),
        make_material("label", "#D8D2BE", contrast=0.5),
    ]

    def parts() -> list[Part]:
        lid_c, lid_r = _hinged((0, (2 * BODY[1] + LID_THICKNESS) * M, 0), open_lid)
        lid_half = (BODY[0] * M + 0.01 * M, LID_THICKNESS * M, BODY[2] * M + 0.01 * M)
        lid_local = _local_frame(lid_c, lid_r)
        body = _hollow(lambda p: rounded_box(p, (0, BODY[1] * M, 0), tuple(v * M for v in BODY), 0.03 * M), open_lid)
        corners = [(x * (BODY[0] - 0.04) * M, (0.05 + 0.52 * k) * M, z * (BODY[2] - 0.03) * M)
                   for x in (-1, 1) for z in (-1, 1) for k in (0, 1)]
        front = (BODY[2] + 0.03) * M
        rivets = [(x * (BODY[0] - 0.1) * M, y * M, front) for x in (-1, 1) for y in (0.1, 0.56)]
        rivets += [(x * M, 0.1 * M, front) for x in (-0.3, 0.0, 0.3)]
        result = [
            Part(body, STEEL),
            Part(lambda p: rounded_box(p, lid_c, lid_half, 0.03 * M, lid_r), STEEL),
            # Deux nervures qui ceinturent la caisse, une sur le couvercle.
            Part(lambda p: _union(*(rounded_box(p, (0, y * M, 0), ((BODY[0] + 0.012) * M, 0.022 * M, (BODY[2] + 0.012) * M), 0.01 * M)
                                    for y in (0.22, 0.44))), EDGE),
            Part(lambda p: rounded_box(lid_local(p), (0, 0.02 * M, 0), (BODY[0] * M, LID_THICKNESS * M + 0.02 * M, 0.04 * M),
                                       0.015 * M), EDGE),
            Part(lambda p: _union(*(rounded_box(p, c, (0.075 * M, 0.075 * M, 0.075 * M), 0.02 * M) for c in corners)), EDGE),
            Part(_studs(rivets, 0.024 * M), HANDLE),
            Part(lambda p: _union(*(capsule(p, (x * (BODY[0] + 0.04) * M, 0.42 * M, -0.12 * M),
                                            (x * (BODY[0] + 0.04) * M, 0.42 * M, 0.12 * M), 0.035 * M) for x in (-1, 1))), HANDLE),
            # Étiquette au pochoir, entre les nervures.
            Part(lambda p: rounded_box(p, (-0.3 * M, 0.33 * M, (BODY[2] + 0.005) * M), (0.16 * M, 0.07 * M, 0.012 * M), 0.01 * M), LABEL),
        ]
        if not open_lid:
            latches = [(x * M, 0.58 * M, (BODY[2] + 0.03) * M) for x in (-0.46, 0.46)]
            result.append(Part(lambda p: _union(*(rounded_box(p, c, (0.05 * M, 0.08 * M, 0.03 * M), 0.012 * M) for c in latches)), HANDLE))
        result += _interior(open_lid, DARK)
        return result

    return PropModel(stem, parts, materials, CHEST_YAW, canvas=(72, 72), footprint=box_footprint(0.78 * M, 0.48 * M))


def crystal_chest(stem: str, open_lid: bool, seed: int) -> PropModel:
    """Caisse de fer envahie par l'Essence : cristaux sur le coin et le flanc, veines lumineuses gravées sur la face."""
    IRON, CRYSTAL, VEIN, DARK, STUD = range(5)
    materials = [
        make_material("iron", "#3E3446"),
        make_material("crystal", "#B58AE4", contrast=1.2),
        make_emissive("vein", "#D8B8FF"),
        make_material("inside", "#1A1422", contrast=0.4),
        make_material("stud", "#6E6478", contrast=0.6),
    ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        lid_c, lid_r = _hinged((0, (2 * BODY[1] + LID_THICKNESS) * M, 0), open_lid)
        lid_half = (BODY[0] * M + 0.02 * M, LID_THICKNESS * M, BODY[2] * M + 0.02 * M)
        lid_local = _local_frame(lid_c, lid_r)
        body = _hollow(lambda p: rounded_box(p, (0, BODY[1] * M, 0), tuple(v * M for v in BODY), 0.05 * M), open_lid)
        shards = []
        for base, direction, length in (((0.62, 0.5, 0.28), (0.3, 1.0, 0.2), 1.0),
                                        ((0.7, 0.3, -0.12), (0.9, 0.6, 0.1), 0.65),
                                        ((0.45, 0.62, 0.36), (0.1, 1.0, 0.5), 0.7),
                                        ((0.74, 0.62, 0.05), (0.6, 1.0, -0.2), 0.55),
                                        ((-0.66, 0.35, 0.3), (-0.7, 0.9, 0.4), 0.75),
                                        ((-0.5, 0.62, 0.2), (-0.35, 1.0, 0.3), 0.6),
                                        ((-0.72, 0.5, -0.1), (-0.8, 0.8, 0.0), 0.5),
                                        ((0.2, 0.12, 0.46), (0.25, 0.45, 1.0), 0.45)):
            d = np.asarray(direction) / np.linalg.norm(direction)
            start = np.asarray(base) * M
            shards.append((start, start + d * length * w.uniform(0.9, 1.1) * M, w.uniform(0.11, 0.14) * M))
        # Veines gravées sur la face avant : une ligne brisée qui part du pied des cristaux.
        front = (BODY[2] + 0.012) * M
        vein_points = [(0.55, 0.14), (0.3, 0.3), (0.05, 0.2), (-0.2, 0.42), (-0.45, 0.26), (-0.62, 0.5)]
        veins = [((a[0] * M, a[1] * M, front), (b[0] * M, b[1] * M, front)) for a, b in zip(vein_points, vein_points[1:])]
        studs = [(x * (BODY[0] - 0.05) * M, y * M, (BODY[2] + 0.02) * M) for x in (-1, 1) for y in (0.08, 0.58)]
        result = [
            Part(body, IRON),
            Part(lambda p: rounded_box(p, lid_c, lid_half, 0.05 * M, lid_r), IRON),
            Part(lambda p: _union(*(rounded_box(lid_local(p), (x * M, 0.02 * M, 0), (0.05 * M, LID_THICKNESS * M + 0.025 * M,
                                                                                     BODY[2] * M + 0.04 * M), 0.01 * M)
                                    for x in (-0.4, 0.2))), STUD),
            Part(lambda p: _union(*(capsule(p, a, b, r, r * 0.25) for a, b, r in shards)), CRYSTAL),
            Part(lambda p: _union(*(capsule(p, a, b, 0.022 * M) for a, b in veins)), VEIN),
            Part(_studs(studs, 0.03 * M), STUD),
        ]
        if open_lid:
            result += _interior(open_lid, DARK)
            result.append(Part(lambda p: _union(*(capsule(p, (x * 0.25 * M, 0.34 * M, 0), (x * 0.3 * M, 0.66 * M, 0.05 * M),
                                                          0.08 * M, 0.02 * M) for x in (-1, 0.4))), CRYSTAL))
        return result

    return PropModel(stem, parts, materials, CHEST_YAW, canvas=(80, 88), footprint=box_footprint(0.78 * M, 0.5 * M))


def ancient_chest(stem: str, open_lid: bool, seed: int) -> PropModel:
    """Châsse de pierre : frise gravée, médaillon d'or, colonnettes aux angles, voûte à côtes dorées, mousse."""
    STONE, GOLD, MOSS, DARK, SEAM = range(5)
    materials = [
        make_material("stone", "#A69C88"),
        make_material("gold", "#D4A843", contrast=0.8),
        make_material("moss", "#5C7A3A"),
        make_material("inside", "#241E1C", contrast=0.4),
        make_material("seam", "#6B6356", contrast=0.5),
    ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        top = 2 * BODY[1]
        dome_c, dome_r = _hinged((0, top * M, 0), open_lid)
        dome_local = _local_frame(dome_c, dome_r)

        def dome(p):
            local = dome_local(p)
            shell = ellipsoid(local, (0, 0, 0), ((BODY[0] + 0.03) * M, 0.26 * M, (BODY[2] + 0.03) * M))
            return np.maximum(shell, -local[:, 1])

        def dome_ribs(p):
            local = dome_local(p)
            ribs = _union(*(np.abs(local[:, 0] - x * M) - 0.035 * M for x in (-0.45, 0.0, 0.45)))
            return np.maximum(dome(p) - 0.015 * M, ribs)

        body_half = tuple(v * M for v in BODY)
        # Frise : deux joints horizontaux, et des encoches verticales alternées entre eux.
        frieze = [(1, (y - BODY[1]) * M, 0.012 * M) for y in (0.16, 0.5)]
        frieze += [(0, x * M, 0.012 * M) for x in (-0.6, -0.36, 0.36, 0.6)]
        body = _hollow(_carved_box((0, BODY[1] * M, 0), body_half, 0.03 * M, frieze), open_lid)
        moss = w.points_on_box((0, 0.1 * M, 0), ((BODY[0] + 0.06) * M, 0.0, (BODY[2] + 0.08) * M), 3, top_only=True)
        pillars = [(x * (BODY[0] + 0.01) * M, z * (BODY[2] + 0.01) * M) for x in (-1, 1) for z in (1,)]
        medallion = rotation_x(np.pi / 2)
        result = [
            Part(lambda p: rounded_box(p, (0, 0.05 * M, 0), ((BODY[0] + 0.08) * M, 0.05 * M, (BODY[2] + 0.08) * M), 0.02 * M), STONE),
            Part(body, STONE),
            Part(dome, STONE),
            Part(dome_ribs, GOLD),
            Part(lambda p: rounded_box(p, (0, (top - 0.05) * M, 0), ((BODY[0] + 0.015) * M, 0.035 * M, (BODY[2] + 0.015) * M),
                                       0.01 * M), GOLD),
            Part(lambda p: _union(*(capsule(p, (x, 0.1 * M, z), (x, (top - 0.08) * M, z), 0.03 * M) for x, z in pillars)), GOLD),
            Part(lambda p: cylinder(p, (0, 0.33 * M, (BODY[2] + 0.01) * M), 0.14 * M, 0.02 * M, 0.008 * M, medallion), GOLD),
            Part(lambda p: cylinder(p, (0, 0.33 * M, (BODY[2] + 0.03) * M), 0.06 * M, 0.01 * M, 0.004 * M, medallion), DARK),
            Part(_seams((0.16, 0.5), BODY[0], BODY[2]), SEAM),
            Part(lambda p: _union(*(ellipsoid(p, c, (0.16 * M, 0.05 * M, 0.12 * M)) for c in moss)), MOSS),
        ]
        result += _interior(open_lid, DARK)
        return result

    return PropModel(stem, parts, materials, CHEST_YAW, canvas=(72, 72), footprint=box_footprint(0.82 * M, 0.54 * M))


def catalog() -> list[PropModel]:
    models = []
    for rarity, build, seed in (("common", wooden_chest, 11), ("rare", metal_chest, 23),
                                ("epic", crystal_chest, 37), ("lore", ancient_chest, 41)):
        for open_lid in (False, True):
            models.append(build(f"chest_{rarity}_{'open' if open_lid else 'closed'}", open_lid, seed))
    return models
