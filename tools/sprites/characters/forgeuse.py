"""
La Forgeuse — « Tout ce qui est tordu peut se redresser. Même le monde. »
La silhouette la plus large : trapue, tablier de cuir, lunettes de soudure sur le front, marteau trop gros sur l'épaule.
Gris acier dominant, rouge forge en accent (charte §4).
"""
from __future__ import annotations

import numpy as np

from ._body import LimbStyle, limbs
from ..palette import make_material
from ..poses import Gait, character_animations
from ..render import Part
from ..rig import Proportions, Skeleton
from ..sdf import capsule, ellipsoid, rounded_box, sphere

CHARACTER_ID = "forgeuse"
DIMENSIONS = Proportions(shin=10.5, thigh=11.5, spine=13.5, head_radius=4.4, shoulder_half=7.8, hip_half=4.0,
                         upper_arm=9.0, forearm=8.0)

SHIRT, APRON, LEGS, BOOTS, SKIN, HAIR, GOGGLES, LENS, HANDLE, HAMMER, GLOVES, EMBER = range(12)
MATERIALS = [
    make_material("shirt", "#6A6A7A"),
    make_material("apron", "#5E4430"),
    make_material("legs", "#4A4A52"),
    make_material("boots", "#35302C"),
    make_material("skin", "#B07A5A"),
    make_material("hair", "#3A2A26"),
    make_material("goggles", "#4A4A52"),
    make_material("lens", "#C44A3A"),
    make_material("handle", "#7A5A3A"),
    make_material("hammer", "#6A6A7A"),
    make_material("gloves", "#4A3528"),
    make_material("ember", "#E07B39"),
]


def build(skeleton: Skeleton) -> list[Part]:
    s = skeleton
    # Braise qui palpite dans la masse et marteau qui se cale sur l'épaule à chaque souffle.
    d = s.drape
    torso_center = (s.point("pelvis") + s.point("chest")) * 0.5
    parts = [
        Part(lambda p, c=torso_center: ellipsoid(p, c, (7.4, 8.4, 4.8), s.torso), SHIRT),
        # Tablier de cuir épais, du torse aux genoux.
        Part(lambda p: rounded_box(p, s.on_torso("pelvis", (0, 3.0, 4.2)), (5.6, 10.0, 0.9), 0.8, s.torso), APRON),
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 1.0, 0)), s.on_torso("pelvis", (0, -4.0, 0)), 5.4, 5.8), LEGS),
        Part(lambda p: sphere(p, s.point("head"), DIMENSIONS.head_radius), SKIN),
        # Cheveux courts tirés en arrière, lunettes de soudure relevées sur le front.
        Part(lambda p: np.maximum(ellipsoid(p, s.on_head((0, 0.9, -0.8)), (4.8, 4.2, 4.6), s.head),
                                  -ellipsoid(p, s.on_head((0, -1.6, 3.6)), (4.0, 3.6, 2.6), s.head)), HAIR),
        Part(lambda p: capsule(p, s.on_head((-4.4, 1.9, 0.5)), s.on_head((4.4, 1.9, 0.5)), 0.8), GOGGLES),
        Part(lambda p: sphere(p, s.on_head((1.7, 2.1, 3.7)), 1.35), LENS),
        Part(lambda p: sphere(p, s.on_head((-1.7, 2.1, 3.7)), 1.35), LENS),
        # Marteau trop gros porté sur l'épaule droite : manche et tête, avec une braise dans la masse.
        Part(lambda p: capsule(p, s.on_torso("chest", (-5.0, -8.0, 2.0)), s.on_torso("chest", (-6.5, 6.5 - 0.9 * d, -5.5)), 1.1), HANDLE),
        Part(lambda p: rounded_box(p, s.on_torso("chest", (-6.7, 7.5 - 0.9 * d, -6.0)), (3.6, 2.8, 2.8), 0.6, s.torso), HAMMER),
        Part(lambda p: sphere(p, s.on_torso("chest", (-3.0, 7.5 - 0.9 * d, -6.0)), 0.9 + 0.5 * max(d, 0.0)), EMBER),
    ]
    parts += limbs(s, LimbStyle(sleeve=SHIRT, hand=GLOVES, leg=LEGS, boot=BOOTS, arm_radius=2.5, hand_radius=2.2,
                                leg_radius=2.9, boot_radius=2.8, boot_height=4.0, foot_radius=2.5))
    return parts


ANIMATIONS = character_animations(Gait(lean=0.02, arm_out=0.32, stride=0.85, arm_swing=0.8, bounce=0.6, heavy=1.0))
