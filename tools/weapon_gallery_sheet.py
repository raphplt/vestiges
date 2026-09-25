#!/usr/bin/env python3
"""Assemble les captures `weapon-<id>-<n>.png` de `RunObservation --capture-weapons` en planches lisibles.

Usage : python3 tools/weapon_gallery_sheet.py <dossier> [armes par planche]
Écrit `<dossier>/sheet-<k>.png` : une ligne par arme, les instants de l'attaque en colonnes.
"""
import re
import sys
from collections import defaultdict
from pathlib import Path

from PIL import Image, ImageDraw

folder = Path(sys.argv[1])
per_sheet = int(sys.argv[2]) if len(sys.argv) > 2 else 6
cell_width = 360

frames = defaultdict(dict)
for path in folder.glob("weapon-*-*.png"):
    match = re.fullmatch(r"weapon-(.+)-(\d+)\.png", path.name)
    if match:
        frames[match.group(1)][int(match.group(2))] = path

weapons = sorted(frames)
for sheet_index in range(0, len(weapons), per_sheet):
    rows = weapons[sheet_index:sheet_index + per_sheet]
    columns = max(len(frames[w]) for w in rows)
    sample = Image.open(next(iter(frames[rows[0]].values())))
    cell_height = round(sample.height * cell_width / sample.width)
    sheet = Image.new("RGB", (cell_width * columns, (cell_height + 16) * len(rows)), (20, 18, 24))
    draw = ImageDraw.Draw(sheet)
    for row, weapon in enumerate(rows):
        y = row * (cell_height + 16)
        draw.text((4, y + 2), weapon, fill=(230, 220, 200))
        for column, shot in enumerate(sorted(frames[weapon])):
            image = Image.open(frames[weapon][shot]).convert("RGB").resize((cell_width, cell_height), Image.NEAREST)
            sheet.paste(image, (column * cell_width, y + 16))
    sheet.save(folder / f"sheet-{sheet_index // per_sheet}.png")
    print(folder / f"sheet-{sheet_index // per_sheet}.png")
