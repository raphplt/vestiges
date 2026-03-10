#!/usr/bin/env python3
"""Generate high-value hero props for the Wild Fields biome."""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image


PAL = {
    "grass_dark": (0x3A, 0x6A, 0x30),
    "grass_vivid": (0x5A, 0xA8, 0x48),
    "grass_gold": (0xA8, 0xB4, 0x58),
    "grass_pale": (0xC8, 0xD4, 0x80),
    "earth_dry": (0x8A, 0x70, 0x58),
    "earth_light": (0xB8, 0xA0, 0x80),
    "field_stone": (0x7A, 0x7A, 0x6A),
    "flower_red": (0xC4, 0x4A, 0x3A),
    "flower_blue": (0x5A, 0x7A, 0xCA),
    "sky_reflect": (0x8A, 0xB0, 0xD0),
    "wood_fence": (0x6A, 0x5A, 0x42),
    "wind_visible": (0xD0, 0xD8, 0xD0),
    "dark_warm_gray": (0x3A, 0x35, 0x35),
    "warm_gray": (0x6B, 0x61, 0x61),
    "light_gray": (0x9E, 0x94, 0x94),
    "off_white": (0xE8, 0xE0, 0xD4),
    "dark_rust": (0x6B, 0x3A, 0x24),
    "orange_rust": (0xA8, 0x5C, 0x30),
    "deep_black": (0x1A, 0x1A, 0x2E),
}

OUTPUT_DIR = Path(__file__).resolve().parent.parent / "assets" / "props" / "wild_fields"
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
CONTACT_SHEET = OUTPUT_DIR / "_wild_fields_hero_props.png"

OUTLINE = {
    PAL["grass_dark"]: PAL["dark_warm_gray"],
    PAL["grass_vivid"]: PAL["grass_dark"],
    PAL["grass_gold"]: PAL["earth_dry"],
    PAL["grass_pale"]: PAL["grass_gold"],
    PAL["earth_dry"]: PAL["wood_fence"],
    PAL["earth_light"]: PAL["earth_dry"],
    PAL["field_stone"]: PAL["warm_gray"],
    PAL["flower_red"]: PAL["dark_rust"],
    PAL["flower_blue"]: PAL["dark_warm_gray"],
    PAL["sky_reflect"]: PAL["warm_gray"],
    PAL["wood_fence"]: PAL["dark_warm_gray"],
    PAL["wind_visible"]: PAL["light_gray"],
    PAL["warm_gray"]: PAL["dark_warm_gray"],
    PAL["light_gray"]: PAL["warm_gray"],
    PAL["off_white"]: PAL["light_gray"],
    PAL["dark_rust"]: PAL["dark_warm_gray"],
    PAL["orange_rust"]: PAL["dark_rust"],
}


def new_image(width: int, height: int) -> Image.Image:
    return Image.new("RGBA", (width, height), (0, 0, 0, 0))


def put(px, x: int, y: int, color, width: int, height: int) -> None:
    if 0 <= x < width and 0 <= y < height:
        px[x, y] = color + (255,)


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


def outline_for(rgb: tuple[int, int, int]) -> tuple[int, int, int]:
    if rgb in OUTLINE:
        return OUTLINE[rgb]
    nearest = min(OUTLINE.keys(), key=lambda value: sum(abs(value[i] - rgb[i]) for i in range(3)))
    return OUTLINE[nearest]


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


