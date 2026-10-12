"""Rat (plan 39 B2) : ville. Brun-gris, oreilles et longue queue rosées ; file le long des murs."""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from ..palette import make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, sphere

FRAME_SIZE = (34, 22)
FRAME_PIVOT = (17.0, 17.0)
SCALE = 1.25

FUR, FUR_DARK, SKIN, EYE = range(4)
MATERIALS = [
    make_material("fur", "#6E5E55"),
    make_material("fur_dark", "#47393A"),
    make_material("skin", "#C99A8E", contrast=0.6),
    make_material("eye", "#141016", contrast=0.4),
]


@dataclass(frozen=True)
class Rat:
    phase: float = 0.0
    stride: float = 0.0
    sniff: float = 0.0
    stretch: float = 1.0
    tail: float = 0.0


def parts(r: Rat) -> list[Part]:
    center = np.array([0.0, 2.6, 0.0])
    head = center + np.array([0.0, 0.6 + r.sniff * 0.4, 5.6 * r.stretch])
    result = [
        Part(lambda p: ellipsoid(p, center, (2.6, 2.3, 4.6 * r.stretch)), FUR),
        Part(lambda p: ellipsoid(p, center + np.array([0.0, 1.0, -0.6]), (2.0, 1.6, 3.8)), FUR_DARK),
        Part(lambda p: ellipsoid(p, head, (1.6, 1.5, 2.6)), FUR),
        Part(lambda p: sphere(p, head + np.array([0.0, -0.3, 2.6 + r.sniff * 0.3]), 0.6), SKIN),
        Part(lambda p: sphere(p, head + np.array([1.2, 1.4, -0.6]), 0.9), SKIN),
        Part(lambda p: sphere(p, head + np.array([-1.2, 1.4, -0.6]), 0.9), SKIN),
        Part(lambda p: sphere(p, head + np.array([0.9, 0.6, 1.0]), 0.45), EYE),
        Part(lambda p: sphere(p, head + np.array([-0.9, 0.6, 1.0]), 0.45), EYE),
    ]
    root = center + np.array([0.0, -0.4, -4.4 * r.stretch])
    mid = root + np.array([r.tail * 1.8, -0.6, -3.6])
    tip = mid + np.array([-r.tail * 1.4, -0.4, -3.4])
    result += [Part(lambda p: capsule(p, root, mid, 0.6, 0.45), SKIN), Part(lambda p: capsule(p, mid, tip, 0.45, 0.25), SKIN)]
    for x, z, offset in ((1.4, 2.6, 0.0), (-1.4, 2.6, np.pi), (1.6, -2.4, np.pi), (-1.6, -2.4, 0.0)):
        hip = center + np.array([x, -1.2, z * r.stretch])
        foot = np.array([hip[0], 0.0, hip[2] + np.sin(r.phase + offset) * r.stride])
        result.append(Part(lambda p, a=hip, f=foot: capsule(p, a, f, 0.6, 0.45), FUR_DARK))
    return result


def _animations() -> dict[str, list[Rat]]:
    stand = Rat()
    return {
        "idle": [stand, replace(stand, sniff=1.0, tail=0.4), replace(stand, sniff=0.3, tail=-0.3), replace(stand, tail=0.2)],
        # Course : corps allongé, pattes qui battent, queue qui ondule.
        "run": [replace(stand, phase=i * np.pi / 2, stride=2.2, stretch=1.12, tail=(0.6, 0.0, -0.6, 0.0)[i]) for i in range(4)],
    }


ANIMATIONS = _animations()
