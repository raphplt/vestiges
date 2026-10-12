"""Vestiges : fragment de sceau doré, même densité et même lumière que les objets.

Usage : python3 -m tools.sprites.ui.currency
"""
from pathlib import Path

from ..items.kit import Item, cut, line, polygon, ring
from ..weapons.icons import render_icon
from .kit import export


def vestige() -> Item:
    item = Item('vestiges', yaw=.18)
    shape = polygon([(-11, -9), (-7, -13), (6, -12), (11, -5),
                     (10, 8), (4, 13), (-8, 11), (-12, 3)], 0, 2.2)
    fracture = line([(8, 11, 0), (4, 5, 0), (7, 1, 0), (4, -4, 0), (7, -13, 0)], 1.1)
    item.add('#D4A843', cut(shape, fracture))
    item.add('copper', polygon([(-10, -8), (-6, -11), (-5, -5), (-9, -1)], 2.1, .25))
    # Un sceau entamé, assez simple pour rester lisible dans les prix.
    item.add('dark', cut(ring((-2, 0, 2.8), 5.6, .8),
                         polygon([(1, -7), (9, -7), (9, 8), (1, 8)], 3, 2)))
    item.add('paper', line([(-6, 7, 2.6), (-3, 10, 2.6), (0, 10, 2.6)], .55))
    item.add('#D4A843', polygon([(12, -10), (16, -7), (13, -3)], 0, 1.8))
    return item


if __name__ == '__main__':
    export({'ui_icon_vestiges': render_icon(vestige().model())}, Path('assets/ui/icons'), 'currency')
    print('Icône des Vestiges exportée en 32 × 32')
