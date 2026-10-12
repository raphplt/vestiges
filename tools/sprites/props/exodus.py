"""
L'exode dans les rues (plan 39 B3) : ce que la ville a laissé en partant d'un coup.

Embouteillages figés, portières encore ouvertes ; barrages d'évacuation (barrières, plots, panneau, flèche peinte) ;
petits restes de fuite (valises, poussette, caddie, cartons). Ces décors ne vont que dans les scènes groupées le long
des rues (`ExodusScenePlacer`), jamais semés seuls. Même échelle et même usure que le mobilier urbain (`urban.py`).
"""
from __future__ import annotations

import numpy as np

from ..palette import make_material
from ..render import MODEL_SCALE, PITCH, Part
from ..sdf import capsule, cylinder, ellipsoid, rounded_box, rotation_x, rotation_y, rotation_z, sphere
from ._kit import AXIS_X_YAW, AXIS_Y_YAW, M, PropModel, Weathering, box_footprint
from ._surface import bands, both, noise_mask, painted
from .urban import car

ROT_WHEEL = rotation_z(np.pi / 2)

# Sens de fuite à l'écran → orientation du modèle (avant local +Z). Les deux axes des rues gardent le trois-quarts
# du mobilier urbain ; le demi-tour inverse le sens.
CAR_YAWS = {"e": AXIS_X_YAW, "w": AXIS_X_YAW + np.pi, "s": AXIS_Y_YAW, "n": AXIS_Y_YAW + np.pi}
# Flèches peintes : à plat, alignées sur l'axe de l'écran.
ARROW_YAWS = {"e": np.pi / 2, "w": -np.pi / 2, "s": 0.0, "n": np.pi}

# Portières laissées ouvertes, une configuration par peinture pour que la file ne se répète pas.
CAR_DOORS = {
    "red": ("front", "trunk"),
    "teal": ("front", "front_far", "rear"),
    "cream": ("rear", "front_far"),
}

# Police 3 × 5 du panneau ; « É » porte son accent sur une sixième ligne.
GLYPHS = {
    "E": ("111", "100", "110", "100", "111"),
    "V": ("101", "101", "101", "101", "010"),
    "A": ("010", "101", "111", "101", "101"),
    "C": ("011", "100", "100", "100", "011"),
    "U": ("101", "101", "101", "101", "111"),
    "T": ("111", "010", "010", "010", "010"),
    "I": ("1", "1", "1", "1", "1"),
    "O": ("010", "101", "101", "101", "010"),
    "N": ("101", "111", "111", "101", "101"),
}
SIGN_WORD = "EVACUATION"
# Flèche du panneau, 5 lignes, selon le sens de fuite à l'écran.
SIGN_ARROWS = {
    "e": ("0000000100", "0000000110", "1111111111", "0000000110", "0000000100"),
    "w": ("0010000000", "0110000000", "1111111111", "0110000000", "0010000000"),
    "n": ("00100", "01110", "11111", "00100", "00100"),
    "s": ("00100", "00100", "11111", "01110", "00100"),
}
# Cadre de rendu du panneau : sa largeur paire met le pivot sur un bord de pixel, d'où l'alignement de la police.
SIGN_CANVAS = (76, 72)


def _union(*distances: np.ndarray) -> np.ndarray:
    result = distances[0]
    for d in distances[1:]:
        result = np.minimum(result, d)
    return result


# ---------------------------------------------------------------------------------------------------------------------
# Petits restes de fuite
# ---------------------------------------------------------------------------------------------------------------------

