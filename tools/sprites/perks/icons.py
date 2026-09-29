"""
Icônes de perks, proposition (plan 05) : chaque perk est un pin's émaillé du monde d'avant.

Même rendu que les icônes d'armes (lumière haut-gauche, quatre tons, contour sel-out), mais l'objet est un disque
tourné vers la caméra : un cerclage de laiton, un émail, un motif en relief qui dit la règle. Deux variantes pour la
planche : émail à la couleur de la famille du perk, ou émail commun à tous les perks.
"""
from __future__ import annotations

from dataclasses import dataclass
from typing import Callable

import numpy as np

from ..palette import Material, make_material
from ..render import PITCH, Part
from ..sdf import capsule, ellipsoid, rounded_box, rotation_z, sphere
from ..weapons.icons import UPRIGHT, IconModel

# Repère du pin's : x à droite, y vers le haut du motif, z vers la caméra (face au regard incliné de 30°).
_AXIS_X = np.array([1.0, 0.0, 0.0])
_AXIS_Z = np.array([0.0, np.sin(PITCH), np.cos(PITCH)])
_AXIS_Y = np.cross(_AXIS_Z, _AXIS_X)
_FRAME = np.stack([_AXIS_X, _AXIS_Y, _AXIS_Z], axis=1)

BADGE_RADIUS = 13.0
BADGE_HALF = 1.6
RELIEF = 2.2

FAMILY_ENAMEL = {
    "combat": "#A6433A",
    "survival": "#3F7F6A",
    "collection": "#3E7FA3",
    "rewards": "#7A5A96",
}
UNIFORM_ENAMEL = "#2E3558"


def badge(p: np.ndarray) -> np.ndarray:
    """Point monde → repère du pin's ; l'inclinaison diagonale des icônes d'armes est annulée."""
    return (p @ UPRIGHT) @ _FRAME


def disc(local: np.ndarray, radius: float, half: float, rounding: float = 0.6, z: float = 0.0) -> np.ndarray:
    radial = np.linalg.norm(local[:, :2], axis=1) - (radius - rounding)
    axial = np.abs(local[:, 2] - z) - (half - rounding)
    outside = np.linalg.norm(np.stack([np.maximum(radial, 0.0), np.maximum(axial, 0.0)], axis=1), axis=1)
    return outside + np.minimum(np.maximum(radial, axial), 0.0) - rounding


def ring(local: np.ndarray, radius: float, thickness: float, z: float) -> np.ndarray:
    """Tore d'axe z : anneau en relief."""
    radial = np.linalg.norm(local[:, :2], axis=1) - radius
    return np.sqrt(radial * radial + (local[:, 2] - z) ** 2) - thickness


def union(*distances: np.ndarray) -> np.ndarray:
    result = distances[0]
    for d in distances[1:]:
        result = np.minimum(result, d)
    return result


def at(x: float, y: float, z: float = RELIEF) -> np.ndarray:
    return np.array([x, y, z])


Motif = Callable[[np.ndarray], list[tuple[np.ndarray, int]]]


@dataclass(frozen=True)
class PerkIcon:
    perk_id: str
    family: str
    motif: Callable[[], tuple[list[tuple[str, str, float]], Motif]]


def build(icon: PerkIcon, enamel_hex: str) -> IconModel:
    """Pin's complet : cerclage de laiton, émail, puis les matériaux et formes du motif."""
    extra, motif = icon.motif()
    materials: list[Material] = [
        make_material("brass", "#C9A04A", contrast=0.8),
        make_material("enamel", enamel_hex, contrast=0.6),
    ] + [make_material(name, color, contrast=contrast) for name, color, contrast in extra]

    def parts() -> list[Part]:
        result = [
            Part(lambda p: union(disc(badge(p), BADGE_RADIUS, BADGE_HALF, 0.7),
                                 ring(badge(p), BADGE_RADIUS - 0.9, 0.9, BADGE_HALF)), 0),
            Part(lambda p: disc(badge(p), BADGE_RADIUS - 1.8, 0.5, 0.3, z=BADGE_HALF + 0.2), 1),
        ]
        for index in range(len(extra)):
            result.append(Part(lambda p, i=index: _material_part(motif, badge(p), i), index + 2))
        return result

    return IconModel(f"perk_icon_{icon.perk_id}", parts, materials, yaw=0.0)


def _material_part(motif: Motif, local: np.ndarray, material: int) -> np.ndarray:
    shapes = [distance for distance, index in motif(local) if index == material]
    return union(*shapes) if shapes else np.full(len(local), np.inf)


