"""Sujets SDF des objets du plan 25, sans effet de gameplay ni hasard implicite."""
from __future__ import annotations

import numpy as np

from .kit import Item, box, cut, disk, line, oval, polygon, ring, union


def spring() -> Item:
    item = Item("memoire_vive")
    t = np.linspace(0, np.pi * 8, 85)
    points = [(6 * np.cos(a), -11 + a * .86, 5 * np.sin(a)) for a in t]
    points.extend([(8, 12, 0), (11, 10, 0)])
    item.add("rust", line(points, 1.35))
    item.add("steel", line(points[20:25], 1.5), line(points[60:65], 1.5))
    return item


def carbon() -> Item:
    item = Item("souffle_du_neant", .12)
    item.add("paper", polygon([(-10, -12), (11, -10), (11, 11), (-8, 13)], -1, .8))
    item.add("navy", polygon([(-12, -8), (8, -11), (9, 12), (-10, 14)], 1, .8))
    item.add("blue", polygon([(-12, -8), (-5, -5), (-10, 14)], 2, .5))
    for y in (-3, 2, 7):
        item.add("blue", line([(-6, y, 2.2), (-2, y + 1, 2.2), (4, y, 2.2)], .55))
        item.add("steel", line([(-3, y - 2, -.1), (9, y - 2, -.1)], .5))
    return item


def mirror() -> Item:
    item = Item("reflet_brise", .08)
    item.add("steel", polygon([(-11, -11), (12, -7), (5, 14)], 0, 1.4))
    item.add("glass", polygon([(-8, -9), (10, -6), (5, 11)], 1.6, .4))
    item.add("light", polygon([(-5, -8), (-1, -8), (7, 5), (5, 11)], 2.1, .25))
    item.add("dark", line([(0, -8, 2.3), (3, -3, 2.3), (2, 0, 2.3)], .45))
    item.add("violet", box((-11, -13, 0), (.7, .7, .5)))
    return item


def washer() -> Item:
    item = Item("resonance")
    item.add("copper", cut(disk((0, 0, 0), 12, 2), disk((0, 0, 0), 5, 4)))
    item.add("patina", line([(-9, 6, 2.1), (-10, 2, 2.1)], 1.2), line([(6, -8, 2.1), (9, -5, 2.1)], .8))
    item.add("light", line([(-5, 9, 2.2), (-1, 10, 2.2)], .6))
    return item


def ruler() -> Item:
    item = Item("portee_etendue", .1)
    points = [(-10, -11, 0), (-9, 10, 0), (2, -8, 1), (10, 11, 2)]
    item.add("yellow", line(points, 2.2))
    for x, y, z in points:
        item.add("steel", disk((x, y, z + 2), .9, .3))
    for y in (-6, -1, 4):
        item.add("dark", line([(-10, y, 2), (-8, y, 2)], .45))
    item.add("dark", line([(5, 1, 4), (7, 1, 4), (6, 3, 4)], .45))
    return item


def peg() -> Item:
    item = Item("persistance")
    item.add("wood", polygon([(-5, -13), (-1, -13), (-1, 12), (-5, 12)], 0, 2.5),
             polygon([(1, -13), (5, -13), (5, 12), (1, 12)], 0, 2.5))
    item.add("dark", box((0, 6, 0), (5.5, 1.5, 1)))
    item.add("steel", ring((0, 0, 2.6), 3, 1), line([(-3, 0, 2.6), (-4, -6, 2.6)], .7),
             line([(3, 0, 2.6), (4, 5, 2.6)], .7))
    return item


def glasses() -> Item:
    item = Item("oeil_critique", .05)
    for x in (-7, 7):
        lens = cut(disk((x, -1, 0), 5.5, .7), box((x, 6, 0), (7, 5, 3)))
        item.add("glass", lens)
        item.add("steel", line([(x - 5, 1, 1), (x - 4, -4, 1), (x, -6, 1), (x + 4, -4, 1), (x + 5, 1, 1)], .8))
    item.add("steel", line([(-13, 1, 0), (-10, 8, -3)], 1), line([(13, 1, 0), (12, 8, -3)], 1),
             line([(-2, 1, 1), (0, 2, 1), (2, 1, 1)], .8))
    item.add("light", line([(5, 0, 1), (8, -2, 1), (7, -4, 1)], .5))
    return item


def button() -> Item:
    item = Item("ancrage")
    holes = union(*(disk((x, y, 0), 1.8, 5) for x in (-3, 3) for y in (-3, 3)))
    item.add("leather", cut(disk((0, 2, 0), 10.5, 2), holes))
    item.add("paper", ring((0, 2, 2), 8.7, .6), line([(-3, 0, 2), (3, 5, 2)], .65),
             line([(3, 0, 2), (-3, 5, 2)], .65), line([(3, 0, 2), (5, -8, 2), (9, -11, 0), (7, -14, 0)], .7))
    return item


def spool() -> Item:
    item = Item("regeneration")
    item.add("wood", box((0, 9, 0), (8, 2, 5), 1), box((0, -9, 0), (8, 2, 5), 1))
    item.add("red", box((0, 0, 0), (6, 8, 4.5), 2))
    for y in range(-6, 8, 3):
        item.add("red", line([(-6, y, 4), (6, y + 1, 4)], .6))
    item.add("steel", line([(-8, -9, 5), (8, 14, 5)], .65))
    item.add("light", ring((8, 14, 5), 1.2, .5))
    item.add("red", line([(6, -6, 3), (12, -9, 1), (9, -13, 0)], .6))
    return item


