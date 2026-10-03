"""
La Forgeuse — « Tout ce qui est tordu peut se redresser. Même le monde. »
La silhouette la plus large : épaules massives, jambes courtes, tablier de cuir, gants épais, lunettes de soudure aux
verres rouges relevées sur le front, chignon serré, marteau démesuré porté sur l'épaule. Proportions stylisées
(plan 25 S2, DECISIONS §61). Gris acier dominant, rouge forge en accent (charte §4).
"""
from __future__ import annotations

import numpy as np

from ._body import LimbStyle, limbs
from ..palette import make_emissive, make_material
from ..poses import Gait, playable_animations
from ..render import Part
from ..rig import Proportions, Skeleton
from ..sdf import capsule, cylinder, ellipsoid, rounded_box, sphere

CHARACTER_ID = "forgeuse"
FRAME_SIZE = (44, 44)
FRAME_PIVOT = (22.0, 40.0)
DIMENSIONS = Proportions(ankle=2.8, shin=8.0, thigh=8.4, spine=10.6, neck=1.0, head_radius=6.8, shoulder_half=8.8,
                         hip_half=4.4, upper_arm=8.0, forearm=7.2)

SHIRT, APRON, LEGS, BOOTS, SKIN, HAIR, GOGGLES, LENS, HANDLE, HAMMER, GLOVES, EMBER, BANDANA, EYES = range(14)
MATERIALS = [
    make_material("shirt", "#7A7A8C", contrast=1.1),
    make_material("apron", "#6A4A30", contrast=1.15),
    make_material("legs", "#4A4A52"),
    make_material("boots", "#35302C", contrast=1.1),
    make_material("skin", "#B8805E", contrast=0.8),
    make_material("hair", "#3A2A26", contrast=1.1),
    make_material("goggles", "#3E3E46", contrast=1.2),
    make_material("lens", "#D84A3A", contrast=1.0),
    make_material("handle", "#8A6440"),
    make_material("hammer", "#5E6070", contrast=1.35),
    make_material("gloves", "#5A3E2A", contrast=1.1),
    make_emissive("ember", "#F08A3A"),
    make_material("bandana", "#C44A3A"),
    make_material("eyes", "#1E1614", contrast=0.3),
]


def build(skeleton: Skeleton) -> list[Part]:
    s = skeleton
    # Braise qui palpite dans la masse et marteau qui se cale sur l'épaule à chaque souffle.
    d = s.drape
    k = DIMENSIONS.head_radius / 6.2

    def h(x: float, y: float, z: float) -> np.ndarray:
        return s.on_head((x * k, y * k, z * k))

    torso_center = (s.point("pelvis") + s.point("chest")) * 0.5
    parts = [
        # Buste en tonneau, épaules rondes et massives.
        Part(lambda p, c=torso_center: ellipsoid(p, c, (8.0, 7.4, 5.0), s.torso), SHIRT),
        Part(lambda p: sphere(p, s.on_torso("chest", (7.6, -1.6, 0)), 3.6), SHIRT),
        Part(lambda p: sphere(p, s.on_torso("chest", (-7.6, -1.6, 0)), 3.6), SHIRT),
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 1.0, 0)), s.on_torso("pelvis", (0, -3.5, 0)), 5.6, 6.0), LEGS),
        # Tablier de cuir épais, du torse aux genoux, bretelles rouges et poche.
        Part(lambda p: rounded_box(p, s.on_torso("pelvis", (0, 3.2, 4.8)), (6.0, 10.4, 1.0), 0.8, s.torso), APRON),
        Part(lambda p: capsule(p, s.on_torso("neck", (-3.0, -1.6, 3.6)), s.on_torso("pelvis", (-4.4, 11.0, 5.8)), 1.0), BANDANA),
        Part(lambda p: capsule(p, s.on_torso("neck", (3.0, -1.6, 3.6)), s.on_torso("pelvis", (4.4, 11.0, 5.8)), 1.0), BANDANA),
        Part(lambda p: rounded_box(p, s.on_torso("pelvis", (1.6, 0.4, 6.0)), (3.0, 2.4, 0.6), 0.5, s.torso), HANDLE),
        # Tête nue : visage, yeux, cheveux tirés en chignon, bandana rouge noué.
        Part(lambda p: sphere(p, h(0, -0.3, 0.2), 5.6 * k), SKIN),
        Part(lambda p: sphere(p, h(-2.0, -0.4, 5.0), 0.85 * k), EYES),
        Part(lambda p: sphere(p, h(2.0, -0.4, 5.0), 0.85 * k), EYES),
        Part(lambda p: np.maximum(ellipsoid(p, h(0, 1.4, -1.0), (5.9 * k, 5.0 * k, 5.6 * k), s.head),
                                  -ellipsoid(p, h(0, -1.4, 4.2), (5.0 * k, 4.4 * k, 3.4 * k), s.head)), HAIR),
        Part(lambda p: sphere(p, h(0, 3.6, -5.4), 3.0 * k), HAIR),
        Part(lambda p: capsule(p, h(-5.4, 3.0, -1.6), h(5.4, 3.0, -1.6), 1.0 * k), BANDANA),
        # Lunettes de soudure relevées sur le front : deux gros verres rouges cerclés.
        Part(lambda p: capsule(p, h(-5.8, 4.6, 0.0), h(5.8, 4.6, 0.0), 0.8 * k), GOGGLES),
        Part(lambda p: sphere(p, h(-2.3, 5.6, 3.6), 2.0 * k), GOGGLES),
        Part(lambda p: sphere(p, h(2.3, 5.6, 3.6), 2.0 * k), GOGGLES),
        Part(lambda p: sphere(p, h(-2.3, 5.8, 4.6), 1.4 * k), LENS),
        Part(lambda p: sphere(p, h(2.3, 5.8, 4.6), 1.4 * k), LENS),
        # Marteau démesuré sur l'épaule droite : long manche, tête plus grosse que la sienne, braise dans la masse.
        Part(lambda p: capsule(p, s.on_torso("chest", (-6.0, -10.0, 3.0)), s.on_torso("chest", (-7.0, 9.0 - 0.9 * d, -6.0)), 1.3), HANDLE),
        Part(lambda p: rounded_box(p, s.on_torso("chest", (-7.2, 10.5 - 0.9 * d, -6.6)), (6.0, 4.4, 4.6), 0.8, s.torso), HAMMER),
        Part(lambda p: rounded_box(p, s.on_torso("chest", (-13.4, 10.5 - 0.9 * d, -6.6)), (0.8, 4.8, 5.0), 0.3, s.torso), HAMMER),
        Part(lambda p: sphere(p, s.on_torso("chest", (-3.2, 10.5 - 0.9 * d, -2.0)), 1.1 + 0.5 * max(d, 0.0)), EMBER),
    ]
    parts += limbs(s, LimbStyle(sleeve=SHIRT, hand=GLOVES, leg=LEGS, boot=BOOTS, arm_radius=2.9, hand_radius=3.0,
                                leg_radius=3.0, boot_radius=3.1, boot_height=4.2, foot_radius=2.8))
    for side in ("l", "r"):
        elbow, hand = s.point(f"elbow_{side}"), s.point(f"hand_{side}")
        start, end = elbow * 0.5 + hand * 0.5, elbow * 0.15 + hand * 0.85
        parts.append(Part(lambda p, a=start, b=end: capsule(p, a, b, 3.0, 2.8), GLOVES))
    return parts


ANIMATIONS = playable_animations(Gait(lean=0.02, arm_out=0.36, stride=0.85, arm_swing=0.8, bounce=0.6, heavy=1.0))
