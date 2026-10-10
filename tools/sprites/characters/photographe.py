"""
La Photographe — Claire Morel (lore §5) : la nuit du 14, depuis le clocher, elle a pris trois photos qu'elle n'a
jamais développées ; la ville lui a demandé de détruire les négatifs, elle ne l'a pas fait. Imperméable bordeaux
ceinturé, col relevé ; appareil à deux objectifs pendu sur la poitrine, flash monté sur une potence avec son grand
réflecteur rond qui dépasse au-dessus de l'épaule droite (la cloche du Sonneur est à gauche) ; sacoche de pellicules à la hanche, boîtes jaunes qui dépassent ; carré
court à frange. Proportions stylisées des personnages joués (plan 25 S2). Bordeaux dominant, argent du flash en
accent.
"""
from __future__ import annotations

import numpy as np

from ._body import LimbStyle, limbs
from ..palette import make_material
from ..poses import Gait, playable_animations
from ..render import Part
from ..rig import Proportions, Skeleton
from ..sdf import capsule, cylinder, ellipsoid, rotation_x, rotation_z, rounded_box, sphere

CHARACTER_ID = "photographe"
FRAME_SIZE = (42, 44)
FRAME_PIVOT = (21.0, 40.0)
DIMENSIONS = Proportions(ankle=2.8, shin=9.2, thigh=9.6, spine=11.2, neck=1.3, head_radius=6.6, shoulder_half=6.0,
                         hip_half=3.0, upper_arm=7.8, forearm=6.9)

COAT, BELT, TROUSERS, BOOTS, SKIN, EYES, HAIR, CAMERA, CHROME, LENS, REFLECTOR, BULB, STRAP, FILM = range(14)
MATERIALS = [
    make_material("coat", "#7A2E3C", contrast=1.15),
    make_material("belt", "#4A1E26"),
    make_material("trousers", "#36323A"),
    make_material("boots", "#2E2624", contrast=1.1),
    make_material("skin", "#D0A084", contrast=0.8),
    make_material("eyes", "#1E1614", contrast=0.3),
    make_material("hair", "#2E2426", contrast=1.2),
    make_material("camera", "#26262C", contrast=0.9),
    make_material("chrome", "#B8BCC4", contrast=1.2),
    make_material("lens", "#3E5A70", contrast=0.6),
    make_material("reflector", "#D8DCE2", contrast=1.3),
    make_material("bulb", "#F4F0D8", contrast=0.4),
    make_material("strap", "#5A3A2A"),
    make_material("film", "#E8B830", contrast=0.9),
]


