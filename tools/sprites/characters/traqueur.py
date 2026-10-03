"""
Le Traqueur — « L'auteur des empreintes n'existe plus. Je les suis quand même. »
La silhouette la plus verticale : capuche très pointue rejetée en arrière, deux pans de cape effilés, arc plus haut que
lui dans le dos, posture penchée en avant. Proportions stylisées (plan 25 S2) : tête et mains plus grosses, jambes
fines, pour qu'il se lise à 30 px. Sous la capuche, deux yeux pâles. Vert forêt dominant, beige clair en accent.
"""
from __future__ import annotations

import numpy as np

from ._body import LimbStyle, limbs
from ..palette import make_emissive, make_material
from ..poses import Gait, character_animations
from ..render import Part
from ..rig import Proportions, Skeleton
from ..sdf import capsule, ellipsoid, sphere

CHARACTER_ID = "traqueur"
FRAME_SIZE = (40, 42)
FRAME_PIVOT = (20.0, 38.0)
DIMENSIONS = Proportions(ankle=2.6, shin=10.0, thigh=10.5, spine=12.0, neck=1.4, head_radius=6.2, shoulder_half=6.0,
                         hip_half=2.8, upper_arm=8.5, forearm=7.5)

CLOAK, TUNIC, LEGS, BOOTS, FACE, BOW, STRING, QUIVER, FLETCH, WRAP, GLOVES, EYES, CLOAK_LINING = range(13)
MATERIALS = [
    make_material("cloak", "#3E6432", contrast=1.15),
    make_material("tunic", "#6A8A48"),
    make_material("legs", "#3E3C34"),
    make_material("boots", "#4A3424", contrast=1.1),
    make_material("face", "#221E1E", contrast=0.4),
    make_material("bow", "#B88A48", contrast=1.2),
    make_material("string", "#E0D4B0", contrast=0.4),
    make_material("quiver", "#7A5634"),
    make_material("fletch", "#D84A30", contrast=0.9),
    make_material("wrap", "#D6C69C", contrast=0.9),
    make_material("gloves", "#5A4430"),
    make_emissive("eyes", "#F2E6B0"),
    make_material("lining", "#2A4224"),
]