# --- Motifs : une règle, une forme ------------------------------------------------------------------------------

IVORY = ("ivory", "#EDE3CC", 0.5)
GOLD = ("gold", "#E0B04A", 0.7)
PALE = ("pale", "#BFD8E6", 0.5)
DARK = ("dark", "#2A2230", 0.4)


def convergence():
    """Convergence : un réticule, et au centre la couronne de l'élite visée."""
    def motif(q):
        ticks = [capsule(q, at(0, 6.0), at(0, 8.6), 0.8), capsule(q, at(0, -6.0), at(0, -8.6), 0.8),
                 capsule(q, at(6.0, 0), at(8.6, 0), 0.8), capsule(q, at(-6.0, 0), at(-8.6, 0), 0.8)]
        crown = union(rounded_box(q, at(0, -1.6), (3.0, 1.0, 0.9), 0.3),
                      capsule(q, at(-2.6, -1.0), at(-3.2, 2.8), 0.7, 0.5), capsule(q, at(0, -1.0), at(0, 3.4), 0.7, 0.5),
                      capsule(q, at(2.6, -1.0), at(3.2, 2.8), 0.7, 0.5), sphere(q, at(-3.2, 3.0), 0.8),
                      sphere(q, at(0, 3.6), 0.8), sphere(q, at(3.2, 3.0), 0.8))
        return [(union(ring(q, 6.2, 0.75, RELIEF), *ticks), 0), (crown, 1)]
    return [IVORY, GOLD], motif


def overflow():
    """Débordement : un verre trop plein, l'or qui déborde en dôme et coule le long de la paroi."""
    def motif(q):
        walls = union(capsule(q, at(-4.2, 3.4), at(-3.2, -5.2), 0.7), capsule(q, at(4.2, 3.4), at(3.2, -5.2), 0.7),
                      capsule(q, at(-3.2, -5.4), at(3.2, -5.4), 0.8))
        liquid = union(rounded_box(q, at(0, -1.2, RELIEF - 0.3), (3.4, 4.2, 0.9), 0.6),
                       ellipsoid(q, at(0, 3.6), (4.8, 2.2, 1.2)),
                       capsule(q, at(4.4, 3.2), at(5.2, -1.6), 0.8, 0.9), sphere(q, at(5.3, -2.8), 1.2),
                       capsule(q, at(-4.4, 3.0), at(-5.0, 0.6), 0.7), sphere(q, at(-5.1, -0.2), 0.9))
        return [(walls, 0), (liquid, 1)]
    return [PALE, GOLD], motif


def carry_control():
    """Propagation : deux silhouettes à plat, un éclair brisé passe de la première à la seconde."""
    def motif(q):
        def figure(x, y):
            return union(sphere(q, at(x, y + 3.0, RELIEF + 0.4), 1.4), ellipsoid(q, at(x, y - 1.4, RELIEF + 0.2), (2.2, 2.4, 0.8)))
        figures = union(figure(-5.2, -3.0), figure(5.2, 1.8))
        bolt_points = [at(-2.6, 0.6, RELIEF + 0.6), at(0.8, 2.6, RELIEF + 0.6), at(-0.6, -0.4, RELIEF + 0.6), at(2.8, 1.2, RELIEF + 0.6)]
        bolt = union(*(capsule(q, a, b, 0.8) for a, b in zip(bolt_points, bolt_points[1:])))
        return [(figures, 0), (bolt, 1)]
    return [IVORY, ("spark", "#9FD3F0", 0.5)], motif


def overheal_reserve():
    """Prévoyance : un bocal fermé, rempli d'or à mi-hauteur."""
    def motif(q):
        jar = rounded_box(q, at(0, -1.2), (4.4, 5.0, 1.0), 1.2)
        lid = rounded_box(q, at(0, 4.6), (4.8, 1.2, 1.3), 0.5)
        honey = rounded_box(q, at(0, -3.0), (3.6, 2.8, 1.3), 0.9)
        return [(jar, 0), (lid, 1), (honey, 2)]
    return [PALE, ("lid", "#8A5A36", 0.7), GOLD], motif


