"""
La Tisseuse — arachnéenne mais sans forme fixe (Bible §6.2) : un petit corps sombre qui pulse d'un cœur vert-acide,
porté haut par huit longues pattes filandreuses et pâles, genoux relevés au-dessus de lui, aucune de la même longueur.
Une filière pend sous l'abdomen ; elle tisse les fils d'effacement qui immobilisent (plan 07).
Reprise du 11 octobre (plan 37 M4) : remplace l'ancien générateur en voxels (scripts/generate_tisseuse.py, quatre
directions, 64 × 64), d'un autre style que le reste du bestiaire.
Plan 54 S3 : pattes de chitine pâle qui accrochent la lumière, abdomen au bord éclairé ; elle avance par à-coups
d'insecte (deux pas vifs, un arrêt où une patte tressaille) et se tasse au ras du sol avant de se cabrer.
"""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, rotation_x, rotation_y, sphere

FRAME_SIZE = (48, 48)
FRAME_PIVOT = (24.0, 40.0)

BODY, BODY_DARK, LEG, JOINT, THREAD, CORE, EYE = range(7)
MATERIALS = [
    make_material("body", "#3A2A4A", contrast=0.9, rim=0.5),
    make_material("body_dark", "#1E1528", contrast=0.7),
    make_material("leg", "#BDB5AA", contrast=0.8, specular=0.45),
    make_material("joint", "#5C5168", contrast=1.1),
    make_material("thread", "#DCE6E8", contrast=0.4),
    make_emissive("core", "#9DFF3A"),
    make_emissive("eye", "#7FFF00"),
]

BODY_HEIGHT = 9.0
# Pattes : angle d'attache autour du corps (0 = avant), longueur relative, hauteur du genou. Asymétriques.
LEGS = [
    (0.45, 1.08, 1.0), (1.05, 0.95, 0.9), (1.75, 1.0, 1.05), (2.45, 0.9, 0.85),
    (-0.5, 1.0, 1.1), (-1.1, 1.05, 0.95), (-1.8, 0.92, 1.0), (-2.5, 1.0, 0.9),
]
REACH = 24.0
KNEE_HEIGHT = 7.0
# Marche en deux groupes alternés de quatre pattes.
GAIT = [0.0, np.pi, 0.0, np.pi, np.pi, 0.0, np.pi, 0.0]


@dataclass(frozen=True)
class Weaver:
    phase: float = 0.0
    stride: float = 0.0
    lift: float = 0.0
    bob: float = 0.0
    pulse: float = 0.0
    rear: float = 0.0
    curl: float = 0.0
    twitch: float = 0.0


