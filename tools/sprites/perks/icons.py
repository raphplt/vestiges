"""
Icônes de perks, proposition 2 (plan 05) : chaque perk est un fragment de mémoire, éclat de cristal à facettes teinté
par la famille du perk, gravé d'un pictogramme clair.

Même rendu que les icônes d'armes (lumière haut-gauche, quatre tons, contour sel-out). L'éclat est tourné vers la
caméra ; sa silhouette est commune à tous les perks (on reconnaît un perk), le pictogramme dit la règle. Les
pictogrammes reprennent autant que possible les retours visuels du jeu : coins du repère de Convergence, « » » du
coup renforcé de Débordement, liseré doré de la réserve de Prévoyance.
"""
from __future__ import annotations

from dataclasses import dataclass
from typing import Callable

import numpy as np

from ..palette import Material, make_material
from ..render import PITCH, Part
from ..sdf import capsule, ellipsoid, rounded_box, rotation_z, sphere
from ..weapons.icons import UPRIGHT, IconModel

# Repère de l'éclat : x à droite, y vers le haut du pictogramme, z vers la caméra (face au regard incliné de 30°).
_AXIS_X = np.array([1.0, 0.0, 0.0])
_AXIS_Z = np.array([0.0, np.sin(PITCH), np.cos(PITCH)])
_AXIS_Y = np.cross(_AXIS_Z, _AXIS_X)
_FRAME = np.stack([_AXIS_X, _AXIS_Y, _AXIS_Z], axis=1)

# Contour de l'éclat, convexe et irrégulier, dans le sens trigonométrique.
SHARD = np.array([(-8.5, -12.5), (5.0, -13.0), (12.5, -4.0), (10.0, 9.5), (-1.0, 13.5), (-12.0, 5.5)])
SHARD_TOP = 1.8
SHARD_BACK = 1.6
FACET_SLOPE = np.radians(38.0)
ENGRAVE = SHARD_TOP + 0.9

FAMILY_CRYSTAL = {
    "combat": "#B4473C",
    "survival": "#3F8F66",
    "collection": "#2F6FA6",
    "rewards": "#8A5CB0",
}


def frame(p: np.ndarray) -> np.ndarray:
    """Point monde → repère de l'éclat ; l'inclinaison diagonale des icônes d'armes est annulée."""
    return (p @ UPRIGHT) @ _FRAME


def shard(q: np.ndarray) -> np.ndarray:
    """Tronc de pyramide sur le contour : chaque arête donne une facette inclinée, la face avant reste plate."""
    result = np.maximum(q[:, 2] - SHARD_TOP, -q[:, 2] - SHARD_BACK)
    count = len(SHARD)
    for i in range(count):
        a, b = SHARD[i], SHARD[(i + 1) % count]
        edge = b - a
        normal = np.array([edge[1], -edge[0]]) / np.linalg.norm(edge)
        offset = normal @ a
        plane = (q[:, :2] @ normal - offset) * np.cos(FACET_SLOPE) + (q[:, 2] + SHARD_BACK) * np.sin(FACET_SLOPE)
        result = np.maximum(result, plane)
    return result


def union(*distances: np.ndarray) -> np.ndarray:
    result = distances[0]
    for d in distances[1:]:
        result = np.minimum(result, d)
    return result


def at(x: float, y: float, z: float = ENGRAVE) -> np.ndarray:
    return np.array([x, y, z])


def stroke(q: np.ndarray, points: list[tuple[float, float]], width: float, z: float = ENGRAVE) -> np.ndarray:
    """Trait gravé le long d'une polyligne."""
    return union(*(capsule(q, at(*a, z), at(*b, z), width) for a, b in zip(points, points[1:])))


def arrow(q: np.ndarray, points: list[tuple[float, float]], width: float, head: float = 3.0) -> np.ndarray:
    """Trait courbe terminé par une pointe dans l'axe du dernier segment."""
    (x0, y0), (x1, y1) = points[-2], points[-1]
    direction = np.array([x1 - x0, y1 - y0]) / np.hypot(x1 - x0, y1 - y0)
    back = -direction * head
    left = np.array([back[0] * 0.8 - back[1] * 0.6, back[0] * 0.6 + back[1] * 0.8])
    right = np.array([back[0] * 0.8 + back[1] * 0.6, -back[0] * 0.6 + back[1] * 0.8])
    return union(stroke(q, points, width), stroke(q, [(x1 + left[0], y1 + left[1]), (x1, y1), (x1 + right[0], y1 + right[1])], width))


def arc_points(cx: float, cy: float, radius: float, start: float, end: float, count: int) -> list[tuple[float, float]]:
    return [(cx + radius * np.cos(a), cy + radius * np.sin(a)) for a in np.linspace(np.radians(start), np.radians(end), count)]


