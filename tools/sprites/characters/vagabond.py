"""
Le Vagabond — « Tant que je marche, le chemin existe. »
Silhouette lisible au sac énorme qui dépasse de la tête (couchage, gamelle, piquets, carte roulée) et au grand chapeau
de route ; visage découvert, barbe courte, longue écharpe orange qui flotte, manteau recousu. Proportions stylisées
(plan 25 S2, DECISIONS §61). Brun terreux dominant, orange outil en accent (charte §4).
"""
from __future__ import annotations

import numpy as np

from ..palette import make_material
from ..render import Part
from ._body import LimbStyle, limbs
from ..poses import Gait, playable_animations
from ..rig import Proportions, Skeleton
from ..sdf import capsule, cylinder, ellipsoid, rounded_box, sphere

CHARACTER_ID = "vagabond"
FRAME_SIZE = (40, 44)
FRAME_PIVOT = (20.0, 40.0)
# Bord relevé vers l'avant : il encadre le visage au lieu de le couvrir.
_BRIM_TILT = np.array([[1.0, 0.0, 0.0], [0.0, np.cos(0.35), np.sin(0.35)], [0.0, -np.sin(0.35), np.cos(0.35)]])
DIMENSIONS = Proportions(ankle=2.8, shin=9.4, thigh=9.9, spine=11.4, neck=1.3, head_radius=6.8, shoulder_half=6.6,
                         hip_half=3.2, upper_arm=8.0, forearm=7.0)

COAT, PATCH, TROUSERS, BOOTS, SKIN, HAT, PACK, ACCENT, SCARF, BEDROLL, METAL, GLOVES, MAP, BEARD, EYES = range(15)
MATERIALS = [
    make_material("coat", "#7A5C42", contrast=1.1),
    make_material("patch", "#9A7A52"),
    make_material("trousers", "#4F4A44"),
    make_material("boots", "#3A2E26", contrast=1.1),
    make_material("skin", "#C88E68", contrast=0.8),
    make_material("hat", "#5A4230", contrast=1.15),
    make_material("pack", "#6B5236", contrast=1.1),
    make_material("accent", "#D4853A"),
    make_material("scarf", "#E08A38", contrast=1.1),
    make_material("bedroll", "#6E8A5A", contrast=1.1),
    make_material("metal", "#8A8484", contrast=1.3),
    make_material("gloves", "#54402F"),
    make_material("map", "#D8C89C"),
    make_material("beard", "#4A3428"),
    make_material("eyes", "#1E1614", contrast=0.3),
]


