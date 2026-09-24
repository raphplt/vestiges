"""
La Scaphandrière sans mer — « Ma mer a été oubliée. Moi, non. »
Seule silhouette ronde en haut : casque de plongée à hublot, bottes lestées, tuyau d'air vers la bouteille dorsale.
Toile olive dominante, laiton en accent.
"""
from __future__ import annotations

from ._body import LimbStyle, limbs
from ..palette import make_material
from ..poses import Gait, character_animations
from ..render import Part
from ..rig import Proportions, Skeleton
from ..sdf import capsule, ellipsoid, sphere

CHARACTER_ID = "scaphandriere"
DIMENSIONS = Proportions(shin=11.0, thigh=12.0, spine=14.0, head_radius=5.8, shoulder_half=7.0, hip_half=3.6,
                         upper_arm=9.0, forearm=8.5)

SUIT, BRASS, GLASS, BOOTS, TANK, HOSE, GLOVES, COLLAR = range(8)
MATERIALS = [
    make_material("suit", "#6F7560"),
    make_material("brass", "#B08D57"),
    make_material("glass", "#2E4A50", contrast=0.6),
    make_material("boots", "#3A3A3E"),
    make_material("tank", "#8A7048"),
    make_material("hose", "#4A4A44"),
    make_material("gloves", "#5A5046"),
    make_material("collar", "#9A7A48"),
]


def build(skeleton: Skeleton) -> list[Part]:
    s = skeleton
    torso_center = (s.point("pelvis") + s.point("chest")) * 0.5
    parts = [
        Part(lambda p, c=torso_center: ellipsoid(p, c, (6.6, 8.8, 4.6), s.torso), SUIT),
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 1.0, 0)), s.on_torso("pelvis", (0, -3.5, 0)), 5.2, 5.4), SUIT),
        # Col de laiton sous le casque, casque rond et hublot frontal.
        Part(lambda p: ellipsoid(p, s.on_torso("neck", (0, -0.5, 0)), (6.2, 2.0, 5.2), s.torso), COLLAR),
        Part(lambda p: sphere(p, s.point("head"), DIMENSIONS.head_radius), BRASS),
        Part(lambda p: ellipsoid(p, s.on_head((0, 0.0, 4.6)), (3.0, 3.0, 1.8), s.head), GLASS),
        Part(lambda p: sphere(p, s.on_head((4.9, 0.4, 2.0)), 1.4), GLASS),
        Part(lambda p: sphere(p, s.on_head((-4.9, 0.4, 2.0)), 1.4), GLASS),
        # Bouteille d'air dans le dos et tuyau qui retombe jusqu'au casque.
        Part(lambda p: capsule(p, s.on_torso("chest", (0, -9.0, -6.2)), s.on_torso("chest", (0, 1.0, -6.2)), 3.2), TANK),
        Part(lambda p: capsule(p, s.on_torso("chest", (2.2, 1.5, -6.0)), s.on_head((3.6, -2.5, -3.5)), 0.9), HOSE),
    ]
    parts += limbs(s, LimbStyle(sleeve=SUIT, hand=GLOVES, leg=SUIT, boot=BOOTS, arm_radius=2.4, hand_radius=2.1,
                                leg_radius=2.7, boot_radius=3.0, boot_height=5.0, foot_radius=2.7))
    return parts


ANIMATIONS = character_animations(Gait(lean=0.04, arm_out=0.3, stride=0.8, arm_swing=0.7, bounce=0.5, heavy=0.7))
