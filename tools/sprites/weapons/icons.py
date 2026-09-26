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

from ..palette import Material, make_emissive, make_material
from ..render import Part, flatten, render_layers
from ..sdf import capsule, cylinder, ellipsoid, rounded_box, rotation_x, rotation_y, rotation_z, sphere

ICON_SIZE = 32
# Marge d'un pixel de chaque côté : le contour ne touche jamais le bord du cadre.
ICON_FILL = 30
# Échelles essayées (pixels par unité), de la plus grande à la plus petite : l'objet remplit le cadre au mieux.
ICON_SCALES = tuple(round(0.9 - 0.02 * i, 2) for i in range(25))
# L'objet est modelé debout (axe +Y) puis incliné vers le haut-droit.
DIAGONAL = rotation_z(-np.pi / 4)
# Trois-quarts léger : la silhouette reste franche et l'objet garde son épaisseur.
ICON_YAW = float(np.radians(28.0))
# Objets posés (boîtes, appareils, cadrans) : annule l'inclinaison diagonale, ils restent droits.
UPRIGHT = rotation_z(np.pi / 4)
# Disques tournés vers le spectateur (cadrans, lentilles) : l'axe du cylindre passe de Y à Z.
FACING = rotation_x(np.pi / 2)


@dataclass(frozen=True)
class IconModel:
    stem: str
    parts: Callable[[], list[Part]]
    materials: Sequence[Material]
    # Angle de vue propre : un objet plat (outil à poignée) se lit mieux de profil que de trois-quarts.
    yaw: float = ICON_YAW
    # Arme de data/weapons/weapons.json dont c'est l'icône.
    weapon_id: str = ""


def _tilted(distance):
    """Applique l'inclinaison diagonale à une distance modelée debout (local = p @ rotation)."""
    return lambda p: distance(p @ DIAGONAL)


def _up(p: np.ndarray) -> np.ndarray:
    """Point d'un objet posé : l'inclinaison diagonale commune est annulée."""
    return p @ UPRIGHT


def _arc(center, radius: float, start: float, end: float, count: int, z: float = 0.0) -> list[np.ndarray]:
    """Points d'un arc dans le plan XY (angles en radians), pour des câbles, anneaux et anses en capsules."""
    return [np.array([center[0] + radius * np.cos(a), center[1] + radius * np.sin(a), z])
            for a in np.linspace(start, end, count)]


def _chain(points: list[np.ndarray], radius: float, local=lambda p: p):
    """Capsules enchaînées le long de points : tube courbe (câble, anse, anneau)."""
    return lambda p: _union(*(capsule(local(p), a, b, radius) for a, b in zip(points, points[1:])))


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

    return IconModel("weapon_icon_sickle", parts, materials, weapon_id="chipped_blade")


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

    return IconModel("weapon_icon_nail_gun", parts, materials, yaw=float(np.radians(-10.0)), weapon_id="crossbow")


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

    return IconModel("weapon_icon_music_box", parts, materials, weapon_id="music_box")


def parking_meter() -> IconModel:
    """Parcmètre arraché : pied de béton, fût, tête peinte, cadran vitré et drapeau rouge « expiré »."""
    CONCRETE, POLE, HEAD, GLASS, FLAG, METAL = range(6)
    materials = [
        make_material("concrete", "#9A948A"),
        make_material("pole", "#5E6670"),
        make_material("head", "#3E6E5A"),
        make_material("glass", "#D8E4C8", contrast=0.5),
        make_material("flag", "#D0443A"),
        make_material("metal", "#A8ACB0", contrast=0.7),
    ]

    def parts() -> list[Part]:
        return [
            Part(lambda p: rounded_box(p, (0.0, -21.0, 0.0), (5.0, 2.8, 5.0), 1.0), CONCRETE),
            Part(lambda p: capsule(p, (0.0, -19.0, 0.0), (0.0, 5.0, 0.0), 2.2), POLE),
            Part(lambda p: rounded_box(p, (0.0, 14.0, 0.0), (8.5, 10.0, 5.5), 3.0), HEAD),
            Part(lambda p: rounded_box(p, (0.0, 16.5, 4.8), (5.8, 4.2, 1.0), 1.2), GLASS),
            Part(lambda p: rounded_box(p, (2.8, 17.5, 5.6), (2.2, 2.2, 0.5), 0.2), FLAG),
            Part(lambda p: rounded_box(p, (0.0, 9.0, 5.2), (3.0, 0.7, 0.6), 0.2), METAL),
            Part(lambda p: cylinder(p, (0.0, 24.5, 0.0), 4.6, 1.0, 0.5), METAL),
        ]

    return IconModel("weapon_icon_parking_meter", parts, materials, weapon_id="heavy_hammer")