def build(skeleton: Skeleton) -> list[Part]:
    s = skeleton
    # Appareil et réflecteur ballottent un peu sur la poitrine.
    d = s.drape
    k = DIMENSIONS.head_radius / 6.2

    def h(x: float, y: float, z: float) -> np.ndarray:
        return s.on_head((x * k, y * k, z * k))

    torso_center = (s.point("pelvis") + s.point("chest")) * 0.5
    camera = s.on_torso("chest", (0.4, -4.4 + 0.2 * d, 5.4))
    # Réflecteur en coupe tourné vers l'avant, au bout d'une potence : il dépasse au-dessus de l'épaule droite.
    dish_center = s.on_torso("chest", (-9.8 - 0.3 * d, 7.6, 1.6))
    dish = s.torso @ rotation_z(0.3) @ rotation_x(np.pi / 2 - 0.4)
    parts = [
        # Imperméable ceinturé, col relevé, pans jusqu'au genou.
        Part(lambda p, c=torso_center: ellipsoid(p, c, (5.4, 7.4, 3.8), s.torso), COAT),
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 1.5, 0)), s.on_torso("pelvis", (0.5 * d, -8.0, -0.6)), 4.6, 5.8), COAT),
        Part(lambda p: ellipsoid(p, s.on_torso("pelvis", (0, 2.0, 0)), (5.0, 1.0, 3.8), s.torso), BELT),
        Part(lambda p: np.maximum(ellipsoid(p, s.on_torso("neck", (0, -0.2, 0)), (5.0, 2.6, 4.4), s.torso),
                                  -ellipsoid(p, s.on_torso("neck", (0, 0.4, 2.4)), (2.6, 3.0, 2.4), s.torso)), COAT),
        # Visage, yeux en amande, carré court à frange.
        Part(lambda p: sphere(p, h(0, -0.4, 0.4), 5.6 * k), SKIN),
        Part(lambda p: ellipsoid(p, h(-2.1, -0.2, 5.3), (1.0 * k, 1.6 * k, 0.9 * k), s.head), EYES),
        Part(lambda p: ellipsoid(p, h(2.1, -0.2, 5.3), (1.0 * k, 1.6 * k, 0.9 * k), s.head), EYES),
        Part(lambda p: np.maximum.reduce([ellipsoid(p, h(0, 1.0, -1.0), (6.0 * k, 5.6 * k, 5.8 * k), s.head),
                                          -ellipsoid(p, h(0, -1.6, 4.4), (4.8 * k, 4.4 * k, 3.6 * k), s.head),
                                          (h(0, -4.4, 0) - p) @ (s.head @ [0.0, 1.0, 0.0])]), HAIR),
        # Bandoulière de l'appareil autour du cou.
        Part(lambda p: capsule(p, s.on_torso("neck", (-3.8, -0.6, 2.0)), camera + s.torso @ [-2.4, 1.2, -0.4], 0.6), STRAP),
        Part(lambda p: capsule(p, s.on_torso("neck", (3.8, -0.6, 2.0)), camera + s.torso @ [2.4, 1.2, -0.4], 0.6), STRAP),
        # Appareil à deux objectifs : boîtier noir, garnitures chromées, deux objectifs l'un sur l'autre.
        Part(lambda p: rounded_box(p, camera, (2.6, 3.0, 2.2), 0.6, s.torso), CAMERA),
        Part(lambda p: rounded_box(p, camera + s.torso @ [0, 3.1, 0], (2.7, 0.5, 2.3), 0.2, s.torso), CHROME),
        Part(lambda p: cylinder(p, camera + s.torso @ [0, 1.2, 2.4], 1.2, 0.6, 0.2, s.torso @ rotation_x(np.pi / 2)), CHROME),
        Part(lambda p: cylinder(p, camera + s.torso @ [0, -1.4, 2.4], 1.3, 0.7, 0.2, s.torso @ rotation_x(np.pi / 2)), LENS),
        # Flash : bras chromé, grand réflecteur et ampoule au centre.
        Part(lambda p: capsule(p, camera + s.torso @ [-2.6, 0, 0], s.on_torso("chest", (-7.4, -1.0, 4.0)), 0.6), CHROME),
        Part(lambda p: capsule(p, s.on_torso("chest", (-7.4, -1.0, 4.0)), dish_center - dish @ [0, 1.2, 0], 0.6), CHROME),
        # Coupe sphérique creuse, concave vers l'avant ; l'ampoule au fond.
        Part(lambda p: np.maximum(np.abs(sphere(p, dish_center + dish @ [0, 4.6, 0], 6.0)) - 0.45,
                                  ((p - dish_center) @ dish)[:, 1] - 0.2), REFLECTOR),
        Part(lambda p: sphere(p, dish_center + dish @ [0, -0.4, 0], 1.3), BULB),
        # Sacoche de pellicules sur la hanche droite, boîtes jaunes qui dépassent.
        Part(lambda p: capsule(p, s.on_torso("neck", (3.6, -1.0, -1.6)), s.on_torso("pelvis", (-5.6, 3.0, 0.0)), 0.6), STRAP),
        Part(lambda p: rounded_box(p, s.on_torso("pelvis", (-6.6, 0.4, 0.6)), (1.8, 3.0, 3.4), 0.8, s.torso), STRAP),
        Part(lambda p: cylinder(p, s.on_torso("pelvis", (-6.4, 3.6, 1.6)), 0.95, 1.2, 0.2, s.torso), FILM),
        Part(lambda p: cylinder(p, s.on_torso("pelvis", (-6.8, 3.4, -0.6)), 0.95, 1.0, 0.2, s.torso), FILM),
    ]
    parts += limbs(s, LimbStyle(sleeve=COAT, hand=SKIN, leg=TROUSERS, boot=BOOTS, arm_radius=2.1, hand_radius=2.0,
                                leg_radius=2.1, boot_radius=2.2, boot_height=4.8, foot_radius=2.0))
    return parts


ANIMATIONS = playable_animations(Gait(lean=0.06, arm_out=0.28, stride=0.95, arm_swing=0.9, bounce=0.8))
