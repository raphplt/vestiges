"""
L'Ombre — presque bidimensionnelle, une tache sombre qui glisse sur le sol (Bible §6.2). Fragile, rapide, en masse.
Reprise du 11 octobre (plan 37 M4), deux fois :
- d'abord des reflets irisés violet et cyan, qui la détachent des sols sombres ;
- puis la forme (« un peu trop simple ») : ce n'était qu'une tache d'encre à trois yeux. C'est désormais l'ombre
  d'un humain effacé qui cherche à se relever : une flaque d'encre irisée, effilochée derrière elle, d'où émergent à
  moitié une tête et des épaules. Un bras tendu se traîne devant, l'autre n'est qu'un moignon qui fond ; deux yeux
  vert-acide dans la tête, un troisième resté dans la flaque. Le Rampant d'ombre est la suite : une ombre qui a
  appris à marcher.
Plan 54 S3 : buste et flaque fondus d'un seul tenant (`smin`), reflets qui accrochent la lumière ; elle rampe en
quatre temps inégaux (la main s'agrippe, le corps se hisse, la flaque se rattrape) et se ramasse dans l'encre avant
de jaillir.
"""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, rotation_x, rotation_y, smin, sphere

FRAME_SIZE = (36, 36)
FRAME_PIVOT = (18.0, 28.0)

INK, SHEEN, FIGURE, EYE, SHEEN_COLD = range(5)
MATERIALS = [
    make_material("ink", "#1C1826", contrast=0.7),
    make_material("sheen", "#6E58AE", contrast=0.8, specular=0.55),
    make_material("figure", "#40305E", contrast=0.95, rim=0.6),
    make_emissive("eye", "#7FFF00"),
    make_material("sheen_cold", "#3E8C9E", contrast=0.8, specular=0.45),
]

K = 1.5
# Filaments de la flaque : angle (0 = avant), longueur relative. Les plus longs traînent derrière.
TENDRILS = [(0.9, 0.8), (1.7, 1.0), (2.4, 1.35), (2.9, 1.5), (3.4, 1.4), (4.0, 1.25), (4.7, 0.95), (5.5, 0.75)]


@dataclass(frozen=True)
class Stain:
    stretch: float = 1.0
    rise: float = 0.0
    ripple: float = 0.0
    lean: float = 0.0
    sink: float = 0.0
    reach: float = 0.0


