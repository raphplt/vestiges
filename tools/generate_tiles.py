"""
Generate placeholder isometric tiles for Vestiges biomes.
Uses palette colors from .gpl files and mimics the pixel-art dithering
style of existing tiles.

Tiles: 64x32 RGBA, isometric diamond mask, ~1088 non-transparent pixels.
"""

from PIL import Image
import random
import os

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TILES_DIR = os.path.join(BASE_DIR, "assets", "tiles")

W, H = 64, 32


def make_diamond_mask():
    """Build the isometric diamond mask matching existing tiles."""
    mask = [[False] * W for _ in range(H)]
    for y in range(H):
        if y <= H // 2 - 1:
            half_width = (y + 1) * 2
        else:
            half_width = (H - y) * 2
        cx = W // 2
        x_start = cx - half_width
        x_end = cx + half_width - 1
        for x in range(max(0, x_start), min(W, x_end + 1)):
            mask[y][x] = True
    return mask


DIAMOND = make_diamond_mask()


def pick_color(palette, weights):
    """Weighted random color pick from palette."""
    r = random.random()
    cumulative = 0.0
    for color, weight in zip(palette, weights):
        cumulative += weight
        if r <= cumulative:
            return color
    return palette[-1]


def generate_tile(palette, weights, seed=None):
    """Generate a single isometric tile with dithered palette colors."""
    if seed is not None:
        random.seed(seed)
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    data = img.load()
    for y in range(H):
        for x in range(W):
            if DIAMOND[y][x]:
                r, g, b = pick_color(palette, weights)
                data[x, y] = (r, g, b, 255)
    return img


def add_cluster_detail(img, detail_colors, cluster_count, cluster_size_range=(2, 5)):
    """Add small clusters of detail pixels (cracks, grass tufts, crystals, etc.)."""
    data = img.load()
    for _ in range(cluster_count):
        cx = random.randint(4, W - 5)
        cy = random.randint(2, H - 3)
        if not DIAMOND[cy][cx]:
            continue
        color = random.choice(detail_colors)
        size = random.randint(*cluster_size_range)
        for _ in range(size):
            dx = cx + random.randint(-2, 2)
            dy = cy + random.randint(-1, 1)
            if 0 <= dx < W and 0 <= dy < H and DIAMOND[dy][dx]:
                data[dx, dy] = (color[0], color[1], color[2], 255)
    return img


