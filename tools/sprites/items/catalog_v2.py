"""Catalogue d'objets V2 (plan 35 §7) : icônes des 34 nouveaux objets, sans effet de gameplay ni hasard implicite."""
from __future__ import annotations

import numpy as np

from ..sdf import capsule
from .kit import Item, box, cut, disk, line, oval, polygon, ring, union

def _rotated(points, center, angle: float) -> list[tuple[float, float]]:
    c, s = np.cos(angle), np.sin(angle)
    return [(center[0] + x * c - y * s, center[1] + x * s + y * c) for x, y in points]


def bib() -> Item:
    """Dossard de course : tissu blanc froissé, bande rouge, gros numéro, épingles aux coins, coin replié."""
    item = Item("dossard_froisse", .06)
    item.add("light", polygon([(-12, -9), (-4, -10), (5, -8.5), (12, -9.5), (11.5, 9), (2, 10), (-6, 8.5), (-12, 9.5)], 0, .8))
    item.add("red", box((0, 6.4, .9), (11, 1.6, .3), .3))
    item.add("dark", line([(-7, 2, 1), (-5, 3.2, 1), (-5, -6, 1)], .75),
             line([(-1, 3, 1), (5, 3, 1), (1, -6, 1)], .75))
    for x, y in ((-10, 8), (10, 8), (-10, -7.5)):
        item.add("steel", line([(x - 1.2, y + .6, 1.2), (x + 1.4, y - .6, 1.2)], .55))
    item.add("paper", polygon([(11.8, -9.4), (6, -8.8), (11.6, -3.5)], 1.2, .3))
    return item


def mint_tin() -> Item:
    """Boîte de pastilles en fer, couvercle vert menthe basculé derrière, pastilles blanches dedans."""
    item = Item("pastille_mentholee", .15)
    item.add("mint", disk((5, 6, -2.6), 9, 1.1))
    item.add("steel", ring((5, 6, -1.4), 7.4, .45))
    item.add("steel", disk((-1, -3, 0), 9, 2.4))
    item.add("dark", disk((-1, -3, 1.4), 7.6, 1.2))
    for x, y in ((-4, -1), (1.5, 0), (-1, -6), (3.5, -5), (-5.5, -6)):
        item.add("light", oval((x, y, 2.6), (2.3, 2.3, .9)))
    return item


def hot_water_bottle() -> Item:
    """Bouillotte en caoutchouc rouge, côtes en relief, goulot et bouchon vissé, un peu affaissée."""
    item = Item("bouillotte", .12)
    item.add("red", box((0, -3, 0), (9, 10, 2.6), 3), box((0, 8.5, 0), (3.4, 3, 2.2), 1))
    for y in range(-10, 5, 3):
        item.add("rust", line([(-6.5, y, 2.6), (6.5, y + .4, 2.6)], .45))
    item.add("dark", box((0, 12.6, 0), (4, 1.6, 2.6), .8))
    item.add("steel", box((0, 14.5, 0), (2.4, .7, 1.8), .4))
    item.add("light", line([(-7, 3, 2.4), (-7.5, -7, 2.4)], .55))
    return item


def staples() -> Item:
    """Barrette d'agrafes entamée, rouillée par plaques, une agrafe dépliée posée devant."""
    item = Item("agrafe_rouillee", .1)
    item.add("steel", box((-2, 1, 0), (4, 11, 1.6), .5))
    for y in range(-8, 12, 2):
        item.add("dark", line([(-5.8, y, 1.6), (1.8, y, 1.6)], .3))
    item.add("rust", oval((-3, 5, 1.6), (2.4, 3.5, .5)), oval((0, -6, 1.6), (2, 2.4, .5)))
    item.add("rust", line([(4, -12, 2.5), (4.5, -5, 2.5), (10, -4.5, 2.5), (11, -10, 2.5)], .75))
    return item


