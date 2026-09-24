"""
Le Charognard — comme un chien ou un rat, mais avec trop de pattes et pas assez de tête (Bible §6.2).
Corps bas et allongé de fourrure et de mousse, six pattes de longueurs inégales, un moignon de tête fendu
d'une gueule, deux yeux vert-acide qui ne sont pas à la même hauteur. Meute nerveuse : il bondit (plan 07).
"""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, rotation_x, rotation_z, sphere

FRAME_SIZE = (32, 32)
FRAME_PIVOT = (16.0, 24.0)

FUR, BELLY, MOSS, STONE, LEG, MAW, EYE = range(7)
MATERIALS = [
    make_material("fur", "#5A4330"),
    make_material("belly", "#3E2F24"),
    make_material("moss", "#558844"),
    make_material("stone", "#77716A"),
    make_material("leg", "#33281F"),
    make_material("maw", "#2B1B3D", contrast=0.5),
    make_emissive("eye", "#7FFF00"),
]

K = 1.25
BODY_HEIGHT = 9.0 * K
# Hanches (x côté gauche positif, z avant), longueur relative : aucune paire n'est symétrique.
HIPS = [
    (4.0 * K, 6.5 * K, 1.0), (-4.3 * K, 6.0 * K, 0.9),
    (4.4 * K, 0.0, 1.05), (-4.2 * K, 0.5 * K, 1.0),
    (3.8 * K, -6.5 * K, 0.95), (-4.1 * K, -6.0 * K, 1.1),
]
# Marche en trépied : avant gauche, milieu droit et arrière gauche ensemble.
GAIT_OFFSETS = [0.0, np.pi, np.pi, 0.0, 0.0, np.pi]


@dataclass(frozen=True)
class Beast:
    phase: float = 0.0
    stride: float = 0.0
    lift: float = 0.0
    bob: float = 0.0
    pitch: float = 0.0
    crouch: float = 0.0
    stretch: float = 1.0
    jaw: float = 0.1
    roll: float = 0.0
    tuck: float = 0.0
    tail: float = 0.0