def heart(q: np.ndarray, cx: float, cy: float, size: float, z: float = ENGRAVE, half: float = 0.8) -> np.ndarray:
    """Cœur 2D (forme classique de distance signée) extrudé : pointe en bas, deux lobes nets en haut."""
    u = np.abs(q[:, 0] - cx) / size
    v = (q[:, 1] - cy) / size + 0.55
    upper = u + v > 1.0
    d_upper = np.sqrt((u - 0.25) ** 2 + (v - 0.75) ** 2) - np.sqrt(2.0) / 4.0
    m = 0.5 * np.maximum(u + v, 0.0)
    d_lower = np.sqrt(np.minimum(u ** 2 + (v - 1.0) ** 2, (u - m) ** 2 + (v - m) ** 2)) * np.sign(u - v)
    flat = np.where(upper, d_upper, d_lower) * size
    return np.maximum(flat, np.abs(q[:, 2] - z) - half)


Motif = Callable[[np.ndarray], list[tuple[np.ndarray, int]]]


@dataclass(frozen=True)
class PerkIcon:
    perk_id: str
    family: str
    motif: Callable[[], tuple[list[tuple[str, str, float]], Motif]]


def build(icon: PerkIcon) -> IconModel:
    """Éclat teinté par la famille, puis les matériaux et formes du pictogramme."""
    extra, motif = icon.motif()
    materials: list[Material] = [make_material("crystal", FAMILY_CRYSTAL[icon.family], contrast=0.9)] + [
        make_material(name, color, contrast=contrast) for name, color, contrast in extra]

    def parts() -> list[Part]:
        result = [Part(lambda p: shard(frame(p)), 0)]
        for index in range(len(extra)):
            result.append(Part(lambda p, i=index: _material_part(motif, frame(p), i), index + 1))
        return result

    return IconModel(f"perk_icon_{icon.perk_id}", parts, materials, yaw=0.0)


def _material_part(motif: Motif, local: np.ndarray, material: int) -> np.ndarray:
    shapes = [distance for distance, index in motif(local) if index == material]
    return union(*shapes) if shapes else np.full(len(local), np.inf)


# --- Pictogrammes -------------------------------------------------------------------------------------------------

LIGHT = ("light", "#F3EBD6", 0.45)
GOLD = ("gold", "#E6B54C", 0.7)
ESSENCE = ("essence", "#7FDCD6", 0.55)


def convergence():
    """Convergence : les quatre coins du repère posé en jeu, autour d'une couronne."""
    def motif(q):
        corners = []
        for sx in (-1, 1):
            for sy in (-1, 1):
                x, y = sx * 7.6, sy * 7.6
                corners.append(stroke(q, [(x - sx * 3.4, y), (x, y), (x, y - sy * 3.4)], 0.9))
        band = rounded_box(q, at(0, -2.6), (4.4, 1.3, 0.9), 0.3)
        spikes = union(capsule(q, at(-3.6, -1.6), at(-4.4, 3.4), 1.3, 0.5), capsule(q, at(0, -1.6), at(0, 4.4), 1.4, 0.5),
                       capsule(q, at(3.6, -1.6), at(4.4, 3.4), 1.3, 0.5))
        gems = union(sphere(q, at(-4.4, 3.8, ENGRAVE + 0.4), 0.9), sphere(q, at(0, 4.8, ENGRAVE + 0.4), 0.9),
                     sphere(q, at(4.4, 3.8, ENGRAVE + 0.4), 0.9))
        return [(union(*corners), 0), (union(band, spikes), 1), (gems, 2)]
    return [LIGHT, GOLD, ("gem", "#D04A4A", 0.6)], motif


def overflow():
    """Débordement : le « » » du coup renforcé, qui déborde en gouttes."""
    def motif(q):
        chevrons = union(stroke(q, [(-6.0, 5.2), (-1.2, 0.0), (-6.0, -5.2)], 1.3),
                         stroke(q, [(0.4, 5.2), (5.2, 0.0), (0.4, -5.2)], 1.3))
        drops = union(sphere(q, at(7.6, 3.8), 1.3), sphere(q, at(8.4, -2.2), 1.0), sphere(q, at(7.2, -6.4), 0.8))
        return [(chevrons, 0), (drops, 1)]
    return [LIGHT, ESSENCE], motif


def carry_control():
    """Propagation : une spirale de vertige qui saute d'une cible tombée vers la suivante."""
    def motif(q):
        spiral = [(4.2 + (0.6 + 0.42 * t) * np.cos(t), 2.2 + (0.6 + 0.42 * t) * np.sin(t)) for t in np.linspace(0, 4.4 * np.pi, 30)]
        fallen = union(stroke(q, [(-8.2, -5.4), (-3.8, -9.0)], 0.9), stroke(q, [(-8.2, -9.0), (-3.8, -5.4)], 0.9))
        leap = stroke(q, arc_points(-2.0, -3.4, 5.2, 200, 40, 7), 0.7)
        return [(stroke(q, spiral, 0.75), 0), (fallen, 1), (leap, 2)]
    return [LIGHT, ("mark", "#2A2230", 0.4), ESSENCE], motif


