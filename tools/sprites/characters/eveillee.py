"""
L'Éveillée — « J'entends ce lieu tel qu'il était, tel qu'il est et tel qu'il aurait pu être. »
Mireille Aymard, standardiste de la mairie : la nuit du 14, elle a reçu les appels du port et ne les a pas passés ;
ensuite, elle répétait les voix à voix haute dans la rue (lore §5). Casque d'opératrice : écouteur sur l'oreille,
cornet de laiton pendu sur la poitrine ; cheveux longs qui flottent vers le haut et cordons du standard qui
s'envolent autour d'elle dans un vent qui n'existe pas, fiches de laiton au bout, lueur d'Essence aux mains et aux
fiches. Robe longue plissée dont l'ourlet est emporté sur le côté, gilet, col blanc. Le contour « double » de la fiche n'est pas dans le modèle : un écho décalé passerait
devant elle dès qu'elle tourne le dos ; c'est un double transparent qui la suit en retard, à faire en jeu (plan 32).
Proportions stylisées des personnages joués (plan 25 S2). Blanc-bleu dominant, cyan Essence en accent.
"""
from __future__ import annotations

import numpy as np

from ._body import LimbStyle, limbs
from ..palette import make_emissive, make_material
from ..poses import Gait, playable_animations
from ..render import Part
from ..rig import Proportions, Skeleton
from ..sdf import capsule, ellipsoid, sphere

CHARACTER_ID = "eveillee"
FRAME_SIZE = (42, 44)
FRAME_PIVOT = (21.0, 40.0)
DIMENSIONS = Proportions(ankle=2.8, shin=9.4, thigh=9.9, spine=11.4, neck=1.4, head_radius=6.6, shoulder_half=5.8,
                         hip_half=3.0, upper_arm=7.8, forearm=6.9)

DRESS, HEM, CARDIGAN, COLLAR, SKIN, HAIR, EYES, BAKELITE, CORD, BRASS, GLOW, SHOES = range(12)
MATERIALS = [
    make_material("dress", "#BAC2D4", contrast=0.8),
    make_material("hem", "#9AA4BC"),
    make_material("cardigan", "#7C88A8", contrast=1.05),
    make_material("collar", "#ECE8E0", contrast=0.5),
    make_material("skin", "#D0A890", contrast=0.8),
    make_material("hair", "#62587A", contrast=1.1),
    make_material("eyes", "#2A2030", contrast=0.3),
    make_material("bakelite", "#2A2428", contrast=0.8),
    make_material("cord", "#9A4A50", contrast=0.9),
    make_material("brass", "#C8A050", contrast=1.0),
    make_emissive("glow", "#5EC4C4"),
    make_material("shoes", "#4A4458"),
]


def _lock(p: np.ndarray, a: np.ndarray, b: np.ndarray, c: np.ndarray, radius: float) -> np.ndarray:
    """Mèche : deux segments qui s'effilent de la racine à la pointe."""
    return np.minimum(capsule(p, a, b, radius, radius * 0.6), capsule(p, b, c, radius * 0.6, 0.4))


