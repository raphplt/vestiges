"""
Generateur du titre atmospherique "VESTIGES" pour le Hub.
Dissolution, braises du Foyer, brume violette, halo dore.
"""

from PIL import Image, ImageDraw, ImageFont
import random
import math

random.seed(42)

# Palette narrative
GOLD_FOYER = (212, 168, 67, 255)
GOLD_BRIGHT = (230, 200, 80, 255)
GOLD_DIM = (160, 120, 42, 255)
ORANGE_FLAME = (224, 123, 57, 255)
RED_EMBER = (180, 60, 30, 255)
VIOLET_BRUME = (74, 48, 102, 255)
CONTOUR_DARK = (42, 30, 22, 255)
CONTOUR_MID = (62, 44, 32, 255)
HALO_GOLD = (212, 168, 67, 100)
TRANSPARENT = (0, 0, 0, 0)

# Charger la font
FONT_PATH = "assets/fonts/pixel-operator/PixelOperator-Bold.ttf"
font = ImageFont.truetype(FONT_PATH, 16)

# Mesurer le texte
text = "VESTIGES"
dummy = Image.new("RGBA", (1, 1))
dd = ImageDraw.Draw(dummy)
bbox = dd.textbbox((0, 0), text, font=font)
text_w = bbox[2] - bbox[0]
text_h = bbox[3] - bbox[1]

# Canvas avec marge pour effets
MARGIN_X = 14
MARGIN_TOP = 12
MARGIN_BOTTOM = 16
W = text_w + MARGIN_X * 2
H = text_h + MARGIN_TOP + MARGIN_BOTTOM

img = Image.new("RGBA", (W, H), TRANSPARENT)
draw = ImageDraw.Draw(img)
px = img.load()

# Render texte en gold
text_x = MARGIN_X - bbox[0]
text_y = MARGIN_TOP - bbox[1]
draw.text((text_x, text_y), text, fill=GOLD_FOYER, font=font)

# Collecter les pixels du texte
text_pixels = set()
for y in range(H):
    for x in range(W):
        if px[x, y][3] > 128:
            text_pixels.add((x, y))

# === Contour sel-out (expand 1px, 8-directions, couleur sombre pas noir) ===
contour_pixels = set()
for (tx, ty) in text_pixels:
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            if dx == 0 and dy == 0:
                continue
            nx, ny = tx + dx, ty + dy
            if 0 <= nx < W and 0 <= ny < H and (nx, ny) not in text_pixels:
                contour_pixels.add((nx, ny))

for (cx, cy) in contour_pixels:
    # Varier legerement la couleur du contour pour le sel-out
    noise = (hash((cx * 7 + cy * 13)) % 100) / 100.0
    if noise > 0.6:
        px[cx, cy] = CONTOUR_MID
    else:
        px[cx, cy] = CONTOUR_DARK

# === Dissolution du bas des lettres ===
# Pour chaque colonne de texte, dissoudre les pixels les plus bas
for x in range(W):
    col_pixels = sorted([(x, y) for (px_x, y) in text_pixels if px_x == x], key=lambda p: p[1])
    if not col_pixels:
        continue

    top_y = col_pixels[0][1]
    bot_y = col_pixels[-1][1]
    col_height = bot_y - top_y + 1
    if col_height < 4:
        continue

    # Les 35% inferieurs se dissolvent
    dissolve_start = top_y + int(col_height * 0.65)

    for (_, y) in col_pixels:
        if y < dissolve_start:
            continue

        # Probabilite de dissolution croissante vers le bas
        t = (y - dissolve_start) / max(1, bot_y - dissolve_start)
        chance = t * 0.8

        # Dithering checkerboard a la frontiere
        if t < 0.3:
            if (x + y) % 2 == 0 and random.random() < chance * 1.5:
                px[x, y] = TRANSPARENT
        elif random.random() < chance:
            px[x, y] = TRANSPARENT
            # Pixel epars en dessous (debris de dissolution)
            scatter_y = y + random.randint(2, 5)
            if scatter_y < H and random.random() < 0.3:
                px[x + random.randint(-1, 1) if 0 < x < W - 1 else x, min(scatter_y, H - 1)] = GOLD_DIM

