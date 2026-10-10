"""
L'Enfant du Bas-Port — Élie à six ans, la nuit du 14 (lore §5) : version alternative du Vagabond, débloquée après la
vraie fin, dans la seule run où il pleut et où la mer est là. Ciré jaune de pêcheur et suroît au bord rabattu sur la
nuque, bottes de caoutchouc, l'écharpe orange du Vagabond, trop grande pour lui, dont le bout traîne ; dans sa main
gauche, le petit bonnet rouge de sa sœur, dont le Vagabond ne se souvient pas. Proportions stylisées des personnages
joués (plan 25 S2) à sa taille d'enfant, plus petit que l'Écolière. Jaune ciré dominant, orange en accent.
"""
from __future__ import annotations

import numpy as np

from ._body import LimbStyle, limbs
from ..palette import make_material
from ..poses import Gait, playable_animations
from ..render import Part
from ..rig import Proportions, Skeleton
from ..sdf import capsule, cylinder, ellipsoid, rotation_x, sphere

CHARACTER_ID = "enfant_bas_port"
FRAME_SIZE = (34, 38)
FRAME_PIVOT = (17.0, 34.0)
DIMENSIONS = Proportions(ankle=2.0, shin=5.6, thigh=5.8, spine=7.4, neck=0.8, head_radius=6.2, shoulder_half=4.4,
                         hip_half=2.3, upper_arm=5.2, forearm=4.8)

OILSKIN, SEAM, BOOTS, SKIN, EYES, HAIR, SCARF, BONNET, POMPOM = range(9)
MATERIALS = [
    make_material("oilskin", "#E0B830", contrast=1.15),
    make_material("seam", "#A88420"),
    make_material("boots", "#2E3A34", contrast=1.1),
    make_material("skin", "#D0A07E", contrast=0.8),
    make_material("eyes", "#1E1614", contrast=0.3),
    make_material("hair", "#4A3428"),
    make_material("scarf", "#D8682E", contrast=1.1),
    make_material("bonnet", "#C0403A", contrast=1.0),
    make_material("pompom", "#ECE4D6", contrast=0.5),
]


def build(skeleton: Skeleton) -> list[Part]:
    s = skeleton
    # Bout d'écharpe, pans du ciré et bonnet ballottent en retard sur le corps.
    d = s.drape
    k = DIMENSIONS.head_radius / 6.2

    def h(x: float, y: float, z: float) -> np.ndarray:
        return s.on_head((x * k, y * k, z * k))

    torso_center = (s.point("pelvis") + s.point("chest")) * 0.5
    hand_l = s.point("hand_l")
    parts = [
        # Ciré trop grand, évasé jusqu'aux genoux, couture de boutonnage plus sombre.
        Part(lambda p, c=torso_center: ellipsoid(p, c, (4.6, 5.6, 3.6), s.torso), OILSKIN),
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 1.0, 0)), s.on_torso("pelvis", (0.4 * d, -6.4, -0.4)), 4.2, 5.8), OILSKIN),
        Part(lambda p: capsule(p, s.on_torso("chest", (0.6, -1.0, 3.6)), s.on_torso("pelvis", (0.6, -5.6, 5.2)), 0.45), SEAM),
        # Visage, grands yeux, mèches brunes sur les tempes, sous le suroît.
        Part(lambda p: sphere(p, h(0, -0.8, 0.4), 5.6 * k), SKIN),
        Part(lambda p: ellipsoid(p, h(-2.1, -0.6, 5.2), (1.1 * k, 1.7 * k, 1.0 * k), s.head), EYES),
        Part(lambda p: ellipsoid(p, h(2.1, -0.6, 5.2), (1.1 * k, 1.7 * k, 1.0 * k), s.head), EYES),
        Part(lambda p: ellipsoid(p, h(-3.8, 0.8, 2.6), (1.6 * k, 2.2 * k, 2.0 * k), s.head), HAIR),
        Part(lambda p: ellipsoid(p, h(3.8, 0.8, 2.6), (1.6 * k, 2.2 * k, 2.0 * k), s.head), HAIR),
        # Suroît : calotte ronde, bord étroit devant, large et rabattu sur la nuque.
        Part(lambda p: ellipsoid(p, h(0, 3.0, -0.6), (5.8 * k, 4.0 * k, 5.8 * k), s.head), OILSKIN),
        Part(lambda p: cylinder(p, h(0, 1.8, -1.6), 7.4 * k, 0.5 * k, 0.4, s.head @ rotation_x(-0.45)), OILSKIN),
        Part(lambda p: ellipsoid(p, h(0, 3.2, -0.6), (5.9 * k, 0.7 * k, 5.9 * k), s.head), SEAM),
        # Écharpe orange du Vagabond, trop grande : gros col et long bout qui traîne derrière lui.
        Part(lambda p: ellipsoid(p, s.on_torso("neck", (0, -0.6, 0.4)), (4.8, 2.4, 4.4), s.torso), SCARF),
        Part(lambda p: capsule(p, s.on_torso("neck", (1.6, -1.0, 3.2)), s.on_torso("neck", (2.4 + 1.2 * d, -8.0, 4.0)), 1.5, 1.2), SCARF),
        Part(lambda p: capsule(p, s.on_torso("neck", (-2.0, -0.6, -1.8)), s.on_torso("chest", (-6.4 - 2.0 * d, -9.0, -5.4 - 2.0 * d)), 1.5, 0.8), SCARF),
        # Le bonnet rouge de sa sœur, serré dans sa main gauche.
        Part(lambda p: ellipsoid(p, hand_l + np.array([0.6, -1.6, 0.4]), (1.8, 2.2, 1.6)), BONNET),
        Part(lambda p: sphere(p, hand_l + np.array([0.9 + 0.4 * d, -4.0, 0.6]), 1.1), POMPOM),
    ]
    parts += limbs(s, LimbStyle(sleeve=OILSKIN, hand=SKIN, leg=BOOTS, boot=BOOTS, arm_radius=1.9, hand_radius=1.7,
                                leg_radius=1.7, boot_radius=2.0, boot_height=4.0, foot_radius=1.9))
    return parts


ANIMATIONS = playable_animations(Gait(lean=0.04, arm_out=0.26, stride=0.95, arm_swing=1.1, bounce=1.2))