def build(skeleton: Skeleton) -> list[Part]:
    s = skeleton
    d = s.drape
    # Capuche, visage et yeux suivent la taille de la tête : changer les proportions ne demande pas de les redessiner.
    k = DIMENSIONS.head_radius / 6.2

    def h(x: float, y: float, z: float) -> np.ndarray:
        return s.on_head((x * k, y * k, z * k))
    torso_center = (s.point("pelvis") + s.point("chest")) * 0.5
    parts = [
        Part(lambda p, c=torso_center: ellipsoid(p, c, (4.6, 7.2, 3.4), s.torso), TUNIC),
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 1.0, 0)), s.on_torso("pelvis", (0, -4.0, -0.2)), 3.4, 3.8), TUNIC),
        # Ceinture et bandoulière claires : elles dessinent le buste de face comme de dos.
        Part(lambda p: ellipsoid(p, s.on_torso("pelvis", (0, 1.4, 0)), (4.0, 1.0, 3.2), s.torso), WRAP),
        Part(lambda p: capsule(p, s.on_torso("neck", (-3.5, -1.0, 3.0)), s.on_torso("pelvis", (3.8, 2.0, 3.2)), 0.9), WRAP),
        # Mantelet qui couvre les épaules, puis deux pans effilés qui battent en retard derrière les jambes.
        Part(lambda p: ellipsoid(p, s.on_torso("chest", (0, -1.6, -0.6)), (7.0, 5.0, 5.0), s.torso), CLOAK),
        Part(lambda p: capsule(p, s.on_torso("chest", (-2.6, -2.0, -3.6)), s.on_torso("pelvis", (-4.2 - 1.2 * d, -9.0, -6.5 - 1.6 * d)), 3.0, 0.5), CLOAK),
        Part(lambda p: capsule(p, s.on_torso("chest", (2.6, -2.0, -3.6)), s.on_torso("pelvis", (4.0 + 1.0 * d, -10.5, -6.0 - 2.0 * d)), 3.0, 0.5), CLOAK),
        # Capuche : volume plus gros que la tête, creusée à l'avant (ombre où brillent les yeux), pointe longue.
        Part(lambda p: np.maximum(ellipsoid(p, h(0, 0.8, -0.6), (7.0 * k, 7.2 * k, 7.0 * k), s.head),
                                  -ellipsoid(p, h(0, -0.6, 5.2), (4.6 * k, 4.6 * k, 3.6 * k), s.head)), CLOAK),
        Part(lambda p: sphere(p, h(0, -0.6, 0.0), 5.0 * k), FACE),
        Part(lambda p: capsule(p, h(0, 4.0, -3.0), h(2.6 * d, 10.5 - 0.8 * d, -9.5), 3.6 * k, 0.4), CLOAK),
        # Doublure plus sombre au bord de la capuche, puis écharpe claire qui sépare la tête du buste.
        Part(lambda p: np.maximum(ellipsoid(p, h(0, 0.6, 3.6), (5.6 * k, 5.8 * k, 1.4 * k), s.head),
                                  -ellipsoid(p, h(0, -0.6, 5.0), (4.4 * k, 4.4 * k, 3.0 * k), s.head)), CLOAK_LINING),
        Part(lambda p: ellipsoid(p, s.on_torso("neck", (0, -0.4, 0.8)), (5.6, 2.2, 5.0), s.torso), WRAP),
        Part(lambda p: sphere(p, h(-2.0, -0.3, 4.6), 1.0 * k), EYES),
        Part(lambda p: sphere(p, h(2.0, -0.3, 4.6), 1.0 * k), EYES),
        # Arc plus haut que lui, très en travers du dos : la branche basse sort à côté de la hanche, la haute au-dessus de
        # l'épaule opposée, la corde tendue à part ; il se lit de face comme de dos.
        Part(lambda p: capsule(p, s.on_torso("chest", (-13.0, -17.0, -5.0)), s.on_torso("chest", (-3.0, -1.0, -7.4)), 1.2, 1.8), BOW),
        Part(lambda p: capsule(p, s.on_torso("chest", (-3.0, -1.0, -7.4)), s.on_torso("chest", (9.5, 19.5, -5.0)), 1.8, 1.1), BOW),
        Part(lambda p: capsule(p, s.on_torso("chest", (-12.4, -16.6, -4.0)), s.on_torso("chest", (9.0, 19.0, -4.0)), 0.5), STRING),
        # Longue écharpe claire nouée au cou, dont le bout flotte derrière l'épaule.
        Part(lambda p: capsule(p, s.on_torso("neck", (2.5, -1.0, -2.0)), s.on_torso("chest", (7.5 + 1.5 * d, -6.5, -6.0 - 1.5 * d)), 1.8, 0.8), WRAP),
        # Carquois et empennages rouges qui dépassent de l'épaule.
        Part(lambda p: capsule(p, s.on_torso("chest", (3.2, -6.0, -5.0)), s.on_torso("chest", (4.6, 3.5, -5.6)), 1.8, 2.0), QUIVER),
        Part(lambda p: ellipsoid(p, s.on_torso("chest", (4.8 + 0.9 * d, 5.6, -5.8)), (2.2, 1.8, 1.8), s.torso), FLETCH),
    ]
    parts += limbs(s, LimbStyle(sleeve=TUNIC, hand=GLOVES, leg=LEGS, boot=BOOTS, arm_radius=1.8, hand_radius=2.1,
                                leg_radius=1.9, boot_radius=2.3, boot_height=5.0, foot_radius=2.1))
    for side in ("l", "r"):
        elbow, hand = s.point(f"elbow_{side}"), s.point(f"hand_{side}")
        start, end = elbow * 0.45 + hand * 0.55, elbow * 0.15 + hand * 0.85
        parts.append(Part(lambda p, a=start, b=end: capsule(p, a, b, 1.9, 1.8), WRAP))
    return parts


ANIMATIONS = character_animations(Gait(lean=0.2, arm_out=0.22, stride=1.15, arm_swing=1.1))