# Nettoyer les contours orphelins (contour sans texte adjacent)
for (cx, cy) in list(contour_pixels):
    has_text_neighbor = False
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            nx, ny = cx + dx, cy + dy
            if 0 <= nx < W and 0 <= ny < H and (nx, ny) in text_pixels and px[nx, ny][3] > 128:
                has_text_neighbor = True
                break
        if has_text_neighbor:
            break
    if not has_text_neighbor and px[cx, cy][3] > 0:
        # Garder comme pixel de dissolution epars ou supprimer
        if random.random() < 0.6:
            px[cx, cy] = TRANSPARENT

# === Braises (etincelles montantes du Foyer) ===
ember_positions = []
for i in range(14):
    ex = random.randint(MARGIN_X, W - MARGIN_X)
    ey = random.randint(2, MARGIN_TOP + 4)
    if px[ex, ey][3] > 0:
        continue  # Ne pas couvrir le texte

    # Taille et couleur aleatoire
    if random.random() > 0.5:
        color = GOLD_BRIGHT
    elif random.random() > 0.3:
        color = ORANGE_FLAME
    else:
        color = RED_EMBER

    px[ex, ey] = color
    ember_positions.append((ex, ey))

    # Quelques braises ont un pixel de trainee en dessous
    if random.random() < 0.4:
        trail_y = ey + 1
        if trail_y < H and px[ex, trail_y][3] == 0:
            c = color
            px[ex, trail_y] = (c[0], c[1], c[2], 120)

# === Brume violette sous la baseline ===
brume_y_start = text_y + text_h + 1
for i in range(18):
    bx = random.randint(MARGIN_X - 3, W - MARGIN_X + 3)
    by = random.randint(brume_y_start, min(H - 1, brume_y_start + 6))
    if 0 <= bx < W and 0 <= by < H and px[bx, by][3] == 0:
        alpha = random.randint(60, 140)
        px[bx, by] = (VIOLET_BRUME[0], VIOLET_BRUME[1], VIOLET_BRUME[2], alpha)

# === Halo dore (corona subtile autour du texte) ===
# Pour chaque pixel texte, poser un halo sur les voisins vides a 2px
halo_done = set()
for (tx, ty) in text_pixels:
    if px[tx, ty][3] < 128:
        continue  # Pixel dissous
    for dx in range(-2, 3):
        for dy in range(-2, 3):
            if abs(dx) <= 1 and abs(dy) <= 1:
                continue  # Contour deja fait, skip
            nx, ny = tx + dx, ty + dy
            if (nx, ny) in halo_done:
                continue
            if 0 <= nx < W and 0 <= ny < H and px[nx, ny][3] == 0:
                halo_done.add((nx, ny))
                # Distance pour moduler l'alpha
                dist = math.sqrt(dx * dx + dy * dy)
                alpha = int(50 * (1.0 - (dist - 1.0) / 2.0))
                if alpha > 0:
                    px[nx, ny] = (GOLD_FOYER[0], GOLD_FOYER[1], GOLD_FOYER[2], alpha)

# === Sauvegarde ===
# 1x
img.save("assets/ui/menus/ui_hub_title_1x.png")

# 4x upscale nearest neighbor pour le Hub 1920x1080
img_4x = img.resize((W * 4, H * 4), Image.NEAREST)
img_4x.save("assets/ui/menus/ui_hub_title.png")

print(f"Done! Title generated:")
print(f"  - assets/ui/menus/ui_hub_title_1x.png ({W}x{H})")
print(f"  - assets/ui/menus/ui_hub_title.png ({W*4}x{H*4})")