def overheal_reserve():
    """Prévoyance : un cœur, et sous lui le liseré doré de la réserve, plein."""
    def motif(q):
        return [(heart(q, 0.0, 2.2, 10.0), 0), (rounded_box(q, at(0, -8.0), (7.8, 1.5, 0.8), 0.5), 1)]
    return [("heart", "#EDE4D2", 0.45), GOLD], motif


def rally():
    """Reprise : un cœur, et la flèche qui revient vers lui."""
    def motif(q):
        body = heart(q, -0.6, -1.4, 8.0)
        loop = arc_points(-0.6, -1.0, 8.2, 200, 25, 9)
        return [(body, 0), (arrow(q, loop, 0.95, 4.2), 1)]
    return [("heart", "#EDE4D2", 0.45), GOLD], motif


def xp_trail():
    """Sillage : le chemin parcouru en pointillés, et les orbes qu'il ramasse derrière le joueur."""
    def motif(q):
        path = [(-8.4 + 15.6 * t, -8.4 + 15.6 * t + 3.0 * np.sin(t * np.pi * 2.0)) for t in np.linspace(0.0, 1.0, 11)]
        dashes = union(*(capsule(q, at(*path[i]), at(*path[i + 1]), 1.15) for i in range(0, len(path) - 3, 2)))
        head = arrow(q, path[-3:], 1.15, 4.0)
        orbs = union(sphere(q, at(-8.0, -1.8), 1.6), sphere(q, at(-3.2, -7.6), 1.4), sphere(q, at(-1.2, 4.4), 1.2))
        return [(union(dashes, head), 0), (orbs, 1)]
    return [LIGHT, GOLD], motif


def salvage_xp():
    """Délestage : une balance, un objet d'un côté, une orbe d'XP de l'autre."""
    def motif(q):
        frame_ = union(stroke(q, [(0, -7.8), (0, 5.8)], 0.8), stroke(q, [(-7.4, 4.8), (7.4, 4.8)], 0.8),
                       rounded_box(q, at(0, -8.2), (3.4, 0.8, 0.9), 0.3),
                       stroke(q, [(-7.2, 4.8), (-7.2, -0.8)], 0.4), stroke(q, [(7.2, 4.8), (7.2, -0.8)], 0.4),
                       ellipsoid(q, at(-7.2, -1.4), (3.2, 0.9, 1.0)), ellipsoid(q, at(7.2, -1.4), (3.2, 0.9, 1.0)))
        cube = rounded_box(q, at(-7.2, 0.9), (1.7, 1.7, 1.0), 0.3)
        orb = sphere(q, at(7.2, 1.2), 2.0)
        return [(frame_, 0), (cube, 1), (orb, 2)]
    return [LIGHT, GOLD, ESSENCE], motif


def carried_choice():
    """Seconde lecture : une carte, et la flèche qui la ramène au tour suivant."""
    def motif(q):
        card = rounded_box(q, at(-2.4, -2.4), (3.8, 5.0, 0.8), 0.6)
        star = sphere(q, at(-2.4, -2.4, ENGRAVE + 0.8), 1.4)
        loop = arc_points(-2.4, -2.4, 8.0, -20, 125, 9)
        return [(card, 0), (star, 1), (arrow(q, loop, 0.95, 4.2), 2)]
    return [LIGHT, ("mark", "#8A5CB0", 0.6), GOLD], motif


def familiar_loot():
    """Habitude : le pictogramme « dupliquer », deux fois la même forme."""
    def motif(q):
        back = rounded_box(q, at(-2.4, 2.4, ENGRAVE - 0.3), (4.4, 4.4, 0.7), 1.0)
        front = rounded_box(q, at(2.4, -2.4, ENGRAVE + 0.7), (4.4, 4.4, 0.7), 1.0)
        plus = union(stroke(q, [(0.4, -2.4), (4.4, -2.4)], 0.8, ENGRAVE + 1.6), stroke(q, [(2.4, -4.4), (2.4, -0.4)], 0.8, ENGRAVE + 1.6))
        return [(back, 0), (front, 1), (plus, 2)]
    return [("pale", "#C9BFA6", 0.5), LIGHT, GOLD], motif


CATALOG = [
    PerkIcon("priority_targeting", "combat", convergence),
    PerkIcon("overflow", "combat", overflow),
    PerkIcon("carry_control", "combat", carry_control),
    PerkIcon("overheal_reserve", "survival", overheal_reserve),
    PerkIcon("rally", "survival", rally),
    PerkIcon("xp_trail", "collection", xp_trail),
    PerkIcon("salvage_xp", "rewards", salvage_xp),
    PerkIcon("carried_choice", "rewards", carried_choice),
    PerkIcon("familiar_loot", "rewards", familiar_loot),
]


def catalog() -> list[IconModel]:
    return [build(icon) for icon in CATALOG]