def merit_cards() -> Item:
    """Deux bons points décalés : grand carton crème bordé de couleur, grosse étoile dorée au centre."""
    item = Item("bons_points", .04)
    card = [(-6, -8.5), (6, -8.5), (6, 8.5), (-6, 8.5)]
    inner = [(-4.4, -6.9), (4.4, -6.9), (4.4, 6.9), (-4.4, 6.9)]
    star = [(np.sin(a * np.pi / 5) * (4 if a % 2 == 0 else 1.7), np.cos(a * np.pi / 5) * (4 if a % 2 == 0 else 1.7) + .8)
            for a in range(10)]
    for k, (cx, cy, angle, color) in enumerate(((-4, 2, .3, "blue"), (4, -2, -.2, "red"))):
        z = k * 1.4
        item.add(color, polygon(_rotated(card, (cx, cy), angle), z, .5))
        item.add("paper", polygon(_rotated(inner, (cx, cy), angle), z + .4, .3))
        item.add("yellow", polygon(_rotated(star, (cx, cy), angle), z + .8, .2))
    return item


def bottle_cap() -> Item:
    """Capsule de bouteille dentelée et cabossée, trèfle peint au centre."""
    item = Item("capsule_porte_bonheur", .15)
    teeth = [(np.cos(a) * (11.5 if i % 2 == 0 else 10), np.sin(a) * (11.5 if i % 2 == 0 else 10))
             for i, a in enumerate(np.linspace(0, 2 * np.pi, 42, endpoint=False))]
    item.add("steel", cut(polygon(teeth, 0, 1.6), oval((9, 7, 1), (3, 2.2, 2))))
    item.add("red", cut(disk((0, 0, 1.6), 8.6, .5), oval((9, 7, 1), (3, 2.2, 3))))
    for x, y in ((-2, 1.5), (2, 1.5), (0, -2)):
        item.add("green", oval((x, y + 1, 2.3), (2.3, 2.3, .5)))
    item.add("green", line([(0, -1, 2.3), (1.5, -6, 2.3)], .5))
    item.add("light", line([(-7, 4, 2.2), (-4, 7, 2.2)], .45))
    return item


def tape_tangle() -> Item:
    """Petite bobine ouverte de bande magnétique : flasques blanches, bande brune enroulée, queue qui s'échappe en boucles."""
    item = Item("bande_magnetique", .2)
    center = (-4, -4)
    holes = union(*(disk((center[0] + 4.6 * np.cos(a), center[1] + 4.6 * np.sin(a), 0), 1.7, 4) for a in (.5, 2.6, 4.7)))
    item.add("light", cut(disk((*center, 0), 9, .5), union(holes, disk((*center, 0), 1.2, 4))))
    item.add("tape", ring((*center, 1), 5.6, 1.4))
    t = np.linspace(0, 2 * np.pi * 1.6, 70)
    tail = [(center[0] + 5.8 + a * 1.25 + 2.6 * np.sin(a * 1.0), center[1] + 1 + a * 1.35 + 3.2 * np.cos(a * 1.0) - 3.2, 1.2)
            for a in t]
    item.add("tape", line(tail, .7))
    item.add("leather", line(tail[20:32], .72), line(tail[50:60], .72))
    return item


def small_key() -> Item:
    """Petite clé de journal intime en laiton, anneau rond, panneton à deux dents."""
    item = Item("petite_cle", .1)
    item.add("brass", ring((0, 7, 0), 4.6, 1.5), line([(0, 2.4, 0), (0, -12, 0)], 1.25),
             box((2.4, -10.5, 0), (2.2, 1, .9), .3), box((2.4, -7.2, 0), (1.6, .9, .9), .3))
    item.add("light", line([(-2.6, 9, 1.2), (-3.8, 6.5, 1.2)], .45), line([(-.6, 0, 1), (-.6, -7, 1)], .35))
    item.add("red", line([(0, 11.6, 0), (2, 14, 0), (-1, 15.5, 0)], .55))
    return item


def school_compass() -> Item:
    """Compas d'écolier ouvert : tête à molette, une branche à pointe, une branche à mine."""
    item = Item("compas", .05)
    item.add("steel", line([(0, 9, 0), (-8, -11, 0)], 1.15), line([(0, 9, 0), (8, -10, 0)], 1.15),
             line([(-4, -1, .5), (4, -1, .5)], .45))
    item.add("light", line([(-8, -11, 0), (-8.6, -15, 0)], .45))
    item.add("wood", line([(8, -10, 0), (8.6, -13, 0)], .95))
    item.add("dark", line([(8.6, -13, 0), (8.8, -15, 0)], .5))
    item.add("blue", disk((0, 9.5, .3), 3, 1.5), box((0, 13.5, 0), (1, 2.5, 1), .4))
    item.add("blue", disk((0, -1, 1.1), 1.6, .8))
    return item


