"""
Le Présage — il voit où tu seras (plan 07). Un voile lilas suspendu à rien, une colonne d'yeux inégaux dans une
capuche vide, des éclats de pierre qui flottent au-dessus. Sous lui pend un fil à plomb : à l'incantation, le poids
se lève vers l'endroit visé et luit du même vert que la marque au sol.
Reprise du 11 octobre (plan 37 M4) : le voile droit se lisait comme une gélule ; il s'évase en cloche, la capuche
pointe et penche vers l'avant pour montrer son vide, les franges pendent plus bas et la couronne de pierres s'écarte.
Plan 54 S3 : bosse fondue dans le voile (`smin`), voile au bord éclairé, pierres qui accrochent la lumière ; il dérive
par à-coups, le fil à plomb en retard sur le corps, et se recueille (manches refermées, capuche baissée) avant de
s'ouvrir pour l'incantation.
"""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, rotation_x, smin, sphere

FRAME_SIZE = (32, 48)
FRAME_PIVOT = (16.0, 45.0)

VEIL, VEIL_DARK, MAW, STONE, STRING, EYE, WEIGHT = range(7)
MATERIALS = [
    make_material("veil", "#A294C8", contrast=0.9, rim=0.6),
    make_material("veil_dark", "#4E3F72", contrast=1.1),
    make_material("maw", "#231632", contrast=0.5),
    make_material("stone", "#A39C94", contrast=1.15, specular=0.45),
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

    head = at((0.0, 29.0, 1.2))
    # Capuche penchée vers l'avant : vue d'en haut à 30°, son vide et ses yeux restent visibles.
    hood = rotation @ rotation_x(0.35)
    result = [
        # Voile en cloche, évasé vers le bas, bosse asymétrique à l'épaule droite.
        Part(lambda p: smin(capsule(p, at((0, 4.0, 0)), at((0, 23.0, 0)), 8.2 + 2.0 * s.collapse, 3.6),
                            ellipsoid(p, at((-3.6, 20.5, -0.5)), (3.4, 3.0, 3.2), rotation), 1.8), VEIL),
        # Capuche vide : on ne voit que les yeux, en colonne, jamais alignés.
        Part(lambda p: np.maximum(ellipsoid(p, head, (5.4, 5.8, 5.4), hood),
                                  -ellipsoid(p, head + hood @ np.array([0, -0.6, 4.6]), (3.6, 4.6, 2.8), hood)), VEIL_DARK),
        # Pointe de capuche rabattue vers l'arrière.
        Part(lambda p: capsule(p, head + hood @ np.array([0, 4.2, -1.5]), head + hood @ np.array([0.8, 8.5, -4.5]), 2.6, 0.7), VEIL_DARK),
        Part(lambda p: ellipsoid(p, head + hood @ np.array([0, -0.6, 1.6]), (3.4, 4.6, 2.6), hood), MAW),
        Part(lambda p: sphere(p, head + hood @ np.array([0.9, 1.8, 3.4]), 1.7), EYE),
        Part(lambda p: sphere(p, head + hood @ np.array([-1.3, -0.4, 3.6]), 1.45), EYE),
        Part(lambda p: sphere(p, head + hood @ np.array([1.0, -2.6, 3.3]), 1.25), EYE),
        # Manches sans bras : elles s'ouvrent pendant l'incantation.
        Part(lambda p: capsule(p, at((5.0, 21.0, 0)), at((6.5 + 7.0 * s.spread, 12.0 + 5.0 * s.spread, 2.5 * s.spread)), 2.8, 1.4), VEIL_DARK),
        Part(lambda p: capsule(p, at((-5.5, 20.0, 0)), at((-7.0 - 6.0 * s.spread, 11.0 + 6.0 * s.spread, 3.0 * s.spread)), 3.0, 1.5), VEIL_DARK),
    ]
    # Éclats de pierre en couronne, qui tombent à la mort.
    for x, y, z, r in ((4.6, 38.5, -1.0, 2.3), (-4.4, 37.0, 0.8, 2.0), (0.4, 42.5, -0.6, 1.8)):
        center = at((x, y + np.sin(s.wave + x) * 0.8, z))
        center[1] = max(center[1] * (1.0 - s.collapse), r)
        result.append(Part(lambda p, c=center, r=r: sphere(p, c, r), STONE))
    # Franges effilochées : elles traînent derrière au déplacement.
    for angle, length in FRINGES:
        top = at((np.cos(angle) * 7.6, 2.0, np.sin(angle) * 7.6))
        drift = np.array([np.sin(s.wave + angle) * 1.0, 0.0, -s.trail * 3.0])
        tip = top + np.array([np.cos(angle) * 1.2, -8.0 * length * squash, np.sin(angle) * 1.2]) + drift
        result.append(Part(lambda p, a=top, b=tip: capsule(p, a, b, 1.6, 0.6), VEIL_DARK))
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
        # Flottement : le fil à plomb oscille à contretemps du voile.
        "idle": [replace(rest, hover=h, wave=w, pendulum=a, lean=l)
                 for h, w, a, l in ((0.0, 0.0, 0.14, 0.02), (0.8, 1.6, 0.02, -0.02), (1.3, 3.1, -0.12, -0.04),
                                    (0.7, 4.7, 0.0, 0.0))],
        # Dérive par à-coups : élan penché, le poids traîne derrière puis rattrape le corps quand il ralentit.
        "walk": [replace(rest, hover=h, wave=w, lean=l, trail=t, pendulum=a)
                 for h, w, l, t, a in ((0.2, 0.0, 0.22, 1.2, -0.48), (1.2, 1.6, 0.15, 0.9, -0.32),
                                       (0.9, 3.1, 0.08, 0.6, -0.08), (0.0, 4.7, 0.18, 1.1, -0.36))],
        # Incantation : il se recueille (manches closes, capuche baissée), puis le voile s'ouvre et le poids se lève
        # vers l'endroit où le joueur sera.
        "attack": [
            replace(rest, hover=-0.6, spread=-0.25, pendulum=-0.3, lean=0.22),
            replace(rest, hover=3.4, spread=1.15, pendulum=1.35, lean=-0.18, wave=1.0),
            replace(rest, hover=2.8, spread=0.9, pendulum=1.0, lean=-0.08, wave=2.0),
            replace(rest, hover=1.0, spread=0.3, pendulum=0.3, wave=3.0),
        ],
        "death": [
            replace(rest, hover=2.4, spread=1.0, pendulum=-0.6, lean=-0.2),
            replace(rest, collapse=0.35, spread=0.6, lean=0.15, wave=1.0),
            replace(rest, collapse=0.7, spread=0.4, lean=0.3, wave=2.0),
            replace(rest, collapse=1.0, spread=0.2, lean=0.35, wave=3.0),
        ],
    }


ANIMATIONS = _animations()
