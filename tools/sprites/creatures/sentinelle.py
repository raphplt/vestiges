"""
La Sentinelle — un pilier organique ancré dans les ruines, comme un réverbère devenu hostile (Bible §6.2).
Un fût de chair et de béton planté sur des racines, courbé en crosse comme un lampadaire. La tête, là où serait la
lanterne, porte des paupières : fermées au repos, elles s'ouvrent sur trois yeux vert-acide pour viser et tirer.
Elle ne bouge pas.
"""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, rotation_x, rotation_z, rounded_box, sphere

FRAME_SIZE = (32, 56)
FRAME_PIVOT = (16.0, 52.0)

FLESH, CONCRETE, ROOT, LID, RUST, EYE = range(6)
MATERIALS = [
    make_material("flesh", "#5A6A4E"),
    make_material("concrete", "#8A8680"),
    make_material("root", "#3E3428"),
    make_material("lid", "#46503C"),
    make_material("rust", "#6A3A24"),
    make_emissive("eye", "#7FFF00"),
]

K = 1.25


@dataclass(frozen=True)
class Pillar:
    open: float = 0.0
    sway: float = 0.0
    recoil: float = 0.0
    fall: float = 0.0


def parts(s: Pillar) -> list[Part]:
    rotation = rotation_z(s.sway + s.fall * 1.3) @ rotation_x(s.fall * 0.3)

    def at(x: float, y: float, z: float = 0.0) -> np.ndarray:
        return rotation @ np.array([x * K, y * K, z * K])

    neck = at(0.0, 26.0)
    head = at(3.5, 28.5 - s.recoil, 3.5 + s.recoil)
    result = [
        # Fût : béton en bas (le réverbère d'origine), chair qui l'a envahi en haut.
        Part(lambda p: capsule(p, at(0.0, 1.0), at(0.0, 12.0), 2.8 * K, 2.3 * K), CONCRETE),
        Part(lambda p: rounded_box(p, at(0.0, 1.2), (3.4 * K, 1.2 * K, 3.4 * K), 0.4 * K, rotation), CONCRETE),
        Part(lambda p: capsule(p, at(0.0, 10.0), neck, 2.4 * K, 1.8 * K), FLESH),
        # Crosse du réverbère : la tête pend vers l'avant.
        Part(lambda p: capsule(p, neck, head, 1.8 * K, 2.0 * K), FLESH),
        Part(lambda p: ellipsoid(p, head, (3.6 * K, 2.6 * K, 3.4 * K), rotation), FLESH),
        # Traces de rouille et d'anciennes fixations.
        Part(lambda p: ellipsoid(p, at(1.6, 7.0, 1.8), (1.0 * K, 1.8 * K, 0.6 * K), rotation), RUST),
    ]
    # Paupières : elles se rétractent pour découvrir les yeux.
    eyes = [(1.4, 0.2, 0.75), (-1.2, 0.8, 0.6), (0.2, -1.0, 0.55)]
    for x, y, radius in eyes:
        center = head + rotation @ np.array([x * K, y * K, 2.8 * K])
        result.append(Part(lambda p, c=center, r=radius: sphere(p, c, r * K * (0.3 + 0.7 * s.open)), EYE))
        lid = center + rotation @ np.array([0.0, (0.6 + 0.9 * s.open) * K, 0.3 * K])
        result.append(Part(lambda p, c=lid, r=radius: ellipsoid(p, c, (r * 1.5 * K, r * (1.2 - 0.8 * s.open) * K + 0.2, r * K)),
                           LID))
    for angle in (0.4, 1.9, 3.3, 4.9):
        base = at(np.cos(angle) * 2.0, 2.0, np.sin(angle) * 2.0)
        foot = np.array([np.cos(angle) * 6.5 * K, 0.2 * K, np.sin(angle) * 5.5 * K])
        result.append(Part(lambda p, a=base, b=foot: capsule(p, a, b, 1.3 * K, 0.6 * K), ROOT))
    return result


def _animations() -> dict[str, list[Pillar]]:
    rest = Pillar()
    idle = [rest, replace(rest, sway=0.02), replace(rest, sway=0.03, open=0.15), replace(rest, sway=0.01)]
    return {
        "idle": idle,
        # Elle ne marche pas : la même veille, un peu plus ouverte quand on la presse.
        "walk": [replace(pose, open=pose.open + 0.2) for pose in idle],
        # Tir : les paupières s'ouvrent grand, la tête recule sous le recul du projectile.
        "attack": [replace(rest, open=0.6), replace(rest, open=1.0), replace(rest, open=1.0, recoil=1.2),
                   replace(rest, open=0.7, recoil=0.4)],
        "death": [replace(rest, open=1.0, fall=0.1), replace(rest, open=0.6, fall=0.4), replace(rest, open=0.2, fall=0.8),
                  replace(rest, open=0.0, fall=1.15)],
    }


ANIMATIONS = _animations()