def build(skeleton: Skeleton) -> list[Part]:
    s = skeleton
    # Éléments souples : écharpe, gamelle et piquets ballottent en retard sur le corps.
    d = s.drape
    k = DIMENSIONS.head_radius / 6.2

    def h(x: float, y: float, z: float) -> np.ndarray:
        return s.on_head((x * k, y * k, z * k))

    torso_center = (s.point("pelvis") + s.point("chest")) * 0.5
    parts = [
        Part(lambda p, c=torso_center: ellipsoid(p, c, (5.4, 7.4, 3.9), s.torso), COAT),
        # Pan de manteau évasé jusqu'au genou.
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 1.5, 0)), s.on_torso("pelvis", (0, -6.5, -0.4)), 4.4, 5.6), COAT),
        # Pièces recousues bien visibles : épaule et flanc.
        Part(lambda p: ellipsoid(p, s.on_torso("pelvis", (3.6, 5.0, 2.6)), (2.2, 2.6, 1.4), s.torso), PATCH),
        Part(lambda p: ellipsoid(p, s.on_torso("chest", (-4.8, -1.0, 1.6)), (1.8, 2.0, 1.6), s.torso), PATCH),
        # Écharpe orange : col épais, pan sur la poitrine, long bout qui flotte derrière l'épaule.
        Part(lambda p: ellipsoid(p, s.on_torso("neck", (0, -0.4, 0.6)), (5.2, 2.6, 4.8), s.torso), SCARF),
        Part(lambda p: capsule(p, s.on_torso("neck", (1.8, -1.0, 3.6)), s.on_torso("neck", (2.6 + 1.5 * d, -7.5, 4.6)), 1.6, 1.3), SCARF),
        Part(lambda p: capsule(p, s.on_torso("neck", (-2.4, -0.4, -1.6)), s.on_torso("chest", (-8.5 - 2.0 * d, -5.0, -4.0 - 2.0 * d)), 1.6, 0.7), SCARF),
        # Visage découvert, barbe courte, deux yeux sombres sous le bord du chapeau.
        Part(lambda p: sphere(p, h(0, -0.4, 0.4), 5.6 * k), SKIN),
        Part(lambda p: np.maximum(ellipsoid(p, h(0, -3.0, 1.6), (4.6 * k, 3.2 * k, 4.0 * k), s.head),
                                  -ellipsoid(p, h(0, -1.6, 6.0), (2.0 * k, 1.0 * k, 2.0 * k), s.head)), BEARD),
        Part(lambda p: sphere(p, h(-2.0, 0.4, 5.0), 0.85 * k), EYES),
        Part(lambda p: sphere(p, h(2.0, 0.4, 5.0), 0.85 * k), EYES),
        # Grand chapeau de route : calotte cabossée et large bord incliné vers l'avant.
        Part(lambda p: ellipsoid(p, h(0, 5.0, -0.6), (4.8 * k, 3.4 * k, 4.8 * k), s.head), HAT),
        Part(lambda p: cylinder(p, h(0, 3.8, -0.6), 7.6 * k, 0.5 * k, 0.4, s.head @ _BRIM_TILT), HAT),
        Part(lambda p: cylinder(p, h(0, 4.4, -0.6), 4.9 * k, 0.7 * k, 0.3, s.head), ACCENT),
        # Sac énorme, plus large que les épaules, qui monte bien au-dessus du chapeau.
        Part(lambda p: rounded_box(p, s.on_torso("chest", (0, 4.0, -8.0)), (7.6, 12.0, 4.4), 2.0, s.torso), PACK),
        Part(lambda p: rounded_box(p, s.on_torso("chest", (0, 9.0, -12.2)), (5.0, 4.6, 0.9), 0.6, s.torso), PATCH),
        Part(lambda p: capsule(p, s.on_torso("chest", (-8.0, 18.0, -7.6)), s.on_torso("chest", (8.0, 18.0, -7.6)), 3.4), BEDROLL),
        Part(lambda p: capsule(p, s.on_torso("chest", (-8.4, 18.0, -7.6)), s.on_torso("chest", (-8.4, 18.0, -7.6)), 3.5), ACCENT),
        # Piquets de tente et carte roulée qui dépassent en éventail.
        Part(lambda p: capsule(p, s.on_torso("chest", (4.0, 14.0, -9.0)), s.on_torso("chest", (8.0 + 1.5 * d, 25.0, -10.5)), 0.8), METAL),
        Part(lambda p: capsule(p, s.on_torso("chest", (2.2, 14.0, -9.4)), s.on_torso("chest", (4.6 + 1.2 * d, 26.5, -11.0)), 0.8), METAL),
        Part(lambda p: capsule(p, s.on_torso("chest", (-3.8, 15.0, -9.6)), s.on_torso("chest", (-5.8, 23.0, -10.4)), 1.3), MAP),
        # Gamelle qui pend au flanc du sac et se balance.
        Part(lambda p: cylinder(p, s.on_torso("chest", (8.6 + 0.8 * d, -2.0, -6.4)), 2.6, 1.6, 0.4, s.torso), METAL),
        Part(lambda p: capsule(p, s.on_torso("chest", (7.6, 1.0, -6.6)), s.on_torso("chest", (8.6 + 0.8 * d, -0.6, -6.4)), 0.5), METAL),
        # Sangle orange devant : le sac se lit aussi de face.
        Part(lambda p: capsule(p, s.on_torso("chest", (4.0, 0.6, 3.0)), s.on_torso("chest", (4.2, -8.0, 4.0)), 0.9), PACK),
        Part(lambda p: capsule(p, s.on_torso("chest", (-4.0, 0.6, 3.0)), s.on_torso("chest", (-4.2, -8.0, 4.0)), 0.9), PACK),
        Part(lambda p: capsule(p, s.on_torso("chest", (-4.2, -4.0, 4.2)), s.on_torso("chest", (4.2, -4.0, 4.2)), 0.8), ACCENT),
    ]
    parts += limbs(s, LimbStyle(sleeve=COAT, hand=GLOVES, leg=TROUSERS, boot=BOOTS, arm_radius=2.1, hand_radius=2.2,
                                leg_radius=2.3, boot_radius=2.5, boot_height=4.6, foot_radius=2.3))
    return parts


ANIMATIONS = playable_animations(Gait())
