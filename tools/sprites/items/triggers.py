"""Objets de déclencheur et objets du monde : seconde planche du plan 25."""
from __future__ import annotations

import numpy as np

from .kit import Item, box, cut, disk, line, oval, polygon, ring, union

WORLD_NAMES = {"glass_paperweight": "Presse-papier en verre (monde)", "torn_calendar": "Calendrier arraché (monde)",
               "opening_locket": "Médaillon ouvrant (monde)"}


def match() -> Item:
    item = Item("allumette_humide")
    item.add("wood", box((0, -2, 0), (1.8, 12, 1.8)))
    item.add("dark", oval((0, 11, 0), (3.3, 4, 3)))
    item.add("rust", oval((-1, 9, 2), (1.5, 1, .7)))
    item.add("glass", oval((5, -3, 0), (2.8, 4, 1.2)))
    item.add("light", line([(4, -2, 1), (4, 0, 1)], .6))
    return item


def ice() -> Item:
    item = Item("glacon")
    item.add("paper", polygon([(-12, -7), (-9, 3), (-5, 6), (8, 5), (13, -5), (5, -11), (-6, -12)], 0, 2))
    for x in (-7, 0, 7):
        item.add("blue", line([(x, -9, 2), (x, 4, 2)], .5))
    for y in (-7, 0):
        item.add("blue", line([(-9, y, 2), (10, y, 2)], .5))
    item.add("glass", box((0, 4, 1), (6, 5, 4), 1))
    item.add("light", line([(-4, 2, 5), (-4, 7, 5), (2, 7, 5)], .7))
    item.add("paper", polygon([(-3, 0), (-7, 11), (-2, 9), (0, 2), (4, 12), (7, 10), (4, 0)], 5, .8))
    return item


def thermometer() -> Item:
    item = Item("thermometre")
    item.add("light", box((0, 2, 0), (3.4, 12, 1.6), 2), disk((0, -10, 0), 4.8, 1.5))
    item.add("red", line([(0, -10, 1.8), (0, 10, 1.8)], 1), disk((0, -10, 1.8), 2.8, .6))
    for y in (-4, 0, 4, 8):
        item.add("dark", line([(1.7, y, 1.7), (3, y, 1.7)], .4))
    return item


def pin() -> Item:
    item = Item("epingle_a_nourrice", .05)
    item.add("steel", ring((0, -10, 0), 2.5, .8), line([(-2, -9, 0), (-4, 10, 0), (-1, 13, 0), (3, 12, 0)], .9),
             line([(1, -8, 0), (9, 9, 0)], .8))
    item.add("steel", box((1, 11, 0), (2.5, 2, 1.8), .8))
    item.add("light", line([(-4, 1, .7), (-4, 7, .7)], .5), oval((9, 9, 0), (.6, 1.3, .6)))
    return item


def firecracker() -> Item:
    item = Item("petard_mouille")
    item.add("red", box((0, -3, 0), (4.5, 9, 4), 2))
    item.add("paper", box((0, 3, 3.6), (4, 1, .5)))
    item.add("dark", line([(0, 6, 0), (2, 10, 0), (-1, 13, 0), (2, 15, 0)], 1))
    item.add("glass", oval((7, -8, 0), (1.7, 2.7, 1)), oval((-6, 5, 0), (1.4, 2.3, 1)))
    return item


def thimble() -> Item:
    item = Item("de_a_coudre", .3)
    shell = union(box((0, -1, 0), (8, 8, 6), 2), oval((0, 6, 0), (7.8, 6, 6)))
    holes = union(*(oval((x, y, 5.8), (.8, .8, 1)) for x in (-4, 0, 4) for y in (-3, 1, 5)))
    item.add("steel", cut(shell, holes))
    item.add("dark", box((0, -9, 0), (8, 1.4, 6), 1))
    item.add("light", line([(-6, -7, 5), (5, -7, 5)], .6))
    return item


def sole() -> Item:
    item = Item("semelle_usee")
    shape = union(oval((0, 6, 0), (7, 9, 1.5)), oval((1, -7, 0), (5, 7, 1.5)))
    item.add("leather", cut(shape, oval((1, -9, 0), (2.5, 3, 4))))
    for y in (0, 4, 8):
        item.add("dark", line([(-4, y, 1.6), (0, y - 1, 1.6), (4, y, 1.6)], .55))
    item.add("paper", line([(-5, 3, 1.5), (-6, 8, 1.5)], .5))
    return item


