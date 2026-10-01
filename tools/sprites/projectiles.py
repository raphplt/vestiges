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
    outline: str | None = None
    dissolve: bool = False
    opacity: tuple[int, ...] = ()


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
            Part(_rotated(lambda p: ellipsoid(p, (0, 0, 0), (4.6, 3.8, 4.2)), roll), 0),
            Part(_rotated(lambda p: sphere(p, (2.3, -2.0, 1.1), 2.1), roll), 1),
        )

    return ProjectileModel(materials, parts, (24, 24), 1, 4, 14)


def _orb(hex_color: str) -> ProjectileModel:
    materials = (make_emissive("core", hex_color), make_material("shell", hex_color, contrast=1.1))
    radii = (3.8, 4.5, 5.0, 4.5)

    def parts(frame: int) -> Sequence[Part]:
        radius = radii[frame]
        return (
            Part(lambda p: sphere(p, (0, 0, 0), radius * 0.62), 0),
            Part(lambda p: sphere(p, (0, 0, 0), radius), 1),
        )

    return ProjectileModel(materials, parts, (24, 24), 1, len(radii), 10)


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
    """Boule de vide aplatie, cœur clair et trois fragments de traînée."""
    materials = (make_material("void", "#6B4FA0"), make_emissive("core", "#EED4FA"))
    def parts(frame: int) -> Sequence[Part]:
        radius = (8.5, 9, 9.5, 9)[frame]
        return [Part(lambda p: ellipsoid(p, (0, 0, 2), (radius, .9, radius)), 0),
                Part(lambda p: ellipsoid(p, (-1, 1, 3), (4, 1, 4)), 1)] + [
            Part(lambda p, i=i: ellipsoid(p, ((i % 2) - .5, 0, -9 - i * 2.5 - frame % 2), (1, .7, 1)), 0)
            for i in range(3)]
    return ProjectileModel(materials, parts, (32, 24), 16, 4, 10, "#C8B8E8")


def _bile() -> ProjectileModel:
    """Goutte verdâtre irrégulière ; l'écume pâle sépare le danger des sols verts."""
    materials = (make_material("bile", "#8A9A4A"), make_emissive("foam", "#E8E0B0"))
    def parts(frame: int) -> Sequence[Part]:
        rx, ry = ((7.5, 2.4), (6.5, 3), (8, 2), (7, 2.7))[frame]
        return (
            Part(lambda p: ellipsoid(p, (0, 0, 2), (rx, ry, 8)), 0),
            Part(lambda p: capsule(p, (0, 0, -9), (0, 0, 2), .7, 4), 0),
            Part(lambda p: ellipsoid(p, (-2, 2, 3), (2.5, 1.5, 3)), 1),
            Part(lambda p: sphere(p, (rx, 0, -5 + frame), 1.5), 0),
        )
    return ProjectileModel(materials, parts, (32, 24), 16, 4, 10, "#D8E8AB")


def _flat_arc(p: np.ndarray, center, major: float, minor: float, spread: float) -> np.ndarray:
    """Croissant couché dans le plan du sol (XZ), bombé vers l'avant +Z : une onde qui court au ras du sol."""
    local = p - np.asarray(center)
    ring = np.linalg.norm(local[:, [0, 2]], axis=1) - major
    torus = np.sqrt(ring * ring + local[:, 1] ** 2) - minor
    # Seul l'arc avant est gardé : demi-espace z > major·cos(spread).
    front = major * np.cos(spread) - local[:, 2]
    return np.maximum(torus, front)


def _howl() -> ProjectileModel:
    """Cri de la Sentinelle Hurlante : deux croissants d'onde pâles, couchés au ras du sol, qui vibrent vers l'avant."""
    materials = (make_material("wave", "#E8E0D4", contrast=0.9), make_material("wave_echo", "#9E9494"))
    radii = ((9, 1.4), (11, 1.4), (13, 1.3), (14, 1.1))

    def parts(frame: int) -> Sequence[Part]:
        major, minor = radii[frame]
        return (
            Part(lambda p: _flat_arc(p, (0, 0, -5), major, minor, 1.25), 0),
            Part(lambda p: _flat_arc(p, (0, 0, -9), major * 0.8, minor * 0.8, 1.2), 1),
        )

    return ProjectileModel(materials, parts, (32, 24), 16, len(radii), 8, "#F5F0EB", opacity=(255, 232, 216, 196))