def kneepad() -> Item:
    item = Item("peau_dure")
    item.add("dark", box((0, 7, -1), (12, 2, 1.5)), box((0, -7, -1), (12, 2, 1.5)))
    item.add("blue", box((0, 0, 0), (8, 11, 3), 3))
    item.add("steel", oval((0, 0, 2), (6, 8, 3)))
    item.add("dark", line([(-3, 1, 5), (1, 3, 5), (4, 1, 5)], .5))
    item.add("paper", line([(9, 6, 1), (12, 5, 1)], .65))
    return item


def badge() -> Item:
    item = Item("carapace", .12)
    edge = [(-11, 10), (0, 14), (11, 10), (8, -6), (0, -13), (-8, -6)]
    item.add("yellow", polygon(edge, 0, 1.2))
    item.add("red", polygon([(x * .82, y * .82) for x, y in edge], 1.4, .5))
    item.add("paper", line([(-5, -6, 2), (5, 5, 2)], .85), line([(5, -6, 2), (-5, 5, 2)], .85))
    item.add("yellow", polygon([(-4, -1), (-3, 5), (0, 9), (1, 4), (4, 6), (4, -1)], 2.5, .4))
    return item


def lace() -> Item:
    item = Item("instinct", .1)
    item.add("red", line([(0, 0, 0), (-8, 2, 0), (-12, 7, 0), (-9, 11, 0), (-4, 8, 0), (0, 0, 0),
                          (7, 10, 0), (12, 9, 0), (12, 4, 0), (0, 0, 0), (-6, -10, 0)], 1.35),
             line([(0, 0, 1), (4, -7, 1), (10, -10, 1)], 1.35), oval((0, 0, 1), (2.5, 2, 1.5)))
    item.add("light", line([(-6, -10, 0), (-7, -13, 0)], 1), line([(10, -10, 1), (12, -12, 1)], 1))
    return item


def magnet() -> Item:
    item = Item("siphon_essence")
    item.add("dark", oval((0, -1, -1), (11, 11, 2)))
    item.add("red", oval((-4, 0, 1), (7, 10, 3)), oval((4, 0, 1), (7, 10, 3)))
    item.add("wood", line([(0, 8, 0), (2, 14, 0)], 1.2))
    item.add("green", oval((5, 12, 1), (5, 2, 1)))
    item.add("paper", polygon([(7, -8), (10, -4), (9, -1), (6, -5)], 3, .5))
    item.add("light", oval((-5, 4, 3.3), (1.5, 3, .8)))
    return item


def photo() -> Item:
    item = Item("photo_de_classe", .05)
    item.add("paper", polygon([(-13, -10), (13, -10), (13, 7), (9, 11), (-13, 11)], 0, 1))
    item.add("blue", box((0, 1, 1.2), (11, 7, .5)))
    for y in (-3, 4):
        for x in (-7, 0, 7):
            item.add("dark", oval((x, y - 1, 2), (2, 2.5, .8)))
            item.add("paper", disk((x, y + 2, 2.3), 1.6, .3))
    item.add("violet", box((7, 6, 2.8), (2.1, 2.1, .4)))
    item.add("light", polygon([(9, 11), (9, 7), (13, 7)], 2, .3))
    return item


def token() -> Item:
    item = Item("jeton_de_fete")
    item.add("copper", cut(disk((0, 0, 0), 12, 1.7), disk((0, 8, 0), 1.7, 4)))
    item.add("yellow", ring((0, 0, 1.8), 10, .55))
    star = [(np.sin(a * np.pi / 5) * (6 if a % 2 == 0 else 2.7), np.cos(a * np.pi / 5) * (6 if a % 2 == 0 else 2.7) - 1) for a in range(10)]
    item.add("paper", polygon(star, 1.9, .4))
    item.add("rust", line([(7, -6, 2), (9, -2, 2)], .7))
    return item


def straw() -> Item:
    """Paille coudée de goûter : tube blanc à rayures rouges, soufflet plissé au coude, goutte rouge au bec."""
    item = Item("paille_tordue", .6)
    radius = 2.3
    bottom, elbow, top = np.array([-11., -15.]), np.array([3., 9.]), np.array([13., 13.])
    item.add("light", line([(*bottom, 0), (*elbow, 0)], radius), line([(*elbow + (2.5, 2.5), 0), (*top, 0)], radius))
    for start, end, stripes in ((bottom, elbow, (.15, .38, .61, .84)), (elbow + (2.5, 2.5), top, (.55,))):
        axis = (end - start) / np.linalg.norm(end - start)
        for t in stripes:
            c = start + (end - start) * t
            item.add("red", line([(*(c - axis * .55), 0), (*(c + axis * .55), 0)], radius + .12))
    for k in range(3):
        c = elbow + (.9 * k, .9 * k)
        item.add("steel", disk((*c, 0), radius + .35, 1.4))
    item.add("red", oval((*top + (1.2, .6), 1), (1.3, 1.3, 1.2)))
    return item


PROPERTY_ITEMS = [spring, carbon, mirror, washer, ruler, peg, glasses, button, spool, kneepad, badge, lace, magnet, photo, token, straw]
