"""
L'Épouvantail — gardien des fermes du plateau (plan 57 G2), là où les champs ont été laissés à eux-mêmes.
Ce que la ville a refusé de regarder dans ses champs : l'épouvantail resté planté quand tout le monde est parti, qui a
fini par se tenir debout tout seul. Un pieu pour toute jambe, sur lequel il avance par bonds ; une traverse pour
épaules, sous un manteau de laine bleu délavé, déchiré au bas et rapiécé d'un carré rouge. La paille sort des manches,
du col et des déchirures. Pas de visage : un sac de toile cousu, noué au cou d'une corde, où deux fentes brûlent de la
lumière violette du repaire ; une bouche en points de couture. Un chapeau de paille écrasé. Deux corbeaux ne le
quittent pas, l'un sur la traverse, l'autre sur le chapeau. Il tient une faux rouillée.
Environ trois fois une créature : cadre de 208×176, pieds à (104, 160). Quatre actions : attente (il oscille sur son
pieu), marche (par bonds), frappe (le grand fauchage : il arme la faux d'un tour de buste et balaie à hauteur de
taille) et mort (il s'affaisse le long de son pieu en penchant, le chapeau tombe, les corbeaux s'envolent).
"""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, rotation_x, rotation_y, rotation_z, smin, sphere, tapered_box

FRAME_SIZE = (208, 176)
FRAME_PIVOT = (104.0, 160.0)
# Le gabarit est pensé à l'échelle d'un homme, puis agrandi d'un bloc : la densité de pixels reste celle du bestiaire.
GROWTH = 1.3
# Faux tendue, il porte loin du pieu : rayons plus longs, bornés à sa boîte.
RAY_RANGE = 460.0
BOUNDS = ((-170.0, -8.0, -170.0), (170.0, 215.0, 170.0))

COAT, COAT_DARK, BURLAP, STRAW, HAT, WOOD, ROPE, BLADE, CROW, BEAK, RAG, STITCH, EYE = range(13)
MATERIALS = [
    make_material("coat", "#46536A", contrast=1.12),
    make_material("coat_dark", "#272A38", contrast=1.05),
    make_material("burlap", "#A88E62", contrast=1.05),
    make_material("straw", "#D2B05A", contrast=1.0),
    make_material("hat", "#8C7240", contrast=1.08),
    make_material("wood", "#5E4A38", contrast=1.12),
    make_material("rope", "#7C6848", contrast=1.0),
    make_material("blade", "#7E7670", contrast=0.95, specular=0.5),
    make_material("crow", "#1C1A22", contrast=0.9, specular=0.35),
    make_material("beak", "#4A4238", contrast=0.9),
    make_material("rag", "#963C34", contrast=1.05),
    make_material("stitch", "#1C1424", contrast=0.5),
    make_emissive("eye", "#C69CFF"),
]

# Hauteurs au repos (y vers le haut, z devant, x à sa gauche).
HIP_Y = 56.0
SHOULDER_Y = 100.0
ARM_REACH = 38.0
HEAD_Y = 117.0
# La faux est tenue de la main droite (x négatif), le manche long de 72 unités.
SNATH = 72.0


@dataclass(frozen=True)
class Scarecrow:
    bob: float = 0.0
    lean: float = 0.0
    twist: float = 0.0
    reap: float = 0.0
    sway: float = 0.0
    flutter: float = 0.0
    head_tilt: float = 0.3
    roll: float = 0.0
    slump: float = 0.0
    hat_fall: float = 0.0
    crows: bool = True
    breath: float = 0.0


def parts(s: Scarecrow) -> list[Part]:
    return [Part(lambda p, d=part.distance: d(p / GROWTH) * GROWTH, part.material, part.relief) for part in _parts(s)]


