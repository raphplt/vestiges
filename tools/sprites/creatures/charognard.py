"""
Le Charognard — comme un chien ou un rat, mais avec trop de pattes et pas assez de tête (Bible §6.2).
Meute nerveuse : il bondit (plan 07). Le chef est le plus gros, il a un œil de plus.
Reprise du 11 octobre (plan 37 M4), deux fois :
- d'abord la couleur : fourrure rouille au lieu d'un brun qui disparaissait sur la terre et l'herbe ;
- puis la forme (« le charognard pourrait être mieux ») : il restait une patate sur des piquets. Silhouette de
  hyène galeuse, garrot haut et croupe basse, taille creusée où saillent des côtes pâles, échine d'os en crête,
  plaques de peau nue. Pas de crâne : le cou finit en une gueule ouverte, crocs d'os en haut et en bas, les yeux
  vert-acide plantés de travers sur le cou. Six pattes grêles aux genoux relevés comme un insecte, griffes pâles,
  longue queue nue.
Plan 54 S3 : garrot, taille, croupe et cou fondus en une seule échine (`smin`), os et crocs qui accrochent la
lumière ; trot heurté (poussée, suspension, réception tassée) et morsure qui claque à la réception du bond.
"""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, rotation_x, rotation_z, smin, sphere

FRAME_SIZE = (40, 36)
FRAME_PIVOT = (20.0, 27.0)

FUR, FUR_DARK, SKIN, BONE, LEG, MAW, EYE = range(7)
MATERIALS = [
    make_material("fur", "#8A5236", contrast=1.12),
    make_material("fur_dark", "#4C2C28", contrast=1.12),
    make_material("skin", "#B8968A", contrast=1.05),
    make_material("bone", "#E4D8BE", contrast=0.75, specular=0.6),
    make_material("leg", "#2E2333"),
    make_material("maw", "#2A1830", contrast=0.5),
    make_emissive("eye", "#7FFF00"),
]

K = 1.55
BODY_HEIGHT = 9.0 * K
# Hanches (x côté gauche positif, z avant), longueur relative : aucune paire n'est symétrique.
HIPS = [
    (3.6 * K, 5.6 * K, 1.05), (-3.8 * K, 5.0 * K, 0.95),
    (3.0 * K, 0.2 * K, 1.0), (-3.0 * K, 0.6 * K, 1.1),
    (3.4 * K, -6.2 * K, 0.9), (-3.5 * K, -5.8 * K, 1.0),
]
# Marche en trépied : avant gauche, milieu droit et arrière gauche ensemble.
GAIT_OFFSETS = [0.0, np.pi, np.pi, 0.0, 0.0, np.pi]
# Crête d'échine : position le long du dos (z) et hauteur au-dessus du centre, du garrot à la croupe.
SPINE = [(6.6, 5.4), (4.4, 5.6), (2.2, 4.6), (0.0, 3.8), (-2.2, 3.6), (-4.4, 3.8), (-6.6, 3.6)]


@dataclass(frozen=True)
class Beast:
    phase: float = 0.0
    stride: float = 0.0
    lift: float = 0.0
    bob: float = 0.0
    pitch: float = 0.0
    crouch: float = 0.0
    stretch: float = 1.0
    jaw: float = 0.55
    roll: float = 0.0
    tuck: float = 0.0
    tail: float = 0.0
    nod: float = 0.0


