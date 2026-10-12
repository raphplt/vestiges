"""
Oiseau commun au corbeau et au pigeon (plan 39 B2) : au sol il picore, en vol il bat des ailes. Corps, tête, bec, queue,
ailes repliées le long du flanc ou déployées et levées par un angle de battement, pattes rentrées en vol.
"""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from ..render import Part
from ..sdf import capsule, ellipsoid, rotation_x, rotation_z, sphere


@dataclass(frozen=True)
class Bird:
    peck: float = 0.0
    flight: float = 0.0
    flap: float = 0.0
    rise: float = 0.0
    turn: float = 0.0


@dataclass(frozen=True)
class Plumage:
    size: float
    body: int
    wing: int
    head: int
    beak: int
    legs: int
    neck: int | None = None


def bird_parts(b: Bird, m: Plumage) -> list[Part]:
    k = m.size
    # Au sol, le corps se redresse et la queue descend ; en vol, il s'allonge à l'horizontale.
    pitch = rotation_x(-0.15 * b.flight + 0.3 * (1.0 - b.flight))
    center = np.array([0.0, (5.0 + b.rise) * k, 0.0])
    head = center + np.array([b.turn * 1.2 * k, (3.4 - b.peck * 4.2) * k, (5.0 + b.peck * 1.4) * k])
    beak_dir = np.array([0.0, -0.4 - b.peck * 0.6, 1.0])
    beak_dir = beak_dir / np.linalg.norm(beak_dir)
    result = [
        Part(lambda p: ellipsoid(p, center, (3.0 * k, 2.9 * k, 5.0 * k), pitch), m.body),
        Part(lambda p: ellipsoid(p, center + pitch @ np.array([0.0, 0.0, -6.8 * k]), (2.2 * k, 0.8 * k, 4.0 * k), pitch), m.wing),
        Part(lambda p: sphere(p, head, 2.3 * k), m.head),
        Part(lambda p: capsule(p, head + beak_dir * 1.8 * k, head + beak_dir * 4.0 * k, 0.8 * k, 0.3 * k), m.beak),
    ]
    if m.neck is not None:
        neck = center + (head - center) * 0.55
        result.append(Part(lambda p: sphere(p, neck, 2.1 * k), m.neck))
    for side in (1.0, -1.0):
        if b.flight < 0.5:
            # Ailes repliées le long du flanc.
            result.append(Part(lambda p, s=side: ellipsoid(p, center + np.array([s * 2.6 * k, 0.6 * k, -1.2 * k]),
                                                           (1.0 * k, 2.2 * k, 4.6 * k), pitch), m.wing))
            hip = center + np.array([side * 1.1 * k, -2.0 * k, 0.4 * k])
            foot = np.array([hip[0], 0.0, hip[2] + 0.5 * k])
            result.append(Part(lambda p, a=hip, f=foot: capsule(p, a, f, 0.55 * k, 0.4 * k), m.legs))
        else:
            # Aile déployée, levée ou baissée par le battement autour de l'axe du corps.
            root = center + np.array([side * 2.4 * k, 1.0 * k, 0.4 * k])
            raise_angle = side * b.flap
            wing_rotation = rotation_z(raise_angle)
            middle = root + wing_rotation @ np.array([side * 4.8 * k, 0.0, -0.6 * k])
            result.append(Part(lambda p, c=middle, r=wing_rotation: ellipsoid(p, c, (5.0 * k, 0.7 * k, 2.6 * k), r), m.wing))
    return result


def bird_animations() -> dict[str, list[Bird]]:
    stand = Bird()
    fly = Bird(flight=1.0, rise=6.0)
    return {
        # Au sol : picore deux fois, puis tourne la tête.
        "idle": [stand, replace(stand, peck=1.0), replace(stand, peck=0.6), replace(stand, turn=1.0)],
        # Envol : battements amples, ailes hautes puis basses.
        "fly": [replace(fly, flap=1.0), replace(fly, flap=0.45), replace(fly, flap=-0.45, rise=5.4),
                replace(fly, flap=0.1, rise=5.8)],
    }
