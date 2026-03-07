#!/usr/bin/env python3
"""Generate isometric pixel art props for Ruines Urbaines biome."""

import math
import random
from PIL import Image
from pathlib import Path

# Seed for reproducibility
random.seed(42)

# ── Palette ──────────────────────────────────────────────────────────────────
PAL = {
    # Biome-specific
    'dark_concrete':    (0x4A, 0x4A, 0x4A),
    'med_concrete':     (0x73, 0x73, 0x73),
    'light_concrete':   (0xA0, 0xA0, 0xA0),
    'dark_rust':        (0x6B, 0x3A, 0x24),
    'orange_rust':      (0xA8, 0x5C, 0x30),
    'oxidized_copper':  (0x5A, 0x9A, 0x8A),
    'faded_brick':      (0x8A, 0x5A, 0x42),
    'peeling_blue':     (0x5A, 0x7A, 0x9A),
    'signage_yellow':   (0xC4, 0xA8, 0x30),
    'reclaim_green':    (0x4A, 0x7A, 0x3A),
    'rotting_wood':     (0x5A, 0x4A, 0x38),
    'broken_glass':     (0x8A, 0xB8, 0xC4),
    # Universal
    'deep_black':       (0x1A, 0x1A, 0x2E),
    'blue_black':       (0x16, 0x21, 0x3E),
    'dark_warm_gray':   (0x3A, 0x35, 0x35),
    'warm_gray':        (0x6B, 0x61, 0x61),
    'light_gray':       (0x9E, 0x94, 0x94),
    'off_white':        (0xE8, 0xE0, 0xD4),
    'erasure_white':    (0xF5, 0xF0, 0xEB),
}

OUTPUT_DIR = Path(__file__).resolve().parent.parent / 'assets' / 'sprites' / 'props' / 'urban_ruins'
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)


# ── Helpers ──────────────────────────────────────────────────────────────────

def sel_out(img):
    """Apply sel-out outline: transparent pixels adjacent to solid get a
    darkened tint of the nearest neighbor color. Never pure black."""
    src = img.copy()
    w, h = img.size
    px_src = src.load()
    px_dst = img.load()
    for y in range(h):
        for x in range(w):
            if px_src[x, y][3] == 0:
                neighbors = []
                for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h and px_src[nx, ny][3] > 0:
                        neighbors.append(px_src[nx, ny])
                if neighbors:
                    base = neighbors[0]
                    outline = (max(8, int(base[0] * 0.35)),
                               max(8, int(base[1] * 0.35)),
                               max(8, int(base[2] * 0.35)), 255)
                    px_dst[x, y] = outline
    return img


def put(px, x, y, color, w, h):
    """Safe putpixel with bounds checking."""
    if 0 <= x < w and 0 <= y < h:
        px[x, y] = color + (255,)


def fill_rect(px, x0, y0, x1, y1, color, w, h):
    """Fill a rectangle with bounds checking."""
    for y in range(max(0, y0), min(h, y1 + 1)):
        for x in range(max(0, x0), min(w, x1 + 1)):
            px[x, y] = color + (255,)


def shade(color, factor):
    """Darken or lighten a color."""
    return tuple(max(0, min(255, int(c * factor))) for c in color)


def pick_concrete(x, y):
    """Deterministic concrete color variation based on position."""
    v = (x * 7 + y * 13) % 5
    if v < 2:
        return PAL['dark_concrete']
    elif v < 4:
        return PAL['med_concrete']
    else:
        return PAL['light_concrete']


def save_prop(name, img):
    """Apply sel-out and save."""
    img = sel_out(img)
    path = OUTPUT_DIR / f'{name}.png'
    img.save(path)
    print(f"  + {name}.png ({img.width}x{img.height})")


# ── Props ────────────────────────────────────────────────────────────────────