def rally():
    """Reprise : un cœur et la flèche qui revient autour de lui."""
    def motif(q):
        heart = union(sphere(q, at(-1.9, 1.2), 2.4), sphere(q, at(1.9, 1.2), 2.4),
                      capsule(q, at(-3.2, 0.2), at(0, -3.8), 1.0), capsule(q, at(3.2, 0.2), at(0, -3.8), 1.0),
                      rounded_box(q, at(0, -0.4), (2.2, 2.0, 1.0), 0.8))
        angles = np.linspace(np.radians(200), np.radians(-20), 9)
        arc = [at(7.2 * np.cos(a), 7.2 * np.sin(a)) for a in angles]
        arrow = union(*(capsule(q, a, b, 0.65) for a, b in zip(arc, arc[1:])),
                      capsule(q, arc[-1], arc[-1] + np.array([-2.2, 0.4, 0]), 0.65),
                      capsule(q, arc[-1], arc[-1] + np.array([0.2, 2.2, 0]), 0.65))
        return [(heart, 0), (arrow, 1)]
    return [("heart", "#C4543F", 0.7), IVORY], motif


def xp_trail():
    """Sillage : une empreinte de chaussure, et les orbes qui suivent le chemin derrière elle."""
    def motif(q):
        tilt = rotation_z(np.radians(-18))
        sole = union(ellipsoid(q, at(3.2, 3.0), (2.2, 3.2, 0.9), tilt), ellipsoid(q, at(1.8, -3.0), (1.7, 1.8, 0.9), tilt))
        orbs = union(sphere(q, at(-2.4, -5.8), 1.0), sphere(q, at(-5.0, -3.6), 1.2), sphere(q, at(-6.4, 0.2), 1.4),
                     sphere(q, at(-5.6, 4.2), 1.6))
        return [(sole, 0), (orbs, 1)]
    return [IVORY, ("essence", "#6FD0CC", 0.6)], motif


def salvage_xp():
    """Délestage : une balance, un objet d'un côté, une orbe d'XP de l'autre."""
    def motif(q):
        frame = union(capsule(q, at(0, -6.4), at(0, 5.2), 0.7), capsule(q, at(-6.2, 4.2), at(6.2, 4.2), 0.7),
                      rounded_box(q, at(0, -6.6), (2.8, 0.7, 0.9), 0.3),
                      capsule(q, at(-6.0, 4.2), at(-6.0, -0.6), 0.35), capsule(q, at(6.0, 4.2), at(6.0, -0.6), 0.35))
        pans = union(ellipsoid(q, at(-6.0, -1.2), (2.8, 0.8, 1.0)), ellipsoid(q, at(6.0, -1.2), (2.8, 0.8, 1.0)))
        cube = rounded_box(q, at(-6.0, 0.8), (1.4, 1.4, 1.0), 0.3)
        orb = sphere(q, at(6.0, 0.9), 1.6)
        return [(frame, 0), (pans, 0), (cube, 1), (orb, 2)]
    return [IVORY, ("object", "#B5835A", 0.7), ("essence", "#6FD0CC", 0.6)], motif


def carried_choice():
    """Seconde lecture : une carte gardée derrière celle qu'on joue, marquée d'un signet."""
    def motif(q):
        back = rounded_box(q, at(-2.8, 1.6, RELIEF), (3.2, 4.6, 0.6), 0.5, rotation_z(np.radians(-18)))
        front = rounded_box(q, at(2.6, -1.4, RELIEF + 1.2), (3.2, 4.6, 0.6), 0.5, rotation_z(np.radians(12)))
        ribbon = union(capsule(q, at(-3.6, 5.8, RELIEF + 0.6), at(-3.2, 9.0, RELIEF + 0.6), 1.0),
                       capsule(q, at(-3.6, 5.8, RELIEF + 0.6), at(-4.2, 2.6, RELIEF + 0.6), 1.0))
        return [(back, 0), (front, 1), (ribbon, 2)]
    return [("card", "#C9A86A", 0.6), IVORY, ("ribbon", "#C4543F", 0.7)], motif


def familiar_loot():
    """Habitude : deux fois le même bouton, côte à côte."""
    def motif(q):
        def button(cx, cy):
            body = union(disc(q - at(cx, cy, 0.0) + at(0, 0, 0.0), 3.4, 0.8, 0.4, z=RELIEF))
            holes = union(*(sphere(q, at(cx + dx, cy + dy, RELIEF + 0.9), 0.75) for dx in (-1.0, 1.0) for dy in (-1.0, 1.0)))
            return body, holes
        a_body, a_holes = button(-3.8, -3.2)
        b_body, b_holes = button(3.8, 3.2)
        return [(union(a_body, b_body), 0), (union(a_holes, b_holes), 1)]
    return [("button", "#D8B25A", 0.7), DARK], motif


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


def catalog(variant: str) -> list[IconModel]:
    """« family » : émail à la couleur de la famille ; « uniform » : émail commun."""
    return [build(icon, FAMILY_ENAMEL[icon.family] if variant == "family" else UNIFORM_ENAMEL) for icon in CATALOG]
