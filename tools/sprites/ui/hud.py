"""Textures du HUD, calculées pixel par pixel à résolution native (plan 25 S6)."""
from __future__ import annotations

import math

from PIL import Image, ImageDraw

from .kit import strip

GOLD = ("#8A5A1C", "#BD913C", "#F0C85C", "#FFF0B8")
VOID = "#6B4FA0"


def pattern(rows: tuple[str, ...], palette: dict[str, str]) -> Image.Image:
    image = Image.new("RGBA", (len(rows[0]), len(rows)))
    draw = ImageDraw.Draw(image)
    for y, row in enumerate(rows):
        for x, pixel in enumerate(row):
            if pixel in palette:
                draw.point((x, y), fill=palette[pixel])
    return image


def xp_frame() -> Image.Image:
    image = Image.new("RGBA", (16, 9), "#16213E")
    draw = ImageDraw.Draw(image)
    draw.line((1, 0, 14, 0), fill="#9E9494")
    draw.line((2, 1, 13, 1), fill="#6B6161")
    draw.line((1, 8, 14, 8), fill="#3A3535")
    for x in (0, 12):
        draw.rectangle((x, 1, x + 3, 8), fill="#3A3535")
        draw.rectangle((x + 1, 1, x + 2, 7), fill="#6B6161")
        draw.point((x + 1, 2), fill="#E8E0D4")
        draw.point((x + 2, 6), fill="#5A9A8A")
    return image


def xp_fill(frame: int) -> Image.Image:
    image = Image.new("RGBA", (16, 6))
    draw = ImageDraw.Draw(image)
    for y in range(6):
        for x in range(16):
            crest = (x + frame * 4) % 16
            tone = 3 if y == 0 and crest < 7 else (2 if y < 3 else 1 if (x + y + frame) % 3 else 0)
            draw.point((x, y), fill=GOLD[tone])
    return image


def xp_tip(frame: int) -> Image.Image:
    image = Image.new("RGBA", (8, 9))
    draw = ImageDraw.Draw(image)
    draw.line((3, 1, 3, 7), fill=GOLD[2])
    draw.line((4, 2, 4, 6), fill=GOLD[3])
    draw.point((1 + frame % 2, frame * 2), fill=GOLD[3])
    draw.point((6, 7 - frame * 2), fill=GOLD[2])
    return image


def burst(frame: int, size: int = 16) -> Image.Image:
    image = Image.new("RGBA", (size, size))
    draw = ImageDraw.Draw(image)
    radius = 2 + frame * 1.8
    for ray in range(8):
        angle = ray * math.tau / 8
        x, y = round(size / 2 + math.cos(angle) * radius), round(size / 2 + math.sin(angle) * radius)
        if 0 <= x < size and 0 <= y < size:
            draw.point((x, y), fill=GOLD[3 if frame < 2 else 2])
            if frame < 3:
                draw.point((x - round(math.cos(angle)), y - round(math.sin(angle))), fill=GOLD[2])
    return image


def seal(color: str, step: int) -> Image.Image:
    image = Image.new("RGBA", (16, 16))
    draw = ImageDraw.Draw(image)
    draw.polygon([(3, 3), (8, 1), (12, 3), (14, 8), (12, 12), (8, 14), (3, 12), (1, 8)], fill="#2D1B3D")
    draw.ellipse((3, 3, 12, 12), fill=color)
    draw.line((5, 5, 9, 5), fill="#E8E0D4")
    draw.line([(6, 7), (5, 9), (9, 10), (11, 7)], fill="#3A3535")
    for index in range(8):
        angle = -math.pi / 2 + index * math.tau / 8
        x, y = round(7.5 + math.cos(angle) * 7), round(7.5 + math.sin(angle) * 7)
        draw.rectangle((x, y, min(15, x + 1), min(15, y + 1)), fill=GOLD[2] if index < step else "#6B6161")
    return image


def assets() -> dict[str, Image.Image]:
    images = {"xp_frame": xp_frame()}
    for frame in range(4):
        images[f"xp_fill_{frame:02}"] = xp_fill(frame)
        images[f"xp_tip_{frame:02}"] = xp_tip(frame)
        images[f"xp_burst_{frame:02}"] = burst(frame)
    images["kill_skull"] = pattern(("  ssssss  ", " slllllls ", "slllllllls", "slddlddlls", "slddlddlos",
                                    "sllldlllos", " slllllos ", "  sososs  ", "  s s s   ", "          "),
                                   {"s": "#3A3535", "l": "#E8E0D4", "o": "#A68B6B", "d": "#1A1A2E"})
    patterns = {"chest": (" sss ", "sllls", "ssoss", "sllls", " sss "),
                "memorial": ("  l  ", " sls ", " sls ", " sls ", "sllls"),
                "rift": ("  l s", " sll ", " ll  ", " lls ", "s l  "),
                "place": ("  l  ", " sss ", "lsosl", " sss ", "  l  "),
                "player": ("  l  ", " lll ", "lllll", "  l  ", "  l  ")}
    for name, rows in patterns.items():
        images[f"minimap_{name}"] = pattern(rows, {"s": "#6B6161", "l": "#F5F0EB", "o": "#C8C2B6"})
    for name, color in (("red", "#C4432B"), ("green", "#4A8C3F"), ("blue", "#5A7A9A")):
        images[f"quest_seal_{name}"] = strip([seal(color, step) for step in range(9)])
    images["quest_seal_break"] = strip([burst(frame) for frame in range(5)])
    return images