def _parts(s: Scarecrow) -> list[Part]:
    # Mort : tout bascule sur le côté autour du pied du pieu.
    fall = rotation_z(s.roll)
    lift = np.array([0.0, s.bob, 0.0])
    # Buste : penché devant autour des hanches, tourné sur le pieu (le fauchage), balancé de côté.
    hip = np.array([0.0, HIP_Y, 0.0])
    upper_rotation = rotation_y(s.twist) @ rotation_x(s.lean) @ rotation_z(s.sway)

    def ground(local) -> np.ndarray:
        return fall @ (np.asarray(local, dtype=np.float64) + lift)

    # Mort : le buste glisse le long du pieu, qui reste planté.
    sink = np.array([0.0, -s.slump, 0.0])

    def at(local) -> np.ndarray:
        return ground(hip + sink + upper_rotation @ (np.asarray(local, dtype=np.float64) - hip))

    body_rotation = fall @ upper_rotation
    result = [
        # Le pieu : une jambe de bois taillée en pointe, qui sort du manteau.
        Part(lambda p: capsule(p, ground((0.0, 1.5, 0.0)), ground((0.0, HIP_Y + 6.0, 0.0)), 2.2, 3.4), WOOD),
        Part(lambda p: capsule(p, ground((0.0, 0.0, 0.0)), ground((0.0, 5.0, 0.0)), 0.6, 2.0), WOOD),
        # Manteau : buste bourré de paille, pans qui s'évasent et battent au bas.
        Part(lambda p: smin(
            ellipsoid(p, at((0.0, 86.0 + s.breath * 0.5, 0.0)), (13.5, 17.0, 9.5 + s.breath * 0.4), body_rotation),
            tapered_box(p, at((0.0, 90.0, 0.0)), at((s.flutter * 0.6, 50.0, -s.flutter)), 12.0, 16.5, 8.0, 11.5,
                        rounding=2.5), 4.0), COAT),
        # Col relevé et revers sombres.
        Part(lambda p: ellipsoid(p, at((0.0, 101.0, -1.0)), (10.5, 4.0, 8.0), body_rotation), COAT_DARK),
        Part(lambda p: capsule(p, at((5.5, 99.0, 8.0)), at((2.0, 74.0, 10.0)), 1.6, 1.2), COAT_DARK),
        Part(lambda p: capsule(p, at((-5.5, 99.0, 8.0)), at((-2.0, 74.0, 10.0)), 1.6, 1.2), COAT_DARK),
        # Ceinture de corde.
        Part(lambda p: ellipsoid(p, at((0.0, 72.0, 0.0)), (13.8, 1.8, 10.0), body_rotation), ROPE),
        # Pièce rouge cousue sur le flanc gauche.
        Part(lambda p: ellipsoid(p, at((8.0, 82.0, 8.0)), (4.0, 4.5, 2.0), body_rotation), RAG),
    ]
    # Lambeaux du bas : des bandes de laine qui pendent sous l'ourlet.
    for x, z, length in ((-10.0, 6.0, 9.0), (-4.0, 10.0, 6.0), (6.0, 9.0, 10.0), (12.0, 2.0, 7.0), (-12.0, -5.0, 8.0),
                         (2.0, -10.0, 9.0), (9.0, -7.0, 6.0)):
        top = at((x, 52.0, z))
        bottom = at((x * 1.08 + s.flutter * 0.5, 52.0 - length, z * 1.08 - s.flutter))
        result.append(Part(lambda p, a=top, b=bottom: capsule(p, a, b, 2.2, 1.2), COAT_DARK))
    # Paille qui sort d'une déchirure et sous l'ourlet.
    for start, end in (((-8.0, 62.0, 9.0), (-12.0, 57.0, 15.0)), ((-6.0, 63.0, 10.0), (-7.0, 55.0, 14.0)),
                       ((3.0, 50.0, 6.0), (4.0, 41.0, 8.0)), ((-3.0, 50.0, -6.0), (-5.0, 40.0, -8.0))):
        result.append(Part(lambda p, a=at(start), b=at(end): capsule(p, a, b, 1.1, 0.5), STRAW))

    # La traverse des épaules, sous les manches qui pendent mollement.
    result.append(Part(lambda p: capsule(p, at((ARM_REACH + 4.0, SHOULDER_Y, -1.0)),
                                         at((-ARM_REACH - 4.0, SHOULDER_Y, -1.0)), 2.0), WOOD))
    for side in (1.0, -1.0):
        shoulder = at((side * 11.0, SHOULDER_Y - 1.0, 0.0))
        elbow = at((side * 25.0, SHOULDER_Y - 4.0, 1.0))
        cuff = at((side * ARM_REACH, SHOULDER_Y - 2.0, 1.0))
        result += [
            Part(lambda p, a=shoulder, b=elbow: capsule(p, a, b, 6.0, 5.0), COAT),
            Part(lambda p, a=elbow, b=cuff: capsule(p, a, b, 5.0, 5.5), COAT),
            # Manche qui pend sous le bras comme un sac vide.
            Part(lambda p, c=at((side * 27.0, SHOULDER_Y - 9.0, 1.0)): ellipsoid(p, c, (8.0, 4.0, 4.5), body_rotation),
                 COAT_DARK),
        ]
        # Paille en gerbe au poignet.
        for dy, dz, length in ((-1.0, 0.0, 8.0), (-4.0, 2.5, 7.0), (2.0, -2.0, 6.0), (-6.0, -1.0, 6.5)):
            a = at((side * (ARM_REACH + 3.0), SHOULDER_Y - 2.0 + dy * 0.4, 1.0 + dz * 0.4))
            b = at((side * (ARM_REACH + 3.0 + length * 0.7), SHOULDER_Y - 2.0 + dy, 1.0 + dz))
            result.append(Part(lambda p, a=a, b=b: capsule(p, a, b, 1.2, 0.5), STRAW))

    result += _head(s, at, body_rotation)
    result += _scythe(s, at, body_rotation)
    if s.crows:
        result += _crow(at((ARM_REACH - 4.0, SHOULDER_Y + 3.5, -1.0)), body_rotation @ rotation_y(1.2))
    return result