def open_suitcase(stem: str, seed: int) -> PropModel:
    """Valise ouverte à plat, couvercle rabattu derrière, vêtements en tas qui débordent et une manche qui pend."""
    SHELL, SHELL_DARK, LINING, CLOTH_A, CLOTH_B, CLOTH_C = range(6)
    materials = [
        make_material("shell", "#7A4F3A"), make_material("shell_dark", "#4E3428", contrast=0.6),
        make_material("lining", "#9C8E7C"), make_material("cloth_a", "#C9C6B6"),
        make_material("cloth_b", "#5F7FA8"), make_material("cloth_c", "#B0583E"),
    ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        base_c, half = np.array([0.0, 0.13 * M, 0.0]), (0.42 * M, 0.13 * M, 0.3 * M)
        # Couvercle rabattu contre un muret imaginaire : relevé, il montre sa doublure à la caméra.
        lid_r = rotation_x(-1.1)
        lid_c = np.array([0.0, 0.3 * M, -0.42 * M])
        clothes = [((w.uniform(-0.26, 0.26) * M, 0.25 * M, w.uniform(-0.18, 0.18) * M),
                    (w.uniform(0.15, 0.24) * M, 0.08 * M, w.uniform(0.12, 0.19) * M), k % 3) for k in range(6)]
        sleeve_a, sleeve_b = (0.34 * M, 0.27 * M, 0.1 * M), (0.66 * M, 0.03 * M, 0.26 * M)
        hollow = lambda p: np.maximum(rounded_box(p, base_c, half, 0.04 * M),
                                      -rounded_box(p, base_c + (0, 0.08 * M, 0), (0.38 * M, 0.13 * M, 0.26 * M), 0.02 * M))
        return [
            Part(hollow, SHELL),
            Part(painted(hollow, lambda p: np.abs(p[:, 1] - 0.24 * M) - 0.015 * M, 0.01 * M), SHELL_DARK),
            Part(lambda p: rounded_box(p, lid_c, (0.42 * M, 0.04 * M, 0.3 * M), 0.03 * M, lid_r), SHELL),
            Part(lambda p: rounded_box(p, lid_c + lid_r @ np.array([0, 0.035 * M, 0]), (0.37 * M, 0.012 * M, 0.25 * M), 0.01 * M, lid_r), LINING),
            *[Part(lambda p, c=c, h=h: ellipsoid(p, c, h), CLOTH_A + k) for c, h, k in clothes],
            Part(lambda p: capsule(p, sleeve_a, sleeve_b, 0.07 * M, 0.06 * M), CLOTH_B),
        ]

    return PropModel(stem, parts, materials, float(np.radians(35.0)), canvas=(60, 48))


def luggage(stem: str, seed: int) -> PropModel:
    """Valise rigide debout, poignée tirée, et sac de voyage affaissé contre elle."""
    SHELL, RIB, HANDLE, BAG, STRAP = range(5)
    materials = [
        make_material("shell", "#4F6470"), make_material("rib", "#36454E", contrast=0.6),
        make_material("handle", "#2E2C2C", contrast=0.5), make_material("bag", "#6F6046"),
        make_material("strap", "#3A3226", contrast=0.6),
    ]

    def parts() -> list[Part]:
        tilt = rotation_z(Weathering(seed).uniform(-0.08, 0.08))
        case_c = tilt @ np.array([-0.15 * M, 0.38 * M, 0.0])
        case = lambda p: rounded_box(p, case_c, (0.24 * M, 0.36 * M, 0.12 * M), 0.05 * M, tilt)
        bag_c = np.array([0.32 * M, 0.17 * M, 0.12 * M])
        bag = lambda p: ellipsoid(p, bag_c, (0.32 * M, 0.17 * M, 0.17 * M))
        return [
            Part(case, SHELL),
            Part(painted(case, bands(0, 0.12 * M, 0.02 * M), 0.012 * M), RIB),
            Part(lambda p: _union(*(capsule(p, case_c + tilt @ np.array([x * 0.12 * M, 0.36 * M, 0]),
                                             case_c + tilt @ np.array([x * 0.12 * M, 0.62 * M, 0]), 0.018 * M) for x in (-1, 1)),
                                  capsule(p, case_c + tilt @ np.array([-0.12 * M, 0.62 * M, 0]),
                                          case_c + tilt @ np.array([0.12 * M, 0.62 * M, 0]), 0.025 * M)), HANDLE),
            Part(bag, BAG),
            Part(painted(bag, lambda p: np.abs(p[:, 0] - 0.32 * M) - 0.03 * M, 0.01 * M), STRAP),
            Part(lambda p: capsule(p, bag_c + (-0.18 * M, 0.12 * M, 0), bag_c + (0.18 * M, 0.12 * M, 0), 0.025 * M), STRAP),
        ]

    return PropModel(stem, parts, materials, float(np.radians(-25.0)), canvas=(48, 56))


def stroller(stem: str, seed: int) -> PropModel:
    """Poussette abandonnée : nacelle sous sa capote, roues fines, guidon tourné vers la rue ; une couverture pend."""
    BODY, HOOD, FRAME, TYRE, BLANKET = range(5)
    materials = [
        make_material("body", "#3E4E6A"), make_material("hood", "#2F3B52"),
        make_material("frame", "#9A9890"), make_material("tyre", "#232023", contrast=0.5),
        make_material("blanket", "#C2A58A"),
    ]

    def parts() -> list[Part]:
        tilt = rotation_z(Weathering(seed).uniform(-0.06, 0.06))
        nacelle_c = np.array([0.0, 0.62 * M, 0.05 * M])
        nacelle = lambda p: np.maximum(rounded_box(p, nacelle_c, (0.24 * M, 0.17 * M, 0.38 * M), 0.12 * M, tilt),
                                       -rounded_box(p, nacelle_c + (0, 0.14 * M, 0.05 * M), (0.19 * M, 0.12 * M, 0.3 * M), 0.08 * M, tilt))
        hood_c = nacelle_c + (0, 0.12 * M, -0.2 * M)
        # Capote : demi-coque au-dessus de la nacelle, ouverte vers l'avant.
        hood = lambda p: np.maximum.reduce([ellipsoid(p, hood_c, (0.26 * M, 0.3 * M, 0.26 * M)),
                                            -ellipsoid(p, hood_c, (0.22 * M, 0.26 * M, 0.22 * M)),
                                            hood_c[1] - p[:, 1], p[:, 2] - hood_c[2] - 0.02 * M])
        wheels = [(x * 0.24 * M, 0.13 * M, z * 0.3 * M) for x in (-1, 1) for z in (-1, 1)]
        return [
            Part(nacelle, BODY),
            Part(hood, HOOD),
            Part(lambda p: _union(*(capsule(p, (x * 0.22 * M, 0.13 * M, -0.3 * M), (x * 0.2 * M, 0.5 * M, 0.0), 0.02 * M) for x in (-1, 1)),
                                  *(capsule(p, (x * 0.22 * M, 0.13 * M, 0.3 * M), (x * 0.2 * M, 0.5 * M, 0.0), 0.02 * M) for x in (-1, 1)),
                                  *(capsule(p, (x * 0.2 * M, 0.55 * M, -0.3 * M), (x * 0.18 * M, 1.0 * M, -0.62 * M), 0.02 * M) for x in (-1, 1)),
                                  capsule(p, (-0.2 * M, 1.0 * M, -0.62 * M), (0.2 * M, 1.0 * M, -0.62 * M), 0.035 * M)), FRAME),
            Part(lambda p: _union(*(cylinder(p, c, 0.13 * M, 0.03 * M, 0.01 * M, ROT_WHEEL) for c in wheels)), TYRE),
            Part(lambda p: rounded_box(p, (0.25 * M, 0.5 * M, 0.15 * M), (0.025 * M, 0.2 * M, 0.17 * M), 0.02 * M, rotation_z(0.15)), BLANKET),
        ]

    return PropModel(stem, parts, materials, float(np.radians(50.0)), canvas=(44, 52))


def shopping_cart(stem: str, seed: int) -> PropModel:
    """Caddie rempli à la hâte : grillage, couverture roulée, sacs ; une roulette de travers."""
    WIRE, WIRE_DARK, HANDLE, TYRE, BLANKET, BAG_A, BAG_B = range(7)
    materials = [
        make_material("wire", "#9EA09A"), make_material("wire_dark", "#4C4E4C", contrast=0.6),
        make_material("handle", "#8A3A30"), make_material("tyre", "#232023", contrast=0.5),
        make_material("blanket", "#7E6A8C"), make_material("bag_a", "#C8C0A8"), make_material("bag_b", "#5E7A60"),
    ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        basket_c = np.array([0.0, 0.72 * M, 0.0])
        outer = lambda p: rounded_box(p, basket_c, (0.3 * M, 0.25 * M, 0.45 * M), 0.02 * M)
        basket = lambda p: np.maximum(outer(p), -rounded_box(p, basket_c + (0, 0.05 * M, 0), (0.27 * M, 0.25 * M, 0.42 * M), 0.0))
        mesh = lambda p: np.maximum(bands(1, 0.1 * M, 0.05 * M)(p), np.maximum(bands(0, 0.1 * M, 0.05 * M)(p), bands(2, 0.1 * M, 0.05 * M)(p)))
        wheels = [(x * 0.24 * M, 0.08 * M, z * 0.36 * M) for x in (-1, 1) for z in (-1, 1)]
        return [
            Part(basket, WIRE),
            Part(painted(basket, mesh, 0.01 * M), WIRE_DARK, relief=False),
            Part(lambda p: _union(*(capsule(p, (x * 0.26 * M, 0.1 * M, z * 0.36 * M), (x * 0.26 * M, 0.47 * M, z * 0.4 * M), 0.02 * M)
                                    for x in (-1, 1) for z in (-1, 1))), WIRE),
            Part(lambda p: capsule(p, (-0.3 * M, 1.02 * M, -0.55 * M), (0.3 * M, 1.02 * M, -0.55 * M), 0.035 * M), HANDLE),
            Part(lambda p: _union(*(cylinder(p, c, 0.07 * M, 0.025 * M, 0.01 * M, ROT_WHEEL @ rotation_x(w.uniform(-0.3, 0.3)))
                                    for c in wheels)), TYRE),
            Part(lambda p: capsule(p, (-0.22 * M, 1.0 * M, 0.12 * M), (0.22 * M, 1.02 * M, 0.08 * M), 0.13 * M), BLANKET),
            Part(lambda p: ellipsoid(p, (0.08 * M, 0.95 * M, -0.22 * M), (0.18 * M, 0.2 * M, 0.15 * M)), BAG_A),
            Part(lambda p: ellipsoid(p, (-0.12 * M, 0.98 * M, 0.3 * M), (0.14 * M, 0.17 * M, 0.12 * M)), BAG_B),
        ]

    return PropModel(stem, parts, materials, float(np.radians(-40.0)), canvas=(48, 56))


def cardboard_boxes(stem: str, seed: int) -> PropModel:
    """Deux cartons empilés de travers, scotch en croix ; celui du haut ouvert, rabats dressés."""
    CARD, CARD_DARK, TAPE = range(3)
    materials = [
        make_material("card", "#A68A60"), make_material("card_dark", "#7A6244", contrast=0.6),
        make_material("tape", "#C9BC94", contrast=0.5),
    ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        low_r = rotation_y(w.uniform(-0.2, 0.2))
        top_r = rotation_y(w.uniform(0.3, 0.6))
        low_c, top_c = np.array([0.0, 0.24 * M, 0.0]), np.array([0.05 * M, 0.66 * M, 0.02 * M])
        low = lambda p: rounded_box(p, low_c, (0.34 * M, 0.24 * M, 0.28 * M), 0.02 * M, low_r)
        top = lambda p: np.maximum(rounded_box(p, top_c, (0.26 * M, 0.18 * M, 0.22 * M), 0.02 * M, top_r),
                                   -rounded_box(p, top_c + (0, 0.04 * M, 0), (0.24 * M, 0.18 * M, 0.2 * M), 0.0, top_r))
        flaps = [top_c + top_r @ np.array([sx * 0.3 * M, 0.2 * M, 0.0]) for sx in (-1, 1)]
        tape = lambda p: np.abs(((p - low_c) @ low_r)[:, 0]) - 0.04 * M
        return [
            Part(low, CARD),
            Part(painted(low, tape, 0.012 * M), TAPE),
            Part(top, CARD),
            Part(lambda p: _union(*(rounded_box(p, c, (0.012 * M, 0.12 * M, 0.2 * M), 0.008 * M, top_r @ rotation_z(-sx * 0.5))
                                    for c, sx in zip(flaps, (-1, 1)))), CARD_DARK),
        ]

    return PropModel(stem, parts, materials, float(np.radians(15.0)), canvas=(48, 56))


# ---------------------------------------------------------------------------------------------------------------------
# Barrage d'évacuation
# ---------------------------------------------------------------------------------------------------------------------

def barrier(stem: str, yaw: float, seed: int) -> PropModel:
    """Barrière de police en chevalet : planche à rayures rouges et blanches sur deux pieds en A, peinture écaillée."""
    WHITE, RED, LEG, RUST = range(4)
    materials = [
        make_material("white", "#CFC8B6"), make_material("red", "#A23A2E"),
        make_material("leg", "#5A5A56"), make_material("rust", "#6A4430"),
    ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        sag = rotation_x(w.uniform(-0.04, 0.04))
        plank = lambda p: rounded_box(p, (0.0, 0.85 * M, 0.0), (0.06 * M, 0.13 * M, 0.85 * M), 0.02 * M, sag)
        stripes = lambda p: np.abs(np.mod(p[:, 2] + p[:, 1] * 1.2, 0.36 * M) - 0.18 * M) - 0.09 * M
        legs = [((0, 0.85 * M, z * 0.7 * M), (x * 0.32 * M, 0.0, z * 0.75 * M)) for z in (-1, 1) for x in (-1, 1)]
        return [
            Part(plank, WHITE),
            Part(painted(plank, stripes, 0.01 * M), RED),
            Part(painted(plank, noise_mask(0.1 * M, seed + 1, 0.2), 0.012 * M), RUST, relief=False),
            Part(lambda p: _union(*(capsule(p, a, b, 0.035 * M) for a, b in legs)), LEG),
        ]

    return PropModel(stem, parts, materials, yaw, canvas=(76, 60), footprint=box_footprint(0.12 * M, 0.85 * M))


def cones(stem: str, seed: int) -> PropModel:
    """Trois plots orange à bande blanche, l'un renversé, roulé de côté."""
    ORANGE, BAND, BASE = range(3)
    materials = [
        make_material("orange", "#C2602E"), make_material("band", "#D6D0C0", contrast=0.5),
        make_material("base", "#2C2A2A", contrast=0.5),
    ]

    def cone_parts(base, rotation) -> list[Part]:
        tip = base + rotation @ np.array([0.0, 0.7 * M, 0.0])
        body = lambda p: capsule(p, base + rotation @ np.array([0.0, 0.05 * M, 0.0]), tip, 0.17 * M, 0.035 * M)
        band = lambda p: np.abs(((p - base) @ rotation)[:, 1] - 0.42 * M) - 0.06 * M
        return [
            Part(body, ORANGE),
            Part(painted(body, band, 0.012 * M), BAND),
            Part(lambda p: rounded_box(p, base + rotation @ np.array([0.0, 0.025 * M, 0.0]), (0.22 * M, 0.025 * M, 0.22 * M), 0.015 * M, rotation), BASE),
        ]

    def parts() -> list[Part]:
        w = Weathering(seed)
        fallen = rotation_y(w.uniform(0.6, 1.0)) @ rotation_z(np.pi / 2 - 0.25)
        return [
            *cone_parts(np.array([-0.45 * M, 0.0, -0.15 * M]), np.eye(3)),
            *cone_parts(np.array([0.2 * M, 0.0, -0.45 * M]), rotation_z(0.06)),
            *cone_parts(np.array([0.2 * M, 0.22 * M, 0.45 * M]), fallen),
        ]

    return PropModel(stem, parts, materials, float(np.radians(10.0)), canvas=(60, 52))


def evacuation_sign(stem: str, arrow: str, seed: int) -> PropModel:
    """
    Grand panneau d'évacuation sur deux pieds lestés : fond blanc sale, liseré rouge, « ÉVACUATION » en capitales
    et une flèche dessous, dans le sens de la fuite. Face à la caméra ; le texte est calé sur la grille des pixels
    de sortie (projection du rendu), sinon un trait d'un pixel tombe à cheval sur deux et s'efface.
    """
    BOARD, BORDER, INK, LEG, WEIGHT, DIRT = range(6)
    materials = [
        make_material("board", "#D4CCB8", contrast=0.4), make_material("border", "#A23A2E", contrast=0.5),
        make_material("ink", "#2B2624", contrast=0.3), make_material("leg", "#5A5A56"),
        make_material("weight", "#6A6458"), make_material("dirt", "#9C9078", contrast=0.4),
    ]
    canvas = SIGN_CANVAS
    pivot_y = canvas[1] * 0.72
    text_w = sum(len(GLYPHS[c][0]) + 1 for c in SIGN_WORD) - 1
    # Planche en pixels d'écran : liseré, deux lignes (accent), mot, une ligne, flèche, une ligne, liseré.
    cols, rows = text_w + 4, 16
    half_w = cols / 2 / MODEL_SCALE
    half_h = rows / 2 / (MODEL_SCALE * np.cos(PITCH))
    board_c = np.array([0.0, 0.62 * M + half_h, 0.0])
    face_z = 0.03 * M
    # Première ligne de la face avant (la tranche du dessus de la planche occupe la ligne au-dessus).
    top_row = int(np.floor(pivot_y - ((board_c[1] + half_h) * np.cos(PITCH) - face_z * np.sin(PITCH)) * MODEL_SCALE)) + 1
    ink_pixels = set()
    x = -(text_w // 2)
    for index, letter in enumerate(SIGN_WORD):
        glyph = GLYPHS[letter]
        for r, line in enumerate(glyph):
            ink_pixels |= {(x + c, top_row + 3 + r) for c, bit in enumerate(line) if bit == "1"}
        if index == 0:
            ink_pixels.add((x + 1, top_row + 2))
        x += len(glyph[0]) + 1
    arrow_glyph = SIGN_ARROWS[arrow]
    ax = -(len(arrow_glyph[0]) // 2)
    for r, line in enumerate(arrow_glyph):
        ink_pixels |= {(ax + c, top_row + 9 + r) for c, bit in enumerate(line) if bit == "1"}
    lookup = np.array(sorted(ink_pixels))

    def ink_mask(p: np.ndarray) -> np.ndarray:
        col = np.floor(p[:, 0] * MODEL_SCALE).astype(int)
        row = np.floor(pivot_y - (p[:, 1] * np.cos(PITCH) - p[:, 2] * np.sin(PITCH)) * MODEL_SCALE).astype(int)
        hit = ((col[:, None] == lookup[None, :, 0]) & (row[:, None] == lookup[None, :, 1])).any(axis=1)
        return np.where(hit, -1.0, 1.0)

    def parts() -> list[Part]:
        board = lambda p: rounded_box(p, board_c, (half_w, half_h, face_z), 0.01 * M)
        # Liseré : peint à moins d'un pixel du bord.
        border = lambda p: np.minimum(half_w - 1.0 / MODEL_SCALE - np.abs(p[:, 0]),
                                      half_h - 1.0 / (MODEL_SCALE * np.cos(PITCH)) - np.abs(p[:, 1] - board_c[1]))
        legs = [(x * (half_w - 0.15 * M), board_c[1]) for x in (-1, 1)]
        front = lambda p: -p[:, 2]
        return [
            Part(board, BOARD),
            Part(painted(board, both(border, front), 0.01 * M), BORDER, relief=False),
            # Coulures de saleté sur le bas de la planche, sous l'encre.
            Part(painted(board, both(noise_mask(0.2 * M, seed, 0.15), lambda p: p[:, 1] - board_c[1] + half_h * 0.4, front), 0.01 * M),
                 DIRT, relief=False),
            Part(painted(board, both(ink_mask, front), 0.014 * M), INK, relief=False),
            Part(lambda p: _union(*(capsule(p, (x, 0.05 * M, -0.06 * M), (x, y, -0.06 * M), 0.035 * M) for x, y in legs)), LEG),
            Part(lambda p: _union(*(rounded_box(p, (x, 0.07 * M, -0.06 * M), (0.14 * M, 0.07 * M, 0.2 * M), 0.04 * M) for x, _ in legs)), WEIGHT),
        ]

    return PropModel(stem, parts, materials, 0.0, canvas=canvas, footprint=box_footprint(half_w, 0.2 * M), smooth_slopes=True)


def road_arrow(stem: str, yaw: float, stretch: float, seed: int) -> PropModel:
    """
    Flèche peinte sur la chaussée, à plat et usée : elle indique le sens de l'évacuation. Le long d'une rue verticale,
    la vue à 30° écrase la longueur de moitié : `stretch` l'allonge d'autant pour qu'elle se lise.
    """
    PAINT, = range(1)
    materials = [make_material("paint", "#C4BEAA", contrast=0.3)]

    def parts() -> list[Part]:
        wear = noise_mask(0.12 * M, seed, 0.3)
        shaft = lambda p: rounded_box(p, (0.0, 0.01 * M, -0.45 * M * stretch), (0.14 * M, 0.01 * M, 0.7 * M * stretch), 0.0)
        # Pointe : triangle entre deux demi-plans obliques et la base.
        head = lambda p: np.maximum.reduce([np.abs(p[:, 1] - 0.01 * M) - 0.01 * M, 0.25 * M * stretch - p[:, 2],
                                            np.abs(p[:, 0]) + (p[:, 2] / stretch - 0.25 * M) * 0.8 - 0.4 * M])
        arrow = lambda p: np.minimum(shaft(p), head(p))
        return [Part(lambda p: np.maximum(arrow(p), -wear(p) * 0.2), PAINT, relief=False)]

    return PropModel(stem, parts, materials, yaw, canvas=(96, 72), smooth_slopes=True)


def radio_car(stem: str, paint: str, yaw: float, seed: int) -> PropModel:
    """
    Voiture de la file avec une radio portative oubliée sur le capot (plan 39 C1), allumée : le message d'évacuation
    joue encore. Même voiture que sa sœur sans radio ; un poste à peine plus grand que nature et son antenne tirée
    pour se lire à une dizaine de pixels de large.
    """
    model = car(stem, paint, yaw, seed, open_doors=CAR_DOORS[paint])
    BODY, GRILLE, DIAL, ANTENNA = range(len(model.materials), len(model.materials) + 4)
    # Poste clair, sauf sur la peinture crème où il disparaîtrait : là, un poste sombre.
    body_hex, grille_hex = ("#3A3431", "#B9B2A0") if paint == "cream" else ("#CDBF9F", "#3A3431")
    materials = [*model.materials,
                 make_material("radio_body", body_hex, contrast=0.5), make_material("radio_grille", grille_hex, contrast=0.6),
                 make_material("radio_dial", "#D08A3C", contrast=0.4), make_material("radio_antenna", "#B8B6AE", contrast=0.4)]
    turn = rotation_y(0.45)
    center = np.array([-0.12 * M, 1.12 * M, 1.45 * M])

    def parts() -> list[Part]:
        radio = lambda p: rounded_box(p, center, (0.32 * M, 0.18 * M, 0.11 * M), 0.04 * M, turn)
        local = lambda p: (p - center) @ turn
        # Haut-parleur à gauche du poste, cadran à droite : visibles quelle que soit l'orientation de la voiture.
        dial = lambda p: np.maximum(np.abs(local(p)[:, 0] - 0.16 * M) - 0.08 * M, np.abs(local(p)[:, 1] - 0.04 * M) - 0.045 * M)
        tip = center + turn @ np.array([0.26 * M, 0.95 * M, -0.08 * M])
        return [
            *model.parts(),
            Part(radio, BODY),
            Part(painted(radio, lambda p: np.maximum(local(p)[:, 0] + 0.03 * M, np.abs(local(p)[:, 1]) - 0.12 * M), 0.012 * M),
                 GRILLE, relief=False),
            Part(painted(radio, dial, 0.012 * M), DIAL, relief=False),
            Part(lambda p: capsule(p, center + turn @ np.array([0.25 * M, 0.16 * M, -0.06 * M]), tip, 0.035 * M, 0.025 * M), ANTENNA),
        ]

    return PropModel(stem, parts, materials, model.yaw, canvas=model.canvas, footprint=model.footprint)


def catalog() -> list[PropModel]:
    models = []
    for paint, seed in (("red", 212), ("teal", 221), ("cream", 231)):
        for direction, yaw in CAR_YAWS.items():
            models.append(car(f"prop_exodus_car_{paint}_{direction}", paint, float(yaw), seed + ord(direction),
                              open_doors=CAR_DOORS[paint]))
            models.append(radio_car(f"prop_exodus_car_{paint}_{direction}_radio", paint, float(yaw), seed + ord(direction)))
    models += [
        open_suitcase("prop_exodus_suitcase_open", 301),
        luggage("prop_exodus_luggage", 311),
        stroller("prop_exodus_stroller", 321),
        shopping_cart("prop_exodus_cart", 331),
        cardboard_boxes("prop_exodus_boxes", 341),
        barrier("prop_exodus_barrier_x", AXIS_X_YAW, 401),
        barrier("prop_exodus_barrier_y", AXIS_Y_YAW, 402),
        cones("prop_exodus_cones", 411),
    ]
    models += [evacuation_sign(f"prop_exodus_sign_{d}", d, 421 + i) for i, d in enumerate("ewns")]
    models += [road_arrow(f"prop_exodus_arrow_{d}", float(yaw), 1.0 if d in "ew" else 1.8, 431 + i)
               for i, (d, yaw) in enumerate(ARROW_YAWS.items())]
    return models