def parts(w: Weaver) -> list[Part]:
    height = (BODY_HEIGHT + w.bob) * (1.0 - 0.75 * w.curl)
    # Cabrée à l'attaque : le corps bascule en arrière, les pattes avant se lèvent.
    pitch = rotation_x(-0.55 * w.rear)
    center = np.array([0.0, height, -1.0])

    def at(local) -> np.ndarray:
        return center + pitch @ np.asarray(local, dtype=np.float64)

    swell = 1.0 + 0.12 * w.pulse
    result = [
        # Abdomen bombé à l'arrière, fendu sur le dos : le cœur vert y bat.
        Part(lambda p: ellipsoid(p, at((0.0, 0.6, -3.2)), (5.6 * swell, 4.6 * swell, 6.6 * swell), pitch), BODY),
        Part(lambda p: ellipsoid(p, at((0.0, 4.4, -3.6)), (1.9 + 0.6 * w.pulse, 1.4 + 0.5 * w.pulse, 3.6), pitch), CORE),
        Part(lambda p: ellipsoid(p, at((0.0, -1.6, -3.0)), (3.4, 2.0, 4.2), pitch), BODY_DARK),
        # Céphalothorax réduit, grappe d'yeux sans ordre.
        Part(lambda p: ellipsoid(p, at((0.0, 0.2, 3.0)), (3.0, 2.5, 2.9), pitch), BODY_DARK),
        Part(lambda p: sphere(p, at((0.9, 1.0, 5.4)), 1.05), EYE),
        Part(lambda p: sphere(p, at((-1.1, 0.5, 5.5)), 0.9), EYE),
        Part(lambda p: sphere(p, at((0.2, 2.0, 4.9)), 0.75), EYE),
        # Filière : un fil pend sous l'abdomen jusqu'au sol.
        Part(lambda p: capsule(p, at((0.0, -2.8, -6.6)), np.array([0.6, 0.6, -9.0 - 2.0 * w.rear]), 0.55, 0.35), THREAD),
    ]

    for (angle, length, knee_scale), offset in zip(LEGS, GAIT):
        side = np.sign(angle)
        hip = at((np.sin(angle) * 3.4, -0.4, np.cos(angle) * 3.0 + 0.4))
        outward = rotation_y(angle) @ np.array([0.0, 0.0, 1.0])
        cycle = w.phase + offset
        step = np.sin(cycle) * w.stride
        raise_foot = max(np.cos(cycle), 0.0) * w.lift
        foot = np.array([0.0, 0.0, 0.0]) + outward * REACH * length + np.array([0.0, raise_foot, step])
        foot[1] = max(foot[1], 0.4)
        front = np.cos(angle) > 0.3
        if front and w.rear > 0:
            # Pattes avant levées, pointes vers la proie.
            foot = hip + outward * 12.0 * length + np.array([0.0, 11.0 * w.rear, 4.0 * w.rear])
        twitch = np.array([0.0, w.twitch * np.sin(angle * 3.1), 0.0])
        knee = hip + outward * REACH * 0.4 * length + np.array([0.0, KNEE_HEIGHT * knee_scale * (1.0 - 0.6 * w.curl), 0.0]) + twitch
        # Repliées à la mort : les pieds reviennent sous le corps.
        foot = foot + (hip + np.array([side * 1.5, -2.0, 0.0]) - foot) * w.curl
        knee = knee + (hip + outward * 4.0 + np.array([0.0, 3.0, 0.0]) - knee) * w.curl
        result += [
            Part(lambda p, a=hip, k=knee: capsule(p, a, k, 1.25, 1.0), LEG),
            Part(lambda p, k=knee: sphere(p, k, 1.3), JOINT),
            Part(lambda p, k=knee, f=foot: capsule(p, k, f, 0.95, 0.55), LEG),
        ]
    return result


def _animations() -> dict[str, list[Weaver]]:
    rest = Weaver()
    return {
        "idle": [replace(rest, pulse=pl, bob=b, twitch=t)
                 for pl, b, t in ((0.0, 0.0, 0.0), (0.6, 0.4, 0.9), (1.0, 0.6, 0.0), (0.5, 0.2, -0.9))],
        # À-coups d'insecte : deux pas vifs, corps qui file bas, puis un arrêt haut où une patte tressaille.
        "walk": [replace(rest, phase=i * np.pi / 2, stride=3.4, lift=3.2, bob=b, pulse=pl, twitch=t)
                 for i, (b, pl, t) in enumerate(((0.2, 0.2, 0.0), (-0.4, 0.4, 0.0), (0.9, 0.1, 1.0), (0.5, 0.3, -0.4)))],
        # Elle se tasse au ras du sol, se cabre pattes avant levées, le cœur s'emballe, puis crache sa toile.
        "attack": [
            replace(rest, bob=-2.0, pulse=0.6, twitch=0.6),
            replace(rest, rear=1.0, pulse=1.0, bob=1.6),
            replace(rest, rear=0.85, pulse=1.0, bob=1.1, twitch=1.0),
            replace(rest, rear=0.3, pulse=0.4, bob=0.3),
        ],
        "death": [
            replace(rest, pulse=1.0, twitch=1.5, bob=0.8),
            replace(rest, curl=0.35, pulse=0.6, twitch=-1.0),
            replace(rest, curl=0.7, pulse=0.2),
            replace(rest, curl=1.0),
        ],
    }


ANIMATIONS = _animations()