def spyglass() -> Item:
    """Petite longue-vue de laiton dépliée : trois tubes de plus en plus fins, objectif de verre au gros bout."""
    item = Item("longue_vue", .3)
    for y0, y1, r in ((-12, -3, 3.8), (-3, 5, 3), (5, 12, 2.2)):
        item.add("yellow", line([(0, y0, 0), (0, y1, 0)], r))
    for y, r in ((-3, 3.4), (5, 2.6)):
        item.add("brass", line([(0, y - .6, 0), (0, y + .6, 0)], r + .2))
    item.add("brass", line([(0, -13.6, 0), (0, -12.2, 0)], 4.4), line([(0, 12, 0), (0, 13.2, 0)], 2.5))
    item.add("glass", oval((0, -14.3, 0), (3.6, .8, 3.6)))
    item.add("leather", line([(0, -9.5, 0), (0, -6, 0)], 3.95))
    return item


def cracked_glass() -> Item:
    """Verre de cantine strié, fêlure en éclair, un éclat tombé à côté."""
    item = Item("verre_fele", .2)
    body = cut(lambda p: capsule(p, (0, -9, 0), (0, 12, 0), 6.4, 8), box((0, 17, 0), (12, 5, 12), .2))
    item.add("glass", body)
    for x in (-5, -2.5, 0, 2.5, 5):
        item.add("light", line([(x * .9, -8, 7.5 - abs(x) * .45), (x, 10, 8.2 - abs(x) * .45)], .3))
    item.add("dark", line([(1, 11, 7.9), (-1, 6, 7.9), (2, 2, 7.9), (0, -3, 7.9)], .45))
    item.add("glass", polygon([(9, -11), (13, -10), (11, -7)], 0, .7))
    return item


def yoyo() -> Item:
    """Yo-yo de bois peint vu de trois quarts, ficelle qui se déroule jusqu'à sa boucle."""
    item = Item("yoyo", .55)
    item.add("blue", disk((2, 3, 1.8), 9, 1.5), disk((2, 3, -1.8), 9, 1.5))
    item.add("wood", disk((2, 3, 0), 3.2, .6))
    item.add("light", ring((2, 3, 3.3), 6, .55))
    item.add("wood", disk((2, 3, 3.3), 1.6, .3))
    item.add("paper", line([(2, 3, 0), (-1, -4, 0), (-6, -9, 0), (-7, -13, 0)], .45))
    item.add("paper", ring((-7.5, -14.5, 0), 1.6, .45))
    return item


def preserve_jar() -> Item:
    """Bocal de conserve : fruits orange au sirop, joint en caoutchouc, fermeture métallique, étiquette manuscrite."""
    item = Item("bocal_de_conserve", .2)
    item.add("orange", box((0, -3, 0), (8, 8.5, 6.6), 3.5))
    item.add("glass", box((0, 5.5, 0), (8, 2.5, 6.6), 1.5))
    for x, y in ((-4, -5), (3, -7), (0, 0), (4.5, 1)):
        item.add("yellow", oval((x, y, 6.2), (2.2, 2, .8)))
    item.add("orange", box((0, 8.6, 0), (8.6, .9, 7.1), .4))
    item.add("glass", box((0, 10.6, 0), (7.6, 1.6, 6.2), 1.4))
    item.add("steel", line([(-8.7, 3, 0), (-8.7, 9.5, 0), (-3, 12.5, 4), (3, 12.5, 4), (8.7, 9.5, 0), (8.7, 3, 0)], .55))
    item.add("paper", box((0, -3.5, 6.8), (4.6, 2.8, .4), .4))
    item.add("dark", line([(-3, -3, 7.2), (3, -3, 7.2)], .3), line([(-3, -5, 7.2), (1, -5, 7.2)], .3))
    item.add("light", line([(-6.5, 6, 6.3), (-6.5, -6, 6.3)], .5))
    return item


