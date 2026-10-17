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

# Pixels par unité de modèle des créatures : densité commune du bestiaire (le Rôdeur fait ~29 px).
MODEL_SCALE = 0.62

# Format des personnages jouables : cadre, point des pieds et échelle propre.
# Un humanoïde mesure ~60 unités ; à 0,53 px/unité il fait ~30 px (retour « encore un peu trop grand » du
# 24 septembre, -15 % par rapport à l'échelle des créatures), sac ou arc compris ~33 px.
CHARACTER_FRAME_SIZE = (32, 40)
CHARACTER_FRAME_PIVOT = (16.0, 36.0)
CHARACTER_MODEL_SCALE = 0.53

SHADE_THRESHOLDS = (0.34, 0.56, 0.8)
EMISSIVE_SHARE = 0.25
DEPTH_JUMP = 2.6
RAY_RANGE = 90.0

Distance = Callable[[np.ndarray], np.ndarray]


@dataclass(frozen=True)
class Part:
    distance: Distance
    material: int
    # False pour un détail peint plus fin qu'un pixel (gravier, crépi) : ses marches casseraient les normales et
    # assombriraient toute la surface ; il colore sans compter dans l'éclairage.
    relief: bool = True
    # Point d'ancrage optionnel (articulation, centre de pièce) pour que le grain 3D suive le membre en mouvement.
    anchor: Sequence[float] | np.ndarray | None = None


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


def _evaluate_with_part(parts: Sequence[Part], points: np.ndarray) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    best = np.full(len(points), np.inf)
    material = np.full(len(points), -1, dtype=np.int32)
    part_idx = np.full(len(points), -1, dtype=np.int32)
    for idx, part in enumerate(parts):
        d = part.distance(points)
        closer = d < best
        best = np.where(closer, d, best)
        material = np.where(closer, part.material, material)
        part_idx = np.where(closer, idx, part_idx)
    return best, material, part_idx


def _triplanar_noise(local_p: np.ndarray) -> np.ndarray:
    """Bruit 3D déterministe à échelle macro (~4-5 px) : crée des nappes de matière sans bruit haute fréquence."""
    x, y, z = local_p[:, 0], local_p[:, 1], local_p[:, 2]
    n1 = np.sin(x * 0.34 + np.cos(y * 0.28 - z * 0.30)) * np.cos(z * 0.32 + np.sin(x * 0.26 + y * 0.30))
    n2 = np.sin((x - z) * 0.52 + y * 0.44) * 0.25
    return np.clip((n1 + n2) / 1.25, -1.0, 1.0)


def _normals(parts: Sequence[Part], points: np.ndarray) -> np.ndarray:
    eps = 0.05
    offsets = np.array([[1, -1, -1], [-1, -1, 1], [-1, 1, -1], [1, 1, 1]], dtype=np.float64)
    normal = np.zeros_like(points)
    for offset in offsets:
        d, _ = _evaluate(parts, points + offset * eps)
        normal += offset * d[:, None]
    return normal / np.maximum(np.linalg.norm(normal, axis=1, keepdims=True), 1e-9)


LAYER_NAMES = ("couleurs", "lignes internes", "contour")


def render(parts: Sequence[Part], materials: Sequence[Material], yaw: float,
           size: tuple[int, int], pivot: tuple[float, float],
           scale: float = MODEL_SCALE, supersample: int = 4, ray_range: float = RAY_RANGE,
           bounds: tuple[Sequence[float], Sequence[float]] | None = None) -> Image.Image:
    return flatten(render_layers(parts, materials, yaw, size, pivot, scale, supersample, ray_range, bounds))


def flatten(layers: Sequence[Image.Image]) -> Image.Image:
    """Superpose les calques de LAYER_NAMES : chaque pixel opaque d'un calque recouvre ceux du dessous."""
    result = layers[0].copy()
    for layer in layers[1:]:
        result.alpha_composite(layer)
    return result


