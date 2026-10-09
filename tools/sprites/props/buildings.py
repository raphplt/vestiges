"""
Immeubles des Ruines Urbaines (plan 08, lot P2b) : modules qui bordent les îlots, à l'échelle du personnage.

Un module est dimensionné en cellules de la grille (une cellule ≈ 3,7 m de large, un rang ≈ 1,9 m de profondeur) pour
que le placeur les aligne le long des rues. Vus presque de face (lacet ±12°) : la façade suit la rue, un liseré de
côté donne le volume, le toit reste lisible. Fenêtres et bandeaux par répétition de domaine (coût constant).
L'usure vient de la graine : brèches, angle effondré, gravats, mousse et lierre.
"""
from __future__ import annotations

from dataclasses import dataclass

import numpy as np

from ..palette import make_material
from ..render import Part
from ..sdf import capsule, cylinder, ellipsoid, rotation_x, rotation_y, rotation_z, rounded_box, sphere
from ._kit import AXIS_X_YAW, M, PropModel, Weathering, box_footprint
from ._surface import bands, both, bricks, cracks, noise_mask, painted, value_noise
from .forest import _clumps

CELL_WIDTH = 103.0   # 64 px à MODEL_SCALE
ROW_DEPTH = 51.6     # 16 px de rang, compressés par l'inclinaison
STOREY = 3.0 * M
BUILDING_YAW = float(np.radians(12.0))
# Hauteur visible bornée par la profondeur d'un îlot à l'écran (~190 px) : au-delà, l'immeuble masque la rue au nord.
DEPTH_ROWS = 4

PLASTERS = ["#A99B84", "#8F8B84", "#B08D6C", "#8D9A9B", "#9E806F", "#A7A08E"]


@dataclass(frozen=True)
class BuildingSpec:
    stem: str
    width_cells: int
    depth_rows: int
    storeys: int
    style: str          # "apartment", "shop", "house", "ruin"
    plaster: str
    damage: float       # 0 intact → 1 effondré
    seed: int
    mirrored: bool = False
    # Entraxe des fenêtres : une maison de campagne en a moins qu'un immeuble de ville.
    window_spacing: float = 1.6 * M


def _union(*distances: np.ndarray) -> np.ndarray:
    result = distances[0]
    for d in distances[1:]:
        result = np.minimum(result, d)
    return result


def _box(p: np.ndarray, center, half) -> np.ndarray:
    return rounded_box(p, center, half, 0.0)


def _hash(*values: np.ndarray) -> np.ndarray:
    """Bruit de cellule déterministe dans [0, 1) (fenêtre, étage, face)."""
    total = np.zeros_like(values[0], dtype=np.float64)
    for index, value in enumerate(values):
        total = total + value * (12.9898 + 41.23 * index)
    return np.modf(np.abs(np.sin(total) * 43758.5453))[0]


def _shade(hex_color: str, factor: float) -> str:
    """Même teinte, plus claire ou plus sombre : enduit à l'ombre, crasse, fissure."""
    rgb = (int(hex_color[i:i + 2], 16) for i in (1, 3, 5))
    return "#" + "".join(f"{max(0, min(255, round(c * factor))):02X}" for c in rgb)


