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


def cylinder(p: np.ndarray, center, radius: float, half_height: float, rounding: float = 0.0,
             rotation: np.ndarray | None = None) -> np.ndarray:
    """Cylindre d'axe Y local (roues, poteaux, fûts), bords éventuellement arrondis."""
    local = p - np.asarray(center)
    if rotation is not None:
        local = local @ rotation
    radial = np.linalg.norm(local[:, [0, 2]], axis=1) - (radius - rounding)
    axial = np.abs(local[:, 1]) - (half_height - rounding)
    outside = np.linalg.norm(np.stack([np.maximum(radial, 0.0), np.maximum(axial, 0.0)], axis=1), axis=1)
    inside = np.minimum(np.maximum(radial, axial), 0.0)
    return outside + inside - rounding


def smin(d1: np.ndarray, d2: np.ndarray, k: float = 1.0) -> np.ndarray:
    """Union organique lisse (polynomial smooth min) : épaules, muscles, bosses de créatures."""
    h = np.clip(0.5 + 0.5 * (d2 - d1) / np.maximum(k, 1e-6), 0.0, 1.0)
    return d2 * (1.0 - h) + d1 * h - k * h * (1.0 - h)


def smax(d1: np.ndarray, d2: np.ndarray, k: float = 1.0) -> np.ndarray:
    """Maximum lisse : utiliser smax(d1, -d2, k) pour creuser une cavité ou un pli franc."""
    return -smin(-d1, -d2, k)


def cloth_folds(
    p: np.ndarray,
    center,
    freq: float = 1.15,
    amp: float = 0.45,
    rotation: np.ndarray | None = None,
) -> np.ndarray:
    """Ondulation spatiale radiale/verticale pour sculpter des plis de manteau, cape ou tablier."""
    local = p - np.asarray(center, dtype=np.float64)
    if rotation is not None:
        local = local @ rotation
    angle = np.arctan2(local[:, 0], local[:, 2])
    wave = np.sin(angle * (4.0 * freq) + local[:, 1] * 0.22) * np.cos(local[:, 0] * freq * 0.7)
    # Plus marqué vers le bas du pan (y < 0).
    weight = np.clip(0.65 - local[:, 1] * 0.08, 0.2, 1.25)
    return wave * amp * weight


def tapered_box(
    p: np.ndarray,
    a,
    b,
    width_a: float,
    width_b: float,
    depth_a: float | None = None,
    depth_b: float | None = None,
    rounding: float = 0.35,
) -> np.ndarray:
    """Prisme trapézoïdal biseauté le long du segment a -> b (bottes, gants à revers, pans, plaques rocheuses)."""
    a = np.asarray(a, dtype=np.float64)
    b = np.asarray(b, dtype=np.float64)
    depth_a = width_a if depth_a is None else depth_a
    depth_b = width_b if depth_b is None else depth_b
    ba = b - a
    length = max(float(np.linalg.norm(ba)), 1e-6)
    axis_y = ba / length
    ref = np.array([0.0, 0.0, 1.0]) if abs(axis_y[2]) < 0.9 else np.array([1.0, 0.0, 0.0])
    axis_x = np.cross(axis_y, ref)
    axis_x /= max(float(np.linalg.norm(axis_x)), 1e-6)
    axis_z = np.cross(axis_x, axis_y)

    rel = p - a
    y = rel @ axis_y
    x = rel @ axis_x
    z = rel @ axis_z
    h = np.clip(y / length, 0.0, 1.0)
    half_w = np.maximum(width_a + (width_b - width_a) * h - rounding, 0.05)
    half_d = np.maximum(depth_a + (depth_b - depth_a) * h - rounding, 0.05)
    half_l = max(0.5 * length - rounding, 0.05)

    qx = np.abs(x) - half_w
    qy = np.abs(y - 0.5 * length) - half_l
    qz = np.abs(z) - half_d
    q = np.stack([qx, qy, qz], axis=1)
    outside = np.linalg.norm(np.maximum(q, 0.0), axis=1)
    inside = np.minimum(np.max(q, axis=1), 0.0)
    return outside + inside - rounding


def arc_smear(
    p: np.ndarray,
    center,
    normal,
    radius: float,
    thickness: float,
    angle_start: float,
    angle_end: float,
) -> np.ndarray:
    """Croissant de mouvement (smear frame) dans le plan orthogonal à `normal`, effilé aux deux extrémités."""
    center = np.asarray(center, dtype=np.float64)
    n = np.asarray(normal, dtype=np.float64)
    n = n / max(float(np.linalg.norm(n)), 1e-6)
    ref = np.array([0.0, 0.0, 1.0]) if abs(n[2]) < 0.9 else np.array([1.0, 0.0, 0.0])
    u = np.cross(n, ref)
    u /= max(float(np.linalg.norm(u)), 1e-6)
    v = np.cross(n, u)

    rel = p - center
    px = rel @ u
    pz = rel @ v
    py = rel @ n

    mid = 0.5 * (angle_start + angle_end)
    half_span = max(0.5 * abs(angle_end - angle_start), 1e-4)
    ang = np.arctan2(px, pz)
    delta = (ang - mid + np.pi) % (2.0 * np.pi) - np.pi
    clamped = np.clip(delta, -half_span, half_span)
    target_ang = mid + clamped

    closest_2d_x = np.sin(target_ang) * radius
    closest_2d_z = np.cos(target_ang) * radius
    dist_plane = np.sqrt((px - closest_2d_x) ** 2 + (pz - closest_2d_z) ** 2)

    # Épaisseur maximale au cœur du balayage, effilée vers la pointe et la traîne.
    taper = 1.0 - 0.55 * (np.abs(clamped) / half_span) ** 1.6
    local_r = thickness * taper
    return np.sqrt(dist_plane ** 2 + (py * 0.95) ** 2) - local_r
