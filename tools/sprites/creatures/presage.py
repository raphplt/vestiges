"""
Le Présage — il voit où tu seras (plan 07). Un voile lilas suspendu à rien, une colonne d'yeux inégaux dans une
capuche vide, des éclats de pierre qui flottent au-dessus. Sous lui pend un fil à plomb : à l'incantation, le poids
se lève vers l'endroit visé et luit du même vert que la marque au sol.
"""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, rotation_x, sphere

FRAME_SIZE = (32, 48)
FRAME_PIVOT = (16.0, 45.0)

VEIL, VEIL_DARK, MAW, STONE, STRING, EYE, WEIGHT = range(7)
MATERIALS = [
    make_material("veil", "#A294C8", contrast=0.8),
    make_material("veil_dark", "#5E507F"),
    make_material("maw", "#231632", contrast=0.5),
    make_material("stone", "#6B6161"),
    make_material("string", "#D8D0C0", contrast=0.4),
    make_emissive("eye", "#7FFF00"),
    make_emissive("weight", "#B8FF5A"),
]

HOVER = 14.0
FRINGES = [(-0.4, 1.0), (0.5, 0.8), (1.4, 1.1), (2.3, 0.7), (3.1, 0.95), (4.0, 0.85), (4.9, 1.05), (5.7, 0.75)]


@dataclass(frozen=True)
class Shroud:
    hover: float = 0.0
    lean: float = 0.0
    trail: float = 0.0
    wave: float = 0.0
    spread: float = 0.0
    pendulum: float = 0.0
    collapse: float = 0.0


def parts(s: Shroud) -> list[Part]:
    rotation = rotation_x(s.lean)
    squash = 1.0 - 0.6 * s.collapse
    base = np.array([0.0, (HOVER + s.hover) * (1.0 - s.collapse), 0.0])

    def at(local) -> np.ndarray:
        x, y, z = local
        return base + rotation @ np.array([x, y * squash, z], dtype=np.float64)

    head = at((0.0, 29.0, 0.5))
    result = [
        # Voile en cloche, plus large en bas, bosse asymétrique à l'épaule droite.
        Part(lambda p: capsule(p, at((0, 5.0, 0)), at((0, 23.0, 0)), 5.8 + 2.0 * s.collapse, 3.8), VEIL),
        Part(lambda p: ellipsoid(p, at((-3.4, 20.5, -0.5)), (3.0, 2.8, 3.0), rotation), VEIL),
        # Capuche vide : on ne voit que les yeux, en colonne, jamais alignés.
        Part(lambda p: np.maximum(ellipsoid(p, head, (5.0, 5.6, 5.0), rotation),
                                  -ellipsoid(p, head + rotation @ np.array([0, -0.6, 4.4]), (3.4, 4.4, 2.6), rotation)), VEIL_DARK),
        Part(lambda p: ellipsoid(p, head + rotation @ np.array([0, -0.6, 1.4]), (3.0, 4.2, 2.6), rotation), MAW),
        Part(lambda p: sphere(p, head + rotation @ np.array([0.9, 1.6, 3.4]), 1.3), EYE),
        Part(lambda p: sphere(p, head + rotation @ np.array([-1.2, -0.3, 3.6]), 1.1), EYE),
        Part(lambda p: sphere(p, head + rotation @ np.array([1.1, -2.2, 3.3]), 0.95), EYE),
        # Manches sans bras : elles s'ouvrent pendant l'incantation.
        Part(lambda p: capsule(p, at((5.0, 21.0, 0)), at((6.5 + 7.0 * s.spread, 12.0 + 5.0 * s.spread, 2.5 * s.spread)), 2.8, 1.4), VEIL_DARK),
        Part(lambda p: capsule(p, at((-5.5, 20.0, 0)), at((-7.0 - 6.0 * s.spread, 11.0 + 6.0 * s.spread, 3.0 * s.spread)), 3.0, 1.5), VEIL_DARK),
    ]
    # Éclats de pierre en couronne, qui tombent à la mort.
    for x, y, z, r in ((2.6, 37.5, -1.0, 1.5), (-2.4, 36.5, 0.8, 1.2), (0.6, 40.0, 0.2, 1.1)):
        center = at((x, y + np.sin(s.wave + x) * 0.8, z))
        center[1] = max(center[1] * (1.0 - s.collapse), r)
        result.append(Part(lambda p, c=center, r=r: sphere(p, c, r), STONE))
    # Franges effilochées : elles traînent derrière au déplacement.
    for angle, length in FRINGES:
        top = at((np.cos(angle) * 5.0, 1.5, np.sin(angle) * 5.0))
        drift = np.array([np.sin(s.wave + angle) * 0.8, 0.0, -s.trail * 2.5])
        tip = top + np.array([0.0, -5.5 * length * squash, 0.0]) + drift
        result.append(Part(lambda p, a=top, b=tip: capsule(p, a, b, 1.4, 0.8), VEIL_DARK))
    if s.collapse < 1.0:
        pivot = at((0, -1.0, 0))
        direction = rotation_x(-s.pendulum) @ np.array([0.0, -1.0, 0.0])
        weight = pivot + direction * (HOVER - 4.5 + s.hover * 0.5)
        result += [
            Part(lambda p: capsule(p, pivot, weight, 0.9), STRING),
            Part(lambda p: sphere(p, weight, 2.3), WEIGHT),
        ]
    return result


def _animations() -> dict[str, list[Shroud]]:
    rest = Shroud()
    return {
        "idle": [replace(rest, hover=h, wave=w, pendulum=a)
                 for h, w, a in ((0.0, 0.0, 0.12), (0.8, 1.6, 0.04), (1.2, 3.1, -0.1), (0.8, 4.7, 0.04))],
        "walk": [replace(rest, hover=h, wave=w, lean=0.16, trail=1.0, pendulum=-0.35)
                 for h, w in ((0.4, 0.0), (1.0, 1.6), (0.6, 3.1), (0.0, 4.7))],
        # Incantation : le voile s'ouvre, le poids se lève vers l'endroit où le joueur sera.
        "attack": [
            replace(rest, hover=1.5, spread=0.5, pendulum=0.6, lean=-0.08),
            replace(rest, hover=3.0, spread=1.0, pendulum=1.25, lean=-0.12, wave=1.0),
            replace(rest, hover=2.5, spread=0.8, pendulum=0.95, lean=-0.06, wave=2.0),
            replace(rest, hover=1.0, spread=0.3, pendulum=0.3, wave=3.0),
        ],
        "death": [
            replace(rest, hover=2.0, spread=0.9, pendulum=-0.5, lean=-0.15),
            replace(rest, collapse=0.35, spread=0.6, lean=0.15, wave=1.0),
            replace(rest, collapse=0.7, spread=0.4, lean=0.3, wave=2.0),
            replace(rest, collapse=1.0, spread=0.2, lean=0.35, wave=3.0),
        ],
    }


ANIMATIONS = _animations()
