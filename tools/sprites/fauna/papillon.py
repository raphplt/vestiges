"""Papillon (plan 39 B2) : forêt. Ailes or-orangé bordées de brun, vole en zigzag et s'écarte du passage."""
from __future__ import annotations

from dataclasses import dataclass

import numpy as np

from ..palette import make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, rotation_z

FRAME_SIZE = (20, 28)
FRAME_PIVOT = (10.0, 24.0)
SCALE = 1.2

WING, EDGE, BODY = range(3)
MATERIALS = [
    make_material("wing", "#E0A840"),
    make_material("edge", "#6A3A20", contrast=0.6),
    make_material("body", "#2A2320", contrast=0.5),
]
HEIGHT = 16.0


@dataclass(frozen=True)
class Flutter:
    flap: float = 0.0
    bob: float = 0.0


def parts(f: Flutter) -> list[Part]:
    center = np.array([0.0, HEIGHT + f.bob, 0.0])
    result = [Part(lambda p: capsule(p, center + np.array([0.0, 0.0, -1.6]), center + np.array([0.0, 0.0, 1.6]), 0.6, 0.5), BODY)]
    for side in (1.0, -1.0):
        rotation = rotation_z(side * f.flap)
        fore = center + rotation @ np.array([side * 2.4, 0.0, 0.8])
        hind = center + rotation @ np.array([side * 1.9, 0.0, -1.4])
        result += [
            Part(lambda p, c=fore, r=rotation: ellipsoid(p, c, (2.6, 0.35, 1.9), r), WING),
            Part(lambda p, c=hind, r=rotation: ellipsoid(p, c, (2.0, 0.35, 1.5), r), EDGE),
        ]
    return result


ANIMATIONS = {"fly": [Flutter(flap=1.2, bob=0.6), Flutter(flap=0.4, bob=0.2), Flutter(flap=-0.3, bob=-0.4), Flutter(flap=0.5, bob=0.0)]}
