"""
Volumes végétaux et bois communs aux biomes : feuillage en touffes, branches torses, rainures d'écorce, polypores.

Réunis ici pour que la forêt, le marais, les champs et la ferme partagent les mêmes formes sans s'importer l'un l'autre.
"""
from __future__ import annotations

from typing import Callable

import numpy as np

from ..sdf import capsule, ellipsoid, rotation_y
from ._kit import M, Weathering
from ._surface import noise_mask, painted, value_noise


def _union(*distances: np.ndarray) -> np.ndarray:
    result = distances[0]
    for d in distances[1:]:
        result = np.minimum(result, d)
    return result


def _leafy(distance: Callable[[np.ndarray], np.ndarray], amplitude: float, period: float):
    """Surface feuillue : bosses régulières qui cassent la boule lisse, sans coût de géométrie."""
    k = 2 * np.pi / period

    def field(p: np.ndarray) -> np.ndarray:
        bumps = np.sin(p[:, 0] * k) * np.sin(p[:, 1] * k * 1.3 + 1.1) * np.sin(p[:, 2] * k + 0.7)
        return distance(p) + amplitude * bumps

    return field


def _clumps(centers, radii, amplitude: float = 0.07 * M, period: float = 0.75 * M):
    return _leafy(lambda p: _union(*(ellipsoid(p, c, r) for c, r in zip(centers, radii))), amplitude, period)


def _branch_chain(points: list[np.ndarray], r0: float, r1: float):
    """Branche torse : capsules enchaînées dont le rayon décroît de r0 à r1."""
    radii = np.linspace(r0, r1, len(points))
    return lambda p: _union(*(capsule(p, a, b, ra, rb) for a, b, ra, rb in zip(points, points[1:], radii, radii[1:])))


def _gnarled(w: Weathering, start: np.ndarray, direction: np.ndarray, length: float, segments: int,
             wobble: float = 0.35) -> list[np.ndarray]:
    """Points d'une branche qui se tord un peu à chaque segment ; un fût se tord moins qu'une branche."""
    points = [start]
    heading = direction / np.linalg.norm(direction)
    step = length / segments
    for _ in range(segments):
        heading = heading + np.array([w.uniform(-wobble, wobble), w.uniform(-0.1, 0.25) * wobble / 0.35, w.uniform(-wobble, wobble)])
        heading /= np.linalg.norm(heading)
        points.append(points[-1] + heading * step)
    return points


def _align_y(direction: np.ndarray, roll: float = 0.0) -> np.ndarray:
    """Rotation qui porte l'axe Y local sur `direction` (local = p @ rotation), tournée de `roll` autour de lui."""
    y = direction / np.linalg.norm(direction)
    helper = np.array([0.0, 0.0, 1.0]) if abs(y[2]) < 0.9 else np.array([1.0, 0.0, 0.0])
    x = np.cross(y, helper)
    x /= np.linalg.norm(x)
    z = np.cross(x, y)
    x, z = x * np.cos(roll) + z * np.sin(roll), z * np.cos(roll) - x * np.sin(roll)
    return np.stack([x, y, z], axis=1)


def _sag(a: np.ndarray, b: np.ndarray, droop: float, steps: int = 6) -> list[np.ndarray]:
    """Points d'un câble ou d'une liane tendus entre deux attaches, avec une flèche au milieu."""
    return [a + (b - a) * t - np.array([0.0, droop * 4 * t * (1 - t), 0.0]) for t in np.linspace(0, 1, steps)]


def _grooves(shape, seed: int, axis: int = 1, period: float = 0.13 * M, coverage: float = 0.3,
             frame: Callable[[np.ndarray], np.ndarray] | None = None):
    """Fibres d'écorce ou de bois : un bruit étiré le long de l'axe du bois, peint en rainures sombres."""
    stretch = [1.0, 1.0, 1.0]
    stretch[axis] = 7.0
    return painted(shape, noise_mask(period, seed, coverage, tuple(stretch), frame))


def _shelves(anchors: list[tuple[np.ndarray, float, float]]):
    """Polypores en console : demi-disques plats accrochés au bois, (point, rayon, orientation)."""
    return lambda p: _union(*(ellipsoid(p, c, (r, r * 0.32, r * 0.75), rotation_y(a)) for c, r, a in anchors))


