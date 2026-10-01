"""Petits volumes et matières partagés par les icônes d'objets."""
from __future__ import annotations

from collections.abc import Callable

import numpy as np

from ..palette import make_material
from ..render import Part
from ..sdf import capsule, cylinder, ellipsoid, rounded_box, rotation_x
from ..weapons.icons import IconModel

Shape = Callable[[np.ndarray], np.ndarray]
COLORS = {
    "steel": "#9E9494", "light": "#E8E0D4", "dark": "#3A3535", "wood": "#A68B6B",
    "rust": "#A85C30", "copper": "#B47A48", "patina": "#5A9A8A", "red": "#C4432B",
    "blue": "#5A7A9A", "navy": "#16213E", "paper": "#C8B898", "yellow": "#C4A830",
    "green": "#4A7A3A", "glass": "#8AB8C4", "violet": "#6B4FA0", "leather": "#7A5C42",
}


def union(*shapes: Shape) -> Shape:
    return lambda p: np.minimum.reduce([s(p) for s in shapes])


def cut(shape: Shape, hole: Shape) -> Shape:
    return lambda p: np.maximum(shape(p), -hole(p))


def box(center, half, rounding: float = .4) -> Shape:
    return lambda p: rounded_box(p, center, half, rounding)


def oval(center, radii) -> Shape:
    return lambda p: ellipsoid(p, center, radii)


def line(points, radius: float = .8) -> Shape:
    return union(*(lambda p, a=a, b=b: capsule(p, a, b, radius) for a, b in zip(points, points[1:])))


def disk(center, radius: float, depth: float = 1) -> Shape:
    return lambda p: cylinder(p, center, radius, depth, .2, rotation_x(np.pi / 2))


def ring(center, radius: float, tube: float = 1) -> Shape:
    def distance(p):
        q = p - np.asarray(center)
        return np.hypot(np.linalg.norm(q[:, :2], axis=1) - radius, q[:, 2]) - tube
    return distance


def polygon(vertices, z: float = 0, depth: float = 1) -> Shape:
    """Distance à un polygone simple extrudé, concave ou convexe."""
    points = np.asarray(vertices, dtype=float)
    def distance(p):
        q = p[:, :2]
        nearest = np.full(len(p), np.inf)
        inside = np.zeros(len(p), dtype=bool)
        for a, b in zip(points, np.roll(points, -1, axis=0)):
            ab = b - a
            t = np.clip(((q - a) @ ab) / max(ab @ ab, 1e-9), 0, 1)
            nearest = np.minimum(nearest, np.linalg.norm(q - a - t[:, None] * ab, axis=1))
            if b[1] != a[1]:
                crossing = ((a[1] > q[:, 1]) != (b[1] > q[:, 1])) & (q[:, 0] < (b[0] - a[0]) * (q[:, 1] - a[1]) / (b[1] - a[1]) + a[0])
                inside ^= crossing
        return np.maximum(np.where(inside, -nearest, nearest), np.abs(p[:, 2] - z) - depth)
    return distance


class Item:
    def __init__(self, item_id: str, yaw: float = .3):
        self.item_id = item_id
        self.yaw = yaw
        self.parts: list[Part] = []
        self.materials = []
        self.indices: dict[str, int] = {}

    def add(self, color: str, *shapes: Shape) -> Item:
        if color not in self.indices:
            self.indices[color] = len(self.materials)
            self.materials.append(make_material(color, COLORS.get(color, color), contrast=.8))
        self.parts.append(Part(union(*shapes), self.indices[color]))
        return self

    def model(self) -> IconModel:
        return IconModel(f"item_{self.item_id}", lambda: self.parts, self.materials, yaw=self.yaw)
