"""Campement M6 : même rendu, lumière et contours que les objets du jeu.

Usage : python3 -m tools.sprites.ui.camp ; modèles 32×32, sans agrandissement lissé.
"""
from pathlib import Path

import numpy as np
from PIL import Image

from ..items.kit import Item, box, cut, disk, line, oval, polygon, ring, union
from ..weapons.icons import render_icon
from .kit import board, export


def satchel() -> Item:
    item = Item('camp_pockets')
    body = box((0, -2, 0), (10, 11, 4), 2)
    item.add('green', body, box((0, 7, 1), (10.5, 3, 4.5), 1))
    item.add('leather', line([(-6, 10, 0), (-6, 16, 0), (6, 16, 0), (6, 10, 0)], 1.4),
             box((-5, -5, 4), (1.2, 9, 1), .4), box((5, -5, 4), (1.2, 9, 1), .4))
    item.add('yellow', box((-5, 0, 5.2), (1.8, 1.5, .7)),
             box((5, 0, 5.2), (1.8, 1.5, .7)))
    item.add('patina', box((0, -8, 4.2), (3, 3, 1.2), .6))
    return item


def matchbox() -> Item:
    item = Item('camp_banishes', yaw=.5)
    item.add('paper', box((0, -2, 0), (8, 11, 4), .6))
    sleeve = cut(box((0, -1, 0), (8.7, 10, 4.6), .4),
                 box((0, -1, 0), (7.9, 12, 3.9), .2))
    item.add('red', sleeve)
    item.add('wood', box((8.8, -1, 0), (.5, 8, 3.5)))
    item.add('paper', box((0, -1, 4.8), (5, 6.5, .25), .3))
    item.add('dark', line([(-2, -4, 5.2), (2, 2, 5.2)], .7))
    item.add('wood', line([(-7, 9, 0), (-7, 19, 0)], .9))
    item.add('red', oval((-7, 19, 0), (1.5, 2, 1.5)))
    return item


def essence_jar() -> Item:
    item = Item('camp_start_essence')
    item.add('glass', union(box((0, -3, 0), (7.5, 9, 5), 3),
                            box((0, 7, 0), (4.5, 3, 3.5), 1)))
    item.add('patina', box((0, -7, .2), (7.6, 4, 5.2), 2.5))
    item.add('wood', box((0, 11, 0), (5.5, 2, 4.2), .6))
    item.add('paper', box((0, -2, 5), (4, 2.5, .45), .4))
    item.add('copper', ring((0, 7, 0), 4.9, .7))
    return item


def weapon_rack() -> Item:
    item = Item('camp_weapon_slots', yaw=.2)
    item.add('wood', box((-9, 0, 0), (1.5, 14, 2)),
             box((9, 0, 0), (1.5, 14, 2)), box((0, -10, 0), (12, 1.5, 2)),
             box((0, 6, 0), (12, 1.5, 2)))
    item.add('steel', box((-4, 9, 3), (1.4, 9, 1)), box((4, 8, 3), (1.7, 10, 1)))
    item.add('copper', box((-4, -1, 3), (3.2, .8, 1.3)), box((4, -3, 3), (3.3, .8, 1.3)))
    item.add('leather', line([(-4, -2, 3), (-4, -8, 3)], 1.2),
             line([(4, -4, 3), (4, -10, 3)], 1.2))
    return item


def cards() -> Item:
    item = Item('camp_rerolls', yaw=.15)
    item.add('paper', box((-4, 2, -2), (8, 11, .7), .5),
             box((4, -2, 1), (8, 11, .7), .5))
    item.add('red', polygon([(4, -6), (7, -2), (4, 2), (1, -2)], 2, .3),
             box((-1, 6, 2), (1, 1.3, .3)))
    item.add('navy', line([(-9, -4, -.9), (-9, 8, -.9)], .7),
             box((-4, 2, -.9), (2, 2, .3)))
    return item


def tin_box() -> Item:
    item = Item('camp_vestige_gain', yaw=.25)
    item.add('red', box((0, -5, 0), (10, 5, 6), .8))
    item.add('yellow', box((0, -5, 6.2), (7, 2.5, .3), .3), box((0, -1, 0), (10.4, .8, 6.4), .3))
    item.add('red', box((0, 8, -6), (10, 7, .9), .8))
    item.add('steel', box((0, 1, -6), (10.4, .8, 1.2), .3))
    item.add('glass', polygon([(-6, 0), (-3, 7), (0, 0)], 0, 2.5), polygon([(1, 0), (4, 5), (7, 0)], 1, 2))
    item.add('yellow', polygon([(-2, 0), (0, 4), (2, 0)], 3, 1.5))
    return item


def net() -> Item:
    item = Item('camp_magnet', yaw=.12)
    item.add('wood', line([(-9, -15, 0), (1, 2, 0)], 1.3))
    item.add('steel', ring((5, 8, 0), 8, 1))
    for offset in [-3, 0, 3]:
        reach = np.sqrt(49 - offset * offset)
        item.add('paper', line([(5 + offset, 8 - reach, 0), (5 + offset, 8 + reach, 0)], .5),
                 line([(5 - reach, 8 + offset, 0), (5 + reach, 8 + offset, 0)], .5))
    return item


def notebook() -> Item:
    item = Item('camp_xp')
    item.add('red', box((0, 0, 0), (9, 13, 2), .7))
    item.add('paper', box((1, 0, 2), (7, 11.5, .8), .2))
    item.add('leather', box((-8, 0, 2), (1, 13, 1), .3))
    for y in [-5, 0, 5]:
        item.add('steel', line([(-3, y, 3), (5, y, 3)], .4))
    item.add('yellow', box((5, -13, 0), (1.1, 3, .4)))
    return item


def rabbit_foot() -> Item:
    item = Item('camp_luck')
    item.add('paper', union(oval((0, -2, 0), (6, 10, 4)), oval((-2, -10, 0), (6, 4, 4))))
    item.add('wood', box((0, 8, 0), (4, 1.3, 3)))
    item.add('copper', ring((0, 13, 0), 3.5, .9))
    item.add('leather', line([(-5, -10, 3), (-2, -10, 4)], .5),
             line([(-5, -12, 2), (-2, -12, 3)], .5))
    return item


FACTORIES = [weapon_rack, satchel, cards, matchbox, net, notebook, rabbit_foot,
             essence_jar, tin_box]


def catalogue() -> dict[str, Image.Image]:
    return {item.model().stem.removeprefix('item_'): render_icon(item.model())
            for item in (factory() for factory in FACTORIES)}


if __name__ == '__main__':
    icons = catalogue()
    export(icons, Path('assets/ui/camp'), 'camp')
    board([(name.removeprefix('camp_'), [image]) for name, image in icons.items()],
          Path('/tmp/vestiges-camp-icons-m6.png'), columns=4)
    print(f'{len(icons)} icônes générées')
