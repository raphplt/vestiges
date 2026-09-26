"""
Icônes d'armes 32×32 (plan 17 §4.7, pilote de style) : l'objet du monde d'avant, détourné.

Même rendu que les décors et les créatures (lumière haut-gauche, quatre tons, contour sel-out), mais à une échelle
d'icône : l'objet remplit le cadre en diagonale, du bas-gauche vers le haut-droit. Une couleur signature par arme,
reprise plus tard par ses effets d'attaque.
"""
from __future__ import annotations

from dataclasses import dataclass
from typing import Callable, Sequence

import numpy as np
from PIL import Image

from ..palette import Material, make_material
from ..render import Part, flatten, render_layers
from ..sdf import capsule, cylinder, rounded_box, rotation_x, rotation_z, sphere

ICON_SIZE = 32
# Marge d'un pixel de chaque côté : le contour ne touche jamais le bord du cadre.
ICON_FILL = 30
# Échelles essayées (pixels par unité), de la plus grande à la plus petite : l'objet remplit le cadre au mieux.
ICON_SCALES = tuple(round(0.9 - 0.02 * i, 2) for i in range(25))
# L'objet est modelé debout (axe +Y) puis incliné vers le haut-droit.
DIAGONAL = rotation_z(-np.pi / 4)
# Trois-quarts léger : la silhouette reste franche et l'objet garde son épaisseur.
ICON_YAW = float(np.radians(28.0))


@dataclass(frozen=True)
class IconModel:
    stem: str
    parts: Callable[[], list[Part]]
    materials: Sequence[Material]
    # Angle de vue propre : un objet plat (outil à poignée) se lit mieux de profil que de trois-quarts.
    yaw: float = ICON_YAW


def _tilted(distance):
    """Applique l'inclinaison diagonale à une distance modelée debout (local = p @ rotation)."""
    return lambda p: distance(p @ DIAGONAL)


def _union(*distances: np.ndarray) -> np.ndarray:
    result = distances[0]
    for d in distances[1:]:
        result = np.minimum(result, d)
    return result


