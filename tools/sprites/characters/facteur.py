"""
Le Facteur sans destination — « Il y a forcément quelqu'un qui attend ces lettres. »
Aimé Ribot a porté le télégramme de tempête à la mairie le matin du 14 ; ensuite, des années durant, il a distribué le
courrier du Bas-Port à des adresses que la ville avait rayées (lore §5). Casquette des postes à visière et insigne,
grosse moustache grise, sacoche de cuir énorme qui déborde de lettres et de liasses ficelées, lettres qui s'envolent
derrière lui, patins bricolés (planchettes et roulements), posture de patineur penchée en avant. Proportions stylisées
des personnages joués (plan 25 S2). Bleu postal délavé dominant, jaune de sacoche en accent.
"""
from __future__ import annotations

import numpy as np

from ._body import LimbStyle, limbs
from ..palette import make_material
from ..poses import Gait, playable_animations
from ..render import Part
from ..rig import Proportions, Skeleton
from ..sdf import capsule, cylinder, ellipsoid, rotation_x, rotation_z, rounded_box, sphere

CHARACTER_ID = "facteur"
FRAME_SIZE = (42, 44)
FRAME_PIVOT = (21.0, 40.0)
# Les patins le surélèvent : la cheville plus haute pose les roues au sol.
DIMENSIONS = Proportions(ankle=5.0, shin=8.6, thigh=9.4, spine=11.0, neck=1.3, head_radius=6.8, shoulder_half=6.4,
                         hip_half=3.1, upper_arm=7.8, forearm=6.9)

(JACKET, COLLAR, TROUSERS, SHOES, SKIN, MOUSTACHE, HAIR, EYES, CAP, VISOR, BADGE, BAG, FLAP, LETTER, STRING, STRAP, BOARD,
 WHEEL) = range(18)
MATERIALS = [
    make_material("jacket", "#4E6A8C", contrast=1.1),
    make_material("collar", "#3A4E6C"),
    make_material("trousers", "#3A4456"),
    make_material("shoes", "#2E2826", contrast=1.1),
    make_material("skin", "#C8906C", contrast=0.8),
    make_material("moustache", "#B4ACA4", contrast=0.8),
    make_material("hair", "#6E6864"),
    make_material("eyes", "#1E1614", contrast=0.3),
    make_material("cap", "#2E3C58", contrast=1.15),
    make_material("visor", "#1E2230", contrast=0.6),
    make_material("badge", "#E0B040", contrast=0.8),
    make_material("bag", "#C0923A", contrast=1.15),
    make_material("flap", "#8E6A2C", contrast=1.1),
    make_material("letter", "#ECE4D6", contrast=0.5),
    make_material("string", "#C44A3A", contrast=0.8),
    make_material("strap", "#6A4A26"),
    make_material("board", "#B08A58", contrast=1.1),
    make_material("wheel", "#8C8888", contrast=1.2),
]


