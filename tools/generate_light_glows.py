#!/usr/bin/env python3
"""
Lueurs des fausses lumières (plan 39 B4) : sprites blancs à alpha en paliers, teintés et dosés en jeu
(`FalseLightDirector`, mélange additif). Paliers tramés au pixel plutôt qu'un dégradé lisse, comme les lueurs des
bonus au sol (`generate_pickups.py`).

- light_core.png : point lumineux sur l'ampoule, cœur plein et deux anneaux ;
- light_halo.png : halo d'air autour de l'ampoule ;
- light_pool.png : flaque de lumière au sol, ellipse écrasée par la vue à 30°.

    python3 tools/generate_light_glows.py
"""
from __future__ import annotations

from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
OUTPUT = ROOT / "assets/vfx/lights"
BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0


def glow(width: int, height: int, levels: list[tuple[float, int]], dither: float) -> Image.Image:
    """
    Ellipse inscrite dans le cadre ; `levels` : (rayon relatif, alpha) du plus large au plus petit. Le bord de chaque
    palier est tramé sur `dither` (part du rayon) pour éviter les marches lisses d'un dégradé.
    """
    y, x = np.mgrid[0:height, 0:width]
    dx = (x + 0.5 - width / 2) / (width / 2)
    dy = (y + 0.5 - height / 2) / (height / 2)
    radius = np.sqrt(dx ** 2 + dy ** 2)
    threshold = BAYER[y % 4, x % 4]
    alpha = np.zeros((height, width))
    for edge, value in levels:
        inside = radius < edge - dither * threshold
        alpha = np.where(inside, value, alpha)
    image = np.zeros((height, width, 4), dtype=np.uint8)
    image[..., :3] = 255
    image[..., 3] = alpha.astype(np.uint8)
    return Image.fromarray(image, "RGBA")


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    glow(7, 7, [(1.0, 70), (0.72, 150), (0.36, 255)], 0.0).save(OUTPUT / "light_core.png")
    glow(26, 26, [(1.0, 22), (0.7, 44), (0.42, 72)], 0.18).save(OUTPUT / "light_halo.png")
    glow(60, 26, [(1.0, 18), (0.7, 36), (0.4, 56)], 0.16).save(OUTPUT / "light_pool.png")
    print(f"[generate_light_glows] 3 lueurs → {OUTPUT.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