def gym_bow() -> IconModel:
    """Arc d'initiation du gymnase : branches de fibre bleue, poignée gainée, corde claire, étiquette d'inventaire."""
    LIMB, GRIP, STRING, LABEL = range(4)
    materials = [
        make_material("limb", "#3C6AB0"),
        make_material("grip", "#3A3230", contrast=0.7),
        make_material("string", "#E8E0D0", contrast=0.4),
        make_material("label", "#F0E8C8", contrast=0.5),
    ]
    center = np.array([14.0, 0.0, 0.0])
    arc = _arc(center, 24.0, np.radians(120.0), np.radians(240.0), 13)

    def parts() -> list[Part]:
        limb = [(a, b, 1.2 + 1.4 * np.sin(np.pi * (i + 0.5) / 12)) for i, (a, b) in enumerate(zip(arc, arc[1:]))]
        return [
            Part(lambda p: _union(*(capsule(p, a, b, r) for a, b, r in limb)), LIMB),
            Part(lambda p: capsule(p, (-9.9, -4.0, 0.0), (-9.9, 4.0, 0.0), 2.9), GRIP),
            Part(lambda p: rounded_box(p, (-9.6, 7.5, 1.4), (1.6, 1.3, 0.4), 0.2), LABEL),
            Part(lambda p: capsule(p, arc[0], arc[-1], 0.45), STRING),
        ]

    return IconModel("weapon_icon_gym_bow", parts, materials, weapon_id="makeshift_bow")


def slingshot() -> IconModel:
    """Lance-pierre d'enfant : fourche de bois, élastiques rouges, poche de cuir, billes de verre."""
    WOOD, BAND, POUCH, BLUE, RED = range(5)
    materials = [
        make_material("wood", "#9A6A3E"),
        make_material("band", "#D0443A"),
        make_material("pouch", "#6A4A32", contrast=0.7),
        make_material("marble_blue", "#58A8E0", contrast=0.8),
        make_material("marble_red", "#E8704A", contrast=0.8),
    ]

    def parts() -> list[Part]:
        return [
            Part(lambda p: _union(capsule(p, (0.0, -25.0, 0.0), (0.0, -2.0, 0.0), 2.4, 2.0),
                                  capsule(p, (0.0, -2.0, 0.0), (-8.0, 12.0, 0.0), 2.0, 1.6),
                                  capsule(p, (0.0, -2.0, 0.0), (8.0, 12.0, 0.0), 2.0, 1.6)), WOOD),
            Part(lambda p: _union(capsule(p, (-8.0, 11.0, 0.5), (-2.5, 3.0, 3.5), 0.7),
                                  capsule(p, (8.0, 11.0, 0.5), (2.5, 3.0, 3.5), 0.7)), BAND),
            Part(lambda p: ellipsoid(p, (0.0, 2.5, 3.8), (3.2, 2.0, 1.4)), POUCH),
            Part(lambda p: sphere(p, (6.0, -14.0, 3.0), 2.6), BLUE),
            Part(lambda p: sphere(p, (7.5, -19.5, 1.0), 2.3), RED),
        ]

    return IconModel("weapon_icon_slingshot", parts, materials, weapon_id="sling")


def umbrella() -> IconModel:
    """Grand parapluie noir fermé : poignée en crosse, toile roulée et sa sangle, pointe ferrée."""
    HANDLE, CLOTH, STRAP, METAL = range(4)
    materials = [
        make_material("handle", "#7A4E2E"),
        make_material("cloth", "#2E3040", contrast=0.9),
        make_material("strap", "#B8A060", contrast=0.7),
        make_material("metal", "#B0B4BA", contrast=0.7),
    ]
    hook = _arc((-4.5, -20.0), 4.5, 0.0, -np.pi * 1.05, 8)

    def parts() -> list[Part]:
        return [
            Part(lambda p: _union(capsule(p, (0.0, -20.0, 0.0), (0.0, -12.0, 0.0), 1.6), _chain(hook, 1.6)(p)), HANDLE),
            Part(lambda p: _union(capsule(p, (0.0, -11.0, 0.0), (0.0, 4.0, 0.0), 1.2, 4.6),
                                  capsule(p, (0.0, 4.0, 0.0), (0.0, 22.0, 0.0), 4.6, 1.0)), CLOTH),
            Part(lambda p: cylinder(p, (0.0, 6.0, 0.0), 4.8, 0.8, 0.3), STRAP),
            Part(lambda p: _union(capsule(p, (0.0, 22.0, 0.0), (0.0, 27.0, 0.0), 0.7),
                                  cylinder(p, (0.0, -11.5, 0.0), 1.6, 0.8, 0.2)), METAL),
        ]

    return IconModel("weapon_icon_umbrella", parts, materials, weapon_id="sharpened_pipe")


