"""
Le Facteur sans destination — « Il y a forcément quelqu'un qui attend ces lettres. »
Aimé Ribot a porté le télégramme de tempête à la mairie le matin du 14 ; ensuite, des années durant, il a distribué le
courrier du Bas-Port à des adresses que la ville avait rayées (lore §5).

Refait de zéro (plan 60 §27) : un vieil homme sec et têtu, voûté par des années de tournée, qui ne s'arrête pas.
Silhouette longue penchée en avant, cou tendu, genoux pliés de patineur. Long nez, moustache de morse qui tombe sous le
menton, sourcils broussailleux, oreilles décollées, casquette des postes trop grande portée de travers. Capote d'uniforme
jusqu'aux genoux, fendue derrière, col relevé, rapiécée. Une longue écharpe flotte derrière lui : vu de dos, comme la
caméra le voit, c'est elle qui dit l'élan. Sacoche énorme et bombée sur la hanche, lettres en éventail, une lettre
toujours tenue à la main (les lettres envolées du sprite 2D, détachées du corps, se lisaient en débris en 3D) ; patins bricolés à grosses roues. Les pièces fines (lettres, écharpe) ont l'épaisseur d'un
pas de maillage au moins, pour sortir entières en 3D.
Bleu postal délavé dominant, jaune de sacoche et rouille de l'écharpe en accents.
"""
from __future__ import annotations

import numpy as np

from ._body import LimbStyle, limbs
from ..palette import make_material
from ..poses import Gait, playable_animations
from ..render import Part
from ..rig import Proportions, Skeleton
from ..sdf import capsule, cylinder, ellipsoid, rotation_x, rotation_y, rotation_z, rounded_box, smax, sphere, tapered_box

CHARACTER_ID = "facteur"
# Haut de 48 : les grosses roues et le corps couché de la mort descendent de 6 px sous le pivot.
FRAME_SIZE = (42, 48)
FRAME_PIVOT = (21.0, 40.0)
# Sec et long : épaules étroites, long cou tendu, grandes mains ; les patins le surélèvent (cheville haute).
DIMENSIONS = Proportions(ankle=5.4, shin=8.8, thigh=9.0, spine=11.6, neck=2.4, head_radius=6.4, shoulder_half=5.6,
                         hip_half=2.9, upper_arm=8.2, forearm=7.4)

(COAT, COLLAR, TROUSERS, SHOES, SKIN, NOSE, MOUSTACHE, HAIR, EYES, CAP, VISOR, BRASS, BAG, FLAP, LETTER, STRING, STRAP,
 BOARD, WHEEL, GLOVE, SCARF, PATCH, BELT) = range(23)
MATERIALS = [
    make_material("coat", "#4A6382", contrast=1.1),
    make_material("collar", "#34465E"),
    make_material("trousers", "#3A3C46"),
    make_material("shoes", "#2C2624", contrast=1.1),
    make_material("skin", "#C08A6A", contrast=0.8),
    make_material("nose", "#B8705A", contrast=0.8),
    make_material("moustache", "#CBC3B6", contrast=0.8),
    make_material("hair", "#A8A098", contrast=0.8),
    make_material("eyes", "#1A1210", contrast=0.3),
    make_material("cap", "#2C3A54", contrast=1.15),
    make_material("visor", "#1A1E28", contrast=0.6),
    make_material("brass", "#D8A838", contrast=0.8, specular=0.4),
    make_material("bag", "#B88A3A", contrast=1.15),
    make_material("flap", "#86622A", contrast=1.1),
    make_material("letter", "#E6DECE", contrast=0.5),
    make_material("string", "#B84434", contrast=0.8),
    make_material("strap", "#5E4224"),
    make_material("board", "#A07E50", contrast=1.1),
    make_material("wheel", "#7E7A78", contrast=1.2, specular=0.5),
    make_material("glove", "#5A4A3C", contrast=0.9),
    make_material("scarf", "#8E4A34", contrast=1.1),
    make_material("patch", "#6A6E52", contrast=0.9),
    make_material("belt", "#3A2A1C", contrast=0.9),
]


