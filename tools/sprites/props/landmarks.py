"""
Lieux du plan 17, vague 3 : le Mémorial (stèle de mémoire, dormante puis éveillée), ses éclats, et la Faille
(déchirure de l'Effacement dans le sol, ouverte puis refermée).

Même échelle que les décors (1 m ≈ 27,5 unités). La lumière de mémoire est dorée, celle de l'oubli violette :
les deux lieux se lisent comme un miroir.
"""
from __future__ import annotations

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, cylinder, ellipsoid, rounded_box, rotation_y, rotation_z, sphere
from ._kit import AXIS_Y_YAW, M, PropModel, Weathering, box_footprint


# Un lieu à trouver se voit de loin : la stèle est plus grande que nature, comme les coffres.
MEMORIAL_SCALE = 1.35
SHARD_SCALE = 1.6


def _scaled(distance, factor: float):
    """Agrandit un modèle SDF d'un facteur uniforme (distance exacte, rapportée à l'échelle)."""
    return lambda p: distance(p / factor) * factor


def _union(*distances: np.ndarray) -> np.ndarray:
    result = distances[0]
    for d in distances[1:]:
        result = np.minimum(result, d)
    return result


def memorial(stem: str, awake: bool, seed: int) -> PropModel:
    """Stèle de pierre sur deux marches, inscription gravée ; éveillée, l'inscription et les bougies s'allument."""
    STONE, DARK_STONE, GLYPH, WAX, MOSS = range(5)
    materials = [
        make_material("stone", "#8E8778"),
        make_material("step", "#6B6558"),
        make_emissive("glyph", "#F0C85C") if awake else make_material("glyph", "#5A5448", contrast=0.5),
        make_emissive("flame", "#F6D98A") if awake else make_material("wax", "#D8CFB8", contrast=0.5),
        make_material("moss", "#5C7A3A"),
    ]

    def parts() -> list[Part]:
        return [Part(_scaled(part.distance, MEMORIAL_SCALE), part.material) for part in base_parts()]

    def base_parts() -> list[Part]:
        w = Weathering(seed)
        top = rotation_z(w.uniform(-0.03, 0.03))
        # Inscription : cinq lignes gravées sur la face avant, de longueurs inégales.
        glyphs = [((-0.18 + 0.05 * (i % 2)) * M, (1.05 - 0.14 * i) * M, (0.2 * M, 0.26 * M)[i % 2]) for i in range(5)]
        candles = [(x * 0.62 * M, 0.3 * M, 0.42 * M) for x in (-1, 1)]
        moss = w.points_on_box((0, 0.2 * M, 0), (0.7 * M, 0.0, 0.5 * M), 3, top_only=True)
        result = [
            Part(lambda p: rounded_box(p, (0, 0.08 * M, 0), (0.8 * M, 0.08 * M, 0.6 * M), 0.02 * M), DARK_STONE),
            Part(lambda p: rounded_box(p, (0, 0.22 * M, 0), (0.62 * M, 0.07 * M, 0.46 * M), 0.02 * M), DARK_STONE),
            Part(lambda p: rounded_box(p @ top, (0, 0.9 * M, 0), (0.42 * M, 0.62 * M, 0.14 * M), 0.08 * M), STONE),
            Part(lambda p: ellipsoid(p @ top, (0, 1.5 * M, 0), (0.42 * M, 0.2 * M, 0.14 * M)), STONE),
            Part(lambda p: _union(*(rounded_box(p @ top, (x, y, 0.13 * M), (half, 0.022 * M, 0.02 * M), 0.008 * M)
                                    for x, y, half in glyphs)), GLYPH),
            Part(lambda p: _union(*(cylinder(p, c, 0.05 * M, 0.08 * M, 0.02 * M) for c in candles)), WAX),
            Part(lambda p: _union(*(ellipsoid(p, c, (0.18 * M, 0.05 * M, 0.14 * M)) for c in moss)), MOSS),
        ]
        if awake:
            result.append(Part(lambda p: _union(*(sphere(p, (c[0], c[1] + 0.13 * M, c[2]), 0.04 * M) for c in candles)), WAX))
        return result

    return PropModel(stem, parts, materials, AXIS_Y_YAW, canvas=(96, 120),
                     footprint=box_footprint(0.8 * M * MEMORIAL_SCALE, 0.6 * M * MEMORIAL_SCALE))


