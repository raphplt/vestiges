"""Objets à effet unique, première fournée du plan 35 (lot I6)."""
from __future__ import annotations

from .kit import Item, box, cut, disk, line, oval, polygon, ring, union


def ticket() -> Item:
    item = Item("ticket_de_consigne", .08)
    stub = cut(box((0, 0, 0), (8, 13, .9), .5), union(*(oval((x, -6, 0), (1.3, 1.3, 2)) for x in (-8, 8))))
    item.add("paper", stub)
    item.add("red", box((0, 9, 1), (7, 2.2, .3)))
    item.add("dark", line([(-5, 3, 1), (5, 3, 1)], .5), line([(-5, 0, 1), (3, 0, 1)], .5))
    for x in (-6, -3, 0, 3, 6):
        item.add("dark", oval((x, -6, .9), (.7, .7, .4)))
    item.add("dark", box((0, -11, 1), (4, 1.6, .3)))
    return item


def metro_map() -> Item:
    item = Item("plan_de_metro", .12)
    item.add("paper", polygon([(-12, -10), (-4, -12), (4, -10), (12, -12), (12, 10), (4, 12), (-4, 10), (-12, 12)], 0, .9))
    item.add("blue", line([(-10, -6, 1), (-3, -1, 1), (4, -1, 1), (10, 6, 1)], .8))
    item.add("red", line([(-6, 9, 1), (-3, -1, 1), (-1, -9, 1)], .8))
    item.add("green", line([(4, 9, 1), (4, -1, 1), (8, -8, 1)], .8))
    for x, y in ((-3, -1), (4, -1)):
        item.add("light", disk((x, y, 1.3), 1.6, .4))
        item.add("dark", ring((x, y, 1.3), 1.6, .35))
    return item


def flask() -> Item:
    item = Item("flacon_de_pharmacie", .25)
    body = union(box((0, -4, 0), (7, 8, 4), 3), box((0, 6, 0), (3, 3, 2.5), 1))
    item.add("glass", body)
    item.add("green", box((0, -7.5, 0), (7.2, 4.6, 4.2), 3))
    item.add("light", box((0, -2, 4.1), (5, 3, .4)))
    item.add("red", line([(0, -4, 4.6), (0, 0, 4.6)], .6), line([(-2, -2, 4.6), (2, -2, 4.6)], .6))
    item.add("dark", box((0, 11, 0), (3.6, 2, 3), .8))
    return item


def clover() -> Item:
    item = Item("trefle_plastifie", .1)
    item.add("glass", box((0, 0, 0), (11, 11, .6), 2.5))
    leaves = union(*(oval((x, y, .9), (4, 4, .6)) for x, y in ((-3.5, 3), (3.5, 3), (-3.5, -3.5), (3.5, -3.5))))
    item.add("green", leaves, line([(1, -5, .9), (5, -10, .9)], .6))
    item.add("light", line([(-9, 8, 1.2), (-6, 9.5, 1.2)], .5))
    return item


def stopped_watch() -> Item:
    item = Item("montre_arretee", .15)
    item.add("copper", disk((0, -1, 0), 10, 2), box((0, 11, 0), (2.4, 2, 1.6), .7))
    item.add("steel", ring((0, 14, 0), 2.6, .7))
    item.add("light", disk((0, -1, 2), 8, .4))
    item.add("dark", line([(0, -1, 2.6), (0, 5, 2.6)], .6), line([(0, -1, 2.6), (-4, -3, 2.6)], .6))
    for x, y in ((0, 6), (7, -1), (0, -8), (-7, -1)):
        item.add("dark", oval((x, y, 2.5), (.6, .6, .3)))
    item.add("glass", line([(3, 5, 2.8), (6, -3, 2.8)], .45))
    return item


GADGET_ITEMS = [ticket, metro_map, flask, clover, stopped_watch]
