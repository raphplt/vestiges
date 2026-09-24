"""
Le Vagabond — « Tant que je marche, le chemin existe. »
Silhouette lisible au sac qui dépasse de la tête : sac débordant (couchage, gamelle, carte roulée, manche d'outil),
capuche, écharpe, manteau recousu. Brun terreux dominant, orange outil en accent (charte §4).
"""
from __future__ import annotations

import numpy as np

from ..palette import make_material
from ..render import Part
from ._body import LimbStyle, limbs
from ..poses import Gait, character_animations
from ..rig import Proportions, Skeleton
from ..sdf import capsule, ellipsoid, rounded_box, sphere

CHARACTER_ID = "vagabond"
DIMENSIONS = Proportions()

COAT, PATCH, TROUSERS, BOOTS, FACE, HOOD, PACK, ACCENT, SCARF, BEDROLL, METAL, GLOVES, MAP = range(13)
MATERIALS = [
    make_material("coat", "#7A5C42"),
    make_material("patch", "#8A6C4C"),
    make_material("trousers", "#4F4A44"),
    make_material("boots", "#3A2E26"),
    make_material("face", "#4A3530", contrast=0.6),
    make_material("hood", "#5E4432"),
    make_material("pack", "#5B4632"),
    make_material("accent", "#D4853A"),
    make_material("scarf", "#A0533A"),
    make_material("bedroll", "#6E7A5A"),
    make_material("metal", "#6B6161"),
    make_material("gloves", "#54402F"),
    make_material("map", "#C4B490"),
]


def build(skeleton: Skeleton) -> list[Part]:
    s = skeleton
    # Éléments souples : pan d'écharpe, pointe de capuche et manche d'outil ballottent en retard sur le corps.
    d = s.drape
    torso_rotation = s.torso
    torso_center = (s.point("pelvis") + s.point("chest")) * 0.5
    parts = [
        Part(lambda p, c=torso_center: ellipsoid(p, c, (5.6, 8.4, 3.9), torso_rotation), COAT),
        # Pan de manteau légèrement évasé, au-dessus du genou.
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 1.5, 0)), s.on_torso("pelvis", (0, -6.0, -0.4)), 5.0, 5.8), COAT),
        # Pièce recousue sur le flanc : la tenue réparée avec les traces du voyage.
        Part(lambda p: ellipsoid(p, s.on_torso("pelvis", (4.4, 6.0, 2.4)), (2.2, 2.6, 1.6), torso_rotation), PATCH),
        Part(lambda p: ellipsoid(p, s.on_torso("neck", (0, -0.4, 0.6)), (4.3, 2.6, 4.0), torso_rotation), SCARF),
        # Pan d'écharpe qui tombe sur la poitrine : couleur lisible de face.
        Part(lambda p: capsule(p, s.on_torso("neck", (1.6, -1.0, 3.4)), s.on_torso("neck", (2.4 + 2.0 * d, -6.5, 4.2 + 0.8 * d)), 1.3, 1.1), SCARF),
        Part(lambda p: sphere(p, s.on_head((0, -0.3, 0.2)), DIMENSIONS.head_radius - 0.4), FACE),
        # Capuche creusée à l'avant : le visage reste une ouverture d'ombre, ambigu (Bible §6.1).
        Part(lambda p: np.maximum(ellipsoid(p, s.on_head((0, 0.8, -1.0)), (4.9, 5.3, 5.1), s.head),
                                  -ellipsoid(p, s.on_head((0, -0.8, 4.2)), (3.4, 3.6, 2.8), s.head)), HOOD),
        Part(lambda p: capsule(p, s.on_head((0, 0.6, -5.2)), s.on_head((1.6 * d, -3.2, -7.0)), 1.9, 1.0), HOOD),
        # Sac à dos, couchage roulé au sommet, gamelle, carte roulée et manche d'outil qui dépassent.
        # Le sac monte au-dessus de la tête : c'est la signature de la silhouette.
        Part(lambda p: rounded_box(p, s.on_torso("chest", (0, 1.5, -7.0)), (5.8, 9.5, 3.6), 1.8, torso_rotation), PACK),
        Part(lambda p: capsule(p, s.on_torso("chest", (-6.6, 12.5, -7.0)), s.on_torso("chest", (6.6, 12.5, -7.0)), 2.7), BEDROLL),
        Part(lambda p: ellipsoid(p, s.on_torso("chest", (6.8, -3.5, -6.4)), (1.8, 2.2, 2.2), torso_rotation), METAL),
        Part(lambda p: capsule(p, s.on_torso("chest", (-3.8, 13.0, -8.6)), s.on_torso("chest", (-4.8, 18.0, -9.2)), 1.1), MAP),
        Part(lambda p: capsule(p, s.on_torso("chest", (3.0, 12.0, -8.8)), s.on_torso("chest", (5.6 + 1.8 * d, 19.0, -9.8)), 0.9), ACCENT),
        Part(lambda p: rounded_box(p, s.on_torso("chest", (5.8 + 1.8 * d, 19.2, -9.8)), (1.8, 0.9, 0.9), 0.3, torso_rotation), METAL),
        # Bretelles.
        Part(lambda p: capsule(p, s.on_torso("chest", (3.4, 0.2, 3.2)), s.on_torso("chest", (3.8, -8.5, 4.4)), 0.75), PACK),
        Part(lambda p: capsule(p, s.on_torso("chest", (-3.4, 0.2, 3.2)), s.on_torso("chest", (-3.8, -8.5, 4.4)), 0.75), PACK),
        Part(lambda p: sphere(p, s.on_torso("chest", (3.8, -6.0, 4.6)), 0.9), ACCENT),
    ]
    parts += limbs(s, LimbStyle(sleeve=PATCH, hand=GLOVES, leg=TROUSERS, boot=BOOTS, arm_radius=2.0,
                                leg_radius=2.5, boot_radius=2.35, foot_radius=2.1))
    return parts


ANIMATIONS = character_animations(Gait())