def gen_collapsed_building():
    """1. Immeuble effondré fragment (48×36) — partial wall with exposed rooms"""
    W, H = 48, 36
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = img.load()

    # Main wall block (left portion, standing)
    for y in range(4, 34):
        for x in range(2, 30):
            # Irregular top edge
            top_edge = 4 + int(abs(math.sin(x * 0.4)) * 3)
            if y < top_edge:
                continue
            # Right crumble
            right_edge = 30 - max(0, int((y - 20) * 0.5 * abs(math.sin(y * 0.7))))
            if x > right_edge:
                continue
            put(px, x, y, pick_concrete(x, y), W, H)

    # Exposed room interior (darker area, recessed)
    for y in range(8, 20):
        for x in range(6, 18):
            if y == 8 or y == 19:
                put(px, x, y, PAL['dark_concrete'], W, H)
            else:
                put(px, x, y, PAL['dark_warm_gray'], W, H)

    # Wallpaper remnant inside room
    for y in range(10, 18):
        for x in range(7, 12):
            if (y + x) % 3 == 0:
                put(px, x, y, PAL['peeling_blue'], W, H)

    # Rebar sticking out (right side of wall)
    for i in range(3):
        ry = 12 + i * 6
        for rx in range(26, 34):
            if ry < H:
                put(px, rx, ry, PAL['dark_rust'], W, H)
                if rx > 30:
                    put(px, rx, ry - 1, PAL['orange_rust'], W, H)

    # Floor slab (collapsed, lower right)
    for y in range(26, 34):
        for x in range(20, 44):
            angle = (x - 20) * 0.15
            ay = int(y + angle)
            if 0 <= ay < H:
                v = (x + ay) % 4
                c = PAL['med_concrete'] if v < 2 else PAL['dark_concrete']
                put(px, x, ay, c, W, H)

    # Rubble at base
    rubble_spots = [(5, 33), (8, 34), (12, 33), (15, 35), (22, 34),
                    (25, 33), (28, 35), (31, 34), (34, 33)]
    for rx, ry in rubble_spots:
        put(px, rx, ry, PAL['light_concrete'], W, H)
        put(px, rx + 1, ry, PAL['dark_concrete'], W, H)

    # Crack vegetation
    veg_spots = [(4, 30), (10, 32), (16, 33), (24, 32)]
    for vx, vy in veg_spots:
        put(px, vx, vy, PAL['reclaim_green'], W, H)
        put(px, vx + 1, vy - 1, PAL['reclaim_green'], W, H)

    save_prop('prop_collapsed_building', img)


def gen_concrete_wall(variant=1):
    """2. Mur béton debout (32×32) — 3 variants"""
    W, H = 32, 32
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = img.load()

    if variant == 1:
        # Mostly intact
        for y in range(2, 30):
            for x in range(4, 28):
                top = 2 + int(abs(math.sin(x * 0.3)) * 2)
                if y < top:
                    continue
                put(px, x, y, pick_concrete(x, y), W, H)
        # Thin crack
        for y in range(6, 24):
            cx = 14 + int(math.sin(y * 0.5) * 2)
            put(px, cx, y, PAL['dark_warm_gray'], W, H)

    elif variant == 2:
        # Cracked with rebar
        for y in range(4, 30):
            for x in range(3, 29):
                top = 4 + int(abs(math.sin(x * 0.5)) * 3)
                if y < top:
                    continue
                # Chunk missing (right-center)
                if 18 < x < 24 and 10 < y < 18:
                    continue
                put(px, x, y, pick_concrete(x, y), W, H)
        # Rebar in hole
        for y in range(11, 17):
            put(px, 20, y, PAL['dark_rust'], W, H)
            put(px, 22, y, PAL['orange_rust'], W, H)
        # Wider cracks
        for y in range(6, 28):
            cx = 10 + int(math.sin(y * 0.4) * 3)
            put(px, cx, y, PAL['dark_warm_gray'], W, H)

    elif variant == 3:
        # Half-collapsed: left half standing, right half rubble
        for y in range(6, 30):
            for x in range(3, 18):
                top = 6 + int(abs(math.sin(x * 0.6)) * 2)
                if y < top:
                    continue
                put(px, x, y, pick_concrete(x, y), W, H)
        # Collapsed rubble right
        rubble = [(18, 24), (20, 26), (22, 28), (19, 28), (21, 27),
                  (23, 26), (24, 29), (16, 28), (25, 28), (17, 26)]
        for rx, ry in rubble:
            put(px, rx, ry, PAL['med_concrete'], W, H)
            put(px, rx + 1, ry, PAL['light_concrete'], W, H)
        # Rebar at break
        for y in range(20, 28):
            put(px, 17, y, PAL['dark_rust'], W, H)

    # Vegetation in cracks
    for x in range(W):
        if (x * 17) % 11 == 0 and x > 3 and x < 28:
            put(px, x, 29, PAL['reclaim_green'], W, H)

    suffix = '' if variant == 1 else f'_v{variant}'
    save_prop(f'prop_concrete_wall{suffix}', img)