def snow_shovel() -> IconModel:
    """Pelle à neige : poignée en D, manche, large lame de plastique orange au bord de métal."""
    GRIP, SHAFT, BLADE, EDGE = range(4)
    materials = [
        make_material("grip", "#3A3A3E", contrast=0.7),
        make_material("shaft", "#9A6A3E"),
        make_material("blade", "#F28C2C", contrast=0.8),
        make_material("edge", "#A8ACB0", contrast=0.6),
    ]
    loop = _arc((0.0, -22.0), 3.6, 0.0, -np.pi, 7)

    def parts() -> list[Part]:
        return [
            Part(lambda p: _union(_chain(loop, 1.1)(p), capsule(p, (-3.6, -22.0, 0.0), (3.6, -22.0, 0.0), 1.1)), GRIP),
            Part(lambda p: capsule(p, (0.0, -21.0, 0.0), (0.0, 12.0, 0.0), 1.5), SHAFT),
            Part(lambda p: _union(rounded_box(p, (0.0, 19.0, 0.0), (13.0, 7.0, 1.0), 0.9, rotation_x(0.35)),
                                  rounded_box(p, (-12.2, 18.5, 1.5), (1.0, 7.0, 2.4), 0.5, rotation_x(0.35)),
                                  rounded_box(p, (12.2, 18.5, 1.5), (1.0, 7.0, 2.4), 0.5, rotation_x(0.35))), BLADE),
            Part(lambda p: rounded_box(p, (0.0, 25.8, 2.4), (13.0, 0.8, 1.1), 0.4, rotation_x(0.35)), EDGE),
        ]

    return IconModel("weapon_icon_snow_shovel", parts, materials, weapon_id="cleaver")


def extension_cord() -> IconModel:
    """Rallonge enroulée : deux boucles de câble orange, fiche à deux broches, prise multiple."""
    CABLE, PLUG, PRONG, SOCKET = range(4)
    materials = [
        make_material("cable", "#E0782A"),
        make_material("plug", "#E8E4D8", contrast=0.6),
        make_material("prong", "#B8BCC0", contrast=0.6),
        make_material("socket", "#3A3A40", contrast=0.7),
    ]
    loop_a = _arc((0.0, 4.0), 12.0, 0.0, np.pi * 2.0, 20, 0.0)
    loop_b = _arc((1.5, 3.0), 11.0, 0.3, np.pi * 2.3, 20, 2.2)
    tail = [np.array([11.5, -2.0, 1.0]), np.array([13.0, -10.0, 1.5]), np.array([10.0, -18.0, 2.0])]

    def parts() -> list[Part]:
        return [
            Part(lambda p: _union(_chain(loop_a, 1.6, _up)(p), _chain(loop_b, 1.6, _up)(p), _chain(tail, 1.6, _up)(p)), CABLE),
            Part(lambda p: rounded_box(_up(p), (9.5, -21.5, 2.0), (2.6, 3.2, 2.0), 1.0), PLUG),
            Part(lambda p: _union(capsule(_up(p), (8.5, -24.5, 2.0), (8.5, -27.5, 2.0), 0.45),
                                  capsule(_up(p), (10.5, -24.5, 2.0), (10.5, -27.5, 2.0), 0.45)), PRONG),
            Part(lambda p: rounded_box(_up(p), (-11.0, -12.0, 2.0), (4.0, 3.2, 2.4), 1.2), SOCKET),
        ]

    return IconModel("weapon_icon_extension_cord", parts, materials, weapon_id="whip")


def plates() -> IconModel:
    """Pile d'assiettes du dimanche : porcelaine blanche au filet bleu, une assiette qui s'envole."""
    CHINA, RIM = range(2)
    materials = [
        make_material("china", "#EEEAE0", contrast=0.5),
        make_material("rim", "#3A5AA8"),
    ]
    flying = rotation_z(0.5) @ rotation_x(0.35)

    def parts() -> list[Part]:
        stack = [(-3.0, -10.0 + 2.6 * i, 0.0) for i in range(3)]
        return [
            Part(lambda p: _union(*(cylinder(_up(p), c, 11.0, 1.0, 0.8) for c in stack),
                                  cylinder(_up(p), (5.0, 12.0, 0.0), 10.0, 0.9, 0.7, flying)), CHINA),
            Part(lambda p: _union(*(cylinder(_up(p), (c[0], c[1] + 0.2, c[2]), 11.3, 0.5, 0.3) for c in stack),
                                  cylinder(_up(p), (5.0, 12.2, 0.0), 10.3, 0.45, 0.3, flying)), RIM),
        ]

    return IconModel("weapon_icon_plates", parts, materials, weapon_id="throwing_axes")