def broken_comb() -> Item:
    """Peigne de poche en écaille cassé en deux, dents manquantes."""
    item = Item("peigne_casse", .08)
    # Deux morceaux décalés ; une dent manque dans chacun, la dernière du premier est cassée court.
    pieces = (((-12.5, 0), (0, 0), 2), ((1.5, 12.5), (2.5, -2.5), 3))
    for (x0, x1), (dx, dy), missing in pieces:
        item.add("comb", box(((x0 + x1) / 2 + dx, 6 + dy, 0), ((x1 - x0) / 2, 2.2, 1), .5))
        for index, x in enumerate(np.arange(x0 + 1, x1, 2.3)):
            if index == missing:
                continue
            length = -6 if x < x1 - 2 else -2
            item.add("comb", line([(x + dx, 4 + dy, 0), (x + dx, length + dy, 0)], .5))
        item.add("dark", oval(((x0 + x1) / 2 + dx - 2, 6.5 + dy, 1), (1.6, .9, .4)))
    item.add("light", line([(-11, 7.3, 1), (-3, 7.3, 1)], .35))
    return item


def red_bulb() -> Item:
    """Ampoule à vis de chambre noire : poire de verre rouge, col étroit, culot de laiton fileté, filament qui affleure."""
    item = Item("ampoule_rouge", .15)
    item.add("red", oval((0, 5, 0), (7.5, 7.5, 7.5)), lambda p: capsule(p, (0, 1, 0), (0, -5, 0), 5.2, 3))
    item.add("brass", box((0, -8.6, 0), (3.2, 3.6, 3.2), .8))
    for y in (-10.6, -8.6, -6.6):
        item.add("dark", line([(-3.4, y + .5, 0), (3.4, y - .5, 0)], .35))
    item.add("dark", oval((0, -13, 0), (1.6, 1.1, 1.6)))
    item.add("yellow", line([(-2.5, 5, 7.2), (-1.2, 7, 7.4), (0, 5, 7.5), (1.2, 7, 7.4), (2.5, 5, 7.2)], .4))
    item.add("light", oval((-3.6, 8, 5.8), (1.3, 2, .6)))
    return item


def iridescent_marble() -> Item:
    """Grosse bille de verre, torsade de couleurs au cœur, reflet net."""
    item = Item("bille_irisee", .1)
    radius = 10
    item.add("glass", oval((0, 0, 0), (radius, radius, radius)))
    def on_sphere(points):
        return [(x, y, np.sqrt(max(radius ** 2 - x * x - y * y, 0)) - .2) for x, y in points]
    t = np.linspace(-1, 1, 24)
    for color, phase in (("violet", 0), ("orange", 2.1), ("green", 4.2)):
        curve = [(6.5 * np.sin(3 * a + phase) * np.cos(a * 1.2), 8 * a) for a in t]
        item.add(color, line(on_sphere(curve), .75))
    item.add("light", oval((-4, 5, 8.6), (2, 1.4, .6)))
    return item


def dented_buckle() -> Item:
    """Boucle de ceinture carrée cabossée, ardillon tordu, bout de cuir passé dedans."""
    item = Item("boucle_cabossee", .12)
    item.add("leather", box((-9, 0, -1.2), (6, 5.2, .9), .6))
    frame = cut(box((0, 0, 0), (10.5, 8, 1.6), 1.2), box((0, 0, 0), (7, 5, 3)))
    item.add("brass", cut(frame, oval((5, 9, 1), (3.5, 2, 2.5))))
    item.add("steel", line([(-7, 0, 1.8), (2, .8, 2.4), (6, -1.5, 2.6), (8.8, -.5, 2.2)], .7))
    item.add("light", line([(-9, 6.5, 1.6), (0, 6.8, 1.6)], .4))
    item.add("dark", line([(4, -6.8, 1.7), (8, -6.2, 1.7)], .35))
    return item


