"""
Détails de surface des décors : rainures d'écorce, veinage, taches de rouille, joints de pierre, liseré d'eau.

Un détail est un second volume qui épouse la surface d'un volume existant, gonflé d'une fraction de pixel et
restreint par un masque (négatif là où la couleur change). Le rendu choisit à chaque pixel la pièce la plus proche :
là où le masque est négatif, c'est le détail qui l'emporte et sa couleur qui s'affiche, sans changer la silhouette.
Le masque est un bruit de valeur 3D reproductible ; étiré le long d'un axe, il donne des fibres (écorce, planches).
"""
from __future__ import annotations

from typing import Callable, Sequence

import numpy as np

from ._kit import M

Distance = Callable[[np.ndarray], np.ndarray]

# Épaisseur du détail : bien moins qu'un pixel à l'échelle des décors, assez pour gagner le choix du matériau.
PAINT_DEPTH = 0.012 * M
# Au-delà de cette distance au volume, le masque n'est pas évalué : le bruit ne coûte qu'à la surface.
_NEAR = 0.25 * M

_PRIMES = (np.uint64(0x9E3779B1), np.uint64(0x85EBCA77), np.uint64(0xC2B2AE3D), np.uint64(0x27D4EB2F))


def _lattice(ix: np.ndarray, iy: np.ndarray, iz: np.ndarray, seed: int) -> np.ndarray:
    """Valeur pseudo-aléatoire dans [0, 1] par nœud de grille entier (hachage sans état, identique à chaque rendu)."""
    h = (ix.astype(np.uint64) * _PRIMES[0]) ^ (iy.astype(np.uint64) * _PRIMES[1]) ^ (iz.astype(np.uint64) * _PRIMES[2])
    h ^= np.uint64(seed & 0xFFFFFFFF) * _PRIMES[3]
    h ^= h >> np.uint64(15)
    h *= _PRIMES[1]
    h ^= h >> np.uint64(13)
    return (h & np.uint64(0xFFFF)).astype(np.float64) / 65535.0


def value_noise(p: np.ndarray, period: float, seed: int, stretch: Sequence[float] = (1.0, 1.0, 1.0)) -> np.ndarray:
    """
    Bruit de valeur lissé dans [0, 1]. `stretch` allonge les motifs par axe : (1, 6, 1) donne des fibres verticales,
    l'écorce d'un tronc ; (6, 1, 1) le veinage d'une planche posée le long de x.
    """
    q = p / (period * np.asarray(stretch, dtype=np.float64))
    base = np.floor(q)
    f = q - base
    f = f * f * (3.0 - 2.0 * f)
    cell = base.astype(np.int64)
    result = np.zeros(len(p))
    for dx in (0, 1):
        wx = f[:, 0] if dx else 1.0 - f[:, 0]
        for dy in (0, 1):
            wy = f[:, 1] if dy else 1.0 - f[:, 1]
            for dz in (0, 1):
                wz = f[:, 2] if dz else 1.0 - f[:, 2]
                result += wx * wy * wz * _lattice(cell[:, 0] + dx, cell[:, 1] + dy, cell[:, 2] + dz, seed)
    return result


def noise_mask(period: float, seed: int, coverage: float, stretch: Sequence[float] = (1.0, 1.0, 1.0),
               frame: Callable[[np.ndarray], np.ndarray] | None = None) -> Distance:
    """Masque d'environ `coverage` de la surface (taches, fibres) : négatif dans la zone peinte."""
    threshold = coverage

    def mask(p: np.ndarray) -> np.ndarray:
        local = frame(p) if frame is not None else p
        # Le bruit lissé reste dans [0,2 ; 0,8] la plupart du temps : on recentre le seuil sur cette plage.
        return (value_noise(local, period, seed, stretch) - (0.2 + 0.6 * threshold)) * period

    return mask


def bands(axis: int, period: float, width: float, offset: float = 0.0,
          frame: Callable[[np.ndarray], np.ndarray] | None = None) -> Distance:
    """Lignes régulières perpendiculaires à un axe (joints de planches, assises de pierre, cerclages)."""

    def mask(p: np.ndarray) -> np.ndarray:
        local = frame(p) if frame is not None else p
        phase = np.mod(local[:, axis] - offset, period)
        return np.minimum(phase, period - phase) - width * 0.5

    return mask


def painted(shape: Distance, mask: Distance, depth: float = PAINT_DEPTH) -> Distance:
    """Détail peint sur `shape` là où `mask` est négatif ; à donner à une Part d'un autre matériau."""

    def field(p: np.ndarray) -> np.ndarray:
        d = shape(p) - depth
        near = d < _NEAR
        if near.any():
            d[near] = np.maximum(d[near], mask(p[near]))
        return d

    return field


def both(*masks: Distance) -> Distance:
    """Intersection de masques : peint là où tous sont négatifs."""
    return lambda p: np.max(np.stack([m(p) for m in masks]), axis=0)


def either(*masks: Distance) -> Distance:
    """Union de masques : peint là où l'un d'eux est négatif."""
    return lambda p: np.min(np.stack([m(p) for m in masks]), axis=0)


def waterline(shape: Distance, level: float, reach: float = 0.16 * M, seed: int | None = None,
              broken: float = 0.0) -> Distance:
    """
    Liseré d'eau claire autour de ce qui perce la surface : un anneau plat à la cote `level`, collé au volume
    (non coupé par l'eau) sur une largeur `reach`. `broken` y ouvre des trouées pour un clapot en pointillés.
    """
    thickness = 0.015 * M

    def field(p: np.ndarray) -> np.ndarray:
        d = np.abs(p[:, 1] - level) - thickness
        near = d < _NEAR
        if near.any():
            s = shape(p[near])
            ring = np.maximum(s - reach, -s)
            d[near] = np.maximum(d[near], ring)
            if broken > 0.0 and seed is not None:
                gaps = (broken - value_noise(p[near], 0.22 * M, seed)) * 0.2 * M
                d[near] = np.maximum(d[near], -gaps)
        return d

    return field


def bricks(course: float, length: float, joint: float, frame: Callable[[np.ndarray], np.ndarray] | None = None) -> Distance:
    """
    Joints d'un appareil de briques ou de pierres en quinconce : négatif sur le mortier. La coordonnée le long du mur
    est x + z, valable sur une face avant (z constant) comme sur un flanc (x constant).
    """

    def mask(p: np.ndarray) -> np.ndarray:
        local = frame(p) if frame is not None else p
        row = np.floor(local[:, 1] / course)
        along = local[:, 0] + local[:, 2] + np.mod(row, 2.0) * length * 0.5
        dy = np.mod(local[:, 1], course)
        dx = np.mod(along, length)
        horizontal = np.minimum(dy, course - dy)
        vertical = np.minimum(dx, length - dx)
        return np.minimum(horizontal, vertical) - joint * 0.5

    return mask


def cracks(period: float, seed: int, width: float, reach: float = 0.5) -> Distance:
    """
    Fissures : les lignes de niveau d'un bruit forment des tracés sinueux d'un pixel ; un second bruit n'en garde
    qu'une part (`reach`), pour des fissures isolées plutôt qu'un réseau.
    """

    def mask(p: np.ndarray) -> np.ndarray:
        lines = np.abs(value_noise(p, period, seed) - 0.5) * period - width * 0.5
        keep = (value_noise(p, period * 2.5, seed + 1) - (1.0 - reach * 0.6)) * -period
        return np.maximum(lines, keep)

    return mask
