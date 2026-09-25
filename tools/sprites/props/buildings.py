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
from ..sdf import rotation_x, rotation_y, rounded_box, sphere
from ._kit import M, PropModel, Weathering, box_footprint

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


class Facades:
    """Grille de fenêtres des quatre faces : distances d'alvéole, d'appui et de planches, par fenêtre tirée."""

    def __init__(self, half_w: float, half_d: float, first_row: float, rows: int, seed: int,
                 spacing: float = 1.6 * M, win_w: float = 0.42 * M, win_h: float = 0.62 * M):
        self.half_w, self.half_d = half_w, half_d
        self.first_row, self.rows, self.seed = first_row, rows, seed
        self.spacing, self.win_w, self.win_h = spacing, win_w, win_h

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
            du = np.abs(along - (start + column * self.spacing))
            dn = np.abs(across) - half_across
            side = np.sign(across) + 2 * face_id
            faces.append((du, dn, _hash(column, row, side, np.full_like(x, self.seed))))
        return y - row_y, faces

    def distances(self, p: np.ndarray, boarded_share: float) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
        """(alvéoles à creuser, planches clouées, appuis de fenêtre)."""
        dy, faces = self._cells(p)
        holes, boards, sills = [], [], []
        for du, dn, noise in faces:
            window = np.maximum(np.maximum(du - self.win_w, np.abs(dy) - self.win_h), np.abs(dn) - 0.2 * M)
            boarded = noise < boarded_share
            holes.append(np.where(boarded, np.inf, window))
            plank = np.maximum(np.maximum(du - self.win_w * 0.95, np.abs(dy) - self.win_h * 0.9), np.abs(dn - 0.02 * M) - 0.04 * M)
            boards.append(np.where(boarded, plank, np.inf))
            sills.append(np.maximum(np.maximum(du - self.win_w - 0.08 * M, np.abs(dy + self.win_h + 0.06 * M) - 0.06 * M),
                                    np.abs(dn - 0.05 * M) - 0.07 * M))
        return _union(*holes), _union(*boards), _union(*sills)


