"""
L'Écolière — Lise Garnier, neuf ans (lore §5) : la nuit du 14, elle attendait ses parents à l'école de la Haute-Ville ;
ils habitaient le Bas-Port et ne sont pas venus. Elle cherche quelqu'un qui vienne la chercher. C'est la même fillette
que l'écho de l'écolière (`habitant_ecoliere`) : blouse d'école bleu marine, col blanc, nattes, cartable rouge. Jouable,
elle prend les proportions des personnages joués (grosse tête, yeux lisibles) à sa taille d'enfant, un cartable plus
large que ses épaules, des rubans et une craie roses. Bleu marine dominant, rose craie en accent.
"""
from __future__ import annotations

import numpy as np

from ._body import LimbStyle, limbs
from ..palette import make_material
from ..poses import Gait, playable_animations
from ..render import Part
from ..rig import Proportions, Skeleton
from ..sdf import capsule, ellipsoid, rounded_box, sphere

CHARACTER_ID = "ecoliere"
FRAME_SIZE = (36, 40)
FRAME_PIVOT = (18.0, 36.0)
# Une enfant : jambes et buste courts, tête aussi grosse que celle des adultes stylisés.
DIMENSIONS = Proportions(ankle=2.2, shin=6.6, thigh=6.8, spine=8.4, neck=0.9, head_radius=6.4, shoulder_half=4.8,
                         hip_half=2.5, upper_arm=6.0, forearm=5.4)

SMOCK, COLLAR, SOCKS, SHOES, SKIN, EYES, HAIR, RIBBON, BAG, FLAP, CLASP, CHALK = range(12)
MATERIALS = [
    make_material("smock", "#3E4A6E", contrast=1.1),
    make_material("collar", "#ECE6DA", contrast=0.5),
    make_material("socks", "#DCD4C4", contrast=0.6),
    make_material("shoes", "#3A2A26", contrast=1.1),
    make_material("skin", "#D2A080", contrast=0.8),
    make_material("eyes", "#1E1614", contrast=0.3),
    make_material("hair", "#6A4430", contrast=1.1),
    make_material("ribbon", "#E888A8", contrast=0.9),
    make_material("bag", "#A8433A", contrast=1.15),
    make_material("flap", "#7E2E28", contrast=1.1),
    make_material("clasp", "#D8B050", contrast=0.8),
    make_material("chalk", "#F4B8CC", contrast=0.5),
]


def build(skeleton: Skeleton) -> list[Part]:
    s = skeleton
    # Nattes et rubans ballottent en retard sur le corps.
    d = s.drape
    k = DIMENSIONS.head_radius / 6.2

    def h(x: float, y: float, z: float) -> np.ndarray:
        return s.on_head((x * k, y * k, z * k))

    torso_center = (s.point("pelvis") + s.point("chest")) * 0.5
    bag = s.on_torso("chest", (0, -1.2, -5.8))
    parts = [
        # Blouse d'école boutonnée, évasée jusqu'aux genoux ; col Claudine blanc.
        Part(lambda p, c=torso_center: ellipsoid(p, c, (4.6, 6.0, 3.4), s.torso), SMOCK),
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 1.0, 0)), s.on_torso("pelvis", (0.4 * d, -5.6, -0.4)), 4.0, 5.4), SMOCK),
        Part(lambda p: ellipsoid(p, s.on_torso("neck", (0, -0.6, 1.0)), (4.0, 1.4, 3.6), s.torso), COLLAR),
        # Visage rond, grands yeux en amande.
        Part(lambda p: sphere(p, h(0, -0.6, 0.4), 5.6 * k), SKIN),
        Part(lambda p: ellipsoid(p, h(-2.1, -0.2, 5.3), (1.1 * k, 1.7 * k, 1.0 * k), s.head), EYES),
        Part(lambda p: ellipsoid(p, h(2.1, -0.2, 5.3), (1.1 * k, 1.7 * k, 1.0 * k), s.head), EYES),
        # Cheveux à frange courte et raie au milieu, deux nattes nouées de rubans roses qui se balancent.
        Part(lambda p: np.maximum(ellipsoid(p, h(0, 0.9, -0.8), (6.1 * k, 5.6 * k, 5.8 * k), s.head),
                                  -ellipsoid(p, h(0, -2.0, 4.6), (4.8 * k, 4.4 * k, 3.4 * k), s.head)), HAIR),
        # Nattes écartées de la tête : elles se voient de face comme de dos.
        *[Part(lambda p, sign=sign: capsule(p, h(sign * 4.8, -0.4, -1.6),
                                            h(sign * (8.2 + 0.6 * d), -5.4, -1.6 - 1.2 * d), 1.6 * k, 1.1 * k), HAIR)
          for sign in (1.0, -1.0)],
        *[Part(lambda p, sign=sign: sphere(p, h(sign * (7.4 + 0.5 * d), -4.2, -1.6 - 1.0 * d), 1.5 * k), RIBBON)
          for sign in (1.0, -1.0)],
        # Cartable de cuir rouge, plus large que les épaules : rabat sombre, deux fermoirs dorés, bretelles devant.
        Part(lambda p: rounded_box(p, bag, (6.8, 5.8, 2.8), 1.1, s.torso), BAG),
        Part(lambda p: rounded_box(p, bag + s.torso @ [0, 1.8, -1.0], (6.9, 4.0, 2.2), 0.9, s.torso), FLAP),
        Part(lambda p: sphere(p, bag + s.torso @ [-3.0, -0.8, -3.1], 0.85), CLASP),
        Part(lambda p: sphere(p, bag + s.torso @ [3.0, -0.8, -3.1], 0.85), CLASP),
        Part(lambda p: capsule(p, s.on_torso("chest", (3.0, 0.4, 2.6)), s.on_torso("chest", (3.2, -6.0, 3.2)), 0.7), FLAP),
        Part(lambda p: capsule(p, s.on_torso("chest", (-3.0, 0.4, 2.6)), s.on_torso("chest", (-3.2, -6.0, 3.2)), 0.7), FLAP),
    ]
    parts += limbs(s, LimbStyle(sleeve=SMOCK, hand=SKIN, leg=SOCKS, boot=SHOES, arm_radius=1.8, hand_radius=1.9,
                                leg_radius=1.7, boot_radius=1.9, boot_height=2.4, foot_radius=1.9))
    # Craie rose serrée dans la main droite.
    hand, elbow = s.point("hand_r"), s.point("elbow_r")
    direction = (hand - elbow) / np.linalg.norm(hand - elbow)
    parts.append(Part(lambda p: capsule(p, hand + direction * 0.6, hand + direction * 3.2, 0.9), CHALK))
    return parts


ANIMATIONS = playable_animations(Gait(lean=0.02, arm_out=0.22, stride=0.95, arm_swing=1.2, bounce=1.3))