def memory_shard(stem: str, seed: int) -> PropModel:
    """Éclat de mémoire : petit cristal doré qui flotte, à ramasser autour d'un Mémorial."""
    CORE, EDGE = range(2)
    materials = [make_emissive("core", "#F6D98A"), make_material("edge", "#C99A3A", contrast=0.8)]

    def parts() -> list[Part]:
        return [Part(_scaled(part.distance, SHARD_SCALE), part.material) for part in base_parts()]

    def base_parts() -> list[Part]:
        tilt = rotation_z(0.35)
        return [
            Part(lambda p: capsule(p @ tilt, (0, 0.35 * M, 0), (0, 0.85 * M, 0), 0.12 * M, 0.02 * M), EDGE),
            Part(lambda p: capsule(p @ tilt, (0, 0.35 * M, 0), (0, 0.15 * M, 0), 0.12 * M, 0.02 * M), EDGE),
            Part(lambda p: sphere(p @ tilt, (0, 0.4 * M, 0.05 * M), 0.07 * M), CORE),
        ]

    return PropModel(stem, parts, materials, AXIS_Y_YAW, canvas=(40, 64))


def rift(stem: str, open_rift: bool, seed: int) -> PropModel:
    """Faille : déchirure du sol bordée de roche soulevée ; ouverte, des fragments flottent au-dessus d'un fond violet."""
    VOID, RIM, ROCK, SHARD = range(4)
    materials = [
        make_emissive("void", "#8A4FD0") if open_rift else make_material("void", "#2A2230", contrast=0.4),
        make_material("rim", "#3E3048", contrast=0.6),
        make_material("rock", "#5E5866"),
        make_material("shard", "#8C84A0", contrast=0.9),
    ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        # Tracé en zigzag : une suite de segments plats, plus larges au centre.
        points = [(-1.3, -0.1), (-0.85, 0.16), (-0.4, -0.12), (0.05, 0.18), (0.5, -0.1), (0.95, 0.14), (1.35, -0.06)]
        segments = [((a[0] * M, 0.01 * M, a[1] * M), (b[0] * M, 0.01 * M, b[1] * M), (0.2 - 0.1 * abs(a[0])) * M)
                    for a, b in zip(points, points[1:])]
        rocks = [(x * M, 0.06 * M, (z + w.uniform(-0.05, 0.05)) * M) for x, z in ((-0.8, 0.35), (-0.2, -0.35), (0.4, 0.38), (0.95, -0.3))]
        # Bord et vide aplatis au ras du sol (capsules écrasées en hauteur) : vu d'en haut, le vide affleure
        # au milieu du bord au lieu d'être recouvert par sa rondeur.
        squash = np.array([1.0, 4.0, 1.0])
        result = [
            Part(lambda p: _union(*(capsule(p * squash, a, b, r * 1.45, r * 1.25) for a, b, r in segments)) / 4.0, RIM),
            Part(lambda p: _union(*(capsule((p - np.array([0, 0.03 * M, 0])) * squash, a, b, r, r * 0.85)
                                    for a, b, r in segments)) / 4.0, VOID),
            Part(lambda p: _union(*(ellipsoid(p, c, (0.18 * M, 0.1 * M, 0.14 * M), rotation_y(w.uniform(0, 3))) for c in rocks)), ROCK),
        ]
        if open_rift:
            shards = [((x + w.uniform(-0.1, 0.1)) * M, (0.5 + 0.35 * i % 3 * 0.3) * M, w.uniform(-0.1, 0.1) * M)
                      for i, x in enumerate((-0.55, -0.1, 0.3, 0.7))]
            result.append(Part(lambda p: _union(*(capsule(p, (c[0], c[1] - 0.12 * M, c[2]), (c[0] + 0.04 * M, c[1] + 0.12 * M, c[2]),
                                                          0.07 * M, 0.01 * M) for c in shards)), SHARD))
        return result

    return PropModel(stem, parts, materials, AXIS_Y_YAW, canvas=(120, 80))


def catalog() -> list[PropModel]:
    return [
        memorial("memorial_dormant", False, 7),
        memorial("memorial_awake", True, 7),
        memory_shard("memory_shard", 3),
        rift("rift_open", True, 13),
        rift("rift_closed", False, 13),
    ]
