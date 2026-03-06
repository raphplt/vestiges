#!/usr/bin/env python3
"""Generate isometric pixel art tiles for Forêt Reconquise biome."""

from PIL import Image
from pathlib import Path

PALETTE = {
    'vert_canopee_sombre': (0x2D, 0x5A, 0x27),
    'vert_mousse': (0x4A, 0x8C, 0x3F),
    'vert_clair': (0x7B, 0xC5, 0x58),
    'vert_jaune': (0xA4, 0xD6, 0x5E),
    'brun_tronc_fonce': (0x4A, 0x37, 0x28),
    'brun_terreux': (0x7A, 0x5C, 0x42),
    'brun_clair': (0xA6, 0x8B, 0x6B),
    'ocre_automne': (0xC4, 0x9B, 0x3E),
    'gris_beton': (0x7A, 0x7A, 0x70),
    'lierre_sombre': (0x1E, 0x3A, 0x1A),
    'fleur_violette': (0x8B, 0x6B, 0xAE),
    'fleur_jaune': (0xE0, 0xC8, 0x4A),
    'noir_profond': (0x1A, 0x1A, 0x2E),
    'gris_chaud_fonce': (0x3A, 0x35, 0x35),
    'gris_chaud': (0x6B, 0x61, 0x61),
    'blanc_casse': (0xE8, 0xE0, 0xD4),
}

OUTPUT_DIR = Path('/sessions/epic-inspiring-hamilton/mnt/vestiges/assets/tiles/foret')
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)


