"""
Habitante oubliée (plan 16 O6) : une ouvrière en bleu de travail, casque de chantier et gamelle à la main. Les échos
de l'oubli la montrent de loin, en deux tons pâles ; elle n'est pas jouable.
"""
from __future__ import annotations

import numpy as np

from ._body import LimbStyle, limbs
from ..palette import make_material
from ..poses import Gait, character_animations
from ..render import Part
from ..rig import Proportions, Skeleton
from ..sdf import capsule, ellipsoid, rounded_box, sphere

CHARACTER_ID = "habitant_ouvriere"
DIMENSIONS = Proportions(head_radius=4.2)

OVERALLS, SHIRT, BOOTS, SKIN, HELMET, HAIR, BOX, BAND = range(8)
MATERIALS = [
    make_material("overalls", "#3E5A7E"),
    make_material("shirt", "#8A8478"),
    make_material("boots", "#3A2E24"),
    make_material("skin", "#B07E62"),
    make_material("helmet", "#C49B3E"),
    make_material("hair", "#2E2420"),
    make_material("box", "#7A7E84"),
    make_material("band", "#D8D0C0", contrast=0.5),
]


def build(skeleton: Skeleton) -> list[Part]:
    s = skeleton
    torso_center = (s.point("pelvis") + s.point("chest")) * 0.5
    hand = s.point("hand_l")
    parts = [
        Part(lambda p, c=torso_center: ellipsoid(p, c, (5.4, 8.2, 3.8), s.torso), OVERALLS),
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 1.5, 0)), s.on_torso("pelvis", (0, -3.0, 0)), 4.6, 4.8), OVERALLS),
        # Bande réfléchissante à la taille.
        Part(lambda p: np.maximum(ellipsoid(p, torso_center, (5.6, 8.4, 4.0), s.torso),
                                  np.abs((p - s.on_torso("pelvis", (0, 3.5, 0))) @ s.torso[:, 1]) - 0.8), BAND),
        Part(lambda p: sphere(p, s.point("head"), DIMENSIONS.head_radius), SKIN),
        Part(lambda p: capsule(p, s.on_head((0, 0.5, -3.6)), s.on_head((0, -4.0, -4.2)), 1.6, 1.2), HAIR),
        # Casque de chantier : calotte et petite visière.
        Part(lambda p: np.maximum(ellipsoid(p, s.on_head((0, 1.8, 0)), (4.8, 3.6, 5.0), s.head),
                                  -((p - s.on_head((0, 0.9, 0))) @ s.head[:, 1])), HELMET),
        Part(lambda p: rounded_box(p, s.on_head((0, 1.0, 4.4)), (3.0, 0.35, 1.4), 0.2, s.head), HELMET),
        # Gamelle en fer tenue à bout de bras.
        Part(lambda p, h=hand: rounded_box(p, h + [0, -2.4, 0.4], (2.2, 1.6, 1.2), 0.4), BOX),
    ]
    parts += limbs(s, LimbStyle(sleeve=SHIRT, hand=SKIN, leg=OVERALLS, boot=BOOTS, arm_radius=2.0,
                                leg_radius=2.4, boot_radius=2.3, foot_radius=2.1))
    return parts


ANIMATIONS = character_animations(Gait(lean=0.06, arm_out=0.22, stride=1.0, arm_swing=0.9, bounce=0.9))