def _web() -> ProjectileModel:
    """Toile couchée : six rayons qui se déploient en trois poses."""
    materials = (make_material("silk", "#E8E0D4", contrast=0.8), make_emissive("sap", "#7FFF00"))

    def parts(frame: int) -> Sequence[Part]:
        radius = (6, 10, 13)[frame]
        points = [(np.cos(a) * radius, 0, np.sin(a) * radius) for a in np.linspace(0, np.pi * 2, 7)]
        return [Part(lambda p, b=b: capsule(p, (0, 0, 0), b, .85), 0) for b in points[:-1]] + [
            Part(lambda p, a=a, b=b: capsule(p, np.asarray(a) * .65, np.asarray(b) * .65, .75), 0)
            for a, b in zip(points, points[1:])] + [Part(lambda p: sphere(p, (0, 1, 0), 1.8), 1)]
    return ProjectileModel(materials, parts, (32, 24), 1, 3, 8, "#E8E0D4")


def _omen() -> ProjectileModel:
    materials = (make_material("lid", "#6B4FA0"), make_emissive("eye", "#C9B8FF"), make_material("pupil", "#2D1B3D"))
    def parts(frame: int) -> Sequence[Part]:
        opening = (.7, 2, 4, 6)[frame]
        return (Part(lambda p: ellipsoid(p, (0, 0, 0), (12, 1, opening)), 0),
                Part(lambda p: ellipsoid(p, (0, 1, 0), (8, .6, max(.3, opening - 1))), 1),
                Part(lambda p: ellipsoid(p, (0, 1.7, 0), (1.5, .5, max(.3, opening - 1.5))), 2))
    return ProjectileModel(materials, parts, (32, 24), 1, 4, 12, "#EED4FA")


ENEMY_IDS = ("spit", "bile", "web", "howl", "omen")


def enemy_appearance(name: str) -> ProjectileModel:
    base = MODELS[name]()
    def parts(frame: int) -> Sequence[Part]:
        factor = (.5, .85)[frame]
        return [Part(lambda p, part=part: part.distance(p / factor) * factor, part.material) for part in base.parts(0)]
    return ProjectileModel(base.materials, parts, base.frame_size, base.directions, 2, 20, base.outline)


def enemy_impact(name: str) -> ProjectileModel:
    base = MODELS[name]()
    def parts(frame: int) -> Sequence[Part]:
        radius = (4, 8, 12, 15)[frame]
        points = [(np.cos(a) * radius, 0, np.sin(a) * radius) for a in np.linspace(0, np.pi * 2, 9)]
        if name == "web":
            return [Part(lambda p, a=a, b=b: capsule(p, a, b, .8), 0) for a, b in zip(points, points[1:])] + [
                Part(lambda p, b=b: capsule(p, (0, 0, 0), b, .65), 0) for b in points[:-1]]
        return [Part(lambda p: ellipsoid(p, (0, 0, 0), (max(1, 10 - frame * 2), .6, max(1, 10 - frame * 2))), 0)] + [
            Part(lambda p, b=b: ellipsoid(p, b, (max(.6, 2 - frame * .3), .5, max(.6, 2 - frame * .3))), 0)
            for b in points[:-1]]
    return ProjectileModel(base.materials, parts, (32, 24), 1, 4, 14, base.outline, True)


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
    "omen": _omen,
}

for _name in ENEMY_IDS:
    MODELS[f"{_name}_appear"] = lambda name=_name: enemy_appearance(name)
    MODELS[f"{_name}_impact"] = lambda name=_name: enemy_impact(name)