def parts(b: Beast) -> list[Part]:
    rotation = rotation_z(b.roll) @ rotation_x(b.pitch)
    center = np.array([0.0, BODY_HEIGHT - b.crouch + b.bob, 0.0])

    def at(local) -> np.ndarray:
        x, y, z = local
        return center + rotation @ np.array([x, y, z * b.stretch], dtype=np.float64)

    head = at((0.6 * K, (0.2 - b.nod) * K, 11.4 * K))
    head_rotation = rotation @ rotation_x(0.25 + 0.3 * b.nod)
    jaw_rotation = head_rotation @ rotation_x(0.35 + 0.55 * b.jaw)
    result = [
        # Garrot haut et massif, taille creusée, croupe plus basse : la pente d'une hyène, d'un seul tenant avec le cou.
        Part(lambda p: smin(smin(smin(
            ellipsoid(p, at((0.0, 1.6 * K, 4.6 * K)), (4.4 * K, 4.6 * K, 4.8 * K), rotation),
            ellipsoid(p, at((0.0, 0.2 * K, -0.8 * K)), (3.0 * K, 3.0 * K, 4.6 * K), rotation), 0.7 * K),
            ellipsoid(p, at((0.0, -0.2 * K, -6.0 * K)), (3.8 * K, 3.6 * K, 4.0 * K), rotation), 0.7 * K),
            capsule(p, at((0.2 * K, 2.2 * K, 7.4 * K)), head, 3.2 * K, 2.4 * K), 0.6 * K), FUR),
        Part(lambda p: ellipsoid(p, at((0.0, -2.0 * K, 1.0 * K)), (2.8 * K, 1.8 * K, 6.4 * K), rotation), FUR_DARK),
        # Plaques de peau nue, pelée par la gale.
        Part(lambda p: ellipsoid(p, at((2.6 * K, 0.4 * K, -6.4 * K)), (1.6 * K, 2.0 * K, 2.2 * K), rotation), SKIN),
        Part(lambda p: ellipsoid(p, at((-3.4 * K, 2.2 * K, 4.8 * K)), (1.4 * K, 1.8 * K, 1.8 * K), rotation), SKIN),
        # Pas de crâne : le cou finit en museau fendu, la gueule béante, la mâchoire qui pend.
        Part(lambda p: ellipsoid(p, head + head_rotation @ np.array([0.0, 0.7 * K, 1.8 * K]), (2.6 * K, 1.3 * K, 3.0 * K),
                                 head_rotation), FUR_DARK),
        Part(lambda p: ellipsoid(p, head + head_rotation @ np.array([0.0, -0.5 * K, 2.4 * K]), (2.1 * K, 1.3 * K, 2.6 * K),
                                 head_rotation), MAW),
        Part(lambda p: ellipsoid(p, head + jaw_rotation @ np.array([0.0, -1.1 * K, 2.4 * K]), (2.3 * K, 0.75 * K, 3.0 * K),
                                 jaw_rotation), SKIN),
        # Yeux plantés de travers sur le cou, à des hauteurs différentes.
        Part(lambda p: sphere(p, at((2.5 * K, 2.6 * K, 9.4 * K)), 1.1 * K), EYE),
        Part(lambda p: sphere(p, at((-2.0 * K, 1.6 * K, 10.0 * K)), 0.9 * K), EYE),
    ]
    # Crocs : trois en haut, deux en bas, assez épais pour tenir un pixel ; ils encadrent la gueule ouverte.
    for side in (-1.3, 0.0, 1.3):
        upper = head + head_rotation @ np.array([side * K, -0.1 * K, 4.2 * K - abs(side) * 0.6 * K])
        result.append(Part(lambda p, a=upper: capsule(p, a, a + head_rotation @ np.array([0.0, -1.5 * K, 0.3 * K]),
                                                      0.7 * K, 0.3 * K), BONE))
    for side in (-0.9, 0.9):
        lower = head + jaw_rotation @ np.array([side * K, -0.8 * K, 4.6 * K])
        result.append(Part(lambda p, a=lower: capsule(p, a, a + jaw_rotation @ np.array([0.0, 1.4 * K, 0.3 * K]),
                                                      0.65 * K, 0.28 * K), BONE))
    # Échine d'os en crête et côtes pâles sur les flancs creusés.
    for z, y in SPINE:
        result.append(Part(lambda p, c=at((0.2 * K, y * K, z * K)): ellipsoid(p, c, (1.1 * K, 1.0 * K, 0.9 * K), rotation), BONE))
    for z in (-2.6, -0.8, 1.0):
        for side in (1.0, -1.0):
            top = at((side * 2.6 * K, 1.6 * K, z * K))
            bottom = at((side * 2.7 * K, -1.4 * K, (z + 0.6) * K))
            result.append(Part(lambda p, a=top, c=bottom: capsule(p, a, c, 0.5 * K, 0.4 * K), BONE))
    # Longue queue nue, en deux tronçons, qui fouette.
    root = at((0.0, 0.8 * K, -9.6 * K))
    mid = root + rotation @ np.array([b.tail * 2.2 * K, 0.8 * K, -3.4 * K])
    tip = mid + rotation @ np.array([b.tail * 3.4 * K, -0.6 * K, -2.8 * K])
    result += [
        Part(lambda p: capsule(p, root, mid, 1.0 * K, 0.7 * K), SKIN),
        Part(lambda p: capsule(p, mid, tip, 0.7 * K, 0.35 * K), SKIN),
    ]
    for (x, z, length), offset in zip(HIPS, GAIT_OFFSETS):
        hip = at((x, -1.0 * K, z))
        side = np.sign(x)
        cycle = b.phase + offset
        foot = np.array([hip[0] + side * 3.4 * K, max(np.cos(cycle), 0.0) * b.lift,
                         hip[2] + np.sin(cycle) * b.stride])
        curled = hip + rotation @ np.array([side * 2.0 * K, -3.0 * K, 1.5 * K])
        foot = foot + (curled - foot) * b.tuck
        reach = foot - hip
        # Genou relevé au-dessus de la ligne du corps : une démarche d'insecte plus que de chien.
        knee = hip + reach * 0.4 + rotation @ np.array([side * 2.2 * K, 2.4 * K * length, 0.0])
        foot = hip + reach * length
        claw = foot + np.array([side * 0.4 * K, -0.2 * K, 1.3 * K])
        result += [
            Part(lambda p, a=hip, k=knee: capsule(p, a, k, 0.95 * K, 0.75 * K), LEG),
            Part(lambda p, k=knee, f=foot: capsule(p, k, f, 0.7 * K, 0.5 * K), LEG),
            Part(lambda p, f=foot, c=claw: capsule(p, f, c, 0.5 * K, 0.25 * K), BONE),
        ]
    return result


