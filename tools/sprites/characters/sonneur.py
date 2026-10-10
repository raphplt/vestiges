"""
Le Sonneur — Baptiste Roux, sonneur de Saint-Aubin (lore §5) : la nuit du 14, on l'a empêché de sonner le tocsin ;
ensuite il l'a sonné chaque 14 novembre, jusqu'à ce qu'on oublie l'homme, puis la cloche. Il porte encore les deux :
la corde coupée enroulée en bandoulière, le bout effiloché qui pend jusqu'au genou, et une cloche de bronze sur le
dos dont le dôme dépasse au-dessus de l'épaule. Béret, long manteau sombre de bedeau, col de chemise blanc.
Proportions stylisées des personnages joués (plan 25 S2), un peu plus grand que le Vagabond. Bleu-noir dominant,
bronze en accent.
"""
from __future__ import annotations

import numpy as np

from ._body import LimbStyle, limbs
from ..palette import make_material
from ..poses import Gait, playable_animations
from ..render import Part
from ..rig import Proportions, Skeleton
from ..sdf import capsule, cylinder, ellipsoid, rotation_x, rotation_z, sphere

CHARACTER_ID = "sonneur"
FRAME_SIZE = (42, 46)
FRAME_PIVOT = (21.0, 42.0)
DIMENSIONS = Proportions(ankle=2.8, shin=9.8, thigh=10.2, spine=11.8, neck=1.3, head_radius=6.6, shoulder_half=7.0,
                         hip_half=3.2, upper_arm=8.4, forearm=7.4)

COAT, COLLAR, TROUSERS, BOOTS, SKIN, EYES, HAIR, BERET, ROPE, FRAY, BRONZE, BELL_IN, CLAPPER = range(13)
MATERIALS = [
    make_material("coat", "#3A3F56", contrast=1.15),
    make_material("collar", "#E4E0D6", contrast=0.5),
    make_material("trousers", "#34323A"),
    make_material("boots", "#4A3426", contrast=1.1),
    make_material("skin", "#C8906E", contrast=0.8),
    make_material("eyes", "#1E1614", contrast=0.3),
    make_material("hair", "#6A625C"),
    make_material("beret", "#24242C", contrast=1.1),
    make_material("rope", "#C8B07E", contrast=1.0),
    make_material("fray", "#E0D0A0", contrast=0.6),
    make_material("bronze", "#B47E3A", contrast=1.25),
    make_material("bell_in", "#3A2A1E", contrast=0.5),
    make_material("clapper", "#5A4A3A"),
]


def _bell_hollow(p: np.ndarray, top: np.ndarray, axis: np.ndarray, height: float, radius: float) -> np.ndarray:
    mouth = top + axis * height
    return capsule(p, mouth - axis * radius * 0.35, mouth + axis * radius, radius * 0.8, radius * 0.9)


def _bell(p: np.ndarray, top: np.ndarray, axis: np.ndarray, height: float, radius: float) -> np.ndarray:
    """Cloche : flanc évasé du cerveau à la pince, lèvre épaissie, bouche creuse ; `axis` (unitaire) va du sommet vers
    la bouche."""
    mouth = top + axis * height
    crown = top + axis * radius * 0.62
    body = np.minimum(sphere(p, crown, radius * 0.62), capsule(p, crown, mouth - axis * radius * 0.2, radius * 0.62, radius))
    lip = capsule(p, mouth - axis * radius * 0.22, mouth, radius * 1.08, radius * 1.08)
    return np.maximum(np.minimum(body, lip), -_bell_hollow(p, top, axis, height, radius))