def bandages() -> Item:
    item = Item("boite_de_pansements", .1)
    item.add("steel", box((0, -3, 0), (12, 7, 4), 1))
    item.add("dark", box((0, 2, 2), (10, 3, 2), .5))
    item.add("light", polygon([(-12, 2), (12, 3), (11, 13), (-11, 12)], 3, 1))
    item.add("red", box((0, 8, 4.2), (1.5, 3.5, .4)), box((0, 8, 4.2), (4, 1.3, .4)))
    item.add("paper", box((1, 1, 4), (7, 1.3, .7)))
    return item


def magnifier() -> Item:
    item = Item("loupe_de_philateliste", .1)
    stamp = box((-2, 0, -2), (9, 11, .8))
    perforations = union(*(disk((x, y, -2), .9, 3) for x in (-11, 7) for y in range(-10, 11, 4)),
                         *(disk((x, y, -2), .9, 3) for y in (-11, 11) for x in range(-10, 8, 4)))
    item.add("paper", cut(stamp, perforations))
    item.add("blue", box((-2, 0, -.7), (6, 8, .6)))
    item.add("steel", ring((0, 5, 2), 6.5, 1.5), line([(0, -1, 2), (6, -12, 2)], 1.7))
    item.add("glass", disk((0, 5, 2), 5.4, .7))
    item.add("light", line([(-3, 3, 3), (-3, 7, 3), (0, 9, 3)], .7))
    return item


def pen() -> Item:
    item = Item("stylo_quatre_couleurs")
    item.add("blue", box((0, -2, 0), (3, 9, 2.5), 1.5))
    item.add("paper", box((0, 9, 0), (3.2, 4, 2.5), 1.2))
    for x, color in ((-2, "red"), (-.6, "green"), (.8, "blue"), (2.2, "dark")):
        item.add(color, box((x, 13, 2), (.6, 2, .8)))
    item.add("steel", polygon([(-2.4, -11), (2.4, -11), (0, -16)], 0, 1.2))
    item.add("light", line([(-1.5, -7, 2.4), (-1.5, 5, 2.4)], .5))
    return item


def stool() -> Item:
    item = Item("tabouret_de_camping", .05)
    item.add("steel", line([(-10, -11, 0), (7, 9, 0)], 1.2), line([(10, -11, -2), (-7, 9, -2)], 1.2),
             line([(1, -12, 5), (1, 9, -4)], 1.2))
    item.add("green", polygon([(-11, 9), (9, 11), (5, 4), (-7, 4)], 1, 2))
    item.add("paper", line([(-10, 8, 3), (7, 10, 3)], .6))
    item.add("dark", oval((0, -1, 1), (2, 2, 2)))
    return item


def gum() -> Item:
    item = Item("chewing_gum", .15)
    item.add("steel", polygon([(-8, -12), (7, -12), (10, 8), (6, 12), (3, 9), (-1, 12), (-5, 9), (-9, 12)], 0, 1))
    item.add("blue", box((0, -6, 1.3), (8, 5, .6)))
    item.add("paper", box((-2, 3, 1.2), (4, 5, 1)))
    item.add("light", line([(5, 0, 1.8), (6, 4, 1.8), (3, 8, 1.8)], .7), line([(-6, -7, 2), (4, -7, 2)], .65))
    return item


def vest() -> Item:
    item = Item("gilet_reflechissant", .05)
    vertices = [(-11, -12), (11, -12), (11, 4), (7, 6), (6, 13), (3, 13), (0, 7), (-3, 13), (-6, 13), (-7, 6), (-11, 4)]
    item.add("yellow", polygon(vertices, 0, 1.4))
    item.add("steel", box((0, -4, 1.6), (10, 1.2, .5)), box((0, -8, 1.6), (10, 1.2, .5)),
             line([(-5, -1, 1.5), (-5, 10, 1.5)], .85), line([(5, -1, 1.5), (5, 10, 1.5)], .85))
    item.add("dark", line([(0, -12, 2), (0, 7, 2)], .55))
    return item