def build_contact_sheet(names: list[str]) -> None:
    assets = [OUTPUT_DIR / f"{name}.png" for name in names if (OUTPUT_DIR / f"{name}.png").is_file()]
    if not assets:
        return
    scale = 8
    cell_w = max(Image.open(path).width for path in assets) * scale + 12
    cell_h = max(Image.open(path).height for path in assets) * scale + 12
    cols = 3 if len(assets) > 4 else 2
    rows = math.ceil(len(assets) / cols)
    sheet = Image.new("RGBA", (cols * cell_w, rows * cell_h), PAL["deep_black"] + (255,))
    for index, path in enumerate(assets):
        with Image.open(path) as source:
            preview = source.resize((source.width * scale, source.height * scale), Image.Resampling.NEAREST)
        col = index % cols
        row = index // cols
        ox = col * cell_w + (cell_w - preview.width) // 2
        oy = row * cell_h + (cell_h - preview.height) // 2
        sheet.alpha_composite(preview, (ox, oy))
    sheet.save(CONTACT_SHEET)
    print(f"  + {CONTACT_SHEET.name} ({sheet.width}x{sheet.height})")


def gen_abandoned_well() -> Image.Image:
    width, height = 14, 16
    img = new_image(width, height)
    px = img.load()
    line(px, 3, 8, 7, 6, PAL["earth_light"], width, height)
    line(px, 7, 6, 11, 8, PAL["earth_light"], width, height)
    line(px, 3, 8, 7, 10, PAL["field_stone"], width, height)
    line(px, 7, 10, 11, 8, PAL["field_stone"], width, height)
    fill_rect(px, 5, 8, 9, 9, PAL["deep_black"], width, height)
    sprinkle(px, [(5, 8), (8, 8)], PAL["sky_reflect"], width, height)
    line(px, 4, 8, 4, 13, PAL["wood_fence"], width, height)
    line(px, 10, 8, 10, 11, PAL["wood_fence"], width, height)
    line(px, 10, 11, 11, 13, PAL["dark_rust"], width, height)
    line(px, 4, 6, 8, 4, PAL["wood_fence"], width, height)
    line(px, 8, 4, 11, 5, PAL["dark_rust"], width, height)
    line(px, 6, 5, 8, 8, PAL["warm_gray"], width, height)
    put(px, 8, 9, PAL["light_gray"], width, height)
    sprinkle(px, [(2, 14), (3, 13), (9, 14), (10, 14), (11, 13)], PAL["grass_gold"], width, height)
    sprinkle(px, [(3, 14), (10, 13)], PAL["grass_vivid"], width, height)
    return img


def gen_standing_stone() -> Image.Image:
    width, height = 8, 20
    img = new_image(width, height)
    px = img.load()
    for y in range(3, 18):
        left = 2 if y < 7 else (1 if y < 15 else 2)
        right = 5 if y < 10 else (4 if y < 16 else 5)
        for x in range(left, right + 1):
            color = PAL["field_stone"]
            if x == left or y > 13:
                color = PAL["warm_gray"]
            if x == right - 1 and y < 10:
                color = PAL["earth_light"]
            put(px, x, y, color, width, height)
    sprinkle(px, [(3, 7), (4, 10), (3, 12), (4, 14)], PAL["off_white"], width, height)
    line(px, 3, 7, 3, 8, PAL["flower_blue"], width, height)
    line(px, 4, 10, 4, 11, PAL["flower_red"], width, height)
    sprinkle(px, [(1, 18), (2, 17), (5, 18), (6, 18)], PAL["grass_gold"], width, height)
    sprinkle(px, [(2, 18), (5, 17)], PAL["grass_vivid"], width, height)
    return img


def gen_rusted_plow() -> Image.Image:
    width, height = 20, 10
    img = new_image(width, height)
    px = img.load()
    line(px, 3, 7, 12, 3, PAL["wood_fence"], width, height)
    line(px, 4, 8, 13, 4, PAL["earth_dry"], width, height)
    line(px, 10, 4, 14, 5, PAL["dark_rust"], width, height)
    line(px, 11, 5, 15, 6, PAL["orange_rust"], width, height)
    line(px, 13, 6, 16, 8, PAL["dark_rust"], width, height)
    line(px, 14, 5, 18, 4, PAL["warm_gray"], width, height)
    line(px, 15, 6, 18, 5, PAL["orange_rust"], width, height)
    sprinkle(px, [(6, 8), (7, 8), (10, 7), (12, 6)], PAL["earth_light"], width, height)
    sprinkle(px, [(2, 8), (3, 8), (4, 9), (16, 9), (17, 9)], PAL["grass_gold"], width, height)
    sprinkle(px, [(4, 8), (15, 8)], PAL["grass_vivid"], width, height)
    return img


