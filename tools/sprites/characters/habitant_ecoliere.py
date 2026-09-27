"""
Habitante oubliée (plan 16 O6) : une écolière au cartable, jupe plissée et chaussettes hautes. Les échos de l'oubli la
montrent de loin, en deux tons pâles ; elle n'est pas jouable. Proportions d'enfant : tête grosse, jambes courtes.
"""
from __future__ import annotations

from ._body import LimbStyle, limbs
from ..palette import make_material
from ..poses import Gait, character_animations
from ..render import Part
from ..rig import Proportions, Skeleton
from ..sdf import capsule, ellipsoid, rounded_box, sphere

CHARACTER_ID = "habitant_ecoliere"
DIMENSIONS = Proportions(ankle=2.4, shin=8.5, thigh=8.5, spine=10.5, neck=1.8, head_radius=4.6, shoulder_half=5.0,
                         hip_half=2.6, upper_arm=7.0, forearm=6.4)

DRESS, SOCKS, SHOES, SKIN, HAIR, BAG, STRAP, COLLAR = range(8)
MATERIALS = [
    make_material("dress", "#3E4A6E"),
    make_material("socks", "#D8D0C0", contrast=0.6),
    make_material("shoes", "#3A2E28"),
    make_material("skin", "#C89A7A"),
    make_material("hair", "#5A3A28"),
    make_material("bag", "#A8433A"),
    make_material("strap", "#6A3A2A"),
    make_material("collar", "#E8E0D4", contrast=0.5),
]


def build(skeleton: Skeleton) -> list[Part]:
    s = skeleton
    torso_center = (s.point("pelvis") + s.point("chest")) * 0.5
    parts = [
        Part(lambda p, c=torso_center: ellipsoid(p, c, (4.4, 6.4, 3.2), s.torso), DRESS),
        # Jupe plissée évasée.
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 1.0, 0)), s.on_torso("pelvis", (0, -4.2, 0)), 3.8, 5.0), DRESS),
        Part(lambda p: ellipsoid(p, s.on_torso("neck", (0, -0.6, 1.0)), (3.2, 1.2, 2.8), s.torso), COLLAR),
        Part(lambda p: sphere(p, s.point("head"), DIMENSIONS.head_radius), SKIN),
        # Cheveux : calotte et deux nattes.
        Part(lambda p: ellipsoid(p, s.on_head((0, 1.2, -0.8)), (4.9, 4.2, 4.8), s.head), HAIR),
        Part(lambda p: capsule(p, s.on_head((-3.8, -0.5, -1.5)), s.on_head((-4.4, -5.0, -2.0)), 1.1, 0.8), HAIR),
        Part(lambda p: capsule(p, s.on_head((3.8, -0.5, -1.5)), s.on_head((4.4, -5.0, -2.0)), 1.1, 0.8), HAIR),
        # Cartable dans le dos, plus large que les épaules : la signature de la silhouette.
        Part(lambda p: rounded_box(p, s.on_torso("chest", (0, -1.5, -5.2)), (5.4, 5.2, 2.4), 1.0, s.torso), BAG),
        Part(lambda p: capsule(p, s.on_torso("chest", (3.0, 0.5, 2.6)), s.on_torso("chest", (3.2, -6.0, 3.2)), 0.6), STRAP),
        Part(lambda p: capsule(p, s.on_torso("chest", (-3.0, 0.5, 2.6)), s.on_torso("chest", (-3.2, -6.0, 3.2)), 0.6), STRAP),
    ]
    parts += limbs(s, LimbStyle(sleeve=DRESS, hand=SKIN, leg=SOCKS, boot=SHOES, arm_radius=1.6, hand_radius=1.4,
                                leg_radius=1.6, boot_radius=1.6, boot_height=2.5, foot_radius=1.6))
    return parts


ANIMATIONS = character_animations(Gait(lean=0.02, arm_out=0.2, stride=0.9, arm_swing=1.2, bounce=1.3))