def parts(s: Stain) -> list[Part]:
    flat = 1.0 - s.sink
    pool = np.array([0.0, 0.7 * K * flat, -0.6 * K])
    # Silhouette qui émerge à l'avant de la flaque : plus haute quand elle frappe, avalée quand elle meurt.
    emerge = (0.95 + 0.35 * s.rise) * (1.0 - s.sink)
    lean = rotation_x(0.28 - 0.2 * s.rise + 0.2 * s.lean) @ rotation_y(0.12 * s.ripple)
    base = np.array([0.0, 0.4 * K, 1.8 * K])
    chest = base + lean @ np.array([0.0, 5.6 * K * emerge, 0.0])
    head = chest + lean @ np.array([0.3 * K, 3.4 * K * emerge + 0.6 * K, 0.9 * K])
    shoulder_l = chest + lean @ np.array([3.4 * K, -0.4 * K, 0.0])
    shoulder_r = chest + lean @ np.array([-3.0 * K, -0.9 * K, 0.0])
    # Bras tendu : la main touche le sol devant, plus loin quand elle se traîne.
    hand = np.array([2.6 * K, 0.4 * K, (6.4 + 2.0 * s.reach + 1.5 * s.rise) * K * s.stretch])
    elbow = (shoulder_l + hand) * 0.5 + np.array([2.2 * K, 1.8 * K * emerge, 0.0])
    stump = shoulder_r + lean @ np.array([-0.8 * K, -2.4 * K, 1.0 * K])

    result = [
        # Flaque d'encre, irrégulière, étirée vers l'arrière : deux nappes fondues en une seule.
        Part(lambda p: smin(ellipsoid(p, pool, (5.6 * K, 0.9 * K * flat + 0.3, 6.0 * K * s.stretch)),
                            ellipsoid(p, pool + [-2.4 * K, 0.0, -3.4 * K * s.stretch], (3.6 * K, 0.7 * K * flat + 0.3, 3.8 * K)),
                            0.6 * K), INK),
        # Reflets irisés à la surface de l'encre.
        Part(lambda p: ellipsoid(p, pool + [2.2 * K, 0.75 * K * flat, -2.4 * K], (2.2 * K, 0.4 * K, 1.8 * K)), SHEEN),
        Part(lambda p: ellipsoid(p, pool + [-2.6 * K, 0.7 * K * flat, -0.4 * K], (1.6 * K, 0.4 * K, 1.4 * K)), SHEEN_COLD),
        Part(lambda p: ellipsoid(p, pool + [-1.0 * K, 0.55 * K * flat, -5.6 * K * s.stretch], (1.2 * K, 0.35 * K, 1.0 * K)), SHEEN_COLD),
        # Troisième œil, resté dans la flaque.
        Part(lambda p: sphere(p, pool + [-3.0 * K, 0.9 * K * flat, -2.4 * K], 0.75 * K), EYE),
    ]
    if emerge > 0.05:
        result += [
            # Torse qui sort de l'encre, épaules inégales fondues dans le buste, tête penchée vers l'avant.
            Part(lambda p: smin(smin(capsule(p, base, chest, 2.6 * K, 1.8 * K),
                                     capsule(p, shoulder_l, shoulder_r, 1.3 * K, 1.1 * K), 0.9 * K),
                                capsule(p, chest, head, 0.9 * K, 0.9 * K), 0.6 * K), FIGURE),
            Part(lambda p: sphere(p, head, 2.1 * K * min(1.0, emerge + 0.2)), FIGURE),
            Part(lambda p: capsule(p, shoulder_l, elbow, 1.1 * K, 0.95 * K), FIGURE),
            Part(lambda p: capsule(p, elbow, hand, 0.95 * K, 0.7 * K), FIGURE),
            # Doigts écartés sur le sol, qui tirent la flaque.
            *[Part(lambda p, d=d: capsule(p, hand, hand + np.array([d * K, -0.2 * K, 1.6 * K]), 0.5 * K, 0.25 * K), FIGURE)
              for d in (-0.9, 0.0, 0.9)],
            # Moignon qui fond et coule vers la flaque.
            Part(lambda p: capsule(p, shoulder_r, stump, 1.1 * K, 0.6 * K), FIGURE),
            Part(lambda p: sphere(p, head + lean @ np.array([0.9 * K, 0.4 * K, 2.1 * K]), 0.75 * K), EYE),
            Part(lambda p: sphere(p, head + lean @ np.array([-1.0 * K, 0.0, 2.2 * K]), 0.65 * K), EYE),
        ]
    for angle, length in TENDRILS:
        sway = s.ripple * (0.25 if length > 1.1 else -0.2)
        a = pool + np.array([np.sin(angle) * 3.6 * K, -0.2 * K, np.cos(angle) * 3.6 * K * s.stretch])
        b = pool + np.array([np.sin(angle + sway) * 8.0 * K * length, -0.3 * K,
                             np.cos(angle + sway) * 8.0 * K * length * s.stretch])
        result.append(Part(lambda p, a=a, b=b: capsule(p, a, b, 0.9 * K, 0.35 * K), INK))
    return result


def _animations() -> dict[str, list[Stain]]:
    rest = Stain()
    return {
        # Elle respire en se soulevant à peine, la tête cherche, la flaque ondule.
        "idle": [rest, replace(rest, ripple=0.6, rise=0.1, lean=-0.1), replace(rest, ripple=1.0, rise=0.18, lean=-0.2),
                 replace(rest, ripple=0.2, rise=0.04, lean=0.15)],
        # Reptation en quatre temps inégaux : la main se tend loin, s'agrippe et le buste se hisse en tassant la
        # flaque, puis la flaque se rattrape d'un coup et le corps se relève en glissant.
        "walk": [replace(rest, stretch=1.0, ripple=0.6, lean=0.5, rise=0.1, reach=1.5),
                 replace(rest, stretch=0.85, ripple=0.2, lean=0.95, rise=-0.2, reach=0.9),
                 replace(rest, stretch=1.38, ripple=-0.9, lean=0.1, rise=0.25, reach=0.3),
                 replace(rest, stretch=1.15, ripple=-0.3, lean=0.3, rise=0.1, reach=0.8)],
        # Frappe : elle se ramasse dans l'encre, jaillit, s'abat sur la cible, puis se laisse retomber.
        "attack": [replace(rest, sink=0.25, lean=-0.7, stretch=0.82, reach=-0.3, ripple=0.4),
                   replace(rest, rise=1.3, stretch=1.2, lean=0.35, reach=1.2),
                   replace(rest, rise=0.7, stretch=1.38, lean=1.15, reach=1.9, ripple=-0.6),
                   replace(rest, rise=0.2, stretch=1.05, lean=0.25, reach=0.6)],
        # Elle se cabre une dernière fois, puis s'enfonce dans le sol, la silhouette d'abord.
        "death": [replace(rest, sink=0.15, ripple=1.0, rise=0.6, lean=-0.6), replace(rest, sink=0.55, stretch=1.3, ripple=-0.5),
                  replace(rest, sink=0.85, stretch=1.5), replace(rest, sink=0.97, stretch=1.7)],
    }


ANIMATIONS = _animations()
