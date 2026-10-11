"""
Le Tréant Corrompu — on croit que c'est un arbre, puis il bouge (Bible §6.2).
Tronc noueux à l'écorce fendue, penché, sur des racines qui servent de pieds. Deux branches maîtresses font des bras
inégaux, l'une plus longue, griffue de brindilles. Dans l'écorce, des excroissances ressemblent à des visages ; deux
nœuds luisent vert-acide. Une couronne de feuillage mort et de mousse. Lent, dévastateur.
Reprise du 11 octobre (plan 37 M4) : brun sur brun, coiffé d'une boule verte, il se lisait comme une souche. Bois
mort grisé fendu de noir, bouche creuse sous les nœuds, couronne de feuilles rousses en touffes d'où sortent des
branches nues, bras plus longs et plus clairs.
"""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, rotation_x, rotation_z, sphere

FRAME_SIZE = (56, 72)
FRAME_PIVOT = (28.0, 67.0)

BARK, BARK_DARK, LEAF, MOSS, KNOT, EYE, HOLLOW, LIMB = range(8)
MATERIALS = [
    make_material("bark", "#7C7064"),
    make_material("bark_dark", "#2F2722"),
    make_material("leaf", "#7E5F34"),
    make_material("moss", "#5F8A36"),
    make_material("knot", "#A08A6C"),
    make_emissive("eye", "#7FFF00"),
    make_material("hollow", "#1A1216", contrast=0.5),
    make_material("limb", "#6A5E54"),
]

K = 1.7


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
        Part(lambda p: capsule(p, at(0.4, 14.0, 0.4), at(-0.6, 25.0, -0.4), 4.2 * K, 3.2 * K), BARK),
        # Fentes sombres de l'écorce.
        Part(lambda p: ellipsoid(p, at(1.6, 7.0, 4.7), (0.6 * K, 3.6 * K, 0.5 * K), rotation @ rotation_z(-0.1)), BARK_DARK),
        Part(lambda p: ellipsoid(p, at(-3.0, 13.0, 3.0), (0.5 * K, 3.4 * K, 0.5 * K), rotation @ rotation_z(0.3)), BARK_DARK),
        Part(lambda p: ellipsoid(p, at(3.4, 14.0, 2.6), (0.5 * K, 2.6 * K, 0.5 * K), rotation @ rotation_z(-0.25)), BARK_DARK),
        # Excroissances en visages ; deux nœuds luisent au-dessus d'une bouche creuse.
        Part(lambda p: sphere(p, at(1.8, 17.0, 3.2), 1.7 * K), KNOT),
        Part(lambda p: sphere(p, at(-1.4, 17.6, 3.3), 1.4 * K), KNOT),
        Part(lambda p: sphere(p, at(2.1, 17.3, 4.6), 0.85 * K), EYE),
        Part(lambda p: sphere(p, at(-1.4, 17.9, 4.4), 0.7 * K), EYE),
        Part(lambda p: ellipsoid(p, at(0.2, 13.2, 4.4), (1.9 * K, 1.2 * K, 0.9 * K), rotation @ rotation_z(-0.15)), HOLLOW),
        Part(lambda p: sphere(p, at(-2.0, 8.5, 4.8), 1.2 * K), KNOT),
        # Couronne de feuillage mort en touffes, mousse, branches nues qui en sortent.
        Part(lambda p: ellipsoid(p, at(-2.4, 28.0, -1.8), (4.0 * K, 3.0 * K, 3.6 * K), rotation), LEAF),
        Part(lambda p: ellipsoid(p, at(2.2, 28.6, -2.4), (3.8 * K, 2.8 * K, 3.4 * K), rotation), LEAF),
        Part(lambda p: ellipsoid(p, at(0.2, 30.4, -0.6), (3.2 * K, 2.4 * K, 3.0 * K), rotation), LEAF),
        Part(lambda p: ellipsoid(p, at(5.6, 25.6, 0.0), (2.8 * K, 2.2 * K, 2.6 * K), rotation), LEAF),
        Part(lambda p: ellipsoid(p, at(-5.8, 25.0, 0.4), (2.6 * K, 2.0 * K, 2.4 * K), rotation), LEAF),
        Part(lambda p: ellipsoid(p, at(1.6, 31.0, 0.0), (2.4 * K, 1.6 * K, 2.2 * K), rotation), MOSS),
        Part(lambda p: ellipsoid(p, at(-3.6, 27.0, 1.6), (1.8 * K, 1.3 * K, 1.6 * K), rotation), MOSS),
        Part(lambda p: ellipsoid(p, at(-2.6, 29.2, -2.2), (2.4 * K, 1.8 * K, 2.4 * K), rotation), LEAF),
        Part(lambda p: capsule(p, at(-1.0, 27.0), at(-4.5, 34.0, -1.0), 0.8 * K, 0.3 * K), BARK_DARK),
        Part(lambda p: capsule(p, at(1.5, 27.5), at(5.5, 33.0, 0.5), 0.7 * K, 0.25 * K), BARK_DARK),
        Part(lambda p: capsule(p, at(0.0, 28.0), at(0.5, 35.0, -2.0), 0.7 * K, 0.25 * K), BARK_DARK),
        # Branches-bras : la gauche plus longue, griffue de brindilles.
        Part(lambda p: capsule(p, shoulder_l, elbow_l, 2.0 * K, 1.5 * K), LIMB),
        Part(lambda p: capsule(p, elbow_l, hand_l, 1.5 * K, 1.0 * K), LIMB),
        Part(lambda p: capsule(p, shoulder_r, elbow_r, 1.8 * K, 1.3 * K), LIMB),
        Part(lambda p: capsule(p, elbow_r, hand_r, 1.3 * K, 0.9 * K), LIMB),
    ]
    for spread in (-1.0, 0.0, 1.2):
        tip = hand_l + rotation @ np.array([spread * 1.5 * K, -3.4 * K, 2.2 * K])
        result.append(Part(lambda p, b=tip: capsule(p, hand_l, b, 0.6 * K, 0.2 * K), BARK_DARK))
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