def leaf_rake() -> IconModel:
    """Râteau à feuilles : long manche, virole, dents d'acier rouillé en éventail et leur barrette."""
    WOOD, METAL, TINE = range(3)
    materials = [
        make_material("wood", "#9A6A3E"),
        make_material("metal", "#8A9098", contrast=0.7),
        make_material("tine", "#9A5A36"),
    ]
    tips = [np.array([22.0 * np.cos(a), 6.0 + 22.0 * np.sin(a), 0.0]) for a in np.radians(np.linspace(45, 135, 9))]

    def parts() -> list[Part]:
        return [
            Part(lambda p: capsule(p, (0.0, -26.0, 0.0), (0.0, 4.0, 0.0), 1.6), WOOD),
            Part(lambda p: capsule(p, (0.0, 3.0, 0.0), (0.0, 8.0, 0.0), 2.3, 1.9), METAL),
            Part(lambda p: _union(*(capsule(p, (0.0, 7.5, 0.0), t, 0.9, 0.7) for t in tips),
                                  _chain(_arc((0.0, 6.0), 12.0, np.radians(45), np.radians(135), 8), 0.8)(p)), TINE),
        ]

    return IconModel("weapon_icon_leaf_rake", parts, materials, weapon_id="nail_mace")


def school_bell() -> IconModel:
    """Cloche de récréation : robe de laiton évasée, rebord, battant, manche de bois tourné."""
    BRASS, MOUTH, CLAPPER, WOOD = range(4)
    materials = [
        make_material("brass", "#E0A830", contrast=1.1),
        make_material("mouth", "#3A2A1E", contrast=0.5),
        make_material("clapper", "#6A6058", contrast=0.7),
        make_material("wood", "#5A3A22"),
    ]
    # Droite, un peu balancée comme si on venait de la sonner ; le battant dépasse sous le rebord.
    sway = rotation_z(-0.25)

    def bell(p):
        return _up(p) @ sway

    def parts() -> list[Part]:
        return [
            Part(lambda p: _union(sphere(bell(p), (0.0, 4.0, 0.0), 5.2),
                                  cylinder(bell(p), (0.0, 0.5, 0.0), 5.6, 3.5, 1.0),
                                  cylinder(bell(p), (0.0, -3.5, 0.0), 7.2, 1.6, 0.8),
                                  cylinder(bell(p), (0.0, -6.0, 0.0), 9.5, 1.3, 0.7),
                                  cylinder(bell(p), (0.0, -8.0, 0.0), 12.0, 1.1, 0.6)), BRASS),
            Part(lambda p: cylinder(bell(p), (0.0, -8.6, 0.0), 10.4, 0.6, 0.3), MOUTH),
            Part(lambda p: _union(capsule(bell(p), (0.0, -8.0, 1.5), (0.0, -14.5, 3.0), 0.8),
                                  sphere(bell(p), (0.0, -16.0, 3.5), 2.8)), CLAPPER),
            Part(lambda p: _union(capsule(bell(p), (0.0, 6.0, 0.0), (0.0, 16.0, 0.0), 2.4, 2.8),
                                  sphere(bell(p), (0.0, 18.5, 0.0), 3.6)), WOOD),
        ]

    return IconModel("weapon_icon_school_bell", parts, materials, weapon_id="teachers_bell")


def scalpel() -> IconModel:
    """Scalpel : manche plat strié, lame courbe au fil clair, repère turquoise d'hôpital."""
    STEEL, EDGE, MARK = range(3)
    materials = [
        make_material("steel", "#A8B0B8", contrast=0.7),
        make_material("edge", "#EEF2F4", contrast=0.4),
        make_material("mark", "#3AB0A0"),
    ]

    def parts() -> list[Part]:
        return [
            Part(lambda p: _union(rounded_box(p, (0.0, -8.0, 0.0), (2.0, 17.0, 0.8), 0.6),
                                  *(rounded_box(p, (0.0, -20.0 + 2.2 * i, 0.9), (2.1, 0.4, 0.3), 0.15) for i in range(5))), STEEL),
            Part(lambda p: _union(capsule(p, (0.0, 9.0, 0.0), (0.4, 17.0, 0.0), 2.4, 1.6),
                                  capsule(p, (0.4, 17.0, 0.0), (-1.2, 25.0, 0.0), 1.6, 0.3)), EDGE),
            Part(lambda p: rounded_box(p, (0.0, 2.5, 0.8), (2.1, 1.2, 0.3), 0.2), MARK),
        ]

    return IconModel("weapon_icon_scalpel", parts, materials, yaw=float(np.radians(15.0)), weapon_id="surgeons_scalpel")


