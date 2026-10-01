"""Planches de contrôle et export des textures UI (plan 25)."""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from ..retouch import save_unless_locked

CARD = Path("assets/ui/menus/ui_card_normal.png")
FONT = "assets/fonts/saira/SairaSemiCondensed-SemiBold.ttf"
DARK = (26, 26, 46, 255)
BIOMES = ("foret", "ruines", "marecages", "carriere", "champs")


def ground_patch(biome: str, size: tuple[int, int]) -> Image.Image:
    tile = Image.open(f"assets/tiles/{biome}/tile_{biome}_sol_base.png").convert("RGBA")
    result = Image.new("RGBA", size, DARK)
    for row, y in enumerate(range(-tile.height, size[1] + tile.height, tile.height // 2)):
        for x in range(-tile.width, size[0] + tile.width, tile.width):
            result.alpha_composite(tile, (x + (row % 2) * tile.width // 2, y))
    return result


def nine_patch(source: Image.Image, size: tuple[int, int], margin: int = 3) -> Image.Image:
    """Conserve les coins et étire les neuf régions au pixel près."""
    result = Image.new("RGBA", size)
    sx, sy = (0, margin, source.width - margin, source.width), (0, margin, source.height - margin, source.height)
    dx, dy = (0, margin, size[0] - margin, size[0]), (0, margin, size[1] - margin, size[1])
    for row in range(3):
        for col in range(3):
            patch = source.crop((sx[col], sy[row], sx[col + 1], sy[row + 1]))
            patch = patch.resize((dx[col + 1] - dx[col], dy[row + 1] - dy[row]), Image.Resampling.NEAREST)
            result.alpha_composite(patch, (dx[col], dy[row]))
    return result


def strip(frames: list[Image.Image]) -> Image.Image:
    result = Image.new("RGBA", (sum(f.width for f in frames), frames[0].height))
    x = 0
    for frame in frames:
        result.alpha_composite(frame, (x, 0))
        x += frame.width
    return result


def export(images: dict[str, Image.Image], output: Path, tool: str) -> None:
    for name, image in images.items():
        save_unless_locked(image, output / f"{name}.png", tool)


def board(rows: list[tuple[str, list[Image.Image]]], path: Path, *, columns: int = 1) -> None:
    """Chaque sujet : ×4, puis ×1 sur fond sombre et sur le vrai cadre de carte."""
    font = ImageFont.truetype(FONT, 14)
    width = max(sum(i.width * 4 + 8 for i in images) for _, images in rows) + 24
    height = max(max(i.height for i in images) * 5 + 70 for _, images in rows)
    sheet = Image.new("RGBA", (width * columns, height * ((len(rows) + columns - 1) // columns)), DARK)
    draw = ImageDraw.Draw(sheet)
    card = Image.open(CARD).convert("RGBA")
    for index, (label, images) in enumerate(rows):
        x0, y0 = index % columns * width + 12, index // columns * height
        draw.text((x0, y0 + 6), label, font=font, fill="#E8E0D4")
        x, baseline = x0, y0 + 30
        for image in images:
            sheet.alpha_composite(image.resize((image.width * 4, image.height * 4), Image.Resampling.NEAREST), (x, baseline))
            low = baseline + max(i.height for i in images) * 4 + 5
            sheet.alpha_composite(image, (x, low))
            panel = nine_patch(card, (image.width + 8, image.height + 8))
            panel.alpha_composite(image, (4, 4))
            sheet.alpha_composite(panel, (x + image.width + 5, low - 4))
            x += image.width * 4 + 8
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)
