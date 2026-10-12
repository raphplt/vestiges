"""Grenouille (plan 39 B2) : marais. Verte et brune, ventre pâle, gros yeux ; saute et plonge à l'approche."""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from ..palette import make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, rotation_x, sphere

FRAME_SIZE = (30, 28)
FRAME_PIVOT = (15.0, 23.0)
SCALE = 1.35

SKIN, SKIN_DARK, BELLY, EYE = range(4)
MATERIALS = [
    make_material("skin", "#5F7B3E"),
    make_material("skin_dark", "#3C4F2A"),
    make_material("belly", "#B8B07A", contrast=0.6),
    make_material("eye", "#D8C24A", contrast=0.6),
]


@dataclass(frozen=True)
class Frog:
    lift: float = 0.0
    extend: float = 0.0
    pitch: float = 0.0
    throat: float = 0.0


def parts(f: Frog) -> list[Part]:
    rotation = rotation_x(f.pitch)
    center = np.array([0.0, 2.4 + f.lift, 0.0])
    head = center + rotation @ np.array([0.0, 0.6, 2.8])
    result = [
        Part(lambda p: ellipsoid(p, center, (3.2, 2.2, 3.6), rotation), SKIN),
        Part(lambda p: ellipsoid(p, center + rotation @ np.array([0.0, 1.2, -0.6]), (2.4, 1.0, 2.6), rotation), SKIN_DARK),
        Part(lambda p: ellipsoid(p, center + rotation @ np.array([0.0, -1.0 - f.throat * 0.3, 1.8]), (2.2, 1.0 + f.throat * 0.4, 2.0), rotation), BELLY),
        Part(lambda p: sphere(p, head + rotation @ np.array([1.6, 1.4, 0.2]), 1.0), EYE),
        Part(lambda p: sphere(p, head + rotation @ np.array([-1.6, 1.4, 0.2]), 1.0), EYE),
    ]
    for side in (1.0, -1.0):
        hip = center + rotation @ np.array([side * 2.6, -0.6, -2.4])
        knee = hip + np.array([side * 2.0, 0.6 - f.extend * 1.4, 1.4 - f.extend * 2.4])
        foot = knee + np.array([-side * 0.6, -1.8 - f.extend * 1.0 + 0.0, -1.6 - f.extend * 2.6])
        foot[1] = max(foot[1], 0.0)
        hand = center + rotation @ np.array([side * 2.2, -2.2, 2.6 + f.extend * 0.6])
        result += [
            Part(lambda p, a=hip, k=knee: capsule(p, a, k, 0.9, 0.7), SKIN),
            Part(lambda p, k=knee, b=foot: capsule(p, k, b, 0.7, 0.5), SKIN_DARK),
            Part(lambda p, a=center + rotation @ np.array([side * 2.0, -1.0, 2.2]), b=hand: capsule(p, a, b, 0.6, 0.45), SKIN_DARK),
        ]
    return result


def _animations() -> dict[str, list[Frog]]:
    sit = Frog()
    return {
        "idle": [sit, replace(sit, throat=1.0), sit, replace(sit, throat=0.6)],
        # Saut : accroupie, détente, en l'air jambes tendues, réception.
        "jump": [replace(sit, lift=-0.4, pitch=0.1), replace(sit, lift=2.0, extend=1.0, pitch=-0.35),
                 replace(sit, lift=4.0, extend=1.0, pitch=-0.15), replace(sit, lift=1.0, extend=0.4, pitch=0.15)],
    }


ANIMATIONS = _animations()
