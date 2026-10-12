"""Étude M6 : icônes communes aux objets et matière raccordable, sans export en jeu.

Usage : python3 -m tools.sprites.ui.camp_material_study --output /tmp/vestiges-m6
Les PNG produits sont des planches de revue, pas les fonds de production.
"""
from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

from .camp import satchel, matchbox, essence_jar
from ..weapons.icons import render_icon

ROOT = Path(__file__).resolve().parents[3]
FONT = ROOT / 'assets/fonts/saira/SairaSemiCondensed-Medium.ttf'


def stone_tile() -> Image.Image:
    """Motif périodique : masses de grains corrélés, sans bruit pixel par pixel."""
    rng = np.random.default_rng(36)
    noise = rng.random((64, 64))
    broad = sum(np.roll(np.roll(noise, y, 0), x, 1)
                for y in range(-4, 5) for x in range(-4, 5)) / 81
    fine = sum(np.roll(np.roll(noise, y, 0), x, 1)
               for y in range(-1, 2) for x in range(-1, 2)) / 9
    palette = np.array([[32, 30, 42], [36, 33, 45], [40, 37, 48], [45, 41, 51]], dtype=np.uint8)
    levels = np.digitize(broad, [.483, .510, .542])
    levels[(fine > .79) & (broad > .53)] = 3
    tile = Image.fromarray(palette[levels])
    draw = ImageDraw.Draw(tile)
    # Quelques inclusions minérales groupées ; éviter le semis uniforme de points.
    for x, y in [(12, 9), (39, 18), (54, 45), (18, 53), (31, 35)]:
        draw.line((x, y, x + 2, y), fill='#36323C')
        draw.line((x + 1, y + 1, x + 3, y + 1), fill='#25212F')
    return tile


def surface(size: tuple[int, int], tile: Image.Image) -> Image.Image:
    width, height = size
    pixels = np.asarray(tile)
    return Image.fromarray(pixels[np.arange(height)[:, None] % 64, np.arange(width)[None, :] % 64])


def panel(canvas: Image.Image, rect: tuple[int, int, int, int], tile: Image.Image,
          selected: bool = False) -> None:
    x0, y0, x1, y1 = rect
    canvas.paste(surface((x1 - x0, y1 - y0), tile), (x0, y0))
    draw = ImageDraw.Draw(canvas)
    # Arêtes de six pixels natifs quelle que soit la dimension du panneau.
    draw.rectangle((x0, y0, x1 - 1, y1 - 1), outline='#1A1A2E', width=2)
    draw.line((x0 + 3, y1 - 4, x0 + 3, y0 + 2, x1 - 4, y0 + 2), fill='#6B6161', width=2)
    draw.line((x1 - 3, y0 + 5, x1 - 3, y1 - 3, x0 + 5, y1 - 3), fill='#3A3535', width=2)
    draw.line((x0 + 5, y0 + 5, x1 - 6, y0 + 5), fill='#3A3535')
    draw.line((x0 + 5, y1 - 6, x1 - 6, y1 - 6), fill='#25212F')
    # Les cassures restent dans la tranche et ne parasitent pas la lecture.
    for x in range(x0 + 19, x1 - 10, 43):
        draw.line((x, y0 + 1, x + 1, y0 + 2, x + 4, y0 + 2), fill='#3A3535')
        draw.point((x - 1, y0 + 3), fill='#9E9494')
    if selected:
        draw.line((x0 + 3, y1 - 5, x0 + 3, y0 + 3, x1 - 5, y0 + 3), fill='#A68B6B', width=2)


def font(size: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(FONT), size)