def building(spec: BuildingSpec) -> PropModel:
    (WALL, TRIM, GLASS, ROOF, DOOR, AWNING, MOSS, RUBBLE, SIGN, BOARD, GRIME, WATER, FOLIAGE,
     FLOOR) = range(14)
    w = Weathering(spec.seed)
    awning_hex = w.choice(["#7B3F36", "#3F6A63", "#A08040", "#4B5670"])
    materials = [
        make_material("wall", spec.plaster),
        make_material("trim", "#C4BBA6"),
        make_material("interior", "#1E1A1D", contrast=0.4),
        make_material("roof", "#7E5646" if spec.style == "house" else "#55504B"),
        make_material("door", "#3A2E28", contrast=0.6),
        make_material("awning", awning_hex),
        make_material("moss", "#5A7A38"),
        make_material("rubble", "#8A857C"),
        make_material("sign", w.choice(["#C9B37A", "#9DB0AE", "#B98C74"]), contrast=0.6),
        make_material("board", "#6B5238"),
        make_material("grime", spec.plaster, contrast=1.0),
        make_material("water", "#2F3A44", contrast=0.4),
        make_material("foliage", "#4E7A34"),
        make_material("floor", "#77726A"),
    ]
    # La crasse : même enduit, assombri et refroidi.
    grime = materials[GRIME]
    materials[GRIME] = type(grime)("grime", tuple(tuple(int(c * 0.72) for c in tone) for tone in grime.ramp),
                                   grime.outline, grime.inner_line)

    half_w = spec.width_cells * CELL_WIDTH * 0.46
    half_d = spec.depth_rows * ROW_DEPTH * 0.46
    height = spec.storeys * STOREY
    shop = spec.style == "shop"
    facades = Facades(half_w, half_d, (STOREY if shop else 0.0) + 1.55 * M, spec.storeys - (1 if shop else 0), spec.seed)
    boarded_share = 0.08 + spec.damage * 0.25

    # Tirages figés une fois : le modèle doit rester identique entre ses évaluations.
    holes = []
    if spec.damage > 0.25:
        for _ in range(int(1 + spec.damage * 3)):
            holes.append((np.array([w.uniform(-0.8, 0.8) * half_w, w.uniform(0.35, 0.9) * height,
                                    half_d * w.choice([-1.0, 1.0])]), w.uniform(0.45, 0.9) * M))
    collapse = []
    if spec.damage > 0.55:
        corner = np.array([half_w * w.choice([-1.0, 1.0]), height, half_d * 0.2])
        for _ in range(4):
            # Volumes hauts : ils emportent toute la toiture au-dessus de l'angle effondré.
            size = np.array([w.uniform(0.25, 0.45) * half_w, w.uniform(0.5, 0.75) * height, half_d * 1.3])
            offset = np.array([-np.sign(corner[0]) * w.uniform(0.0, 0.35) * half_w, w.uniform(0.0, 0.3) * height, 0.0])
            a = w.uniform(-0.5, 0.5)
            rotation = np.array([[np.cos(a), -np.sin(a), 0], [np.sin(a), np.cos(a), 0], [0, 0, 1]])
            collapse.append((corner + offset, size, rotation))
    rubble = []
    for _ in range(int(spec.damage * 12)):
        rubble.append((np.array([w.uniform(-1.0, 1.0) * half_w, 0.15 * M, half_d + w.uniform(0.1, 1.1) * M]),
                       np.array([w.uniform(0.2, 0.5), w.uniform(0.15, 0.35), w.uniform(0.2, 0.45)]) * M,
                       w.uniform(0, np.pi)))
    # Dessus de la dalle de toit (sous le garde-corps) : mousse, flaques et arbuste y reposent.
    roof_y = height + 0.2 * M
    roof_moss = [np.array([w.uniform(-0.8, 0.8) * half_w, roof_y, w.uniform(-0.8, 0.8) * half_d]) for _ in range(4)]
    puddles = [np.array([w.uniform(-0.6, 0.6) * half_w, roof_y, w.uniform(-0.6, 0.6) * half_d]) for _ in range(2)]
    roof_items = [np.array([w.uniform(-0.6, 0.6) * half_w, height + 0.5 * M, w.uniform(-0.5, 0.5) * half_d])
                  for _ in range(2 if spec.style == "apartment" else 1)]
    sapling = (np.array([w.uniform(-0.5, 0.5) * half_w, roof_y, w.uniform(-0.4, 0.4) * half_d])
               if spec.damage > 0.3 and spec.style != "house" else None)
    base_grime = [(np.array([w.uniform(-0.9, 0.9) * half_w, 0.2 * M, half_d]), w.uniform(0.6, 1.0) * M) for _ in range(4)]
    streaks = [np.array([w.uniform(-0.85, 0.85) * half_w, w.uniform(0.3, 0.8) * height, half_d]) for _ in range(5)]
    ivy = [np.array([w.uniform(-0.9, 0.9) * half_w, w.uniform(0.25, 0.55) * height, half_d]) for _ in range(2 + int(spec.damage * 3))]

    def collapse_cut(p: np.ndarray, shrink: float = 1.0) -> np.ndarray:
        if not collapse:
            return np.full(len(p), np.inf)
        return _union(*(rounded_box(p, c, h * shrink, 0.0, r) for c, h, r in collapse))

    def shell(p: np.ndarray) -> np.ndarray:
        outer = _box(p, (0, height / 2, 0), (half_w, height / 2, half_d))
        carve, _, _ = facades.distances(p, boarded_share)
        if shop:
            carve = np.minimum(carve, _box(p, (0, 1.2 * M, half_d), (half_w * 0.8, 0.95 * M, 0.2 * M)))
        for center, radius in holes:
            carve = np.minimum(carve, sphere(p, center, radius))
        carve = np.minimum(carve, collapse_cut(p))
        return np.maximum(outer, -carve)

    def interior(p: np.ndarray) -> np.ndarray:
        inner = _box(p, (0, height / 2, 0), (half_w - 0.16 * M, height / 2 - 0.05, half_d - 0.16 * M))
        return np.maximum(inner, -collapse_cut(p))

    def floors(p: np.ndarray) -> np.ndarray:
        # Planchers visibles dans les brèches, un peu moins rongés que les murs.
        slabs = _union(*(_box(p, (0, k * STOREY, 0), (half_w - 0.05 * M, 0.12 * M, half_d - 0.05 * M))
                         for k in range(1, spec.storeys)))
        return np.maximum(slabs, -collapse_cut(p, 0.8))

    def trim(p: np.ndarray) -> np.ndarray:
        bands = [_box(p, (0, k * STOREY, 0), (half_w + 0.06 * M, 0.08 * M, half_d + 0.06 * M)) for k in range(1, spec.storeys)]
        cornice = np.maximum(_box(p, (0, height + 0.12 * M, 0), (half_w + 0.1 * M, 0.14 * M, half_d + 0.1 * M)),
                             -_box(p, (0, height + 0.12 * M, 0), (half_w - 0.2 * M, 0.3 * M, half_d - 0.2 * M)))
        _, _, sills = facades.distances(p, boarded_share)
        return np.maximum(_union(cornice, sills, *bands), -collapse_cut(p))

    def boards(p: np.ndarray) -> np.ndarray:
        _, planks, _ = facades.distances(p, boarded_share)
        return np.maximum(planks, -collapse_cut(p))

    def roof(p: np.ndarray) -> np.ndarray:
        if spec.style == "house":
            local = p - np.array([0, roof_y, 0])
            ridge = 1.8 * M
            slope = (np.abs(local[:, 2]) * ridge / (half_d + 0.3 * M) + local[:, 1] - ridge) / np.sqrt(1 + (ridge / half_d) ** 2)
            d = np.maximum(np.maximum(slope, -local[:, 1]), np.abs(local[:, 0]) - (half_w + 0.25 * M))
        else:
            parapet = _box(p, (0, height + 0.35 * M, 0), (half_w, 0.25 * M, half_d))
            well = _box(p, (0, height + 0.5 * M, 0), (half_w - 0.3 * M, 0.3 * M, half_d - 0.3 * M))
            d = np.maximum(parapet, -well)
            d = np.minimum(d, _union(*(_box(p, c, (0.55 * M, 0.45 * M, 0.45 * M)) for c in roof_items)))
        return np.maximum(d, -collapse_cut(p))

    def grime_part(p: np.ndarray) -> np.ndarray:
        stains = [sphere(p, c - np.array([0, 0, 0.9 * r]), r) for c, r in base_grime]
        runs = [_box(p, c, (0.12 * M, 0.9 * M, 0.02 * M)) for c in streaks]
        return np.maximum(_union(*stains, *runs), -collapse_cut(p))

    def moss_part(p: np.ndarray) -> np.ndarray:
        if spec.style == "house":
            blobs = []
        else:
            blobs = [rounded_box(p, c, (0.6 * M, 0.06 * M, 0.5 * M), 0.05 * M) for c in roof_moss]
        vines = [rounded_box(p, c, (0.3 * M, 1.1 * M, 0.08 * M), 0.1 * M) for c in ivy]
        return np.maximum(_union(*blobs, *vines), -collapse_cut(p))

    def water_part(p: np.ndarray) -> np.ndarray:
        if spec.style == "house":
            return np.full(len(p), np.inf)
        return np.maximum(_union(*(rounded_box(p, c, (0.7 * M, 0.035 * M, 0.45 * M), 0.03 * M) for c in puddles)),
                          -collapse_cut(p))

    def foliage_part(p: np.ndarray) -> np.ndarray:
        if sapling is None:
            return np.full(len(p), np.inf)
        return _union(sphere(p, sapling + np.array([0, 1.4 * M, 0]), 0.7 * M),
                      sphere(p, sapling + np.array([0.4 * M, 1.0 * M, 0.2 * M]), 0.5 * M))

    def door(p: np.ndarray) -> np.ndarray:
        return _box(p, (-half_w * 0.35, 1.05 * M, half_d - 0.08 * M), (0.5 * M, 1.05 * M, 0.12 * M))

    def awning(p: np.ndarray, stripe: int) -> np.ndarray:
        cloth = rounded_box(p, (0, 2.35 * M, half_d + 0.55 * M), (half_w * 0.82, 0.05 * M, 0.6 * M), 0.02 * M,
                            rotation_x(0.3))
        # Rayures du store : une bande sur deux dans chaque matériau.
        band = np.floor(p[:, 0] / (0.45 * M)).astype(np.int64) % 2
        return np.where(band == stripe, cloth, np.inf)

    def sign(p: np.ndarray) -> np.ndarray:
        return _box(p, (0, 2.75 * M, half_d + 0.1 * M), (half_w * 0.55, 0.28 * M, 0.08 * M))

    def rubble_part(p: np.ndarray) -> np.ndarray:
        if not rubble:
            return np.full(len(p), np.inf)
        return _union(*(rounded_box(p, c, h, 0.05 * M, rotation_y(a)) for c, h, a in rubble))

    def parts() -> list[Part]:
        result = [
            # L'intérieur sombre passe avant la coque : sur une face de cassure, il l'emporte à distance égale.
            Part(interior, GLASS), Part(shell, WALL), Part(floors, FLOOR), Part(trim, TRIM), Part(boards, BOARD),
            Part(roof, ROOF), Part(grime_part, GRIME), Part(moss_part, MOSS), Part(water_part, WATER),
            Part(foliage_part, FOLIAGE), Part(rubble_part, RUBBLE),
        ]
        if spec.style != "ruin":
            result.append(Part(door, DOOR))
        if shop:
            result += [Part(lambda p: awning(p, 0), AWNING), Part(lambda p: awning(p, 1), TRIM), Part(sign, SIGN)]
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
                     supersample=3)


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
    return [building(spec) for spec in specs]
