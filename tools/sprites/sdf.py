"""Primitives de distance signée (SDF) vectorisées : P est un tableau (N, 3) de points monde."""
from __future__ import annotations

import numpy as np


def rotation_x(angle: float) -> np.ndarray:
    c, s = np.cos(angle), np.sin(angle)
    return np.array([[1, 0, 0], [0, c, -s], [0, s, c]], dtype=np.float64)


def rotation_y(angle: float) -> np.ndarray:
    c, s = np.cos(angle), np.sin(angle)
    return np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]], dtype=np.float64)


def rotation_z(angle: float) -> np.ndarray:
    c, s = np.cos(angle), np.sin(angle)
    return np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]], dtype=np.float64)


def sphere(p: np.ndarray, center, radius: float) -> np.ndarray:
    return np.linalg.norm(p - np.asarray(center), axis=1) - radius


def capsule(p: np.ndarray, a, b, radius_a: float, radius_b: float | None = None) -> np.ndarray:
    """Segment arrondi, rayon interpolé de a vers b (cône arrondi approché)."""
    a = np.asarray(a, dtype=np.float64)
    b = np.asarray(b, dtype=np.float64)
    radius_b = radius_a if radius_b is None else radius_b
    ab = b - a
    denom = max(float(ab @ ab), 1e-9)
    h = np.clip(((p - a) @ ab) / denom, 0.0, 1.0)
    closest = a + h[:, None] * ab
    return np.linalg.norm(p - closest, axis=1) - (radius_a + (radius_b - radius_a) * h)


def ellipsoid(p: np.ndarray, center, radii, rotation: np.ndarray | None = None) -> np.ndarray:
    """Approximation classique (borne) de la distance à un ellipsoïde orienté."""
    local = p - np.asarray(center)
    if rotation is not None:
        local = local @ rotation  # rotation^T appliquée ligne par ligne
    radii = np.asarray(radii, dtype=np.float64)
    k0 = np.linalg.norm(local / radii, axis=1)
    k1 = np.linalg.norm(local / (radii * radii), axis=1)
    return k0 * (k0 - 1.0) / np.maximum(k1, 1e-9)


def rounded_box(p: np.ndarray, center, half_extents, rounding: float, rotation: np.ndarray | None = None) -> np.ndarray:
    local = p - np.asarray(center)
    if rotation is not None:
        local = local @ rotation
    q = np.abs(local) - (np.asarray(half_extents) - rounding)
    outside = np.linalg.norm(np.maximum(q, 0.0), axis=1)
    inside = np.minimum(np.max(q, axis=1), 0.0)
    return outside + inside - rounding