def parts(b: Beast) -> list[Part]:
    rotation = rotation_z(b.roll) @ rotation_x(b.pitch)
    center = np.array([0.0, BODY_HEIGHT - b.crouch + b.bob, 0.0])

    def at(local) -> np.ndarray:
        return center + rotation @ np.asarray(local, dtype=np.float64)

    result = [
        Part(lambda p: ellipsoid(p, center, (5.0 * K, 4.2 * K, 10.0 * K * b.stretch), rotation), FUR),
        # Arrière-train plus haut que les épaules, épaule gauche plus massive que la droite.
        Part(lambda p: ellipsoid(p, at((0.6 * K, 1.0 * K, -6.0 * K * b.stretch)), (5.4 * K, 5.0 * K, 5.4 * K), rotation), FUR),
        Part(lambda p: ellipsoid(p, at((1.4 * K, 0.4 * K, 5.0 * K * b.stretch)), (4.6 * K, 4.2 * K, 4.2 * K), rotation), FUR),
        Part(lambda p: ellipsoid(p, at((0.0, -2.2 * K, 0.0)), (3.8 * K, 2.2 * K, 8.5 * K * b.stretch), rotation), BELLY),
        # Crête de mousse et d'éclats de pierre le long du dos.
        Part(lambda p: ellipsoid(p, at((0.8 * K, 3.8 * K, -2.0 * K)), (2.8 * K, 1.4 * K, 6.5 * K), rotation), MOSS),
        Part(lambda p: capsule(p, at((-1.2 * K, 4.0 * K, 2.5 * K)), at((-1.8 * K, 6.4 * K, 1.0 * K)), 1.1 * K, 0.4 * K), STONE),
        Part(lambda p: capsule(p, at((1.5 * K, 5.0 * K, -6.5 * K)), at((2.4 * K, 7.4 * K, -8.0 * K)), 1.2 * K, 0.4 * K), STONE),
        # Moignon de tête : une mâchoire qui s'ouvre sous un bourrelet, sans crâne.
        Part(lambda p: ellipsoid(p, at((0.8 * K, 0.4 * K, 10.8 * K * b.stretch)), (3.4 * K, 2.6 * K, 3.2 * K), rotation), FUR),
        Part(lambda p: ellipsoid(p, at((0.8 * K, -1.2 * K, 11.6 * K * b.stretch)), (2.4 * K, 1.2 * K, 2.2 * K), rotation), MAW),
        Part(lambda p: ellipsoid(p, jaw_center(at, b), (2.8 * K, 1.1 * K, 2.8 * K), rotation @ rotation_x(b.jaw)), BELLY),
        Part(lambda p: sphere(p, at((2.4 * K, 1.4 * K, 13.3 * K * b.stretch)), 1.2 * K), EYE),
        Part(lambda p: sphere(p, at((-1.0 * K, 0.2 * K, 13.7 * K * b.stretch)), 0.95 * K), EYE),
        # Queue fine et nue, qui fouette.
        Part(lambda p: capsule(p, at((0.0, 1.5 * K, -10.5 * K * b.stretch)),
                               at((b.tail * 3.0 * K, 4.5 * K, -15.0 * K * b.stretch)), 1.1 * K, 0.6 * K), BELLY),
        Part(lambda p: capsule(p, at((b.tail * 3.0 * K, 4.5 * K, -15.0 * K * b.stretch)),
                               at((b.tail * 6.0 * K, 3.5 * K, -18.5 * K * b.stretch)), 0.6 * K, 0.45 * K), BELLY),
    ]
    for (x, z, length), offset in zip(HIPS, GAIT_OFFSETS):
        hip = at((x, -1.0 * K, z * b.stretch))
        side = np.sign(x)
        cycle = b.phase + offset
        foot = np.array([hip[0] + side * 2.2 * K, max(np.cos(cycle), 0.0) * b.lift,
                         hip[2] + np.sin(cycle) * b.stride])
        curled = hip + rotation @ np.array([side * 2.0 * K, -3.0 * K, 1.5 * K])
        foot = foot + (curled - foot) * b.tuck
        reach = foot - hip
        # Genou relevé au-dessus de la ligne du corps : une démarche d'insecte plus que de chien.
        knee = hip + reach * 0.45 + rotation @ np.array([side * 2.6 * K, 2.4 * K * length, 0.0])
        foot = hip + reach * length
        result += [
            Part(lambda p, a=hip, k=knee: capsule(p, a, k, 1.5 * K, 1.2 * K), LEG),
            Part(lambda p, k=knee, f=foot: capsule(p, k, f, 1.1 * K, 0.75 * K), LEG),
        ]
    return result


def jaw_center(at, b: Beast) -> np.ndarray:
    return at((0.8 * K, -2.2 * K - b.jaw * 1.2 * K, 10.8 * K * b.stretch + b.jaw * 0.6 * K))


def _animations() -> dict[str, list[Beast]]:
    stand = Beast()
    walk = [replace(stand, phase=i * np.pi / 2, stride=3.6 * K, lift=2.4 * K, bob=0.6 * K * (i % 2),
                    pitch=0.05, tail=(-1.0, 0.0, 1.0, 0.0)[i]) for i in range(4)]
    return {
        "idle": [
            stand,
            replace(stand, bob=0.3, tail=0.5, jaw=0.2),
            replace(stand, bob=0.5, pitch=-0.06, tail=1.0, jaw=0.35),
            replace(stand, bob=0.3, tail=0.4, jaw=0.15),
        ],
        "walk": walk,
        # Bond : corps étiré en vol, gueule grande ouverte, puis réception accroupie.
        "attack": [
            replace(stand, stretch=1.15, pitch=-0.22, tuck=0.6, bob=4.0 * K, jaw=0.6, tail=-1.0),
            replace(stand, stretch=1.1, pitch=0.1, tuck=0.3, bob=2.0 * K, jaw=1.0, tail=-0.5),
            replace(stand, crouch=2.0 * K, pitch=0.22, jaw=0.4, stride=1.2 * K),
            replace(stand, crouch=1.0 * K, pitch=0.08, jaw=0.2),
        ],
        # Renversé sur le flanc, pattes repliées.
        "death": [
            replace(stand, pitch=-0.3, jaw=0.8, bob=1.0 * K, tail=1.0),
            replace(stand, roll=0.6, crouch=2.0 * K, tuck=0.4, jaw=0.6),
            replace(stand, roll=1.15, crouch=3.5 * K, tuck=0.8, jaw=0.4),
            replace(stand, roll=1.45, crouch=4.4 * K, tuck=1.0, jaw=0.3),
        ],
    }


ANIMATIONS = _animations()
