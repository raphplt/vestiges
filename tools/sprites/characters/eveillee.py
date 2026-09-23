"""
L'Éveillée — « J'entends ce lieu tel qu'il était, tel qu'il est et tel qu'il aurait pu être. »
Silhouette éthérée : robe longue et manches amples qui flottent dans un vent inexistant, lueur d'Essence aux mains,
un pan d'étoffe dédoublé derrière elle. Blanc-bleu dominant, cyan Essence en accent (charte §4).
"""
from __future__ import annotations

import numpy as np

from ._body import LimbStyle, limbs
from ..palette import make_material
from ..poses import Gait, humanoid_animations
from ..render import Part
from ..rig import Proportions, Skeleton
from ..sdf import capsule, ellipsoid, sphere

CHARACTER_ID = "eveillee"
DIMENSIONS = Proportions(spine=14.5, head_radius=4.0, shoulder_half=5.6, hip_half=3.0)

ROBE, SASH, SKIN, HAIR, GLOW, ECHO, SHOES = range(7)
MATERIALS = [
    make_material("robe", "#A9B2C6"),
    make_material("sash", "#7F8FA8"),
    make_material("skin", "#C9A58E"),
    make_material("hair", "#8E86A8"),
    make_material("glow", "#5EC4C4", contrast=0.6),
    make_material("echo", "#8FB8C8", contrast=0.5),
    make_material("shoes", "#5A6070"),
]


def build(skeleton: Skeleton) -> list[Part]:
    s = skeleton
    torso_center = (s.point("pelvis") + s.point("chest")) * 0.5
    parts = [
        Part(lambda p, c=torso_center: ellipsoid(p, c, (4.8, 8.4, 3.4), s.torso), ROBE),
        # Robe évasée jusqu'aux chevilles : elle masque la marche et donne l'impression de flotter.
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 2.0, 0)), s.on_torso("pelvis", (0, -22.0, -1.2)), 4.4, 7.4), ROBE),
        Part(lambda p: ellipsoid(p, s.on_torso("pelvis", (0, 2.2, 0)), (4.9, 1.0, 3.6), s.torso), SASH),
        # Pan d'étoffe dédoublé qui flotte derrière : la silhouette « double » de la fiche.
        Part(lambda p: capsule(p, s.on_torso("chest", (1.5, -2.0, -3.5)), s.on_torso("pelvis", (4.0, -14.0, -9.0)), 1.6, 0.6), ECHO),
        Part(lambda p: capsule(p, s.on_torso("chest", (-1.5, -2.0, -3.5)), s.on_torso("pelvis", (-3.0, -12.0, -10.0)), 1.4, 0.5), ECHO),
        Part(lambda p: sphere(p, s.point("head"), DIMENSIONS.head_radius), SKIN),
        # Cheveux lilas rabattus vers l'arrière, visage dégagé.
        Part(lambda p: np.maximum(ellipsoid(p, s.on_head((0, 1.0, -1.2)), (4.5, 4.6, 4.6), s.head),
                                  -ellipsoid(p, s.on_head((0, -1.2, 3.8)), (3.4, 3.4, 2.6), s.head)), HAIR),
        # Éclats d'Essence qui flottent autour des mains : lisibles même sur sol effacé.
        Part(lambda p: sphere(p, s.point("hand_l") + [1.5, 3.0, 1.5], 0.9), GLOW),
        Part(lambda p: sphere(p, s.point("hand_r") + [-1.8, 4.2, 0.8], 0.8), GLOW),
        Part(lambda p: sphere(p, s.on_head((3.5, 4.5, 1.0)), 0.7), GLOW),
        Part(lambda p: capsule(p, s.on_head((0, 0.0, -3.5)), s.on_torso("chest", (0, -4.0, -5.0)), 3.0, 1.6), HAIR),
    ]
    parts += limbs(s, LimbStyle(sleeve=ROBE, hand=GLOW, leg=ROBE, boot=SHOES, arm_radius=2.3, hand_radius=1.9,
                                leg_radius=1.8, boot_radius=1.6, boot_height=2.0, foot_radius=1.5))
    return parts


ANIMATIONS = humanoid_animations(Gait(lean=-0.02, arm_out=0.38, stride=0.7, arm_swing=0.5, bounce=0.3))
