"""Projectiles du joueur et des créatures (plan 08, effets d'attaque) : petits modèles SDF rendus comme les créatures.

Un projectile vole à hauteur de buste ; son avant local est +Z. Les projectiles orientés sont prérendus
dans 16 directions d'écran pour ne jamais tourner un sprite en jeu (pixels de biais). Les objets qui
tournoient (hache, pierre) ou rayonnent (orbe, note) portent leur animation dans les frames.
"""
from __future__ import annotations

from dataclasses import dataclass
from typing import Callable, Sequence

import numpy as np

from .palette import Material, make_emissive, make_material
from .render import Part
from .sdf import capsule, ellipsoid, rotation_x, rotation_y, rotation_z, rounded_box, sphere

PartsFactory = Callable[[int], Sequence[Part]]


@dataclass(frozen=True)
class ProjectileModel:
    materials: Sequence[Material]
    parts: PartsFactory
    frame_size: tuple[int, int]
    directions: int
    frames: int
    fps: float


def _rotated(distance, rotation: np.ndarray):
    """Pivote un volume autour de l'origine du projectile (tournoiement)."""
    return lambda p: distance(p @ rotation)


def _arrow() -> ProjectileModel:
    materials = (make_material("shaft", "#A68B6B"), make_material("head", "#9E9494", contrast=1.2),
                 make_material("fletching", "#E8E0D4", contrast=0.8))

    def parts(_: int) -> Sequence[Part]:
        return (
            Part(lambda p: capsule(p, (0, 0, -10), (0, 0, 8), 1.0), 0),
            Part(lambda p: capsule(p, (0, 0, 7), (0, 0, 14), 2.8, 0.3), 1),
            Part(lambda p: rounded_box(p, (0, 0.8, -8.5), (0.4, 1.4, 2.2), 0.3), 2),
        )

    return ProjectileModel(materials, parts, (24, 24), 16, 1, 0)


def _bolt() -> ProjectileModel:
    materials = (make_material("shaft", "#5A4A38"), make_material("head", "#6B6161", contrast=1.3),
                 make_material("vane", "#A85C30"))

    def parts(_: int) -> Sequence[Part]:
        return (
            Part(lambda p: capsule(p, (0, 0, -7), (0, 0, 6), 1.4), 0),
            Part(lambda p: capsule(p, (0, 0, 6), (0, 0, 11), 2.6, 0.4), 1),
            Part(lambda p: rounded_box(p, (0, 0, -6), (0.5, 2.2, 1.8), 0.3), 2),
        )

    return ProjectileModel(materials, parts, (24, 24), 16, 1, 0)


def _needle() -> ProjectileModel:
    materials = (make_material("north", "#C4432B", contrast=0.9), make_material("south", "#E8E0D4", contrast=0.8),
                 make_material("pivot", "#D4A843"))

    def parts(_: int) -> Sequence[Part]:
        return (
            Part(lambda p: capsule(p, (0, 0, 0), (0, 0, 11), 1.9, 0.2), 0),
            Part(lambda p: capsule(p, (0, 0, 0), (0, 0, -9), 1.7, 0.3), 1),
            Part(lambda p: sphere(p, (0, 0.4, 0), 1.6), 2),
        )

    return ProjectileModel(materials, parts, (24, 24), 16, 1, 0)


def _shard() -> ProjectileModel:
    materials = (make_material("glass", "#8AB8C4", contrast=1.2), make_emissive("light", "#F5F0EB"))

    def parts(_: int) -> Sequence[Part]:
        return (
            Part(lambda p: ellipsoid(p, (0, 0, 0), (2.0, 2.4, 7.5)), 0),
            Part(lambda p: sphere(p, (0, 0.3, 2.5), 1.4), 1),
        )

    return ProjectileModel(materials, parts, (24, 24), 16, 1, 0)


def _axe() -> ProjectileModel:
    materials = (make_material("handle", "#7A5C42"), make_material("blade", "#9E9494", contrast=1.2),
                 make_material("edge", "#E8E0D4", contrast=0.7))

    def parts(frame: int) -> Sequence[Part]:
        # Tournoiement vers l'avant, un quart de tour par frame.
        spin = rotation_x(frame * np.pi / 2)
        return (
            Part(_rotated(lambda p: capsule(p, (0, -7, 0), (0, 6, 0), 1.2), spin), 0),
            Part(_rotated(lambda p: rounded_box(p, (0, 4.5, 2.6), (0.7, 2.8, 2.8), 0.4), spin), 1),
            Part(_rotated(lambda p: rounded_box(p, (0, 4.5, 5.4), (0.5, 3.1, 0.6), 0.2), spin), 2),
        )

    return ProjectileModel(materials, parts, (24, 24), 16, 4, 16)


def _stone() -> ProjectileModel:
    materials = (make_material("stone", "#7A7A70"), make_material("stone_dark", "#6B6161"))

    def parts(frame: int) -> Sequence[Part]:
        roll = rotation_z(frame * np.pi / 2)
        return (
            Part(_rotated(lambda p: ellipsoid(p, (0, 0, 0), (3.2, 2.6, 2.9)), roll), 0),
            Part(_rotated(lambda p: sphere(p, (1.6, -1.4, 0.8), 1.5), roll), 1),
        )

    return ProjectileModel(materials, parts, (16, 16), 1, 4, 14)


