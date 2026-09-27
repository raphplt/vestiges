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
from ..sdf import capsule, ellipsoid, rotation_x, rotation_y, rotation_z, rounded_box, sphere
from ._kit import AXIS_X_YAW, M, PropModel, Weathering, box_footprint

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
    facades = Facades(half_w, half_d, (STOREY if shop else 0.0) + 1.55 * M, spec.storeys - (1 if shop else 0), spec.seed,
                      spacing=spec.window_spacing)
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

    def masonry(p: np.ndarray) -> np.ndarray:
        # Brèches cassées le long des blocs (plan 08 P2) : décalage constant par bloc de maçonnerie, d'où des bords
        # en escalier au lieu de trous ronds. Bloc d'environ 0,5 × 0,3 m, amplitude ±0,3 m.
        cell = np.floor(p / np.array([0.5 * M, 0.3 * M, 0.5 * M]))
        return (_hash(cell[:, 0], cell[:, 1], cell[:, 2] + spec.seed) - 0.5) * 0.6 * M

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
            carve = np.minimum(carve, sphere(p, center, radius) - masonry(p))
        # Les grands plans d'effondrement restent nets : décalés par bloc, ils se couvraient de mouchetures.
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


def church(stem: str, seed: int, mirrored: bool) -> PropModel:
    """Église de quartier, repère rare (plan 08 P2) : nef longée de contreforts et de hautes baies en ogive, clocher
    à une extrémité avec abat-sons et flèche basse, croix penchée. Toit de la nef crevé, gravats devant le portail.
    Le clocher dépasse les immeubles voisins sans sortir de la hauteur permise (~190 px)."""
    STONE, TRIM, GLASS, ROOF, DOOR, MOSS, RUBBLE, INTERIOR, BRONZE, BEAM = range(10)
    materials = [
        make_material("stone", "#9C9384"), make_material("trim", "#BDB39C"),
        make_material("glass", "#3A3456", contrast=0.6), make_material("roof", "#4E4A54"),
        make_material("door", "#3A2E28", contrast=0.6), make_material("moss", "#5A7A38"),
        make_material("rubble", "#8A857C"), make_material("interior", "#1E1A1D", contrast=0.4),
        make_material("bronze", "#8A6A3A"), make_material("beam", "#6E4E36", contrast=0.7),
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
    # Lierre : grappes de feuillage qui grimpent le long du mur, du pied vers les baies.
    ivy = []
    for _ in range(2):
        x = w.uniform(0.0, 0.85) * half_w
        ivy += [np.array([x + w.uniform(-0.35, 0.35) * M, (0.15 + k * 0.3) * M, nave_d + 0.05 * M]) for k in range(9)]

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

    def moss(p: np.ndarray) -> np.ndarray:
        return _union(*(sphere(p, c, 0.2 * M) for c in ivy))

    def rubble_part(p: np.ndarray) -> np.ndarray:
        return _union(*(rounded_box(p, c, h, 0.05 * M, rotation_y(a)) for c, h, a in rubble))

    # Variante b : lacet opposé et clocher à l'autre bout (miroir en x), pour qu'il reste du côté proche de la caméra
    # et que la silhouette ne dépasse pas la hauteur permise.
    flip = np.array([-1.0 if mirrored else 1.0, 1.0, 1.0])

    def parts() -> list[Part]:
        volumes = [(inside, INTERIOR), (walls, STONE), (glass, GLASS), (trim, TRIM), (roof, ROOF), (rafters, BEAM), (cross, BRONZE),
                   (bell, BRONZE), (door, DOOR), (moss, MOSS), (rubble_part, RUBBLE)]
        return [Part(lambda p, f=f: f(p * flip), material) for f, material in volumes]

    extent_x = half_w + 1.4 * M
    extent_z = half_d + 1.6 * M
    top = tower_h + 2.8 * M
    return PropModel(stem, parts, materials, -BUILDING_YAW if mirrored else BUILDING_YAW,
                     ray_range=float(2.5 * max(extent_x, extent_z, top)),
                     canvas=(int(extent_x * 2 * 0.62 * 1.05) + 24, int(top * 0.62 * 0.87 + extent_z * 2 * 0.31) + 30),
                     footprint=box_footprint(half_w, nave_d),
                     bounds=((-extent_x, 0.0, -extent_z), (extent_x, top, extent_z)),
                     supersample=3)


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
                     bounds=((-1.5 * M, 0.0, -1.5 * M), (2.4 * M, height, 1.8 * M)))


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
