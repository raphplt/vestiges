"""
Le Tréant Corrompu — on croit que c'est un arbre, puis il bouge (Bible §6.2).
Tronc noueux à l'écorce fendue, penché, sur des racines qui servent de pieds. Deux branches maîtresses font des bras
inégaux, l'une plus longue, griffue de brindilles. Dans l'écorce, des excroissances ressemblent à des visages ; deux
nœuds luisent vert-acide. Une couronne de feuillage mort et de mousse. Lent, dévastateur.
"""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, rotation_x, rotation_z, sphere

FRAME_SIZE = (56, 72)
FRAME_PIVOT = (28.0, 67.0)

BARK, BARK_DARK, LEAF, MOSS, KNOT, EYE = range(6)
MATERIALS = [
    make_material("bark", "#5E4A36"),
    make_material("bark_dark", "#3A2E24"),
    make_material("leaf", "#4A5A2A"),
    make_material("moss", "#3D5C28"),
    make_material("knot", "#7A6248"),
    make_emissive("eye", "#7FFF00"),
]

K = 1.5


@dataclass(frozen=True)
class Tree:
    step: float = 0.0
    sway: float = 0.0
    swing: float = 0.0
    lean: float = 0.1
    fall: float = 0.0


def parts(t: Tree) -> list[Part]:
    rotation = rotation_z(t.sway + t.fall * 1.2) @ rotation_x(t.lean + t.fall * 0.3)

    def at(x: float, y: float, z: float = 0.0) -> np.ndarray:
        return rotation @ np.array([x * K, y * K, z * K])

    shoulder_l, shoulder_r = at(4.0, 20.0, 0.5), at(-3.6, 18.0, 0.5)
    elbow_l = at(8.5, 15.0 + 3.0 * t.swing, 3.0 + 3.0 * t.swing)
    hand_l = at(9.5, 9.0 + 7.0 * t.swing, 5.5 + 5.0 * t.swing)
    elbow_r = at(-7.0, 14.0, 2.0)
    hand_r = at(-7.5, 10.0, 4.0)
    result = [
        # Tronc : large en bas, qui se tord vers le haut.
        Part(lambda p: capsule(p, at(0.0, 3.0), at(0.4, 14.0, 0.4), 5.0 * K, 4.2 * K), BARK),
        Part(lambda p: capsule(p, at(0.4, 14.0, 0.4), at(-0.6, 22.0, -0.4), 4.2 * K, 3.4 * K), BARK),
        # Fentes sombres de l'écorce.
        Part(lambda p: ellipsoid(p, at(1.2, 9.0, 4.6), (0.6 * K, 4.0 * K, 0.5 * K), rotation), BARK_DARK),
        Part(lambda p: ellipsoid(p, at(-2.4, 16.0, 3.2), (0.5 * K, 3.0 * K, 0.5 * K), rotation @ rotation_z(0.3)), BARK_DARK),
        # Excroissances en visages ; deux nœuds luisent.
        Part(lambda p: sphere(p, at(1.8, 17.0, 3.2), 1.6 * K), KNOT),
        Part(lambda p: sphere(p, at(-1.4, 11.0, 4.2), 1.3 * K), KNOT),
        Part(lambda p: sphere(p, at(2.2, 17.4, 4.5), 0.75 * K), EYE),
        Part(lambda p: sphere(p, at(-0.4, 18.2, 4.0), 0.6 * K), EYE),
        # Couronne de feuillage mort et mousse.
        Part(lambda p: ellipsoid(p, at(-0.4, 24.5, -0.6), (6.5 * K, 3.6 * K, 6.0 * K), rotation), LEAF),
        Part(lambda p: ellipsoid(p, at(2.0, 26.5, 1.0), (3.5 * K, 2.2 * K, 3.2 * K), rotation), MOSS),
        Part(lambda p: ellipsoid(p, at(-3.5, 23.0, 1.5), (3.0 * K, 2.0 * K, 3.0 * K), rotation), LEAF),
        # Branches-bras : la gauche plus longue, griffue de brindilles.
        Part(lambda p: capsule(p, shoulder_l, elbow_l, 1.8 * K, 1.4 * K), BARK_DARK),
        Part(lambda p: capsule(p, elbow_l, hand_l, 1.4 * K, 0.9 * K), BARK_DARK),
        Part(lambda p: capsule(p, shoulder_r, elbow_r, 1.6 * K, 1.2 * K), BARK_DARK),
        Part(lambda p: capsule(p, elbow_r, hand_r, 1.2 * K, 0.8 * K), BARK_DARK),
    ]
    for spread in (-1.0, 0.0, 1.2):
        tip = hand_l + rotation @ np.array([spread * 1.2 * K, -2.5 * K, 1.8 * K])
        result.append(Part(lambda p, b=tip: capsule(p, hand_l, b, 0.5 * K, 0.2 * K), BARK))
    for index, angle in enumerate((0.5, 2.0, 3.6, 5.1)):
        lift = max(0.0, np.sin(t.step + index * np.pi / 2)) * 1.6 * K
        hip = at(np.cos(angle) * 3.0, 3.0, np.sin(angle) * 3.0)
        foot = np.array([np.cos(angle) * 7.5 * K, lift, np.sin(angle) * 6.0 * K])
        result.append(Part(lambda p, a=hip, b=foot: capsule(p, a, b, 2.0 * K, 1.0 * K), BARK_DARK))
    return result


def _animations() -> dict[str, list[Tree]]:
    rest = Tree()
    return {
        # Immobile comme un arbre : à peine un souffle dans la couronne.
        "idle": [rest, replace(rest, sway=0.015), replace(rest, sway=0.025), replace(rest, sway=0.01)],
        "walk": [replace(rest, step=i * np.pi / 2, sway=(0.05, 0.0, -0.05, 0.0)[i]) for i in range(4)],
        # Coup de branche : il arme loin en arrière et frappe de haut en bas.
        "attack": [replace(rest, swing=0.8, lean=-0.05), replace(rest, swing=1.0, lean=-0.1),
                   replace(rest, swing=-0.6, lean=0.3), replace(rest, swing=-0.3, lean=0.2)],
        "death": [replace(rest, lean=-0.1, sway=0.1), replace(rest, fall=0.3), replace(rest, fall=0.7), replace(rest, fall=1.05)],
    }


ANIMATIONS = _animations()
