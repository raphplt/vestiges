"""Icônes du Campement (plan 36 M5) : objets en 32 px, volumes francs, contour commun et palette courte.

Usage : python3 -m tools.sprites.ui.camp. Les silhouettes priment sur la texture : aucun bruit ni antialiasing.
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

from .kit import board, export

INK = '#1A1A2E'
LINEN = '#E8E0D4'
GREY = '#9E9494'
STONE = '#6B6161'
SHADOW = '#3A3535'
WOOD = '#A68B6B'
BROWN = '#5A4A38'
GOLD = '#D4A843'
OLIVE = '#6B7950'
MOSS = '#3A4A38'
CYAN = '#5EC4C4'
RED = '#C4432B'


def icon(commands: list[tuple]) -> Image.Image:
    image = Image.new('RGBA', (32, 32))
    draw = ImageDraw.Draw(image)
    for shape, coordinates, color, *width in commands:
        if shape == 'line':
            draw.line(coordinates, fill=color, width=width[0] if width else 1)
        else:
            getattr(draw, shape)(coordinates, fill=color)
    # Un contour extérieur d'un pixel détache les objets sans manger leurs détails.
    alpha = image.getchannel('A')
    outline = Image.new('RGBA', image.size, INK)
    outline.putalpha(alpha.filter(ImageFilter.MaxFilter(3)))
    outline.alpha_composite(image)
    return outline


def catalogue() -> dict[str, Image.Image]:
    return {
        'camp_weapon_slots': icon([
            ('rectangle', (4, 23, 28, 26), WOOD), ('rectangle', (6, 8, 8, 28), BROWN),
            ('rectangle', (24, 8, 26, 28), BROWN), ('rectangle', (5, 12, 27, 15), WOOD),
            ('rectangle', (11, 4, 13, 21), GREY), ('rectangle', (10, 17, 15, 19), GOLD),
            ('rectangle', (18, 2, 20, 22), LINEN), ('rectangle', (16, 18, 22, 20), GOLD),
            ('rectangle', (12, 21, 13, 27), BROWN), ('rectangle', (19, 22, 20, 27), BROWN),
        ]),
        'camp_pockets': icon([
            ('polygon', [(7, 7), (24, 7), (27, 13), (27, 28), (4, 28), (4, 13)], OLIVE),
            ('rectangle', (10, 3, 21, 6), BROWN), ('rectangle', (12, 4, 19, 6), LINEN),
            ('rectangle', (5, 10, 25, 13), WOOD), ('rectangle', (6, 17, 13, 26), MOSS),
            ('rectangle', (17, 17, 24, 26), MOSS), ('rectangle', (6, 17, 13, 19), LINEN),
            ('rectangle', (17, 17, 24, 19), LINEN), ('rectangle', (24, 14, 26, 27), SHADOW),
        ]),
        'camp_rerolls': icon([
            ('polygon', [(3, 7), (13, 3), (22, 22), (11, 27)], STONE),
            ('polygon', [(7, 4), (19, 4), (23, 25), (11, 27)], WOOD),
            ('rectangle', (13, 6, 27, 28), LINEN), ('rectangle', (25, 7, 27, 28), GREY),
            ('polygon', [(20, 13), (24, 18), (20, 23), (16, 18)], RED),
            ('rectangle', (15, 8, 17, 10), RED),
        ]),
        'camp_banishes': icon([
            ('polygon', [(4, 12), (12, 8), (27, 8), (27, 25), (20, 29), (4, 29)], BROWN),
            ('rectangle', (4, 12, 20, 28), RED), ('rectangle', (6, 14, 18, 25), WOOD),
            ('rectangle', (9, 16, 15, 23), LINEN), ('rectangle', (23, 9, 26, 24), SHADOW),
            ('line', (8, 3, 13, 10), LINEN, 2), ('rectangle', (7, 2, 10, 4), RED),
            ('line', (17, 2, 19, 9), WOOD, 2), ('rectangle', (16, 2, 19, 4), RED),
        ]),
        'camp_max_hp': icon([
            ('rectangle', (6, 10, 26, 24), OLIVE), ('rectangle', (6, 10, 26, 13), WOOD),
            ('rectangle', (6, 22, 26, 26), MOSS), ('ellipse', (3, 7, 16, 27), LINEN),
            ('ellipse', (5, 10, 13, 24), WOOD), ('ellipse', (7, 13, 11, 22), BROWN),
            ('rectangle', (19, 10, 21, 26), BROWN), ('rectangle', (19, 13, 21, 17), GOLD),
        ]),
        'camp_damage': icon([
            ('polygon', [(4, 17), (10, 10), (28, 10), (23, 22), (4, 22)], GREY),
            ('polygon', [(4, 22), (23, 22), (28, 10), (28, 17), (23, 27), (4, 27)], STONE),
            ('line', (9, 18, 23, 14), LINEN, 2), ('line', (12, 20, 21, 18), SHADOW, 1),
            ('polygon', [(8, 6), (24, 3), (26, 5), (11, 9)], LINEN),
            ('rectangle', (4, 7, 10, 10), BROWN),
        ]),
        'camp_attack_speed': icon([
            ('polygon', [(14, 3), (19, 3), (27, 27), (5, 27)], WOOD),
            ('polygon', [(16, 7), (19, 10), (24, 25), (9, 25)], BROWN),
            ('rectangle', (15, 8, 18, 23), LINEN), ('line', (16, 22, 23, 9), GREY, 2),
            ('rectangle', (21, 8, 25, 11), GOLD), ('rectangle', (5, 26, 27, 29), SHADOW),
        ]),
        'camp_speed': icon([
            ('polygon', [(5, 4), (13, 4), (13, 18), (18, 21), (18, 25), (4, 25)], BROWN),
            ('rectangle', (5, 5, 12, 9), WOOD), ('rectangle', (5, 24, 18, 27), GREY),
            ('polygon', [(17, 7), (25, 7), (25, 20), (29, 23), (29, 27), (16, 27)], WOOD),
            ('rectangle', (17, 8, 24, 11), LINEN), ('rectangle', (16, 27, 29, 29), STONE),
            ('line', (18, 15, 23, 15), SHADOW, 1), ('line', (18, 18, 23, 18), SHADOW, 1),
        ]),
        'camp_regen': icon([
            ('rectangle', (7, 12, 25, 23), STONE), ('ellipse', (7, 16, 25, 28), STONE),
            ('rectangle', (4, 14, 7, 20), GREY), ('rectangle', (25, 14, 28, 20), GREY),
            ('ellipse', (6, 9, 26, 16), GREY), ('rectangle', (13, 6, 19, 9), SHADOW),
            ('line', (10, 16, 10, 23), LINEN, 2), ('rectangle', (17, 25, 23, 27), SHADOW),
            ('line', (11, 4, 11, 6), LINEN, 1), ('line', (23, 3, 23, 6), GREY, 1),
        ]),
        'camp_armor': icon([
            ('polygon', [(7, 7), (12, 4), (20, 4), (25, 7), (26, 28), (6, 28)], WOOD),
            ('polygon', [(12, 4), (16, 11), (20, 4)], SHADOW),
            ('rectangle', (8, 12, 24, 25), LINEN), ('rectangle', (15, 11, 17, 27), BROWN),
            ('line', (8, 17, 24, 17), WOOD, 2), ('line', (8, 22, 24, 22), WOOD, 2),
            ('rectangle', (23, 12, 25, 27), GREY),
        ]),
        'camp_magnet': icon([
            ('line', (6, 28, 19, 13), WOOD, 3), ('ellipse', (11, 2, 29, 20), LINEN),
            ('ellipse', (14, 5, 26, 17), CYAN), ('line', (16, 7, 24, 15), LINEN, 1),
            ('line', (23, 6, 16, 15), LINEN, 1), ('line', (5, 27, 7, 29), BROWN, 2),
        ]),
        'camp_xp': icon([
            ('polygon', [(5, 6), (23, 3), (27, 6), (27, 27), (9, 29), (5, 26)], SHADOW),
            ('polygon', [(8, 7), (25, 5), (25, 26), (8, 28)], LINEN),
            ('rectangle', (5, 6, 8, 27), RED), ('line', (12, 11, 21, 10), STONE, 1),
            ('line', (12, 15, 22, 14), STONE, 1), ('line', (12, 19, 20, 18), STONE, 1),
            ('rectangle', (21, 24, 23, 30), GOLD),
        ]),
        'camp_luck': icon([
            ('ellipse', (13, 2, 21, 10), GOLD), ('ellipse', (15, 4, 19, 8), SHADOW),
            ('polygon', [(12, 9), (21, 9), (24, 16), (23, 25), (19, 29), (9, 27), (8, 21)], LINEN),
            ('polygon', [(19, 11), (22, 13), (24, 19), (22, 26), (18, 28), (18, 20)], GREY),
            ('line', (10, 24, 15, 24), WOOD, 1), ('line', (10, 26, 14, 26), WOOD, 1),
            ('rectangle', (12, 10, 21, 12), BROWN),
        ]),
        'camp_start_essence': icon([
            ('rectangle', (10, 3, 22, 7), WOOD), ('rectangle', (11, 7, 21, 9), GREY),
            ('polygon', [(11, 9), (21, 9), (25, 13), (25, 26), (22, 29), (9, 29), (6, 26), (6, 13)], GREY),
            ('polygon', [(9, 15), (22, 15), (23, 25), (21, 27), (10, 27), (8, 25)], CYAN),
            ('rectangle', (9, 11, 11, 20), LINEN), ('rectangle', (19, 22, 23, 25), '#397979'),
            ('rectangle', (12, 3, 20, 4), LINEN),
        ]),
    }


if __name__ == '__main__':
    icons = catalogue()
    export(icons, Path('assets/ui/camp'), 'camp')
    board([(name.removeprefix('camp_'), [image]) for name, image in icons.items()],
          Path('/tmp/vestiges-camp-icons.png'), columns=4)
    print('14 icônes natives 32×32 exportées ; planche : /tmp/vestiges-camp-icons.png')
