"""
La Scaphandrière sans mer — « Ma mer a été oubliée. Moi, non. »
Solange Delmas, scaphandrière du chantier naval de Vaulme : après la nuit du 14, elle a remonté vingt-neuf corps du
port, et plonge encore là où il n'y a plus d'eau (lore §5). Seule silhouette ronde en haut : casque de cuivre plus gros
que la tête des autres, hublot grillagé, taches de vert-de-gris ; pèlerine de laiton boulonnée, bouteille dans le dos
et tuyau qui pend en boucle, ceinture de plombs, semelles de plomb, filin enroulé à la hanche. Proportions stylisées
des personnages joués (plan 25 S2). Toile grise dominante, laiton et cuivre en accent.
"""
from __future__ import annotations

import numpy as np

from ._body import LimbStyle, limbs
from ..palette import make_emissive, make_material
from ..poses import Gait, playable_animations
from ..render import Part
from ..rig import Proportions, Skeleton
from ..sdf import capsule, cylinder, ellipsoid, rotation_x, rounded_box, sphere

CHARACTER_ID = "scaphandriere"
FRAME_SIZE = (42, 44)
FRAME_PIVOT = (21.0, 40.0)
# La tête porte le casque : son rayon est celui du cuivre, plus gros que la tête des autres personnages.
DIMENSIONS = Proportions(ankle=2.8, shin=8.8, thigh=9.4, spine=11.0, neck=0.6, head_radius=7.6, shoulder_half=7.4,
                         hip_half=3.6, upper_arm=8.0, forearm=7.0)

SUIT, SEAM, COPPER, BRASS, GLASS, GLINT, PATINA, BOLT, TANK, HOSE, LEAD, BELT, GLOVES, BOOTS, ROPE = range(15)
MATERIALS = [
    make_material("suit", "#A08E6E"),
    make_material("seam", "#7A6A50"),
    make_material("copper", "#B8743A", contrast=1.2),
    make_material("brass", "#C8A050", contrast=1.15),
    make_material("glass", "#1E3438", contrast=0.5),
    make_emissive("glint", "#CDE8E0"),
    make_material("patina", "#5E9A84", contrast=0.8),
    make_material("bolt", "#7A5A30", contrast=0.6),
    make_material("tank", "#A88A4A", contrast=1.15),
    make_material("hose", "#3A3A3E", contrast=1.1),
    make_material("lead", "#5A5E66", contrast=1.2),
    make_material("belt", "#4A3A2C"),
    make_material("gloves", "#4E4A44", contrast=1.1),
    make_material("boots", "#3E3C3A", contrast=1.1),
    make_material("rope", "#B8A47A", contrast=0.9),
]


