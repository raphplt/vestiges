"""
Le Facteur sans destination — « Il y a forcément quelqu'un qui attend ces lettres. »
Casquette de facteur, sacoche en bandoulière qui déborde de lettres, patins bricolés aux pieds, posture de patineur.
Bleu postal délavé dominant, jaune de sacoche en accent.
"""
from __future__ import annotations

import numpy as np

from ._body import LimbStyle, limbs
from ..palette import make_material
from ..poses import Gait, character_animations
from ..render import Part
from ..rig import Proportions, Skeleton
from ..sdf import capsule, ellipsoid, rotation_z, rounded_box, sphere

CHARACTER_ID = "facteur"
DIMENSIONS = Proportions(spine=14.0, head_radius=4.2)

JACKET, TROUSERS, SHOES, SKIN, CAP, VISOR, BAG, LETTER, STRAP, WHEEL, BOARD = range(11)
MATERIALS = [
    make_material("jacket", "#4E6A8C"),
    make_material("trousers", "#3E4A5A"),
    make_material("shoes", "#3A3230"),
    make_material("skin", "#B8866A"),
    make_material("cap", "#3E5676"),
    make_material("visor", "#262A36", contrast=0.5),
    make_material("bag", "#C9A54A"),
    make_material("letter", "#E8E0D4", contrast=0.5),
    make_material("strap", "#6A5230"),
    make_material("wheel", "#6B6161"),
    make_material("board", "#8A6A42"),
]


def build(skeleton: Skeleton) -> list[Part]:
    s = skeleton
    torso_center = (s.point("pelvis") + s.point("chest")) * 0.5
    parts = [
        Part(lambda p, c=torso_center: ellipsoid(p, c, (5.6, 8.4, 3.9), s.torso), JACKET),
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 1.5, 0)), s.on_torso("pelvis", (0, -3.0, 0)), 4.6, 4.8), JACKET),
        Part(lambda p: sphere(p, s.point("head"), DIMENSIONS.head_radius), SKIN),
        # Casquette : calotte aplatie et visière.
        Part(lambda p: np.maximum(ellipsoid(p, s.on_head((0, 2.0, -0.3)), (4.6, 2.8, 4.8), s.head),
                                  -(s.head @ [0, 1, 0] @ (p - s.on_head((0, 0.8, 0))).T)), CAP),
        Part(lambda p: rounded_box(p, s.on_head((0, 1.4, 4.3)), (3.4, 0.45, 1.9), 0.3, s.head), VISOR),
        # Sacoche en bandoulière sur la hanche gauche, lettres qui dépassent.
        Part(lambda p: capsule(p, s.on_torso("chest", (-4.6, 0.5, 2.6)), s.on_torso("pelvis", (4.8, 2.0, 3.2)), 0.8), STRAP),
        # Sacoche débordante sur la hanche, liasse qui dépasse et lettres qui s'envolent derrière lui.
        Part(lambda p: rounded_box(p, s.on_torso("pelvis", (7.0, 1.0, 0.6)), (2.6, 4.6, 5.0), 1.2, s.torso), BAG),
        Part(lambda p: rounded_box(p, s.on_torso("pelvis", (7.0, 6.2, 1.6)), (0.4, 2.2, 2.6), 0.1, s.torso), LETTER),
        Part(lambda p: rounded_box(p, s.on_torso("pelvis", (7.4, 5.8, -1.6)), (0.4, 2.0, 2.2), 0.1, s.torso), LETTER),
        Part(lambda p: rounded_box(p, s.on_torso("chest", (9.5, 4.0, -8.0)), (0.3, 1.6, 2.2), 0.1,
                                   s.torso @ rotation_z(0.6)), LETTER),
        Part(lambda p: rounded_box(p, s.on_torso("chest", (6.0, 10.0, -11.0)), (0.3, 1.4, 2.0), 0.1,
                                   s.torso @ rotation_z(-0.5)), LETTER),
    ]
    parts += limbs(s, LimbStyle(sleeve=JACKET, hand=SKIN, leg=TROUSERS, boot=SHOES, arm_radius=1.9,
                                leg_radius=2.2, boot_radius=2.1, boot_height=3.5, foot_radius=1.9))
    # Patins bricolés : planchette et deux roues sous chaque pied.
    for side in ("l", "r"):
        parts += [
            Part(lambda p, side=side: capsule(p, s.point(f"ankle_{side}") + [0, -2.4, -1.5],
                                              s.point(f"toe_{side}") + [0, -1.6, 0.5], 1.0), BOARD),
            Part(lambda p, side=side: sphere(p, s.point(f"ankle_{side}") + [0, -3.8, -1.6], 1.6), WHEEL),
            Part(lambda p, side=side: sphere(p, s.point(f"toe_{side}") + [0, -3.0, 0.4], 1.6), WHEEL),
        ]
    return parts


ANIMATIONS = character_animations(Gait(lean=0.28, arm_out=0.3, stride=0.8, arm_swing=1.2, bounce=0.5))