def comparison(icons: dict[str, Image.Image], output: Path) -> None:
    sheet = Image.new('RGB', (1050, 635), '#1A1A2E')
    draw = ImageDraw.Draw(sheet)
    draw.text((30, 20), 'CAMP : même rendu que les objets du jeu', font=font(32), fill='#E8E0D4')
    for x, title in [(30, 'ACTUEL'), (350, 'PROPOSITION'), (700, 'OBJETS DU JEU')]:
        draw.text((x, 78), title, font=font(22), fill='#A68B6B')
    refs = ['item_tabouret_de_camping', 'item_allumette_humide', 'item_flacon_de_pharmacie']
    labels = ['Poches', "Boîte d’allumettes", "Bocal d’Essence"]
    for row, ((name, icon), ref, label) in enumerate(zip(icons.items(), refs, labels)):
        y = 125 + row * 165
        for x, source in [(38, Image.open(ROOT / f'assets/ui/camp/{name}.png')),
                          (358, icon), (708, Image.open(ROOT / f'assets/items/icons/{ref}.png'))]:
            image = source.convert('RGBA')
            sheet.paste(image.resize((128, 128), Image.Resampling.NEAREST), (x, y),
                        image.resize((128, 128), Image.Resampling.NEAREST))
            sheet.paste(image, (x + 155, y + 48), image)
        draw.text((358, y + 128), label, font=font(20), fill='#E8E0D4')
    sheet.save(output / 'icones.png')


def mockup(icons: dict[str, Image.Image], tile: Image.Image, output: Path) -> None:
    background_tile = Image.fromarray((np.asarray(tile).astype(float) * .72).astype(np.uint8))
    native = surface((480, 300), background_tile)
    for rect, selected in [((16, 52, 288, 113), True), ((16, 121, 288, 182), False),
                           ((16, 190, 288, 251), False), ((304, 52, 464, 251), False)]:
        panel(native, rect, tile, selected)
    sheet = native.resize((1440, 900), Image.Resampling.NEAREST)
    draw = ImageDraw.Draw(sheet)
    draw.text((48, 26), 'Campement — étude de matières', font=font(40), fill='#D4A843')
    draw.text((48, 83), 'Pierre mate • contours à échelle constante • objets en 32 px', font=font(24), fill='#9E9494')
    labels = [('Poches', 'Plus de place pour les objets trouvés.', '1/5'),
              ("Boîte d’allumettes", 'Écarter une carte pour toute la run.', '0/3'),
              ("Bocal d’Essence", "De l’Essence en poche dès le départ.", '0/5')]
    for row, (icon, (title, description, rank)) in enumerate(zip(icons.values(), labels)):
        y = (52 + row * 69) * 3
        enlarged = icon.resize((96, 96), Image.Resampling.NEAREST)
        sheet.paste(enlarged, (82, y + 35), enlarged)
        draw.text((208, y + 28), title, font=font(32), fill='#E8E0D4')
        draw.text((208, y + 83), description, font=font(24), fill='#9E9494')
        draw.text((770, y + 36), rank, font=font(26), fill='#A68B6B')
    icon = next(iter(icons.values())).resize((128, 128), Image.Resampling.NEAREST)
    sheet.paste(icon, (1087, 200), icon)
    for y, label, size, color in [(353, 'Poches', 36, '#E8E0D4'),
                                 (408, 'Sac de 13 → 16 places', 28, '#5A9A8A'),
                                 (467, '1 / 5', 24, '#A68B6B'),
                                 (535, '700 Essence', 30, '#D4A843')]:
        draw.text((967, y), label, font=font(size), fill=color)
    button_native = surface((116, 28), tile)
    panel(button_native, (0, 0, 116, 28), tile, True)
    sheet.paste(button_native.resize((348, 84), Image.Resampling.NEAREST), (966, 620))
    draw.text((1090, 640), 'Acheter', font=font(28), fill='#E8E0D4')
    draw.text((48, 808), 'Étude visuelle : motif raccordable 64 × 64, aucune grande illustration étirée.',
              font=font(26), fill='#9E9494')
    sheet.save(output / 'matieres.png')
    # Vérifier la couture sur quatre exemplaires, à la même échelle de pixels.
    surface((128, 128), tile).resize((512, 512), Image.Resampling.NEAREST).save(output / 'raccords.png')


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, default=Path('/tmp/vestiges-m6'))
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    icons = {item.model().stem.removeprefix('item_'): render_icon(item.model())
             for item in [satchel(), matchbox(), essence_jar()]}
    for name, image in icons.items():
        image.save(args.output / f'{name}.png')
    comparison(icons, args.output)
    mockup(icons, stone_tile(), args.output)
    print(args.output)


if __name__ == '__main__':
    main()
