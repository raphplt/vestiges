"""
Sols en tuiles de Wang pour la grille isométrique « stacked » (plan 10 T1).

Chaque arête de la grille porte une couleur (0 ou 1) tirée en jeu par hachage de l'arête (WangTiles.cs) ; une matière
existe donc en 16 tuiles, une par combinaison (NE, NW, SE, SW). Près d'une arête, le motif ne dépend que de la
couleur de cette arête et de la position relative au milieu de l'arête : deux tuiles voisines se raccordent sans
couture et la carte ne répète aucun motif à l'échelle de la grille. Les sommets, partagés par quatre tuiles, suivent
un champ unique. Le motif est un bruit lent ramené à quatre tons (grandes plaques calmes), plus quelques détails rares
au centre de la tuile.
"""
from __future__ import annotations

from dataclasses import dataclass, field

import numpy as np
from PIL import Image

from .palette import RampColor, hex_to_rgb

TILE_W, TILE_H = 64, 32
# Repère au sol : la vue 2:1 écrase la profondeur de moitié, on double y pour un bruit isotrope.
GROUND_Y = 2.0

# Bits de la tuile : index = NE·1 + NW·2 + SE·4 + SW·8 (même ordre que WangTiles.cs).
EDGES = {
    "NE": ((32.0, 0.0), (64.0, 16.0), 1, "d1"),
    "NW": ((0.0, 16.0), (32.0, 0.0), 2, "d2"),
    "SE": ((64.0, 16.0), (32.0, 32.0), 4, "d2"),
    "SW": ((32.0, 32.0), (0.0, 16.0), 8, "d1"),
}
VERTICES = ((32.0, 0.0), (64.0, 16.0), (32.0, 32.0), (0.0, 16.0))


@dataclass(frozen=True)
class Detail:
    """Petit motif posé au centre des tuiles : lignes de caractères, '.' transparent, chiffre = index de couleur."""
    pattern: tuple[str, ...]
    colors: tuple[str, ...]
    weight: float = 1.0


@dataclass(frozen=True)
class GroundMaterial:
    """Quatre tons du plus sombre au plus clair et la part de surface visée pour chacun."""
    name: str
    tones: tuple[str, str, str, str]
    shares: tuple[float, float, float, float]
    feature_px: float = 14.0
    details: tuple[Detail, ...] = ()
    details_per_tile: float = 0.8
    seed: int = 1
    # Étirement du motif au sol (x, y) : (4, 1) allonge les formes en rangs parallèles (blé, chaume).
    stretch: tuple[float, float] = (1.0, 1.0)
    # Dallage : côté d'une dalle au sol (diviseur de 32, pour que les joints se raccordent d'une tuile à l'autre),
    # couleur des joints, et décalage d'une demi-dalle un rang sur deux (appareil en quinconce).
    slab_px: int = 0
    joint: str = "#000000"
    running_bond: bool = False
    # Part des joints effacés par l'usure (0 = joints intacts).
    joint_wear: float = 0.0
    ramp: tuple[RampColor, ...] = field(init=False, repr=False)

    def __post_init__(self) -> None:
        object.__setattr__(self, "ramp", tuple(hex_to_rgb(t) for t in self.tones))


def _value_noise(points: np.ndarray, seed: int, scale: float) -> np.ndarray:
    """Bruit de valeur lissé à deux octaves, déterministe, sur des points (N, 2) du repère au sol."""
    total = np.zeros(len(points))
    amplitude = 1.0
    norm = 0.0
    for octave in range(2):
        p = points / (scale / (2 ** octave))
        i = np.floor(p).astype(np.int64)
        f = p - i
        f = f * f * (3.0 - 2.0 * f)

        def lattice(ix: np.ndarray, iy: np.ndarray) -> np.ndarray:
            h = (ix * 374761393 + iy * 668265263 + (seed + octave * 7919) * 1442695041) & 0xFFFFFFFF
            h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535.0

        a = lattice(i[:, 0], i[:, 1])
        b = lattice(i[:, 0] + 1, i[:, 1])
        c = lattice(i[:, 0], i[:, 1] + 1)
        d = lattice(i[:, 0] + 1, i[:, 1] + 1)
        total += amplitude * (a + (b - a) * f[:, 0] + (c - a) * f[:, 1] + (a - b - c + d) * f[:, 0] * f[:, 1])
        norm += amplitude
        amplitude *= 0.45
    return total / norm


def _segment_distance(points: np.ndarray, a: tuple[float, float], b: tuple[float, float]) -> np.ndarray:
    a_ = np.array([a[0], a[1] * GROUND_Y])
    b_ = np.array([b[0], b[1] * GROUND_Y])
    ab = b_ - a_
    t = np.clip(((points - a_) @ ab) / (ab @ ab), 0.0, 1.0)
    closest = a_ + t[:, None] * ab
    return np.linalg.norm(points - closest, axis=1)


def _diamond_pixels() -> tuple[np.ndarray, np.ndarray]:
    """Pixels du losange (même masque que les tuiles existantes) et leurs centres au sol."""
    xs, ys = [], []
    for y in range(TILE_H):
        half = (y + 1) * 2 if y <= TILE_H // 2 - 1 else (TILE_H - y) * 2
        for x in range(max(0, 32 - half), min(TILE_W, 32 + half)):
            xs.append(x)
            ys.append(y)
    pixels = np.stack([np.array(xs), np.array(ys)], axis=1)
    ground = np.stack([pixels[:, 0] + 0.5, (pixels[:, 1] + 0.5) * GROUND_Y], axis=1)
    return pixels, ground


