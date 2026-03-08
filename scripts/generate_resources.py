#!/usr/bin/env python3
"""
VESTIGES - Resource sprite generator.

Produces grounded 32x32 isometric resource drops with:
- colored sel-out outlines
- top-left lighting
- soft grounded drop shadows
- silhouettes aligned with the world/lore direction
"""

from __future__ import annotations

import math
import os
import random
from typing import Iterable

from PIL import Image, ImageDraw

ROOT_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT_DIR = os.path.join(ROOT_DIR, "assets", "sprites", "resources")
SIZE = 32
BG = (0, 0, 0, 0)


def rgb(hex_value: str) -> tuple[int, int, int]:
    hex_value = hex_value.lstrip("#")
    return tuple(int(hex_value[i:i + 2], 16) for i in (0, 2, 4))


def rgba(hex_value: str, alpha: int = 255) -> tuple[int, int, int, int]:
    return (*rgb(hex_value), alpha)


PALETTE = {
    "shadow": rgb("#16213E"),
    "deep": rgb("#1A1A2E"),
    "wood_outline": rgb("#2E241D"),
    "wood_dark": rgb("#4A3728"),
    "wood_mid": rgb("#7A5C42"),
    "wood_light": rgb("#A68B6B"),
    "wood_rot": rgb("#5A4A38"),
    "moss_outline": rgb("#1E3A1A"),
    "moss_dark": rgb("#2D5A27"),
    "moss_mid": rgb("#4A8C3F"),
    "moss_light": rgb("#7BC558"),
    "stone_outline": rgb("#3A3535"),
    "stone_dark": rgb("#4A4A4A"),
    "stone_mid": rgb("#6B6161"),
    "stone_light": rgb("#9E9494"),
    "stone_edge": rgb("#E8E0D4"),
    "metal_outline": rgb("#394651"),
    "metal_dark": rgb("#5A6A7A"),
    "metal_mid": rgb("#8A9AAA"),
    "metal_edge": rgb("#E8E0D4"),
    "rust_outline": rgb("#5A2A18"),
    "rust_dark": rgb("#6B3A24"),
    "rust_mid": rgb("#A85C30"),
    "rust_hot": rgb("#C77B3F"),
    "fiber_outline": rgb("#233D1D"),
    "fiber_dark": rgb("#2D5A27"),
    "fiber_mid": rgb("#4A8C3F"),
    "fiber_light": rgb("#A4D65E"),
    "root_dark": rgb("#4A3728"),
    "root_mid": rgb("#7A5C42"),
    "essence_outline": rgb("#365600"),
    "essence_dark": rgb("#284D00"),
    "essence_mid": rgb("#56A900"),
    "essence_bright": rgb("#7FFF00"),
    "essence_core": rgb("#D8FF8A"),
    "charcoal": rgb("#2A2222"),
}


def blank() -> Image.Image:
    return Image.new("RGBA", (SIZE, SIZE), BG)


def in_bounds(x: int, y: int) -> bool:
    return 0 <= x < SIZE and 0 <= y < SIZE


def hashed_noise(x: int, y: int, seed: int) -> float:
    value = (x * 92837111) ^ (y * 689287499) ^ (seed * 283923481)
    value = (value ^ (value >> 13)) & 0xFFFFFFFF
    return (value % 1000) / 1000.0


def lerp_color(a: tuple[int, int, int], b: tuple[int, int, int], t: float) -> tuple[int, int, int, int]:
    t = max(0.0, min(1.0, t))
    return (
        int(a[0] + (b[0] - a[0]) * t),
        int(a[1] + (b[1] - a[1]) * t),
        int(a[2] + (b[2] - a[2]) * t),
        255,
    )


