"""Plan 25 S7 : planches d'écrans et de chargement ; --export après validation."""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from tools.sprites.ui.kit import BIOMES, DARK, FONT, board, export, nine_patch
from tools.sprites.ui.screens import FRAMES, SIZE, TINTS, assets, background, loading_ground, skin


def write_sheets(folder: Path, images: dict[str, Image.Image]) -> None:
    folder.mkdir(parents=True, exist_ok=True)
    sheet = Image.new("RGBA", (1024, 800), DARK)
    font = ImageFont.truetype(FONT, 16)
    draw = ImageDraw.Draw(sheet)
    for index, tint in enumerate(TINTS):
        x, y = index % 2 * 512 + 16, index // 2 * 400 + 28
        panel = background(tint, 0)
        # Cadres réels : la planche vérifie que les rayons restent derrière le contenu.
        for card in range(3):
            panel.alpha_composite(nine_patch(skin("ui_card_normal"), (124, 144)), (42 + card * 134, 64))
            icon = Image.open(f"assets/weapons/icons/weapon_icon_{('sickle', 'nail_gun', 'music_box')[card]}.png").convert("RGBA")
            panel.alpha_composite(icon, (88 + card * 134, 92))
        sheet.alpha_composite(panel, (x, y))
        draw.text((x, y - 24), f"{tint} — fond natif 480 × 270", font=font, fill="#E8E0D4")
        for f in range(4):
            crop = background(tint, f * 2).crop((300, 30, 324, 54)).resize((96, 96), Image.Resampling.NEAREST)
            sheet.alpha_composite(crop, (x + f * 112, y + 278))
    sheet.save(folder / "25-s7-ecrans.png")
    rows = [(name, [image]) for name, image in images.items() if name.startswith("ui_")]
    board(rows, folder / "25-s7-cadres.png", columns=3)
    loading = []
    for biome in BIOMES:
        sample = Image.new("RGBA", (128, 48))
        sample.alpha_composite(loading_ground(biome), (0, 16))
        player = Image.open("assets/characters/traqueur/char_traqueur_SE_walk_01.png").convert("RGBA")
        sample.alpha_composite(player, (48, 0))
        loading.append((f"Chargement / {biome} — personnage de référence", [sample]))
    board(loading, folder / "25-s7-chargement.png")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sheets", type=Path, default=Path("doc/plans/planches"))
    parser.add_argument("--export", action="store_true")
    args = parser.parse_args()
    images = assets()
    write_sheets(args.sheets, images)
    if args.export:
        output = Path("assets/ui/screens/plan25")
        export(images, output, "generate_screen_sprites")
        manifest = {"background": {"frame_size": list(SIZE), "frames": FRAMES, "fps": 3, "layout": "horizontal", "tints": list(TINTS)},
                    "skin_margins": [4, 4, 4, 4], "loading_ground": {"size": [128, 32], "repeat_axis": "x", "biomes": list(BIOMES)},
                    "filter": "nearest"}
        (output / "screens_manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    print(f"[generate_screen_sprites] trois planches dans {args.sheets}")


if __name__ == "__main__":
    main()
