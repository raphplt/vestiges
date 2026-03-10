#!/usr/bin/env python3
"""Generate additional Wild Fields parcel tiles."""

from __future__ import annotations

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
    "wood_fence": (0x6A, 0x5A, 0x42),
    "wind_visible": (0xD0, 0xD8, 0xD0),
    "dark_warm_gray": (0x3A, 0x35, 0x35),
    "off_white": (0xE8, 0xE0, 0xD4),
}

OUTPUT_DIR = Path(__file__).resolve().parent.parent / "assets" / "tiles" / "champs"
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
CONTACT_SHEET = OUTPUT_DIR / "_champs_field_variants.png"


def new_image(width: int = 64, height: int = 32) -> Image.Image:
    return Image.new("RGBA", (width, height), (0, 0, 0, 0))


def put(px, x: int, y: int, color, width: int, height: int) -> None:
    if 0 <= x < width and 0 <= y < height:
        px[x, y] = color + (255,)


def inside_diamond(x: int, y: int, width: int, height: int) -> bool:
    cx = width // 2
    cy = height // 2
    dx = abs(x - cx)
    dy = abs(y - cy)
    return dx * height + dy * width <= (width * height) // 2


def fill_diamond(img: Image.Image, chooser) -> Image.Image:
    width, height = img.size
    px = img.load()
    for y in range(height):
        for x in range(width):
            if not inside_diamond(x, y, width, height):
                continue
            put(px, x, y, chooser(x, y), width, height)
    return img


def save(name: str, img: Image.Image) -> None:
    path = OUTPUT_DIR / f"{name}.png"
    img.save(path)
    print(f"  + {name}.png ({img.width}x{img.height})")


def build_sheet(names: list[str]) -> None:
    scale = 4
    cell_w = 64 * scale + 8
    cell_h = 32 * scale + 8
    cols = 2
    rows = (len(names) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * cell_w, rows * cell_h), PAL["dark_warm_gray"] + (255,))
    for index, name in enumerate(names):
        with Image.open(OUTPUT_DIR / f"{name}.png") as source:
            preview = source.resize((source.width * scale, source.height * scale), Image.Resampling.NEAREST)
        col = index % cols
        row = index // cols
        ox = col * cell_w + 4
        oy = row * cell_h + 4
        sheet.alpha_composite(preview, (ox, oy))
    sheet.save(CONTACT_SHEET)
    print(f"  + {CONTACT_SHEET.name} ({sheet.width}x{sheet.height})")


def make_wheat_dense(variant: int) -> Image.Image:
    def chooser(x: int, y: int):
        row = (x + y * 2 + variant * 3) % 11
        if row in (0, 1):
            return PAL["grass_pale"]
        if row in (2, 3):
            return PAL["grass_gold"]
        if row == 4:
            return PAL["earth_light"]
        if (x * 3 + y * 5 + variant) % 17 == 0:
            return PAL["off_white"]
        return PAL["grass_dark"] if (x + y + variant) % 7 == 0 else PAL["grass_vivid"]

    img = new_image()
    return fill_diamond(img, chooser)


def make_stubble(variant: int) -> Image.Image:
    def chooser(x: int, y: int):
        furrow = (x * 2 + y + variant * 5) % 12
        if furrow in (0, 1):
            return PAL["earth_light"]
        if furrow in (2, 3):
            return PAL["earth_dry"]
        if (x + y * 2 + variant) % 13 == 0:
            return PAL["grass_gold"]
        if (x * 5 + y * 3 + variant) % 19 == 0:
            return PAL["wood_fence"]
        return PAL["earth_dry"] if (x + y + variant) % 5 else PAL["field_stone"]

    img = new_image()
    return fill_diamond(img, chooser)


def make_flower_field(variant: int) -> Image.Image:
    def chooser(x: int, y: int):
        sway = (x + y * 3 + variant * 7) % 14
        if sway in (0, 1, 2):
            return PAL["grass_pale"]
        if sway in (3, 4, 5):
            return PAL["grass_gold"]
        if (x * 7 + y * 11 + variant) % 37 == 0:
            return PAL["flower_red"]
        if (x * 5 + y * 9 + variant) % 41 == 0:
            return PAL["flower_blue"]
        if (x + y * 2 + variant) % 23 == 0:
            return PAL["wind_visible"]
        return PAL["grass_dark"] if (x + y + variant) % 6 == 0 else PAL["grass_vivid"]

    img = new_image()
    return fill_diamond(img, chooser)


def main() -> None:
    print("Generating Wild Fields parcel tiles...")
    names = [
        "tile_champs_ble_dense_base",
        "tile_champs_ble_dense_v2",
        "tile_champs_chaume_base",
        "tile_champs_chaume_v2",
        "tile_champs_fleurs_base",
        "tile_champs_fleurs_v2",
    ]
    save(names[0], make_wheat_dense(0))
    save(names[1], make_wheat_dense(1))
    save(names[2], make_stubble(0))
    save(names[3], make_stubble(1))
    save(names[4], make_flower_field(0))
    save(names[5], make_flower_field(1))
    build_sheet(names)
    print(f"\nDone. {len(names)} tiles generated in {OUTPUT_DIR}")


if __name__ == "__main__":
    main()