def _head(s: Scarecrow, at, body_rotation: np.ndarray) -> list[Part]:
    # Sac de toile qui pend de côté sur le cou de paille.
    neck = at((0.0, 104.0, 0.0))
    head_rotation = body_rotation @ rotation_z(s.head_tilt) @ rotation_x(0.12)
    head = neck + head_rotation @ np.array([0.0, 13.0, 0.0])

    def on_head(local) -> np.ndarray:
        return head + head_rotation @ np.asarray(local, dtype=np.float64)

    result = [
        Part(lambda p: capsule(p, neck, neck + head_rotation @ np.array([0.0, 5.0, 0.0]), 3.5, 3.0), STRAW),
        Part(lambda p: smin(ellipsoid(p, head, (10.0, 11.0, 9.5), head_rotation),
                            ellipsoid(p, on_head((0.0, -8.5, 0.0)), (6.5, 3.5, 6.0), head_rotation), 2.5), BURLAP),
        # Corde nouée au cou, et le bout du nœud qui pend.
        Part(lambda p: ellipsoid(p, on_head((0.0, -9.5, 0.0)), (6.8, 1.6, 6.3), head_rotation), ROPE),
        Part(lambda p: capsule(p, on_head((4.0, -10.0, 4.5)), on_head((5.5, -16.0, 6.0)), 1.0, 0.7), ROPE),
        # Fentes des yeux qui brûlent, bouche en points de couture.
        Part(lambda p: ellipsoid(p, on_head((3.8, 1.5, 8.6)), (1.4, 2.0, 1.2), head_rotation), EYE),
        Part(lambda p: ellipsoid(p, on_head((-3.8, 1.5, 8.6)), (1.4, 2.0, 1.2), head_rotation), EYE),
        Part(lambda p: capsule(p, on_head((-4.5, -4.0, 8.2)), on_head((4.5, -4.5, 8.2)), 0.7), STITCH),
    ]
    for x in (-3.0, 0.0, 3.0):
        result.append(Part(lambda p, a=on_head((x, -2.6, 8.5)), b=on_head((x, -5.8, 8.3)): capsule(p, a, b, 0.6),
                           STITCH))
    # Chapeau de paille écrasé ; à la mort il glisse et roule.
    hat_drop = np.array([14.0, -40.0, 6.0]) * s.hat_fall
    hat_rotation = head_rotation @ rotation_z(-0.15 + 1.2 * s.hat_fall) @ rotation_x(-0.1)
    brim = on_head((0.0, 9.0, 0.0)) + hat_drop
    crown = brim + hat_rotation @ np.array([0.0, 4.5, -0.5])
    result += [
        Part(lambda p: ellipsoid(p, brim, (17.0, 1.7, 15.5), hat_rotation), HAT),
        Part(lambda p: ellipsoid(p, crown, (8.5, 5.5, 8.0), hat_rotation), HAT),
        Part(lambda p: ellipsoid(p, crown + hat_rotation @ np.array([0.0, -2.6, 0.0]), (8.8, 1.4, 8.3), hat_rotation),
             COAT_DARK),
    ]
    # Paille raide sous le bord du chapeau.
    for angle in (0.6, 1.5, 2.6, 3.6, 4.6, 5.5):
        a = on_head((np.cos(angle) * 8.5, 6.0, np.sin(angle) * 8.0)) + hat_drop * 0.0
        b = on_head((np.cos(angle) * 13.0, 2.0, np.sin(angle) * 12.0))
        result.append(Part(lambda p, a=a, b=b: capsule(p, a, b, 1.0, 0.45), STRAW))
    if s.crows:
        result += _crow(crown + hat_rotation @ np.array([2.0, 5.0, -1.0]), hat_rotation @ rotation_y(-0.6))
    return result