def render_layers(parts: Sequence[Part], materials: Sequence[Material], yaw: float,
                  size: tuple[int, int], pivot: tuple[float, float],
                  scale: float = MODEL_SCALE, supersample: int = 4, ray_range: float = RAY_RANGE,
                  bounds: tuple[Sequence[float], Sequence[float]] | None = None,
                  smooth_slopes: bool = False) -> list[Image.Image]:
    """
    Rendu en calques séparés (LAYER_NAMES), pour une retouche dans Aseprite sans perdre la structure.
    `smooth_slopes` : pas de ligne interne sur une pente régulière (voir _compose) ; activé pour les décors.
    """
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
    origins = plane - view * ray_range * 0.5

    count = len(origins)
    t = np.zeros(count)
    hit = np.zeros(count, dtype=bool)
    active = np.ones(count, dtype=bool)
    if bounds is not None:
        # Boîte englobante (espace du modèle) : les rayons démarrent à son entrée, ceux qui la ratent sont écartés.
        low = np.asarray(bounds[0], dtype=np.float64)
        high = np.asarray(bounds[1], dtype=np.float64)
        with np.errstate(divide="ignore", invalid="ignore"):
            inverse = 1.0 / view
            t1 = (low - origins) * inverse
            t2 = (high - origins) * inverse
        near = np.nanmax(np.minimum(t1, t2), axis=1)
        far = np.nanmin(np.maximum(t1, t2), axis=1)
        active = far >= np.maximum(near, 0.0)
        t = np.where(active, np.maximum(near, 0.0), 0.0)
    for _ in range(int(110 * max(1.0, ray_range / RAY_RANGE))):
        index = np.nonzero(active)[0]
        if len(index) == 0:
            break
        d, _ = _evaluate(parts, origins[index] + view * t[index, None])
        t[index] += d * 0.85
        landed = d < 0.03
        hit[index[landed]] = True
        active[index[landed | (t[index] > ray_range)]] = False

    material = np.full(count, -1, dtype=np.int32)
    value = np.zeros(count)
    hit_index = np.nonzero(hit)[0]
    if len(hit_index):
        points = origins[hit_index] + view * t[hit_index, None]
        _, hit_mats, hit_parts = _evaluate_with_part(parts, points)
        material[hit_index] = hit_mats
        normal = _normals([part for part in parts if part.relief], points)
        diffuse = np.clip(normal @ light, 0.0, 1.0)
        shade = 0.2 + 0.8 * diffuse
        # Les faces tournées vers le sol restent dans l'ombre portée du volume.
        shade *= np.where(normal[:, 1] < -0.35, 0.82, 1.0)

        mat_roughness = np.array([m.roughness for m in materials], dtype=np.float64)[hit_mats]
        mat_specular = np.array([m.specular for m in materials], dtype=np.float64)[hit_mats]
        mat_rim = np.array([m.rim for m in materials], dtype=np.float64)[hit_mats]

        if np.any(mat_roughness > 0.0):
            local_pts = points.copy()
            for idx, part in enumerate(parts):
                if part.anchor is not None:
                    mask_p = hit_parts == idx
                    if np.any(mask_p):
                        local_pts[mask_p] -= np.asarray(part.anchor, dtype=np.float64)
            shade += mat_roughness * _triplanar_noise(local_pts) * 0.10

        if np.any(mat_specular > 0.0):
            to_cam = -view
            half_vec = (to_cam + light) / max(float(np.linalg.norm(to_cam + light)), 1e-9)
            spec = np.clip(normal @ half_vec, 0.0, 1.0) ** 10.0
            shade += mat_specular * spec * 0.44

        if np.any(mat_rim > 0.0):
            grazing = 1.0 - np.clip(normal @ (-view), 0.0, 1.0)
            upward = np.clip(normal[:, 1] + 0.35, 0.0, 1.0)
            shade += mat_rim * (grazing ** 2.4) * upward * 0.20

        value[hit_index] = np.clip(shade, 0.0, 1.2)

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
    emissive = np.array([m.emissive for m in materials], dtype=bool)
    for y, x in zip(*np.nonzero(covered)):
        sample_mats = mats[y, x][hits[y, x]]
        counts = np.bincount(sample_mats, minlength=len(materials))
        # Un œil d'un pixel à peine doit rester visible : l'émissif l'emporte dès un quart d'échantillons.
        glowing = np.where(emissive & (counts >= EMISSIVE_SHARE * ss * ss), counts, 0)
        chosen = int(np.argmax(glowing)) if glowing.any() else int(np.argmax(counts))
        mask = hits[y, x] & (mats[y, x] == chosen)
        pixel_material[y, x] = chosen
        # Pour l'émissif, la « valeur » est sa couverture : cœur clair seulement sur un pixel plein.
        pixel_value[y, x] = counts[chosen] / (ss * ss) if emissive[chosen] else values[y, x][mask].mean()
        pixel_depth[y, x] = depths[y, x][mask].min()

    # Tramage damier 2×2 uniquement à l'intérieur d'une plage du même matériau (jamais en bordure de contour).
    for y in range(1, height - 1):
        for x in range(1, width - 1):
            if not covered[y, x]:
                continue
            chosen = pixel_material[y, x]
            dither_strength = materials[chosen].dither
            if dither_strength <= 0.0 or emissive[chosen]:
                continue
            if not all(pixel_material[ny, nx] == chosen for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1))):
                continue
            val = pixel_value[y, x]
            band = 0.042 * dither_strength
            nearest_dist = min(abs(val - thr) for thr in SHADE_THRESHOLDS)
            if nearest_dist < band:
                checker = 1.0 if ((x + y) & 1) else -1.0
                pixel_value[y, x] = val + checker * band * 0.55

    shade_index = np.digitize(pixel_value, SHADE_THRESHOLDS)
    shade_index = _clean_orphans(shade_index, pixel_material, covered, materials)
    return _compose(materials, covered, pixel_material, shade_index, pixel_depth, smooth_slopes)