def lighthouse_lens() -> IconModel:
    """Fragment de lentille de phare : anneaux de verre en gradins dans un cadre de laiton, cœur encore lumineux."""
    BRASS, GLASS, LIGHT = range(3)
    materials = [
        make_material("brass", "#C9953A", contrast=0.9),
        make_material("glass", "#BFE6F0", contrast=0.5),
        make_emissive("light", "#FFE9A0"),
    ]
    tilt = rotation_y(-0.35)

    def parts() -> list[Part]:
        rings = [(12.0, 1.2), (9.5, 2.2), (7.0, 3.2), (4.5, 4.2)]
        return [
            Part(lambda p: cylinder(_up(p), (0.0, 0.0, -1.0), 14.0, 1.6, 0.6, FACING @ tilt), BRASS),
            Part(lambda p: _union(*(cylinder(_up(p), (0.0, 0.0, 0.0), r, h, 0.6, FACING @ tilt) for r, h in rings)), GLASS),
            Part(lambda p: sphere(_up(p), (0.0, 0.0, 4.2), 2.2), LIGHT),
        ]

    return IconModel("weapon_icon_lighthouse_lens", parts, materials, weapon_id="lighthouse_shard")


def keyring() -> IconModel:
    """Trousseau : anneau d'acier, quatre clés de métaux différents, étiquettes de papier."""
    RING, BRASS, STEEL, IRON, TAG = range(5)
    materials = [
        make_material("ring", "#9AA0A8", contrast=0.7),
        make_material("brass", "#D4A843", contrast=0.9),
        make_material("steel", "#B8BEC6", contrast=0.6),
        make_material("iron", "#6A6058"),
        make_material("tag", "#E8DCC0", contrast=0.5),
    ]
    ring = _arc((0.0, 16.0), 7.0, 0.0, np.pi * 2.0, 18)

    def key(p, top, angle, length):
        rot = rotation_z(angle)
        local = (p - np.asarray(top)) @ rot
        return _union(cylinder(local, (0.0, -3.0, 0.0), 3.0, 0.8, 0.3, FACING),
                      capsule(local, (0.0, -6.0, 0.0), (0.0, -6.0 - length, 0.0), 1.0),
                      rounded_box(local, (1.4, -3.0 - length, 0.0), (1.2, 1.8, 0.7), 0.3))

    def parts() -> list[Part]:
        return [
            Part(_chain(ring, 0.9, _up), RING),
            Part(lambda p: key(_up(p), (-5.0, 10.0, 0.0), -0.45, 14.0), BRASS),
            Part(lambda p: key(_up(p), (0.0, 9.0, 1.5), 0.05, 17.0), STEEL),
            Part(lambda p: key(_up(p), (5.0, 10.0, 0.0), 0.5, 12.0), IRON),
            Part(lambda p: rounded_box(_up(p), (-11.0, -6.0, 1.0), (3.0, 2.0, 0.4), 0.3, rotation_z(-0.5)), TAG),
        ]

    return IconModel("weapon_icon_keyring", parts, materials, weapon_id="chain_of_names")


def compass() -> IconModel:
    """Boussole de randonnée : boîtier de laiton, cadran clair, aiguille rouge et blanche, anneau de suspension."""
    BRASS, FACE, NEEDLE_N, NEEDLE_S, CAP = range(5)
    materials = [
        make_material("brass", "#C9953A", contrast=0.9),
        make_material("face", "#EEE8D8", contrast=0.4),
        make_material("north", "#D0443A"),
        make_material("south", "#F4F2EC", contrast=0.4),
        make_material("cap", "#5A5048", contrast=0.6),
    ]
    loop = _arc((0.0, 15.5), 3.2, 0.0, np.pi * 2.0, 12)
    needle = rotation_z(-0.6)

    def parts() -> list[Part]:
        return [
            Part(lambda p: _union(cylinder(_up(p), (0.0, 0.0, 0.0), 13.0, 2.6, 1.0, FACING),
                                  cylinder(_up(p), (0.0, 13.2, 0.0), 1.8, 1.4, 0.4),
                                  _chain(loop, 0.8)(_up(p))), BRASS),
            Part(lambda p: cylinder(_up(p), (0.0, 0.0, 1.4), 10.5, 1.6, 0.5, FACING), FACE),
            Part(lambda p: capsule(_up(p) @ needle, (0.0, 0.0, 3.2), (0.0, 8.5, 3.2), 1.3, 0.3), NEEDLE_N),
            Part(lambda p: capsule(_up(p) @ needle, (0.0, 0.0, 3.2), (0.0, -8.5, 3.2), 1.3, 0.3), NEEDLE_S),
            Part(lambda p: sphere(_up(p), (0.0, 0.0, 3.6), 1.4), CAP),
        ]

    return IconModel("weapon_icon_compass", parts, materials, yaw=float(np.radians(12.0)), weapon_id="compass_needle")


