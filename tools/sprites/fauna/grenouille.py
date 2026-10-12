"""
Grenouille (plan 39 B2) : marais. Assise en coin, avant relevé ; tête large et plate, yeux saillants dorés à pupille,
pattes arrière repliées en Z sur les flancs, dos olive taché de brun entre deux bourrelets clairs, gorge pâle qui
palpite. À l'approche, elle bondit corps tendu, pattes arrière droites derrière elle, puis plonge.
"""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from ..palette import make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, rotation_x, sphere

FRAME_SIZE = (60, 44)
FRAME_PIVOT = (30.0, 34.0)
SCALE = 2.0

SKIN, SKIN_DARK, SPOT, RIDGE, BELLY, IRIS, PUPIL = range(7)
MATERIALS = [
    make_material("skin", "#627D3A"),
    make_material("skin_dark", "#3D5029"),
    make_material("spot", "#4B3B22", contrast=0.6),
    make_material("ridge", "#93A157", contrast=0.6),
    make_material("belly", "#C8BC82", contrast=0.6),
    make_material("iris", "#D6A93A", contrast=0.5),
    make_material("pupil", "#16140F", contrast=0.3),
]

# Taches du dos, en repère du corps (x, y, z) et rayon : asymétriques, comme sur une vraie peau.
SPOTS = (
    ((0.9, 2.1, 0.9), 0.8),
    ((-1.3, 2.0, -0.6), 0.9),
    ((0.5, 1.9, -2.2), 0.75),
    ((-0.4, 2.2, 1.9), 0.6),
    ((1.9, 1.3, -1.4), 0.6),
)


@dataclass(frozen=True)
class Frog:
    # Hauteur du corps au-dessus du sol, inclinaison (négative : avant relevé), détente des pattes de 0 (repliées)
    # à 1 (tendues derrière), gorge gonflée, paupières closes.
    lift: float = 0.0
    pitch: float = -0.42
    extend: float = 0.0
    reach: float = 0.0
    throat: float = 0.0
    blink: bool = False


def _lerp(a, b, t: float) -> np.ndarray:
    return np.asarray(a, dtype=np.float64) * (1.0 - t) + np.asarray(b, dtype=np.float64) * t


def parts(f: Frog) -> list[Part]:
    rotation = rotation_x(f.pitch)
    center = np.array([0.0, 3.0 + f.lift, -0.4])

    def body(local) -> np.ndarray:
        return center + rotation @ np.asarray(local, dtype=np.float64)

    head = body((0.0, 1.0, 3.6))
    result = [
        Part(lambda p: ellipsoid(p, center, (3.4, 2.3, 4.2), rotation), SKIN),
        # Tête large et plate, museau arrondi.
        Part(lambda p: ellipsoid(p, head, (3.3, 1.7, 2.6), rotation), SKIN),
        Part(lambda p: ellipsoid(p, body((0.0, -1.4 - f.throat * 0.35, 3.6)), (2.0, 0.9 + f.throat * 0.5, 1.9), rotation), BELLY),
        Part(lambda p: ellipsoid(p, body((0.0, -1.6, 0.2)), (2.8, 1.0, 3.2), rotation), BELLY),
    ]
    # Bouche : une fente sombre du museau jusque sous l'œil.
    for side in (1.0, -1.0):
        result.append(Part(lambda p, s=side: capsule(p, body((s * 0.6, 0.35, 6.1)), body((s * 2.9, 0.55, 3.4)), 0.32),
                           SKIN_DARK, relief=False))
    # Bourrelets clairs le long du dos, taches brunes entre eux.
    for side in (1.0, -1.0):
        result.append(Part(lambda p, s=side: capsule(p, body((s * 2.0, 2.0, 2.4)), body((s * 2.3, 1.6, -3.0)), 0.42),
                           RIDGE, relief=False))
    for local, radius in SPOTS:
        result.append(Part(lambda p, c=body(local), r=radius: sphere(p, c, r), SPOT, relief=False))
    # Yeux saillants : bosse de peau, iris doré, pupille vers l'avant et le dehors ; paupière close au clignement.
    for side in (1.0, -1.0):
        bulge = body((side * 2.2, 2.3, 4.0))
        result.append(Part(lambda p, c=bulge: sphere(p, c, 1.45), SKIN))
        # L'œil regarde de côté et un peu devant : de face comme de profil, un anneau doré autour d'une pupille noire.
        eye = bulge + rotation @ np.array([side * 0.55, 0.35, 0.45])
        if f.blink:
            result.append(Part(lambda p, c=eye: sphere(p, c, 1.15), SKIN_DARK))
        else:
            result.append(Part(lambda p, c=eye: sphere(p, c, 1.15), IRIS))
            result.append(Part(lambda p, c=eye + rotation @ np.array([side * 0.75, 0.15, 0.6]): ellipsoid(p, c, (0.55, 0.42, 0.75)),
                               PUPIL, relief=False))
    for side in (1.0, -1.0):
        result += _hind_leg(side, f, body)
        result += _front_leg(side, f, body)
    return result


