"""
Planche avant/après d'une ou plusieurs créatures (plan 54, lot S3) : les PNG de assets/enemies/<id>/ face au rendu du
générateur, sans rien écrire dans assets/. Pour chaque créature, deux rangées agrandies (avant, après) de toutes les
frames de chaque direction demandée, puis la première direction à taille réelle sur deux sols (ancré, effacé).
Libellés en ASCII : la police par défaut de Pillow n'a pas d'accents.

Usage : python3 tools/creature_compare.py sortie.png [--scale 4] [--directions SE,E,N] [--before <ref git>] ids...
--before lit les PNG « avant » dans un commit (une fois les sprites réécrits, l'état précédent n'est plus sur disque).
"""
from __future__ import annotations

import argparse
import io
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from tools.generate_character import DIRECTIONS  # noqa: E402
from tools.generate_enemy import ACTIONS  # noqa: E402
from tools.sprites.models import creature  # noqa: E402
from tools.sprites.render import flatten, screen_direction_to_yaw  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
BACKGROUND = (34, 36, 40, 255)
CELL = (52, 56, 62, 255)
GROUND_ANCHORED = (74, 88, 60, 255)
GROUND_ERASED = (206, 204, 214, 255)
TEXT = (232, 224, 212, 255)
LABEL_W = 70
TITLE_H = 22


def _current(enemy_id: str, direction: str, action: str, ref: str | None) -> list[Image.Image]:
    folder = ROOT / "assets" / "enemies" / enemy_id
    if ref is None:
        paths = sorted(folder.glob(f"enemy_{enemy_id}_{direction}_{action}_*.png"))
        return [Image.open(path).convert("RGBA") for path in paths]
    frames = []
    for index in range(1, 100):
        path = f"assets/enemies/{enemy_id}/enemy_{enemy_id}_{direction}_{action}_{index:02d}.png"
        shown = subprocess.run(["git", "show", f"{ref}:{path}"], cwd=ROOT, capture_output=True)
        if shown.returncode != 0:
            break
        frames.append(Image.open(io.BytesIO(shown.stdout)).convert("RGBA"))
    return frames


def _rendered(model, direction: str, action: str) -> list[Image.Image]:
    dx, dy = DIRECTIONS[direction]
    yaw = screen_direction_to_yaw(dx, dy)
    return [flatten(model.render_layers(state, yaw)) for state in model.animations[action]]


def _strip(frames: list[Image.Image], scale: int, size: tuple[int, int], pivot: tuple[float, float]) -> Image.Image:
    cell_w, cell_h = size[0] * scale, size[1] * scale
    strip = Image.new("RGBA", (max(1, len(frames)) * (cell_w + 2), cell_h), BACKGROUND)
    draw = ImageDraw.Draw(strip)
    for index, frame in enumerate(frames):
        x = index * (cell_w + 2)
        draw.rectangle([x, 0, x + cell_w - 1, cell_h - 1], fill=CELL)
        cx, cy = x + int(pivot[0] * scale), int(pivot[1] * scale)
        draw.polygon([(cx - 8 * scale, cy), (cx, cy - 4 * scale), (cx + 8 * scale, cy), (cx, cy + 4 * scale)],
                     fill=GROUND_ANCHORED)
        if frame.size != size:
            frame = frame.resize(size, Image.NEAREST)
        strip.alpha_composite(frame.resize((cell_w, cell_h), Image.NEAREST), (x, 0))
    return strip


def _real_size(frames: list[Image.Image], size: tuple[int, int], ground) -> Image.Image:
    strip = Image.new("RGBA", (len(frames) * (size[0] + 2) + 4, size[1] + 4), ground)
    for index, frame in enumerate(frames):
        strip.alpha_composite(frame, (2 + index * (size[0] + 2), 2))
    return strip


def creature_block(enemy_id: str, directions: list[str], scale: int, ref: str | None = None) -> Image.Image:
    model = creature(enemy_id)
    size, pivot = model.frame_size, model.pivot
    rows: list[tuple[str, Image.Image]] = []
    real: list[Image.Image] = []
    for direction in directions:
        for label, source in (("avant", lambda a, d=direction: _current(enemy_id, d, a, ref)),
                              ("apres", lambda a, d=direction: _rendered(model, d, a))):
            frames = [frame for action in ACTIONS for frame in source(action)]
            rows.append((f"{direction} {label}", _strip(frames, scale, size, pivot)))
            if direction == directions[0]:
                for ground in (GROUND_ANCHORED, GROUND_ERASED):
                    real.append(_real_size(frames, size, ground))
    width = LABEL_W + max(strip.width for _, strip in rows)
    height = TITLE_H + sum(strip.height + 4 for _, strip in rows) + sum(image.height + 2 for image in real) + 8
    block = Image.new("RGBA", (width, height), BACKGROUND)
    draw = ImageDraw.Draw(block)
    actions = " / ".join(f"{action} {len(model.animations[action])}" for action in ACTIONS)
    draw.text((4, 4), f"{enemy_id} | {actions} | {size[0]}x{size[1]}", fill=TEXT)
    y = TITLE_H
    for label, strip in rows:
        draw.text((4, y + strip.height // 2), label, fill=TEXT)
        block.alpha_composite(strip, (LABEL_W, y))
        y += strip.height + 4
    for image in real:
        block.alpha_composite(image, (LABEL_W, y))
        y += image.height + 2
    return block


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("output", type=Path)
    parser.add_argument("ids", nargs="+")
    parser.add_argument("--scale", type=int, default=4)
    parser.add_argument("--directions", default="SE,E,N")
    parser.add_argument("--before", help="commit d'où lire les PNG « avant » (défaut : assets/ sur disque)")
    args = parser.parse_args()
    directions = args.directions.split(",")
    blocks = [creature_block(enemy_id, directions, args.scale, args.before) for enemy_id in args.ids]
    sheet = Image.new("RGBA", (max(b.width for b in blocks), sum(b.height for b in blocks)), BACKGROUND)
    y = 0
    for block in blocks:
        sheet.alpha_composite(block, (0, y))
        y += block.height
    args.output.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(args.output)
    print(f"[creature_compare] {args.output}")


if __name__ == "__main__":
    main()
