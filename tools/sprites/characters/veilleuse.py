"""
La Veilleuse — Jeanne Oriol, personnage caché (lore §5) : la nuit du 14, la seule porte qui s'est ouverte au pied de
la barrière était la sienne, peinte en vert ; elle a tiré Élie à l'intérieur. Très âgée, lente, elle ne se bat bien
qu'à l'arrêt et tient les créatures à distance avec sa lampe. Veuve en noir, voûtée, châle de laine vert (la couleur
de sa porte) sur la tête et les épaules, franges qui pendent ; cheveux blancs sous le châle ; lampe à pétrole allumée
qui pend au bout de sa main gauche et se balance, canne dans la droite. Proportions stylisées des personnages joués
(plan 25 S2), plus petite et courbée. Noir dominant, vert de porte et flamme en accent.
"""
from __future__ import annotations

import numpy as np

from ._body import LimbStyle, limbs
from ..palette import make_emissive, make_material
from ..poses import Gait, playable_animations
from ..render import Part
from ..rig import Proportions, Skeleton
from ..sdf import capsule, cylinder, ellipsoid, rotation_x, sphere

CHARACTER_ID = "veilleuse"
FRAME_SIZE = (40, 42)
FRAME_PIVOT = (20.0, 38.0)
DIMENSIONS = Proportions(ankle=2.6, shin=8.2, thigh=8.6, spine=10.0, neck=0.8, head_radius=6.4, shoulder_half=5.6,
                         hip_half=3.2, upper_arm=7.4, forearm=6.6)

DRESS, APRON, SHAWL, FRINGE, SKIN, EYES, HAIR, SHOES, CANE, BRASS, GLASS, FLAME = range(12)
MATERIALS = [
    make_material("dress", "#2E2A34", contrast=1.15),
    make_material("apron", "#5A5660"),
    make_material("shawl", "#2C6A5C", contrast=1.1),
    make_material("fringe", "#4E9482", contrast=0.9),
    make_material("skin", "#D6B09A", contrast=0.8),
    make_material("eyes", "#1E1614", contrast=0.3),
    make_material("hair", "#E4E0DA", contrast=0.6),
    make_material("shoes", "#26222A"),
    make_material("cane", "#7A5838", contrast=1.1),
    make_material("brass", "#C8A050", contrast=1.1),
    make_material("glass", "#E8E2C8", contrast=0.5),
    make_emissive("flame", "#F4B040"),
]


