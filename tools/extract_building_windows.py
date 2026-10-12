#!/usr/bin/env python3
"""
Fenêtres vitrées des immeubles (plan 39 B4b) : repérées dans les sprites rendus, à la couleur exacte des tons des
matériaux « pane » (vitre) et « glint » (son reflet) de `tools/sprites/props/buildings.py`, puis groupées en
rectangles ; les morceaux d'une même vitre (coupée par un meneau) se fusionnent. `FalseLightDirector` en allume une de
loin en loin.

Seules les vitres assez grandes et pleines comptent : celles des façades de côté, vues de biais sur deux ou trois
pixels de large, et les morceaux qu'un lierre ou une ruine laissent dépasser ne se liraient pas comme une fenêtre
allumée, et l'aplat d'une vitre trop creuse déborderait sur la maçonnerie.

À relancer après `python3 tools/generate_props.py urban_buildings` :

    python3 tools/extract_building_windows.py
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from tools.sprites.palette import make_material  # noqa: E402

FOLDER = ROOT / "assets/props/urban_ruins"
OUTPUT = FOLDER / "building_windows.json"
# Doivent rester les matériaux des vitres de buildings.py.
GLASS = (make_material("pane", "#34424E", contrast=0.5), make_material("glint", "#9CB0BA", contrast=0.4))
MIN_SIZE = 8
MIN_PIXELS = 40
# Part de la vitre dans son rectangle : en dessous, l'aplat déborderait sur le cadre ou la maçonnerie.
MIN_FILL = 0.8


def components(mask: np.ndarray) -> list[tuple[int, int, int, int, int]]:
    """Composantes 4-connexes : (x, y, largeur, hauteur, pixels)."""
    height, width = mask.shape
    seen = np.zeros_like(mask, dtype=bool)
    found = []
    for y in range(height):
        for x in range(width):
            if not mask[y, x] or seen[y, x]:
                continue
            stack, xs, ys = [(y, x)], [], []
            seen[y, x] = True
            while stack:
                cy, cx = stack.pop()
                ys.append(cy)
                xs.append(cx)
                for ny, nx in ((cy + 1, cx), (cy - 1, cx), (cy, cx + 1), (cy, cx - 1)):
                    if 0 <= ny < height and 0 <= nx < width and mask[ny, nx] and not seen[ny, nx]:
                        seen[ny, nx] = True
                        stack.append((ny, nx))
            found.append((min(xs), min(ys), max(xs) - min(xs) + 1, max(ys) - min(ys) + 1, len(xs)))
    return found


def merge(found: list[tuple[int, int, int, int, int]]) -> list[tuple[int, int, int, int, int]]:
    """Fusionne les rectangles qui se recouvrent ou se touchent : les morceaux d'une même vitre."""
    boxes = [list(box) for box in found]
    merged = True
    while merged:
        merged = False
        for i in range(len(boxes)):
            for j in range(i + 1, len(boxes)):
                ax, ay, aw, ah, an = boxes[i]
                bx, by, bw, bh, bn = boxes[j]
                if ax <= bx + bw and bx <= ax + aw and ay <= by + bh and by <= ay + ah:
                    x0, y0 = min(ax, bx), min(ay, by)
                    x1, y1 = max(ax + aw, bx + bw), max(ay + ah, by + bh)
                    boxes[i] = [x0, y0, x1 - x0, y1 - y0, an + bn]
                    del boxes[j]
                    merged = True
                    break
            if merged:
                break
    return [tuple(box) for box in boxes]


def main() -> None:
    tones = np.array([tone for material in GLASS for tone in material.ramp])
    windows = {}
    for path in sorted(FOLDER.glob("prop_bld_*.png")):
        pixels = np.asarray(Image.open(path).convert("RGBA"))
        mask = (pixels[..., 3] > 0) & np.any(np.all(pixels[..., None, :3] == tones[None, None], axis=-1), axis=-1)
        rects = [[x, y, w, h] for x, y, w, h, n in merge(components(mask))
                 if w >= MIN_SIZE and h >= MIN_SIZE and n >= MIN_PIXELS and n >= MIN_FILL * w * h]
        windows[path.stem] = sorted(rects, key=lambda r: (r[1], r[0]))
    OUTPUT.write_text(json.dumps(windows, indent=1) + "\n")
    total = sum(len(rects) for rects in windows.values())
    print(f"[extract_building_windows] {total} fenêtres dans {len(windows)} immeubles → {OUTPUT.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
