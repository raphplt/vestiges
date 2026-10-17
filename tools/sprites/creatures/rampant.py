"""
Le Rampant — ce qui se déplace sous le sol et surgit (Bible §6.2, plan 07 lot B).
Hors de terre : une larve segmentée et basse, cuirassée de plaques de pierre et de mottes de terre, qui ondule. À
l'avant, une gueule ronde à trois mandibules et deux yeux vert-acide enfoncés entre les plaques. Il surgit en se
dressant, la gueule ouverte.
Reprise du 11 octobre (plan 37 M4) : brun sur brun, il se lisait comme une bûche. Chair de larve blême entre des
plaques d'ardoise sombres, mandibules d'os plus grandes.
Plan 54 S3 : ardoise et mandibules qui accrochent la lumière ; il avance par vagues de contraction (les anneaux se
serrent puis s'étirent, la tête tâte le sol) et se ramasse avant de surgir.
"""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, rotation_x, rotation_z, sphere

FRAME_SIZE = (48, 40)
FRAME_PIVOT = (24.0, 31.0)

HIDE, PLATE, EARTH, MANDIBLE, MAW, EYE = range(6)
MATERIALS = [
    make_material("hide", "#CDB99C", contrast=1.1),
    make_material("plate", "#555C6A", contrast=1.15, specular=0.4),
    make_material("earth", "#4C3A2E", contrast=1.05),
    make_material("mandible", "#C9BFA6", contrast=0.75, specular=0.6),
    make_material("maw", "#2B1B2A", contrast=0.5),
    make_emissive("eye", "#7FFF00"),
]

K = 2.4
SEGMENTS = 6


@dataclass(frozen=True)
class Larva:
    wave: float = 0.0
    amplitude: float = 0.5
    rear: float = 0.0
    jaw: float = 0.2
    roll: float = 0.0
    sink: float = 0.0
    # Contraction des anneaux (reptation) : les écarts entre segments se serrent et s'étirent avec la vague.
    squeeze: float = 0.0


def _segment(l: Larva, index: int) -> tuple[np.ndarray, float]:
    gap = 3.4 * K * (1.0 + 0.16 * l.squeeze * np.cos(l.wave + index * 1.1))
    z = (2.5 - index) * gap
    x = np.sin(l.wave + index * 1.1) * l.amplitude * K
    # Les segments de tête se dressent quand il surgit.
    lift = max(0.0, 2.0 - index) * l.rear * 3.5 * K
    radius = (3.2 - abs(index - 1.5) * 0.35) * K
    y = radius * 0.8 + lift - l.sink * radius
    return np.array([x, y, z]), radius


def parts(l: Larva) -> list[Part]:
    roll = rotation_z(l.roll)
    result = []
    for index in range(SEGMENTS):
        center, radius = _segment(l, index)
        center = roll @ center
        result += [
            Part(lambda p, c=center, r=radius: ellipsoid(p, c, (r, r * 0.8, r * 0.75)), HIDE),
            # Plaque de pierre sur le dos, un peu de travers, plus étroite que l'anneau : la chair blême se voit entre
            # deux plaques. Mottes de terre accrochées.
            Part(lambda p, c=center, r=radius, i=index: ellipsoid(p, c + [0.4 * K * (-1) ** i, r * 0.55, 0.0],
                                                                  (r * 0.75, r * 0.32, r * 0.42), rotation_z(0.25 * (-1) ** i)), PLATE),
        ]
        if index % 2 == 1:
            result.append(Part(lambda p, c=center, r=radius: sphere(p, c + [r * 0.7, r * 0.3, 0.0], r * 0.35), EARTH))
    head, radius = _segment(l, 0)
    head = roll @ head
    front = head + np.array([0.0, 0.0, radius * 0.7])
    result += [
        Part(lambda p: ellipsoid(p, front, (radius * 0.55, radius * 0.5, 0.4 * K + l.jaw * 0.3 * K)), MAW),
        Part(lambda p: sphere(p, head + [radius * 0.55, radius * 0.45, radius * 0.4], 0.8 * K), EYE),
        Part(lambda p: sphere(p, head + [-radius * 0.35, radius * 0.6, radius * 0.45], 0.65 * K), EYE),
    ]
    for angle in (0.3, 2.4, 4.4):
        spread = 1.0 + l.jaw * 0.8
        base = front + np.array([np.cos(angle) * radius * 0.45, np.sin(angle) * radius * 0.45, 0.0])
        tip = base + np.array([np.cos(angle) * spread * 1.6 * K, np.sin(angle) * spread * 1.6 * K, 2.8 * K])
        result.append(Part(lambda p, a=base, b=tip: capsule(p, a, b, 0.7 * K, 0.25 * K), MANDIBLE))
    return result


def _animations() -> dict[str, list[Larva]]:
    rest = Larva()
    return {
        "idle": [replace(rest, wave=i * np.pi / 2, amplitude=0.3, jaw=0.2 + 0.15 * (i % 2), squeeze=0.4,
                         rear=(0.0, 0.06, 0.1, 0.04)[i]) for i in range(4)],
        # Reptation : la vague de contraction parcourt les anneaux, la tête tâte le sol au temps où le corps se serre.
        "walk": [replace(rest, wave=i * np.pi / 2, amplitude=1.1, squeeze=1.0, rear=(0.12, 0.0, 0.05, 0.0)[i],
                         jaw=(0.4, 0.2, 0.3, 0.2)[i]) for i in range(4)],
        # Surgissement : il se ramasse, se dresse gueule grande ouverte, s'abat, puis retombe.
        "attack": [replace(rest, rear=0.15, jaw=0.1, squeeze=-1.0, sink=0.12, amplitude=0.2),
                   replace(rest, rear=1.15, jaw=1.2, squeeze=0.6),
                   replace(rest, rear=0.6, jaw=0.7, amplitude=0.8, squeeze=1.0),
                   replace(rest, rear=0.2, jaw=0.3)],
        "death": [replace(rest, rear=0.5, jaw=1.1), replace(rest, roll=0.5, jaw=0.7, sink=0.2),
                  replace(rest, roll=1.0, jaw=0.4, sink=0.4), replace(rest, roll=1.4, jaw=0.2, sink=0.55)],
    }


ANIMATIONS = _animations()
