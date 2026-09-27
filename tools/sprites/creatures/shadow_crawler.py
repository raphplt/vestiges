"""
Le Rampant d'Ombre — une ombre qui a appris à marcher à quatre pattes en copiant quelqu'un (plan 07, Bible §6.2).
Corps long et noir, quatre membres trop longs aux coudes relevés comme une araignée, traînées rouge sombre sur le dos.
Au bout du cou, un masque pâle et lisse, sans expression, percé de deux yeux vert-acide inégaux. Il poursuit vite.
"""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, rotation_x, rotation_z, sphere

FRAME_SIZE = (40, 36)
FRAME_PIVOT = (20.0, 28.0)

BODY, STREAK, LIMB, MASK, EYE = range(5)
MATERIALS = [
    make_material("body", "#22182A", contrast=0.7),
    make_material("streak", "#8A2A2A"),
    make_material("limb", "#2E2236", contrast=0.7),
    make_material("mask", "#D8D0C0", contrast=0.6),
    make_emissive("eye", "#7FFF00"),
]

K = 1.6
BODY_HEIGHT = 8.0 * K
HIPS = [(3.2 * K, 5.0 * K, 1.15), (-3.0 * K, 5.4 * K, 1.0), (3.0 * K, -5.0 * K, 0.95), (-3.2 * K, -4.6 * K, 1.05)]
GAIT_OFFSETS = [0.0, np.pi, np.pi, 0.0]


@dataclass(frozen=True)
class Crawler:
    phase: float = 0.0
    stride: float = 0.0
    lift: float = 0.0
    bob: float = 0.0
    pitch: float = 0.0
    crouch: float = 0.0
    neck: float = 0.0
    roll: float = 0.0
    tuck: float = 0.0


def parts(c: Crawler) -> list[Part]:
    rotation = rotation_z(c.roll) @ rotation_x(c.pitch)
    center = np.array([0.0, BODY_HEIGHT - c.crouch + c.bob, 0.0])

    def at(local) -> np.ndarray:
        return center + rotation @ np.asarray(local, dtype=np.float64)

    neck_base = at((0.0, 0.6 * K, 6.5 * K))
    head = at((0.6 * K, (2.2 + c.neck * 2.0) * K, (9.5 + c.neck * 2.5) * K))
    result = [
        Part(lambda p: ellipsoid(p, center, (3.4 * K, 2.8 * K, 7.8 * K), rotation), BODY),
        Part(lambda p: capsule(p, at((-1.0 * K, 2.4 * K, -5.0 * K)), at((0.8 * K, 2.6 * K, 4.0 * K)), 0.6 * K, 0.4 * K), STREAK),
        Part(lambda p: capsule(p, at((1.8 * K, 2.0 * K, -2.0 * K)), at((2.2 * K, 1.6 * K, 2.5 * K)), 0.4 * K, 0.3 * K), STREAK),
        Part(lambda p: capsule(p, neck_base, head, 1.4 * K, 1.1 * K), BODY),
        # Masque pâle et lisse, tourné un peu de travers.
        Part(lambda p: ellipsoid(p, head + rotation @ np.array([0.0, 0.0, 0.8 * K]), (2.2 * K, 2.6 * K, 1.2 * K),
                                 rotation @ rotation_z(0.2)), MASK),
        Part(lambda p: sphere(p, head + rotation @ np.array([0.8 * K, 0.6 * K, 1.8 * K]), 0.6 * K), EYE),
        Part(lambda p: sphere(p, head + rotation @ np.array([-0.9 * K, 0.1 * K, 1.8 * K]), 0.45 * K), EYE),
    ]
    for (x, z, length), offset in zip(HIPS, GAIT_OFFSETS):
        hip = at((x, 0.0, z))
        side = np.sign(x)
        cycle = c.phase + offset
        foot = np.array([hip[0] + side * 4.5 * K, max(np.cos(cycle), 0.0) * c.lift, hip[2] + np.sin(cycle) * c.stride])
        curled = hip + rotation @ np.array([side * 2.0 * K, -2.5 * K, 1.0 * K])
        foot = foot + (curled - foot) * c.tuck
        reach = foot - hip
        knee = hip + reach * 0.4 + rotation @ np.array([side * 2.8 * K, 5.0 * K * length, 0.0])
        result += [
            Part(lambda p, a=hip, k=knee: capsule(p, a, k, 1.0 * K, 0.8 * K), LIMB),
            Part(lambda p, k=knee, f=foot: capsule(p, k, f, 0.8 * K, 0.45 * K), LIMB),
        ]
    return result


def _animations() -> dict[str, list[Crawler]]:
    rest = Crawler()
    walk = [replace(rest, phase=i * np.pi / 2, stride=4.0 * K, lift=2.2 * K, bob=0.5 * K * (i % 2), roll=(0.05, 0.0, -0.05, 0.0)[i])
            for i in range(4)]
    return {
        "idle": [rest, replace(rest, neck=0.2, bob=0.3), replace(rest, neck=0.35, bob=0.5, roll=0.04), replace(rest, neck=0.15, bob=0.2)],
        "walk": walk,
        # Frappe : le cou se détend vers l'avant, le corps plonge.
        "attack": [replace(rest, neck=-0.4, crouch=1.2 * K, pitch=0.1), replace(rest, neck=1.0, pitch=0.25),
                   replace(rest, neck=1.2, pitch=0.3, crouch=0.8 * K), replace(rest, neck=0.4, pitch=0.1)],
        "death": [replace(rest, pitch=-0.25, neck=0.8), replace(rest, roll=0.6, crouch=2.0 * K, tuck=0.4),
                  replace(rest, roll=1.1, crouch=3.5 * K, tuck=0.8), replace(rest, roll=1.45, crouch=4.5 * K, tuck=1.0)],
    }


ANIMATIONS = _animations()