def instant_camera() -> IconModel:
    """Appareil instantané : boîtier crème, bande arc-en-ciel, objectif noir, flash, photo qui sort."""
    BODY, LENS, GLASS, FLASH, PHOTO, RED, YELLOW, BLUE = range(8)
    materials = [
        make_material("body", "#E8E4D8", contrast=0.5),
        make_material("lens", "#2E2E34", contrast=0.7),
        make_material("glass", "#6A8AA8", contrast=0.8),
        make_emissive("flash", "#FFF6C8"),
        make_material("photo", "#F4F2EC", contrast=0.4),
        make_material("red", "#D8443A"),
        make_material("yellow", "#E8B83A"),
        make_material("blue", "#3A7AC8"),
    ]

    def parts() -> list[Part]:
        return [
            Part(lambda p: rounded_box(_up(p), (0.0, 0.0, 0.0), (13.0, 9.0, 6.5), 2.2), BODY),
            Part(lambda p: cylinder(_up(p), (-2.0, -0.5, 6.0), 5.6, 2.0, 0.8, FACING), LENS),
            Part(lambda p: cylinder(_up(p), (-2.0, -0.5, 7.6), 3.4, 1.0, 0.5, FACING), GLASS),
            Part(lambda p: rounded_box(_up(p), (7.5, 6.0, 5.8), (3.2, 1.8, 1.0), 0.6), FLASH),
            Part(lambda p: rounded_box(_up(p), (0.0, -11.5, 2.0), (8.0, 3.5, 0.4), 0.3), PHOTO),
            Part(lambda p: rounded_box(_up(p), (6.5, -1.5, 6.3), (0.7, 5.5, 0.4), 0.2), RED),
            Part(lambda p: rounded_box(_up(p), (8.0, -1.5, 6.3), (0.7, 5.5, 0.4), 0.2), YELLOW),
            Part(lambda p: rounded_box(_up(p), (9.5, -1.5, 6.3), (0.7, 5.5, 0.4), 0.2), BLUE),
        ]

    return IconModel("weapon_icon_instant_camera", parts, materials, yaw=float(np.radians(18.0)), weapon_id="photographers_flash")


def dowsing_rod() -> IconModel:
    """Baguette de sourcier : branche de noisetier fourchue, nœuds d'écorce, goutte d'eau qui luit au bout."""
    BARK, KNOT, DROP = range(3)
    materials = [
        make_material("bark", "#8A6A42"),
        make_material("knot", "#5A4230", contrast=0.8),
        make_emissive("drop", "#7AD8F0"),
    ]

    def parts() -> list[Part]:
        return [
            Part(lambda p: _union(capsule(p, (-8.0, -25.0, 0.0), (0.0, 4.0, 0.0), 1.5, 1.9),
                                  capsule(p, (8.0, -25.0, 0.0), (0.0, 4.0, 0.0), 1.5, 1.9),
                                  capsule(p, (0.0, 4.0, 0.0), (0.5, 22.0, 0.0), 1.9, 0.9)), BARK),
            Part(lambda p: _union(sphere(p, (-4.5, -12.0, 0.8), 1.4), sphere(p, (5.2, -15.0, 0.8), 1.3),
                                  sphere(p, (0.3, 12.0, 0.8), 1.2)), KNOT),
            Part(lambda p: sphere(p, (0.7, 25.0, 0.0), 1.8), DROP),
        ]

    return IconModel("weapon_icon_dowsing_rod", parts, materials, weapon_id="essence_staff")


def eraser() -> IconModel:
    """Gomme d'écolier bicolore, rose et bleue, coin usé, miettes."""
    PINK, BLUE, CRUMB = range(3)
    materials = [
        make_material("pink", "#E88AA0"),
        make_material("blue", "#4A7AC8"),
        make_material("crumb", "#D8A0B0", contrast=0.6),
    ]
    tilt = rotation_z(-0.5)

    def parts() -> list[Part]:
        return [
            Part(lambda p: rounded_box(_up(p) @ tilt, (0.0, 5.0, 0.0), (6.5, 8.0, 3.8), 2.6), PINK),
            Part(lambda p: rounded_box(_up(p) @ tilt, (0.0, -9.0, 0.0), (6.5, 6.0, 3.8), 1.2), BLUE),
            Part(lambda p: _union(sphere(_up(p), (10.0, 12.0, 1.0), 1.2), sphere(_up(p), (13.0, 9.0, 0.0), 0.9),
                                  sphere(_up(p), (11.5, 15.5, -0.5), 0.8)), CRUMB),
        ]

    return IconModel("weapon_icon_eraser", parts, materials, weapon_id="void_edge")