def add_line_detail(img, color, line_count, horizontal_bias=True):
    """Add thin line patterns (cracks, grid lines, rails)."""
    data = img.load()
    for _ in range(line_count):
        if horizontal_bias:
            y = random.randint(2, H - 3)
            x_start = random.randint(4, W // 2)
            length = random.randint(4, 12)
            for dx in range(length):
                x = x_start + dx
                if 0 <= x < W and DIAMOND[y][x]:
                    data[x, y] = (color[0], color[1], color[2], 255)
        else:
            x = random.randint(4, W - 5)
            y_start = random.randint(2, H // 2)
            length = random.randint(2, 6)
            for dy in range(length):
                y = y_start + dy
                if 0 <= y < H and DIAMOND[y][x]:
                    data[x, y] = (color[0], color[1], color[2], 255)
    return img


def save_tile(img, folder, name):
    """Save tile to the appropriate folder."""
    path = os.path.join(TILES_DIR, folder, name + ".png")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)
    print(f"  Created: {folder}/{name}.png")


# =============================================================================
# CARRIERE PALETTE (from vestiges_carriere.gpl)
# =============================================================================
CAR_ROCHE_SOMBRE = (58, 48, 48)
CAR_ROCHE_GRISE = (90, 80, 80)
CAR_ROCHE_CLAIRE = (138, 122, 106)
CAR_TERRE_ROUGE = (106, 58, 40)
CAR_METAL_INDUS = (90, 106, 122)
CAR_ROUILLE = (90, 42, 24)
CAR_CRISTAL_BLEU = (74, 186, 224)
CAR_CRISTAL_VIF = (122, 224, 240)
CAR_BOIS_MINE = (106, 80, 56)
CAR_CHARBON = (42, 34, 34)
CAR_POUSSIERE = (180, 160, 128)
CAR_OMBRE = (26, 18, 24)

# =============================================================================
# CHAMPS PALETTE (from vestiges_champs.gpl)
# =============================================================================
CHA_HERBE_SOMBRE = (58, 106, 48)
CHA_HERBE_VIVE = (90, 168, 72)
CHA_HERBE_DOREE = (168, 180, 88)
CHA_HERBE_PALE = (200, 212, 128)
CHA_TERRE_SECHE = (138, 112, 88)
CHA_TERRE_CLAIRE = (184, 160, 128)
CHA_PIERRE = (122, 122, 106)
CHA_FLEUR_ROUGE = (196, 74, 58)
CHA_FLEUR_BLEUE = (90, 122, 202)
CHA_CIEL_REFLET = (138, 176, 208)
CHA_CLOTURE_BOIS = (106, 90, 66)
CHA_VENT = (208, 216, 208)


def generate_carriere_tiles():
    """Generate all carriere (collapsed quarry) tile variants."""
    print("=== CARRIERE ===")

    # --- Roche (rocky ground) ---
    roche_pal = [CAR_ROCHE_SOMBRE, CAR_ROCHE_GRISE, CAR_ROCHE_CLAIRE, CAR_TERRE_ROUGE, CAR_POUSSIERE, CAR_CHARBON]
    roche_w = [0.60, 0.16, 0.08, 0.08, 0.04, 0.04]

    for i, suffix in enumerate(["base", "v2", "v3"]):
        img = generate_tile(roche_pal, roche_w, seed=100 + i)
        add_line_detail(img, CAR_CHARBON, 3, horizontal_bias=True)
        add_cluster_detail(img, [CAR_TERRE_ROUGE, CAR_POUSSIERE], 4, (2, 4))
        save_tile(img, "carriere", f"tile_carriere_roche_{suffix}")

    # --- Industriel (industrial floor with metal/rust) ---
    indus_pal = [CAR_METAL_INDUS, CAR_ROCHE_GRISE, CAR_ROUILLE, CAR_ROCHE_SOMBRE, CAR_POUSSIERE, CAR_CHARBON]
    indus_w = [0.35, 0.22, 0.18, 0.12, 0.08, 0.05]

    for i, suffix in enumerate(["base", "v2"]):
        img = generate_tile(indus_pal, indus_w, seed=200 + i)
        add_line_detail(img, CAR_ROUILLE, 4, horizontal_bias=True)
        add_line_detail(img, CAR_CHARBON, 2, horizontal_bias=False)
        add_cluster_detail(img, [CAR_ROUILLE, CAR_POUSSIERE], 3, (2, 4))
        save_tile(img, "carriere", f"tile_carriere_industriel_{suffix}")

    # --- Tunnel (dark, enclosed) ---
    tunnel_pal = [CAR_OMBRE, CAR_CHARBON, CAR_ROCHE_SOMBRE, CAR_ROCHE_GRISE, CAR_BOIS_MINE, CAR_ROCHE_CLAIRE]
    tunnel_w = [0.30, 0.25, 0.22, 0.10, 0.08, 0.05]

    for i, suffix in enumerate(["base", "v2"]):
        img = generate_tile(tunnel_pal, tunnel_w, seed=300 + i)
        add_line_detail(img, CAR_BOIS_MINE, 3, horizontal_bias=False)
        add_cluster_detail(img, [CAR_ROCHE_GRISE, CAR_BOIS_MINE], 3, (1, 3))
        save_tile(img, "carriere", f"tile_carriere_tunnel_{suffix}")

    # --- Cristal (crystal-veined ground) ---
    cristal_pal = [CAR_ROCHE_SOMBRE, CAR_ROCHE_GRISE, CAR_CHARBON, CAR_OMBRE, CAR_CRISTAL_BLEU, CAR_CRISTAL_VIF]
    cristal_w = [0.38, 0.20, 0.15, 0.10, 0.12, 0.05]

    img = generate_tile(cristal_pal, cristal_w, seed=400)
    add_cluster_detail(img, [CAR_CRISTAL_BLEU, CAR_CRISTAL_VIF], 6, (3, 6))
    add_line_detail(img, CAR_CRISTAL_BLEU, 3, horizontal_bias=True)
    add_line_detail(img, CAR_CRISTAL_VIF, 2, horizontal_bias=False)
    save_tile(img, "carriere", "tile_carriere_cristal_base")


def generate_champs_tiles():
    """Generate all champs (wild fields) tile variants."""
    print("=== CHAMPS ===")

    # --- Herbe (grass ground) ---
    herbe_pal = [CHA_HERBE_VIVE, CHA_HERBE_SOMBRE, CHA_HERBE_DOREE, CHA_HERBE_PALE, CHA_TERRE_SECHE, CHA_TERRE_CLAIRE]
    herbe_w = [0.55, 0.18, 0.12, 0.06, 0.05, 0.04]

    for i, suffix in enumerate(["base", "v2", "v3"]):
        img = generate_tile(herbe_pal, herbe_w, seed=500 + i)
        add_cluster_detail(img, [CHA_HERBE_DOREE, CHA_HERBE_PALE], 5, (2, 4))
        if i == 1:
            add_cluster_detail(img, [CHA_FLEUR_ROUGE], 2, (1, 2))
        elif i == 2:
            add_cluster_detail(img, [CHA_FLEUR_BLEUE], 2, (1, 2))
        save_tile(img, "champs", f"tile_champs_herbe_{suffix}")

    # --- Ble (wheat field) ---
    ble_pal = [CHA_HERBE_DOREE, CHA_HERBE_PALE, CHA_HERBE_VIVE, CHA_HERBE_SOMBRE, CHA_TERRE_SECHE, CHA_TERRE_CLAIRE]
    ble_w = [0.40, 0.25, 0.15, 0.08, 0.07, 0.05]

    for i, suffix in enumerate(["base", "v2"]):
        img = generate_tile(ble_pal, ble_w, seed=600 + i)
        add_line_detail(img, CHA_HERBE_PALE, 5, horizontal_bias=False)
        add_cluster_detail(img, [CHA_HERBE_DOREE, CHA_HERBE_PALE, CHA_VENT], 4, (1, 3))
        save_tile(img, "champs", f"tile_champs_ble_{suffix}")

    # --- Chemin (dirt path) ---
    chemin_pal = [CHA_TERRE_SECHE, CHA_TERRE_CLAIRE, CHA_PIERRE, CHA_HERBE_SOMBRE, CHA_HERBE_VIVE, CHA_CLOTURE_BOIS]
    chemin_w = [0.38, 0.25, 0.15, 0.10, 0.07, 0.05]

    for i, suffix in enumerate(["base", "v2"]):
        img = generate_tile(chemin_pal, chemin_w, seed=700 + i)
        add_cluster_detail(img, [CHA_PIERRE, CHA_TERRE_CLAIRE], 4, (1, 3))
        add_cluster_detail(img, [CHA_HERBE_SOMBRE, CHA_HERBE_VIVE], 3, (1, 2))
        save_tile(img, "champs", f"tile_champs_chemin_{suffix}")

    # --- Bosquet (grove/thicket) ---
    bosquet_pal = [CHA_HERBE_SOMBRE, CHA_HERBE_VIVE, CHA_HERBE_DOREE, CHA_CLOTURE_BOIS, CHA_TERRE_SECHE, CHA_HERBE_PALE]
    bosquet_w = [0.35, 0.28, 0.14, 0.10, 0.08, 0.05]

    for i, suffix in enumerate(["base", "v2"]):
        img = generate_tile(bosquet_pal, bosquet_w, seed=800 + i)
        add_cluster_detail(img, [CHA_CLOTURE_BOIS, CHA_TERRE_SECHE], 5, (3, 6))
        add_cluster_detail(img, [CHA_HERBE_SOMBRE], 4, (2, 4))
        save_tile(img, "champs", f"tile_champs_bosquet_{suffix}")


if __name__ == "__main__":
    print(f"Generating tiles in: {TILES_DIR}")
    print()
    generate_carriere_tiles()
    print()
    generate_champs_tiles()
    print()
    print("Done!")