def render_icon(model: IconModel) -> Image.Image:
    canvas = (72, 72)
    pivot = (36.0, 36.0)
    parts = [Part(_tilted(part.distance), part.material) for part in model.parts()]
    for scale in ICON_SCALES:
        image = flatten(render_layers(parts, model.materials, model.yaw, canvas, pivot, scale))
        alpha = np.asarray(image)[..., 3]
        ys, xs = np.nonzero(alpha)
        width, height = xs.max() - xs.min() + 1, ys.max() - ys.min() + 1
        if width <= ICON_FILL and height <= ICON_FILL:
            icon = Image.new("RGBA", (ICON_SIZE, ICON_SIZE), (0, 0, 0, 0))
            icon.paste(image.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1)),
                       ((ICON_SIZE - width) // 2, (ICON_SIZE - height) // 2))
            return icon
    raise ValueError(f"{model.stem} : ne tient pas dans {ICON_FILL} px même à l'échelle {ICON_SCALES[-1]}")


def sickle() -> IconModel:
    """Faucille : manche effilé ligaturé de ficelle, virole de laiton, lame à dos marqué et fil clair ; signature or du blé."""
    WOOD, TWINE, STEEL, EDGE, BRASS, RUST = range(6)
    materials = [
        make_material("wood", "#7A4E2E"),
        make_material("twine", "#CDB27A", contrast=0.7),
        make_material("steel", "#7F848C"),
        make_material("edge", "#E4E0D4", contrast=0.4),
        make_material("brass", "#D4A843", contrast=0.8),
        make_material("rust", "#8A4E2A"),
    ]

    def parts() -> list[Part]:
        center = np.array([6.5, 15.0, 0.0])
        angles = np.linspace(np.pi * 1.02, -np.pi * 0.38, 12)
        arc = [center + 13.5 * np.array([np.cos(a), np.sin(a), 0.0]) for a in angles]
        # Lame épaisse au talon, effilée à la pointe ; le fil court le long de l'intérieur du croissant.
        blade = [(arc[i], arc[i + 1], 3.6 * (1 - i / 13) + 0.5) for i in range(len(arc) - 1)]
        inner = [(a - (a - center) / np.linalg.norm(a - center) * r * 0.75, b - (b - center) / np.linalg.norm(b - center) * r * 0.75,
                  0.7) for a, b, r in blade]
        spine = [(a + (a - center) / np.linalg.norm(a - center) * r * 0.55, b + (b - center) / np.linalg.norm(b - center) * r * 0.55,
                  0.8) for a, b, r in blade[:8]]
        return [
            Part(lambda p: capsule(p, (-6.0, -25.0, 0.0), (-6.8, 11.0, 0.0), 2.1, 2.7), WOOD),
            Part(lambda p: sphere(p, (-6.0, -25.5, 0.0), 2.8), WOOD),
            Part(lambda p: _union(*(cylinder(p, (-6.2 - 0.02 * y, y, 0.0), 2.9, 0.7, 0.3) for y in (-12.0, -9.5, -7.0, -4.5))), TWINE),
            Part(lambda p: cylinder(p, (-6.8, 10.0, 0.0), 3.0, 1.6, 0.4), BRASS),
            Part(lambda p: _union(*(capsule(p, a, b, r, r * 0.85) for a, b, r in blade)), STEEL),
            Part(lambda p: _union(*(capsule(p, a, b, r) for a, b, r in inner)), EDGE),
            Part(lambda p: _union(*(capsule(p, a, b, r) for a, b, r in spine)), RUST),
        ]

    return IconModel("weapon_icon_sickle", parts, materials)


def nail_gun() -> IconModel:
    """Cloueuse de chantier en pistolet : carter jaune, poignée caoutchoutée, gâchette, bande de clous, raccord d'air."""
    BODY, GRIP, METAL, NAIL, BRASS = range(5)
    materials = [
        make_material("body", "#E0A82E"),
        make_material("grip", "#4A444C", contrast=0.7),
        make_material("metal", "#80858C"),
        make_material("nail", "#E2DDD0", contrast=0.4),
        make_material("brass", "#C9953A", contrast=0.8),
    ]

    def parts() -> list[Part]:
        # Canon le long de l'axe (nez en haut à droite) ; la poignée part franchement à angle droit vers le bas,
        # le chargeur de clous relie le nez au bout de la poignée : c'est ce triangle qui fait lire « pistolet ».
        nails = [np.array([4.6 + 0.62 * i, 9.5 - 2.3 * i, 1.6]) for i in range(7)]
        return [
            Part(lambda p: rounded_box(p, (0.0, 4.0, 0.0), (3.0, 10.5, 3.2), 1.6), BODY),
            Part(lambda p: cylinder(p, (0.0, -6.0, 0.0), 3.6, 1.6, 0.8), BODY),
            Part(lambda p: _union(*(rounded_box(p, (0.0, y, 2.9), (1.6, 0.35, 0.5), 0.1) for y in (0.0, 2.0, 4.0))), GRIP),
            Part(lambda p: cylinder(p, (0.0, 15.8, 0.0), 1.6, 1.6, 0.4), METAL),
            Part(lambda p: capsule(p, (0.0, 17.5, 0.0), (0.0, 20.0, 0.0), 1.0), METAL),
            Part(lambda p: rounded_box(p, (10.0, -5.0, 0.0), (7.5, 2.2, 2.4), 1.0, rotation_z(0.08)), GRIP),
            Part(lambda p: capsule(p, (3.6, -0.5, 0.0), (5.2, -2.8, 0.0), 0.8), GRIP),
            Part(lambda p: capsule(p, (3.4, 12.0, 0.0), (16.0, -3.5, 0.0), 1.2), METAL),
            Part(lambda p: _union(*(rounded_box(p, n, (1.0, 0.45, 0.45), 0.2) for n in nails)), NAIL),
            Part(lambda p: cylinder(p, (18.2, -5.4, 0.0), 1.3, 1.2, 0.3, rotation_z(np.pi / 2)), BRASS),
        ]

    return IconModel("weapon_icon_nail_gun", parts, materials, yaw=float(np.radians(-10.0)))


def music_box() -> IconModel:
    """Boîte à musique ouverte : coffret à coins de laiton et petits pieds, velours rose, cylindre à picots, danseuse, manivelle."""
    WOOD, INSIDE, BRASS, VELVET, DANCER = range(5)
    materials = [
        make_material("wood", "#8E5A3C"),
        make_material("inside", "#3A2226", contrast=0.5),
        make_material("brass", "#D4A843", contrast=0.8),
        make_material("velvet", "#B86E8A"),
        make_material("dancer", "#F0D6DA", contrast=0.5),
    ]
    lid = rotation_x(-1.25)
    hinge = np.array([0.0, 3.0, -9.0])

    def parts() -> list[Part]:
        # Objet posé, pas une arme longue : il reste droit, sans l'inclinaison des autres icônes.
        tilt = rotation_z(np.pi / 4)
        corners = [(x * 12.5, y, 8.5) for x in (-1, 1) for y in (-9.5, 1.5)]
        feet = [(x * 11.0, -11.5, z * 7.0) for x in (-1, 1) for z in (-1, 1)]

        def up(p):
            return p @ tilt

        def lid_local(p):
            return (up(p) - hinge) @ lid

        return [
            Part(lambda p: rounded_box(up(p), (0.0, -4.0, 0.0), (13.0, 7.0, 9.0), 1.2), WOOD),
            Part(lambda p: rounded_box(up(p), (0.0, -1.0, 0.0), (11.0, 5.0, 7.0), 0.8), INSIDE),
            Part(lambda p: _union(*(rounded_box(up(p), c, (1.4, 1.4, 1.2), 0.4) for c in corners)), BRASS),
            Part(lambda p: _union(*(sphere(up(p), c, 1.6) for c in feet)), BRASS),
            Part(lambda p: cylinder(up(p), (0.0, 4.2, -2.5), 1.4, 7.0, 0.3, rotation_z(np.pi / 2)), BRASS),
            Part(lambda p: _union(capsule(up(p), (2.5, 4.5, 2.0), (2.5, 7.5, 2.0), 0.9, 0.6),
                                  sphere(up(p), (2.5, 8.8, 2.0), 1.0)), DANCER),
            Part(lambda p: rounded_box(lid_local(p), (0.0, 0.0, 9.0), (13.0, 1.4, 9.0), 1.0), WOOD),
            Part(lambda p: rounded_box(lid_local(p), (0.0, -1.4, 9.0), (11.0, 0.5, 7.0), 0.4), VELVET),
            Part(lambda p: _union(capsule(up(p), (13.5, -4.0, 0.0), (17.0, -4.0, 0.0), 0.9),
                                  capsule(up(p), (17.0, -4.0, 0.0), (17.0, -0.5, 0.0), 0.9),
                                  sphere(up(p), (17.0, -0.5, 0.0), 1.5)), BRASS),
        ]

    return IconModel("weapon_icon_music_box", parts, materials)


def catalog() -> list[IconModel]:
    return [sickle(), nail_gun(), music_box()]
