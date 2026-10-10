"""
L'Indicible, boss de fin de la partie classique (plan 07, lot B3) : la nuit du 14 elle-même, sans corps à viser.
Il se combat par ses mains : des mains grises de noyé qui sortent du sol (ou de l'eau) près du joueur, paume ouverte,
puis se referment pour agripper. Même chair que la main proposée pour la Barrière (planche B2a), avant-bras dressé.

Le sol remué au pied de l'avant-bras ancre la main ; le pivot est le point où le bras sort de terre.
"""
from __future__ import annotations

import numpy as np

from ..palette import make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, rounded_box, sphere
from ._kit import M, PropModel, Weathering

FLESH, NAIL, MUD, WEED = range(4)
K = 1.5 * M


def _materials() -> list:
    return [
        make_material("flesh", "#6F7B74", contrast=0.85),
        make_material("nail", "#4E5650", contrast=0.9),
        make_material("mud", "#3A3430", contrast=0.7),
        make_material("weed", "#3F5A3A", contrast=0.8),
    ]


def _union(*distances: np.ndarray) -> np.ndarray:
    result = distances[0]
    for d in distances[1:]:
        result = np.minimum(result, d)
    return result


def hand(stem: str, closed: bool, seed: int) -> PropModel:
    """Avant-bras dressé, légèrement penché vers la caméra ; paume ouverte doigts écartés, ou poing qui agrippe."""
    materials = _materials()

    def parts() -> list[Part]:
        w = Weathering(seed)
        lean = 0.18 * K
        forearm = lambda p: capsule(p, (0, -0.1 * K, 0), (0, 1.25 * K, lean), 0.24 * K, 0.2 * K)
        if closed:
            # Poing qui agrippe, cassé vers la caméra : jointures en rang sur le dessus, doigts repliés devant.
            palm = lambda p: ellipsoid(p, (0, 1.5 * K, lean * 1.3), (0.3 * K, 0.24 * K, 0.24 * K))
            xs = (-0.21 * K, -0.07 * K, 0.07 * K, 0.21 * K)
            knuckles = lambda p: _union(*(sphere(p, (x, 1.7 * K, lean * 1.3 + 0.1 * K), 0.105 * K) for x in xs))
            curled = lambda p: _union(*(capsule(p, (x, 1.68 * K, lean * 1.3 + 0.22 * K), (x * 0.9, 1.42 * K, lean * 1.3 + 0.28 * K),
                                                0.085 * K) for x in xs))
            thumb = lambda p: capsule(p, (0.3 * K, 1.38 * K, lean * 1.2), (0.08 * K, 1.5 * K, lean * 1.3 + 0.3 * K), 0.08 * K)
            fingers = lambda p: _union(knuckles(p), curled(p), thumb(p))
            nails = lambda p: _union(*(sphere(p, (x * 0.9, 1.44 * K, lean * 1.3 + 0.35 * K), 0.04 * K) for x in xs))
        else:
            palm = lambda p: rounded_box(p, (0, 1.58 * K, lean * 1.1), (0.28 * K, 0.26 * K, 0.1 * K), 0.08 * K)
            tips = []
            for i, x in enumerate((-0.21 * K, -0.07 * K, 0.07 * K, 0.21 * K)):
                spread = (i - 1.5) * 0.12 * K
                length = (0.55, 0.65, 0.62, 0.5)[i] * K
                tips.append(((x, 1.8 * K, lean * 1.1), (x + spread, 1.8 * K + length, lean * 1.1 + w.uniform(0.02, 0.12) * K)))
            thumb_seg = ((0.27 * K, 1.5 * K, lean), (0.55 * K, 1.85 * K, lean + 0.1 * K))
            fingers = lambda p: _union(*(capsule(p, a, b, 0.075 * K, 0.055 * K) for a, b in tips + [thumb_seg]))
            nails = lambda p: _union(*(sphere(p, b, 0.045 * K) for _, b in tips))
        mud = lambda p: ellipsoid(p, (0, 0.02 * K, 0), (0.62 * K, 0.06 * K, 0.5 * K))
        clods = [((w.uniform(-0.55, 0.55) * K, 0.06 * K, w.uniform(-0.4, 0.45) * K), w.uniform(0.07, 0.11) * K) for _ in range(6)]
        dirt = lambda p: _union(*(sphere(p, c, r) for c, r in clods))
        weeds = [((w.uniform(-0.2, 0.2) * K, h * K, lean * h / 1.25 + 0.2 * K), w.uniform(0.04, 0.06) * K)
                 for h in (0.35, 0.6, 0.9)]
        weed = lambda p: _union(*(sphere(p, c, r) for c, r in weeds))
        return [Part(forearm, FLESH), Part(palm, FLESH), Part(fingers, FLESH), Part(nails, NAIL),
                Part(mud, MUD), Part(dirt, MUD), Part(weed, WEED)]

    bounds = ((-1.0 * K, -0.2 * K, -0.8 * K), (1.0 * K, 2.8 * K, 1.0 * K))
    return PropModel(stem, parts, materials, 0.0, canvas=(90, 120), bounds=bounds)


def catalog() -> list[PropModel]:
    return [hand("indicible_hand_open", False, 801), hand("indicible_hand_grab", True, 801)]