def build(skeleton: Skeleton) -> list[Part]:
    s = skeleton
    # Bout de corde et cloche ballottent en retard sur le corps.
    d = s.drape
    k = DIMENSIONS.head_radius / 6.2

    def h(x: float, y: float, z: float) -> np.ndarray:
        return s.on_head((x * k, y * k, z * k))

    torso_center = (s.point("pelvis") + s.point("chest")) * 0.5
    # Cloche sanglée haut dans le dos, au-dessus de l'épaule gauche, penchée vers l'extérieur : de face, son profil
    # (dôme, flanc évasé, lèvre) dépasse à côté de la tête et sa bouche sombre se voit.
    bell_top = s.on_torso("chest", (8.4 + 0.3 * d, 13.0, -5.2))
    bell_axis = s.torso @ np.array([0.38 + 0.12 * d, -1.0, 0.18])
    bell_axis = bell_axis / np.linalg.norm(bell_axis)
    bell_mouth = bell_top + bell_axis * 9.5
    # Corde en bandoulière, de l'épaule gauche à la hanche droite : deux tours serrés.
    coil = [((5.4, -0.4, 2.6), (-4.6, -10.4, 4.2)), ((6.4, -2.2, 1.6), (-3.2, -12.0, 4.0))]
    parts = [
        # Long manteau de bedeau jusqu'au genou, col de chemise blanc.
        Part(lambda p, c=torso_center: ellipsoid(p, c, (5.8, 7.6, 4.0), s.torso), COAT),
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 1.5, 0)), s.on_torso("pelvis", (0.4 * d, -9.5, -0.8)), 4.8, 6.0), COAT),
        Part(lambda p: ellipsoid(p, s.on_torso("neck", (0, -0.6, 0.8)), (4.2, 1.6, 4.0), s.torso), COLLAR),
        # Visage, yeux en amande, cheveux poivre et sel sur la nuque.
        Part(lambda p: sphere(p, h(0, -0.4, 0.4), 5.6 * k), SKIN),
        Part(lambda p: ellipsoid(p, h(-2.1, 0.4, 5.3), (1.0 * k, 1.6 * k, 0.9 * k), s.head), EYES),
        Part(lambda p: ellipsoid(p, h(2.1, 0.4, 5.3), (1.0 * k, 1.6 * k, 0.9 * k), s.head), EYES),
        Part(lambda p: np.maximum(ellipsoid(p, h(0, 0.2, -2.2), (5.8 * k, 4.2 * k, 4.4 * k), s.head),
                                  -ellipsoid(p, h(0, 0.0, 2.6), (5.0 * k, 6.0 * k, 4.6 * k), s.head)), HAIR),
        # Béret penché sur l'oreille droite, avec sa queue au sommet.
        Part(lambda p: cylinder(p, h(-0.8, 4.4, -0.4), 6.2 * k, 1.1 * k, 1.0 * k, s.head @ rotation_z(-0.22) @ rotation_x(-0.1)), BERET),
        Part(lambda p: capsule(p, h(-1.0, 5.4, -0.4), h(-1.2, 6.8, -0.6), 0.5 * k), BERET),
        # Cloche de bronze, battant qui pend dans la bouche, anse au sommet.
        # L'intérieur creux, sombre, se voit de dos et d'en dessous : à égalité de distance, il passe avant le bronze.
        Part(lambda p: np.maximum(_bell(p, bell_top, bell_axis, 9.5, 5.4),
                                  _bell_hollow(p, bell_top, bell_axis, 9.5, 5.4) - 0.3), BELL_IN),
        Part(lambda p: _bell(p, bell_top, bell_axis, 9.5, 5.4), BRONZE),
        Part(lambda p: sphere(p, bell_mouth - bell_axis * 1.0, 1.4), CLAPPER),
        Part(lambda p: np.abs(ellipsoid(p, bell_top - bell_axis * 1.2, (1.6, 1.6, 0.6), s.torso)) - 0.5, BRONZE),
        *[Part(lambda p, a=a, b=b: capsule(p, s.on_torso("chest", a), s.on_torso("chest", b), 1.3), ROPE) for a, b in coil],
        *[Part(lambda p, a=a, b=b: capsule(p, s.on_torso("chest", (a[0], a[1], -a[2])), s.on_torso("chest", (b[0], b[1], -b[2] - 1.0)), 1.3), ROPE)
          for a, b in coil],
        # Bout coupé qui pend de la hanche droite jusqu'au genou, effiloché.
        Part(lambda p: capsule(p, s.on_torso("chest", (-4.4, -11.4, 4.0)), s.on_torso("pelvis", (-5.6 - 0.8 * d, -9.0, 3.0 - 1.2 * d)), 1.2), ROPE),
        *[Part(lambda p, a=a: capsule(p, s.on_torso("pelvis", (-5.6 - 0.8 * d, -9.0, 3.0 - 1.2 * d)),
                                        s.on_torso("pelvis", (-5.6 - 0.8 * d + a, -11.4, 3.0 - 1.4 * d + a * 0.5)), 0.6, 0.35), FRAY)
          for a in (-1.2, 0.0, 1.2)],
    ]
    parts += limbs(s, LimbStyle(sleeve=COAT, hand=SKIN, leg=TROUSERS, boot=BOOTS, arm_radius=2.2, hand_radius=2.2,
                                leg_radius=2.2, boot_radius=2.4, boot_height=4.4, foot_radius=2.2))
    return parts


ANIMATIONS = playable_animations(Gait(lean=0.1, arm_out=0.3, stride=1.0, arm_swing=0.9, bounce=0.7, heavy=0.3))
