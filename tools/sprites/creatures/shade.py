"""
L'Ombre — presque bidimensionnelle, une tache sombre qui glisse sur le sol (Bible §6.2).
Une flaque d'encre aux reflets irisés, bordée de filaments, d'où se dresse une crête basse. Ses yeux sont les seuls
éléments solides : trois points vert-acide sur de courtes tiges, jamais alignés. Fragile, rapide, en masse.
"""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, rotation_x, sphere

FRAME_SIZE = (32, 28)
FRAME_PIVOT = (16.0, 21.0)

INK, SHEEN, CREST, EYE = range(4)
MATERIALS = [
    make_material("ink", "#1E1A28", contrast=0.6),
    make_material("sheen", "#4A3466", contrast=0.7),
    make_material("crest", "#2D1B3D", contrast=0.6),
    make_emissive("eye", "#7FFF00"),
]

K = 1.45


@dataclass(frozen=True)
class Stain:
    stretch: float = 1.0
    rise: float = 0.0
    ripple: float = 0.0
    lean: float = 0.0
    sink: float = 0.0
    eye_lift: float = 0.0


def parts(s: Stain) -> list[Part]:
    flat = 1.0 - s.sink
    body_center = np.array([0.0, 0.8 * K * flat, 0.0])
    # Crête : la partie qui se dresse, basse au repos, levée pour frapper.
    crest_top = np.array([0.0, (3.4 + 5.0 * s.rise) * K * flat, (1.0 + 2.0 * s.rise) * K])
    tendrils = []
    for index in range(6):
        angle = index * np.pi / 3 + 0.4 + s.ripple * (0.3 if index % 2 else -0.3)
        reach = (6.5 + (1.4 if index % 3 == 0 else 0.0)) * K
        tendrils.append((np.array([np.sin(angle) * 3.0 * K, 0.3 * K, np.cos(angle) * 3.0 * K * s.stretch]),
                         np.array([np.sin(angle) * reach, 0.2 * K, np.cos(angle) * reach * s.stretch])))
    # Trois yeux sur des tiges de hauteurs différentes, écartés pour se lire séparément.
    eyes = [((2.4, 5.5, 2.2), 0.72), ((-2.2, 3.6, 2.6), 0.6), ((0.2, 7.5, 0.4), 0.55)]
    result = [
        Part(lambda p: ellipsoid(p, body_center, (5.8 * K, 1.3 * K * flat + 0.3, 6.2 * K * s.stretch)), INK),
        # Reflets irisés à la surface de l'encre.
        Part(lambda p: ellipsoid(p, body_center + [1.2 * K, 0.9 * K * flat, -1.0 * K], (2.6 * K, 0.5 * K, 2.2 * K)), SHEEN),
        Part(lambda p: ellipsoid(p, body_center + [-2.2 * K, 0.8 * K * flat, 2.2 * K * s.stretch], (1.4 * K, 0.4 * K, 1.2 * K)), SHEEN),
        Part(lambda p: capsule(p, body_center + [0.0, 0.4 * K, 0.0], crest_top + [s.lean * K, 0.0, 0.0],
                               3.0 * K * flat + 0.3, 1.2 * K * flat + 0.2), CREST),
    ]
    result += [Part(lambda p, a=a, b=b: capsule(p, a, b, 0.9 * K, 0.35 * K), INK) for a, b in tendrils]
    for (x, y, z), radius in eyes:
        base = crest_top * 0.55 + np.array([x * K, 0.0, z * K])
        tip = base + np.array([0.0, (y * 0.3 + s.eye_lift) * K * flat, 0.0])
        result += [
            Part(lambda p, a=base, b=tip: capsule(p, a, b, 0.4 * K, 0.3 * K), CREST),
            Part(lambda p, c=tip, r=radius: sphere(p, c, r * K), EYE),
        ]
    return result


def _animations() -> dict[str, list[Stain]]:
    rest = Stain()
    return {
        "idle": [rest, replace(rest, ripple=0.5, eye_lift=0.3), replace(rest, ripple=1.0, eye_lift=0.5),
                 replace(rest, ripple=0.5, eye_lift=0.2)],
        # Glissement : la flaque s'étire vers l'avant puis se rattrape.
        "walk": [replace(rest, stretch=1.25, ripple=1.0, lean=0.4), replace(rest, stretch=1.05, ripple=0.3),
                 replace(rest, stretch=1.3, ripple=-1.0, lean=-0.4), replace(rest, stretch=1.05, ripple=-0.3)],
        # Frappe : la crête se dresse comme une vague, puis retombe sur la cible.
        "attack": [replace(rest, rise=0.5, eye_lift=0.6), replace(rest, rise=1.0, stretch=1.15, eye_lift=0.9),
                   replace(rest, rise=0.4, stretch=1.35, lean=0.6), replace(rest, rise=0.1, stretch=1.1)],
        # Elle s'enfonce dans le sol, les yeux en dernier.
        "death": [replace(rest, sink=0.3, ripple=1.0, eye_lift=0.8), replace(rest, sink=0.55, stretch=1.3),
                  replace(rest, sink=0.8, stretch=1.5, eye_lift=-0.5), replace(rest, sink=0.95, stretch=1.7, eye_lift=-1.0)],
    }


ANIMATIONS = _animations()