def double_slide() -> Item:
    """Deux diapositives 35 mm à cadre blanc, superposées et décalées, vue bleutée dans la fenêtre."""
    item = Item("diapositive_en_double", .05)
    for cx, cy, z in ((-3.5, 3.5, 0), (3.5, -3.5, 1.6)):
        item.add("light", cut(box((cx, cy, z), (9, 9, .7), .6), box((cx, cy, z), (6.2, 4.2, 2))))
        item.add("blue", box((cx, cy, z - .2), (6.2, 4.2, .3)))
        item.add("green", polygon([(cx - 6, cy - 4), (cx - 1, cy), (cx + 2, cy - 2), (cx + 6, cy + 1), (cx + 6, cy - 4)], z, .4))
        item.add("dark", line([(cx - 5, cy - 7, z + .8), (cx + 1, cy - 7, z + .8)], .3))
    return item


STAT_ITEMS = [bib, mint_tin, hot_water_bottle, staples, merit_cards, bottle_cap, tape_tangle, small_key, school_compass,
              spyglass, cracked_glass, yoyo, preserve_jar, broken_comb, red_bulb, iridescent_marble, dented_buckle,
              double_slide]


def door_stop() -> Item:
    """Butoir de porte trapu en caoutchouc gris, vis qui dépasse : le tampon qui renvoie ce qui le heurte."""
    item = Item("tampon_elastique", .3)
    item.add("rubber", cut(lambda p: capsule(p, (0, -6, 0), (0, 3, 0), 8, 5.2), box((0, -12, 0), (11, 5, 11))))
    item.add("rubber", box((0, -6.6, 0), (8.6, 1, 8.6), .5))
    for y in (-3, 0):
        item.add("dark", line([(-6.6 + y * .3, y, 5.4), (6.6 - y * .3, y, 5.4)], .3))
    item.add("steel", line([(0, 3, 0), (0, 10, 0)], 1.2), oval((0, 10.8, 0), (2.6, 1, 2.6)))
    item.add("light", line([(-5.5, -4, 5.4), (-3.8, 2.5, 3.8)], .45))
    return item


def foam_dart() -> Item:
    """Fléchette en mousse orange, ventouse bleue au bout, empennage bleu."""
    item = Item("flechette_mousse", .2)
    item.add("orange", line([(0, -7, 0), (0, 8, 0)], 2.5))
    item.add("blue", oval((0, 10.5, 0), (3.6, 1.4, 3.6)), line([(0, 8, 0), (0, 10, 0)], 1.4))
    for fin in ([(0, -4), (-5, -11), (-1, -9)], [(0, -4), (5, -11), (1, -9)]):
        item.add("blue", polygon(fin, 0, .6))
    item.add("blue", polygon([(0, -4), (-1, -12), (1, -12)], 0, 2.4))
    item.add("light", line([(-1.2, -4, 2), (-1.2, 6, 2)], .45))
    return item


def ball_bearing() -> Item:
    """Roulement à billes de face : bague extérieure, billes brillantes, bague intérieure, trace de graisse."""
    item = Item("roulement_lisse", .3)
    item.add("steel", ring((0, 0, 0), 10, 2.2), ring((0, 0, 0), 4.2, 1.6))
    item.add("dark", ring((0, 0, -.6), 7.1, 1.4))
    for k in range(9):
        a = k * 2 * np.pi / 9 + .2
        item.add("light", oval((7.1 * np.cos(a), 7.1 * np.sin(a), .6), (1.7, 1.7, 1.7)))
    item.add("tape", oval((-6, -7, 2), (2.2, 1.4, .5)))
    return item


def coiled_cord() -> Item:
    """Cordon de téléphone beige spiralé, fiche de plastique transparent au bout."""
    item = Item("cordon_spirale", .35)
    t = np.linspace(0, np.pi * 2 * 7, 210)
    span = t / t[-1]
    centre = np.stack([-7 + 12 * span + 2.5 * np.sin(span * np.pi * 1.4), -12 + 20 * span, np.zeros_like(span)], axis=1)
    coil = centre + np.stack([3.2 * np.cos(t), np.zeros_like(t), 3.2 * np.sin(t)], axis=1)
    item.add("paper", line([tuple(point) for point in coil], .62))
    item.add("paper", line([(tuple(centre[-1])), (7, 11, 0)], .7))
    item.add("glass", box((8, 13, 0), (1.9, 2.3, 1.5), .4))
    item.add("yellow", line([(7.2, 14.3, 1.6), (8.8, 14.3, 1.6)], .25))
    return item


