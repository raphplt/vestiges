"""
La Brute du Vide — un amas de chair, de pierre et de métal fusionnés (Bible §6.2).
Quadrupède massif et bas : son corps a absorbé des débris du monde. Une portière rouillée sur le flanc, une dalle de
béton hérissée de ferraille sur le dos, un pied de chaise qui sort de l'épaule. La patte avant gauche est un pilier
de béton, la droite de chair. Tête basse, soudée à une plaque de pierre ; trois yeux vert-acide qui ne s'alignent
pas. Elle charge tête baissée (plan 07 lot B).
"""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, rotation_x, rotation_y, rotation_z, rounded_box, sphere

FRAME_SIZE = (64, 56)
FRAME_PIVOT = (32.0, 47.0)

FLESH, FLESH_DARK, CONCRETE, REBAR, DOOR, WOOD, STONE, MAW, EYE = range(9)
MATERIALS = [
    make_material("flesh", "#5E4A5E"),
    make_material("flesh_dark", "#3A2C3E"),
    make_material("concrete", "#8A8680"),
    make_material("rebar", "#6A4A34", contrast=0.7),
    make_material("door", "#8A3A2E"),
    make_material("wood", "#6E5238"),
    make_material("stone", "#6A6660"),
    make_material("maw", "#221A2A", contrast=0.5),
    make_emissive("eye", "#7FFF00"),
]

K = 2.1
BODY_HEIGHT = 10.0 * K
# Hanches (x côté gauche positif, z avant), épaisseur de patte : l'avant gauche est un pilier de béton.
HIPS = [(5.6 * K, 6.0 * K, 2.4 * K), (-5.4 * K, 6.4 * K, 1.9 * K), (5.0 * K, -6.0 * K, 1.8 * K), (-5.2 * K, -5.6 * K, 1.9 * K)]
# Marche en diagonale : avant gauche et arrière droit ensemble.
GAIT_OFFSETS = [0.0, np.pi, np.pi, 0.0]


@dataclass(frozen=True)
class Brute:
    phase: float = 0.0
    stride: float = 0.0
    lift: float = 0.0
    bob: float = 0.0
    pitch: float = 0.0
    crouch: float = 0.0
    head_drop: float = 0.0
    jaw: float = 0.1
    roll: float = 0.0
    stretch: float = 1.0