def gen_brick_wall():
    """3. Mur brique exposé (24×28)"""
    W, H = 24, 28
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = img.load()

    for y in range(3, 26):
        for x in range(2, 22):
            # Irregular top
            top = 3 + int(abs(math.sin(x * 0.7)) * 3)
            if y < top:
                continue
            # Brick pattern: alternating offset rows
            brick_y = y % 4
            brick_x = (x + (2 if (y // 4) % 2 else 0)) % 6
            if brick_y == 0 or brick_x == 0:
                # Mortar
                put(px, x, y, PAL['dark_warm_gray'], W, H)
            else:
                # Brick with variation
                v = (x * 3 + y * 7) % 5
                if v < 3:
                    put(px, x, y, PAL['faded_brick'], W, H)
                elif v < 4:
                    put(px, x, y, shade(PAL['faded_brick'], 0.8), W, H)
                else:
                    put(px, x, y, shade(PAL['faded_brick'], 1.2), W, H)

    # Missing bricks (holes)
    holes = [(8, 8), (9, 8), (14, 14), (15, 14), (6, 20), (7, 20)]
    for hx, hy in holes:
        put(px, hx, hy, PAL['dark_warm_gray'], W, H)

    # Some mortar crumbling at bottom
    for x in range(4, 20):
        if (x * 13) % 7 == 0:
            put(px, x, 25, PAL['light_gray'], W, H)

    save_prop('prop_brick_wall', img)


def gen_steel_beam(variant=1):
    """4. Poutrelle acier (32×8) — horizontal and diagonal"""
    W, H = 32, 8
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = img.load()

    if variant == 1:
        # Horizontal I-beam
        # Top flange
        for x in range(2, 30):
            put(px, x, 1, PAL['dark_rust'], W, H)
            put(px, x, 2, PAL['med_concrete'], W, H)  # Use gray for metal look
        # Web (thin middle)
        for x in range(2, 30):
            v = (x * 5) % 4
            c = PAL['dark_rust'] if v < 2 else PAL['orange_rust']
            put(px, x, 3, c, W, H)
            put(px, x, 4, c, W, H)
        # Bottom flange
        for x in range(2, 30):
            put(px, x, 5, PAL['dark_rust'], W, H)
            put(px, x, 6, PAL['warm_gray'], W, H)
        # Rust patches
        for x in [6, 12, 18, 24]:
            put(px, x, 3, PAL['orange_rust'], W, H)
            put(px, x, 4, PAL['orange_rust'], W, H)

    elif variant == 2:
        # Diagonal beam (leaning)
        for i in range(26):
            x = 2 + i
            y = 6 - int(i * 0.2)
            if 0 <= y < H and 0 <= x < W:
                put(px, x, y, PAL['dark_rust'], W, H)
                put(px, x, y + 1, PAL['orange_rust'], W, H)
                if y - 1 >= 0:
                    put(px, x, y - 1, PAL['warm_gray'], W, H)

    suffix = '' if variant == 1 else '_diagonal'
    save_prop(f'prop_steel_beam{suffix}', img)


def gen_concrete_debris(variant=1):
    """5. Débris de béton au sol (16×8) — 3 variants"""
    W, H = 16, 8
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = img.load()

    random.seed(42 + variant * 100)
    chunks = []
    for _ in range(4 + variant):
        cx = random.randint(2, W - 3)
        cy = random.randint(2, H - 2)
        size = random.randint(1, 3)
        chunks.append((cx, cy, size))

    for cx, cy, size in chunks:
        for dy in range(size):
            for dx in range(size):
                v = (dx + dy + variant) % 3
                colors = [PAL['dark_concrete'], PAL['med_concrete'], PAL['light_concrete']]
                put(px, cx + dx, cy + dy, colors[v], W, H)

    # Rebar bits
    if variant >= 2:
        put(px, 5, 3, PAL['dark_rust'], W, H)
        put(px, 6, 3, PAL['orange_rust'], W, H)
    if variant == 3:
        put(px, 10, 5, PAL['dark_rust'], W, H)

    # Dust pixels
    for _ in range(3):
        dx = random.randint(1, W - 2)
        dy = random.randint(1, H - 2)
        put(px, dx, dy, PAL['light_gray'], W, H)

    suffix = f'_v{variant}' if variant > 1 else ''
    save_prop(f'prop_concrete_debris{suffix}', img)


def gen_overturned_desk():
    """6. Bureau renversé (16×12)"""
    W, H = 16, 12
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = img.load()

    # Desk body on its side (rotated rectangle)
    for y in range(3, 10):
        for x in range(2, 14):
            if y < 5:
                put(px, x, y, PAL['rotting_wood'], W, H)
            else:
                put(px, x, y, shade(PAL['rotting_wood'], 0.8), W, H)

    # Desk top (now vertical, visible side)
    for y in range(2, 10):
        put(px, 13, y, shade(PAL['rotting_wood'], 1.1), W, H)
        put(px, 14, y, PAL['rotting_wood'], W, H)

    # Open drawer
    fill_rect(px, 4, 5, 8, 8, PAL['dark_warm_gray'], W, H)
    fill_rect(px, 5, 6, 7, 7, PAL['warm_gray'], W, H)

    # Drawer handle
    put(px, 6, 5, PAL['warm_gray'], W, H)

    # Scattered paper pixels
    put(px, 1, 10, PAL['off_white'], W, H)
    put(px, 3, 11, PAL['off_white'], W, H)
    put(px, 6, 10, PAL['erasure_white'], W, H)
    put(px, 9, 11, PAL['off_white'], W, H)

    # Leg sticking up
    put(px, 3, 2, PAL['warm_gray'], W, H)
    put(px, 3, 1, PAL['warm_gray'], W, H)

    save_prop('prop_overturned_desk', img)


def gen_urban_car():
    """7. Voiture abandonnée urbaine (32×16)"""
    W, H = 32, 16
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = img.load()

    # Car body (isometric-ish, front-left facing)
    # Bottom body
    for y in range(7, 14):
        for x in range(3, 29):
            # Shape: narrower at front and back
            if x < 6 and y < 9:
                continue
            if x > 25 and y < 9:
                continue
            v = (x + y) % 4
            if v < 2:
                put(px, x, y, PAL['dark_concrete'], W, H)
            else:
                put(px, x, y, PAL['med_concrete'], W, H)

    # Roof / top
    for y in range(4, 8):
        for x in range(8, 24):
            if y == 4 and (x < 10 or x > 22):
                continue
            put(px, x, y, PAL['med_concrete'], W, H)

    # Windshield (cracked)
    for y in range(5, 8):
        for x in range(8, 12):
            put(px, x, y, PAL['broken_glass'], W, H)
    # Crack line
    put(px, 9, 5, PAL['light_gray'], W, H)
    put(px, 10, 6, PAL['light_gray'], W, H)
    put(px, 9, 7, PAL['light_gray'], W, H)

    # Rear window
    for y in range(5, 8):
        for x in range(20, 23):
            put(px, x, y, PAL['broken_glass'], W, H)

    # Wheels (flat tires — darker, deflated)
    # Front wheel
    for dy in range(-1, 2):
        for dx in range(-1, 2):
            put(px, 7 + dx, 13 + dy, PAL['dark_warm_gray'], W, H)
    put(px, 7, 13, PAL['warm_gray'], W, H)
    # Rear wheel
    for dy in range(-1, 2):
        for dx in range(-1, 2):
            put(px, 24 + dx, 13 + dy, PAL['dark_warm_gray'], W, H)
    put(px, 24, 13, PAL['warm_gray'], W, H)

    # Rust patches
    rust_spots = [(5, 10), (6, 11), (14, 12), (15, 12), (26, 10), (27, 11)]
    for rx, ry in rust_spots:
        put(px, rx, ry, PAL['orange_rust'], W, H)

    # Dust on hood
    for x in range(10, 20):
        if (x * 7) % 5 == 0:
            put(px, x, 8, PAL['light_gray'], W, H)

    save_prop('prop_urban_car', img)


def gen_traffic_light():
    """8. Feu de signalisation tombé (8×16)"""
    W, H = 8, 16
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = img.load()

    # Pole (bent)
    for y in range(2, 14):
        x = 3 + int(math.sin((y - 2) * 0.2) * 1.5)
        put(px, x, y, PAL['dark_concrete'], W, H)
        put(px, x + 1, y, PAL['warm_gray'], W, H)

    # Light housing (at top, tilted)
    for y in range(1, 7):
        put(px, 4, y, PAL['dark_warm_gray'], W, H)
        put(px, 5, y, PAL['dark_concrete'], W, H)

    # Lights (all dark/broken)
    put(px, 5, 2, PAL['dark_rust'], W, H)   # Red (dark)
    put(px, 5, 4, PAL['dark_warm_gray'], W, H)  # Yellow (dark)
    put(px, 5, 6, PAL['dark_warm_gray'], W, H)  # Green (dark)

    # Cracked lens
    put(px, 6, 2, PAL['orange_rust'], W, H)

    # Base on ground
    put(px, 2, 14, PAL['dark_concrete'], W, H)
    put(px, 3, 14, PAL['dark_concrete'], W, H)
    put(px, 4, 14, PAL['med_concrete'], W, H)
    put(px, 3, 15, PAL['med_concrete'], W, H)

    save_prop('prop_traffic_light', img)


def gen_dumpster(variant=1):
    """9. Poubelle/Conteneur (12×12) — 2 variants"""
    W, H = 12, 12
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = img.load()

    # Main body
    for y in range(3, 11):
        for x in range(1, 11):
            if y < 5:
                c = PAL['reclaim_green'] if variant == 1 else PAL['dark_concrete']
                put(px, x, y, shade(c, 0.7), W, H)
            else:
                c = PAL['reclaim_green'] if variant == 1 else PAL['dark_concrete']
                put(px, x, y, c, W, H)

    # Lid (open, angled)
    if variant == 1:
        for x in range(1, 11):
            put(px, x, 2, PAL['dark_concrete'], W, H)
            put(px, x, 1, PAL['med_concrete'], W, H)
    else:
        # Lid open wider
        for x in range(1, 8):
            put(px, x, 1, PAL['med_concrete'], W, H)
            put(px, x, 0, PAL['warm_gray'], W, H)

    # Dents
    put(px, 4, 7, shade(PAL['dark_concrete'], 0.6), W, H)
    put(px, 5, 7, shade(PAL['dark_concrete'], 0.6), W, H)

    # Rust spots
    put(px, 3, 9, PAL['dark_rust'], W, H)
    put(px, 8, 6, PAL['orange_rust'], W, H)

    # Handle lines
    put(px, 2, 5, PAL['warm_gray'], W, H)
    put(px, 9, 5, PAL['warm_gray'], W, H)

    suffix = '' if variant == 1 else '_v2'
    save_prop(f'prop_dumpster{suffix}', img)


def gen_chain_link_fence():
    """10. Clôture grillagée déformée (24×16)"""
    W, H = 24, 16
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = img.load()

    # Posts (2 posts, one leaning)
    # Left post (slightly leaning)
    for y in range(2, 15):
        put(px, 3, y, PAL['warm_gray'], W, H)

    # Right post (more leaning)
    for y in range(4, 15):
        x = 20 + int((y - 4) * 0.15)
        put(px, x, y, PAL['warm_gray'], W, H)

    # Chain-link mesh (diamond pattern with gaps)
    for y in range(3, 13):
        for x in range(4, 21):
            # Diamond wire pattern
            if ((x + y) % 3 == 0 or (x - y) % 3 == 0) and (x + y) % 2 == 0:
                # Skip some for "torn" effect
                if 10 < x < 16 and 6 < y < 11:
                    continue  # Torn section
                put(px, x, y, PAL['light_gray'], W, H)

    # Torn section: dangling wires
    put(px, 11, 7, PAL['warm_gray'], W, H)
    put(px, 11, 8, PAL['warm_gray'], W, H)
    put(px, 15, 8, PAL['warm_gray'], W, H)
    put(px, 15, 9, PAL['warm_gray'], W, H)
    put(px, 15, 10, PAL['warm_gray'], W, H)

    # Rust on posts
    put(px, 3, 8, PAL['orange_rust'], W, H)
    put(px, 3, 12, PAL['dark_rust'], W, H)

    # Top rail
    for x in range(3, 22):
        bend = int(math.sin(x * 0.3) * 0.8)
        put(px, x, 2 + bend, PAL['warm_gray'], W, H)

    save_prop('prop_chain_link_fence', img)


def gen_collapsed_stairs():
    """11. Escalier effondré (24×24) — stairs leading to nothing"""
    W, H = 24, 24
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = img.load()

    # Steps (going up-right, isometric style)
    step_h = 3
    step_w = 6
    num_steps = 5

    for i in range(num_steps):
        sx = 2 + i * 3
        sy = 20 - i * 3
        # Step top surface (lighter - top-left lit)
        for x in range(sx, sx + step_w):
            for y in range(sy, sy + 2):
                if 0 <= x < W and 0 <= y < H:
                    v = (x + y) % 3
                    c = PAL['light_concrete'] if v == 0 else PAL['med_concrete']
                    put(px, x, y, c, W, H)
        # Step front face (darker)
        for x in range(sx, sx + step_w):
            for y in range(sy + 2, sy + step_h):
                if 0 <= x < W and 0 <= y < H:
                    put(px, x, y, PAL['dark_concrete'], W, H)

    # Broken top (last step crumbles)
    # Crumbling edge
    put(px, 17, 6, PAL['med_concrete'], W, H)
    put(px, 18, 5, PAL['light_concrete'], W, H)
    put(px, 19, 7, PAL['dark_concrete'], W, H)

    # Rebar exposed at break
    put(px, 16, 7, PAL['dark_rust'], W, H)
    put(px, 17, 7, PAL['orange_rust'], W, H)

    # Side wall (left side of stairway)
    for y in range(8, 22):
        put(px, 1, y, PAL['dark_concrete'], W, H)
        put(px, 2, y, PAL['med_concrete'], W, H)

    # Rubble at base
    rubble = [(6, 22), (8, 23), (10, 22), (12, 23), (14, 22)]
    for rx, ry in rubble:
        put(px, rx, ry, PAL['light_concrete'], W, H)
        put(px, rx + 1, ry, PAL['med_concrete'], W, H)

    # Child's shoe detail (2-3 pixels, heartbreaking)
    put(px, 8, 17, PAL['peeling_blue'], W, H)
    put(px, 9, 17, PAL['peeling_blue'], W, H)
    put(px, 9, 16, shade(PAL['peeling_blue'], 0.8), W, H)

    save_prop('prop_collapsed_stairs', img)


def gen_supermarket_shelves():
    """12. Étagères vides de supermarché (24×20) — completely empty"""
    W, H = 24, 20
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = img.load()

    # Metal frame (vertical posts)
    for y in range(1, 19):
        put(px, 2, y, PAL['warm_gray'], W, H)
        put(px, 21, y, PAL['warm_gray'], W, H)
        # Center post
        put(px, 11, y, PAL['warm_gray'], W, H)

    # Shelves (horizontal, 4 levels, ALL EMPTY)
    shelf_ys = [4, 8, 12, 16]
    for sy in shelf_ys:
        for x in range(2, 22):
            put(px, x, sy, PAL['light_gray'], W, H)
            put(px, x, sy + 1, PAL['warm_gray'], W, H)

    # Empty spaces between shelves — just darkness
    for sy in shelf_ys:
        for y in range(sy - 2, sy):
            for x in range(3, 11):
                put(px, x, y, PAL['dark_warm_gray'], W, H)
            for x in range(12, 21):
                put(px, x, y, PAL['dark_warm_gray'], W, H)

    # Top bracket
    for x in range(2, 22):
        put(px, x, 1, PAL['warm_gray'], W, H)

    # Single price tag still attached (tiny detail)
    put(px, 6, 7, PAL['signage_yellow'], W, H)

    # Dust on bottom shelf
    put(px, 5, 16, PAL['light_gray'], W, H)
    put(px, 8, 16, PAL['light_gray'], W, H)
    put(px, 15, 16, PAL['light_gray'], W, H)

    save_prop('prop_supermarket_shelves', img)


def gen_phone_booth():
    """13. Cabine téléphonique (10×20)"""
    W, H = 10, 20
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = img.load()

    # Frame (metal structure)
    # Left post
    for y in range(2, 19):
        put(px, 1, y, PAL['warm_gray'], W, H)
    # Right post
    for y in range(2, 19):
        put(px, 8, y, PAL['warm_gray'], W, H)

    # Top
    for x in range(1, 9):
        put(px, x, 1, PAL['warm_gray'], W, H)
        put(px, x, 2, PAL['dark_concrete'], W, H)

    # Glass panels (broken/missing)
    for y in range(3, 17):
        for x in range(2, 8):
            if y < 6:
                # Intact glass (top panel)
                put(px, x, y, PAL['broken_glass'], W, H)
            elif y < 10:
                # Missing panel (empty)
                continue
            else:
                # Cracked lower panel
                if (x + y) % 4 != 0:
                    put(px, x, y, PAL['broken_glass'], W, H)
                else:
                    put(px, x, y, shade(PAL['broken_glass'], 0.6), W, H)

    # Phone unit inside (dangling)
    put(px, 4, 8, PAL['dark_warm_gray'], W, H)
    put(px, 5, 8, PAL['dark_warm_gray'], W, H)
    # Cord dangling
    put(px, 5, 9, PAL['dark_warm_gray'], W, H)
    put(px, 4, 10, PAL['dark_warm_gray'], W, H)
    put(px, 4, 11, PAL['warm_gray'], W, H)  # Handset

    # Base
    for x in range(1, 9):
        put(px, x, 18, PAL['dark_concrete'], W, H)
        put(px, x, 19, PAL['med_concrete'], W, H)

    # Rust
    put(px, 1, 14, PAL['orange_rust'], W, H)
    put(px, 8, 10, PAL['dark_rust'], W, H)

    save_prop('prop_phone_booth', img)


def gen_mailbox():
    """14. Boîte aux lettres (8×12) — leaning, stuffed with papers"""
    W, H = 8, 12
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = img.load()

    # Post (leaning right slightly)
    for y in range(5, 12):
        x = 3 + int((y - 5) * 0.1)
        put(px, x, y, PAL['rotting_wood'], W, H)

    # Box body
    for y in range(2, 7):
        for x in range(1, 7):
            if y == 2:
                put(px, x, y, PAL['dark_rust'], W, H)
            else:
                put(px, x, y, PAL['peeling_blue'], W, H)

    # Slot
    put(px, 2, 3, PAL['dark_warm_gray'], W, H)
    put(px, 3, 3, PAL['dark_warm_gray'], W, H)
    put(px, 4, 3, PAL['dark_warm_gray'], W, H)

    # Papers sticking out
    put(px, 2, 2, PAL['off_white'], W, H)
    put(px, 3, 1, PAL['erasure_white'], W, H)
    put(px, 4, 2, PAL['off_white'], W, H)
    put(px, 5, 1, PAL['off_white'], W, H)

    # Rust
    put(px, 5, 5, PAL['orange_rust'], W, H)
    put(px, 1, 6, PAL['dark_rust'], W, H)

    save_prop('prop_mailbox', img)


def gen_graffiti_wall():
    """15. Graffiti mur (24×16) — 'NE PAS OUBLIER' in fading letters"""
    W, H = 24, 16
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = img.load()

    # Concrete wall fragment
    for y in range(2, 15):
        for x in range(1, 23):
            top = 2 + int(abs(math.sin(x * 0.5)) * 1.5)
            if y < top:
                continue
            put(px, x, y, pick_concrete(x, y), W, H)

    # Graffiti text "NPO" (abstract/deteriorating — "Ne Pas Oublier")
    # N
    for y in range(5, 11):
        put(px, 4, y, PAL['signage_yellow'], W, H)
    put(px, 5, 6, PAL['signage_yellow'], W, H)
    put(px, 6, 7, PAL['signage_yellow'], W, H)
    put(px, 7, 8, PAL['signage_yellow'], W, H)
    for y in range(5, 11):
        put(px, 8, y, PAL['signage_yellow'], W, H)

    # P
    for y in range(5, 11):
        put(px, 10, y, PAL['signage_yellow'], W, H)
    put(px, 11, 5, PAL['signage_yellow'], W, H)
    put(px, 12, 5, PAL['signage_yellow'], W, H)
    put(px, 12, 6, PAL['signage_yellow'], W, H)
    put(px, 12, 7, PAL['signage_yellow'], W, H)
    put(px, 11, 7, PAL['signage_yellow'], W, H)

    # O (partial, fading)
    put(px, 15, 6, PAL['signage_yellow'], W, H)
    put(px, 15, 7, PAL['signage_yellow'], W, H)
    put(px, 15, 8, PAL['signage_yellow'], W, H)
    put(px, 15, 9, PAL['signage_yellow'], W, H)
    put(px, 16, 5, PAL['signage_yellow'], W, H)
    put(px, 17, 5, PAL['signage_yellow'], W, H)
    put(px, 18, 6, PAL['signage_yellow'], W, H)
    put(px, 18, 7, shade(PAL['signage_yellow'], 0.6), W, H)
    # Fading/missing bottom of O
    put(px, 17, 10, shade(PAL['signage_yellow'], 0.4), W, H)
    put(px, 16, 10, shade(PAL['signage_yellow'], 0.5), W, H)

    # Drip mark from letters
    put(px, 4, 11, shade(PAL['signage_yellow'], 0.5), W, H)
    put(px, 10, 11, shade(PAL['signage_yellow'], 0.4), W, H)

    save_prop('prop_graffiti_wall', img)


def gen_torn_billboard():
    """16. Affiche publicitaire déchirée (20×28)"""
    W, H = 20, 28
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = img.load()

    # Frame (rusted metal)
    # Left post
    for y in range(0, 28):
        put(px, 1, y, PAL['dark_rust'], W, H)
    # Right post
    for y in range(0, 28):
        put(px, 18, y, PAL['dark_rust'], W, H)

    # Top bar
    for x in range(1, 19):
        put(px, x, 1, PAL['orange_rust'], W, H)

    # Billboard backing (plywood)
    for y in range(2, 16):
        for x in range(2, 18):
            put(px, x, y, PAL['rotting_wood'], W, H)

    # Torn advertisement poster
    for y in range(3, 14):
        for x in range(3, 16):
            # Irregular tear (right side missing more)
            tear_edge = 16 - int((y - 3) * 0.8 + abs(math.sin(y * 1.2)) * 3)
            if x > tear_edge:
                continue
            # Faded colors
            v = (x * 3 + y * 5) % 7
            if v < 2:
                put(px, x, y, PAL['peeling_blue'], W, H)
            elif v < 4:
                put(px, x, y, shade(PAL['signage_yellow'], 0.7), W, H)
            elif v < 5:
                put(px, x, y, PAL['off_white'], W, H)
            else:
                put(px, x, y, shade(PAL['faded_brick'], 1.2), W, H)

    # Hanging torn piece (bottom)
    put(px, 5, 14, PAL['off_white'], W, H)
    put(px, 6, 15, PAL['peeling_blue'], W, H)
    put(px, 5, 15, PAL['off_white'], W, H)

    # Rust on frame
    put(px, 1, 10, PAL['orange_rust'], W, H)
    put(px, 1, 16, PAL['orange_rust'], W, H)
    put(px, 18, 8, PAL['orange_rust'], W, H)
    put(px, 18, 14, PAL['orange_rust'], W, H)

    # Support struts at bottom
    for y in range(16, 27):
        put(px, 5, y, PAL['warm_gray'], W, H)
        put(px, 14, y, PAL['warm_gray'], W, H)

    # Cross brace
    for i in range(9):
        bx = 5 + i
        by = 18 + int(i * 0.6)
        if 0 <= bx < W and 0 <= by < H:
            put(px, bx, by, PAL['warm_gray'], W, H)

    save_prop('prop_torn_billboard', img)


def gen_rusted_swing():
    """BONUS: Balançoire rouillée (from forest biome list, fits urban too)"""
    # Skipping — not in urban ruins spec


# ── Main ─────────────────────────────────────────────────────────────────────

def main():
    print("Generating Ruines Urbaines props...")
    print(f"Output: {OUTPUT_DIR}\n")

    gen_collapsed_building()
    for v in range(1, 4):
        gen_concrete_wall(v)
    gen_brick_wall()
    gen_steel_beam(1)
    gen_steel_beam(2)
    for v in range(1, 4):
        gen_concrete_debris(v)
    gen_overturned_desk()
    gen_urban_car()
    gen_traffic_light()
    gen_dumpster(1)
    gen_dumpster(2)
    gen_chain_link_fence()
    gen_collapsed_stairs()
    gen_supermarket_shelves()
    gen_phone_booth()
    gen_mailbox()
    gen_graffiti_wall()
    gen_torn_billboard()

    # Count generated
    count = len(list(OUTPUT_DIR.glob('*.png')))
    print(f"\nDone! {count} props generated in {OUTPUT_DIR}")


if __name__ == '__main__':
    main()