def tiny_horseshoe() -> Item:
    """Petit fer à cheval porte-bonheur ouvert vers le haut, trous de clous, ruban rouge noué."""
    item = Item("fer_a_cheval_miniature", .1)
    angles = np.linspace(np.pi * 1.1, np.pi * 1.9 + np.pi * 1.0, 26) - np.pi * .5
    arc = [(8 * np.cos(a), 8 * np.sin(a) - 1, 0) for a in np.linspace(-.3, np.pi + .3, 26)]
    arc = [(x, -y - 1, z) for x, y, z in arc]
    item.add("steel", line(arc, 2.4))
    for k in (3, 7, 12, 18, 22):
        x, y, _ = arc[k]
        item.add("dark", oval((x, y, 2.1), (.65, .65, .4)))
    item.add("red", line([(-2, -10, 2.5), (-5, -13, 2), (-6, -9, 2), (-2, -10, 2.5), (2, -10, 2.5), (5, -13, 2), (6, -9, 2), (2, -10, 2.5)], .8),
             line([(-1, -10, 2.5), (-3, -15, 2)], .7), line([(1, -10, 2.5), (3, -15, 2)], .7))
    item.add("light", line([(-8.5, 2, 2), (-6, 6, 2)], .45))
    return item


def chipped_domino() -> Item:
    """Domino ivoire debout, points noirs, coin ébréché."""
    item = Item("domino_ebreche", .25)
    item.add("light", cut(box((0, 0, 0), (6.4, 12.5, 2.2), 1.2), oval((6.5, 12.5, 2), (3, 2.6, 2.4))))
    item.add("dark", line([(-5, 0, 2.3), (5, 0, 2.3)], .4))
    for x, y in ((-3, 9), (0, 6), (3, 3), (-3, -3), (3, -3), (0, -6.2), (-3, -9.4), (3, -9.4)):
        item.add("dark", oval((x, y, 2.2), (1.15, 1.15, .4)))
    return item


def cloakroom_token() -> Item:
    """Jeton de vestiaire oblong en plastique rouge, numéroté, suspendu à un crochet de portemanteau."""
    item = Item("ticket_de_vestiaire", .2)
    item.add("steel", line([(0, 15, -1), (0, 8, -1), (-.5, 5.5, -1)], 1), line([(0, 12, -1), (4, 13, -1), (6, 10, -1)], .9))
    item.add("red", cut(box((0, -4, 0), (5.6, 9, 1), 2.5), disk((0, 2.4, 0), 1.5, 3)))
    item.add("steel", ring((0, 2.4, 1.1), 1.7, .45))
    item.add("light", line([(-3.5, -1.5, 1.1), (-3.5, -3.5, 1.1), (-1, -3.5, 1.1)], .5), line([(-1.6, -1, 1.1), (-1.6, -7, 1.1)], .5),
             line([(1, -1.5, 1.1), (3.6, -1.5, 1.1), (1.8, -7, 1.1)], .5))
    return item


def crowbar() -> Item:
    """Pied-de-biche d'acier noirci, col de cygne au pied fendu, bout plat, peinture rouge écaillée sur la tige."""
    item = Item("pied_de_biche", .15)
    bend = [(0, -14, 0), (0, 7, 0)] + [(-3.6 + 3.6 * np.cos(a), 7 + 3.6 * np.sin(a), 0) for a in np.linspace(0, np.pi * .95, 8)]
    item.add("rubber", line(bend, 1.5))
    item.add("rubber", line([(-7.2, 7.4, 0), (-8.2, 3.2, 0)], 1.3))
    item.add("dark", line([(-8.2, 3.2, 1.2), (-7.7, 5.6, 1.2)], .4))
    item.add("rubber", line([(0, -14, 0), (1.4, -16.4, 0)], 1.25))
    item.add("red", line([(0, -10, 0), (0, 3, 0)], 1.62))
    item.add("rubber", oval((.6, -5, 1.3), (.9, 1.6, .4)), oval((-.5, 0, 1.3), (.8, 1.1, .4)))
    return item


