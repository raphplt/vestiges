"""
Habitant oublié (plan 16 O6) : un vieil homme voûté, chapeau de feutre, pardessus et canne. Les échos de l'oubli le
montrent de loin, en deux tons pâles ; il n'est pas jouable. Pas lent, dos courbé.
"""
from __future__ import annotations

import numpy as np

from ._body import LimbStyle, limbs
from ..palette import make_material
from ..poses import Gait, character_animations
from ..render import Part
from ..rig import Proportions, Skeleton
from ..sdf import capsule, cylinder, ellipsoid, sphere

CHARACTER_ID = "habitant_vieil_homme"
DIMENSIONS = Proportions(spine=13.0, head_radius=4.1, shoulder_half=6.0)

COAT, TROUSERS, SHOES, SKIN, HAT, BAND, CANE, SCARF = range(8)
MATERIALS = [
    make_material("coat", "#5E5A50"),
    make_material("trousers", "#44403A"),
    make_material("shoes", "#2E2622"),
    make_material("skin", "#C0927A"),
    make_material("hat", "#4A3E32"),
    make_material("band", "#2A2420", contrast=0.6),
    make_material("cane", "#6A4A30"),
    make_material("scarf", "#7A4A3A"),
]


def build(skeleton: Skeleton) -> list[Part]:
    s = skeleton
    torso_center = (s.point("pelvis") + s.point("chest")) * 0.5
    hand = s.point("hand_r")
    parts = [
        Part(lambda p, c=torso_center: ellipsoid(p, c, (5.8, 8.0, 4.2), s.torso), COAT),
        # Pardessus long, jusqu'aux genoux.
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 1.5, 0)), s.on_torso("pelvis", (0, -8.5, -0.4)), 5.0, 5.6), COAT),
        Part(lambda p: ellipsoid(p, s.on_torso("neck", (0, -0.4, 0.6)), (3.8, 2.0, 3.6), s.torso), SCARF),
        Part(lambda p: sphere(p, s.point("head"), DIMENSIONS.head_radius), SKIN),
        # Chapeau de feutre : bord large et calotte creusée.
        Part(lambda p: cylinder(p, s.on_head((0, 2.2, 0)), 6.2, 0.35, 0.2, s.head), HAT),
        Part(lambda p: np.maximum(ellipsoid(p, s.on_head((0, 4.0, 0)), (3.8, 2.6, 3.8), s.head),
                                  -ellipsoid(p, s.on_head((0, 6.4, 0)), (1.6, 1.0, 3.0), s.head)), HAT),
        Part(lambda p: cylinder(p, s.on_head((0, 2.9, 0)), 3.9, 0.5, 0.1, s.head), BAND),
        # Canne tenue dans la main droite, plantée devant lui.
        Part(lambda p, h=hand: capsule(p, h + [0, 1.0, 0], h + [0.8, -16.0, 2.5], 0.6), CANE),
    ]
    parts += limbs(s, LimbStyle(sleeve=COAT, hand=SKIN, leg=TROUSERS, boot=SHOES, arm_radius=2.0,
                                leg_radius=2.2, boot_radius=2.1, boot_height=3.0, foot_radius=2.0))
    return parts


ANIMATIONS = character_animations(Gait(lean=0.14, arm_out=0.18, stride=0.55, arm_swing=0.5, bounce=0.4, heavy=0.25))