def build(skeleton: Skeleton) -> list[Part]:
    s = skeleton
    # Écharpe, pans de la capote et lettres ballottent en retard sur le corps : recul (d), soulèvement (lift), fouet (sway).
    d = s.drape
    lift = s.drape_lift
    sway = s.drape_sway
    k = DIMENSIONS.head_radius / 6.2
    torso = s.torso

    # Voûté, il relève la tête pour regarder devant lui ; elle avance au bout du cou tendu.
    head = s.head @ rotation_x(-0.36)
    head_centre = s.point("head") + torso @ np.array([0.0, -0.6, 1.8])

    def h(x: float, y: float, z: float) -> np.ndarray:
        return head_centre + head @ np.array([x * k, y * k, z * k])

    # La casquette, trop grande, penche sur l'oreille droite.
    cap = head @ rotation_z(-0.14) @ rotation_x(-0.1)

    def c(x: float, y: float, z: float) -> np.ndarray:
        return head_centre + cap @ np.array([x * k, y * k, z * k])

    def t(anchor: str, x: float, y: float, z: float) -> np.ndarray:
        return s.on_torso(anchor, (x, y, z))

    def slab(center, size, rotation):
        """Pièce plate (lettre, rabat) : au moins un pas de maillage d'épaisseur, pour sortir entière en 3D."""
        return lambda p: rounded_box(p, center, size, 0.3, rotation)

    parts: list[Part] = []

    # Capote : buste creusé devant et bossu derrière, jupe jusqu'aux genoux ouverte devant et fendue derrière.
    torso_centre = (s.point("pelvis") + s.point("chest")) * 0.5
    parts += [
        Part(lambda p: ellipsoid(p, torso_centre + torso @ [0, 0.4, 0.2], (5.0, 7.0, 3.6), torso), COAT),
        Part(lambda p: ellipsoid(p, t("chest", 0, -2.4, -1.6), (4.8, 4.6, 3.8), torso), COAT),
    ]
    skirt_top = t("pelvis", 0, 1.6, -0.2)
    skirt_bottom = t("pelvis", 0, -10.4, -1.4 - 1.4 * d)

    def skirt(p):
        body = tapered_box(p, skirt_top, skirt_bottom, 4.6, 6.0, 3.8, 5.0 + 0.6 * d, 0.8)
        local = (p - t("pelvis", 0, 0, 0)) @ torso
        # Ouverture devant (les jambes passent) et fente derrière, plus bas que la ceinture.
        front = np.maximum(np.maximum(np.abs(local[:, 0]) - 1.4, -local[:, 2]), local[:, 1] + 1.0)
        back = np.maximum(np.maximum(np.abs(local[:, 0]) - 0.7, local[:, 2]), local[:, 1] + 4.0)
        return smax(smax(body, -front, 0.4), -back, 0.4)

    parts += [
        Part(skirt, COAT),
        # Ceinture de cuir et sa boucle.
        Part(lambda p: ellipsoid(p, t("pelvis", 0, 1.4, 0.1), (5.0, 1.1, 4.0), torso), BELT),
        Part(lambda p: rounded_box(p, t("pelvis", 0, 1.4, 4.0), (1.2, 1.0, 0.7), 0.3, torso), BRASS),
        # Deux rangées de boutons de laiton.
        *[Part(lambda p, x=x, y=y: sphere(p, t("chest", x, y, 3.2 + 0.1 * y), 0.9), BRASS)
          for x in (-1.8, 1.8) for y in (-1.4, -4.4, -7.4)],
        # Pièce rapportée sur le dos, d'un autre drap.
        Part(lambda p: rounded_box(p, t("chest", 1.6, -5.4, -5.0), (2.2, 2.4, 0.9), 0.4, torso @ rotation_z(0.2)), PATCH),
        # Col relevé contre la nuque.
        Part(lambda p: smax(ellipsoid(p, t("neck", 0, -0.2, -0.6), (4.6, 3.0, 4.2), torso),
                            -ellipsoid(p, t("neck", 0, 1.4, 1.0), (3.4, 3.4, 3.4), torso), 0.3), COLLAR),
    ]

    # Écharpe : un tour au cou, et un long pan qui flotte derrière, plus haut et plus loin à mesure qu'il file.
    tail = [
        t("neck", -1.6, -0.6, -3.4),
        t("chest", -2.4 + 0.4 * sway, -2.0 + 1.4 * lift, -7.0 - 1.6 * d),
        t("chest", -1.4 + 1.2 * sway, -4.8 + 2.4 * lift, -11.0 - 2.8 * d),
        t("chest", -0.2 + 2.0 * sway, -6.4 + 3.4 * lift, -14.6 - 3.6 * d),
    ]
    parts.append(Part(lambda p: ellipsoid(p, t("neck", 0, 0.2, 0.2), (4.0, 1.6, 3.8), torso), SCARF))
    for a, b, ra, rb in zip(tail, tail[1:], (1.5, 1.4, 1.3), (1.4, 1.3, 1.1)):
        parts.append(Part(lambda p, a=a, b=b, ra=ra, rb=rb: capsule(p, a, b, ra, rb), SCARF))
    parts.append(Part(lambda p, e=tail[-1]: rounded_box(p, e, (1.6, 1.0, 1.0), 0.4, torso), SCARF))

    # Tête : crâne long, bajoues, oreilles décollées, long nez au bout rond.
    parts += [
        Part(lambda p: ellipsoid(p, h(0, 0.4, 0), (4.8, 5.6, 4.8), head), SKIN),
        Part(lambda p: ellipsoid(p, h(0, -2.6, 1.6), (4.2, 2.8, 3.6), head), SKIN),
        Part(lambda p: ellipsoid(p, h(4.8, 0.2, -0.4), (1.0, 1.9, 1.5), head @ rotation_y(0.3)), SKIN),
        Part(lambda p: ellipsoid(p, h(-4.8, 0.2, -0.4), (1.0, 1.9, 1.5), head @ rotation_y(-0.3)), SKIN),
        Part(lambda p: capsule(p, h(0, 0.8, 4.2), h(0, -1.0, 7.0), 1.1 * k, 1.4 * k), NOSE),
        Part(lambda p: sphere(p, h(0, -1.2, 7.0), 1.7 * k), NOSE),
        # Yeux enfoncés sous des sourcils broussailleux.
        Part(lambda p: ellipsoid(p, h(-2.0, 1.4, 4.3), (0.9 * k, 1.3 * k, 0.8 * k), head), EYES),
        Part(lambda p: ellipsoid(p, h(2.0, 1.4, 4.3), (0.9 * k, 1.3 * k, 0.8 * k), head), EYES),
        Part(lambda p: ellipsoid(p, h(-2.2, 2.9, 4.4), (2.0, 0.9, 1.2), head @ rotation_z(-0.25)), MOUSTACHE),
        Part(lambda p: ellipsoid(p, h(2.2, 2.9, 4.4), (2.0, 0.9, 1.2), head @ rotation_z(0.25)), MOUSTACHE),
        # Moustache de morse : une masse sous le nez qui tombe en deux crocs sous le menton.
        Part(lambda p: ellipsoid(p, h(0, -2.4, 5.4), (4.0, 1.5, 1.8), head), MOUSTACHE),
        Part(lambda p: capsule(p, h(-2.6, -2.6, 5.0), h(-3.4, -5.6, 3.8), 1.3 * k, 0.8 * k), MOUSTACHE),
        Part(lambda p: capsule(p, h(2.6, -2.6, 5.0), h(3.4, -5.6, 3.8), 1.3 * k, 0.8 * k), MOUSTACHE),
        # Cheveux gris qui dépassent de la casquette, sur la nuque et au-dessus des oreilles.
        Part(lambda p: ellipsoid(p, h(0, 1.2, -3.0), (4.6, 3.6, 2.6), head), HAIR),
        Part(lambda p: ellipsoid(p, h(4.4, 2.4, -0.6), (1.3, 1.6, 2.0), head), HAIR),
        Part(lambda p: ellipsoid(p, h(-4.4, 2.4, -0.6), (1.3, 1.6, 2.0), head), HAIR),
        # Casquette des postes trop grande : bandeau, calotte large et plate, visière tombante, insigne.
        Part(lambda p: cylinder(p, c(0, 5.0, -0.4), 5.4 * k, 1.6 * k, 0.4, cap), CAP),
        Part(lambda p: cylinder(p, c(0, 7.2, -0.8), 6.3 * k, 1.0 * k, 0.8, cap @ rotation_x(-0.08)), CAP),
        Part(lambda p: ellipsoid(p, c(0, 4.3, 5.0), (4.6 * k, 0.7 * k, 2.4 * k), cap @ rotation_x(0.22)), VISOR),
        Part(lambda p: sphere(p, c(0, 5.4, 5.0), 1.2 * k), BRASS),
    ]

    # Bandoulière de l'épaule droite à la hanche gauche, sacoche énorme et bombée, lettres en éventail.
    bag = t("pelvis", 8.4 + 0.4 * d + 0.4 * sway, 0.6 + 0.6 * lift, 0.8)
    bag_rotation = torso @ rotation_z(0.08 * sway)

    def b(x: float, y: float, z: float) -> np.ndarray:
        return bag + bag_rotation @ np.array([x, y, z])

    parts += [
        Part(lambda p: capsule(p, t("neck", -4.4, -1.2, 2.2), t("pelvis", 6.0, 4.6, 3.2), 1.0), STRAP),
        Part(lambda p: capsule(p, t("neck", -4.4, -1.2, 2.2), t("chest", -3.2, -3.0, -4.0), 1.0), STRAP),
        Part(lambda p: ellipsoid(p, bag, (3.6, 5.8, 6.6), bag_rotation), BAG),
        Part(lambda p: rounded_box(p, b(0.4, 3.4, 0.4), (3.4, 2.4, 6.4), 1.4, bag_rotation), FLAP),
        Part(lambda p: rounded_box(p, b(3.6, 2.2, 0.4), (0.6, 1.2, 1.0), 0.3, bag_rotation), BRASS),
    ]
    for x, y, z, angle, tilt in ((0.4, 7.2, 3.6, -0.2, 0.35), (1.0, 7.8, 0.8, -0.5, 0.0), (1.8, 6.8, -2.2, -0.8, -0.3),
                                 (0.0, 7.0, -4.6, -0.3, -0.55)):
        parts.append(Part(slab(b(x, y, z), (1.0, 3.0, 2.3), bag_rotation @ rotation_z(angle) @ rotation_x(tilt)), LETTER))
    # Liasse ficelée de rouge coincée sous le rabat.
    bundle = b(-0.6, 6.0, 1.8)
    parts += [
        Part(lambda p: rounded_box(p, bundle, (1.6, 2.0, 2.4), 0.4, bag_rotation), LETTER),
        Part(lambda p: rounded_box(p, bundle, (1.7, 0.5, 2.5), 0.2, bag_rotation), STRING),
    ]

    # La lettre qu'il tient toujours, à la main droite.
    hand = s.point("hand_r")
    forward = s.hand_dir("r")
    held = hand + forward * 2.6
    parts.append(Part(slab(held, (0.9, 2.6, 2.0), torso @ rotation_x(0.6)), LETTER))

    parts += limbs(s, LimbStyle(sleeve=COAT, hand=GLOVE, leg=TROUSERS, boot=SHOES, arm_radius=1.9, hand_radius=2.5,
                                leg_radius=2.0, boot_radius=2.3, boot_height=3.6, foot_radius=2.2))
    # Revers de manche plus sombres, au poignet.
    for side in ("l", "r"):
        el, ha = s.point(f"elbow_{side}"), s.point(f"hand_{side}")
        cuff = ha + (el - ha) * 0.25
        parts.append(Part(lambda p, a=cuff, e=ha: capsule(p, a, e + (a - e) * 0.4, 2.2, 2.1), COLLAR, anchor=el))

    # Patins bricolés : planche épaisse sous la chaussure, deux grosses roues d'axe latéral à chaque bout.
    wheel_axis = rotation_z(np.pi / 2.0)
    for side in ("l", "r"):
        ankle, toe = s.point(f"ankle_{side}"), s.point(f"toe_{side}")
        heel = ankle + np.array([0.0, -2.4, -2.2])
        front = toe + np.array([0.0, -1.2, 1.4])
        parts += [
            Part(lambda p, a=heel, f=front: tapered_box(p, a, f, 2.0, 2.0, 0.9, 0.9, 0.4), BOARD, anchor=ankle),
            Part(lambda p, a=heel: cylinder(p, a + [0, -1.4, 0.4], 2.2, 1.2, 0.4, wheel_axis), WHEEL, anchor=ankle),
            Part(lambda p, f=front: cylinder(p, f + [0, -1.4, -0.4], 2.2, 1.2, 0.4, wheel_axis), WHEEL, anchor=ankle),
        ]
    return parts


# Patineur voûté : penché, genoux fléchis (un peu de poids), longue glissade, bras qui balancent large.
ANIMATIONS = playable_animations(Gait(lean=0.32, arm_out=0.32, stride=0.95, arm_swing=1.15, bounce=0.5, heavy=0.25))