class Facades:
    """
    Fenêtres des quatre faces, par répétition de domaine (coût constant) : alvéoles, vitres, appuis, linteaux,
    encadrements, volets et planches. Chaque fenêtre tire son état (vitre intacte, brisée, condamnée, volets) de sa
    cellule ; les mêmes tirages servent à toutes les pièces, pour qu'une vitre ne tombe jamais dans une fenêtre condamnée.
    """

    def __init__(self, half_w: float, half_d: float, first_row: float, rows: int, seed: int,
                 spacing: float = 1.6 * M, win_w: float = 0.42 * M, win_h: float = 0.62 * M,
                 boarded_share: float = 0.1, glass_share: float = 0.4, shutter_share: float = 0.0, balcony_share: float = 0.0):
        self.half_w, self.half_d = half_w, half_d
        self.first_row, self.rows, self.seed = first_row, rows, seed
        self.spacing, self.win_w, self.win_h = spacing, win_w, win_h
        self.boarded_share, self.glass_share = boarded_share, glass_share
        self.shutter_share, self.balcony_share = shutter_share, balcony_share
        self._memo: tuple[np.ndarray, dict] | None = None

    def _cells(self, p: np.ndarray):
        x, y, z = p[:, 0], p[:, 1], p[:, 2]
        row = np.clip(np.round((y - self.first_row) / STOREY), 0, max(0, self.rows - 1))
        row_y = self.first_row + row * STOREY
        faces = []
        for face_id, (along, across, half_along, half_across) in enumerate(
                ((x, z, self.half_w, self.half_d), (z, x, self.half_d, self.half_w))):
            count = max(1, int((2 * half_along) // self.spacing))
            start = -(count - 1) * self.spacing / 2
            column = np.clip(np.round((along - start) / self.spacing), 0, count - 1)
            du = along - (start + column * self.spacing)
            dn = np.abs(across) - half_across
            side = np.sign(across) + 2 * face_id
            faces.append((du, dn, column, side))
        return y - row_y, row, faces

    def parts(self, p: np.ndarray) -> dict[str, np.ndarray]:
        """Distances de chaque pièce de fenêtre ; mémorisées pour le tableau de points en cours d'évaluation."""
        if self._memo is not None and self._memo[0] is p:
            return self._memo[1]
        dy, row, faces = self._cells(p)
        w, h = self.win_w, self.win_h
        result = {key: [] for key in ("holes", "panes", "glints", "boards", "sills", "lintels", "frames", "shutters",
                                      "balconies", "railings")}
        for du, dn, column, side in faces:
            noise = _hash(column, row, side, np.full_like(du, self.seed))
            shutter_noise = _hash(column, row, side, np.full_like(du, self.seed + 13))
            balcony_noise = _hash(column, row, side, np.full_like(du, self.seed + 29))
            adu = np.abs(du)
            boarded = noise < self.boarded_share
            glass = (noise >= self.boarded_share) & (noise < self.boarded_share + self.glass_share)
            shuttered = (shutter_noise < self.shutter_share) & ~boarded
            window = np.maximum(np.maximum(adu - w, np.abs(dy) - h), np.abs(dn) - 0.2 * M)
            result["holes"].append(np.where(boarded, np.inf, window))
            pane = np.maximum(np.maximum(adu - w, np.abs(dy) - h), np.abs(dn + 0.12 * M) - 0.02 * M)
            result["panes"].append(np.where(glass, pane, np.inf))
            # Reflet : une diagonale claire sur la vitre, comme le ciel qui s'y accroche.
            glint = np.maximum(pane - 0.004 * M, np.abs(du * 0.9 + dy + h * 0.25) - 0.07 * M)
            result["glints"].append(np.where(glass, glint, np.inf))
            plank = np.maximum(np.maximum(adu - w * 0.95, np.abs(dy) - h * 0.9), np.abs(dn - 0.02 * M) - 0.04 * M)
            result["boards"].append(np.where(boarded, plank, np.inf))
            result["sills"].append(np.maximum(np.maximum(adu - w - 0.08 * M, np.abs(dy + h + 0.06 * M) - 0.06 * M),
                                              np.abs(dn - 0.05 * M) - 0.07 * M))
            result["lintels"].append(np.maximum(np.maximum(adu - w - 0.05 * M, np.abs(dy - h - 0.09 * M) - 0.07 * M),
                                                np.abs(dn - 0.02 * M) - 0.04 * M))
            ring = np.maximum(np.maximum(adu - w - 0.06 * M, np.abs(dy) - h - 0.03 * M), np.abs(dn - 0.01 * M) - 0.025 * M)
            result["frames"].append(np.maximum(ring, -np.maximum(adu - w, np.abs(dy) - h)))
            leaf = np.maximum(np.maximum(np.abs(adu - w * 1.55 - 0.05 * M) - w * 0.5, np.abs(dy) - h), np.abs(dn - 0.04 * M) - 0.025 * M)
            result["shutters"].append(np.where(shuttered, leaf, np.inf))
            # Balcons : façade avant, à l'étage, une fenêtre sur trois environ.
            front = (side == 1) & (row >= 1) & (balcony_noise < self.balcony_share)
            slab = np.maximum(np.maximum(adu - w - 0.3 * M, np.abs(dy + h + 0.1 * M) - 0.06 * M), np.abs(dn - 0.3 * M) - 0.3 * M)
            result["balconies"].append(np.where(front, slab, np.inf))
            rail_box = np.maximum(np.maximum(adu - w - 0.28 * M, np.abs(dy + h * 0.45) - h * 0.55 + 0.04 * M), np.abs(dn - 0.57 * M) - 0.02 * M)
            bars = np.minimum(np.abs(np.mod(du, 0.14 * M) - 0.07 * M) - 0.022 * M, np.abs(dy - 0.04 * M) - 0.03 * M)
            sides = np.maximum(np.abs(adu - w - 0.28 * M) - 0.02 * M, np.maximum(np.abs(dy + h * 0.45) - h * 0.55 + 0.04 * M, np.abs(dn - 0.3 * M) - 0.28 * M))
            result["railings"].append(np.where(front, np.minimum(np.maximum(rail_box, bars), sides), np.inf))
        merged = {key: _union(*values) for key, values in result.items()}
        self._memo = (p, merged)
        return merged


def _ivy(w: Weathering, x0: float, face_z: float, climb: float) -> list[tuple[np.ndarray, np.ndarray]]:
    """
    Lierre : deux ou trois tiges qui grimpent en zigzag depuis le pied du mur et s'écartent en montant, chargées de
    petites grappes de feuilles plaquées au mur ; une tache ajourée et irrégulière, pas une colonne.
    """
    leaves = []
    for _ in range(int(w.uniform(2, 3.99))):
        x, y = x0 + w.uniform(-0.2, 0.2) * M, 0.1 * M
        while y < climb:
            size = w.uniform(0.16, 0.26) * M * (1.0 - 0.35 * y / climb)
            leaves.append((np.array([x, y, face_z + 0.04 * M]), np.array([size, size * 0.85, 0.07 * M])))
            x += w.uniform(-0.32, 0.32) * M
            y += w.uniform(0.18, 0.3) * M
    return leaves


def _letters(center: np.ndarray, half_width: float, height: float, seed: int):
    """Enseigne illisible : une rangée de pavés de lettres d'un ou deux pixels, quelques-uns tombés."""

    def mask(p: np.ndarray) -> np.ndarray:
        local = p - center
        cell = np.floor(local[:, 0] / (0.2 * M))
        inside = np.mod(local[:, 0], 0.2 * M)
        kept = _hash(cell, np.full_like(cell, seed)) > 0.18
        glyph = np.maximum(np.abs(inside - 0.08 * M) - 0.06 * M, np.abs(local[:, 1]) - height)
        glyph = np.where(kept, glyph, np.inf)
        return np.maximum(glyph, np.abs(local[:, 0]) - half_width)

    return mask


def building(spec: BuildingSpec) -> PropModel:
    (WALL, WALL_SHADE, GRIME, STREAK, BRICK, MORTAR, CRACK, TRIM, INTERIOR, PANE, GLINT, ROOF, GRAVEL, DOOR, AWNING,
     MOSS, MOSS_LIGHT, RUBBLE, RUBBLE_DARK, SIGN, LETTERS, BOARD, WATER, FOLIAGE, FOLIAGE_LIGHT, FLOOR, SHUTTER, IRON,
     ZINC, INNER, INNER_STRIPE, PLINTH, TILE_DARK, BARK) = range(34)
    w = Weathering(spec.seed)
    awning_hex = w.choice(["#7B3F36", "#3F6A63", "#A08040", "#4B5670"])
    sign_hex = w.choice(["#C9B37A", "#9DB0AE", "#B98C74"])
    shutter_hex = w.choice(["#4E6E5A", "#5A6E86", "#7A5A3E", "#8A8670"])
    wallpaper = w.choice(["#A8947A", "#9AA08A", "#A88A86"])
    house = spec.style == "house"
    roof_hex = "#8A5A48" if house else "#4E4A47"
    materials = [
        make_material("wall", spec.plaster),
        make_material("wall_shade", _shade(spec.plaster, 0.93), contrast=0.9),
        make_material("grime", _shade(spec.plaster, 0.7), contrast=0.9),
        make_material("streak", _shade(spec.plaster, 0.82), contrast=0.9),
        make_material("brick", "#8E5644"),
        make_material("mortar", "#B2A68E", contrast=0.7),
        make_material("crack", _shade(spec.plaster, 0.42), contrast=0.5),
        make_material("trim", "#C4BBA6"),
        make_material("interior", "#1E1A1D", contrast=0.4),
        make_material("pane", "#34424E", contrast=0.5),
        make_material("glint", "#9CB0BA", contrast=0.4),
        make_material("roof", roof_hex),
        make_material("gravel", _shade(roof_hex, 1.13), contrast=0.8),
        make_material("door", "#3A2E28", contrast=0.6),
        make_material("awning", awning_hex),
        make_material("moss", "#5A7A38"),
        make_material("moss_light", "#7E9A48", contrast=0.8),
        make_material("rubble", "#8A857C"),
        make_material("rubble_dark", "#5E5A54", contrast=0.8),
        make_material("sign", sign_hex, contrast=0.6),
        make_material("letters", "#3A3430", contrast=0.5),
        make_material("board", "#6B5238"),
        make_material("water", "#2F3A44", contrast=0.4),
        make_material("foliage", "#3E6630"),
        make_material("foliage_light", "#6E9A40", contrast=0.8),
        make_material("floor", "#77726A"),
        make_material("shutter", shutter_hex),
        make_material("iron", "#3E3C3E", contrast=0.8),
        make_material("zinc", "#7E8682"),
        make_material("inner", wallpaper),
        make_material("inner_stripe", _shade(wallpaper, 0.85), contrast=0.8),
        make_material("plinth", "#7E7A70"),
        make_material("tile_dark", _shade(roof_hex, 0.7), contrast=0.8),
        make_material("bark", "#5A4632"),
    ]

    half_w = spec.width_cells * CELL_WIDTH * 0.46
    half_d = spec.depth_rows * ROW_DEPTH * 0.46
    height = spec.storeys * STOREY
    shop = spec.style == "shop"
    ruin = spec.style == "ruin"
    boarded_share = 0.08 + spec.damage * 0.25
    facades = Facades(half_w, half_d, (STOREY if shop else 0.0) + 1.55 * M, spec.storeys - (1 if shop else 0), spec.seed,
                      spacing=spec.window_spacing, boarded_share=boarded_share, glass_share=0.45 - spec.damage * 0.35,
                      shutter_share=0.55 if house else (0.0 if ruin else 0.18),
                      balcony_share=0.35 if spec.style == "apartment" else 0.0)

    # Tirages figés une fois : le modèle doit rester identique entre ses évaluations.
    holes = []
    if spec.damage > 0.25:
        for _ in range(int(1 + spec.damage * 3)):
            holes.append((np.array([w.uniform(-0.8, 0.8) * half_w, w.uniform(0.35, 0.9) * height,
                                    half_d * w.choice([-1.0, 1.0])]), w.uniform(0.45, 0.9) * M))
    collapse = []
    corner = np.array([half_w * w.choice([-1.0, 1.0]), height, half_d * 0.2])
    if spec.damage > 0.55:
        for _ in range(4):
            # Volumes hauts : ils emportent toute la toiture au-dessus de l'angle effondré.
            size = np.array([w.uniform(0.25, 0.45) * half_w, w.uniform(0.5, 0.75) * height, half_d * 1.3])
            offset = np.array([-np.sign(corner[0]) * w.uniform(0.0, 0.35) * half_w, w.uniform(0.0, 0.3) * height, 0.0])
            # Coupes presque verticales : la ruine montre ses pièces en coupe, comme une maison de poupée.
            a = w.uniform(-0.18, 0.18)
            rotation = np.array([[np.cos(a), -np.sin(a), 0], [np.sin(a), np.cos(a), 0], [0, 0, 1]])
            collapse.append((corner + offset, size, rotation))
    # Gravats : en tas au pied de la façade (plus gros sous l'angle effondré), pas en cubes isolés.
    heaps = []
    for _ in range(int(spec.damage * 5)):
        heaps.append((np.array([w.uniform(-0.9, 0.9) * half_w, 0.0, half_d + w.uniform(0.2, 0.9) * M]),
                      np.array([w.uniform(0.5, 0.9), w.uniform(0.25, 0.45), w.uniform(0.35, 0.6)]) * M))
    if collapse:
        heaps.append((np.array([corner[0] - np.sign(corner[0]) * 0.25 * half_w, 0.0, half_d * 0.55]),
                      np.array([0.32 * half_w, 0.22 * height, half_d * 0.75])))
    # Pans de plancher qui pendent dans l'effondrement, armatures à nu au bout.
    hanging, rebars = [], []
    if collapse:
        side = np.sign(corner[0])
        edge = corner[0] - side * 0.42 * half_w
        for k in range(1, spec.storeys):
            tilt = rotation_z(side * -0.65) @ rotation_y(w.uniform(-0.15, 0.15))
            center = np.array([edge + side * 0.55 * M, k * STOREY - 0.45 * M, w.uniform(-0.2, 0.2) * half_d])
            hanging.append((center, np.array([0.65 * M, 0.1 * M, half_d * 0.55]), tilt))
            tip = center + tilt @ np.array([0.62 * M, 0.0, 0.0])
            for z in np.linspace(-0.4, 0.4, 4) * half_d:
                rebars.append((tip + np.array([0.0, 0.0, z]), tip + np.array([side * w.uniform(0.2, 0.45) * M, -w.uniform(0.15, 0.5) * M, z])))
    chunks = []
    for center, radii in heaps:
        for _ in range(5 if radii[1] < 1.0 * M else 16):
            a = w.uniform(0, 2 * np.pi)
            r = w.uniform(0.2, 0.8)
            spot = center + np.array([np.cos(a) * r * radii[0], 0.0, np.sin(a) * r * radii[2]])
            top = radii[1] * np.sqrt(max(0.0, 1.0 - r * r))
            chunks.append((spot + np.array([0.0, top, 0.0]), np.array([w.uniform(0.15, 0.32), w.uniform(0.08, 0.18), w.uniform(0.12, 0.28)]) * M,
                           rotation_y(w.uniform(0, np.pi)) @ rotation_z(w.uniform(-0.4, 0.4))))
    roof_y = height + 0.09 * M
    # Mobilier de toit : édicule d'escalier, souches de cheminée, ventilations, antenne ; jamais plus haut que 1,6 m.
    slots = [np.array([sx * 0.55 * half_w, 0.0, sz * 0.35 * half_d]) for sx in (-1, 1) for sz in (-1, 1)]
    order = [slots[i] for i in np.argsort([w.uniform(0, 1) for _ in slots])]
    stair = order[0] if not house else None
    chimneys = [order[1] + np.array([w.uniform(-0.4, 0.4) * M, 0.0, 0.0]), order[2] + np.array([0.0, 0.0, w.uniform(-0.3, 0.3) * M])]
    vents = [order[3] + np.array([w.uniform(-0.3, 0.3) * M, 0.0, w.uniform(-0.2, 0.2) * M])]
    antenna = (order[1] + np.array([0.6 * M, 0.0, 0.0])) if spec.style == "apartment" else None
    sapling = (np.array([w.uniform(-0.6, 0.6) * half_w, roof_y, w.uniform(-0.3, 0.3) * half_d])
               if spec.damage > 0.3 and not house else None)
    sapling_crown = []
    if sapling is not None:
        lean = np.array([w.uniform(-0.3, 0.3) * M, 0.0, w.uniform(-0.2, 0.2) * M])
        crown = sapling + np.array([0.0, 1.0 * M, 0.0]) + lean
        sapling_crown = [crown + np.array([w.uniform(-0.55, 0.55) * M, w.uniform(-0.35, 0.3) * M, w.uniform(-0.35, 0.35) * M]) for _ in range(7)]
    ivy = []
    for _ in range(1 + int(spec.damage * 3)):
        ivy += _ivy(w, w.uniform(-0.85, 0.85) * half_w, half_d, w.uniform(0.4, 0.75) * height)
    pipe_x = (half_w - 0.25 * M) * w.choice([-1.0, 1.0])

    def masonry(p: np.ndarray) -> np.ndarray:
        # Brèches cassées le long des blocs (plan 08 P2) : décalage constant par bloc de maçonnerie, d'où des bords
        # en escalier au lieu de trous ronds. Bloc d'environ 0,5 × 0,3 m, amplitude ±0,3 m.
        cell = np.floor(p / np.array([0.5 * M, 0.3 * M, 0.5 * M]))
        return (_hash(cell[:, 0], cell[:, 1], cell[:, 2] + spec.seed) - 0.5) * 0.6 * M

    cut_memo: dict[float, tuple[np.ndarray, np.ndarray]] = {}

    def collapse_cut(p: np.ndarray, shrink: float = 1.0) -> np.ndarray:
        """
        Volume emporté par l'effondrement. Ses faces sont rongées par un bruit lisse d'un demi-mètre : murs et
        planchers s'arrêtent en dents irrégulières au lieu de plans nets (lus comme une aile plus basse). Mémorisé
        pour le tableau de points en cours : une quinzaine de pièces l'interrogent à chaque pas.
        """
        if not collapse:
            return np.full(len(p), np.inf)
        memo = cut_memo.get(shrink)
        if memo is not None and memo[0] is p:
            return memo[1]
        d = _union(*(rounded_box(p, c, h * shrink, 0.0, r) for c, h, r in collapse))
        near = d < 0.8 * M
        if near.any():
            # Le bruit ne fait qu'agrandir la coupe : en la rétrécissant, il laissait des fragments de toit flotter en l'air.
            d[near] = (d[near] - value_noise(p[near], 0.7 * M, spec.seed + 12) * 0.8 * M) * 0.5
        cut_memo[shrink] = (p, d)
        return d

    def cut_wide(p: np.ndarray) -> np.ndarray:
        # Corniches, toiture et mobilier de toit reculent un peu plus que les murs : coupés au même endroit, leurs
        # éclats restaient suspendus au-dessus d'un mur emporté.
        return collapse_cut(p) - 0.2 * M

    def shop_front(p: np.ndarray) -> np.ndarray:
        return _box(p, (0, 1.2 * M, half_d), (half_w * 0.8, 0.95 * M, 0.2 * M))

    def door_cut(p: np.ndarray) -> np.ndarray:
        return _box(p, (-half_w * 0.35, 1.05 * M, half_d), (0.5 * M, 1.05 * M, 0.2 * M))

    def shell(p: np.ndarray) -> np.ndarray:
        outer = _box(p, (0, height / 2, 0), (half_w, height / 2, half_d))
        carve = facades.parts(p)["holes"]
        if shop:
            carve = np.minimum(carve, shop_front(p))
        for center, radius in holes:
            carve = np.minimum(carve, sphere(p, center, radius) - masonry(p))
        # Les grands plans d'effondrement restent nets : décalés par bloc, ils se couvraient de mouchetures.
        carve = np.minimum(carve, collapse_cut(p))
        return np.maximum(outer, -carve)

    def plinth(p: np.ndarray) -> np.ndarray:
        # Soubassement de pierre : une marche plus sombre en pied de façade, coupée par la porte et la vitrine.
        base = _box(p, (0, 0.32 * M, 0), (half_w + 0.05 * M, 0.32 * M, half_d + 0.05 * M))
        cut = np.minimum(collapse_cut(p), door_cut(p) if not ruin else np.inf)
        if shop:
            cut = np.minimum(cut, shop_front(p))
        return np.maximum(base, -cut)

    def interior(p: np.ndarray) -> np.ndarray:
        inner = _box(p, (0, height / 2, 0), (half_w - 0.16 * M, height / 2 - 0.05, half_d - 0.16 * M))
        return np.maximum(inner, -collapse_cut(p))

    def floors(p: np.ndarray) -> np.ndarray:
        # Planchers visibles dans les brèches, un peu moins rongés que les murs ; dans l'effondrement, un pan de
        # plancher cassé pend encore à son bord, incliné vers la rue.
        slabs = _union(*(_box(p, (0, k * STOREY, 0), (half_w - 0.05 * M, 0.12 * M, half_d - 0.05 * M))
                         for k in range(1, spec.storeys)))
        result = np.maximum(slabs, -collapse_cut(p, 0.85))
        if hanging:
            result = np.minimum(result, _union(*(rounded_box(p, c, h, 0.02 * M, r) for c, h, r in hanging)))
        return result

    def trim(p: np.ndarray) -> np.ndarray:
        bands = [_box(p, (0, k * STOREY, 0), (half_w + 0.06 * M, 0.08 * M, half_d + 0.06 * M)) for k in range(1, spec.storeys)]
        cornice = np.maximum(_box(p, (0, height + 0.12 * M, 0), (half_w + 0.1 * M, 0.14 * M, half_d + 0.1 * M)),
                             -_box(p, (0, height + 0.12 * M, 0), (half_w - 0.2 * M, 0.3 * M, half_d - 0.2 * M)))
        parts = facades.parts(p)
        pieces = [cornice, parts["sills"], parts["lintels"], *bands]
        if spec.style in ("apartment", "shop"):
            pieces.append(parts["frames"])
        return np.maximum(_union(*pieces), -cut_wide(p))

    def boards(p: np.ndarray) -> np.ndarray:
        return np.maximum(facades.parts(p)["boards"], -collapse_cut(p))

    def panes(p: np.ndarray) -> np.ndarray:
        return np.maximum(facades.parts(p)["panes"], -collapse_cut(p))

    def glints(p: np.ndarray) -> np.ndarray:
        return np.maximum(facades.parts(p)["glints"], -collapse_cut(p))

    def shutters(p: np.ndarray) -> np.ndarray:
        return np.maximum(facades.parts(p)["shutters"], -collapse_cut(p))

    def balconies(p: np.ndarray) -> np.ndarray:
        return np.maximum(facades.parts(p)["balconies"], -collapse_cut(p))

    def railings(p: np.ndarray) -> np.ndarray:
        return np.maximum(facades.parts(p)["railings"], -collapse_cut(p))

    def pitched(p: np.ndarray) -> np.ndarray:
        local = p - np.array([0, roof_y, 0])
        ridge = 1.8 * M
        slope = (np.abs(local[:, 2]) * ridge / (half_d + 0.3 * M) + local[:, 1] - ridge) / np.sqrt(1 + (ridge / half_d) ** 2)
        return np.maximum(np.maximum(slope, -local[:, 1]), np.abs(local[:, 0]) - (half_w + 0.25 * M))

    def membrane(p: np.ndarray) -> np.ndarray:
        return np.maximum(_box(p, (0, height + 0.04 * M, 0), (half_w - 0.18 * M, 0.05 * M, half_d - 0.18 * M)), -cut_wide(p))

    def roof(p: np.ndarray) -> np.ndarray:
        if house:
            d = pitched(p)
        else:
            parapet = _box(p, (0, height + 0.35 * M, 0), (half_w, 0.25 * M, half_d))
            # Le creux descend jusqu'à la membrane : un fond plein de parapet la recouvrait, gravier et mousse compris.
            well = _box(p, (0, height + 0.43 * M, 0), (half_w - 0.3 * M, 0.37 * M, half_d - 0.3 * M))
            d = np.minimum(np.maximum(parapet, -well), membrane(p))
        return np.maximum(d, -cut_wide(p))

    def roof_surface(p: np.ndarray) -> np.ndarray:
        return pitched(p) if house else membrane(p)

    def stair_house(p: np.ndarray) -> np.ndarray:
        if stair is None:
            return np.full(len(p), np.inf)
        return np.maximum(_box(p, stair + np.array([0.0, height + 0.72 * M, 0.0]), (0.8 * M, 0.72 * M, 0.65 * M)), -cut_wide(p))

    def stair_top(p: np.ndarray) -> np.ndarray:
        if stair is None:
            return np.full(len(p), np.inf)
        return np.maximum(_box(p, stair + np.array([0.0, height + 1.5 * M, 0.0]), (0.9 * M, 0.06 * M, 0.75 * M)), -cut_wide(p))

    def stair_door(p: np.ndarray) -> np.ndarray:
        if stair is None:
            return np.full(len(p), np.inf)
        return np.maximum(_box(p, stair + np.array([0.2 * M, height + 0.6 * M, 0.65 * M]), (0.28 * M, 0.5 * M, 0.04 * M)), -cut_wide(p))

    def chimney(p: np.ndarray) -> np.ndarray:
        if house:
            stacks = [_box(p, (0.45 * half_w, roof_y + 1.4 * M, -0.25 * half_d), (0.28 * M, 0.85 * M, 0.28 * M))]
        else:
            stacks = [_box(p, c + np.array([0.0, height + 0.5 * M, 0.0]), (0.24 * M, 0.5 * M, 0.24 * M)) for c in chimneys]
        return np.maximum(_union(*stacks), -cut_wide(p))

    def chimney_caps(p: np.ndarray) -> np.ndarray:
        if house:
            caps = [_box(p, (0.45 * half_w, roof_y + 2.27 * M, -0.25 * half_d), (0.34 * M, 0.05 * M, 0.34 * M))]
        else:
            caps = [_box(p, c + np.array([0.0, height + 1.02 * M, 0.0]), (0.3 * M, 0.05 * M, 0.3 * M)) for c in chimneys]
        return np.maximum(_union(*caps), -cut_wide(p))

    def metalwork(p: np.ndarray) -> np.ndarray:
        # Ventilations et descente d'eau en zinc.
        pieces = [cylinder(p, c + np.array([0.0, height + 0.32 * M, 0.0]), 0.11 * M, 0.26 * M, 0.02 * M) for c in vents]
        if spec.style in ("apartment", "shop"):
            pieces.append(capsule(p, (pipe_x, 0.1 * M, half_d + 0.09 * M), (pipe_x, height - 0.1 * M, half_d + 0.09 * M), 0.06 * M))
            pieces.append(_box(p, (pipe_x, height - 0.05 * M, half_d + 0.1 * M), (0.12 * M, 0.1 * M, 0.08 * M)))
        if shop:
            pieces.append(rolling_shutter(p))
        return np.maximum(_union(*pieces), -cut_wide(p))

    def rolling_shutter(p: np.ndarray) -> np.ndarray:
        # Rideau de fer à moitié baissé sur la vitrine.
        return _box(p, (half_w * 0.2, 1.75 * M, half_d - 0.05 * M), (half_w * 0.58, 0.4 * M, 0.03 * M))

    def antenna_part(p: np.ndarray) -> np.ndarray:
        if antenna is None:
            return np.full(len(p), np.inf)
        base = antenna + np.array([0.0, height + 0.09 * M, 0.0])
        return np.maximum(_union(capsule(p, base, base + np.array([0.0, 1.5 * M, 0.0]), 0.03 * M),
                                 capsule(p, base + np.array([-0.35 * M, 1.25 * M, 0.0]), base + np.array([0.35 * M, 1.25 * M, 0.0]), 0.025 * M),
                                 capsule(p, base + np.array([-0.25 * M, 1.0 * M, 0.0]), base + np.array([0.25 * M, 1.0 * M, 0.0]), 0.025 * M)),
                          -cut_wide(p))

    ivy_leaves = _clumps([c for c, _ in ivy], [tuple(r) for _, r in ivy], 0.035 * M, 0.2 * M) if ivy else None

    def ivy_shape(p: np.ndarray) -> np.ndarray:
        if ivy_leaves is None:
            return np.full(len(p), np.inf)
        # Borne rapide : le lierre tient dans une tranche plaquée à la façade ; loin d'elle, la distance à la tranche
        # suffit. Près du bord, on évalue la vraie forme : la borne vaut zéro sur le bord et y ferait une fausse surface.
        d = np.abs(p[:, 2] - (half_d + 0.04 * M)) - 0.4 * M
        near = d < 0.2 * M
        if near.any():
            d[near] = np.maximum(ivy_leaves(p[near]), -collapse_cut(p[near]))
        return d

    def foliage_part(p: np.ndarray) -> np.ndarray:
        if sapling is None:
            return np.full(len(p), np.inf)
        return _clumps(sapling_crown, [(0.34 * M, 0.28 * M, 0.3 * M)] * len(sapling_crown), 0.05 * M, 0.28 * M)(p)

    def bark_part(p: np.ndarray) -> np.ndarray:
        if sapling is None:
            return np.full(len(p), np.inf)
        # Arbuste poussé dans une fissure du toit : trois tiges courtes qui partent du même pied.
        return _union(*(capsule(p, sapling, sapling + (c - sapling) * 0.75, 0.07 * M, 0.035 * M) for c in sapling_crown[:3]))

    def door(p: np.ndarray) -> np.ndarray:
        return _box(p, (-half_w * 0.35, 1.05 * M, half_d - 0.08 * M), (0.5 * M, 1.05 * M, 0.12 * M))

    def door_frame(p: np.ndarray) -> np.ndarray:
        ring = _box(p, (-half_w * 0.35, 1.08 * M, half_d + 0.02 * M), (0.6 * M, 1.12 * M, 0.04 * M))
        return np.maximum(ring, -_box(p, (-half_w * 0.35, 1.05 * M, half_d), (0.5 * M, 1.06 * M, 0.3 * M)))

    def awning(p: np.ndarray, stripe: int) -> np.ndarray:
        cloth = rounded_box(p, (0, 2.35 * M, half_d + 0.55 * M), (half_w * 0.82, 0.05 * M, 0.6 * M), 0.02 * M,
                            rotation_x(0.3))
        # Rayures du store : une bande sur deux dans chaque matériau.
        band = np.floor(p[:, 0] / (0.45 * M)).astype(np.int64) % 2
        return np.where(band == stripe, cloth, np.inf)

    sign_center = np.array([0.0, 2.75 * M, half_d + 0.18 * M])

    def sign(p: np.ndarray) -> np.ndarray:
        return _box(p, (0, 2.75 * M, half_d + 0.1 * M), (half_w * 0.55, 0.28 * M, 0.08 * M))

    def heaps_part(p: np.ndarray) -> np.ndarray:
        if not heaps:
            return np.full(len(p), np.inf)
        piles = _union(*(ellipsoid(p, c, r) for c, r in heaps))
        # Surface bosselée de blocs : un bruit à l'échelle d'un moellon, pas une calotte lisse.
        near = piles < 0.4 * M
        if near.any():
            piles[near] += (value_noise(p[near], 0.28 * M, spec.seed + 11) - 0.5) * 0.3 * M
        return np.maximum(piles * 0.8, -p[:, 1])

    def chunks_part(p: np.ndarray) -> np.ndarray:
        if not chunks:
            return np.full(len(p), np.inf)
        # Borne rapide : les blocs reposent sur les tas ; loin d'eux, la distance au tas moins leur taille suffit
        # (évaluation exacte près du bord, comme pour le lierre).
        d = _union(*(ellipsoid(p, c, r) for c, r in heaps)) - 0.6 * M
        near = d < 0.2 * M
        if near.any():
            q = p[near]
            d[near] = _union(*(rounded_box(q, c, h, 0.04 * M, r) for c, h, r in chunks))
        return d

    # Matières de la façade : chaque couche l'emporte sur la précédente par une épaisseur de peinture un peu plus forte.
    patches = noise_mask(1.1 * M, spec.seed, 0.35)
    damp = lambda p: p[:, 1] - (0.75 * M + value_noise(p, 0.55 * M, spec.seed + 1) * 0.8 * M)
    runs = both(noise_mask(0.15 * M, spec.seed + 2, 0.22, (1.0, 9.0, 1.0)), lambda p: p[:, 1] - (height - 0.15 * M),
                lambda p: 1.4 * M - p[:, 1])
    bare = noise_mask(0.8 * M, spec.seed + 3, 0.03 + spec.damage * 0.3)
    joints = bricks(0.2 * M, 0.5 * M, 0.045 * M)
    split = cracks(0.9 * M, spec.seed + 5, 0.022 * M, reach=0.1 + spec.damage * 0.45)
    near_collapse = lambda p: np.abs(collapse_cut(p)) - 0.35 * M
    roof_moss = noise_mask(0.45 * M, spec.seed + 6, (0.05 if house else 0.12) + 0.25 * spec.damage)
    # Flaques sur le toit plat : l'eau stagne là où la membrane s'est affaissée.
    puddles = noise_mask(0.7 * M, spec.seed + 10, 0.04)

    def parts() -> list[Part]:
        result = [
            # L'intérieur sombre passe avant la coque : sur une face de cassure, il l'emporte à distance égale.
            Part(interior, INTERIOR), Part(shell, WALL),
            Part(painted(shell, patches, 0.010 * M), WALL_SHADE, relief=False),
            Part(painted(shell, runs, 0.013 * M), STREAK, relief=False),
            Part(painted(shell, damp, 0.016 * M), GRIME, relief=False),
            Part(painted(shell, bare, 0.02 * M), BRICK),
            Part(painted(shell, both(bare, joints), 0.026 * M), MORTAR),
            Part(painted(shell, split, 0.03 * M), CRACK),
            Part(painted(interior, near_collapse, 0.02 * M), INNER, relief=False),
            Part(painted(interior, both(near_collapse, bands(0, 0.32 * M, 0.08 * M)), 0.026 * M), INNER_STRIPE, relief=False),
            Part(floors, FLOOR), Part(trim, TRIM), Part(boards, BOARD), Part(panes, PANE), Part(glints, GLINT),
            Part(shutters, SHUTTER), Part(painted(shutters, bands(1, 0.1 * M, 0.03 * M), 0.012 * M), IRON),
            Part(balconies, TRIM), Part(railings, IRON),
            Part(roof, ROOF),
            Part(painted(roof_surface, noise_mask(0.11 * M, spec.seed + 4, 0.35), 0.01 * M), GRAVEL, relief=False),
            Part(painted(roof_surface, roof_moss, 0.02 * M), MOSS, relief=False),
            Part(painted(roof_surface, both(roof_moss, noise_mask(0.18 * M, spec.seed + 7, 0.4)), 0.024 * M), MOSS_LIGHT, relief=False),

            Part(stair_house, WALL), Part(stair_top, TRIM), Part(stair_door, DOOR),
            Part(chimney, BRICK), Part(painted(chimney, bricks(0.18 * M, 0.42 * M, 0.04 * M), 0.012 * M), MORTAR),
            Part(chimney_caps, TRIM), Part(metalwork, ZINC), Part(antenna_part, IRON),
            Part(plinth, PLINTH), Part(painted(plinth, bricks(0.32 * M, 0.75 * M, 0.04 * M), 0.012 * M), RUBBLE_DARK),
            Part(ivy_shape, FOLIAGE), Part(painted(ivy_shape, noise_mask(0.22 * M, spec.seed + 8, 0.4), 0.02 * M), FOLIAGE_LIGHT),
            Part(foliage_part, FOLIAGE), Part(painted(foliage_part, lambda p: (sapling_crown[0][1] if sapling_crown else 0.0) - p[:, 1]
                                                      + noise_mask(0.2 * M, spec.seed + 9, 0.5)(p), 0.02 * M), FOLIAGE_LIGHT),
            Part(bark_part, BARK),
            Part(heaps_part, RUBBLE_DARK), Part(chunks_part, RUBBLE),
            Part(lambda p: _union(*(capsule(p, a, b, 0.025 * M) for a, b in rebars)) if rebars else np.full(len(p), np.inf), BARK),
        ]
        if not house:
            result.append(Part(painted(membrane, puddles, 0.022 * M), WATER, relief=False))
        if house:
            result.append(Part(painted(pitched, bricks(0.24 * M, 0.36 * M, 0.045 * M, frame=lambda q: np.stack(
                [q[:, 0], q[:, 1], np.zeros(len(q))], axis=1)), 0.012 * M), TILE_DARK))
        if not ruin:
            result += [Part(door, DOOR), Part(door_frame, TRIM)]
        if shop:
            result += [Part(lambda p: awning(p, 0), AWNING), Part(lambda p: awning(p, 1), TRIM), Part(sign, SIGN),
                       Part(painted(sign, _letters(sign_center, half_w * 0.45, 0.12 * M, spec.seed), 0.02 * M), LETTERS),
                       Part(painted(rolling_shutter, bands(1, 0.12 * M, 0.035 * M), 0.012 * M), IRON),
                       Part(lambda p: _box(p, (0, 1.2 * M, half_d - 0.14 * M), (half_w * 0.8, 0.95 * M, 0.02 * M)), PANE)]
        return result

    extent_x = half_w + 1.4 * M
    extent_z = half_d + 1.6 * M
    top = height + 3.2 * M
    width_px = int((extent_x * 2) * 0.62 * 1.05) + 24
    height_px = int(top * 0.62 * 0.87 + extent_z * 2 * 0.31) + 30
    yaw = -BUILDING_YAW if spec.mirrored else BUILDING_YAW
    return PropModel(spec.stem, parts, materials, yaw,
                     ray_range=float(2.5 * max(extent_x, extent_z, top)),
                     canvas=(width_px, height_px),
                     footprint=box_footprint(half_w, half_d),
                     bounds=((-extent_x, 0.0, -extent_z), (extent_x, top, extent_z)),
                     supersample=3, smooth_slopes=True)


def church(stem: str, seed: int, mirrored: bool) -> PropModel:
    """Église de quartier, repère rare (plan 08 P2) : nef longée de contreforts et de hautes baies en ogive, clocher
    à une extrémité avec abat-sons et flèche basse, croix penchée. Toit de la nef crevé, gravats devant le portail.
    Le clocher dépasse les immeubles voisins sans sortir de la hauteur permise (~190 px)."""
    STONE, TRIM, GLASS, ROOF, DOOR, MOSS, RUBBLE, INTERIOR, BRONZE, BEAM, JOINT, GRIME, SLATE = range(13)
    materials = [
        make_material("stone", "#9C9384"), make_material("trim", "#BDB39C"),
        make_material("glass", "#3A3456", contrast=0.6), make_material("roof", "#4E4A54"),
        make_material("door", "#3A2E28", contrast=0.6), make_material("moss", "#5A7A38"),
        make_material("rubble", "#8A857C"), make_material("interior", "#1E1A1D", contrast=0.4),
        make_material("bronze", "#8A6A3A"), make_material("beam", "#6E4E36", contrast=0.7),
        make_material("joint", "#7A7266", contrast=0.6), make_material("grime", _shade("#9C9384", 0.72), contrast=0.9),
        make_material("slate", "#3E3A44", contrast=0.7),
    ]
    w = Weathering(seed)
    half_w = 4 * CELL_WIDTH * 0.46
    half_d = DEPTH_ROWS * ROW_DEPTH * 0.46
    tower = 1.35 * M
    tower_x = -half_w + tower
    nave_x0 = tower_x + tower
    nave_cx = (nave_x0 + half_w) / 2
    nave_hw = (half_w - nave_x0) / 2
    nave_d = half_d * 0.8
    wall_h = 4.6 * M
    ridge = 2.2 * M
    tower_h = 7.6 * M
    bays = [nave_x0 + (k + 0.5) * (2 * nave_hw) / 4 for k in range(4)]
    breach = np.array([nave_cx + w.uniform(-0.3, 0.3) * nave_hw, wall_h + 1.2 * M, w.uniform(-0.3, 0.3) * nave_d])
    rubble = [(np.array([w.uniform(-1.0, 1.0) * half_w, 0.15 * M, half_d + w.uniform(0.1, 1.0) * M]),
               np.array([w.uniform(0.2, 0.45), w.uniform(0.15, 0.3), w.uniform(0.2, 0.4)]) * M, w.uniform(0, np.pi))
              for _ in range(6)]
    ivy = _ivy(w, w.uniform(0.1, 0.4) * half_w, nave_d, 2.6 * M) + _ivy(w, w.uniform(0.55, 0.85) * half_w, nave_d, 1.8 * M)

    def ogive(p: np.ndarray, x: float, y0: float, height: float, half_width: float, z: float, depth: float) -> np.ndarray:
        # Baie en ogive : fente droite coiffée d'un arc (deux sphères qui se recoupent donnent la pointe).
        slot = _box(p, (x, y0 + height / 2, z), (half_width, height / 2, depth))
        top = y0 + height
        arch = np.maximum(sphere(p, (x - half_width * 0.6, top, z), half_width * 1.6),
                          sphere(p, (x + half_width * 0.6, top, z), half_width * 1.6))
        return np.minimum(slot, np.maximum(arch, top - p[:, 1]))

    def openings(p: np.ndarray) -> np.ndarray:
        bays_cut = _union(*(ogive(p, x, 1.3 * M, 2.1 * M, 0.3 * M, nave_d, 0.3 * M) for x in bays))
        belfry = _union(ogive(p, tower_x, 5.5 * M, 1.1 * M, 0.35 * M, tower, 0.3 * M),
                        ogive(p, tower_x - tower, 5.5 * M, 1.1 * M, 0.3 * M, 0, 0.3 * M))
        portal = ogive(p, tower_x, 0.0, 2.0 * M, 0.55 * M, tower, 0.25 * M)
        return _union(bays_cut, belfry, portal)

    def breach_cut(p: np.ndarray) -> np.ndarray:
        # Brèche cassée le long des tuiles (rangs de 0,3 m, tuiles de 0,4 m), comme les immeubles (plan 08 P2) :
        # un bord en dents de scie au lieu d'un disque net.
        cell = np.floor(p / np.array([0.4 * M, 0.3 * M, 0.3 * M]))
        jag = (_hash(cell[:, 0], cell[:, 1], cell[:, 2] + seed) - 0.5) * 0.8 * M
        return sphere(p, breach, 1.3 * M) - jag

    def nave_roof(p: np.ndarray) -> np.ndarray:
        local = p - np.array([nave_cx, wall_h, 0])
        slope = (np.abs(local[:, 2]) * ridge / (nave_d + 0.3 * M) + local[:, 1] - ridge) / np.sqrt(1 + (ridge / nave_d) ** 2)
        return np.maximum(np.maximum(slope, -local[:, 1]), np.abs(local[:, 0]) - (nave_hw + 0.2 * M))

    def spire(p: np.ndarray) -> np.ndarray:
        # Flèche basse : pyramide à quatre pans au-dessus de la corniche du clocher.
        local = p - np.array([tower_x, tower_h, 0])
        height = 1.5 * M
        r = tower + 0.15 * M
        side = (np.maximum(np.abs(local[:, 0]), np.abs(local[:, 2])) * height / r + local[:, 1] - height) / np.sqrt(1 + (height / r) ** 2)
        return np.maximum(side, -local[:, 1])

    def walls(p: np.ndarray) -> np.ndarray:
        nave = _box(p, (nave_cx, wall_h / 2, 0), (nave_hw, wall_h / 2, nave_d))
        # Pignons seuls aux deux bouts : un comble plein se verrait par la brèche du toit.
        ends = np.abs(np.abs(p[:, 0] - nave_cx) - (nave_hw - 0.15 * M)) - 0.15 * M
        gable = np.maximum(nave_roof(p - np.array([0, -0.05 * M, 0])), ends)
        body = _union(nave, gable, _box(p, (tower_x, tower_h / 2, 0), (tower, tower_h / 2, tower)))
        buttresses = _union(*(_box(p, (x + (2 * nave_hw) / 8, 1.4 * M, nave_d + 0.25 * M), (0.22 * M, 1.4 * M, 0.3 * M))
                              for x in bays[:-1]))
        return np.maximum(_union(body, buttresses), -_union(openings(p), breach_cut(p)))

    def inside(p: np.ndarray) -> np.ndarray:
        under_roof = np.maximum(nave_roof(p + np.array([0, 0.2 * M, 0])), np.abs(p[:, 0] - nave_cx) - (nave_hw - 0.3 * M))
        return _union(_box(p, (nave_cx, wall_h / 2, 0), (nave_hw - 0.2 * M, wall_h / 2, nave_d - 0.2 * M)), under_roof,
                      _box(p, (tower_x, tower_h / 2, 0), (tower - 0.2 * M, tower_h / 2, tower - 0.2 * M)))

    def glass(p: np.ndarray) -> np.ndarray:
        return _union(*(ogive(p, x, 1.3 * M, 2.1 * M, 0.3 * M, nave_d - 0.12 * M, 0.04 * M) for x in bays[1:]))

    def trim(p: np.ndarray) -> np.ndarray:
        cornice = _box(p, (tower_x, tower_h, 0), (tower + 0.15 * M, 0.12 * M, tower + 0.15 * M))
        band = _box(p, (tower_x, 5.1 * M, 0), (tower + 0.08 * M, 0.08 * M, tower + 0.08 * M))
        eaves = _box(p, (nave_cx, wall_h, nave_d + 0.1 * M), (nave_hw, 0.1 * M, 0.12 * M))
        return np.maximum(_union(cornice, band, eaves), -openings(p))

    def roof(p: np.ndarray) -> np.ndarray:
        return _union(np.maximum(nave_roof(p), -breach_cut(p)), spire(p))

    def rafters(p: np.ndarray) -> np.ndarray:
        # Chevrons restés en place sous les tuiles tombées : la brèche se lit comme un toit crevé, pas comme un trou.
        under = np.abs(nave_roof(p + np.array([0, 0.2 * M, 0]))) - 0.08 * M
        strips = _union(*(np.abs(p[:, 0] - (breach[0] + k * 0.75 * M)) - 0.08 * M for k in (-1, 0, 1)))
        return np.maximum(np.maximum(under, strips), sphere(p, breach, 1.6 * M))

    def cross(p: np.ndarray) -> np.ndarray:
        # Croix de fer penchée par le temps.
        base = np.array([tower_x, tower_h + 1.5 * M, 0])
        tilt = rotation_z(0.28)
        local = (p - base) @ tilt
        return _union(capsule(local, (0, 0, 0), (0, 0.9 * M, 0), 0.07 * M),
                      capsule(local, (-0.3 * M, 0.6 * M, 0), (0.3 * M, 0.6 * M, 0), 0.06 * M))

    def bell(p: np.ndarray) -> np.ndarray:
        return ellipsoid(p, (tower_x, 5.9 * M, 0), (0.45 * M, 0.5 * M, 0.45 * M))

    def door(p: np.ndarray) -> np.ndarray:
        return ogive(p, tower_x, 0.0, 2.0 * M, 0.5 * M, tower - 0.1 * M, 0.08 * M)

    leaves = _clumps([c for c, _ in ivy], [tuple(r) for _, r in ivy], 0.035 * M, 0.2 * M)

    def moss(p: np.ndarray) -> np.ndarray:
        d = np.abs(p[:, 2] - (nave_d + 0.04 * M)) - 0.4 * M
        near = d < 0.2 * M
        if near.any():
            d[near] = leaves(p[near])
        return d

    def rubble_part(p: np.ndarray) -> np.ndarray:
        return _union(*(rounded_box(p, c, h, 0.05 * M, rotation_y(a)) for c, h, a in rubble))

    # Variante b : lacet opposé et clocher à l'autre bout (miroir en x), pour qu'il reste du côté proche de la caméra
    # et que la silhouette ne dépasse pas la hauteur permise.
    flip = np.array([-1.0 if mirrored else 1.0, 1.0, 1.0])

    # Appareil de pierre de taille, pied sali par l'humidité, rangs d'ardoises sur les toits.
    ashlar = bricks(0.36 * M, 0.7 * M, 0.04 * M)
    damp = lambda p: p[:, 1] - (0.9 * M + value_noise(p, 0.6 * M, seed + 1) * 0.9 * M)
    slates = bricks(0.26 * M, 0.42 * M, 0.04 * M, frame=lambda q: np.stack([q[:, 0], q[:, 1], np.zeros(len(q))], axis=1))

    def parts() -> list[Part]:
        volumes = [(inside, INTERIOR, True), (walls, STONE, True), (painted(walls, ashlar, 0.012 * M), JOINT, True),
                   (painted(walls, damp, 0.016 * M), GRIME, False), (glass, GLASS, True), (trim, TRIM, True), (roof, ROOF, True),
                   (painted(roof, slates, 0.012 * M), SLATE, True), (rafters, BEAM, True), (cross, BRONZE, True),
                   (bell, BRONZE, True), (door, DOOR, True), (moss, MOSS, True), (rubble_part, RUBBLE, True)]
        return [Part(lambda p, f=f: f(p * flip), material, relief) for f, material, relief in volumes]

    extent_x = half_w + 1.4 * M
    extent_z = half_d + 1.6 * M
    top = tower_h + 2.8 * M
    return PropModel(stem, parts, materials, -BUILDING_YAW if mirrored else BUILDING_YAW,
                     ray_range=float(2.5 * max(extent_x, extent_z, top)),
                     canvas=(int(extent_x * 2 * 0.62 * 1.05) + 24, int(top * 0.62 * 0.87 + extent_z * 2 * 0.31) + 30),
                     footprint=box_footprint(half_w, nave_d),
                     bounds=((-extent_x, 0.0, -extent_z), (extent_x, top, extent_z)),
                     supersample=3, smooth_slopes=True)


def radio_mast(stem: str, seed: int) -> PropModel:
    """Pylône de télécommunication, repère rare des cours d'îlot (plan 08 P2) : treillis à trois pieds qui s'affine,
    bandes rouges et blanches, paraboles, feu de balisage éteint, local technique au pied. Visible par-dessus les toits."""
    RED, WHITE, DISH, CABIN, LAMP = range(5)
    materials = [make_material("red", "#A0443A"), make_material("white", "#C8C0B4"),
                 make_material("dish", "#B4B0A6", contrast=0.8), make_material("cabin", "#8A857C"),
                 make_material("lamp", "#6A2A26", contrast=0.6)]
    w = Weathering(seed)
    top = 12.5 * M
    feet = [(np.cos(a) * 1.0 * M, 0.0, np.sin(a) * 1.0 * M) for a in (np.pi / 2, np.pi / 2 + 2 * np.pi / 3, np.pi / 2 + 4 * np.pi / 3)]
    heads = [(x * 0.22, top, z * 0.22) for x, _, z in feet]
    legs = list(zip(feet, heads))
    braces = []
    for k in range(1, 8):
        t = k / 8
        ring = [tuple(np.array(a) + (np.array(b) - np.array(a)) * t) for a, b in legs]
        braces += list(zip(ring, ring[1:] + ring[:1]))
    missing = int(w.uniform(3, len(braces) - 1))
    braces = braces[:missing] + braces[missing + 1:]
    dishes = [((0.55 * M, 8.6 * M, 0.35 * M), 0.2), ((-0.4 * M, 10.3 * M, 0.45 * M), -0.5)]

    def lattice(p: np.ndarray) -> np.ndarray:
        return _union(*(capsule(p, a, b, 0.07 * M) for a, b in legs + braces),
                      capsule(p, (0, top, 0), (0, top + 1.6 * M, 0), 0.05 * M))

    def banded(p: np.ndarray, band: int) -> np.ndarray:
        # Bandes de balisage de 1,6 m, rouge en haut.
        stripe = np.floor((top - p[:, 1]) / (1.6 * M)).astype(np.int64) % 2
        return np.where(stripe == band, lattice(p), np.inf)

    def dish_part(p: np.ndarray) -> np.ndarray:
        return _union(*(ellipsoid(p, c, (0.55 * M, 0.55 * M, 0.12 * M), rotation_y(a)) for c, a in dishes))

    def cabin(p: np.ndarray) -> np.ndarray:
        return rounded_box(p, (1.4 * M, 0.9 * M, 0.9 * M), (0.8 * M, 0.9 * M, 0.6 * M), 0.05 * M)

    def lamp(p: np.ndarray) -> np.ndarray:
        return sphere(p, (0, top + 1.65 * M, 0), 0.14 * M)

    def parts() -> list[Part]:
        return [Part(lambda p: banded(p, 0), RED), Part(lambda p: banded(p, 1), WHITE), Part(dish_part, DISH),
                Part(cabin, CABIN), Part(lamp, LAMP)]

    height = top + 2.0 * M
    return PropModel(stem, parts, materials, AXIS_X_YAW, ray_range=float(2.5 * height),
                     canvas=(100, int(height * 0.62 * 0.87 / 0.68) + 20), footprint=box_footprint(1.1 * M, 1.1 * M),
                     bounds=((-1.5 * M, 0.0, -1.5 * M), (2.4 * M, height, 1.8 * M)), smooth_slopes=True)


def catalog() -> list[PropModel]:
    """Nom : prop_bld_<style>[_damaged]_w<largeur en cellules>_<a|b> (b = lacet opposé, pour varier les rues)."""
    specs = []
    seed = 500
    for style, widths, storeys, damage in (
        ("apartment", (4, 5), 2, 0.15),
        ("apartment", (4,), 2, 0.45),
        ("shop", (3, 4), 2, 0.2),
        ("house", (3,), 2, 0.1),
        ("ruin", (3, 4), 2, 0.8),
    ):
        for width in widths:
            for mirrored in (False, True):
                seed += 1
                plaster = PLASTERS[seed % len(PLASTERS)]
                damaged = "_damaged" if style != "ruin" and damage > 0.4 else ""
                stem = f"prop_bld_{style}{damaged}_w{width}_{'b' if mirrored else 'a'}"
                specs.append(BuildingSpec(stem, width, DEPTH_ROWS, storeys, style, plaster, damage, seed, mirrored))
    # Repères rares (plan 08 P2) : l'église est un module de rangée, le pylône se plante dans une cour.
    return [building(spec) for spec in specs] + [church("prop_bld_church_w4_a", 541, False), church("prop_bld_church_w4_b", 542, True),
                                                 radio_mast("prop_radio_mast", 551)]
