"""
Le Hurleur — un tube de chair avec une ouverture en haut (Bible §6.2).
Colonne de chair plissée, un peu penchée, qui se traîne sur trois moignons de racines. Au sommet, une bouche ronde
cerclée de dents ; quand elle s'ouvre, la lumière verte de l'intérieur éclaire le haut du corps. Deux petits yeux
décalés sur le flanc. Son cri appelle des renforts (plan 07 lot B) : la cible à abattre en premier.
"""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, cylinder, ellipsoid, rotation_x, rotation_z, sphere

FRAME_SIZE = (32, 48)
FRAME_PIVOT = (16.0, 45.0)

FLESH, FOLD, ROOT, TOOTH, THROAT, EYE = range(6)
MATERIALS = [
    make_material("flesh", "#6E7A5A"),
    make_material("fold", "#4A5440"),
    make_material("root", "#3E3428"),
    make_material("tooth", "#C9BFA6", contrast=0.6),
    make_emissive("throat", "#7FFF00"),
    make_emissive("eye", "#B8FF6A"),
]

K = 1.4
HEIGHT = 17.0 * K


@dataclass(frozen=True)
class Tube:
    lean: float = 0.12
    sway: float = 0.0
    squash: float = 1.0
    mouth: float = 0.2
    step: float = 0.0
    fall: float = 0.0


def parts(t: Tube) -> list[Part]:
    rotation = rotation_z(t.sway + t.fall * 1.2) @ rotation_x(t.lean + t.fall * 0.4)
    height = HEIGHT * t.squash

    def at(y: float, x: float = 0.0, z: float = 0.0) -> np.ndarray:
        return rotation @ np.array([x, y, z])

    top = at(height)
    result = [
        # Colonne : trois tronçons d'épaisseur inégale, plis sombres entre eux.
        Part(lambda p: capsule(p, at(2.0 * K), at(height * 0.45, 0.4 * K), 4.2 * K, 3.8 * K), FLESH),
        # Le haut est creusé : une vraie ouverture, visible d'en haut.
        Part(lambda p: np.maximum(capsule(p, at(height * 0.45, 0.4 * K), top, 3.8 * K, 3.4 * K + t.mouth * 0.6 * K),
                                  -ellipsoid(p, top + rotation @ np.array([0.0, 1.2 * K, 0.0]),
                                             ((3.0 + t.mouth) * K, 2.6 * K, (3.0 + t.mouth) * K), rotation)), FLESH),
        Part(lambda p: ellipsoid(p, at(height * 0.42, 0.4 * K), (4.3 * K, 0.9 * K, 4.2 * K), rotation), FOLD),
        Part(lambda p: ellipsoid(p, at(height * 0.72, 0.2 * K), (4.0 * K, 0.8 * K, 3.9 * K), rotation), FOLD),
        # Bouche au sommet : anneau de chair, gorge lumineuse qui s'élargit quand il crie.
        # Entonnoir évasé, plus large que le corps : l'intérieur lumineux se voit d'en haut.
        Part(lambda p: np.maximum(cylinder(p, top + rotation @ np.array([0.0, 0.4 * K, 0.0]), (4.6 + t.mouth) * K, 0.5 * K,
                                           0.3 * K, rotation),
                                  -cylinder(p, top + rotation @ np.array([0.0, 0.4 * K, 0.0]), (3.3 + t.mouth) * K, 1.0 * K,
                                            0.0, rotation)), FOLD),
        # Gorge au ras du bord : vue d'en haut, la lumière se voit toujours, et s'étale quand il crie.
        Part(lambda p: ellipsoid(p, top + rotation @ np.array([0.0, -0.2 * K, 0.0]),
                                 ((2.9 + t.mouth) * K, 0.8 * K, (2.9 + t.mouth) * K), rotation), THROAT),
        # Yeux décalés sur le flanc avant.
        Part(lambda p: sphere(p, at(height * 0.6, 1.6 * K, 3.4 * K), 0.7 * K), EYE),
        Part(lambda p: sphere(p, at(height * 0.52, -1.8 * K, 3.2 * K), 0.55 * K), EYE),
    ]
    for index in range(7):
        angle = index * np.pi * 2 / 7
        base = top + rotation @ np.array([np.cos(angle) * (3.9 + t.mouth) * K, 0.8 * K, np.sin(angle) * (3.9 + t.mouth) * K])
        tip = base + rotation @ np.array([np.cos(angle) * 0.5 * K, (1.3 + t.mouth * 0.6) * K, np.sin(angle) * 0.5 * K])
        result.append(Part(lambda p, a=base, b=tip: capsule(p, a, b, 0.45 * K, 0.15 * K), TOOTH))
    for index, angle in enumerate((0.3, 2.4, 4.3)):
        lift = max(0.0, np.sin(t.step + index * 2.1)) * 1.4 * K
        hip = at(2.5 * K, np.cos(angle) * 2.8 * K, np.sin(angle) * 2.8 * K)
        foot = np.array([np.cos(angle) * 6.0 * K, lift, np.sin(angle) * 5.0 * K])
        result.append(Part(lambda p, a=hip, b=foot: capsule(p, a, b, 1.8 * K, 1.0 * K), ROOT))
    return result


def _animations() -> dict[str, list[Tube]]:
    rest = Tube()
    return {
        "idle": [rest, replace(rest, squash=1.03, mouth=0.3), replace(rest, squash=1.05, mouth=0.35, sway=0.04),
                 replace(rest, squash=1.02, mouth=0.25)],
        "walk": [replace(rest, step=i * np.pi / 2, sway=(0.08, 0.0, -0.08, 0.0)[i], squash=(1.0, 0.97, 1.0, 0.97)[i])
                 for i in range(4)],
        # Cri : il se tasse, puis s'étire bouche grande ouverte, gorge en pleine lumière.
        "attack": [replace(rest, squash=0.88, mouth=0.1, lean=0.2), replace(rest, squash=1.12, mouth=1.0, lean=-0.05),
                   replace(rest, squash=1.15, mouth=1.2, lean=-0.1), replace(rest, squash=1.0, mouth=0.5)],
        "death": [replace(rest, mouth=1.0, squash=0.95, fall=0.15), replace(rest, mouth=0.6, squash=0.85, fall=0.45),
                  replace(rest, mouth=0.3, squash=0.75, fall=0.85), replace(rest, mouth=0.0, squash=0.7, fall=1.2)],
    }


ANIMATIONS = _animations()