def _clean_orphans(
    shade: np.ndarray,
    material: np.ndarray,
    covered: np.ndarray,
    materials: Sequence[Material] | None = None,
) -> np.ndarray:
    """Un ton isolé entouré d'un autre ton du même matériau devient du bruit : il prend la valeur voisine,
    sauf s'il s'agit d'un éclat spéculaire franc ou d'un motif de dithering soutenu par au moins 2 diagonales."""
    result = shade.copy()
    height, width = shade.shape
    for y in range(1, height - 1):
        for x in range(1, width - 1):
            if not covered[y, x]:
                continue
            mat_id = material[y, x]
            cur = shade[y, x]
            if materials is not None and mat_id >= 0:
                mat = materials[mat_id]
                if mat.specular > 0.5 and cur == 3:
                    continue
                if mat.dither > 0.0:
                    diags = [(y - 1, x - 1), (y - 1, x + 1), (y + 1, x - 1), (y + 1, x + 1)]
                    diag_matches = sum(1 for d in diags if covered[d] and material[d] == mat_id and shade[d] == cur)
                    if diag_matches >= 2:
                        continue
            neighbours = [(y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)]
            same = [shade[n] for n in neighbours if covered[n] and material[n] == mat_id]
            if len(same) >= 3 and all(v != cur for v in same):
                result[y, x] = int(np.median(same))
    return result


def _compose(materials: Sequence[Material], covered: np.ndarray, material: np.ndarray,
             shade: np.ndarray, depth: np.ndarray, smooth_slopes: bool = False) -> list[Image.Image]:
    height, width = covered.shape
    fill = np.zeros((height, width, 4), dtype=np.uint8)
    inner = np.zeros_like(fill)
    for y, x in zip(*np.nonzero(covered)):
        mat = materials[material[y, x]]
        fill[y, x, :3] = mat.ramp[shade[y, x]]
        fill[y, x, 3] = 255
        if mat.emissive:
            continue
        # Ligne interne : ce pixel est nettement derrière un voisin (bras devant le torse, sac derrière).
        for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
            if not (0 <= ny < height and 0 <= nx < width and covered[ny, nx]):
                continue
            jump = depth[y, x] - depth[ny, nx]
            if jump <= DEPTH_JUMP:
                continue
            # Une surface horizontale vue à 30° s'éloigne de 2,8 unités par pixel : sans ce test, chaque pixel d'un
            # toit ou d'un plateau devenait une ligne interne. On ne trace que si l'écart rompt la pente du pixel opposé.
            oy, ox = 2 * y - ny, 2 * x - nx
            if (smooth_slopes and 0 <= oy < height and 0 <= ox < width and covered[oy, ox]
                    and material[oy, ox] == material[y, x] and jump - (depth[oy, ox] - depth[y, x]) < DEPTH_JUMP * 0.6):
                continue
            inner[y, x, :3] = mat.inner_line
            inner[y, x, 3] = 255
            break

    # Contour sel-out extérieur : teinté du matériau adjacent, plus clair côté lumière (haut-gauche).
    outline = np.zeros_like(fill)
    for y in range(height):
        for x in range(width):
            if covered[y, x]:
                continue
            for ny, nx, lit_side in ((y + 1, x, True), (y, x + 1, True), (y - 1, x, False), (y, x - 1, False)):
                if 0 <= ny < height and 0 <= nx < width and covered[ny, nx]:
                    mat = materials[material[ny, nx]]
                    outline[y, x, :3] = mat.ramp[0] if lit_side and shade[ny, nx] >= 2 and not mat.emissive else mat.outline
                    outline[y, x, 3] = 255
                    break
    return [Image.fromarray(layer, "RGBA") for layer in (fill, inner, outline)]
