"""
Rendu d'un modèle SDF en sprite pixel art isométrique.

Caméra orthographique inclinée de 30° (projection 2:1 des tuiles), lumière fixe à l'écran venant du haut-gauche
(charte §5). Suréchantillonnage 4×4 pour décider couverture/matériau/ton d'un pixel, puis passes pixel art :
bandes de ton par rampe, lignes internes sur rupture de profondeur, contour sel-out extérieur, nettoyage des orphelins.
"""
from __future__ import annotations

from dataclasses import dataclass
from typing import Callable, Sequence

import numpy as np
from PIL import Image

from .palette import Material

PITCH = np.radians(30.0)
VIEW = np.array([0.0, -np.sin(PITCH), -np.cos(PITCH)])
SCREEN_RIGHT = np.array([1.0, 0.0, 0.0])
SCREEN_UP = np.array([0.0, np.cos(PITCH), -np.sin(PITCH)])
LIGHT = np.array([-0.55, 0.85, 0.5]) / np.linalg.norm([-0.55, 0.85, 0.5])

# Format des personnages : cadre, point des pieds et pixels par unité de modèle.
# Le modèle mesure ~60 unités ; à 0,62 px/unité il fait ~35 px, à l'échelle des ennemis et décors du monde.
FRAME_SIZE = (32, 48)
FRAME_PIVOT = (16.0, 45.0)
MODEL_SCALE = 0.62

SHADE_THRESHOLDS = (0.34, 0.56, 0.8)
DEPTH_JUMP = 2.6
RAY_RANGE = 90.0

Distance = Callable[[np.ndarray], np.ndarray]


@dataclass(frozen=True)
class Part:
    distance: Distance
    material: int


def screen_direction_to_yaw(dx: float, dy: float) -> float:
    """Direction écran (y vers le bas) → lacet du modèle dont l'avant local est +Z (vers la caméra)."""
    ground_x = dx
    ground_z = dy / np.sin(PITCH)
    return float(np.arctan2(ground_x, ground_z))


def _evaluate(parts: Sequence[Part], points: np.ndarray) -> tuple[np.ndarray, np.ndarray]:
    best = np.full(len(points), np.inf)
    material = np.full(len(points), -1, dtype=np.int32)
    for part in parts:
        d = part.distance(points)
        closer = d < best
        best = np.where(closer, d, best)
        material = np.where(closer, part.material, material)
    return best, material


def _normals(parts: Sequence[Part], points: np.ndarray) -> np.ndarray:
    eps = 0.05
    offsets = np.array([[1, -1, -1], [-1, -1, 1], [-1, 1, -1], [1, 1, 1]], dtype=np.float64)
    normal = np.zeros_like(points)
    for offset in offsets:
        d, _ = _evaluate(parts, points + offset * eps)
        normal += offset * d[:, None]
    return normal / np.maximum(np.linalg.norm(normal, axis=1, keepdims=True), 1e-9)