def _scythe(s: Scarecrow, at, body_rotation: np.ndarray) -> list[Part]:
    """La faux dans la main droite : dressée au repos, tendue à l'horizontale pour faucher."""
    hand = at((-ARM_REACH + 2.0, SHOULDER_Y - 3.0, 4.0))
    # Manche : de la verticale (reap 0) à l'horizontale vers l'extérieur droit (reap 1), un peu devant.
    snath_rotation = body_rotation @ rotation_z(1.45 * s.reap) @ rotation_x(0.1 + 0.25 * s.reap)
    up = snath_rotation @ np.array([0.0, 1.0, 0.0])
    butt = hand - up * 26.0
    head = hand + up * (SNATH - 26.0)
    # Lame courbe qui part du haut du manche vers l'avant ; à l'horizontale, elle balaie devant lui.
    blade_points = [(0.0, 0.0, 0.0), (-1.5, 1.0, 9.0), (-2.5, 0.0, 18.0), (-2.0, -2.5, 26.0), (0.0, -6.0, 32.0)]
    blade = [head + snath_rotation @ np.array(point) for point in blade_points]
    radii = [2.4, 2.2, 1.8, 1.3, 0.6]
    result = [
        Part(lambda p: capsule(p, butt, head, 1.8, 1.6), WOOD),
        # Poignée de bois sur le manche, et la main de paille qui le serre.
        Part(lambda p, c=hand + up * 12.0: capsule(p, c, c + snath_rotation @ np.array([0.0, 0.0, 7.0]), 1.3), WOOD),
        Part(lambda p: sphere(p, hand, 3.4), STRAW),
        Part(lambda p: ellipsoid(p, head + snath_rotation @ np.array([0.0, -1.0, 0.0]), (2.8, 3.0, 2.8)), ROPE),
    ]
    for i in range(len(blade) - 1):
        result.append(Part(lambda p, a=blade[i], b=blade[i + 1], ra=radii[i], rb=radii[i + 1]:
                           capsule(p, a, b, ra, rb), BLADE))
    return result