def bike_bell() -> Item:
    """Sonnette de vélo vue de dessus : dôme chromé cerclé, téton central, levier à palette de laiton sur le flanc."""
    item = Item("sonnette", .15)
    item.add("dark", disk((0, 0, -.4), 10.6, .5))
    item.add("steel", oval((0, 0, 0), (9.5, 9.5, 4.5)))
    item.add("dark", ring((0, 0, 2.7), 7.6, .5), ring((0, 0, 3.9), 4.6, .45))
    item.add("steel", oval((0, 0, 4.4), (1.8, 1.8, 1.2)))
    item.add("light", oval((-4, 4, 3.4), (2.2, 1.4, .8)))
    item.add("brass", line([(7, -6, 0), (10, -10, 0)], 1.1), box((11, -12, 0), (2.4, 1.6, 1.2), .5))
    item.add("dark", box((0, -10.5, -1), (3, 1.8, 1.4), .5))
    return item


def struck_ledger() -> Item:
    """Gros registre relié toile ouvert, lignes d'écriture barrées à l'encre rouge."""
    item = Item("registre_biffe", .05)
    item.add("green", polygon([(-14, -10), (0, -12), (14, -10), (14, 11), (0, 9), (-14, 11)], -.8, .7))
    item.add("paper", polygon([(-13, -9), (-.5, -11), (-.5, 8.5), (-13, 10)], .4, .5),
             polygon([(13, -9), (.5, -11), (.5, 8.5), (13, 10)], .4, .5))
    item.add("dark", line([(0, -11, 1), (0, 8.5, 1)], .4))
    for y in (6, 2.5, -1, -4.5):
        for side in (-1, 1):
            item.add("dark", line([(side * 2.5, y + .2, 1), (side * 11.5, y + .9 * side * 0 + .6, 1)], .32))
    item.add("red", line([(-12, 3, 1.3), (-2, 2, 1.3)], .45), line([(2, -1.4, 1.3), (12, -.4, 1.3)], .45),
             line([(-11, -4.4, 1.3), (-3, -5.2, 1.3)], .45))
    return item


def red_yarn() -> Item:
    """Pelote de laine rouge, brins enroulés, un fil qui part en serpentant."""
    item = Item("pelote_rouge", .2)
    radius = 8.5
    centre = (2, 3)
    item.add("red", oval((*centre, 0), (radius, radius, radius)))
    def on_ball(points):
        return [(centre[0] + x, centre[1] + y, np.sqrt(max(radius ** 2 - x * x - y * y, 0)) + .1) for x, y in points]
    t = np.linspace(-1, 1, 20)
    for k, angle in enumerate((.4, 1.2, 2.1, 2.8)):
        c, s_ = np.cos(angle), np.sin(angle)
        strand = [(7.5 * a * c - 2.5 * np.sin(a * 2 + k) * s_, 7.5 * a * s_ + 2.5 * np.sin(a * 2 + k) * c) for a in t]
        item.add("rust", line(on_ball(strand), .4))
    item.add("red", line([(-5, -1, 2), (-8, -6, 0), (-5, -10, 0), (-10, -13, 0), (-13, -11, 0)], .6))
    item.add("light", oval((-1, 8, 7), (1.6, 1.2, .5)))
    return item


def cancel_stamp() -> Item:
    """Tampon encreur à manche de bois sur un bout de papier marqué d'un cadre rouge ANNULÉ."""
    item = Item("tampon_annule", .25)
    item.add("paper", polygon([(-13, -14), (6, -14), (8, -4), (-12, -3)], -2.5, .5))
    item.add("red", cut(box((-4, -9, -1.8), (6.5, 2.8, .2), .2), box((-4, -9, -1.8), (5.3, 1.7, 1), .1)))
    item.add("red", line([(-8.5, -9, -1.6), (.5, -9, -1.6)], .45))
    item.add("red", box((5, 0, 0), (5.2, .9, 3.2), .3))
    item.add("wood", box((5, 2.6, 0), (5, 1.8, 3), .5), line([(5, 4, 0), (5, 8, 0)], 1.6), oval((5, 10.5, 0), (3.2, 3, 3.2)))
    item.add("light", line([(3.5, 9.5, 2.5), (4, 11.5, 2.5)], .45))
    return item