def _animations() -> dict[str, list[Beast]]:
    stand = Beast()
    # Trot heurté : poussée nez bas, suspension étirée, réception tassée, reprise. La tête suit avec un temps de
    # retard et la queue fouette à contretemps.
    beats = [
        dict(bob=0.2 * K, pitch=0.1, stretch=1.04, nod=0.1, jaw=0.4, tail=-1.0),
        dict(bob=1.0 * K, pitch=-0.05, stretch=1.08, nod=-0.1, jaw=0.5, tail=-0.3),
        dict(bob=-0.2 * K, crouch=0.6 * K, pitch=0.13, stretch=0.95, nod=0.45, jaw=0.3, tail=1.0),
        dict(bob=0.45 * K, pitch=0.04, stretch=1.0, nod=0.3, jaw=0.45, tail=0.3),
    ]
    walk = [replace(stand, phase=i * np.pi / 2, stride=3.6 * K, lift=2.6 * K, **beat) for i, beat in enumerate(beats)]
    return {
        # Nerveux à l'arrêt : la gueule claque, la tête se baisse, la queue fouette.
        "idle": [
            stand,
            replace(stand, bob=0.3, tail=0.6, jaw=0.7, nod=0.2),
            replace(stand, bob=0.5, pitch=-0.06, tail=1.0, jaw=0.1, nod=-0.1),
            replace(stand, bob=0.3, tail=0.3, jaw=0.8, nod=0.35),
        ],
        "walk": walk,
        # Bond, joué au départ du saut (l'annonce est la posture accroupie du jeu) : détente étirée, vol gueule
        # grande ouverte, réception où la gueule claque, puis reprise.
        "attack": [
            replace(stand, stretch=1.16, pitch=-0.3, bob=2.6 * K, tuck=0.2, jaw=0.9, tail=-1.0, nod=-0.45),
            replace(stand, stretch=1.1, pitch=-0.05, tuck=0.6, bob=3.6 * K, jaw=1.3, tail=-0.6, nod=-0.25),
            replace(stand, crouch=2.2 * K, stretch=0.92, pitch=0.28, jaw=0.0, stride=1.2 * K, nod=0.6, tail=0.8),
            replace(stand, crouch=0.9 * K, pitch=0.08, jaw=0.35, nod=0.2, tail=0.3),
        ],
        # Renversé sur le flanc, pattes repliées.
        "death": [
            replace(stand, pitch=-0.3, jaw=1.2, bob=1.2 * K, tail=1.0, nod=-0.5),
            replace(stand, roll=0.6, crouch=2.0 * K, tuck=0.4, jaw=0.8),
            replace(stand, roll=1.15, crouch=3.5 * K, tuck=0.8, jaw=0.6),
            replace(stand, roll=1.45, crouch=4.4 * K, tuck=1.0, jaw=0.5),
        ],
    }


ANIMATIONS = _animations()