def parts(b: Brute) -> list[Part]:
    rotation = rotation_z(b.roll) @ rotation_x(b.pitch)
    center = np.array([0.0, BODY_HEIGHT - b.crouch + b.bob, 0.0])

    def at(local) -> np.ndarray:
        return center + rotation @ np.asarray(local, dtype=np.float64)

    head = at((0.6 * K, -0.4 * K - b.head_drop, 12.0 * K * b.stretch))
    result = [
        # Masse du corps, plus haute aux épaules : une bosse de muscle et de débris.
        Part(lambda p: ellipsoid(p, center, (7.2 * K, 5.8 * K, 10.5 * K * b.stretch), rotation), FLESH),
        Part(lambda p: ellipsoid(p, at((0.4 * K, 2.4 * K, 4.0 * K)), (7.6 * K, 5.6 * K, 6.0 * K), rotation), FLESH),
        Part(lambda p: ellipsoid(p, at((0.0, -2.8 * K, 0.0)), (5.6 * K, 3.0 * K, 9.0 * K * b.stretch), rotation), FLESH_DARK),
        # Dalle de béton enfoncée dans le dos, fers à béton tordus qui en sortent.
        Part(lambda p: rounded_box(p, at((-0.8 * K, 5.6 * K, -1.5 * K)), (4.2 * K, 1.3 * K, 5.0 * K), 0.3 * K,
                                   rotation @ rotation_z(0.25) @ rotation_y(0.3)), CONCRETE),
        Part(lambda p: capsule(p, at((-2.0 * K, 6.4 * K, 1.0 * K)), at((-3.4 * K, 10.0 * K, 2.6 * K)), 0.35 * K), REBAR),
        Part(lambda p: capsule(p, at((0.6 * K, 6.6 * K, -3.0 * K)), at((1.6 * K, 9.6 * K, -5.2 * K)), 0.35 * K), REBAR),
        Part(lambda p: capsule(p, at((1.6 * K, 9.6 * K, -5.2 * K)), at((3.4 * K, 10.2 * K, -4.6 * K)), 0.3 * K), REBAR),
        # Portière de voiture fondue dans le flanc droit, peinture rouge écaillée.
        Part(lambda p: rounded_box(p, at((-6.6 * K, 0.4 * K, -2.0 * K)), (0.6 * K, 3.0 * K, 4.4 * K), 0.4 * K,
                                   rotation @ rotation_z(-0.12)), DOOR),
        # Pied de chaise qui sort de l'épaule gauche.
        Part(lambda p: capsule(p, at((5.4 * K, 4.4 * K, 5.0 * K)), at((8.6 * K, 8.4 * K, 6.4 * K)), 0.5 * K, 0.4 * K), WOOD),
        # Tête basse soudée à une plaque de pierre, gueule en fente.
        Part(lambda p: ellipsoid(p, head, (4.8 * K, 3.8 * K, 4.0 * K), rotation), FLESH_DARK),
        Part(lambda p: ellipsoid(p, head + rotation @ np.array([-0.6 * K, 1.8 * K, 0.4 * K]), (4.6 * K, 1.6 * K, 3.8 * K),
                                 rotation @ rotation_z(0.15)), STONE),
        Part(lambda p: ellipsoid(p, head + rotation @ np.array([0.0, -1.6 * K - b.jaw * 1.2 * K, 2.6 * K]),
                                 (3.0 * K, 0.9 * K + b.jaw * 0.8 * K, 1.4 * K), rotation), MAW),
        Part(lambda p: sphere(p, head + rotation @ np.array([2.2 * K, 0.6 * K, 3.0 * K]), 0.9 * K), EYE),
        Part(lambda p: sphere(p, head + rotation @ np.array([-1.4 * K, 0.2 * K, 3.2 * K]), 0.75 * K), EYE),
        Part(lambda p: sphere(p, head + rotation @ np.array([0.6 * K, -0.2 * K, 3.5 * K]), 0.6 * K), EYE),
    ]
    for index, ((x, z, thickness), offset) in enumerate(zip(HIPS, GAIT_OFFSETS)):
        hip = at((x, -2.0 * K, z * b.stretch))
        cycle = b.phase + offset
        foot = np.array([hip[0] + np.sign(x) * 0.6 * K, max(np.cos(cycle), 0.0) * b.lift, hip[2] + np.sin(cycle) * b.stride])
        knee = (hip + foot) * 0.5 + rotation @ np.array([np.sign(x) * 0.8 * K, 0.0, 1.2 * K])
        material = CONCRETE if index == 0 else FLESH_DARK
        result += [
            Part(lambda p, a=hip, k=knee, t=thickness: capsule(p, a, k, t, t * 0.9), material),
            Part(lambda p, k=knee, f=foot, t=thickness: capsule(p, k, f + np.array([0.0, t * 0.5, 0.0]), t * 0.9, t), material),
        ]
        if index == 0:
            # Fers qui dépassent du pilier de béton.
            result.append(Part(lambda p, k=knee: capsule(p, k + np.array([1.2 * K, 0.0, 0.0]),
                                                         k + np.array([2.8 * K, 1.6 * K, -0.6 * K]), 0.3 * K), REBAR))
    return result


def _animations() -> dict[str, list[Brute]]:
    stand = Brute()
    walk = [replace(stand, phase=i * np.pi / 2, stride=3.0 * K, lift=1.6 * K, bob=0.5 * K * (i % 2), pitch=0.03,
                    roll=(0.03, 0.0, -0.03, 0.0)[i]) for i in range(4)]
    return {
        "idle": [
            stand,
            replace(stand, bob=0.3 * K, jaw=0.25),
            replace(stand, bob=0.5 * K, head_drop=0.3 * K, jaw=0.35),
            replace(stand, bob=0.3 * K, jaw=0.2),
        ],
        "walk": walk,
        # Charge : tête baissée sous la plaque, corps étiré vers l'avant, pattes qui poussent.
        "attack": [
            replace(stand, crouch=1.2 * K, head_drop=1.6 * K, pitch=0.18, jaw=0.2),
            replace(stand, stretch=1.08, head_drop=2.0 * K, pitch=0.24, jaw=0.6, phase=0.0, stride=4.0 * K, lift=1.2 * K),
            replace(stand, stretch=1.1, head_drop=2.0 * K, pitch=0.22, jaw=0.8, phase=np.pi, stride=4.0 * K, lift=1.2 * K),
            replace(stand, crouch=0.6 * K, head_drop=0.8 * K, pitch=0.08, jaw=0.3),
        ],
        # Effondrement sur le flanc : les débris restent, la chair s'affaisse.
        "death": [
            replace(stand, pitch=-0.12, jaw=0.9, bob=0.6 * K),
            replace(stand, roll=0.3, crouch=2.5 * K, jaw=0.7),
            replace(stand, roll=0.7, crouch=4.5 * K, jaw=0.5, head_drop=1.0 * K),
            replace(stand, roll=0.95, crouch=5.8 * K, jaw=0.4, head_drop=1.6 * K),
        ],
    }


ANIMATIONS = _animations()
