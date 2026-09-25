"""
Socle des décors procéduraux (plan 08, lot P0).

Un décor est un modèle SDF statique rendu par le même pipeline que les créatures (MODEL_SCALE, lumière, contours).
Le cadre est ajusté automatiquement : centré horizontalement sur le point au sol (le jeu centre le sprite sur la
cellule), rogné en bas sous la silhouette. Le jeu déduit ensuite l'emprise et la collision des pixels (PropFootprint).
"""
from __future__ import annotations

from dataclasses import dataclass
from typing import Callable, Sequence

import numpy as np
from PIL import Image

from ..palette import Material
from ..render import MODEL_SCALE, Part, render

# Un humain (~30 px à l'écran, ~48 unités à MODEL_SCALE) mesure 1,75 m : 1 m ≈ 27,5 unités.
M = 27.5

# Le TileSet iso est en disposition « stacked » : une rue le long des x de cellule est horizontale à l'écran,
# une rue le long des y est verticale. Vus exactement de profil ou de face, les décors perdent leur volume (un
# grillage devient un trait) : chaque axe prend un trois-quarts proche, qui montre toujours deux faces.
AXIS_X_YAW = float(np.radians(62.0))
AXIS_Y_YAW = float(np.radians(22.0))

DEFAULT_CANVAS = (120, 110)


@dataclass(frozen=True)
class PropModel:
    """Un fichier de décor : volumes (avant local +Z), matériaux, orientation et portée de rayon."""
    stem: str
    parts: Callable[[], list[Part]]
    materials: Sequence[Material]
    yaw: float = AXIS_X_YAW
    ray_range: float = 220.0
    # Cadre de rendu avant rognage : le plus petit possible, le coût croît avec sa surface.
    canvas: tuple[int, int] = DEFAULT_CANVAS


def render_prop(model: PropModel) -> Image.Image:
    width, height = model.canvas
    pivot = (width / 2.0, height * 0.72)
    image = render(model.parts(), model.materials, model.yaw, model.canvas, pivot, MODEL_SCALE, ray_range=model.ray_range)
    return fit_frame(image, pivot)


def fit_frame(image: Image.Image, pivot: tuple[float, float]) -> Image.Image:
    alpha = np.asarray(image)[..., 3]
    ys, xs = np.nonzero(alpha)
    if len(xs) == 0:
        raise ValueError("décor vide : aucun pixel rendu")
    if xs.min() == 0 or xs.max() == image.width - 1 or ys.min() == 0 or ys.max() == image.height - 1:
        raise ValueError("décor rogné par le cadre de rendu : agrandir le canvas du modèle")
    px = int(round(pivot[0]))
    half = max(px - int(xs.min()), int(xs.max()) + 1 - px)
    return image.crop((px - half, int(ys.min()), px + half, int(ys.max()) + 1))


class Weathering:
    """Tirages reproductibles pour l'usure (rouille, mousse, éclats) : même graine, même décor."""

    def __init__(self, seed: int):
        self._rng = np.random.default_rng(seed)

    def uniform(self, low: float, high: float) -> float:
        return float(self._rng.uniform(low, high))

    def choice(self, items: Sequence):
        return items[int(self._rng.integers(0, len(items)))]

    def points_on_box(self, center, half_extents, count: int, top_only: bool = False) -> list[np.ndarray]:
        """Points sur les faces d'une boîte (dessus et côtés), pour poser taches et touffes."""
        return [point for point, _ in self.surface_points(center, half_extents, count, top_only)]

    def surface_points(self, center, half_extents, count: int, top_only: bool = False) -> list[tuple[np.ndarray, np.ndarray]]:
        """Points et normales sur les faces d'une boîte : une tache s'enfonce le long de la normale pour affleurer."""
        center = np.asarray(center, dtype=np.float64)
        half = np.asarray(half_extents, dtype=np.float64)
        result = []
        for _ in range(count):
            # Loin des arêtes : une tache posée sur un coin dépasserait de la face voisine.
            local = self._rng.uniform(-0.72, 0.72, 3) * half
            axis = 1 if top_only else int(self._rng.choice([0, 1, 1, 2]))
            sign = 1.0 if axis == 1 else float(self._rng.choice([-1.0, 1.0]))
            local[axis] = half[axis] * sign
            normal = np.zeros(3)
            normal[axis] = sign
            result.append((center + local, normal))
        return result