def _hind_leg(side: float, f: Frog, body) -> list[Part]:
    """Repliée en Z sur le flanc (cuisse vers l'avant, jambe vers l'arrière, pied vers l'avant), tendue derrière au saut."""
    hip = body((side * 2.6, -0.4, -2.8))
    folded_knee = np.array([side * 4.6, 1.3, 0.4])
    folded_ankle = np.array([side * 3.9, 0.5, -3.6])
    folded_toe = np.array([side * 5.0, 0.2, -0.2])
    stretched_knee = hip + np.array([side * 1.6, -0.6, -3.6])
    stretched_ankle = stretched_knee + np.array([side * 0.4, -0.4, -3.8])
    stretched_toe = stretched_ankle + np.array([side * 0.6, -0.2, -2.8])
    lift = np.array([0.0, f.lift, 0.0])
    knee = _lerp(folded_knee + lift, stretched_knee, f.extend)
    ankle = _lerp(folded_ankle + lift * 0.5, stretched_ankle, f.extend)
    toe = _lerp(folded_toe + lift * 0.3, stretched_toe, f.extend)
    for joint in (knee, ankle, toe):
        joint[1] = max(joint[1], 0.25)
    web = (toe - ankle) / max(np.linalg.norm(toe - ankle), 0.001)
    return [
        Part(lambda p, a=hip, b=knee: capsule(p, a, b, 1.5, 1.0), SKIN),
        Part(lambda p, a=knee, b=ankle: capsule(p, a, b, 0.85, 0.6), SKIN_DARK),
        Part(lambda p, a=ankle, b=toe: capsule(p, a, b, 0.5, 0.45), SKIN_DARK),
        # Pied palmé : une palette plate au bout des orteils.
        Part(lambda p, c=toe - web * 0.6: ellipsoid(p, c, (1.1, 0.35, 1.1)), SKIN_DARK),
    ]


def _front_leg(side: float, f: Frog, body) -> list[Part]:
    """Bras courts et droits, mains écartées au sol ; tendus vers l'avant pendant le saut."""
    shoulder = body((side * 2.3, -0.6, 2.4))
    planted = np.array([side * 2.9, 0.25, 4.2])
    reached = shoulder + np.array([side * 0.6, -0.6, 3.2])
    hand = _lerp(planted + np.array([0.0, f.lift * 0.4, 0.0]), reached, f.reach)
    hand[1] = max(hand[1], 0.25)
    return [
        Part(lambda p, a=shoulder, b=hand: capsule(p, a, b, 0.7, 0.5), SKIN),
        Part(lambda p, c=hand + np.array([side * 0.3, 0.0, 0.4]): ellipsoid(p, c, (0.9, 0.3, 0.7)), SKIN_DARK),
    ]


def _animations() -> dict[str, list[Frog]]:
    sit = Frog()
    return {
        # Un cycle de 1,5 s à 8 poses par seconde : la gorge palpite deux fois, les yeux clignent une fois.
        "idle": [sit, sit, sit, replace(sit, throat=1.0), replace(sit, throat=0.6), sit, sit, sit,
                 replace(sit, throat=1.0), sit, replace(sit, blink=True), sit],
        # Accroupie, détente, en l'air corps tendu, plongeon avant relevé vers le bas.
        "jump": [
            replace(sit, lift=-0.5, pitch=-0.55),
            replace(sit, lift=2.4, pitch=-0.3, extend=0.7, reach=0.6),
            replace(sit, lift=4.2, pitch=-0.05, extend=1.0, reach=1.0),
            replace(sit, lift=2.6, pitch=0.3, extend=1.0, reach=1.0),
            replace(sit, lift=0.6, pitch=0.45, extend=0.8, reach=1.0),
        ],
    }


ANIMATIONS = _animations()