def build(skeleton: Skeleton) -> list[Part]:
    s = skeleton
    # Lettres envolées et liasses de la sacoche ballottent en retard sur le corps.
    d = s.drape
    k = DIMENSIONS.head_radius / 6.2

    # Penché comme un patineur, il garde la tête droite et regarde devant lui.
    head = s.head @ rotation_x(-0.24)

    def h(x: float, y: float, z: float) -> np.ndarray:
        return s.point("head") + head @ np.array([x * k, y * k, z * k])

    def letter(center, angle: float, tilt: float = 0.0, size=(0.35, 2.2, 1.7)):
        """Enveloppe : plaque mince tournée de `angle` dans le plan du torse, inclinée de `tilt` vers l'avant."""
        rotation = s.torso @ rotation_z(angle) @ rotation_x(tilt)
        return lambda p: rounded_box(p, center, size, 0.1, rotation)

    torso_center = (s.point("pelvis") + s.point("chest")) * 0.5
    bag = s.on_torso("pelvis", (8.6 + 0.3 * d, 2.0, 1.4))
    parts = [
        # Veste d'uniforme boutonnée, col rabattu plus sombre, deux boutons dorés.
        Part(lambda p, c=torso_center: ellipsoid(p, c, (5.6, 7.4, 4.0), s.torso), JACKET),
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 1.5, 0)), s.on_torso("pelvis", (0, -4.0, -0.2)), 4.6, 5.0), JACKET),
        Part(lambda p: ellipsoid(p, s.on_torso("neck", (0, -0.8, 0.4)), (5.0, 2.0, 4.6), s.torso), COLLAR),
        Part(lambda p: sphere(p, s.on_torso("chest", (-1.2, -3.0, 3.9)), 0.8), BADGE),
        Part(lambda p: sphere(p, s.on_torso("chest", (-1.2, -7.0, 3.7)), 0.8), BADGE),
        # Visage : yeux, grosse moustache grise qui tombe aux coins, favoris.
        Part(lambda p: sphere(p, h(0, -0.4, 0.4), 5.6 * k), SKIN),
        # Yeux en amande verticale : un rond d'un pixel se perd entre deux pixels du visage.
        Part(lambda p: ellipsoid(p, h(-2.1, 0.6, 5.3), (1.0 * k, 1.6 * k, 0.9 * k), head), EYES),
        Part(lambda p: ellipsoid(p, h(2.1, 0.6, 5.3), (1.0 * k, 1.6 * k, 0.9 * k), head), EYES),
        Part(lambda p: ellipsoid(p, h(0, -1.6, 5.2), (3.4 * k, 1.1 * k, 1.4 * k), head), MOUSTACHE),
        Part(lambda p: capsule(p, h(-2.6, -1.8, 4.8), h(-3.4, -3.6, 4.0), 0.9 * k, 0.6 * k), MOUSTACHE),
        Part(lambda p: capsule(p, h(2.6, -1.8, 4.8), h(3.4, -3.6, 4.0), 0.9 * k, 0.6 * k), MOUSTACHE),
        # Cheveux gris coupés court sous la casquette, favoris.
        Part(lambda p: np.maximum(ellipsoid(p, h(0, 1.0, -1.6), (5.6 * k, 4.0 * k, 4.6 * k), head),
                                  -ellipsoid(p, h(0, -0.4, 3.0), (4.8 * k, 5.4 * k, 4.2 * k), head)), HAIR),
        # Casquette des postes : bandeau droit, calotte plate un peu évasée, visière noire et insigne doré.
        Part(lambda p: cylinder(p, h(0, 5.0, -0.6), 5.0 * k, 1.4 * k, 0.4, head @ rotation_x(-0.1)), CAP),
        Part(lambda p: cylinder(p, h(0, 6.8, -1.0), 5.6 * k, 0.8 * k, 0.6, head @ rotation_x(-0.16)), CAP),
        Part(lambda p: ellipsoid(p, h(0, 4.2, 4.8), (4.0 * k, 0.5 * k, 1.7 * k), head @ rotation_x(0.2)), VISOR),
        Part(lambda p: sphere(p, h(0, 5.4, 4.6), 1.0 * k), BADGE),
        # Bandoulière de l'épaule droite à la hanche gauche.
        Part(lambda p: capsule(p, s.on_torso("neck", (-4.6, -1.0, 2.4)), s.on_torso("pelvis", (5.6, 4.0, 3.6)), 0.9), STRAP),
        Part(lambda p: capsule(p, s.on_torso("neck", (-4.6, -1.0, 2.4)), s.on_torso("chest", (-3.0, -2.0, -4.4)), 0.9), STRAP),
        # Sacoche énorme sur la hanche gauche : rabat ouvert, boucle dorée, lettres qui débordent en éventail.
        Part(lambda p: rounded_box(p, bag, (3.0, 5.6, 6.4), 1.6, s.torso), BAG),
        Part(lambda p: rounded_box(p, bag + s.torso @ [0.8, 2.4, 0.0], (2.6, 3.6, 6.6), 1.2, s.torso), FLAP),
        Part(lambda p: sphere(p, bag + s.torso @ [3.4, 0.2, 0.0], 1.0), BADGE),
        # Le bouquet de lettres s'évase vers l'extérieur : il élargit la silhouette du côté de la sacoche.
        Part(letter(bag + s.torso @ [0.4, 7.0, 3.8], -0.25, 0.35, (0.4, 2.8, 2.1)), LETTER),
        Part(letter(bag + s.torso @ [1.4, 7.4, 1.0], -0.6, 0.0, (0.4, 2.8, 2.1)), LETTER),
        Part(letter(bag + s.torso @ [2.4, 6.4, -1.8], -0.95, -0.3, (0.4, 2.8, 2.1)), LETTER),
        Part(letter(bag + s.torso @ [0.2, 7.0, -4.4], -0.3, -0.6, (0.4, 2.8, 2.1)), LETTER),
        # Liasse ficelée de rouge qui dépasse du rabat.
        Part(lambda p: rounded_box(p, bag + s.torso @ [-0.2, 6.6, 1.6], (1.4, 1.9, 2.3), 0.3, s.torso), LETTER),
        Part(lambda p: rounded_box(p, bag + s.torso @ [-0.2, 6.6, 1.6], (1.5, 0.4, 2.4), 0.1, s.torso), STRING),
        # Lettres envolées derrière lui : l'élan de la tournée.
        Part(letter(s.on_torso("chest", (8.0 + 1.6 * d, 4.0 + 1.2 * d, -8.0)), 0.7 + 0.3 * d, 0.4), LETTER),
        Part(letter(s.on_torso("chest", (3.0 - 1.0 * d, 9.0 - 0.8 * d, -11.0)), -0.6, 0.9), LETTER),
        Part(letter(s.on_torso("chest", (13.0 + 0.8 * d, 3.0 - 1.0 * d, -3.0)), -1.2 - 0.3 * d, 0.5), LETTER),
    ]
    parts += limbs(s, LimbStyle(sleeve=JACKET, hand=SKIN, leg=TROUSERS, boot=SHOES, arm_radius=2.0, hand_radius=2.0,
                                leg_radius=2.2, boot_radius=2.2, boot_height=3.5, foot_radius=2.1))
    # Patins bricolés : planchette sous la chaussure, deux roues à chaque bout.
    for side in ("l", "r"):
        ankle, toe = s.point(f"ankle_{side}"), s.point(f"toe_{side}")
        parts += [
            Part(lambda p, a=ankle, b=toe: capsule(p, a + [0, -2.2, -2.0], b + [0, -1.0, 1.2], 1.0), BOARD),
            Part(lambda p, a=ankle: sphere(p, a + [0, -3.4, -1.4], 1.5), WHEEL),
            Part(lambda p, b=toe: sphere(p, b + [0, -2.2, 0.4], 1.5), WHEEL),
        ]
    return parts


ANIMATIONS = playable_animations(Gait(lean=0.2, arm_out=0.3, stride=0.85, arm_swing=1.2, bounce=0.6))