def gen_tractor_remains() -> Image.Image:
    width, height = 28, 16
    img = new_image(width, height)
    px = img.load()
    fill_rect(px, 8, 7, 17, 10, PAL["dark_rust"], width, height)
    fill_rect(px, 9, 6, 15, 7, PAL["orange_rust"], width, height)
    fill_rect(px, 12, 5, 15, 6, PAL["earth_light"], width, height)
    fill_rect(px, 12, 7, 14, 9, PAL["deep_black"], width, height)
    line(px, 16, 6, 20, 8, PAL["wood_fence"], width, height)
    line(px, 20, 8, 23, 7, PAL["dark_rust"], width, height)
    line(px, 9, 7, 7, 4, PAL["warm_gray"], width, height)
    line(px, 10, 7, 8, 4, PAL["light_gray"], width, height)
    line(px, 15, 5, 15, 2, PAL["warm_gray"], width, height)
    line(px, 15, 2, 18, 3, PAL["warm_gray"], width, height)
    fill_rect(px, 4, 9, 8, 13, PAL["dark_warm_gray"], width, height)
    fill_rect(px, 5, 10, 7, 12, PAL["warm_gray"], width, height)
    fill_rect(px, 18, 10, 20, 12, PAL["dark_warm_gray"], width, height)
    put(px, 19, 11, PAL["warm_gray"], width, height)
    sprinkle(px, [(5, 11), (6, 10), (9, 8), (11, 10), (17, 9), (20, 10)], PAL["light_gray"], width, height)
    sprinkle(px, [(11, 6), (16, 7), (18, 8)], PAL["grass_vivid"], width, height)
    sprinkle(px, [(7, 7), (13, 4), (14, 4), (22, 7)], PAL["grass_gold"], width, height)
    line(px, 2, 14, 9, 12, PAL["grass_gold"], width, height)
    line(px, 16, 14, 24, 12, PAL["grass_gold"], width, height)
    return img


def gen_tractor_husk() -> Image.Image:
    width, height = 26, 15
    img = new_image(width, height)
    px = img.load()
    fill_rect(px, 8, 6, 16, 9, PAL["dark_rust"], width, height)
    fill_rect(px, 9, 5, 13, 6, PAL["orange_rust"], width, height)
    fill_rect(px, 11, 4, 13, 5, PAL["earth_light"], width, height)
    fill_rect(px, 11, 6, 13, 8, PAL["deep_black"], width, height)
    line(px, 16, 7, 20, 6, PAL["warm_gray"], width, height)
    line(px, 18, 8, 22, 9, PAL["dark_rust"], width, height)
    line(px, 7, 8, 4, 4, PAL["warm_gray"], width, height)
    line(px, 8, 8, 5, 4, PAL["light_gray"], width, height)
    line(px, 15, 5, 16, 2, PAL["warm_gray"], width, height)
    fill_rect(px, 4, 9, 8, 13, PAL["dark_warm_gray"], width, height)
    fill_rect(px, 5, 10, 7, 12, PAL["warm_gray"], width, height)
    fill_rect(px, 18, 9, 21, 12, PAL["dark_warm_gray"], width, height)
    fill_rect(px, 19, 10, 20, 11, PAL["warm_gray"], width, height)
    line(px, 3, 13, 10, 11, PAL["grass_gold"], width, height)
    line(px, 15, 13, 23, 11, PAL["grass_gold"], width, height)
    sprinkle(px, [(6, 9), (10, 7), (14, 6), (17, 8), (19, 9)], PAL["light_gray"], width, height)
    sprinkle(px, [(9, 6), (12, 9), (16, 8), (17, 7)], PAL["grass_vivid"], width, height)
    sprinkle(px, [(4, 13), (8, 12), (18, 13), (21, 12)], PAL["grass_pale"], width, height)
    return img