def address_book() -> Item:
    """Petit carnet d'adresses de cuir, onglets alphabétiques colorés en escalier, élastique."""
    item = Item("carnet_dadresses", .2)
    item.add("leather", box((-2, 0, 0), (8.5, 12, 1.8), 1))
    item.add("paper", box((-1.6, 0, -.3), (8.4, 11.6, 1.3), .3))
    for (y, color) in zip((8.5, 4.5, .5, -3.5, -7.5), ("red", "blue", "yellow", "green", "light")):
        item.add(color, box((8, y, -.3), (2.6, 1.7, 1), .3))
    item.add("dark", line([(4, -12.4, 2), (4, 12.4, 2)], .55))
    item.add("light", line([(-9, 10, 1.9), (-2, 10, 1.9)], .4))
    item.add("paper", box((-2.5, 3, 1.9), (4, 3, .2), .3))
    item.add("dark", line([(-5, 3.6, 2.2), (0, 3.6, 2.2)], .3), line([(-5, 1.8, 2.2), (-1, 1.8, 2.2)], .3))
    return item


def rewound_cassette() -> Item:
    """Cassette audio, crayon engagé dans une bobine pour la rembobiner, boucle de bande qui pend."""
    item = Item("cassette_rembobinee", .2)
    holes = union(disk((-4.5, 1, 0), 2.2, 3), disk((4.5, 1, 0), 2.2, 3))
    item.add("rubber", cut(box((0, 0, 0), (12, 8, 1.6), 1), holes))
    item.add("paper", cut(box((0, 2, 1.6), (10, 4.6, .2), .3), box((0, 1, 1.6), (7.5, 2.2, 1), .4)))
    item.add("red", box((0, 5.6, 1.8), (10, .6, .2)))
    item.add("tape", box((0, 1, 1), (7.5, 2.2, .6), .4))
    item.add("yellow", line([(4.5, 1, 1.2), (12, 12, 2)], 1.15))
    item.add("wood", line([(12, 12, 2), (13.2, 13.8, 2)], .8))
    item.add("dark", line([(13.2, 13.8, 2), (13.8, 14.7, 2)], .35))
    item.add("red", line([(3.5, -.4, 2.2), (11, 10.6, 3)], .3))
    item.add("tape", line([(-3, -8, 0), (-5, -12, 0), (-1, -14, 0), (2, -11, 0), (1, -8, 0)], .6))
    return item


def faded_portrait() -> Item:
    """Portrait ovale dans un cadre doré : buste sombre, visage qui s'efface en pixels violets."""
    item = Item("portrait_efface", .1)
    frame = cut(oval((0, 0, 0), (10, 13, 1.6)), oval((0, 0, 1), (7.4, 10.4, 2)))
    item.add("brass", frame)
    item.add("paper", oval((0, 0, 0), (7.6, 10.6, 1)))
    item.add("leather", oval((0, -8.5, .9), (6.5, 4, .4)), oval((0, 2, .9), (3.6, 4.4, .4)))
    item.add("violet", box((-1.5, 3, 1.4), (1, 1, .3), .1), box((1, 1, 1.4), (1, 1, .3), .1),
             box((2.6, 4.6, 1.4), (.8, .8, .3), .1), box((-2.8, -.6, 1.4), (.8, .8, .3), .1))
    item.add("paper", box((0, 2.2, 1.3), (1.2, 1.2, .3), .1), box((-.4, 4.8, 1.3), (.9, .9, .3), .1))
    item.add("brass", oval((0, 13, 0), (2.4, 1.6, 1.4)))
    return item


PLAY_ITEMS = [door_stop, foam_dart, ball_bearing, coiled_cord, tiny_horseshoe, chipped_domino, cloakroom_token,
              crowbar, bike_bell, struck_ledger, red_yarn, cancel_stamp, address_book, rewound_cassette, faded_portrait]