def build(skeleton: Skeleton) -> list[Part]:
    s = skeleton
    # Franges et pan de châle ballottent en retard sur le corps.
    d = s.drape
    k = DIMENSIONS.head_radius / 6.2
    # Voûtée, elle relève la tête pour regarder devant elle.
    head = s.head @ rotation_x(-0.3)

    def h(x: float, y: float, z: float) -> np.ndarray:
        return s.point("head") + head @ np.array([x * k, y * k, z * k])

    torso_center = (s.point("pelvis") + s.point("chest")) * 0.5
    # La lampe pend sous la main gauche, d'aplomb : elle suit le balancement du bras sans pencher.
    hand_l, hand_r = s.point("hand_l"), s.point("hand_r")
    lamp = hand_l + np.array([0.0, -6.4, 0.8])
    parts = [
        # Robe noire de veuve jusqu'aux chevilles, tablier gris.
        Part(lambda p, c=torso_center: ellipsoid(p, c, (5.0, 6.8, 4.0), s.torso), DRESS),
        # Voûtée, la jupe tombe d'aplomb sous les hanches : son ourlet se place dans le repère du monde.
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 1.5, 0)), s.point("pelvis") + np.array([0.5 * d, -13.6, 0.4]), 4.6, 6.6), DRESS),
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 1.0, 4.0)), s.point("pelvis") + np.array([0.3 * d, -11.0, 5.4]), 3.0, 3.6), APRON),
        # Visage, yeux, mèches blanches qui dépassent du châle.
        Part(lambda p: sphere(p, h(0, -0.6, 0.4), 5.6 * k), SKIN),
        Part(lambda p: ellipsoid(p, h(-2.1, -0.2, 5.3), (1.0 * k, 1.5 * k, 0.9 * k), head), EYES),
        Part(lambda p: ellipsoid(p, h(2.1, -0.2, 5.3), (1.0 * k, 1.5 * k, 0.9 * k), head), EYES),
        Part(lambda p: ellipsoid(p, h(-3.2, 1.4, 3.4), (1.8 * k, 1.8 * k, 2.0 * k), head), HAIR),
        Part(lambda p: ellipsoid(p, h(3.2, 1.4, 3.4), (1.8 * k, 1.8 * k, 2.0 * k), head), HAIR),
        # Châle de laine vert : capuche ronde sur la tête, ouverte sur le visage, puis sur les épaules jusqu'aux coudes.
        Part(lambda p: np.maximum(ellipsoid(p, h(0, 1.0, -0.8), (6.8 * k, 6.6 * k, 6.6 * k), head),
                                  -ellipsoid(p, h(0, -1.4, 5.0), (4.8 * k, 5.4 * k, 3.6 * k), head)), SHAWL),
        Part(lambda p: ellipsoid(p, s.on_torso("chest", (0, -2.0, -0.4)), (7.6, 4.6, 5.2), s.torso), SHAWL),
        Part(lambda p: capsule(p, s.on_torso("chest", (-2.0, -1.0, 4.0)), s.on_torso("chest", (-1.2 + 0.6 * d, -9.0, 4.8)), 1.8, 1.2), SHAWL),
        Part(lambda p: capsule(p, s.on_torso("chest", (2.0, -1.0, 4.0)), s.on_torso("chest", (1.6 + 0.6 * d, -8.2, 4.6)), 1.8, 1.2), SHAWL),
        *[Part(lambda p, x=x: capsule(p, s.on_torso("chest", (x + 0.6 * d, -8.6, 4.8)),
                                      s.on_torso("chest", (x + 0.9 * d, -11.0, 4.6 - 0.6 * d)), 0.5), FRINGE)
          for x in (-2.0, -0.6, 0.8, 2.2)],
        # Lampe à pétrole : anse, réservoir de laiton, verre clair, flamme.
        # La flamme dépasse du verre vers l'avant : elle reste lisible même quand le verre est dans l'ombre.
        Part(lambda p: capsule(p, hand_l, lamp + np.array([0.0, 4.6, 0.0]), 0.5), BRASS),
        Part(lambda p: cylinder(p, lamp, 2.8, 1.2, 0.6), BRASS),
        Part(lambda p: capsule(p, lamp + np.array([0.0, 1.2, 0.0]), lamp + np.array([0.0, 4.0, 0.0]), 2.0, 1.4), GLASS),
        Part(lambda p: capsule(p, lamp + np.array([0.0, 1.6, 0.6]), lamp + np.array([0.0, 3.2, 0.9]), 1.5, 0.9), FLAME),
        Part(lambda p: cylinder(p, lamp + np.array([0.0, 4.4, 0.0]), 1.5, 0.35, 0.1), BRASS),
        # Canne de bois dans la main droite.
        Part(lambda p: capsule(p, hand_r + np.array([0.0, 1.0, 0.0]), np.array([hand_r[0] - 0.6, 0.4, hand_r[2] + 2.2]), 0.7), CANE),
    ]
    parts += limbs(s, LimbStyle(sleeve=DRESS, hand=SKIN, leg=DRESS, boot=SHOES, arm_radius=1.9, hand_radius=1.8,
                                leg_radius=1.9, boot_radius=1.9, boot_height=2.4, foot_radius=1.9))
    return parts


ANIMATIONS = playable_animations(Gait(lean=0.32, arm_out=0.26, stride=0.6, arm_swing=0.35, bounce=0.3, heavy=0.4))