def gen_windmill_ruin() -> Image.Image:
    width, height = 18, 28
    img = new_image(width, height)
    px = img.load()
    line(px, 9, 5, 8, 22, PAL["wood_fence"], width, height)
    line(px, 10, 5, 9, 22, PAL["earth_light"], width, height)
    line(px, 7, 12, 11, 12, PAL["wood_fence"], width, height)
    line(px, 6, 9, 12, 15, PAL["warm_gray"], width, height)
    line(px, 6, 15, 12, 9, PAL["light_gray"], width, height)
    line(px, 7, 10, 4, 8, PAL["warm_gray"], width, height)
    line(px, 11, 10, 14, 8, PAL["warm_gray"], width, height)
    line(px, 7, 14, 4, 16, PAL["light_gray"], width, height)
    line(px, 11, 14, 13, 17, PAL["dark_rust"], width, height)
    line(px, 10, 6, 12, 3, PAL["orange_rust"], width, height)
    line(px, 8, 21, 5, 25, PAL["wood_fence"], width, height)
    line(px, 9, 21, 13, 25, PAL["wood_fence"], width, height)
    line(px, 5, 25, 13, 25, PAL["earth_dry"], width, height)
    sprinkle(px, [(6, 26), (7, 25), (10, 26), (11, 26), (12, 25)], PAL["grass_gold"], width, height)
    sprinkle(px, [(7, 26), (11, 25)], PAL["grass_vivid"], width, height)
    return img


def gen_silo_ruin() -> Image.Image:
    width, height = 22, 26
    img = new_image(width, height)
    px = img.load()
    fill_rect(px, 7, 5, 14, 20, PAL["light_gray"], width, height)
    fill_rect(px, 7, 20, 14, 22, PAL["warm_gray"], width, height)
    line(px, 7, 5, 10, 2, PAL["earth_light"], width, height)
    line(px, 10, 2, 14, 5, PAL["off_white"], width, height)
    fill_rect(px, 9, 9, 12, 15, PAL["deep_black"], width, height)
    line(px, 14, 11, 18, 13, PAL["dark_rust"], width, height)
    line(px, 15, 12, 19, 15, PAL["orange_rust"], width, height)
    line(px, 8, 16, 4, 18, PAL["warm_gray"], width, height)
    line(px, 4, 18, 3, 22, PAL["dark_rust"], width, height)
    line(px, 3, 22, 6, 24, PAL["earth_dry"], width, height)
    sprinkle(px, [(8, 7), (11, 6), (13, 8), (8, 18), (12, 19)], PAL["off_white"], width, height)
    sprinkle(px, [(15, 14), (16, 15), (17, 14), (18, 15)], PAL["light_gray"], width, height)
    sprinkle(px, [(5, 24), (6, 23), (11, 24), (14, 24), (15, 23)], PAL["grass_gold"], width, height)
    sprinkle(px, [(6, 24), (14, 23)], PAL["grass_vivid"], width, height)
    return img


def main() -> None:
    print("Generating Wild Fields hero props...")
    names = [
        "prop_abandoned_well",
        "prop_standing_stone",
        "prop_rusted_plow",
        "prop_tractor_remains",
        "prop_tractor_husk",
        "prop_windmill_ruin",
        "prop_silo_ruin",
    ]
    save_prop(names[0], gen_abandoned_well())
    save_prop(names[1], gen_standing_stone())
    save_prop(names[2], gen_rusted_plow())
    save_prop(names[3], gen_tractor_remains())
    save_prop(names[4], gen_tractor_husk())
    save_prop(names[5], gen_windmill_ruin())
    save_prop(names[6], gen_silo_ruin())
    build_contact_sheet(names)
    print(f"\nDone. {len(names)} hero props generated in {OUTPUT_DIR}")


if __name__ == "__main__":
    main()
