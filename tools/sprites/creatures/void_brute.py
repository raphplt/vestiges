"""
La Brute du Vide — un amas de chair, de pierre et de métal fusionnés (Bible §6.2).
Quadrupède massif et bas : son corps a absorbé des débris du monde. Une portière rouillée sur le flanc, une dalle de
béton hérissée de ferraille sur le dos, un pied de chaise qui sort de l'épaule. La patte avant gauche est un pilier
de béton, la droite de chair. Tête basse, soudée à une plaque de pierre ; trois yeux vert-acide qui ne s'alignent
pas. Elle charge tête baissée (plan 07 lot B).
Reprise du 11 octobre (plan 37 M4) : chair violacée uniforme, elle se lisait comme un ours violet. Chair à vif
couturée, débris plus clairs et plus saillants : dalle de béton pâle, fers à béton rouillés plus épais et plus
nombreux, portière rouge.
Plan 54 S3 : bosse d'épaules fondue dans la masse (`smin`), métal et pierre qui accrochent la lumière ; marche lourde
et boiteuse (le pilier de béton écrase le sol, le corps roule de son côté), charge qui part d'une poussée brutale.
"""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, rotation_x, rotation_y, rotation_z, rounded_box, smin, sphere

FRAME_SIZE = (64, 56)
FRAME_PIVOT = (32.0, 47.0)

FLESH, FLESH_DARK, CONCRETE, REBAR, DOOR, WOOD, STONE, MAW, EYE = range(9)
MATERIALS = [
    make_material("flesh", "#7A5258", contrast=1.1),
    make_material("flesh_dark", "#3E2830", contrast=1.1),
    make_material("concrete", "#ABA79E", contrast=1.12),
    make_material("rebar", "#A0603A", contrast=0.85, specular=0.5),
    make_material("door", "#A8402E", contrast=1.1, specular=0.4),
    make_material("wood", "#94704A", contrast=1.05),
    make_material("stone", "#8E8A82", contrast=1.15, specular=0.3),
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
        Part(lambda p: smin(ellipsoid(p, center, (7.2 * K, 5.8 * K, 10.5 * K * b.stretch), rotation),
                            ellipsoid(p, at((0.4 * K, 2.4 * K, 4.0 * K)), (7.6 * K, 5.6 * K, 6.0 * K), rotation), 1.2 * K),
             FLESH),
        Part(lambda p: ellipsoid(p, at((0.0, -2.8 * K, 0.0)), (5.6 * K, 3.0 * K, 9.0 * K * b.stretch), rotation), FLESH_DARK),
        # Dalle de béton enfoncée dans le dos, fers à béton tordus qui en sortent.
        Part(lambda p: rounded_box(p, at((-0.8 * K, 5.6 * K, -1.5 * K)), (4.2 * K, 1.3 * K, 5.0 * K), 0.3 * K,
                                   rotation @ rotation_z(0.25) @ rotation_y(0.3)), CONCRETE),
        Part(lambda p: capsule(p, at((-2.0 * K, 6.4 * K, 1.0 * K)), at((-3.6 * K, 10.6 * K, 2.8 * K)), 0.5 * K), REBAR),
        Part(lambda p: capsule(p, at((0.6 * K, 6.6 * K, -3.0 * K)), at((1.6 * K, 10.0 * K, -5.2 * K)), 0.5 * K), REBAR),
        Part(lambda p: capsule(p, at((1.6 * K, 10.0 * K, -5.2 * K)), at((3.6 * K, 10.6 * K, -4.6 * K)), 0.45 * K), REBAR),
        Part(lambda p: capsule(p, at((-3.4 * K, 6.2 * K, -4.0 * K)), at((-5.0 * K, 9.4 * K, -6.0 * K)), 0.45 * K), REBAR),
        Part(lambda p: capsule(p, at((2.4 * K, 6.8 * K, 1.6 * K)), at((3.0 * K, 10.4 * K, 3.4 * K)), 0.45 * K), REBAR),
        # Coutures sombres là où la chair a avalé les débris.
        Part(lambda p: ellipsoid(p, at((3.8 * K, 1.6 * K, 2.0 * K)), (0.5 * K, 3.4 * K, 0.6 * K), rotation @ rotation_x(0.6)), FLESH_DARK),
        Part(lambda p: ellipsoid(p, at((-3.2 * K, 3.2 * K, -4.6 * K)), (0.5 * K, 2.6 * K, 0.6 * K), rotation @ rotation_z(0.5)), FLESH_DARK),
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
    # Marche lourde : le pilier de béton (avant gauche) retombe sur le premier temps, le corps s'écrase et roule de
    # son côté ; la patte de chair reprend plus vite, la tête suit en retard.
    beats = [dict(bob=-0.35 * K, crouch=0.3 * K, pitch=0.06, roll=0.06, head_drop=0.5 * K, jaw=0.2),
             dict(bob=0.35 * K, pitch=0.02, roll=0.02, head_drop=0.2 * K, jaw=0.1),
             dict(bob=0.0, pitch=0.04, roll=-0.03, head_drop=0.3 * K, jaw=0.15),
             dict(bob=0.55 * K, pitch=0.0, roll=0.0, head_drop=0.0, jaw=0.1)]
    walk = [replace(stand, phase=i * np.pi / 2, stride=3.0 * K, lift=1.6 * K, **beat) for i, beat in enumerate(beats)]
    return {
        # Souffle de bête : les épaules se soulèvent, la gueule bave, la tête se balance.
        "idle": [
            stand,
            replace(stand, bob=0.3 * K, jaw=0.3, roll=0.02),
            replace(stand, bob=0.55 * K, head_drop=0.4 * K, jaw=0.4, pitch=-0.03),
            replace(stand, bob=0.2 * K, jaw=0.15, roll=-0.02),
        ],
        "walk": walk,
        # Charge, jouée au départ (l'annonce est la posture accroupie du jeu) : poussée brutale tête sous la plaque,
        # deux foulées étirées, puis le corps se redresse.
        "attack": [
            replace(stand, stretch=1.12, crouch=0.8 * K, head_drop=2.2 * K, pitch=0.26, jaw=0.4, phase=np.pi / 2,
                    stride=4.4 * K, lift=0.6 * K),
            replace(stand, stretch=1.1, head_drop=2.0 * K, pitch=0.22, jaw=0.7, phase=0.0, stride=4.0 * K, lift=1.4 * K,
                    bob=0.4 * K),
            replace(stand, stretch=1.1, head_drop=2.0 * K, pitch=0.24, jaw=0.9, phase=np.pi, stride=4.0 * K, lift=1.4 * K),
            replace(stand, crouch=0.6 * K, head_drop=0.6 * K, pitch=0.04, jaw=0.3),
        ],
        # Effondrement sur le flanc : les débris restent, la chair s'affaisse.
        "death": [
            replace(stand, pitch=-0.16, jaw=1.0, bob=0.8 * K),
            replace(stand, roll=0.3, crouch=2.5 * K, jaw=0.7),
            replace(stand, roll=0.7, crouch=4.5 * K, jaw=0.5, head_drop=1.0 * K),
            replace(stand, roll=0.95, crouch=5.8 * K, jaw=0.4, head_drop=1.6 * K),
        ],
    }


ANIMATIONS = _animations()