def build(skeleton: Skeleton) -> list[Part]:
    s = skeleton
    # La robe, les mèches et les cordons flottent en retard sur le corps, même à l'arrêt.
    d = s.drape
    k = DIMENSIONS.head_radius / 6.2

    def h(x: float, y: float, z: float) -> np.ndarray:
        return s.on_head((x * k, y * k, z * k))

    torso_center = (s.point("pelvis") + s.point("chest")) * 0.5
    waist = s.on_torso("pelvis", (0, 1.5, 0))
    # L'ourlet est emporté sur le côté par un vent qui n'existe pas, et ondule avec lui.
    hem = s.on_torso("pelvis", (2.0 + 1.2 * d, -17.0, -2.0 - 1.0 * d))

    def skirt(p: np.ndarray) -> np.ndarray:
        """Robe évasée jusqu'aux chevilles, plissée : les plis s'ouvrent vers l'ourlet."""
        axis = hem - waist
        t = np.clip((p - waist) @ axis / (axis @ axis), 0.0, 1.0)
        local = (p - waist) @ s.torso
        pleats = np.sin(np.arctan2(local[:, 0], local[:, 2]) * 7.0) * 0.35 * t
        return capsule(p, waist, hem, 3.8, 6.8) + pleats

    parts = [
        Part(lambda p, c=torso_center: ellipsoid(p, c, (4.8, 7.4, 3.6), s.torso), CARDIGAN),
        # Gilet long jusqu'aux hanches, robe qui masque la marche et donne l'impression de flotter, ourlet plus sombre.
        Part(lambda p: capsule(p, waist, s.on_torso("pelvis", (0, -2.5, -0.2)), 4.6, 5.2), CARDIGAN),
        Part(lambda p: np.maximum(skirt(p), -((p - hem) @ (hem - waist) / np.linalg.norm(hem - waist) + 2.2)), HEM),
        Part(skirt, DRESS),
        Part(lambda p: sphere(p, h(0, -0.4, 0.4), 5.6 * k), SKIN),
        # Cheveux tirés en arrière, qui dégagent le visage vu d'en haut.
        Part(lambda p: np.maximum(ellipsoid(p, h(0, 0.9, -1.4), (6.2 * k, 5.7 * k, 6.0 * k), s.head),
                                  -ellipsoid(p, h(0, -1.0, 4.4), (4.8 * k, 5.2 * k, 3.6 * k), s.head)), HAIR),
        # Col blanc, gilet boutonné sur une robe claire : le buste se détache de la jupe.
        Part(lambda p: ellipsoid(p, s.on_torso("neck", (0, -0.8, 0.8)), (4.2, 1.6, 3.8), s.torso), COLLAR),
        Part(lambda p: ellipsoid(p, s.on_torso("chest", (0, -6.0, 3.0)), (2.0, 3.8, 1.2), s.torso), DRESS),
        # Yeux en amande verticale : un rond d'un pixel se perd entre deux pixels du visage.
        Part(lambda p: ellipsoid(p, h(-2.1, 0.0, 5.3), (1.1 * k, 1.7 * k, 1.0 * k), s.head), EYES),
        Part(lambda p: ellipsoid(p, h(2.1, 0.0, 5.3), (1.1 * k, 1.7 * k, 1.0 * k), s.head), EYES),
        # Mèches longues qui flottent vers le haut, toutes emportées sur sa gauche par le même vent que l'ourlet, comme
        # sous l'eau : une flamme de cheveux plutôt que deux pointes symétriques.
        *[Part(lambda p, a=a, b=b, c=c: _lock(p, h(*a), h(*b), h(*c), 1.7 * k), HAIR) for a, b, c in (
            ((-3.0, 2.0, -3.0), (-1.6 + 0.6 * d, 7.6, -5.6), (1.4 + 1.2 * d, 12.0 - 0.6 * d, -7.0)),
            ((-0.4, 3.0, -3.4), (2.4 + 0.8 * d, 8.0, -5.4), (6.0 + 1.6 * d, 10.6 - 0.6 * d, -5.0)),
            ((2.6, 2.2, -3.4), (6.0 + 0.8 * d, 5.8, -5.0), (9.4 + 1.2 * d, 7.0 - 0.8 * d, -4.0)),
            ((0.0, 0.0, -5.4), (2.0 - 0.6 * d, 3.6, -9.4), (5.6 - 1.4 * d, 5.2 - 0.8 * d, -11.0)))],
        # Casque d'opératrice : serre-tête, écouteur sur l'oreille droite, cornet pendu sur la poitrine.
        Part(lambda p: np.abs(ellipsoid(p, h(0, 0.6, -0.4), (6.3 * k, 6.4 * k, 1.0 * k), s.head)) - 0.35 * k, BAKELITE),
        Part(lambda p: ellipsoid(p, h(-6.4, -0.4, 0.4), (1.4 * k, 2.3 * k, 2.3 * k), s.head), BAKELITE),
        Part(lambda p: capsule(p, s.on_torso("neck", (-3.6, -0.4, 2.4)), s.on_torso("chest", (0, -2.6, 4.6)), 0.5), BAKELITE),
        Part(lambda p: capsule(p, s.on_torso("neck", (3.6, -0.4, 2.4)), s.on_torso("chest", (0, -2.6, 4.6)), 0.5), BAKELITE),
        Part(lambda p: capsule(p, s.on_torso("chest", (0, -3.6, 4.2)), s.on_torso("chest", (0, -0.6, 7.0)), 0.9, 2.2), BRASS),
        Part(lambda p: sphere(p, s.on_torso("chest", (0, -0.2, 7.6)), 1.3), BAKELITE),
    ]
    # Cordons du standard : l'un s'envole de la main gauche par-dessus l'épaule, l'autre traîne derrière la main droite ;
    # fiche de laiton et lueur au bout, comme un appel resté en attente.
    hand_l, hand_r = s.point("hand_l"), s.point("hand_r")
    cords = [
        [hand_l, hand_l + s.torso @ np.array([2.6, -1.5, -0.5]),
         hand_l + s.torso @ np.array([6.0 + 0.8 * d, 4.0, -2.5]),
         hand_l + s.torso @ np.array([7.0 + 1.4 * d, 11.0 - 0.8 * d, -4.5]),
         hand_l + s.torso @ np.array([5.0 + 2.0 * d, 18.0 - 1.2 * d, -6.0 - 1.0 * d])],
        [hand_r, hand_r + s.torso @ np.array([-1.6, -3.4, -1.8]),
         hand_r + s.torso @ np.array([-3.4 - 0.6 * d, -3.0, -6.0]),
         hand_r + s.torso @ np.array([-5.0 - 1.2 * d, 0.6 - 0.6 * d, -9.6 - 0.8 * d])],
    ]
    for cord in cords:
        heading = (cord[-1] - cord[-2]) / np.linalg.norm(cord[-1] - cord[-2])
        tip = cord[-1] + heading * 1.8
        parts += [Part(lambda p, a=a, b=b: capsule(p, a, b, 0.75), CORD) for a, b in zip(cord, cord[1:])]
        parts += [
            Part(lambda p, a=cord[-1], b=tip: capsule(p, a, b, 1.0, 0.8), BRASS),
            Part(lambda p, c=tip: sphere(p, c, 0.9), GLOW),
        ]
    parts += limbs(s, LimbStyle(sleeve=CARDIGAN, hand=GLOW, leg=DRESS, boot=SHOES, arm_radius=2.1, hand_radius=1.8,
                                leg_radius=1.8, boot_radius=1.7, boot_height=2.2, foot_radius=1.7))
    # Manches amples qui s'évasent aux poignets.
    for side in ("l", "r"):
        elbow, hand = s.point(f"elbow_{side}"), s.point(f"hand_{side}")
        parts.append(Part(lambda p, a=elbow, b=elbow * 0.2 + hand * 0.8: capsule(p, a, b, 2.1, 3.0), CARDIGAN))
    return parts


ANIMATIONS = playable_animations(Gait(lean=-0.02, arm_out=0.4, stride=0.7, arm_swing=0.5, bounce=0.35))