def oil_lamp() -> IconModel:
    """Lampe tempête : réservoir rouge, verre, flamme, chapeau et anse de fil de fer."""
    TANK, GLASS, FLAME, WIRE = range(4)
    materials = [
        make_material("tank", "#B84A32"),
        make_material("glass", "#D8E8E8", contrast=0.4),
        make_emissive("flame", "#FFC850"),
        make_material("wire", "#6A6E74", contrast=0.7),
    ]
    handle = _arc((0.0, 16.0), 8.0, 0.0, np.pi, 10)

    def parts() -> list[Part]:
        return [
            Part(lambda p: _union(cylinder(_up(p), (0.0, -14.0, 0.0), 9.5, 4.5, 2.0),
                                  cylinder(_up(p), (0.0, 12.0, 0.0), 6.0, 1.8, 0.8)), TANK),
            Part(lambda p: ellipsoid(_up(p), (0.0, -0.5, 0.0), (7.0, 10.5, 7.0)), GLASS),
            Part(lambda p: ellipsoid(_up(p), (0.0, -1.5, 0.0), (2.2, 4.2, 2.2)), FLAME),
            Part(lambda p: _union(_chain(handle, 0.7)(_up(p)),
                                  *(capsule(_up(p), (x, -9.5, 0.0), (x * 0.8, 10.0, 0.0), 0.6) for x in (-7.5, 7.5))), WIRE),
        ]

    return IconModel("weapon_icon_oil_lamp", parts, materials, weapon_id="memory_lantern")


def boxing_gloves() -> IconModel:
    """Paire de gants de boxe au cuir rouge fendu, manchettes blanches, lacet."""
    LEATHER, CUFF, LACE = range(3)
    materials = [
        make_material("leather", "#C83A3A"),
        make_material("cuff", "#EEEAE0", contrast=0.5),
        make_material("lace", "#E8D8A0", contrast=0.5),
    ]

    def glove(p, center, angle, thumb_side):
        local = (p - np.asarray(center)) @ rotation_z(angle)
        return _union(ellipsoid(local, (0.0, -5.0, 0.0), (7.2, 8.5, 6.2)),
                      ellipsoid(local, (6.0 * thumb_side, -2.0, 2.4), (2.6, 5.0, 2.6), rotation_z(-0.3 * thumb_side)),
                      cylinder(local, (0.0, 4.5, 0.0), 5.0, 3.2, 1.2))

    def cuff(p, center, angle):
        local = (p - np.asarray(center)) @ rotation_z(angle)
        return cylinder(local, (0.0, 6.0, 0.0), 5.3, 1.3, 0.5)

    left, right = (-8.0, -4.0, 0.0), (8.0, -6.0, -2.0)
    lace = _arc((0.0, 8.0), 8.5, 0.0, np.pi, 10, 1.0)

    def parts() -> list[Part]:
        return [
            Part(lambda p: _union(glove(_up(p), left, 0.2, 1.0), glove(_up(p), right, -0.25, -1.0)), LEATHER),
            Part(lambda p: _union(cuff(_up(p), left, 0.2), cuff(_up(p), right, -0.25)), CUFF),
            Part(_chain(lace, 0.6, _up), LACE),
        ]

    return IconModel("weapon_icon_boxing_gloves", parts, materials, weapon_id="echo_gauntlets")


def chalks() -> IconModel:
    """Boîte de craies ouverte : carton, craies de couleur à la pointe usée qui en dépassent."""
    BOX, INSIDE, PINK, YELLOW, BLUE, GREEN = range(6)
    materials = [
        make_material("box", "#D8B060"),
        make_material("inside", "#4A3A2E", contrast=0.6),
        make_material("pink", "#F09AB0", contrast=0.6),
        make_material("yellow", "#F4D860", contrast=0.6),
        make_material("blue", "#78B8F0", contrast=0.6),
        make_material("green", "#8AD890", contrast=0.6),
    ]
    sticks = [((-6.5, -4.0, 0.0), (-9.5, 10.0, 1.0)), ((-2.2, -4.0, 0.5), (-2.0, 13.0, 1.5)),
              ((2.4, -4.0, 0.0), (4.5, 11.5, 0.5)), ((6.8, -4.0, 0.5), (11.0, 8.0, 1.0))]

    def stick(index):
        a, b = sticks[index]
        return lambda p: capsule(_up(p), a, b, 2.3, 2.0)

    def parts() -> list[Part]:
        return [
            Part(lambda p: rounded_box(_up(p), (0.0, -9.0, 0.0), (11.0, 7.0, 4.5), 0.8), BOX),
            Part(lambda p: rounded_box(_up(p), (0.0, -2.8, 0.0), (10.0, 0.8, 3.6), 0.4), INSIDE),
            Part(stick(0), PINK),
            Part(stick(1), YELLOW),
            Part(stick(2), BLUE),
            Part(stick(3), GREEN),
        ]

    return IconModel("weapon_icon_chalks", parts, materials, weapon_id="childs_drawing")


