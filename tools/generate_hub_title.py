"""Titre « VESTIGES » de l'accueil, en pixels natifs (affiché ×4, au grain du fond du Hub).

Glyphes dessinés à la main, biseau doré éclairé en haut à gauche, contour sel-out sombre.
Les trois dernières lettres s'effacent : pixels arrachés vers le haut-droite, blanchis vers le blanc d'effacement.

Usage : python3 tools/generate_hub_title.py [sortie.png]
"""

import random
import sys

from PIL import Image

OUTPUT = sys.argv[1] if len(sys.argv) > 1 else "assets/ui/menus/ui_hub_title.png"

GLYPHS = {
    "V": [
        "xxx....xxx",
        "xxx....xxx",
        "xxx....xxx",
        "xxx....xxx",
        "xxx....xxx",
        "xxx....xxx",
        ".xxx..xxx.",
        ".xxx..xxx.",
        ".xxx..xxx.",
        "..xxxxxx..",
        "..xxxxxx..",
        "...xxxx...",
        "...xxxx...",
        "....xx....",
    ],
    "E": [
        "xxxxxxxxxx",
        "xxxxxxxxxx",
        "xxx.......",
        "xxx.......",
        "xxx.......",
        "xxx.......",
        "xxxxxxxx..",
        "xxxxxxxx..",
        "xxx.......",
        "xxx.......",
        "xxx.......",
        "xxx.......",
        "xxxxxxxxxx",
        "xxxxxxxxxx",
    ],
    "S": [
        "..xxxxxxx.",
        ".xxxxxxxxx",
        "xxx.....xx",
        "xxx.......",
        "xxx.......",
        ".xxxxxx...",
        "..xxxxxxx.",
        "....xxxxxx",
        ".......xxx",
        ".......xxx",
        ".......xxx",
        "xx.....xxx",
        "xxxxxxxxx.",
        ".xxxxxxx..",
    ],
    "T": [
        "xxxxxxxxxx",
        "xxxxxxxxxx",
        "...xxxx...",
        "...xxxx...",
        "...xxxx...",
        "...xxxx...",
        "...xxxx...",
        "...xxxx...",
        "...xxxx...",
        "...xxxx...",
        "...xxxx...",
        "...xxxx...",
        "...xxxx...",
        "...xxxx...",
    ],
    "I": [
        "xxxxxx",
        "xxxxxx",
        ".xxxx.",
        ".xxxx.",
        ".xxxx.",
        ".xxxx.",
        ".xxxx.",
        ".xxxx.",
        ".xxxx.",
        ".xxxx.",
        ".xxxx.",
        ".xxxx.",
        "xxxxxx",
        "xxxxxx",
    ],
    "G": [
        "..xxxxxxx.",
        ".xxxxxxxxx",
        "xxx.....xx",
        "xxx.......",
        "xxx.......",
        "xxx.......",
        "xxx..xxxxx",
        "xxx..xxxxx",
        "xxx....xxx",
        "xxx....xxx",
        "xxx....xxx",
        "xxx....xxx",
        ".xxxxxxxxx",
        "..xxxxxxx.",
    ],
}

TEXT = "VESTIGES"
SPACING = 3
MARGIN_X, MARGIN_TOP, MARGIN_BOTTOM = 4, 12, 4
EROSION_START = 5  # indice de la première lettre qui s'efface (G)

HIGHLIGHT = (244, 222, 150)
GOLD = (212, 168, 67)
GOLD_SHADE = (160, 118, 42)
GOLD_DEEP = (112, 78, 32)
OUTLINE = (42, 30, 22)
SHADOW = (26, 26, 46)
ERASURE = (245, 240, 235)


def lerp(a, b, t):
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3))


def main():
    random.seed(1407)
    glyph_h = len(GLYPHS["V"])
    width = sum(len(GLYPHS[c][0]) for c in TEXT) + SPACING * (len(TEXT) - 1)
    canvas_w = width + MARGIN_X * 2 + 10
    canvas_h = glyph_h + MARGIN_TOP + MARGIN_BOTTOM

    solid = set()
    erosion_x0 = None
    cursor = MARGIN_X
    for index, char in enumerate(TEXT):
        if index == EROSION_START:
            erosion_x0 = cursor
        rows = GLYPHS[char]
        for y, row in enumerate(rows):
            for x, cell in enumerate(row):
                if cell == "x":
                    solid.add((cursor + x, MARGIN_TOP + y))
        cursor += len(rows[0]) + SPACING
    erosion_x1 = cursor - SPACING

    # Effacement : plus on va à droite, plus les pixels s'arrachent et dérivent.
    flakes = []
    kept = set()
    for (x, y) in sorted(solid):
        t = max(0.0, (x - erosion_x0) / (erosion_x1 - erosion_x0)) if x >= erosion_x0 else 0.0
        if t > 0 and random.random() < 0.06 + 0.52 * t ** 1.6:
            if random.random() < 0.55:
                drift = 1 + int(random.random() * (3 + 9 * t))
                flakes.append((x + drift, y - int(drift * random.uniform(0.4, 1.1)), t))
            continue
        kept.add((x, y))

    image = Image.new("RGBA", (canvas_w, canvas_h), (0, 0, 0, 0))
    px = image.load()

    def put(x, y, color, alpha=255):
        if 0 <= x < canvas_w and 0 <= y < canvas_h:
            px[x, y] = (*color, alpha)

    for (x, y) in kept:
        put(x + 1, y + 2, SHADOW, 150)
    for (x, y) in kept:
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                if (x + dx, y + dy) not in kept:
                    put(x + dx, y + dy, OUTLINE)
    for (x, y) in kept:
        above = (x, y - 1) in kept
        below = (x, y + 1) in kept
        left = (x - 1, y) in kept
        if not above:
            color = HIGHLIGHT
        elif not below:
            color = GOLD_DEEP
        elif not (x, y + 2) in kept:
            color = GOLD_SHADE
        elif not left:
            color = lerp(GOLD, HIGHLIGHT, 0.35)
        else:
            color = GOLD
        t = max(0.0, (x - erosion_x0) / (erosion_x1 - erosion_x0)) if x >= erosion_x0 else 0.0
        put(x, y, lerp(color, ERASURE, min(0.85, t ** 1.3 * 0.9)))
    for (x, y, t) in flakes:
        if (x, y) in kept:
            continue
        color = lerp(GOLD, ERASURE, min(1.0, 0.3 + t))
        put(x, y, color, int(255 * (0.95 - 0.45 * t)))

    image.save(OUTPUT)
    print(f"{OUTPUT} : {canvas_w}x{canvas_h} px natifs")


if __name__ == "__main__":
    main()