def _field(material: GroundMaterial, points: np.ndarray, key: int) -> np.ndarray:
    return _value_noise(points / np.asarray(material.stretch), material.seed * 9973 + key * 131, material.feature_px)


def _tile_values(material: GroundMaterial, index: int, ground: np.ndarray) -> np.ndarray:
    weights = []
    values = []
    for name, (a, b, bit, family) in EDGES.items():
        color = 1 if index & bit else 0
        middle = np.array([(a[0] + b[0]) / 2, (a[1] + b[1]) / 2 * GROUND_Y])
        # Même champ pour une arête vue des deux côtés : clé = famille d'arête et couleur, position relative au milieu.
        key = (1 if family == "d1" else 3) + color
        values.append(_field(material, ground - middle + 500.0, key))
        weights.append(np.exp(-(_segment_distance(ground, a, b) / 5.0) ** 2))
    for vertex in VERTICES:
        v = np.array([vertex[0], vertex[1] * GROUND_Y])
        values.append(_field(material, ground - v + 900.0, 7))
        # Petit et bref : partagé par toute la carte, un champ de sommet trop large dessinerait une trame.
        weights.append(4.0 * np.exp(-(np.linalg.norm(ground - v, axis=1) / 2.2) ** 2))
    # Cœur de la tuile : propre à la combinaison, il varie l'intérieur sans toucher aux raccords.
    values.append(_field(material, ground + 1300.0, 11 + index))
    weights.append(np.full(len(ground), 0.12))

    w = np.stack(weights)
    v = np.stack(values)
    return (w * v).sum(axis=0) / w.sum(axis=0)


def _thresholds(material: GroundMaterial, ground: np.ndarray) -> np.ndarray:
    """Seuils de quantification réglés sur l'ensemble des 16 tuiles, pour respecter les parts de surface visées."""
    sample = np.concatenate([_tile_values(material, i, ground) for i in range(16)])
    cumulative = np.cumsum(material.shares[:-1]) / sum(material.shares)
    return np.quantile(sample, cumulative)


def _joint_mask(material: GroundMaterial, ground: np.ndarray) -> np.ndarray:
    """Joints du dallage, en coordonnées au sol de la tuile : les origines des tuiles tombent sur des multiples de 32
    au sol, donc un côté qui divise 32 donne des joints continus d'une tuile à l'autre."""
    if material.slab_px <= 0:
        return np.zeros(len(ground), dtype=bool)
    size = float(material.slab_px)
    gx = ground[:, 0] - 0.5
    gy = ground[:, 1] - 1.0
    row = np.floor(gy / size)
    if material.running_bond:
        gx = gx + (row % 2) * size * 0.5
    # Un pixel de joint à l'écran dans chaque sens (deux unités au sol en y).
    joints = (np.mod(gx, size) < 1.0) | (np.mod(gy, size) < 2.0)
    # Joints usés : un bruit lent (continu d'une tuile à l'autre) en efface des tronçons.
    wear = _value_noise(ground, material.seed * 31 + 5, size * 0.75)
    return joints & (wear > material.joint_wear)


def _interior_distance(ground: np.ndarray) -> np.ndarray:
    return np.min(np.stack([_segment_distance(ground, a, b) for a, b, _, _ in EDGES.values()]), axis=0)


def render_tiles(material: GroundMaterial) -> list[Image.Image]:
    pixels, ground = _diamond_pixels()
    thresholds = _thresholds(material, ground)
    interior = _interior_distance(ground)
    tiles = []
    for index in range(16):
        values = _tile_values(material, index, ground)
        tones = np.searchsorted(thresholds, values)
        joints = _joint_mask(material, ground)
        joint_color = hex_to_rgb(material.joint)
        image = Image.new("RGBA", (TILE_W, TILE_H), (0, 0, 0, 0))
        data = image.load()
        for (x, y), tone, joint in zip(pixels, tones, joints):
            data[int(x), int(y)] = (*(joint_color if joint else material.ramp[int(tone)]), 255)
        _place_details(material, index, image, pixels, interior)
        tiles.append(image)
    return tiles


def _place_details(material: GroundMaterial, index: int, image: Image.Image, pixels: np.ndarray,
                   interior: np.ndarray) -> None:
    if not material.details:
        return
    rng = np.random.default_rng(material.seed * 1009 + index)
    count = rng.poisson(material.details_per_tile)
    # Loin des arêtes : un détail coupé par une arête trahirait la tuile.
    anchors = pixels[interior > 11.0]
    weights = np.array([d.weight for d in material.details])
    data = image.load()
    for _ in range(count):
        detail = material.details[rng.choice(len(material.details), p=weights / weights.sum())]
        ax, ay = anchors[rng.integers(len(anchors))]
        colors = [hex_to_rgb(c) for c in detail.colors]
        for dy, row in enumerate(detail.pattern):
            for dx, char in enumerate(row):
                if char == ".":
                    continue
                px = int(ax) + dx - len(row) // 2
                py = int(ay) + dy - len(detail.pattern) // 2
                if 0 <= px < TILE_W and 0 <= py < TILE_H and data[px, py][3] > 0:
                    data[px, py] = (*colors[int(char)], 255)