def thermos() -> Item:
    item = Item("thermos")
    item.add("red", box((0, -1, 0), (6, 11, 4), 2))
    for x in (-3, 3):
        item.add("dark", box((x, -1, 3.9), (.7, 10, .4)))
    for y in (-8, -2, 4):
        item.add("paper", box((0, y, 4), (5, .6, .3)))
    item.add("steel", box((0, 12, 0), (6.2, 3, 4), 1), box((0, -12, 0), (6, 1, 4)))
    item.add("dark", line([(6, 12, 0), (9, 12, 0), (9, 8, 0), (6, 8, 0)], .85))
    return item


def medal() -> Item:
    item = Item("medaille_cabossee", .1)
    for x, color in ((-4, "blue"), (0, "light"), (4, "red")):
        item.add(color, polygon([(x - 2, 14), (x + 2, 14), (x + 1, 1), (x - 1, 1)], 0, .9))
    item.add("copper", cut(disk((0, -5, 0), 8, 1.6), oval((7, -9, 0), (2.5, 2, 4))))
    item.add("yellow", ring((0, -5, 1.7), 6.2, .6))
    item.add("paper", line([(-2, -8, 2), (0, -2, 2), (2, -8, 2)], .7))
    return item


def purse() -> Item:
    item = Item("porte_monnaie_use", .1)
    item.add("leather", box((0, -3, 0), (11, 8, 4), 4))
    item.add("steel", line([(-10, 0, 2), (-9, 6, 2), (9, 6, 2), (10, 0, 2)], .9),
             oval((-2, 8, 2), (2, 2, 1.5)), oval((2, 8, 2), (2, 2, 1.5)))
    item.add("dark", line([(-6, 1, 4), (-3, -3, 4), (-4, -7, 4)], .5),
             line([(-3, -3, 4), (1, -4, 4), (5, -2, 4)], .5))
    return item


def paperweight() -> Item:
    item = Item("glass_paperweight", .05)
    dome = cut(oval((0, 0, 0), (12, 12, 6)), box((0, -17, 0), (15, 9, 9)))
    item.add("glass", dome)
    item.add("steel", box((0, -8, 0), (11, 1, 5), 1))
    item.add("green", line([(0, -6, 5.6), (0, 2, 5.6)], .8), oval((3, -2, 5.6), (3, 1, .7)))
    for x, y in ((-2, 3), (2, 3), (0, 5), (0, 1)):
        item.add("violet", oval((x, y, 5.8), (2, 2, .5)))
    item.add("paper", disk((0, 3, 6.2), 1.2, .4))
    item.add("light", line([(-9, -1, 4), (-8, 5, 4), (-4, 9, 3)], .9))
    return item


def calendar() -> Item:
    item = Item("torn_calendar", .07)
    item.add("steel", box((0, 0, -1), (10, 13, 1)))
    item.add("paper", polygon([(-9, -12), (9, -8), (9, 10), (-9, 10)], 1, 2))
    item.add("red", box((0, 10, 2), (10, 3, 1)))
    item.add("dark", line([(-4, 6, 3.1), (3, 6, 3.1), (3, 2, 3.1), (-4, -3, 3.1), (4, -3, 3.1)], .9))
    item.add("light", line([(-8, -8, 3), (8, -4, 3)], .7))
    for x in (-6, 6):
        item.add("steel", ring((x, 12, 2), 1.5, .5))
    return item


def locket() -> Item:
    item = Item("opening_locket", .03)
    for x in (-7, 7):
        item.add("copper", oval((x, 0, 0), (6, 10, 1.5)))
        item.add("paper", oval((x, 0, 1.5), (4.6, 8, .8)))
    item.add("steel", line([(0, -4, 0), (0, 4, 0)], 1), ring((7, 12, 0), 2, .7))
    item.add("dark", oval((-7, -3, 2.2), (3, 3, .3)), disk((-7, 2, 2.2), 2, .3))
    item.add("violet", line([(-10, 0, 2.6), (-5, 2, 2.6)], 1.5), box((-13, 3, 0), (.8, .8, .5)))
    return item


TRIGGER_ITEMS = [match, ice, thermometer, pin, firecracker, thimble, sole, bandages, magnifier, pen, stool, gum, vest, thermos, medal, purse]
WORLD_ITEMS = [paperweight, calendar, locket]