def _orb(hex_color: str) -> ProjectileModel:
    materials = (make_emissive("core", hex_color), make_material("shell", hex_color, contrast=1.1))
    radii = (2.4, 2.9, 3.3, 2.9)

    def parts(frame: int) -> Sequence[Part]:
        radius = radii[frame]
        return (
            Part(lambda p: sphere(p, (0, 0, 0), radius * 0.62), 0),
            Part(lambda p: sphere(p, (0, 0, 0), radius), 1),
        )

    return ProjectileModel(materials, parts, (16, 16), 1, len(radii), 10)


def _note() -> ProjectileModel:
    materials = (make_material("brass", "#D4A843", contrast=1.1),)

    def parts(frame: int) -> Sequence[Part]:
        tilt = rotation_y(0.5 if frame == 0 else -0.5)
        return (
            Part(_rotated(lambda p: ellipsoid(p, (0, -3.2, 0), (2.4, 1.7, 1.7)), tilt), 0),
            Part(_rotated(lambda p: capsule(p, (1.9, -3.0, 0), (1.9, 5.0, 0), 0.8), tilt), 0),
            Part(_rotated(lambda p: capsule(p, (1.9, 5.0, 0), (4.3, 2.4, 0), 0.8), tilt), 0),
        )

    return ProjectileModel(materials, parts, (16, 16), 1, 2, 4)


def _spit() -> ProjectileModel:
    """Crachat de créature : goutte vert-acide (Bible §6.2) qui ondule, perlée de fluide iridescent."""
    materials = (make_emissive("acid", "#7FFF00"), make_material("fluid", "#5A3A7A", contrast=1.1))
    wobble = ((3.4, 2.8), (3.0, 3.2), (3.6, 2.6), (3.1, 3.0))

    def parts(frame: int) -> Sequence[Part]:
        rx, ry = wobble[frame]
        return (
            Part(lambda p: ellipsoid(p, (0, 0, 0), (rx, ry, rx)), 0),
            Part(lambda p: sphere(p, (-rx * 0.9, -ry * 0.6, -0.8), 1.4), 1),
            Part(lambda p: sphere(p, (rx * 0.7, -ry * 0.9, -0.4), 0.9), 1),
        )

    return ProjectileModel(materials, parts, (16, 16), 1, len(wobble), 10)


def _bile() -> ProjectileModel:
    """Caillot du Cracheur Pâli : bile rouille qui tremble, écume pâle de sa chair effacée."""
    materials = (make_material("bile", "#A85C30", contrast=1.2), make_material("foam", "#E8E0D4", contrast=0.7))
    wobble = ((3.3, 2.7), (2.9, 3.1), (3.5, 2.5), (3.0, 2.9))

    def parts(frame: int) -> Sequence[Part]:
        rx, ry = wobble[frame]
        return (
            Part(lambda p: ellipsoid(p, (0, 0, 0), (rx, ry, rx)), 0),
            Part(lambda p: sphere(p, (-rx * 0.45, ry * 0.55, 0.8), 1.3), 1),
            Part(lambda p: sphere(p, (rx * 0.85, -ry * 0.7, -0.3), 0.9), 0),
        )

    return ProjectileModel(materials, parts, (16, 16), 1, len(wobble), 10)


def _torus(p: np.ndarray, center, major: float, minor: float) -> np.ndarray:
    """Anneau dans le plan XY (perpendiculaire à l'avant +Z du projectile)."""
    local = p - np.asarray(center)
    ring = np.linalg.norm(local[:, :2], axis=1) - major
    return np.sqrt(ring * ring + local[:, 2] ** 2) - minor


def _howl() -> ProjectileModel:
    """Cri de la Sentinelle Hurlante : deux anneaux d'onde pâles, perpendiculaires à la course, qui vibrent."""
    materials = (make_material("wave", "#E8E0D4", contrast=0.9), make_material("wave_echo", "#9E9494"))
    radii = ((4.2, 2.6), (4.8, 3.2))

    def parts(frame: int) -> Sequence[Part]:
        front, back = radii[frame]
        return (
            Part(lambda p: _torus(p, (0, 0, 1.5), front, 1.0), 0),
            Part(lambda p: _torus(p, (0, 0, -2.5), back, 0.8), 1),
        )

    return ProjectileModel(materials, parts, (24, 24), 16, len(radii), 8)


def _web() -> ProjectileModel:
    """Pelote de la Tisseuse : fils pâles serrés autour d'un œil de sève acide."""
    materials = (make_material("silk", "#E8E0D4", contrast=0.8), make_emissive("sap", "#7FFF00"))

    def parts(frame: int) -> Sequence[Part]:
        turn = rotation_z(frame * np.pi / 4)
        return (
            Part(_rotated(lambda p: capsule(p, (-3.2, -1.0, 0), (3.2, 1.0, 0), 1.0), turn), 0),
            Part(_rotated(lambda p: capsule(p, (-1.0, -3.2, 0), (1.0, 3.2, 0), 1.0), turn), 0),
            Part(lambda p: sphere(p, (0, 0, 0), 2.3), 0),
            Part(lambda p: sphere(p, (0, 0.3, 1.8), 0.9), 1),
        )

    return ProjectileModel(materials, parts, (16, 16), 1, 2, 6)


MODELS: dict[str, Callable[[], ProjectileModel]] = {
    "arrow": _arrow,
    "bolt": _bolt,
    "needle": _needle,
    "shard": _shard,
    "axe": _axe,
    "stone": _stone,
    "orb_essence": lambda: _orb("#5EC4C4"),
    "orb_fire": lambda: _orb("#E07B39"),
    "note": _note,
    "spit": _spit,
    "bile": _bile,
    "howl": _howl,
    "web": _web,
}