def _bounded(shape: Callable[[np.ndarray], np.ndarray], hull: Callable[[np.ndarray], np.ndarray],
             margin: float = 0.2 * M) -> Callable[[np.ndarray], np.ndarray]:
    """
    Borne rapide d'une pièce coûteuse (feuilles, lierre) : loin de l'enveloppe `hull`, sa distance suffit ; près de
    son bord, on évalue la vraie forme, sinon la borne nulle sur son bord ferait une fausse surface.
    """

    def field(p: np.ndarray) -> np.ndarray:
        d = hull(p)
        near = d < margin
        if near.any():
            d[near] = shape(p[near])
        return d

    return field


def _top_skin(shape: Callable[[np.ndarray], np.ndarray], thickness: float) -> Callable[[np.ndarray], np.ndarray]:
    """
    Masque des faces tournées vers le ciel : négatif là où monter de `thickness` éloigne du volume d'au moins la
    moitié de cette hauteur. Une face verticale, qu'on longe en montant, n'est pas prise.
    """
    lift = np.array([0.0, thickness, 0.0])
    return lambda p: thickness * 0.5 - shape(p + lift)


def _leaf_mass(centers, radii, seed: int, per: int = 7, ratio: float = 0.5, spread: float = 0.62,
               rough: float = 0.05 * M, cluster: float | None = None) -> Callable[[np.ndarray], np.ndarray]:
    """
    Feuillage en grappes : chaque touffe est un cœur ellipsoïde hérissé de `per` boules plus petites réparties sur sa
    surface. Chaque grappe prend sa propre lumière et se détache de ses voisines par une ligne interne : la lecture
    « feuille par paquet » du pixel art, sans bosses régulières. Un bruit fin (`rough`) froisse les grappes pour
    qu'aucune ne se lise en bulle parfaite. `cluster` plafonne le rayon des grappes d'une grande couronne et en ajoute
    d'autant : au-delà de ~6 px, une grappe à peine sortie de son cœur se lit en œil. Une sphère englobante par touffe
    évite d'évaluer les grappes loin d'elle.
    """
    w = Weathering(seed)
    groups = []
    for center, radius in zip(centers, radii):
        center, radius = np.asarray(center, dtype=np.float64), np.asarray(radius, dtype=np.float64)
        size = float(np.mean(radius))
        sub = size * ratio
        count = per
        if cluster is not None and sub > cluster:
            count = int(round(per * (sub / cluster) ** 2))
            sub = cluster
        subs = []
        for k in range(count):
            y = 1.0 - 2.0 * (k + 0.5) / count
            ring = np.sqrt(max(0.0, 1.0 - y * y))
            a = k * 2.39996 + w.uniform(0, 2 * np.pi)
            subs.append((center + np.array([np.cos(a) * ring, y, np.sin(a) * ring]) * radius * spread,
                         sub * w.uniform(0.85, 1.15)))
        reach = float(np.max(radius)) * spread + sub * 1.15
        groups.append((center, radius * 0.8, max(reach, float(np.max(radius)) * 0.8), subs))

    def field(p: np.ndarray) -> np.ndarray:
        d = np.full(len(p), np.inf)
        for center, core, reach, subs in groups:
            bound = np.linalg.norm(p - center, axis=1) - reach
            near = bound < 0.15 * M
            d = np.where(near, d, np.minimum(d, bound))
            if near.any():
                q = p[near]
                local = ellipsoid(q, center, core)
                for sub, r in subs:
                    local = np.minimum(local, np.linalg.norm(q - sub, axis=1) - r)
                d[near] = np.minimum(d[near], local)
        if rough > 0.0:
            skin = d < 0.1 * M
            if skin.any():
                d[skin] += (value_noise(p[skin], 0.2 * M, seed + 1) - 0.5) * 2.0 * rough
        return d

    return field


def _grass(w: Weathering, spots: list[tuple[float, float]], blades: int = 5, height: float = 0.3 * M, ground: float = 0.0):
    """
    Touffes d'herbe au pied d'un objet : brins fins qui s'écartent en éventail, (x, z) de chaque touffe ; `ground`
    les fait pousser plus haut (sur une balle pourrie, un toit).
    """
    pieces = []
    for x, z in spots:
        for index in range(blades):
            angle = w.uniform(0, 2 * np.pi)
            lean = w.uniform(0.25, 0.6)
            tip = np.array([x + np.sin(angle) * lean * height * 0.6, ground + height * w.uniform(0.6, 1.0), z + np.cos(angle) * lean * height * 0.5])
            pieces.append((np.array([x, ground, z]), tip))
    return lambda p: _union(*(capsule(p, a, b, 0.03 * M, 0.012 * M) for a, b in pieces))