def iso_diamond_coords(w, h):
    """Get list of (x, y) coordinates that form isometric diamond"""
    coords = set()
    mid_y = h // 2
    for y in range(h):
        if y <= mid_y:
            half_width = int((y / mid_y) * (w / 2))
        else:
            half_width = int(((h - 1 - y) / mid_y) * (w / 2))
        x_start = max(0, w // 2 - half_width)
        x_end = min(w, w // 2 + half_width)
        for x in range(x_start, x_end):
            coords.add((x, y))
    return coords


def create_ground_tile(name, fill_fn, width=32, height=16):
    """Create ground tile with diamond shape"""
    img = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    pixels = img.load()

    diamond = iso_diamond_coords(width, height)
    for x, y in diamond:
        color = fill_fn(x, y, width, height)
        if color:
            pixels[x, y] = color + (255,)

    img.save(OUTPUT_DIR / f'{name}.png')


def grass_1_pattern(x, y, w, h):
    base_idx = (x // 4 + y // 3) % 3
    colors = [PALETTE['vert_mousse'], PALETTE['vert_clair'], PALETTE['vert_canopee_sombre']]
    return colors[base_idx]


def grass_2_pattern(x, y, w, h):
    base_idx = (x // 3 + y // 4) % 4
    if base_idx <= 2:
        return PALETTE['vert_canopee_sombre']
    else:
        return PALETTE['vert_mousse']


def grass_3_pattern(x, y, w, h):
    base_idx = (x // 5 + y // 3) % 5
    if base_idx == 0:
        return PALETTE['vert_mousse']
    elif base_idx == 1:
        return PALETTE['vert_clair']
    elif base_idx == 2:
        return PALETTE['vert_canopee_sombre']
    elif base_idx == 3 and (x % 7 == 2 and y % 6 == 3):
        return PALETTE['fleur_jaune']
    elif base_idx == 4 and (x % 9 == 4 and y % 7 == 2):
        return PALETTE['fleur_violette']
    else:
        return PALETTE['vert_mousse']


def dirt_1_pattern(x, y, w, h):
    base_idx = (x // 4 + y // 3) % 4
    if base_idx <= 1:
        return PALETTE['brun_terreux']
    elif base_idx == 2:
        return PALETTE['brun_tronc_fonce']
    else:
        return PALETTE['vert_mousse']


def dirt_2_pattern(x, y, w, h):
    base_idx = (x // 5 + y // 4) % 5
    if base_idx <= 3:
        return PALETTE['brun_terreux']
    else:
        return PALETTE['brun_clair']


def path_straight_pattern(x, y, w, h):
    mid_x = w // 2
    dist = abs(x - mid_x)
    if dist < 8:
        return PALETTE['brun_clair'] if (x + y) % 3 == 0 else PALETTE['brun_terreux']
    else:
        return PALETTE['vert_mousse']


def path_curve_pattern(x, y, w, h):
    diag = x + (y * 0.5)
    if 8 < diag < 16:
        return PALETTE['brun_clair'] if (x + y) % 3 == 1 else PALETTE['brun_terreux']
    else:
        return PALETTE['vert_mousse']


def highway_1_pattern(x, y, w, h):
    if (x // 8 + y // 6) % 2 == 0:
        base = PALETTE['gris_beton']
    else:
        base = PALETTE['gris_chaud']

    if x % 8 == 0 or y % 6 == 0:
        return PALETTE['gris_chaud_fonce']
    if (x + y) % 5 == 0:
        return PALETTE['vert_mousse']
    elif (x + y) % 7 == 1:
        return PALETTE['vert_clair']

    return base


def highway_2_pattern(x, y, w, h):
    idx = (x // 3 + y // 3) % 7
    if idx <= 2:
        return [PALETTE['vert_mousse'], PALETTE['vert_clair'], PALETTE['vert_canopee_sombre']][idx]
    elif idx == 3:
        return PALETTE['gris_chaud_fonce']
    elif idx <= 5:
        return PALETTE['gris_beton']
    else:
        return PALETTE['vert_mousse']


def create_sprite(name, w, h, draw_fn):
    """Create tall sprite (tree, wall, etc)"""
    img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    pixels = img.load()
    draw_fn(pixels, w, h)
    img.save(OUTPUT_DIR / f'{name}.png')


def draw_large_tree_1(pixels, w, h):
    """Large tree 1"""
    trunk_x = w // 2

    # Trunk
    for y in range(24, h):
        for x in range(trunk_x - 2, trunk_x + 3):
            if 0 <= x < w:
                color = PALETTE['brun_tronc_fonce'] if y < 28 else PALETTE['brun_terreux']
                pixels[x, y] = color + (255,)

    # Roots
    for dy in range(2):
        for dx in [-3, -1, 1, 3]:
            if 0 <= trunk_x + dx < w:
                pixels[trunk_x + dx, h - 1 - dy] = PALETTE['brun_tronc_fonce'] + (255,)

    # Leaves
    leaf_pos = [
        (8, 10), (8, 12), (8, 14), (8, 16),
        (10, 8), (10, 10), (10, 12), (10, 14), (10, 16), (10, 18),
        (12, 8), (12, 10), (12, 12), (12, 14), (12, 16), (12, 18), (12, 20),
        (14, 10), (14, 12), (14, 14), (14, 16),
        (16, 12), (16, 14),
        (18, 12), (18, 14), (18, 16),
        (20, 10), (20, 12), (20, 14), (20, 16), (20, 18),
        (22, 8), (22, 10), (22, 12), (22, 14), (22, 16), (22, 18), (22, 20),
        (24, 10), (24, 12), (24, 14), (24, 16),
    ]

    for x, y in leaf_pos:
        if 0 <= x < w and 0 <= y < h:
            shade = (x + y) % 3
            colors = [PALETTE['vert_canopee_sombre'], PALETTE['vert_mousse'], PALETTE['vert_clair']]
            pixels[x, y] = colors[shade] + (255,)
            if (x + y) % 7 == 0 and 0 <= y - 1 < h:
                pixels[x, y - 1] = PALETTE['vert_jaune'] + (255,)


def draw_large_tree_2(pixels, w, h):
    """Large tree 2"""
    trunk_x = w // 2 + 1

    for y in range(22, h):
        for x in range(trunk_x - 2, trunk_x + 3):
            if 0 <= x < w:
                color = PALETTE['brun_tronc_fonce'] if y < 26 else PALETTE['brun_terreux']
                pixels[x, y] = color + (255,)

    leaf_pos = [
        (6, 12), (6, 14), (6, 16),
        (8, 10), (8, 12), (8, 14), (8, 16), (8, 18),
        (10, 8), (10, 10), (10, 12), (10, 14), (10, 16), (10, 18), (10, 20),
        (12, 10), (12, 12), (12, 14), (12, 16), (12, 18), (12, 20),
        (14, 12), (14, 14), (14, 16), (14, 18),
        (18, 14), (18, 16),
        (20, 12), (20, 14), (20, 16), (20, 18),
        (22, 14), (22, 16), (22, 18),
        (24, 16),
    ]

    for x, y in leaf_pos:
        if 0 <= x < w and 0 <= y < h:
            shade = (x * 3 + y * 2) % 4
            colors = [PALETTE['vert_canopee_sombre'], PALETTE['vert_mousse'], PALETTE['vert_clair'], PALETTE['vert_jaune']]
            pixels[x, y] = colors[shade] + (255,)


def draw_medium_tree_1(pixels, w, h):
    trunk_x = w // 2
    for y in range(16, h):
        for x in range(trunk_x - 1, trunk_x + 2):
            if 0 <= x < w:
                pixels[x, y] = PALETTE['brun_tronc_fonce'] + (255,)

    leaf_pos = [
        (5, 12), (5, 14),
        (7, 10), (7, 12), (7, 14), (7, 16),
        (9, 8), (9, 10), (9, 12), (9, 14), (9, 16),
        (11, 10), (11, 12), (11, 14), (11, 16),
        (13, 12), (13, 14),
    ]

    for x, y in leaf_pos:
        if 0 <= x < w and 0 <= y < h:
            shade = (x + y) % 3
            colors = [PALETTE['vert_canopee_sombre'], PALETTE['vert_mousse'], PALETTE['vert_clair']]
            pixels[x, y] = colors[shade] + (255,)


def draw_medium_tree_2(pixels, w, h):
    trunk_x = w // 2 - 1
    for y in range(14, h):
        for x in range(trunk_x - 1, trunk_x + 2):
            if 0 <= x < w:
                pixels[x, y] = PALETTE['brun_tronc_fonce'] + (255,)

    leaf_pos = [
        (4, 14), (4, 16),
        (6, 12), (6, 14), (6, 16),
        (8, 10), (8, 12), (8, 14), (8, 16),
        (10, 12), (10, 14), (10, 16), (10, 18),
        (12, 14), (12, 16),
    ]

    for x, y in leaf_pos:
        if 0 <= x < w and 0 <= y < h:
            color = PALETTE['vert_clair'] if (x + y) % 2 == 0 else PALETTE['vert_mousse']
            pixels[x, y] = color + (255,)


def draw_medium_tree_3(pixels, w, h):
    trunk_x = w // 2
    for y in range(16, h):
        for x in range(trunk_x - 1, trunk_x + 2):
            if 0 <= x < w:
                pixels[x, y] = PALETTE['brun_tronc_fonce'] + (255,)

    leaf_pos = [
        (5, 12), (5, 14),
        (7, 10), (7, 12), (7, 14), (7, 16),
        (9, 8), (9, 10), (9, 12), (9, 14), (9, 16),
        (11, 10), (11, 12), (11, 14), (11, 16),
        (13, 12), (13, 14),
    ]

    for x, y in leaf_pos:
        if 0 <= x < w and 0 <= y < h:
            if (x * y) % 5 == 0:
                color = PALETTE['ocre_automne']
            elif (x + y) % 3 == 0:
                color = PALETTE['vert_canopee_sombre']
            elif (x + y) % 3 == 1:
                color = PALETTE['vert_mousse']
            else:
                color = PALETTE['vert_clair']
            pixels[x, y] = color + (255,)


def draw_bush_1(pixels, w, h):
    bush_pos = [
        (6, 4), (6, 6), (6, 8),
        (8, 2), (8, 4), (8, 6), (8, 8), (8, 10),
        (10, 4), (10, 6), (10, 8),
    ]

    for x, y in bush_pos:
        if 0 <= x < w and 0 <= y < h:
            color = PALETTE['vert_mousse'] if (x + y) % 2 == 0 else PALETTE['vert_clair']
            pixels[x, y] = color + (255,)


def draw_bush_2(pixels, w, h):
    bush_pos = [
        (6, 4), (6, 6), (6, 8),
        (8, 2), (8, 4), (8, 6), (8, 8), (8, 10),
        (10, 4), (10, 6), (10, 8),
    ]

    for x, y in bush_pos:
        if 0 <= x < w and 0 <= y < h:
            color = PALETTE['vert_mousse'] if (x + y) % 2 == 0 else PALETTE['vert_clair']
            pixels[x, y] = color + (255,)

    if 0 <= 7 < w and 0 <= 5 < h:
        pixels[7, 5] = (0xC4, 0x43, 0x2B, 255)
    if 0 <= 9 < w and 0 <= 7 < h:
        pixels[9, 7] = (0xC4, 0x43, 0x2B, 255)


def draw_bush_3(pixels, w, h):
    bush_pos = [
        (5, 5), (5, 6), (5, 7),
        (7, 4), (7, 5), (7, 6), (7, 7), (7, 8),
        (9, 4), (9, 5), (9, 6), (9, 7), (9, 8),
        (11, 5), (11, 6), (11, 7),
    ]

    for x, y in bush_pos:
        if 0 <= x < w and 0 <= y < h:
            color = PALETTE['vert_clair'] if (x * y) % 3 == 0 else PALETTE['vert_mousse']
            pixels[x, y] = color + (255,)


def draw_rock_1(pixels, w, h):
    rock_pos = [
        (6, 5), (6, 6), (6, 7),
        (8, 4), (8, 5), (8, 6), (8, 7), (8, 8),
        (10, 6), (10, 7),
    ]

    for x, y in rock_pos:
        if 0 <= x < w and 0 <= y < h:
            color = PALETTE['gris_chaud'] if (x + y) % 2 == 0 else PALETTE['gris_chaud_fonce']
            pixels[x, y] = color + (255,)

    for x, y in [(7, 6), (9, 7), (8, 8)]:
        if 0 <= x < w and 0 <= y < h:
            pixels[x, y] = PALETTE['vert_mousse'] + (255,)


def draw_rock_2(pixels, w, h):
    rock_pos = [
        (7, 5), (7, 6), (7, 7),
        (9, 4), (9, 5), (9, 6), (9, 7), (9, 8),
        (11, 5), (11, 6), (11, 7),
    ]

    for x, y in rock_pos:
        if 0 <= x < w and 0 <= y < h:
            color = PALETTE['gris_chaud'] if (x * 2 + y) % 3 == 0 else PALETTE['gris_chaud_fonce']
            pixels[x, y] = color + (255,)

    for x, y in [(8, 5), (8, 7), (9, 6), (10, 5), (10, 7)]:
        if 0 <= x < w and 0 <= y < h:
            pixels[x, y] = PALETTE['vert_canopee_sombre'] + (255,)


def draw_ruin_wall(pixels, w, h):
    # Bottom concrete
    for y in range(16, h):
        for x in range(w):
            if (x // 4 + y // 4) % 2 == 0:
                color = PALETTE['gris_beton']
            else:
                color = PALETTE['gris_chaud_fonce']

            if (x + y) % 5 == 0:
                color = PALETTE['gris_chaud_fonce']

            pixels[x, y] = color + (255,)

    # Cracks
    for y in range(20, h):
        if (y % 6) == 0:
            if 0 <= w // 2 - 1 < w:
                pixels[w // 2 - 1, y] = PALETTE['noir_profond'] + (255,)
            if 0 <= w // 2 + 1 < w:
                pixels[w // 2 + 1, y] = PALETTE['noir_profond'] + (255,)

    # Top wall
    for y in range(4, 16):
        for x in range(w):
            if (x // 8 + y // 4) % 2 == 0:
                pixels[x, y] = PALETTE['gris_chaud'] + (255,)
            else:
                pixels[x, y] = PALETTE['gris_beton'] + (255,)

    # Ivy
    ivy_pos = [
        (4, 12), (4, 14), (4, 16), (4, 18), (4, 20),
        (6, 10), (6, 12), (6, 14), (6, 16), (6, 18),
        (8, 8), (8, 10), (8, 12), (8, 14),
        (24, 14), (24, 16), (24, 18), (24, 20),
        (26, 10), (26, 12), (26, 14), (26, 16),
        (28, 12), (28, 14),
    ]

    for x, y in ivy_pos:
        if 0 <= x < w and 0 <= y < h:
            color = PALETTE['lierre_sombre'] if (x + y) % 2 == 0 else PALETTE['vert_canopee_sombre']
            pixels[x, y] = color + (255,)


def create_preview_sheet():
    """Create preview sheet"""
    tiles = [
        ('tile_foret_herbe_1.png', 32, 16),
        ('tile_foret_herbe_2.png', 32, 16),
        ('tile_foret_herbe_3.png', 32, 16),
        ('tile_foret_terre_1.png', 32, 16),
        ('tile_foret_terre_2.png', 32, 16),
        ('tile_foret_chemin_droit.png', 32, 16),
        ('tile_foret_chemin_courbe.png', 32, 16),
        ('tile_foret_autoroute_1.png', 32, 16),
        ('tile_foret_autoroute_2.png', 32, 16),
        ('tile_foret_arbre_grand.png', 32, 48),
        ('tile_foret_arbre_grand_2.png', 32, 48),
        ('tile_foret_arbre_moyen_1.png', 16, 32),
        ('tile_foret_arbre_moyen_2.png', 16, 32),
        ('tile_foret_arbre_moyen_3.png', 16, 32),
        ('tile_foret_buisson_1.png', 16, 12),
        ('tile_foret_buisson_2.png', 16, 12),
        ('tile_foret_buisson_3.png', 16, 12),
        ('tile_foret_rocher_mousse_1.png', 16, 12),
        ('tile_foret_rocher_mousse_2.png', 16, 12),
        ('tile_foret_mur_ruine.png', 32, 32),
    ]

    cols = 5
    rows = (len(tiles) + cols - 1) // cols
    cell_size = 64

    preview = Image.new('RGB', (cols * cell_size, rows * cell_size), (20, 20, 30))

    for idx, (fname, tw, th) in enumerate(tiles):
        row = idx // cols
        col = idx % cols
        path = OUTPUT_DIR / fname

        if path.exists():
            tile = Image.open(path).convert('RGBA')
            x = col * cell_size + (cell_size - tw) // 2
            y = row * cell_size + (cell_size - th) // 2
            preview.paste(tile, (x, y), tile)

    preview.save(OUTPUT_DIR / 'PREVIEW_FORET_TILES.png')


def main():
    print("Generating Forêt Reconquise tiles...")

    # Ground tiles
    tiles_ground = [
        ('tile_foret_herbe_1', grass_1_pattern),
        ('tile_foret_herbe_2', grass_2_pattern),
        ('tile_foret_herbe_3', grass_3_pattern),
        ('tile_foret_terre_1', dirt_1_pattern),
        ('tile_foret_terre_2', dirt_2_pattern),
        ('tile_foret_chemin_droit', path_straight_pattern),
        ('tile_foret_chemin_courbe', path_curve_pattern),
        ('tile_foret_autoroute_1', highway_1_pattern),
        ('tile_foret_autoroute_2', highway_2_pattern),
    ]

    for name, pattern in tiles_ground:
        print(f"  - {name}.png (32x16)")
        create_ground_tile(name, pattern)

    # Tall tiles
    tiles_tall = [
        ('tile_foret_arbre_grand', 32, 48, draw_large_tree_1),
        ('tile_foret_arbre_grand_2', 32, 48, draw_large_tree_2),
        ('tile_foret_arbre_moyen_1', 16, 32, draw_medium_tree_1),
        ('tile_foret_arbre_moyen_2', 16, 32, draw_medium_tree_2),
        ('tile_foret_arbre_moyen_3', 16, 32, draw_medium_tree_3),
        ('tile_foret_buisson_1', 16, 12, draw_bush_1),
        ('tile_foret_buisson_2', 16, 12, draw_bush_2),
        ('tile_foret_buisson_3', 16, 12, draw_bush_3),
        ('tile_foret_rocher_mousse_1', 16, 12, draw_rock_1),
        ('tile_foret_rocher_mousse_2', 16, 12, draw_rock_2),
        ('tile_foret_mur_ruine', 32, 32, draw_ruin_wall),
    ]

    for name, w, h, fn in tiles_tall:
        print(f"  - {name}.png ({w}x{h})")
        create_sprite(name, w, h, fn)

    print("\nGenerating preview sheet...")
    create_preview_sheet()

    print(f"\nAll tiles generated successfully in {OUTPUT_DIR}")


if __name__ == '__main__':
    main()
