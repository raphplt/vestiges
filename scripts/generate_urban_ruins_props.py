#!/usr/bin/env python3
"""Generate redesigned isometric props for the Urban Ruins biome."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


PAL = {
    "dark_concrete": (0x4A, 0x4A, 0x4A),
    "med_concrete": (0x73, 0x73, 0x73),
    "light_concrete": (0xA0, 0xA0, 0xA0),
    "dark_rust": (0x6B, 0x3A, 0x24),
    "orange_rust": (0xA8, 0x5C, 0x30),
    "oxidized_copper": (0x5A, 0x9A, 0x8A),
    "faded_brick": (0x8A, 0x5A, 0x42),
    "peeling_blue": (0x5A, 0x7A, 0x9A),
    "signage_yellow": (0xC4, 0xA8, 0x30),
    "reclaim_green": (0x4A, 0x7A, 0x3A),
    "rotting_wood": (0x5A, 0x4A, 0x38),
    "broken_glass": (0x8A, 0xB8, 0xC4),
    "deep_black": (0x1A, 0x1A, 0x2E),
    "blue_black": (0x16, 0x21, 0x3E),
    "dark_warm_gray": (0x3A, 0x35, 0x35),
    "warm_gray": (0x6B, 0x61, 0x61),
    "light_gray": (0x9E, 0x94, 0x94),
    "off_white": (0xE8, 0xE0, 0xD4),
    "erasure_white": (0xF5, 0xF0, 0xEB),
}

OUTPUT_DIR = Path(__file__).resolve().parent.parent / "assets" / "props" / "urban_ruins"
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
CONTACT_SHEET = OUTPUT_DIR / "_urban_ruins_contact_sheet.png"

OUTLINE_MAP = {
    PAL["dark_concrete"]: PAL["dark_warm_gray"],
    PAL["med_concrete"]: PAL["dark_concrete"],
    PAL["light_concrete"]: PAL["dark_concrete"],
    PAL["dark_rust"]: PAL["dark_warm_gray"],
    PAL["orange_rust"]: PAL["dark_rust"],
    PAL["oxidized_copper"]: PAL["dark_concrete"],
    PAL["faded_brick"]: PAL["dark_rust"],
    PAL["peeling_blue"]: PAL["dark_concrete"],
    PAL["signage_yellow"]: PAL["orange_rust"],
    PAL["reclaim_green"]: PAL["dark_concrete"],
    PAL["rotting_wood"]: PAL["dark_warm_gray"],
    PAL["broken_glass"]: PAL["warm_gray"],
    PAL["warm_gray"]: PAL["dark_warm_gray"],
    PAL["light_gray"]: PAL["warm_gray"],
    PAL["off_white"]: PAL["light_gray"],
    PAL["erasure_white"]: PAL["light_gray"],
}

DEFAULT_TARGETS = [
    "prop_chain_link_fence",
    "prop_collapsed_stairs",
    "prop_supermarket_shelves",
    "prop_phone_booth",
    "prop_torn_billboard",
    "prop_steel_beam",
    "prop_steel_beam_diagonal",
    "prop_traffic_light",
    "prop_overturned_desk",
    "prop_mailbox",
    "prop_dumpster",
    "prop_dumpster_v2",
    "prop_urban_car",
    "prop_concrete_debris",
    "prop_concrete_debris_v2",
    "prop_concrete_debris_v3",
]


def new_image(width: int, height: int) -> Image.Image:
    return Image.new("RGBA", (width, height), (0, 0, 0, 0))


def put(px, x: int, y: int, color: tuple[int, int, int], width: int, height: int) -> None:
    if 0 <= x < width and 0 <= y < height:
        px[x, y] = color + (255,)


def erase(px, x: int, y: int, width: int, height: int) -> None:
    if 0 <= x < width and 0 <= y < height:
        px[x, y] = (0, 0, 0, 0)


def fill_rect(px, x0: int, y0: int, x1: int, y1: int, color, width: int, height: int) -> None:
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            put(px, x, y, color, width, height)


def line(px, x0: int, y0: int, x1: int, y1: int, color, width: int, height: int) -> None:
    dx = abs(x1 - x0)
    sx = 1 if x0 < x1 else -1
    dy = -abs(y1 - y0)
    sy = 1 if y0 < y1 else -1
    err = dx + dy
    while True:
        put(px, x0, y0, color, width, height)
        if x0 == x1 and y0 == y1:
            return
        e2 = err * 2
        if e2 >= dy:
            err += dy
            x0 += sx
        if e2 <= dx:
            err += dx
            y0 += sy


def sprinkle(px, points, color, width: int, height: int) -> None:
    for x, y in points:
        put(px, x, y, color, width, height)


def add_rubble(px, base_y: int, chunks, width: int, height: int) -> None:
    palette = [PAL["dark_concrete"], PAL["med_concrete"], PAL["light_concrete"]]
    for index, (x, y, size) in enumerate(chunks):
        for dy in range(size):
            for dx in range(size + (dy % 2)):
                put(px, x + dx, y + dy, palette[(index + dx + dy) % len(palette)], width, height)
    for x, y in [(chunk[0] - 1, min(height - 1, chunk[1] + chunk[2])) for chunk in chunks]:
        put(px, x, min(base_y, y), PAL["light_gray"], width, height)


def add_weeds(px, roots, width: int, height: int) -> None:
    for x, y in roots:
        put(px, x, y, PAL["reclaim_green"], width, height)
        put(px, x - 1, y - 1, PAL["reclaim_green"], width, height)
        put(px, x + 1, y - 1, PAL["reclaim_green"], width, height)


def outline_for(rgb: tuple[int, int, int]) -> tuple[int, int, int]:
    if rgb in OUTLINE_MAP:
        return OUTLINE_MAP[rgb]
    nearest = min(OUTLINE_MAP.keys(), key=lambda value: sum(abs(value[i] - rgb[i]) for i in range(3)))
    return OUTLINE_MAP[nearest]


def sel_out(img: Image.Image) -> Image.Image:
    src = img.copy()
    px_src = src.load()
    px_dst = img.load()
    width, height = img.size
    for y in range(height):
        for x in range(width):
            if px_src[x, y][3] != 0:
                continue
            neighbors = []
            for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
                nx = x + dx
                ny = y + dy
                if 0 <= nx < width and 0 <= ny < height and px_src[nx, ny][3] != 0:
                    neighbors.append(px_src[nx, ny][:3])
            if 0 < len(neighbors) <= 2:
                px_dst[x, y] = outline_for(neighbors[0]) + (255,)
    return img


def save_prop(name: str, img: Image.Image) -> None:
    sel_out(img)
    path = OUTPUT_DIR / f"{name}.png"
    img.save(path)
    print(f"  + {name}.png ({img.width}x{img.height})")


def build_contact_sheet(targets: list[str]) -> None:
    assets = [OUTPUT_DIR / f"{target}.png" for target in targets if (OUTPUT_DIR / f"{target}.png").is_file()]
    if not assets:
        return

    scale = 6
    cols = 4
    cell_w = max(Image.open(path).width for path in assets) * scale + 10
    cell_h = max(Image.open(path).height for path in assets) * scale + 10
    rows = (len(assets) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * cell_w, rows * cell_h), PAL["blue_black"] + (255,))

    for index, path in enumerate(assets):
        with Image.open(path) as source:
            preview = source.resize((source.width * scale, source.height * scale), Image.Resampling.NEAREST)
        col = index % cols
        row = index // cols
        ox = col * cell_w + 5 + (cell_w - 10 - preview.width) // 2
        oy = row * cell_h + 5 + (cell_h - 10 - preview.height) // 2
        sheet.alpha_composite(preview, (ox, oy))

    sheet.save(CONTACT_SHEET)
    print(f"  + {CONTACT_SHEET.name} ({sheet.width}x{sheet.height})")


def gen_chain_link_fence() -> Image.Image:
    width, height = 24, 16
    img = new_image(width, height)
    px = img.load()
    line(px, 4, 3, 5, 14, PAL["warm_gray"], width, height)
    line(px, 17, 2, 20, 13, PAL["warm_gray"], width, height)
    line(px, 4, 3, 17, 2, PAL["light_gray"], width, height)
    line(px, 5, 14, 20, 13, PAL["dark_concrete"], width, height)
    for x in range(6, 18):
        top = 3 + (1 if x > 13 else 0)
        bottom = 13 - (1 if x < 10 else 0)
        for y in range(top, bottom + 1):
            if (x + y) % 4 == 0 or (x - y) % 4 == 0:
                if 12 <= x <= 15 and 7 <= y <= 10:
                    continue
                put(px, x, y, PAL["light_gray"], width, height)
    line(px, 12, 7, 16, 11, PAL["warm_gray"], width, height)
    line(px, 13, 6, 18, 12, PAL["warm_gray"], width, height)
    sprinkle(px, [(5, 8), (5, 11), (18, 9)], PAL["orange_rust"], width, height)
    add_weeds(px, [(4, 14), (10, 14), (19, 13)], width, height)
    return img


def gen_collapsed_stairs() -> Image.Image:
    width, height = 24, 24
    img = new_image(width, height)
    px = img.load()
    line(px, 3, 11, 3, 21, PAL["dark_concrete"], width, height)
    line(px, 4, 10, 4, 21, PAL["med_concrete"], width, height)
    for step in range(4):
        sx = 4 + step * 3
        sy = 20 - step * 3
        fill_rect(px, sx, sy - 1, sx + 5, sy, PAL["light_concrete"], width, height)
        fill_rect(px, sx, sy + 1, sx + 5, sy + 1, PAL["dark_concrete"], width, height)
        put(px, sx + 1, sy - 1, PAL["off_white"], width, height)
        put(px, sx + 4, sy, PAL["med_concrete"], width, height)
    fill_rect(px, 16, 8, 19, 9, PAL["light_concrete"], width, height)
    erase(px, 18, 8, width, height)
    erase(px, 19, 9, width, height)
    line(px, 17, 9, 20, 6, PAL["dark_rust"], width, height)
    line(px, 18, 10, 21, 7, PAL["orange_rust"], width, height)
    line(px, 6, 19, 10, 15, PAL["warm_gray"], width, height)
    sprinkle(px, [(8, 17), (9, 17)], PAL["peeling_blue"], width, height)
    put(px, 9, 16, PAL["off_white"], width, height)
    add_rubble(px, 22, [(5, 21, 2), (9, 22, 2), (13, 21, 2), (17, 22, 2)], width, height)
    add_weeds(px, [(6, 22), (14, 22)], width, height)
    return img


def gen_supermarket_shelves() -> Image.Image:
    width, height = 24, 20
    img = new_image(width, height)
    px = img.load()
    line(px, 4, 4, 4, 18, PAL["warm_gray"], width, height)
    line(px, 9, 2, 9, 16, PAL["light_gray"], width, height)
    line(px, 18, 1, 18, 13, PAL["warm_gray"], width, height)
    for front_y, back_y in [(6, 4), (10, 8), (14, 12)]:
        line(px, 4, front_y, 18, back_y, PAL["light_gray"], width, height)
        line(px, 4, front_y + 1, 18, back_y + 1, PAL["warm_gray"], width, height)
    for y in range(5, 16):
        for x in range(5, 18):
            if 4 < x < 9 and 6 < y < 16 and (x + y) % 5 == 0:
                put(px, x, y, PAL["dark_warm_gray"], width, height)
            elif 9 < x < 17 and 4 < y < 14 and (x + y) % 4 == 0:
                put(px, x, y, PAL["dark_warm_gray"], width, height)
    line(px, 17, 5, 18, 13, PAL["dark_concrete"], width, height)
    line(px, 6, 10, 9, 8, PAL["signage_yellow"], width, height)
    sprinkle(px, [(5, 17), (8, 16), (13, 15), (16, 14)], PAL["light_gray"], width, height)
    put(px, 6, 11, PAL["off_white"], width, height)
    return img


def gen_phone_booth() -> Image.Image:
    width, height = 10, 20
    img = new_image(width, height)
    px = img.load()
    line(px, 2, 5, 2, 18, PAL["warm_gray"], width, height)
    line(px, 5, 4, 5, 18, PAL["light_gray"], width, height)
    line(px, 8, 3, 8, 15, PAL["warm_gray"], width, height)
    line(px, 2, 5, 8, 3, PAL["signage_yellow"], width, height)
    line(px, 2, 6, 8, 4, PAL["dark_concrete"], width, height)
    fill_rect(px, 3, 7, 4, 10, PAL["broken_glass"], width, height)
    fill_rect(px, 6, 6, 7, 9, PAL["broken_glass"], width, height)
    sprinkle(px, [(3, 11), (4, 12), (6, 10), (7, 11)], PAL["broken_glass"], width, height)
    put(px, 5, 10, PAL["dark_warm_gray"], width, height)
    line(px, 5, 11, 4, 14, PAL["dark_warm_gray"], width, height)
    put(px, 4, 15, PAL["warm_gray"], width, height)
    line(px, 2, 18, 5, 18, PAL["dark_concrete"], width, height)
    line(px, 5, 18, 8, 15, PAL["med_concrete"], width, height)
    sprinkle(px, [(2, 13), (8, 8)], PAL["orange_rust"], width, height)
    return img


def gen_torn_billboard() -> Image.Image:
    width, height = 20, 28
    img = new_image(width, height)
    px = img.load()
    line(px, 2, 8, 3, 27, PAL["dark_rust"], width, height)
    line(px, 13, 5, 16, 24, PAL["dark_rust"], width, height)
    line(px, 4, 4, 14, 2, PAL["orange_rust"], width, height)
    line(px, 5, 13, 15, 11, PAL["warm_gray"], width, height)
    for offset in range(7):
        y_top = 4 + offset
        x_left = 4 + offset // 2
        x_right = 14 + offset // 2
        for x in range(x_left, x_right + 1):
            if offset > 4 and x > x_right - 2:
                continue
            color = PAL["rotting_wood"]
            if (x + y_top) % 5 == 0:
                color = PAL["peeling_blue"]
            elif (x + y_top) % 7 == 0:
                color = PAL["signage_yellow"]
            elif (x + y_top) % 9 == 0:
                color = PAL["off_white"]
            put(px, x, y_top, color, width, height)
    sprinkle(px, [(11, 10), (12, 11), (11, 12)], PAL["off_white"], width, height)
    line(px, 6, 15, 6, 24, PAL["warm_gray"], width, height)
    line(px, 10, 17, 13, 23, PAL["warm_gray"], width, height)
    line(px, 3, 21, 6, 17, PAL["dark_concrete"], width, height)
    sprinkle(px, [(2, 13), (3, 16), (14, 11), (15, 14)], PAL["orange_rust"], width, height)
    add_weeds(px, [(3, 27), (15, 24)], width, height)
    return img


def gen_steel_beam() -> Image.Image:
    width, height = 32, 8
    img = new_image(width, height)
    px = img.load()
    line(px, 3, 2, 26, 2, PAL["warm_gray"], width, height)
    line(px, 3, 3, 26, 3, PAL["dark_concrete"], width, height)
    line(px, 4, 4, 25, 4, PAL["dark_rust"], width, height)
    line(px, 4, 5, 25, 5, PAL["orange_rust"], width, height)
    line(px, 3, 6, 23, 6, PAL["dark_concrete"], width, height)
    sprinkle(px, [(24, 6), (25, 5), (26, 4), (27, 4)], PAL["dark_rust"], width, height)
    sprinkle(px, [(8, 4), (15, 5), (21, 3)], PAL["orange_rust"], width, height)
    put(px, 5, 4, PAL["light_gray"], width, height)
    return img


def gen_steel_beam_diagonal() -> Image.Image:
    width, height = 32, 8
    img = new_image(width, height)
    px = img.load()
    line(px, 4, 6, 25, 1, PAL["warm_gray"], width, height)
    line(px, 4, 7, 25, 2, PAL["dark_concrete"], width, height)
    line(px, 5, 7, 26, 3, PAL["dark_rust"], width, height)
    sprinkle(px, [(11, 5), (16, 4), (22, 2)], PAL["orange_rust"], width, height)
    sprinkle(px, [(24, 3), (25, 3), (26, 2), (27, 2)], PAL["dark_rust"], width, height)
    return img


def gen_traffic_light() -> Image.Image:
    width, height = 8, 16
    img = new_image(width, height)
    px = img.load()
    line(px, 2, 13, 4, 6, PAL["warm_gray"], width, height)
    line(px, 3, 13, 5, 6, PAL["dark_concrete"], width, height)
    fill_rect(px, 4, 3, 6, 7, PAL["dark_warm_gray"], width, height)
    put(px, 5, 4, PAL["orange_rust"], width, height)
    put(px, 5, 5, PAL["dark_concrete"], width, height)
    put(px, 5, 6, PAL["dark_concrete"], width, height)
    put(px, 6, 4, PAL["light_gray"], width, height)
    sprinkle(px, [(1, 14), (2, 14), (3, 15)], PAL["med_concrete"], width, height)
    put(px, 4, 12, PAL["dark_rust"], width, height)
    return img


def gen_overturned_desk() -> Image.Image:
    width, height = 16, 12
    img = new_image(width, height)
    px = img.load()
    fill_rect(px, 3, 4, 10, 9, PAL["dark_concrete"], width, height)
    line(px, 4, 3, 11, 2, PAL["rotting_wood"], width, height)
    line(px, 4, 4, 11, 3, PAL["warm_gray"], width, height)
    fill_rect(px, 4, 6, 6, 8, PAL["dark_warm_gray"], width, height)
    fill_rect(px, 7, 5, 9, 7, PAL["dark_warm_gray"], width, height)
    put(px, 5, 7, PAL["light_gray"], width, height)
    put(px, 8, 6, PAL["light_gray"], width, height)
    line(px, 2, 10, 4, 9, PAL["off_white"], width, height)
    sprinkle(px, [(6, 10), (9, 10), (13, 9)], PAL["off_white"], width, height)
    put(px, 12, 8, PAL["rotting_wood"], width, height)
    put(px, 12, 9, PAL["warm_gray"], width, height)
    return img


def gen_mailbox() -> Image.Image:
    width, height = 8, 12
    img = new_image(width, height)
    px = img.load()
    line(px, 3, 5, 4, 11, PAL["rotting_wood"], width, height)
    fill_rect(px, 1, 2, 5, 5, PAL["peeling_blue"], width, height)
    line(px, 1, 2, 5, 1, PAL["dark_rust"], width, height)
    line(px, 2, 3, 4, 3, PAL["dark_warm_gray"], width, height)
    sprinkle(px, [(2, 1), (3, 0), (4, 1), (5, 0)], PAL["off_white"], width, height)
    sprinkle(px, [(1, 5), (5, 4)], PAL["orange_rust"], width, height)
    put(px, 5, 11, PAL["reclaim_green"], width, height)
    return img


def gen_dumpster() -> Image.Image:
    width, height = 12, 12
    img = new_image(width, height)
    px = img.load()
    fill_rect(px, 2, 4, 8, 10, PAL["reclaim_green"], width, height)
    line(px, 2, 4, 8, 2, PAL["med_concrete"], width, height)
    line(px, 2, 5, 8, 3, PAL["dark_concrete"], width, height)
    line(px, 8, 2, 10, 4, PAL["med_concrete"], width, height)
    line(px, 8, 3, 10, 5, PAL["dark_concrete"], width, height)
    fill_rect(px, 3, 5, 7, 6, PAL["deep_black"], width, height)
    sprinkle(px, [(4, 8), (7, 7)], PAL["orange_rust"], width, height)
    put(px, 3, 10, PAL["dark_warm_gray"], width, height)
    put(px, 8, 10, PAL["dark_warm_gray"], width, height)
    return img


def gen_dumpster_v2() -> Image.Image:
    width, height = 12, 12
    img = new_image(width, height)
    px = img.load()
    fill_rect(px, 2, 5, 8, 10, PAL["dark_concrete"], width, height)
    line(px, 2, 5, 8, 4, PAL["warm_gray"], width, height)
    line(px, 8, 4, 10, 5, PAL["med_concrete"], width, height)
    line(px, 2, 4, 6, 1, PAL["med_concrete"], width, height)
    line(px, 2, 5, 6, 2, PAL["dark_concrete"], width, height)
    fill_rect(px, 3, 5, 6, 6, PAL["deep_black"], width, height)
    sprinkle(px, [(4, 7), (7, 8), (8, 6)], PAL["orange_rust"], width, height)
    put(px, 7, 5, PAL["signage_yellow"], width, height)
    put(px, 3, 10, PAL["dark_warm_gray"], width, height)
    return img


def gen_urban_car() -> Image.Image:
    width, height = 32, 16
    img = new_image(width, height)
    px = img.load()
    for y in range(8, 13):
        fill_rect(px, 5, y, 25, y, PAL["dark_concrete"], width, height)
    line(px, 8, 7, 22, 7, PAL["med_concrete"], width, height)
    line(px, 9, 6, 21, 6, PAL["light_concrete"], width, height)
    line(px, 11, 4, 19, 4, PAL["med_concrete"], width, height)
    line(px, 10, 5, 20, 5, PAL["light_concrete"], width, height)
    fill_rect(px, 10, 6, 13, 7, PAL["broken_glass"], width, height)
    fill_rect(px, 17, 5, 20, 7, PAL["broken_glass"], width, height)
    fill_rect(px, 14, 7, 16, 9, PAL["deep_black"], width, height)
    line(px, 5, 8, 9, 6, PAL["med_concrete"], width, height)
    line(px, 22, 7, 26, 9, PAL["med_concrete"], width, height)
    sprinkle(px, [(6, 10), (7, 11), (12, 12), (20, 11), (23, 10)], PAL["orange_rust"], width, height)
    line(px, 8, 13, 10, 13, PAL["dark_warm_gray"], width, height)
    line(px, 21, 13, 23, 13, PAL["dark_warm_gray"], width, height)
    put(px, 9, 12, PAL["warm_gray"], width, height)
    put(px, 22, 12, PAL["warm_gray"], width, height)
    sprinkle(px, [(8, 8), (12, 8), (17, 8), (21, 8)], PAL["light_gray"], width, height)
    add_weeds(px, [(6, 13), (25, 13)], width, height)
    return img


def gen_concrete_debris() -> Image.Image:
    width, height = 16, 8
    img = new_image(width, height)
    px = img.load()
    add_rubble(px, 7, [(3, 4, 2), (7, 3, 3), (12, 5, 2)], width, height)
    line(px, 9, 4, 11, 2, PAL["dark_rust"], width, height)
    return img


def gen_concrete_debris_v2() -> Image.Image:
    width, height = 16, 8
    img = new_image(width, height)
    px = img.load()
    add_rubble(px, 7, [(2, 5, 2), (5, 3, 2), (8, 4, 2), (11, 2, 3)], width, height)
    sprinkle(px, [(4, 4), (10, 5), (13, 4)], PAL["light_gray"], width, height)
    put(px, 7, 4, PAL["orange_rust"], width, height)
    return img


def gen_concrete_debris_v3() -> Image.Image:
    width, height = 16, 8
    img = new_image(width, height)
    px = img.load()
    add_rubble(px, 7, [(1, 5, 2), (4, 4, 2), (8, 3, 3), (13, 5, 2)], width, height)
    line(px, 6, 5, 8, 3, PAL["dark_rust"], width, height)
    put(px, 14, 4, PAL["reclaim_green"], width, height)
    return img


GENERATORS = {
    "prop_chain_link_fence": gen_chain_link_fence,
    "prop_collapsed_stairs": gen_collapsed_stairs,
    "prop_supermarket_shelves": gen_supermarket_shelves,
    "prop_phone_booth": gen_phone_booth,
    "prop_torn_billboard": gen_torn_billboard,
    "prop_steel_beam": gen_steel_beam,
    "prop_steel_beam_diagonal": gen_steel_beam_diagonal,
    "prop_traffic_light": gen_traffic_light,
    "prop_overturned_desk": gen_overturned_desk,
    "prop_mailbox": gen_mailbox,
    "prop_dumpster": gen_dumpster,
    "prop_dumpster_v2": gen_dumpster_v2,
    "prop_urban_car": gen_urban_car,
    "prop_concrete_debris": gen_concrete_debris,
    "prop_concrete_debris_v2": gen_concrete_debris_v2,
    "prop_concrete_debris_v3": gen_concrete_debris_v3,
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("targets", nargs="*", help="Asset ids to regenerate. Defaults to the redesigned urban set.")
    parser.add_argument("--list", action="store_true", help="List supported asset ids and exit.")
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    if args.list:
        print("\n".join(GENERATORS.keys()))
        return

    targets = args.targets or DEFAULT_TARGETS
    unknown = [target for target in targets if target not in GENERATORS]
    if unknown:
        raise SystemExit(f"Unknown target(s): {', '.join(unknown)}")

    print("Generating redesigned Ruines Urbaines props...")
    print(f"Output: {OUTPUT_DIR}\n")

    for target in targets:
        image = GENERATORS[target]()
        save_prop(target, image)

    build_contact_sheet(targets)
    print(f"\nDone. {len(targets)} props regenerated.")


if __name__ == "__main__":
    main()
