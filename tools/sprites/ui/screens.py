"""Habillage des écrans : poussière, rayons en marches, bords effilochés et sols (plan 25 S7)."""
from __future__ import annotations

import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

from .kit import BIOMES, ground_patch, strip

FRAMES = 8
SIZE = (480, 270)
COLORS = {p["id"]: p for p in json.loads(Path("data/ui/rarities.json").read_text())["rarities"]}
TINTS = {"gold": "legendary", "cyan": "memorial", "violet": "rift", "neutral": "common"}


def background(tint: str, frame: int) -> Image.Image:
    rgb = np.asarray(tuple(bytes.fromhex(COLORS[TINTS[tint]]["color"][1:])), dtype=float)
    # Quatre niveaux discrets pour toute l'image ; la faible valeur laisse les cartes au premier plan.
    palette = np.asarray([(12, 12, 21), tuple((rgb * .09 + 13).astype(int)),
                          tuple((rgb * .16 + 16).astype(int)), tuple((rgb * .42 + 32).astype(int))], dtype=np.uint8)
    yy, xx = np.mgrid[:SIZE[1], :SIZE[0]]
    x, y = xx - 240, yy - 144
    angle = np.arctan2(y, x)
    ray = np.mod(angle / (np.pi / 4) + frame / FRAMES, 1)
    radius = np.hypot(x, y)
    indices = np.where(ray < .2, 2, np.where(ray < .28, 1, 0))
    indices[radius < 24] = 0
    # Une poussière éparse, phase cyclique sans nouveau hasard à chaque génération.
    rng = np.random.default_rng(2507)
    for px, py, phase in rng.integers(0, 480, (140, 3)):
        if (int(phase) + frame) % FRAMES < 4:
            indices[int(py) % 270, int(px)] = 3
    data = np.concatenate([palette[indices], np.full((270, 480, 1), 255, dtype=np.uint8)], axis=2)
    return Image.fromarray(data)


def skin(name: str) -> Image.Image:
    image = Image.open(Path("assets/ui/menus") / f"{name}.png").convert("RGBA")
    w, h = image.size
    focused = "selected" in name or "hover" in name
    light, dark = ("#7FD6CF", "#2A5A5E") if focused else ("#9E9494", "#3A3535")
    if "disabled" in name or "locked" in name:
        light, dark = "#6B6161", "#3A3535"
    # Les anciens cadres emploient de l'or même à l'état normal. Le métal patiné
    # garde désormais ce code couleur disponible pour les récompenses mémorielles.
    pixels = np.array(image)
    gold = (pixels[:, :, 0] > pixels[:, :, 2] * 1.4) & (pixels[:, :, 1] > pixels[:, :, 2] * 1.2)
    for y, x in np.argwhere(gold):
        value = int(pixels[y, x, 0])
        color = light if value >= 200 else dark
        pixels[y, x, :3] = tuple(bytes.fromhex(color[1:]))
    image = Image.fromarray(pixels)
    draw = ImageDraw.Draw(image)
    draw.line([(1, h - 3), (1, 2), (2, 1), (w - 3, 1)], fill=light)
    draw.line([(w - 2, 2), (w - 2, h - 3), (w - 3, h - 2), (2, h - 2)], fill=dark)
    # Usure cantonnée aux marges : le centre et les neuf zones restent raccordables.
    for x, y in ((w - 1, h - 1), (w - 2, h - 2), (w - 1, h - 3), (w - 3, h - 1), (0, 0)):
        draw.point((x, y), fill=(0, 0, 0, 0))
    draw.point((w - 3, h - 3), fill="#6B4FA0")
    draw.point((w - 1, h - 5), fill="#4A3066")
    return image


def loading_ground(biome: str) -> Image.Image:
    image = ground_patch(biome, (128, 32))
    # Coupe en marches dans l'épaisseur de terre : raccord périodique tous les 32 pixels.
    draw = ImageDraw.Draw(image)
    for x in range(128):
        depth = 25 + ((x // 4) % 8 // 2)
        draw.line((x, depth, x, 31), fill=(0, 0, 0, 0))
        draw.point((x, depth - 1), fill="#3A3535")
    return image


def assets() -> dict[str, Image.Image]:
    result = {f"choice_dust_{tint}": strip([background(tint, f) for f in range(FRAMES)]) for tint in TINTS}
    for path in sorted(Path("assets/ui/menus").glob("*.png")):
        if path.stem.startswith(("ui_button_", "ui_card_", "ui_panel_frame")):
            result[path.stem] = skin(path.stem)
    result.update({f"loading_ground_{biome}": loading_ground(biome) for biome in BIOMES})
    return result