def transistor() -> IconModel:
    """Poste de radio à piles : boîtier turquoise, grille de haut-parleur, cadran d'accord, antenne déployée."""
    BODY, GRILLE, DIAL, CHROME, STRAP = range(5)
    materials = [
        make_material("body", "#3E9A96"),
        make_material("grille", "#D8D0B8", contrast=0.6),
        make_material("dial", "#E8B83A", contrast=0.8),
        make_material("chrome", "#C0C4CA", contrast=0.6),
        make_material("strap", "#5A3A26"),
    ]
    strap = _arc((0.0, 8.5), 7.0, 0.0, np.pi, 9)

    def parts() -> list[Part]:
        holes = [(-9.0 + 2.4 * i, -4.0 + 2.4 * j, 5.0) for i in range(4) for j in range(4)]
        return [
            Part(lambda p: rounded_box(_up(p), (0.0, -2.0, 0.0), (13.5, 9.0, 4.8), 2.0), BODY),
            Part(lambda p: _union(*(sphere(_up(p), h, 0.9) for h in holes)), GRILLE),
            Part(lambda p: cylinder(_up(p), (7.0, -1.5, 4.4), 3.6, 1.0, 0.5, FACING), DIAL),
            Part(lambda p: _union(capsule(_up(p), (10.5, 7.0, 0.0), (19.0, 26.0, 0.0), 0.7, 0.45),
                                  sphere(_up(p), (19.0, 26.0, 0.0), 1.0)), CHROME),
            Part(_chain(strap, 0.9, _up), STRAP),
        ]

    return IconModel("weapon_icon_transistor", parts, materials, weapon_id="last_broadcast")


def stopwatch() -> IconModel:
    """Chronomètre d'entraîneur : boîtier d'acier, cadran blanc, aiguilles, poussoir, cordon rouge."""
    STEEL, FACE, HAND, CORD = range(4)
    materials = [
        make_material("steel", "#B8BCC4", contrast=0.7),
        make_material("face", "#F2F0E8", contrast=0.4),
        make_material("hand", "#2E2E34", contrast=0.6),
        make_material("cord", "#D0443A"),
    ]
    cord = [np.array([0.0, 17.0, 0.0]), np.array([-4.0, 22.0, 0.0]), np.array([-9.0, 24.0, 0.5]),
            np.array([-14.0, 22.0, 1.0]), np.array([-17.0, 17.0, 1.5])]

    def parts() -> list[Part]:
        return [
            Part(lambda p: _union(cylinder(_up(p), (0.0, -2.0, 0.0), 12.5, 3.0, 1.2, FACING),
                                  cylinder(_up(p), (0.0, 12.5, 0.0), 2.2, 1.8, 0.5),
                                  cylinder(_up(p), (0.0, 15.2, 0.0), 1.6, 1.0, 0.4),
                                  capsule(_up(p), (8.0, 8.0, 0.0), (9.8, 10.4, 0.0), 1.2)), STEEL),
            Part(lambda p: cylinder(_up(p), (0.0, -2.0, 2.0), 10.0, 1.5, 0.5, FACING), FACE),
            Part(lambda p: _union(capsule(_up(p), (0.0, -2.0, 3.8), (0.0, 6.0, 3.8), 0.7),
                                  capsule(_up(p), (0.0, -2.0, 3.8), (4.5, -5.5, 3.8), 0.7),
                                  sphere(_up(p), (0.0, -2.0, 3.8), 1.3)), HAND),
            Part(_chain(cord, 1.0, _up), CORD),
        ]

    return IconModel("weapon_icon_stopwatch", parts, materials, weapon_id="clock_hand")


def catalog() -> list[IconModel]:
    return [sickle(), parking_meter(), gym_bow(), slingshot(), umbrella(), nail_gun(), snow_shovel(), extension_cord(),
            plates(), leaf_rake(), school_bell(), scalpel(), lighthouse_lens(), music_box(), keyring(), compass(),
            instant_camera(), dowsing_rod(), eraser(), oil_lamp(), boxing_gloves(), chalks(), transistor(), stopwatch()]
