"""Curseurs de souris pixel art (plan 37 H5) : flèche de pierre à arête dorée, et main qui pointe au survol.

Chaque forme est un masque de pixels pleins : son bord intérieur devient le contour sombre, le reste est ombré en
pierre, lumière en haut à gauche comme le reste du jeu. Exportés à l'échelle 1 ; le jeu les agrandit d'un facteur
entier selon la hauteur de la fenêtre.
"""
from __future__ import annotations

from PIL import Image, ImageDraw

OUTLINE = (24, 20, 34, 255)
LIGHT = (239, 230, 210, 255)
MID = (196, 184, 162, 255)
DARK = (142, 129, 112, 255)
GOLD = (240, 200, 92, 255)
GOLD_DARK = (189, 145, 60, 255)

# Point actif de chaque forme, en pixels de l'échelle 1.
HOTSPOTS = {"arrow": (0, 0), "hand": (6, 0)}


def _mask(size: tuple[int, int], polygons: list[list[tuple[int, int]]], rects: list[tuple[int, int, int, int]]) -> Image.Image:
    mask = Image.new("1", size, 0)
    draw = ImageDraw.Draw(mask)
    for polygon in polygons:
        draw.polygon(polygon, fill=1)
    for rect in rects:
        draw.rectangle(rect, fill=1)
    return mask


def _shade(mask: Image.Image, accent: callable) -> Image.Image:
    """Bord intérieur sombre ; dedans, lumière en haut à gauche, ombre contre le bord bas et droit."""
    width, height = mask.size
    inside = lambda x, y: 0 <= x < width and 0 <= y < height and mask.getpixel((x, y))
    image = Image.new("RGBA", mask.size)
    for y in range(height):
        for x in range(width):
            if not inside(x, y):
                continue
            if not all(inside(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                color = OUTLINE
            elif not inside(x + 1, y + 1) or not inside(x + 2, y) or not inside(x, y + 2):
                color = DARK
            elif not inside(x - 2, y) or not inside(x, y - 2):
                color = LIGHT
            else:
                color = MID
            image.putpixel((x, y), accent(x, y) or color if color != OUTLINE else color)
    return image


ARROW = (
    "X..........",
    "XX.........",
    "XwX........",
    "XwwX.......",
    "XwwwX......",
    "XwwwwX.....",
    "XwwwwwX....",
    "XwwwwwwX...",
    "XwwwwwwwX..",
    "XwwwwwwwwX.",
    "XwwwwwwwwwX",
    "XwwwwwwXXXX",
    "XwwwXwwX...",
    "XwwX.XwwX..",
    "XwX..XwwX..",
    "XX....XwwX.",
    "......XwwX.",
    ".......XX..",
)


def arrow() -> Image.Image:
    """Flèche classique tracée au pixel, arête gauche dorée : le geste du menu, dans la pierre des écrans."""
    mask = Image.new("1", (len(ARROW[0]), len(ARROW)), 0)
    for y, row in enumerate(ARROW):
        for x, pixel in enumerate(row):
            if pixel != ".":
                mask.putpixel((x, y), 1)
    image = _shade(mask, lambda x, y: GOLD if x == 1 and 1 < y < 12 else None)
    # Le contour suit le dessin, pas le calcul : les marches de la diagonale restent pleines.
    for y, row in enumerate(ARROW):
        for x, pixel in enumerate(row):
            if pixel == "X":
                image.putpixel((x, y), OUTLINE)
    return image


def hand() -> Image.Image:
    """Main qui pointe de l'index ; manchette dorée au poignet."""
    rects = [
        (5, 0, 8, 10),    # index
        (8, 5, 10, 10),   # majeur replié
        (10, 6, 12, 10),  # annulaire
        (12, 7, 14, 11),  # auriculaire
        (4, 9, 14, 15),   # paume
        (1, 9, 4, 12),    # pouce
        (2, 12, 4, 13),
        (5, 15, 13, 17),  # poignet
    ]
    mask = _mask((16, 18), [], rects)
    image = _shade(mask, lambda x, y: (GOLD if y == 16 else GOLD_DARK) if y >= 16 and 5 < x < 13 else None)
    # Les doigts repliés se séparent par un trait d'ombre, l'ongle de l'index par un point clair.
    for x, top in ((8, 6), (10, 7), (12, 8)):
        for y in range(top, 10):
            image.putpixel((x, y), DARK)
    image.putpixel((6, 1), LIGHT)
    return image


def assets() -> dict[str, Image.Image]:
    return {"cursor_arrow": arrow(), "cursor_hand": hand()}
