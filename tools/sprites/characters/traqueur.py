"""
Le Traqueur — « L'auteur des empreintes n'existe plus. Je les suis quand même. »
La silhouette la plus verticale : capuche pointue, arc plus haut que lui dans le dos, posture penchée en avant.
Vert forêt dominant, beige clair en accent (charte §4).
"""
from __future__ import annotations

import numpy as np

from ._body import LimbStyle, limbs
from ..palette import make_material
from ..poses import Gait, character_animations
from ..render import Part
from ..rig import Proportions, Skeleton
from ..sdf import capsule, ellipsoid, sphere

CHARACTER_ID = "traqueur"
DIMENSIONS = Proportions(shin=13.0, thigh=14.0, spine=14.5, head_radius=4.0, shoulder_half=5.8, hip_half=3.0,
                         upper_arm=10.0, forearm=9.0)

CLOAK, TUNIC, LEGS, BOOTS, FACE, BOW, STRING, QUIVER, FLETCH, WRAP, GLOVES = range(11)
MATERIALS = [
    make_material("cloak", "#3A5A30"),
    make_material("tunic", "#4B6A3A"),
    make_material("legs", "#46443A"),
    make_material("boots", "#3A2E26"),
    make_material("face", "#2E2A28", contrast=0.5),
    make_material("bow", "#8A6A42"),
    make_material("string", "#C4B490", contrast=0.4),
    make_material("quiver", "#6A4E34"),
    make_material("fletch", "#C4B490"),
    make_material("wrap", "#C4B490"),
    make_material("gloves", "#4E3E2E"),
]


def build(skeleton: Skeleton) -> list[Part]:
    s = skeleton
    # Éléments souples : pointe de capuche et empennages oscillent en retard sur le corps.
    d = s.drape
    torso_center = (s.point("pelvis") + s.point("chest")) * 0.5
    parts = [
        Part(lambda p, c=torso_center: ellipsoid(p, c, (4.9, 8.6, 3.5), s.torso), TUNIC),
        # Cape courte fendue : épaules couvertes, dos jusqu'aux reins.
        Part(lambda p: ellipsoid(p, s.on_torso("chest", (0, -3.0, -0.8)), (6.0, 6.5, 4.4), s.torso), CLOAK),
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 1.0, 0)), s.on_torso("pelvis", (0, -5.0, -0.2)), 4.2, 4.6), TUNIC),
        # Bandes de tissu clair : ceinture et avant-bras.
        Part(lambda p: ellipsoid(p, s.on_torso("pelvis", (0, 1.4, 0)), (4.8, 1.1, 3.8), s.torso), WRAP),
        Part(lambda p: sphere(p, s.on_head((0, -0.4, 0.3)), DIMENSIONS.head_radius - 0.5), FACE),
        # Capuche pointue creusée à l'avant, pointe rejetée vers l'arrière.
        Part(lambda p: np.maximum(ellipsoid(p, s.on_head((0, 0.6, -0.9)), (4.5, 5.0, 4.8), s.head),
                                  -ellipsoid(p, s.on_head((0, -0.9, 4.0)), (3.0, 3.4, 2.6), s.head)), CLOAK),
        Part(lambda p: capsule(p, s.on_head((0, 3.0, -2.5)), s.on_head((2.2 * d, 7.5 - 0.6 * d, -6.5)), 2.6, 0.5), CLOAK),
        # Arc en diagonale : les deux branches dépassent nettement de la silhouette (tête et hanche).
        Part(lambda p: capsule(p, s.on_torso("chest", (-10.5, -13.0, -5.0)), s.on_torso("chest", (-2.0, 1.5, -6.4)), 1.0, 1.3), BOW),
        Part(lambda p: capsule(p, s.on_torso("chest", (-2.0, 1.5, -6.4)), s.on_torso("chest", (6.5, 17.5, -5.0)), 1.3, 0.9), BOW),
        Part(lambda p: capsule(p, s.on_torso("chest", (-10.0, -12.6, -4.4)), s.on_torso("chest", (6.2, 17.0, -4.4)), 0.4), STRING),
        # Carquois et empennages.
        Part(lambda p: capsule(p, s.on_torso("chest", (3.0, -6.0, -5.0)), s.on_torso("chest", (4.8, 3.5, -5.6)), 1.8, 2.0), QUIVER),
        Part(lambda p: ellipsoid(p, s.on_torso("chest", (5.0 + 0.9 * d, 5.2, -5.8)), (1.9, 1.6, 1.6), s.torso), FLETCH),
    ]
    parts += limbs(s, LimbStyle(sleeve=TUNIC, hand=GLOVES, leg=LEGS, boot=BOOTS, arm_radius=1.7,
                                leg_radius=2.1, boot_radius=2.1, boot_height=6.0, foot_radius=1.8))
    return parts


ANIMATIONS = character_animations(Gait(lean=0.14, arm_out=0.2, stride=1.15, arm_swing=1.1))
