"""Compare les exports d'un personnage sur trois sols réels, sans retoucher les PNG du jeu.

Usage : python3 tools/review_character.py traqueur --baseline /tmp/anciens/traqueur --output /tmp/revue
Le rapport contrôle séquences, alpha et cadre ; les pixels au bord sont signalés pour inspection.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
from tools.generate_character import ACTIONS, DIRECTIONS
from tools.sprites.models import character
from tools.sprites.render import render, screen_direction_to_yaw


def background(biome: str, size: tuple[int, int]) -> Image.Image:
    tile = Image.open(f"assets/tiles/{biome}/tile_{biome}_sol_base.png").convert("RGBA")
    result = Image.new("RGBA", size, "#1A1A2E")
    for row, y in enumerate(range(-tile.height, size[1] + tile.height, tile.height // 2)):
        for x in range(-tile.width, size[0] + tile.width, tile.width):
            result.alpha_composite(tile, (x + (row % 2) * (tile.width // 2), y))
    return result


def review(character_id: str, baseline: Path, output: Path) -> None:
    model = character(character_id)
    folder = Path("assets/characters") / character_id
    expected = {f"char_{character_id}_{direction}_{action}_{frame:02d}.png"
                for direction in DIRECTIONS for action in ACTIONS
                for frame in range(1, len(model.animations[action]) + 1)}
    actual = {p.name for p in folder.glob("*.png")}
    assert actual == expected, f"Séquences : manquants={expected - actual}, orphelins={actual - expected}"
    edges, old_edges, changed = [], [], 0
    hashes = {}
    for name in sorted(expected):
        path = folder / name
        image = Image.open(path)
        assert image.size == model.frame_size and image.mode == "RGBA", name
        assert set(image.getchannel("A").tobytes()) <= {0, 255}, f"Alpha non binaire : {name}"
        assert image.getbbox() is not None, f"Frame vide : {name}"
        old = Image.open(baseline / name)
        if image.tobytes() != old.tobytes(): changed += 1
        for candidate, target in ((image, edges), (old, old_edges)):
            x0, y0, x1, y1 = candidate.getbbox()
            if x0 == 0 or y0 == 0 or x1 == candidate.width or y1 == candidate.height:
                target.append(name)
        hashes[name] = hashlib.sha256(path.read_bytes()).hexdigest()

    clipped = []
    for name in edges:
        direction, action, number = name.removesuffix(".png").split("_")[-3:]
        pose = model.animations[action][int(number) - 1]
        dx, dy = DIRECTIONS[direction]
        expanded = render(model.parts(pose), model.materials, screen_direction_to_yaw(dx, dy),
                          (model.frame_size[0] + 8, model.frame_size[1] + 8),
                          (model.pivot[0] + 4, model.pivot[1] + 4), model.scale)
        x0, y0, x1, y1 = expanded.getbbox()
        if x0 < 4 or y0 < 4 or x1 > model.frame_size[0] + 4 or y1 > model.frame_size[1] + 4:
            clipped.append(name)

    # Chaque paire montre ancien à gauche, nouveau à droite, au même pivot et à la même taille.
    cell_w, cell_h, scale = 76, 62, 3
    sheet = Image.new("RGBA", (cell_w * 8 * scale, 32 + cell_h * 4 * scale), "#1A1A2E")
    draw = ImageDraw.Draw(sheet)
    draw.text((8, 8), f"{character_id} | ANCIEN / NOUVEAU | pixels x3 | foret, ruines, carriere, marche", fill="#E8E0D4")
    for row, (biome, action) in enumerate((("foret", "idle"), ("ruines", "idle"), ("carriere", "idle"), ("ruines", "walk"))):
        for col, direction in enumerate(DIRECTIONS):
            cell = background(biome, (cell_w, cell_h))
            name = f"char_{character_id}_{direction}_{action}_01.png"
            cell.alpha_composite(Image.open(baseline / name), (4, 14))
            cell.alpha_composite(Image.open(folder / name), (40, 14))
            x, y = col * cell_w * scale, 32 + row * cell_h * scale
            sheet.alpha_composite(cell.resize((cell_w * scale, cell_h * scale), Image.Resampling.NEAREST), (x, y))
            draw.text((x + 4, y + 4), direction, fill="#E8E0D4")
    output.mkdir(parents=True, exist_ok=True)
    sheet.save(output / "comparison.png")
    report = {"character": character_id, "frames": len(expected), "changed": changed,
              "frame_size": model.frame_size, "pivot": model.pivot, "scale": model.scale,
              "edge_frames_before": old_edges, "edge_frames_after": edges,
              "new_edge_frames": sorted(set(edges) - set(old_edges)), "clipped_frames": clipped, "sha256": hashes}
    (output / "report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n")
    print(json.dumps({k: v for k, v in report.items() if k != "sha256"}, ensure_ascii=False))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("character")
    parser.add_argument("--baseline", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    review(args.character, args.baseline, args.output)