def render(parts: Sequence[Part], materials: Sequence[Material], yaw: float,
           size: tuple[int, int] = FRAME_SIZE, pivot: tuple[float, float] = FRAME_PIVOT,
           scale: float = MODEL_SCALE, supersample: int = 4) -> Image.Image:
    width, height = size
    ss = supersample
    # Rayons exprimés dans l'espace du modèle : la lumière reste fixe à l'écran quelle que soit l'orientation.
    to_model = np.array([[np.cos(-yaw), 0, np.sin(-yaw)], [0, 1, 0], [-np.sin(-yaw), 0, np.cos(-yaw)]])
    view = to_model @ VIEW
    right = to_model @ SCREEN_RIGHT
    up = to_model @ SCREEN_UP
    light = to_model @ LIGHT

    sub = (np.arange(ss) + 0.5) / ss
    xs = ((np.arange(width)[:, None] + sub[None, :]).reshape(-1) - pivot[0]) / scale
    ys = (pivot[1] - (np.arange(height)[:, None] + sub[None, :]).reshape(-1)) / scale
    grid_y, grid_x = np.meshgrid(ys, xs, indexing="ij")
    plane = grid_x.reshape(-1, 1) * right + grid_y.reshape(-1, 1) * up
    origins = plane - view * RAY_RANGE * 0.5

    count = len(origins)
    t = np.zeros(count)
    hit = np.zeros(count, dtype=bool)
    active = np.ones(count, dtype=bool)
    for _ in range(110):
        index = np.nonzero(active)[0]
        if len(index) == 0:
            break
        d, _ = _evaluate(parts, origins[index] + view * t[index, None])
        t[index] += d * 0.85
        landed = d < 0.03
        hit[index[landed]] = True
        active[index[landed | (t[index] > RAY_RANGE)]] = False

    material = np.full(count, -1, dtype=np.int32)
    value = np.zeros(count)
    hit_index = np.nonzero(hit)[0]
    if len(hit_index):
        points = origins[hit_index] + view * t[hit_index, None]
        _, material[hit_index] = _evaluate(parts, points)
        normal = _normals(parts, points)
        diffuse = np.clip(normal @ light, 0.0, 1.0)
        shade = 0.2 + 0.8 * diffuse
        # Les faces tournées vers le sol restent dans l'ombre portée du volume.
        shade *= np.where(normal[:, 1] < -0.35, 0.82, 1.0)
        value[hit_index] = shade

    # Regroupement par pixel : (H, ss, W, ss) → (H, W, ss*ss)
    def per_pixel(array: np.ndarray) -> np.ndarray:
        return array.reshape(height, ss, width, ss).transpose(0, 2, 1, 3).reshape(height, width, ss * ss)

    hits = per_pixel(hit)
    mats = per_pixel(material)
    values = per_pixel(value)
    depths = per_pixel(np.where(hit, t, np.inf))

    covered = hits.sum(axis=2) >= (ss * ss + 1) // 2
    pixel_material = np.full((height, width), -1, dtype=np.int32)
    pixel_value = np.zeros((height, width))
    pixel_depth = np.full((height, width), np.inf)
    for y, x in zip(*np.nonzero(covered)):
        sample_mats = mats[y, x][hits[y, x]]
        counts = np.bincount(sample_mats)
        chosen = int(np.argmax(counts))
        mask = hits[y, x] & (mats[y, x] == chosen)
        pixel_material[y, x] = chosen
        pixel_value[y, x] = values[y, x][mask].mean()
        pixel_depth[y, x] = depths[y, x][mask].min()

    shade_index = np.digitize(pixel_value, SHADE_THRESHOLDS)
    shade_index = _clean_orphans(shade_index, pixel_material, covered)
    return _compose(materials, covered, pixel_material, shade_index, pixel_depth)


def _clean_orphans(shade: np.ndarray, material: np.ndarray, covered: np.ndarray) -> np.ndarray:
    """Un ton isolé entouré d'un autre ton du même matériau devient du bruit : il prend la valeur voisine."""
    result = shade.copy()
    height, width = shade.shape
    for y in range(1, height - 1):
        for x in range(1, width - 1):
            if not covered[y, x]:
                continue
            neighbours = [(y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)]
            same = [shade[n] for n in neighbours if covered[n] and material[n] == material[y, x]]
            if len(same) >= 3 and all(v != shade[y, x] for v in same):
                result[y, x] = int(np.median(same))
    return result


def _compose(materials: Sequence[Material], covered: np.ndarray, material: np.ndarray,
             shade: np.ndarray, depth: np.ndarray) -> Image.Image:
    height, width = covered.shape
    rgba = np.zeros((height, width, 4), dtype=np.uint8)
    for y, x in zip(*np.nonzero(covered)):
        mat = materials[material[y, x]]
        rgba[y, x, :3] = mat.ramp[shade[y, x]]
        rgba[y, x, 3] = 255
        # Ligne interne : ce pixel est nettement derrière un voisin (bras devant le torse, sac derrière).
        for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
            if 0 <= ny < height and 0 <= nx < width and covered[ny, nx] and depth[y, x] - depth[ny, nx] > DEPTH_JUMP:
                rgba[y, x, :3] = mat.inner_line
                break

    # Contour sel-out extérieur : teinté du matériau adjacent, plus clair côté lumière (haut-gauche).
    outline = np.zeros_like(rgba)
    for y in range(height):
        for x in range(width):
            if covered[y, x]:
                continue
            for ny, nx, lit_side in ((y + 1, x, True), (y, x + 1, True), (y - 1, x, False), (y, x - 1, False)):
                if 0 <= ny < height and 0 <= nx < width and covered[ny, nx]:
                    mat = materials[material[ny, nx]]
                    outline[y, x, :3] = mat.ramp[0] if lit_side and shade[ny, nx] >= 2 else mat.outline
                    outline[y, x, 3] = 255
                    break
    rgba = np.where(outline[..., 3:4] > 0, outline, rgba)
    return Image.fromarray(rgba, "RGBA")