def _crow(perch: np.ndarray, rotation: np.ndarray) -> list[Part]:
    def on_crow(local) -> np.ndarray:
        return perch + rotation @ np.asarray(local, dtype=np.float64)

    return [
        Part(lambda p: ellipsoid(p, on_crow((0.0, 3.2, 0.0)), (2.8, 2.8, 4.6), rotation), CROW),
        Part(lambda p: sphere(p, on_crow((0.0, 6.0, 3.6)), 2.1), CROW),
        Part(lambda p: capsule(p, on_crow((0.0, 5.8, 5.2)), on_crow((0.0, 5.2, 7.6)), 0.8, 0.3), BEAK),
        # Queue en éventail et pattes agrippées.
        Part(lambda p: ellipsoid(p, on_crow((0.0, 2.6, -5.5)), (1.8, 0.8, 3.0), rotation), CROW),
        Part(lambda p: capsule(p, on_crow((0.8, 1.0, 0.5)), on_crow((0.8, -0.5, 1.2)), 0.45), BEAK),
        Part(lambda p: capsule(p, on_crow((-0.8, 1.0, 0.5)), on_crow((-0.8, -0.5, 1.2)), 0.45), BEAK),
    ]


def _animations() -> dict[str, list[Scarecrow]]:
    stand = Scarecrow()
    return {
        # Il oscille sur son pieu, les manches et l'ourlet bougent au vent.
        "idle": [
            stand,
            replace(stand, sway=0.03, flutter=1.0, breath=0.6, head_tilt=0.34),
            replace(stand, sway=0.05, flutter=2.0, breath=1.0, head_tilt=0.38),
            replace(stand, sway=0.02, flutter=0.8, breath=0.4, head_tilt=0.33),
        ],
        # Par bonds : il s'accroupit sur son pieu, décolle, retombe ; les pans battent à contretemps.
        "walk": [
            replace(stand, bob=0.0, lean=0.06, flutter=-1.0),
            replace(stand, bob=6.0, lean=0.12, flutter=2.5, head_tilt=0.2),
            replace(stand, bob=9.0, lean=0.08, flutter=3.5, head_tilt=0.24),
            replace(stand, bob=3.0, lean=0.02, flutter=1.0, head_tilt=0.36),
        ],
        # Le grand fauchage : il arme d'un tour de buste, faux levée, puis balaie à hauteur de taille.
        "attack": [
            replace(stand, twist=0.9, reap=0.55, lean=-0.05, flutter=1.0),
            replace(stand, twist=1.35, reap=1.0, lean=-0.08, flutter=2.0, head_tilt=0.15),
            replace(stand, twist=-1.2, reap=1.0, lean=0.12, flutter=-3.0, head_tilt=0.45),
            replace(stand, twist=-0.5, reap=0.4, lean=0.05, flutter=-1.0),
        ],
        # Il s'affaisse sur son pieu : le buste glisse et ploie en avant, la tête tombe, le chapeau roule, les corbeaux
        # s'envolent. Le pieu reste planté et ne penche qu'un peu : la chute se lit de toutes les directions sans sortir
        # du cadre.
        "death": [
            replace(stand, lean=0.18, head_tilt=0.6, flutter=1.0),
            replace(stand, slump=12.0, roll=0.12, lean=0.35, head_tilt=0.8, crows=False, hat_fall=0.25, reap=0.3,
                    flutter=-1.5),
            replace(stand, slump=24.0, roll=0.22, lean=0.55, head_tilt=0.95, crows=False, hat_fall=0.6, reap=0.6,
                    flutter=1.0),
            replace(stand, slump=33.0, roll=0.28, lean=0.7, head_tilt=1.0, crows=False, hat_fall=0.9, reap=0.9),
            replace(stand, slump=36.0, roll=0.3, lean=0.72, head_tilt=1.0, crows=False, hat_fall=1.0, reap=1.0),
        ],
    }


ANIMATIONS = _animations()
