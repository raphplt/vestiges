"""
Emprise au sol, boîte de collision et règle de blocage d'un modèle low poly (plan 60, lot C2).

L'emprise se lit dans le maillage exporté : les sommets proches du bas du modèle, réduits à leur enveloppe convexe. Un
arbre n'y garde que son tronc, une maison ses murs. La boîte de collision est le rectangle orienté le plus serré autour
de cette emprise. Le blocage suit les règles des décors du jeu en 2D (`data/world/world_gen.json`, bloc `props`), dont
les seuils en pixels d'écran se ramènent aux mètres par l'échelle des sprites.
"""
from __future__ import annotations

import math

import numpy as np

# Hauteur de la coupe : une tranche basse, mais jamais plus d'un tiers du modèle (une souche ne garde pas son dessus).
SLICE_METRES = 0.5
SLICE_SHARE = 0.35
MAX_POINTS = 8
# Le contour sel-out des sprites ajoute un pixel en haut et un en bas à la hauteur que le jeu mesure.
OUTLINE_PX = 2.0


def screen_height(positions: np.ndarray, pixels_per_metre: float, pitch: float) -> float:
    """
    Hauteur à l'écran du sprite du même décor, en pixels : l'étendue verticale des sommets vus sous l'angle de la caméra
    des sprites (hauteur × cos, profondeur × sin), contour compris, comme `VisibleHeight` du jeu en 2D.
    """
    if len(positions) == 0:
        return 0.0
    projected = positions[:, 1] * math.cos(pitch) + positions[:, 2] * math.sin(pitch)
    return float(np.ptp(projected)) * pixels_per_metre + OUTLINE_PX


def _hull(points: np.ndarray) -> np.ndarray:
    """Enveloppe convexe (chaîne monotone), sens trigonométrique."""
    pts = sorted(set(map(tuple, np.round(points, 4))))
    if len(pts) < 3:
        return np.asarray(pts, dtype=float)

    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])

    lower, upper = [], []
    for p in pts:
        while len(lower) >= 2 and cross(lower[-2], lower[-1], p) <= 0:
            lower.pop()
        lower.append(p)
    for p in reversed(pts):
        while len(upper) >= 2 and cross(upper[-2], upper[-1], p) <= 0:
            upper.pop()
        upper.append(p)
    return np.asarray(lower[:-1] + upper[:-1], dtype=float)


def _simplify(hull: np.ndarray, limit: int) -> np.ndarray:
    """Retire le sommet qui porte le moins d'aire jusqu'à `limit` sommets ; le polygone reste convexe et dedans."""
    points = list(hull)
    while len(points) > limit:
        areas = []
        for i, p in enumerate(points):
            a, b = points[i - 1], points[(i + 1) % len(points)]
            areas.append(abs((p[0] - a[0]) * (b[1] - a[1]) - (p[1] - a[1]) * (b[0] - a[0])))
        points.pop(int(np.argmin(areas)))
    return np.asarray(points)


def _min_box(hull: np.ndarray) -> tuple[np.ndarray, np.ndarray, float]:
    """Rectangle d'aire minimale contenant l'enveloppe (un côté porté par une arête) : centre, demi-côtés, angle."""
    best = None
    for i in range(len(hull)):
        edge = hull[(i + 1) % len(hull)] - hull[i]
        if not edge.any():
            continue
        angle = math.atan2(edge[1], edge[0])
        c, s = math.cos(angle), math.sin(angle)
        local = hull @ np.array([[c, -s], [s, c]])
        low, high = local.min(axis=0), local.max(axis=0)
        area = float(np.prod(high - low))
        if best is None or area < best[0]:
            centre = (low + high) / 2.0
            best = (area, np.array([c * centre[0] - s * centre[1], s * centre[0] + c * centre[1]]), (high - low) / 2.0, angle)
    _, centre, half, angle = best
    # Le grand côté en premier, angle ramené dans ]−90°, 90°] : deux décors identiques donnent la même boîte.
    if half[1] > half[0]:
        half, angle = half[::-1], angle + math.pi / 2.0
    angle = (angle + math.pi / 2.0) % math.pi - math.pi / 2.0
    return centre, half, angle


def ground(positions: np.ndarray, name: str, rules: dict, pixels_per_metre: float, pitch: float) -> dict:
    """
    Le bloc `ground` du manifeste d'un décor : emprise (x, z en mètres, relative à l'origine), boîte de collision,
    ancrage, et la règle du jeu (bloquant, piétiné, décalque). `positions` : sommets du maillage, y vers le haut.
    """
    height = float(positions[:, 1].max() - positions[:, 1].min()) if len(positions) else 0.0
    height_px = screen_height(positions, pixels_per_metre, pitch)
    trampled = any(name.startswith(prefix) for prefix in rules["trample_prefixes"]) and height_px <= rules["trample_max_height"]
    block = {
        "height_m": round(height, 3),
        "screen_height_px": round(height_px, 1),
        "blocking": height_px >= rules["min_blocking_height"] and not trampled,
        "trampled": trampled,
        "decal": height_px <= rules["ground_decal_max_height"],
    }
    if len(positions) == 0:
        return block
    cut = positions[:, 1].min() + min(SLICE_METRES, SLICE_SHARE * height)
    low = positions[positions[:, 1] <= cut][:, [0, 2]]
    hull = _hull(low)
    if len(hull) < 3:
        # Une pièce trop mince pour une surface (poteau isolé) : un petit carré autour de ses points.
        centre = low.mean(axis=0)
        hull = centre + np.array([[-0.05, -0.05], [0.05, -0.05], [0.05, 0.05], [-0.05, 0.05]])
    hull = _simplify(hull, MAX_POINTS)
    centre, half, angle = _min_box(hull)
    block.update({
        "footprint": [[round(float(x), 3), round(float(z), 3)] for x, z in hull],
        "anchor": [round(float(centre[0]), 3), round(float(centre[1]), 3)],
        "collision": {
            "centre": [round(float(centre[0]), 3), round(float(centre[1]), 3)],
            "half_size": [round(float(half[0]), 3), round(float(half[1]), 3)],
            "angle_deg": round(math.degrees(angle), 1),
            "height_m": round(height, 3),
        },
    })
    return block
