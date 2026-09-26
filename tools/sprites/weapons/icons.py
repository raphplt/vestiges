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
# Vu presque de face : l'icône montre la silhouette, pas le dessus de l'objet.
ICON_YAW = float(np.radians(8.0))


@dataclass(frozen=True)
class IconModel:
    stem: str
    parts: Callable[[], list[Part]]
    materials: Sequence[Material]


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
        image = flatten(render_layers(parts, model.materials, ICON_YAW, canvas, pivot, scale))
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
    """Faucille : manche de bois ligaturé, lame en croissant ; signature or du blé."""
    WOOD, BIND, BLADE, EDGE = range(4)
    materials = [
        make_material("wood", "#7A4E2E"),
        make_material("bind", "#D4A843", contrast=0.8),
        make_material("blade", "#8C8F94"),
        make_material("edge", "#DCD8CC", contrast=0.5),
    ]

    def parts() -> list[Part]:
        # Croissant : suite de capsules le long d'un arc, de plus en plus fines vers la pointe.
        center = np.array([6.0, 14.0, 0.0])
        arc = [center + 13.0 * np.array([np.cos(a), np.sin(a), 0.0]) for a in np.linspace(np.pi * 1.05, -np.pi * 0.35, 9)]
        blade = [(arc[i], arc[i + 1], 3.2 * (1 - i / 10) + 0.6) for i in range(len(arc) - 1)]
        edge = [(a - (a - center) / np.linalg.norm(a - center) * r * 0.7, b - (b - center) / np.linalg.norm(b - center) * r * 0.7, 0.8)
                for a, b, r in blade]
        return [
            Part(lambda p: capsule(p, (-5.5, -24.0, 0.0), (-6.5, 11.0, 0.0), 2.4), WOOD),
            Part(lambda p: _union(*(cylinder(p, (-5.5, y, 0.0), 2.9, 0.9, 0.3) for y in (-6.0, -2.5))), BIND),
            Part(lambda p: _union(*(capsule(p, a, b, r, r * 0.8) for a, b, r in blade)), BLADE),
            Part(lambda p: _union(*(capsule(p, a, b, r) for a, b, r in edge)), EDGE),
        ]

    return IconModel("weapon_icon_sickle", parts, materials)


def nail_gun() -> IconModel:
    """Cloueuse : outil de chantier jaune, poignée, bande de clous ; signature jaune chantier."""
    BODY, GRIP, METAL, NAIL = range(4)
    materials = [
        make_material("body", "#D8A12A"),
        make_material("grip", "#2E2A2C", contrast=0.6),
        make_material("metal", "#7E8288"),
        make_material("nail", "#C8C4BA", contrast=0.5),
    ]

    def parts() -> list[Part]:
        # Silhouette d'outil : corps le long de la diagonale (nez en haut), poignée perpendiculaire à l'arrière,
        # chargeur tendu du nez au bout de la poignée.
        magazine_start = np.array([4.5, 13.0, 0.0])
        magazine_end = np.array([15.0, -7.0, 0.0])
        nails = [magazine_start + (magazine_end - magazine_start) * t for t in np.linspace(0.05, 0.9, 7)]
        return [
            Part(lambda p: rounded_box(p, (0.0, 3.0, 0.0), (3.4, 14.0, 3.6), 1.6), BODY),
            Part(lambda p: cylinder(p, (0.0, 18.5, 0.0), 1.9, 2.2, 0.4), METAL),
            Part(lambda p: rounded_box(p, (8.0, -8.0, 0.0), (7.5, 2.6, 2.8), 1.2, rotation_z(0.12)), GRIP),
            Part(lambda p: capsule(p, magazine_start, magazine_end, 1.5), METAL),
            Part(lambda p: _union(*(sphere(p, n + np.array([0.0, 0.0, 1.6]), 0.9) for n in nails)), NAIL),
        ]

    return IconModel("weapon_icon_nail_gun", parts, materials)


def music_box() -> IconModel:
    """Boîte à musique : coffret de bois ouvert, peigne de laiton, manivelle ; signature rose passé."""
    WOOD, INSIDE, BRASS, VELVET = range(4)
    materials = [
        make_material("wood", "#8E5A3C"),
        make_material("inside", "#3A2226", contrast=0.5),
        make_material("brass", "#D4A843", contrast=0.8),
        make_material("velvet", "#B86E8A"),
    ]
    lid = rotation_x(-1.25)

    def parts() -> list[Part]:
        # La boîte n'est pas une arme longue : elle reste droite, un peu tournée, sans l'inclinaison des autres.
        tilt = rotation_z(np.pi / 4)
        return [
            Part(lambda p: rounded_box(p @ tilt, (0.0, -4.0, 0.0), (13.0, 7.0, 9.0), 1.2), WOOD),
            Part(lambda p: rounded_box(p @ tilt, (0.0, -1.0, 0.0), (11.0, 5.0, 7.0), 0.8), INSIDE),
            Part(lambda p: rounded_box(p @ tilt, (0.0, 4.4, -1.0), (8.0, 0.6, 3.0), 0.3), BRASS),
            Part(lambda p: rounded_box((p @ tilt - np.array([0.0, 3.0, -9.0])) @ lid, (0.0, 0.0, 9.0), (13.0, 1.4, 9.0), 1.0), WOOD),
            Part(lambda p: rounded_box((p @ tilt - np.array([0.0, 3.0, -9.0])) @ lid, (0.0, -1.4, 9.0), (11.0, 0.5, 7.0), 0.4), VELVET),
            Part(lambda p: _union(capsule(p @ tilt, (13.5, -4.0, 0.0), (17.0, -4.0, 0.0), 0.9),
                                  capsule(p @ tilt, (17.0, -4.0, 0.0), (17.0, -0.5, 0.0), 0.9),
                                  sphere(p @ tilt, (17.0, -0.5, 0.0), 1.5)), BRASS),
        ]

    return IconModel("weapon_icon_music_box", parts, materials)


def catalog() -> list[IconModel]:
    return [sickle(), nail_gun(), music_box()]
