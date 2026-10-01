"""Les cinq bonus lâchés : modèles SDF, reflet et dissolution déterministe (plan 25 S5)."""
from __future__ import annotations

import numpy as np
from PIL import Image, ImageDraw

from .kit import Item, box, line, oval, polygon, ring
from ..weapons.icons import render_icon

NAMES = {"heal": "Gourde", "magnet": "Fer à cheval aimanté", "shield": "Couverture de survie",
         "haste": "Café froid", "blast": "Pétard"}
COLORS = {"heal": "#7FCB89", "magnet": "#67A6E8", "shield": "#F0C85C", "haste": "#E8A868", "blast": "#C4432B"}


def models() -> list[Item]:
    canteen = Item("heal", .1)
    canteen.add("patina", oval((0, -2, 0), (9, 10, 4)), box((0, 8, 0), (4, 3, 2)))
    canteen.add("steel", box((0, 12, 0), (4.5, 2, 3)))
    canteen.add("dark", line([(-8, -2, 1), (-11, 5, 0), (-7, 9, 0)], 1))
    canteen.add("light", line([(-4, 3, 3.5), (-5, -2, 3.5)], .8))
    magnet = Item("magnet", .08)
    magnet.add("red", line([(-8, 11, 0), (-8, -3, 0), (-5, -9, 0), (5, -9, 0), (8, -3, 0), (8, 11, 0)], 2.6))
    magnet.add("steel", box((-8, 9, 0), (2.8, 3, 2.8)), box((8, 9, 0), (2.8, 3, 2.8)))
    blanket = Item("shield", .1)
    blanket.add("#BD913C", box((0, 0, 0), (10, 11, 2), 1))
    blanket.add("#F0C85C", polygon([(-9, 10), (9, 10), (7, -9), (-9, -7)], 2, .7))
    blanket.add("light", line([(-7, 8, 3), (-1, 2, 3), (-5, -5, 3)], .7))
    blanket.add("#8A5A1C", line([(2, 9, 3), (5, 2, 3), (0, -7, 3)], 1.2))
    coffee = Item("haste", .1)
    coffee.add("paper", polygon([(-9, 10), (9, 10), (6, -11), (-6, -11)], 0, 3))
    coffee.add("blue", box((0, -1, 2.8), (7, 4, .8)))
    coffee.add("dark", oval((0, 11, 0), (9, 2.5, 3)))
    coffee.add("light", line([(-9, 10, 2), (9, 10, 2)], .7))
    cracker = Item("blast", .1)
    cracker.add("red", box((0, -3, 0), (4, 9, 3), 1.5))
    cracker.add("paper", box((0, 0, 2.8), (3.8, 1, .5)))
    cracker.add("dark", line([(0, 6, 0), (3, 10, 0), (1, 13, 0)], 1))
    return [canteen, magnet, blanket, coffee, cracker]


def render(item: Item, seed: int) -> tuple[list[Image.Image], list[Image.Image], Image.Image]:
    base = render_icon(item.model(), 16, 14, (.5, .46, .42, .38, .34))
    idle = []
    for frame, offset in enumerate((0, -1, 0, 1)):
        sprite = Image.new("RGBA", (16, 16))
        sprite.alpha_composite(base, (0, offset))
        for y in range(16):
            for x in range(16):
                if sprite.getpixel((x, y))[3] and x + y == 5 + frame * 6:
                    sprite.putpixel((x, y), (232, 224, 212, 255))
        idle.append(sprite)
    rng = np.random.default_rng(seed)
    order = rng.random((16, 16))
    disappear = []
    for frame in range(3):
        sprite = Image.new("RGBA", (16, 16))
        for y in range(16):
            for x in range(16):
                pixel = base.getpixel((x, y))
                if pixel[3] and order[y, x] > (frame + 1) * .28:
                    dx, dy = min(15, x + frame), max(0, y - frame)
                    sprite.putpixel((dx, dy), (107, 79, 160, 255) if frame > 0 or (x + y) % 3 == 0 else pixel)
        disappear.append(sprite)
    glow = Image.new("RGBA", (16, 8))
    rgb = tuple(bytes.fromhex(COLORS[item.item_id][1:]))
    draw = ImageDraw.Draw(glow)
    draw.ellipse((0, 0, 15, 7), fill=(*rgb, 42))
    draw.ellipse((3, 2, 12, 5), fill=(*rgb, 75))
    return idle, disappear, glow
