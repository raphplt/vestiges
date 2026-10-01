"""Éclats à 1–5 facettes, frises et transitions de rareté, palette JSON unique.

UI à résolution native : triangles de verre quantifiés, contours sel-out,
reflets en marches. Aucun flou, aucune interpolation de pixels.
"""
from __future__ import annotations

import json
import math
from pathlib import Path

from PIL import Image, ImageDraw

from .kit import CARD, strip

PALETTE = json.loads(Path("data/ui/rarities.json").read_text())["rarities"][:5]


def shard(rank: int, size: int, frame: int = 0) -> Image.Image:
    palette = PALETTE[rank]
    image = Image.new("RGBA", (size, size))
    draw = ImageDraw.Draw(image)
    # Un polygone à n+2 sommets se partage en exactement n triangles.
    count = rank + 3
    vertices = [(round(size * .49 + math.cos(-math.pi / 2 + i * math.tau / count) * size * .38),
                 round(size * .48 + math.sin(-math.pi / 2 + i * math.tau / count) * size * .42)) for i in range(count)]
    colors = (palette["color"], palette["dark"], palette["light"], palette["color"], palette["dark"])
    for facet in range(rank + 1):
        draw.polygon([vertices[0], vertices[facet + 1], vertices[facet + 2]], fill=colors[facet])
    draw.line(vertices + vertices[:1], fill=palette["dark"], width=1)
    draw.line(vertices[-1:] + vertices[:2], fill=palette["light"], width=1)
    for index in range(2, count - 1):
        draw.line([vertices[0], vertices[index]], fill=palette["light"], width=1)
    if rank == 4:
        # Un reflet discret traverse la matière, sans halo lissé.
        px = image.load()
        for y in range(size):
            for x in range(size):
                if px[x, y][3] and (x + y - frame * (size // 3)) % (size * 2) == size // 2:
                    px[x, y] = (*bytes.fromhex(palette["light"][1:]), 255)
    return image


def card(rank: int, frame: int = 0) -> Image.Image:
    image = Image.open(CARD).convert("RGBA")
    draw = ImageDraw.Draw(image)
    palette = PALETTE[rank]
    w, h = image.size
    draw.line([(2, h - 2), (1, 2), (2, 1), (w - 3, 1)], fill=palette["color"])
    draw.line([(w - 2, 2), (w - 2, h - 3), (w - 3, h - 2), (2, h - 2)], fill=palette["dark"])
    for x, y in ((2, 2), (w - 3, 2), (2, h - 3), (w - 3, h - 3)):
        draw.point((x, y), fill=palette["light"])
    if rank == 4:
        for x in range(4, w - 4, 3):
            draw.point((x, 2), fill=palette["light"] if x % 4 == frame else palette["color"])
            draw.point((w - x - 1, h - 3), fill=palette["light"] if x % 4 == frame else palette["color"])
    return image


def clover() -> Image.Image:
    image = Image.new("RGBA", (8, 8))
    draw = ImageDraw.Draw(image)
    for x, y in ((1, 1), (4, 1), (1, 4), (4, 4)):
        draw.rectangle((x, y, x + 1, y + 1), fill=PALETTE[1]["color"])
        draw.point((x, y), fill=PALETTE[1]["light"])
    draw.line((3, 3, 4, 7), fill=PALETTE[1]["dark"])
    return image


def jump(rank: int) -> list[Image.Image]:
    frames = []
    for frame in range(6):
        image = Image.new("RGBA", (32, 32))
        before, after = shard(rank, 24), shard(rank + 1, 24, max(0, frame - 2))
        if frame < 3:
            image.alpha_composite(after, (4, 4))
            image.alpha_composite(before.crop((0, 0, 12, 24)), (4 - frame * 2, 4))
            image.alpha_composite(before.crop((12, 0, 24, 24)), (16 + frame * 2, 4))
            ImageDraw.Draw(image).line([(15, 6), (13, 12), (17, 17), (15, 25)], fill=PALETTE[rank + 1]["light"])
        else:
            image.alpha_composite(after, (4, 4))
        image.alpha_composite(clover(), (24, 24))
        frames.append(image)
    return frames


def assets() -> dict[str, Image.Image]:
    images = {"rarity_clover": clover()}
    for rank, palette in enumerate(PALETTE):
        name = palette["id"]
        for size in (12, 24):
            frames = [shard(rank, size, frame) for frame in range(4 if rank == 4 else 1)]
            images[f"rarity_{name}_{size}"] = strip(frames)
        images[f"ui_card_{name}"] = strip([card(rank, f) for f in range(4 if rank == 4 else 1)])
        if rank < 4:
            images[f"rarity_jump_{name}"] = strip(jump(rank))
    return images
