"""
La Sentinelle — un pilier organique ancré dans les ruines, comme un réverbère devenu hostile (Bible §6.2).
Un fût de chair et de béton planté sur des racines, courbé en crosse comme un lampadaire. La tête, là où serait la
lanterne, porte des paupières : fermées au repos, elles s'ouvrent sur trois yeux vert-acide pour viser et tirer.
Elle ne bouge pas.
Reprise du 11 octobre (plan 37 M4) : tige vert-de-gris grêle, elle se perdait dans le décor. Le réverbère se lit
désormais : socle de béton clair, fût de fonte rouillée envahi de bourrelets de chair, chapeau de lanterne en fonte
sous lequel pend le bulbe de chair aux paupières.
Plan 54 S3 : bourrelets de chair fondus en une seule coulée sur le fût (`smin`), fonte et rouille qui accrochent la
lumière ; elle cligne en veillant, plisse les paupières et avance la tête pour viser avant d'ouvrir grand.
"""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, rotation_x, rotation_z, rounded_box, smin, sphere

FRAME_SIZE = (32, 56)
FRAME_PIVOT = (16.0, 52.0)

FLESH, CONCRETE, ROOT, LID, RUST, EYE, IRON = range(7)
MATERIALS = [
    make_material("flesh", "#A48C9C", contrast=1.1, rim=0.4),
    make_material("concrete", "#A29E96", contrast=1.1),
    make_material("root", "#3A2832", contrast=1.05),
    make_material("lid", "#6A5266", contrast=1.1),
    make_material("rust", "#8A4E34", contrast=1.1, specular=0.35),
    make_emissive("eye", "#7FFF00"),
    make_material("iron", "#3C404C", contrast=1.1, specular=0.5),
]

K = 1.6


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
    head = at(1.2, 28.5 - s.recoil, 4.6 + s.recoil)
    result = [
        # Socle de béton du réverbère d'origine, fût de fonte rouillée.
        Part(lambda p: rounded_box(p, at(0.0, 1.4), (3.6 * K, 1.4 * K, 3.6 * K), 0.4 * K, rotation), CONCRETE),
        Part(lambda p: capsule(p, at(0.0, 2.0), at(0.0, 6.0), 2.9 * K, 2.4 * K), CONCRETE),
        Part(lambda p: capsule(p, at(0.0, 5.0), neck, 1.5 * K, 1.2 * K), RUST),
        # Bourrelets de chair qui ont envahi le fût, de plus en plus serrés vers le haut.
        Part(lambda p: smin(smin(ellipsoid(p, at(0.3, 11.0, 0.2), (3.1 * K, 3.4 * K, 2.9 * K), rotation),
                                 ellipsoid(p, at(-0.4, 17.0, 0.0), (2.8 * K, 3.8 * K, 2.6 * K), rotation), 0.6 * K),
                            ellipsoid(p, at(0.5, 22.5, 0.4), (2.4 * K, 2.6 * K, 2.3 * K), rotation), 0.6 * K), FLESH),
        # Crosse du réverbère : la tête pend vers l'avant, sous un chapeau de lanterne en fonte.
        Part(lambda p: capsule(p, neck, head + rotation @ np.array([0.0, 3.6 * K, -1.0 * K]), 1.2 * K, 1.0 * K), RUST),
        Part(lambda p: ellipsoid(p, head + rotation @ np.array([0.0, 3.4 * K, -0.4 * K]), (3.6 * K, 1.1 * K, 3.4 * K), rotation), IRON),
        Part(lambda p: capsule(p, head + rotation @ np.array([0.0, 4.2 * K, -0.4 * K]), head + rotation @ np.array([0.0, 5.6 * K, -0.4 * K]),
                               1.1 * K, 0.4 * K), IRON),
        Part(lambda p: ellipsoid(p, head, (3.8 * K, 3.0 * K, 3.6 * K), rotation), FLESH),
    ]
    # Paupières : elles se rétractent pour découvrir les yeux.
    eyes = [(1.4, 0.2, 1.05), (-1.3, 0.7, 0.85), (0.2, -1.3, 0.75)]
    for x, y, radius in eyes:
        center = head + rotation @ np.array([x * K, y * K, 3.0 * K])
        result.append(Part(lambda p, c=center, r=radius: sphere(p, c, r * K * (0.3 + 0.7 * s.open)), EYE))
        lid = center + rotation @ np.array([0.0, (0.7 + 1.8 * s.open) * K, -0.2 * K])
        result.append(Part(lambda p, c=lid, r=radius: ellipsoid(p, c, (r * 1.5 * K, r * (1.1 - 0.8 * s.open) * K + 0.2, r * K)),
                           LID))
    for angle in (0.4, 1.9, 3.3, 4.9):
        base = at(np.cos(angle) * 2.0, 2.0, np.sin(angle) * 2.0)
        foot = np.array([np.cos(angle) * 6.5 * K, 0.2 * K, np.sin(angle) * 5.5 * K])
        result.append(Part(lambda p, a=base, b=foot: capsule(p, a, b, 1.3 * K, 0.6 * K), ROOT))
    return result


def _animations() -> dict[str, list[Pillar]]:
    rest = Pillar()
    # Paupières entrouvertes au repos : elle veille, ses yeux se devinent, et elle cligne une fois par cycle.
    idle = [replace(rest, open=0.55), replace(rest, sway=0.02, open=0.6), replace(rest, sway=0.03, open=0.1),
            replace(rest, sway=0.01, open=0.55)]
    return {
        "idle": idle,
        # Elle ne marche pas : la même veille, un peu plus ouverte quand on la presse.
        "walk": [replace(pose, open=pose.open + 0.2) for pose in idle],
        # Tir : elle plisse les paupières en avançant la tête pour viser, ouvre grand, recule sous le recul du
        # projectile, puis se rassied.
        "attack": [replace(rest, open=0.25, recoil=-0.6), replace(rest, open=1.1), replace(rest, open=1.0, recoil=1.4),
                   replace(rest, open=0.7, recoil=0.4)],
        "death": [replace(rest, open=1.1, fall=0.1), replace(rest, open=0.6, fall=0.4), replace(rest, open=0.2, fall=0.8),
                  replace(rest, open=0.0, fall=1.15)],
    }


ANIMATIONS = _animations()