def build(skeleton: Skeleton) -> list[Part]:
    s = skeleton
    # Tuyau et filin ballottent en retard sur le corps.
    d = s.drape
    k = DIMENSIONS.head_radius / 6.2

    def h(x: float, y: float, z: float) -> np.ndarray:
        return s.on_head((x * k, y * k, z * k))

    torso_center = (s.point("pelvis") + s.point("chest")) * 0.5
    # Le hublot regarde devant : disque d'axe z dans le repère du casque.
    porthole = s.head @ rotation_x(np.pi / 2)
    # Tuyau d'air : de l'arrière du casque, il pend en boucle sur le flanc droit jusqu'au pied de la bouteille.
    hose = [h(-3.4, -2.8, -4.4), s.on_torso("chest", (-6.6, -0.5, -6.0)),
            s.on_torso("chest", (-9.0 - 0.6 * d, -6.5, -5.0)), s.on_torso("chest", (-8.4 - 0.9 * d, -12.0, -5.6)),
            s.on_torso("chest", (-4.0 - 0.4 * d, -12.4, -7.6)), s.on_torso("chest", (-1.2, -10.0, -7.4))]
    parts = [
        # Scaphandre de toile épaisse, gonflé, coutures plus sombres sur le devant.
        Part(lambda p, c=torso_center: ellipsoid(p, c, (6.6, 7.6, 5.0), s.torso), SUIT),
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 1.0, 0)), s.on_torso("pelvis", (0, -3.6, 0)), 5.4, 5.6), SUIT),
        Part(lambda p: capsule(p, s.on_torso("chest", (0, -2.0, 4.9)), s.on_torso("pelvis", (0, 1.0, 5.2)), 0.5), SEAM),
        # Ceinture de plombs : sangle de cuir et quatre pains de plomb gris sur le devant et les flancs.
        Part(lambda p: ellipsoid(p, s.on_torso("pelvis", (0, 1.6, 0)), (6.0, 1.3, 5.4), s.torso), BELT),
        Part(lambda p: rounded_box(p, s.on_torso("pelvis", (-2.6, 1.6, 5.2)), (1.5, 1.6, 0.9), 0.3, s.torso), LEAD),
        Part(lambda p: rounded_box(p, s.on_torso("pelvis", (2.6, 1.6, 5.2)), (1.5, 1.6, 0.9), 0.3, s.torso), LEAD),
        Part(lambda p: rounded_box(p, s.on_torso("pelvis", (-5.8, 1.6, 2.0)), (0.9, 1.6, 1.5), 0.3, s.torso), LEAD),
        Part(lambda p: rounded_box(p, s.on_torso("pelvis", (5.8, 1.6, 2.0)), (0.9, 1.6, 1.5), 0.3, s.torso), LEAD),
        # Filin de sécurité enroulé à la hanche gauche, qui se balance.
        Part(lambda p: np.abs(ellipsoid(p, s.on_torso("pelvis", (7.0 + 0.4 * d, -1.4, 1.4)), (1.2, 3.2, 3.0), s.torso)) - 0.8, ROPE),
        # Pèlerine de laiton boulonnée sur les épaules, plus large que le buste : elle porte le casque.
        Part(lambda p: ellipsoid(p, s.on_torso("neck", (0, -2.2, 0)), (9.0, 3.4, 7.4), s.torso), BRASS),
        *[Part(lambda p, a=a: sphere(p, s.on_torso("neck", (np.sin(a) * 8.0, -1.8, np.cos(a) * 6.4)), 0.9), BOLT)
          for a in (-1.9, -0.9, 0.0, 0.9, 1.9)],
        # Casque de cuivre : grosse sphère, collerette, hublot grillagé devant, deux hublots latéraux, robinet au sommet.
        Part(lambda p: sphere(p, h(0, 0.4, 0), 6.2 * k), COPPER),
        Part(lambda p: ellipsoid(p, h(0, -5.0, 0), (5.2 * k, 1.4 * k, 5.0 * k), s.head), BRASS),
        Part(lambda p: np.maximum(cylinder(p, h(0, 0.8, 5.6), 4.1 * k, 1.2 * k, 0.3, porthole),
                                  -cylinder(p, h(0, 0.8, 5.6), 3.2 * k, 2.0 * k, 0.0, porthole)), BRASS),
        Part(lambda p: cylinder(p, h(0, 0.8, 5.4), 3.3 * k, 1.0 * k, 0.2, porthole), GLASS),
        Part(lambda p: sphere(p, h(-1.5, 2.2, 6.5), 0.7 * k), GLINT),
        Part(lambda p: sphere(p, h(5.6, 0.8, 2.0), 1.5 * k), BRASS),
        Part(lambda p: sphere(p, h(5.9, 0.8, 2.2), 1.0 * k), GLASS),
        Part(lambda p: sphere(p, h(-5.6, 0.8, 2.0), 1.5 * k), BRASS),
        Part(lambda p: sphere(p, h(-5.9, 0.8, 2.2), 1.0 * k), GLASS),
        Part(lambda p: capsule(p, h(0, 5.6, -0.4), h(0, 7.4, -0.6), 0.9 * k, 0.7 * k), BRASS),
        # Vert-de-gris : la mer qui n'est plus là a laissé ses taches sur le cuivre.
        Part(lambda p: ellipsoid(p, h(-4.4, 3.4, -2.2), (1.8 * k, 1.3 * k, 1.6 * k), s.head), PATINA),
        Part(lambda p: ellipsoid(p, h(2.2, 5.2, -2.4), (1.6 * k, 1.0 * k, 1.5 * k), s.head), PATINA),
        Part(lambda p: ellipsoid(p, h(4.6, -2.0, -3.0), (1.3 * k, 1.1 * k, 1.3 * k), s.head), PATINA),
        # Bouteille d'air dans le dos, robinet de laiton.
        Part(lambda p: capsule(p, s.on_torso("chest", (0, -9.0, -6.6)), s.on_torso("chest", (0, 0.4, -6.6)), 3.4), TANK),
        Part(lambda p: capsule(p, s.on_torso("chest", (0, 0.4, -6.6)), s.on_torso("chest", (0, 3.0, -6.6)), 1.0), BRASS),
        *[Part(lambda p, a=a, b=b: capsule(p, a, b, 1.15), HOSE) for a, b in zip(hose, hose[1:])],
    ]
    parts += limbs(s, LimbStyle(sleeve=SUIT, hand=GLOVES, leg=SUIT, boot=BOOTS, arm_radius=2.6, hand_radius=2.6,
                                leg_radius=2.8, boot_radius=3.2, boot_height=5.0, foot_radius=3.0))
    for side in ("l", "r"):
        elbow, hand = s.point(f"elbow_{side}"), s.point(f"hand_{side}")
        start, end = elbow * 0.45 + hand * 0.55, elbow * 0.15 + hand * 0.85
        ankle, toe = s.point(f"ankle_{side}"), s.point(f"toe_{side}")
        parts += [
            # Gants épais remontés sur l'avant-bras.
            Part(lambda p, a=start, b=end: capsule(p, a, b, 2.7, 2.6), GLOVES),
            # Semelles de plomb : une dalle grise sous chaque botte.
            Part(lambda p, a=ankle, b=toe: capsule(p, a + [0, -2.2, -0.8], b + [0, -1.4, 0.6], 2.4), LEAD),
        ]
    return parts


ANIMATIONS = playable_animations(Gait(lean=0.06, arm_out=0.32, stride=0.8, arm_swing=0.7, bounce=0.5, heavy=0.8))