def draw_shadow(img: Image.Image, cx: int, cy: int, rx: int, ry: int, alpha: int = 70) -> None:
    draw = ImageDraw.Draw(img, "RGBA")
    shadow_color = (*PALETTE["shadow"], alpha)
    soft_color = (*PALETTE["shadow"], alpha // 2)
    feather_color = (*PALETTE["shadow"], alpha // 4)
    draw.ellipse((cx - rx - 2, cy - ry - 1, cx + rx + 2, cy + ry + 1), fill=feather_color)
    draw.ellipse((cx - rx - 1, cy - ry, cx + rx + 1, cy + ry), fill=soft_color)
    draw.ellipse((cx - rx, cy - ry, cx + rx, cy + ry), fill=shadow_color)


def outline_mask(img: Image.Image, mask: set[tuple[int, int]], outline: tuple[int, int, int]) -> None:
    for x, y in mask:
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if (nx, ny) not in mask and in_bounds(nx, ny) and img.getpixel((nx, ny))[3] == 0:
                img.putpixel((nx, ny), (*outline, 255))


def paint_blob(
    img: Image.Image,
    cx: float,
    cy: float,
    rx: float,
    ry: float,
    colors: list[tuple[int, int, int]],
    outline: tuple[int, int, int],
    seed: int,
    roughness: float = 0.12,
) -> set[tuple[int, int]]:
    mask: set[tuple[int, int]] = set()
    x0 = max(0, int(math.floor(cx - rx - 2)))
    x1 = min(SIZE - 1, int(math.ceil(cx + rx + 2)))
    y0 = max(0, int(math.floor(cy - ry - 2)))
    y1 = min(SIZE - 1, int(math.ceil(cy + ry + 2)))

    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            nx = (x - cx) / max(rx, 0.1)
            ny = (y - cy) / max(ry, 0.1)
            dist = nx * nx + ny * ny
            n = (hashed_noise(x, y, seed) - 0.5) * roughness
            if dist <= 1.0 + n:
                light = (-0.85 * nx) + (-0.95 * ny) + (0.38 * (1.0 - min(1.0, dist)))
                if light > 0.95:
                    color = colors[3]
                elif light > 0.45:
                    color = colors[2]
                elif light > -0.15:
                    color = colors[1]
                else:
                    color = colors[0]
                img.putpixel((x, y), (*color, 255))
                mask.add((x, y))

    outline_mask(img, mask, outline)
    return mask


def paint_capsule(
    img: Image.Image,
    p0: tuple[float, float],
    p1: tuple[float, float],
    radius: float,
    colors: list[tuple[int, int, int]],
    outline: tuple[int, int, int],
    seed: int,
    roughness: float = 0.1,
) -> set[tuple[int, int]]:
    mask: set[tuple[int, int]] = set()
    x0 = max(0, int(math.floor(min(p0[0], p1[0]) - radius - 2)))
    x1 = min(SIZE - 1, int(math.ceil(max(p0[0], p1[0]) + radius + 2)))
    y0 = max(0, int(math.floor(min(p0[1], p1[1]) - radius - 2)))
    y1 = min(SIZE - 1, int(math.ceil(max(p0[1], p1[1]) + radius + 2)))

    dx = p1[0] - p0[0]
    dy = p1[1] - p0[1]
    length_sq = max(0.001, dx * dx + dy * dy)

    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            t = ((x - p0[0]) * dx + (y - p0[1]) * dy) / length_sq
            t = max(0.0, min(1.0, t))
            px = p0[0] + dx * t
            py = p0[1] + dy * t
            lx = (x - px) / max(radius, 0.1)
            ly = (y - py) / max(radius, 0.1)
            dist = lx * lx + ly * ly
            n = (hashed_noise(x, y, seed) - 0.5) * roughness
            if dist <= 1.0 + n:
                along = 1.0 - abs(0.5 - t) * 1.3
                light = (-0.95 * lx) + (-0.7 * ly) + along * 0.14
                if light > 0.95:
                    color = colors[3]
                elif light > 0.35:
                    color = colors[2]
                elif light > -0.15:
                    color = colors[1]
                else:
                    color = colors[0]
                img.putpixel((x, y), (*color, 255))
                mask.add((x, y))

    outline_mask(img, mask, outline)
    return mask


def paint_circle(
    img: Image.Image,
    cx: float,
    cy: float,
    r: float,
    colors: list[tuple[int, int, int]],
    outline: tuple[int, int, int],
    seed: int,
    roughness: float = 0.1,
) -> set[tuple[int, int]]:
    return paint_blob(img, cx, cy, r, r, colors, outline, seed, roughness=roughness)


def draw_polygon(img: Image.Image, points: Iterable[tuple[int, int]], fill: tuple[int, int, int], outline: tuple[int, int, int]) -> None:
    draw = ImageDraw.Draw(img, "RGBA")
    draw.polygon(list(points), fill=(*fill, 255), outline=(*outline, 255))


def draw_line(img: Image.Image, points: Iterable[tuple[int, int]], color: tuple[int, int, int], width: int = 1) -> None:
    draw = ImageDraw.Draw(img, "RGBA")
    draw.line(list(points), fill=(*color, 255), width=width)


def draw_rivet(img: Image.Image, x: int, y: int, hot: bool = False) -> None:
    outer = PALETTE["metal_outline"]
    mid = PALETTE["rust_mid"] if hot else PALETTE["metal_mid"]
    hi = PALETTE["metal_edge"]
    for px, py, color in (
        (x, y, mid),
        (x - 1, y, outer),
        (x + 1, y, outer),
        (x, y - 1, outer),
        (x, y + 1, outer),
        (x - 1, y - 1, hi),
    ):
        if in_bounds(px, py):
            img.putpixel((px, py), (*color, 255))


def draw_moss_patch(img: Image.Image, coords: Iterable[tuple[int, int]]) -> None:
    pixels = list(coords)
    for i, (x, y) in enumerate(pixels):
        if in_bounds(x, y):
            if i % 4 == 0:
                color = PALETTE["moss_light"]
            elif i % 2 == 0:
                color = PALETTE["moss_mid"]
            else:
                color = PALETTE["moss_dark"]
            img.putpixel((x, y), (*color, 255))


def draw_crack(img: Image.Image, points: Iterable[tuple[int, int]], highlight_points: Iterable[tuple[int, int]] = ()) -> None:
    draw_line(img, points, PALETTE["stone_outline"], width=1)
    for x, y in highlight_points:
        if in_bounds(x, y):
            img.putpixel((x, y), (*PALETTE["stone_edge"], 255))


def draw_thorns(img: Image.Image, positions: Iterable[tuple[int, int]]) -> None:
    for i, (x, y) in enumerate(positions):
        tone = PALETTE["fiber_light"] if i % 3 == 0 else PALETTE["fiber_mid"]
        tri = [(x, y), (x + 1, y - 1), (x + 1, y + 1)]
        draw_polygon(img, tri, tone, PALETTE["fiber_outline"])


def draw_toxic_mist(img: Image.Image, cx: int, cy: int, radius: int, seed: int) -> None:
    for y in range(cy - radius, cy + radius + 1):
        for x in range(cx - radius - 2, cx + radius + 3):
            if not in_bounds(x, y):
                continue
            dx = (x - cx) / max(radius, 1)
            dy = (y - cy) / max(radius * 0.8, 1)
            dist = dx * dx + dy * dy
            noise = hashed_noise(x, y, seed)
            if dist < 1.1 and noise > 0.48 + dist * 0.18:
                base = lerp_color(PALETTE["essence_mid"], PALETTE["essence_bright"], 0.55 + noise * 0.25)
                img.putpixel((x, y), (base[0], base[1], base[2], 84))


def wood_palette() -> list[tuple[int, int, int]]:
    return [PALETTE["wood_outline"], PALETTE["wood_dark"], PALETTE["wood_mid"], PALETTE["wood_light"]]


def stone_palette() -> list[tuple[int, int, int]]:
    return [PALETTE["stone_outline"], PALETTE["stone_dark"], PALETTE["stone_mid"], PALETTE["stone_light"]]


def metal_palette() -> list[tuple[int, int, int]]:
    return [PALETTE["metal_outline"], PALETTE["metal_dark"], PALETTE["metal_mid"], PALETTE["metal_edge"]]


def rust_palette() -> list[tuple[int, int, int]]:
    return [PALETTE["rust_outline"], PALETTE["rust_dark"], PALETTE["rust_mid"], PALETTE["rust_hot"]]


def fiber_palette() -> list[tuple[int, int, int]]:
    return [PALETTE["fiber_outline"], PALETTE["fiber_dark"], PALETTE["fiber_mid"], PALETTE["fiber_light"]]


def essence_palette() -> list[tuple[int, int, int]]:
    return [PALETTE["essence_outline"], PALETTE["essence_dark"], PALETTE["essence_mid"], PALETTE["essence_bright"]]


def draw_log_end(img: Image.Image, cx: int, cy: int, rx: int = 2, ry: int = 3) -> None:
    draw = ImageDraw.Draw(img, "RGBA")
    draw.ellipse((cx - rx - 1, cy - ry - 1, cx + rx + 1, cy + ry + 1), fill=(*PALETTE["wood_outline"], 255))
    draw.ellipse((cx - rx, cy - ry, cx + rx, cy + ry), fill=(*PALETTE["wood_mid"], 255))
    draw.ellipse((cx - 1, cy - 2, cx + 1, cy + 2), fill=(*PALETTE["wood_light"], 255))
    draw.point((cx - 1, cy - 1), fill=(*PALETTE["wood_outline"], 255))
    draw.point((cx, cy + 1), fill=(*PALETTE["wood_outline"], 255))


def gen_bois_1() -> Image.Image:
    img = blank()
    draw_shadow(img, 15, 27, 9, 3, alpha=66)
    paint_capsule(img, (8, 23), (21, 18), 3.6, wood_palette(), PALETTE["wood_outline"], seed=11)
    paint_capsule(img, (10, 20), (22, 23), 3.0, wood_palette(), PALETTE["wood_outline"], seed=12)
    paint_capsule(img, (12, 17), (20, 14), 2.3, wood_palette(), PALETTE["wood_outline"], seed=13)
    draw_log_end(img, 22, 18, 2, 3)
    draw_log_end(img, 9, 23, 2, 3)
    draw_moss_patch(img, [(11, 19), (12, 18), (13, 18), (14, 17), (17, 18), (18, 17), (19, 17), (15, 16), (16, 16)])
    draw_moss_patch(img, [(13, 22), (14, 21), (15, 21), (16, 20), (17, 20), (18, 19)])
    return img


def gen_bois_2() -> Image.Image:
    img = blank()
    draw_shadow(img, 16, 27, 9, 3, alpha=66)
    paint_capsule(img, (8, 24), (18, 20), 3.4, wood_palette(), PALETTE["wood_outline"], seed=21)
    paint_capsule(img, (12, 19), (23, 22), 3.0, wood_palette(), PALETTE["wood_outline"], seed=22)
    paint_capsule(img, (15, 15), (20, 12), 1.8, [PALETTE["wood_outline"], PALETTE["wood_rot"], PALETTE["wood_mid"], PALETTE["wood_light"]], PALETTE["wood_outline"], seed=23)
    draw_log_end(img, 18, 20, 2, 3)
    draw_log_end(img, 23, 22, 2, 3)
    draw_line(img, [(10, 23), (13, 20), (17, 18), (20, 16)], PALETTE["root_mid"])
    draw_line(img, [(11, 24), (14, 21), (18, 18), (21, 17)], PALETTE["root_dark"])
    draw_moss_patch(img, [(10, 21), (11, 20), (12, 19), (13, 19), (14, 18), (16, 18), (17, 17), (18, 17), (17, 21), (18, 20), (19, 20)])
    for p in ((16, 14), (17, 13), (18, 13)):
        if in_bounds(*p):
            img.putpixel(p, (*PALETTE["stone_edge"], 255))
    return img


def gen_bois_3() -> Image.Image:
    img = blank()
    draw_shadow(img, 15, 27, 10, 3, alpha=68)
    paint_capsule(img, (7, 22), (20, 26), 3.1, [PALETTE["wood_outline"], PALETTE["wood_rot"], PALETTE["wood_dark"], PALETTE["wood_mid"]], PALETTE["wood_outline"], seed=31)
    paint_capsule(img, (10, 21), (23, 16), 2.7, wood_palette(), PALETTE["wood_outline"], seed=32)
    paint_capsule(img, (14, 17), (22, 14), 2.0, [PALETTE["wood_outline"], PALETTE["wood_rot"], PALETTE["wood_dark"], PALETTE["wood_light"]], PALETTE["wood_outline"], seed=33)
    draw_log_end(img, 22, 16, 2, 3)
    draw_line(img, [(8, 23), (9, 21), (11, 19), (13, 17)], PALETTE["root_dark"], width=1)
    draw_line(img, [(15, 26), (16, 23), (18, 20), (21, 18)], PALETTE["root_mid"], width=1)
    draw_moss_patch(img, [(11, 22), (12, 21), (13, 21), (14, 20), (16, 19), (17, 19), (18, 18), (19, 18)])
    draw_moss_patch(img, [(15, 16), (16, 15), (17, 15), (18, 14), (19, 14)])
    return img


def gen_pierre_1() -> Image.Image:
    img = blank()
    draw_shadow(img, 15, 27, 8, 3, alpha=58)
    draw_polygon(img, [(8, 22), (12, 17), (15, 21), (13, 25)], PALETTE["stone_mid"], PALETTE["stone_outline"])
    draw_polygon(img, [(13, 23), (17, 16), (21, 19), (18, 24)], PALETTE["stone_light"], PALETTE["stone_outline"])
    draw_polygon(img, [(17, 24), (21, 19), (24, 22), (21, 26)], PALETTE["stone_mid"], PALETTE["stone_outline"])
    draw_polygon(img, [(10, 25), (12, 21), (16, 24), (14, 27)], PALETTE["stone_dark"], PALETTE["stone_outline"])
    draw_crack(img, [(14, 18), (15, 20), (17, 21), (19, 23)], highlight_points=[(14, 17), (18, 22)])
    draw_moss_patch(img, [(11, 24), (12, 23), (13, 23), (18, 24), (19, 23)])
    return img


def gen_pierre_2() -> Image.Image:
    img = blank()
    draw_shadow(img, 16, 27, 8, 3, alpha=60)
    paint_blob(img, 15.5, 21.5, 6.2, 4.8, stone_palette(), PALETTE["stone_outline"], seed=41, roughness=0.16)
    paint_blob(img, 20.7, 24.0, 2.2, 1.7, [PALETTE["stone_outline"], PALETTE["stone_dark"], PALETTE["stone_mid"], PALETTE["stone_light"]], PALETTE["stone_outline"], seed=42, roughness=0.18)
    draw_crack(img, [(10, 22), (13, 20), (16, 19), (19, 18)], highlight_points=[(9, 22), (17, 18)])
    draw_crack(img, [(16, 22), (18, 23), (20, 24)], highlight_points=[(16, 21)])
    return img


def gen_pierre_3() -> Image.Image:
    img = blank()
    draw_shadow(img, 15, 27, 9, 3, alpha=58)
    draw_polygon(img, [(7, 23), (13, 20), (19, 22), (14, 26)], PALETTE["stone_dark"], PALETTE["stone_outline"])
    draw_polygon(img, [(11, 20), (17, 16), (22, 18), (18, 22)], PALETTE["stone_mid"], PALETTE["stone_outline"])
    draw_polygon(img, [(16, 17), (20, 14), (24, 17), (21, 20)], PALETTE["stone_light"], PALETTE["stone_outline"])
    draw_polygon(img, [(8, 25), (11, 22), (13, 24), (11, 27)], PALETTE["stone_mid"], PALETTE["stone_outline"])
    draw_crack(img, [(12, 21), (14, 21), (16, 20), (18, 19)], highlight_points=[(12, 20), (19, 18)])
    draw_moss_patch(img, [(9, 24), (10, 24), (11, 23), (17, 21), (18, 20)])
    return img


def gen_metal_1() -> Image.Image:
    img = blank()
    draw_shadow(img, 15, 27, 9, 3, alpha=64)
    draw_polygon(img, [(8, 24), (14, 17), (19, 19), (13, 26)], PALETTE["metal_dark"], PALETTE["metal_outline"])
    draw_polygon(img, [(12, 24), (18, 15), (24, 17), (18, 25)], PALETTE["metal_mid"], PALETTE["metal_outline"])
    draw_polygon(img, [(9, 20), (14, 18), (18, 21), (12, 23)], PALETTE["rust_dark"], PALETTE["rust_outline"])
    draw_line(img, [(14, 17), (18, 15), (23, 17)], PALETTE["metal_edge"])
    draw_line(img, [(12, 24), (18, 25)], PALETTE["metal_edge"])
    draw_rivet(img, 15, 21)
    draw_rivet(img, 18, 20, hot=True)
    draw_rivet(img, 12, 23)
    return img


def gen_metal_2() -> Image.Image:
    img = blank()
    draw_shadow(img, 16, 27, 9, 3, alpha=64)
    paint_capsule(img, (10, 24), (20, 16), 2.6, metal_palette(), PALETTE["metal_outline"], seed=51, roughness=0.08)
    draw_polygon(img, [(15, 24), (20, 20), (25, 22), (20, 26)], PALETTE["rust_dark"], PALETTE["rust_outline"])
    draw_polygon(img, [(12, 19), (15, 16), (18, 17), (15, 20)], PALETTE["metal_mid"], PALETTE["metal_outline"])
    draw_line(img, [(12, 24), (20, 17)], PALETTE["metal_edge"])
    draw_line(img, [(11, 23), (18, 18)], PALETTE["rust_mid"])
    draw_rivet(img, 18, 21)
    draw_rivet(img, 21, 23, hot=True)
    draw_rivet(img, 14, 18)
    return img


def gen_metal_3() -> Image.Image:
    img = blank()
    draw_shadow(img, 16, 27, 9, 3, alpha=64)
    draw_polygon(img, [(8, 24), (13, 18), (20, 18), (24, 23), (19, 27), (11, 27)], PALETTE["rust_dark"], PALETTE["rust_outline"])
    draw_polygon(img, [(12, 24), (16, 20), (21, 20), (18, 24)], PALETTE["metal_dark"], PALETTE["metal_outline"])
    draw_polygon(img, [(16, 19), (21, 20), (22, 17), (18, 15)], PALETTE["metal_mid"], PALETTE["metal_outline"])
    draw_line(img, [(13, 18), (18, 15)], PALETTE["metal_edge"])
    draw_line(img, [(10, 25), (18, 26)], PALETTE["rust_mid"])
    draw_rivet(img, 13, 22, hot=True)
    draw_rivet(img, 16, 22)
    draw_rivet(img, 19, 22)
    draw_rivet(img, 17, 17)
    return img


def gen_fibre_1() -> Image.Image:
    img = blank()
    draw_shadow(img, 15, 27, 9, 3, alpha=56)
    paint_capsule(img, (9, 24), (20, 20), 2.6, fiber_palette(), PALETTE["fiber_outline"], seed=61)
    paint_capsule(img, (11, 22), (22, 24), 2.2, fiber_palette(), PALETTE["fiber_outline"], seed=62)
    paint_capsule(img, (12, 18), (18, 15), 1.8, [PALETTE["fiber_outline"], PALETTE["root_dark"], PALETTE["root_mid"], PALETTE["wood_light"]], PALETTE["fiber_outline"], seed=63)
    draw_line(img, [(8, 25), (11, 22), (14, 19), (16, 17)], PALETTE["root_dark"])
    draw_line(img, [(13, 25), (16, 22), (19, 20), (22, 18)], PALETTE["fiber_light"])
    draw_thorns(img, [(12, 20), (15, 18), (18, 18), (19, 22), (14, 24)])
    return img


def gen_fibre_2() -> Image.Image:
    img = blank()
    draw_shadow(img, 15, 27, 8, 3, alpha=56)
    paint_capsule(img, (10, 24), (19, 19), 2.5, [PALETTE["fiber_outline"], PALETTE["root_dark"], PALETTE["root_mid"], PALETTE["wood_light"]], PALETTE["fiber_outline"], seed=71)
    paint_capsule(img, (12, 21), (22, 23), 2.0, fiber_palette(), PALETTE["fiber_outline"], seed=72)
    draw_line(img, [(13, 24), (13, 18)], PALETTE["fiber_mid"])
    draw_line(img, [(16, 25), (17, 18)], PALETTE["fiber_light"])
    draw_line(img, [(19, 24), (21, 19)], PALETTE["fiber_mid"])
    draw_thorns(img, [(13, 19), (16, 19), (21, 20), (18, 23)])
    return img


def gen_fibre_3() -> Image.Image:
    img = blank()
    draw_shadow(img, 16, 27, 9, 3, alpha=58)
    paint_capsule(img, (8, 24), (18, 19), 2.8, fiber_palette(), PALETTE["fiber_outline"], seed=81)
    paint_capsule(img, (11, 20), (22, 22), 2.3, fiber_palette(), PALETTE["fiber_outline"], seed=82)
    paint_capsule(img, (15, 18), (21, 14), 1.7, fiber_palette(), PALETTE["fiber_outline"], seed=83)
    draw_line(img, [(10, 25), (12, 23), (15, 21), (18, 20)], PALETTE["root_dark"])
    draw_line(img, [(18, 25), (19, 22), (20, 19), (21, 16)], PALETTE["fiber_light"])
    draw_thorns(img, [(12, 22), (15, 20), (18, 18), (20, 16), (20, 22)])
    return img


def draw_crystal_faces(
    img: Image.Image,
    left: list[tuple[int, int]],
    right: list[tuple[int, int]],
    core: list[tuple[int, int]] | None = None,
) -> None:
    draw_polygon(img, left, PALETTE["essence_mid"], PALETTE["essence_outline"])
    draw_polygon(img, right, PALETTE["essence_bright"], PALETTE["essence_outline"])
    draw_line(img, [left[0], right[0]], PALETTE["essence_core"])
    if core:
        draw_polygon(img, core, PALETTE["essence_core"], PALETTE["essence_outline"])


def gen_essence_1() -> Image.Image:
    img = blank()
    draw_shadow(img, 15, 27, 8, 3, alpha=68)
    paint_blob(img, 15.5, 25.0, 5.0, 2.2, [PALETTE["deep"], PALETTE["charcoal"], PALETTE["stone_dark"], PALETTE["stone_mid"]], PALETTE["stone_outline"], seed=91, roughness=0.18)
    draw_crystal_faces(
        img,
        left=[(12, 23), (14, 14), (16, 23)],
        right=[(14, 14), (18, 18), (16, 23)],
        core=[(14, 17), (15, 15), (16, 18), (15, 22)],
    )
    draw_crystal_faces(
        img,
        left=[(9, 24), (11, 18), (13, 24)],
        right=[(11, 18), (14, 20), (13, 24)],
    )
    draw_crystal_faces(
        img,
        left=[(17, 24), (19, 19), (20, 24)],
        right=[(19, 19), (22, 21), (20, 24)],
    )
    draw_toxic_mist(img, 15, 24, 6, seed=92)
    for p in ((14, 13), (15, 12), (19, 18), (11, 17), (22, 20)):
        if in_bounds(*p):
            img.putpixel(p, (*PALETTE["essence_core"], 255))
    return img


def gen_essence_2() -> Image.Image:
    img = blank()
    draw_shadow(img, 16, 27, 8, 3, alpha=70)
    paint_blob(img, 16.0, 25.0, 5.0, 2.2, [PALETTE["deep"], PALETTE["charcoal"], PALETTE["stone_dark"], PALETTE["stone_mid"]], PALETTE["stone_outline"], seed=101, roughness=0.18)
    draw_crystal_faces(
        img,
        left=[(13, 24), (15, 11), (18, 24)],
        right=[(15, 11), (20, 17), (18, 24)],
        core=[(15, 15), (17, 13), (18, 18), (17, 23), (15, 22)],
    )
    draw_crystal_faces(
        img,
        left=[(10, 23), (12, 17), (14, 23)],
        right=[(12, 17), (15, 19), (14, 23)],
    )
    draw_crystal_faces(
        img,
        left=[(19, 24), (21, 18), (23, 24)],
        right=[(21, 18), (24, 21), (23, 24)],
    )
    draw_toxic_mist(img, 16, 24, 7, seed=102)
    for p in ((16, 10), (17, 11), (17, 16), (18, 14), (12, 16), (22, 17), (20, 22)):
        if in_bounds(*p):
            img.putpixel(p, (*PALETTE["essence_core"], 255))
    return img


def make_preview_sheet(sprites: list[tuple[str, Image.Image]]) -> None:
    scale = 4
    cols = 5
    rows = 3
    cell = 42
    padding = 10
    sheet = Image.new("RGBA", (padding * 2 + cols * cell, padding * 2 + rows * cell), rgba("#25272B"))
    draw = ImageDraw.Draw(sheet, "RGBA")

    for idx, (_name, sprite) in enumerate(sprites):
        col = idx % cols
        row = idx // cols
        x0 = padding + col * cell
        y0 = padding + row * cell
        draw.rectangle((x0, y0, x0 + cell - 4, y0 + cell - 4), outline=rgba("#6B6161", 160))
        scaled = sprite.resize((SIZE * scale, SIZE * scale), Image.NEAREST)
        ox = x0 + ((cell - 4) - scaled.width) // 2
        oy = y0 + ((cell - 4) - scaled.height) // 2
        sheet.paste(scaled, (ox, oy), scaled)

    sheet.save(os.path.join(OUT_DIR, "PREVIEW_RESOURCES.png"))


def make_showcase(sprites: dict[str, Image.Image]) -> None:
    canvas = Image.new("RGBA", (800, 400), rgba("#242529"))
    draw = ImageDraw.Draw(canvas, "RGBA")
    draw.rectangle((40, 72, 760, 328), outline=rgba("#6B6161", 180))
    draw.line((120, 72, 120, 328), fill=rgba("#3A3535", 120))
    draw.line((680, 72, 680, 328), fill=rgba("#3A3535", 120))

    featured = [
        ("resource_bois_1.png", (110, 185), 7),
        ("resource_pierre_2.png", (260, 200), 8),
        ("resource_metal_1.png", (410, 200), 8),
        ("resource_fibre_3.png", (560, 198), 8),
        ("resource_essence_2.png", (690, 188), 8),
    ]
    for name, center, scale in featured:
        sprite = sprites[name]
        scaled = sprite.resize((SIZE * scale, SIZE * scale), Image.NEAREST)
        canvas.paste(scaled, (center[0] - scaled.width // 2, center[1] - scaled.height // 2), scaled)

    canvas.save(os.path.join(OUT_DIR, "SHOWCASE_DETAIL.png"))


def make_zoomed_details(sprites: dict[str, Image.Image]) -> None:
    canvas = Image.new("RGBA", (768, 256), rgba("#242529"))
    featured = [
        "resource_bois_2.png",
        "resource_pierre_1.png",
        "resource_metal_2.png",
        "resource_fibre_1.png",
        "resource_essence_1.png",
    ]
    for idx, name in enumerate(featured):
        sprite = sprites[name]
        scaled = sprite.resize((SIZE * 6, SIZE * 6), Image.NEAREST)
        x = 24 + idx * 148
        y = 32
        canvas.paste(scaled, (x, y), scaled)
    canvas.save(os.path.join(OUT_DIR, "ZOOMED_DETAILS.png"))


def main() -> None:
    os.makedirs(OUT_DIR, exist_ok=True)

    generators = [
        ("resource_bois_1.png", gen_bois_1),
        ("resource_bois_2.png", gen_bois_2),
        ("resource_bois_3.png", gen_bois_3),
        ("resource_pierre_1.png", gen_pierre_1),
        ("resource_pierre_2.png", gen_pierre_2),
        ("resource_pierre_3.png", gen_pierre_3),
        ("resource_metal_1.png", gen_metal_1),
        ("resource_metal_2.png", gen_metal_2),
        ("resource_metal_3.png", gen_metal_3),
        ("resource_fibre_1.png", gen_fibre_1),
        ("resource_fibre_2.png", gen_fibre_2),
        ("resource_fibre_3.png", gen_fibre_3),
        ("resource_essence_1.png", gen_essence_1),
        ("resource_essence_2.png", gen_essence_2),
    ]

    generated: list[tuple[str, Image.Image]] = []
    for filename, generator in generators:
        image = generator()
        path = os.path.join(OUT_DIR, filename)
        image.save(path)
        generated.append((filename, image))
        print(f"generated {filename}")

    sprite_map = {name: image for name, image in generated}
    make_preview_sheet(generated)
    make_showcase(sprite_map)
    make_zoomed_details(sprite_map)
    print(f"generated {len(generated)} resource sprites in {OUT_DIR}")


if __name__ == "__main__":
    main()
