"""Quatorze motifs de Réminiscences : neuf prototypes conservés et cinq propositions (plan 25 S8)."""
from __future__ import annotations

from .icons import CATALOG, ENGRAVE, ESSENCE, LIGHT, PerkIcon, arc_points, arrow, at, stroke, union
from ..sdf import capsule, rounded_box, sphere


def backlash():
    """Contrecoup : chevron du dash arrivant dans une onde d'impact."""
    def motif(q):
        dash = stroke(q, [(-8, 5), (-3, 0), (-8, -5)], 1.2)
        burst = union(stroke(q, [(1, 2), (5, 7)], 1.0), stroke(q, [(3, 0), (9, 0)], 1.0),
                      stroke(q, [(1, -2), (5, -7)], 1.0))
        return [(dash, 0), (burst, 1)]
    return [LIGHT, ESSENCE], motif


def ember_transfer():
    """Braise : grande flamme qui transmet une petite braise à sa droite."""
    def motif(q):
        flame = union(capsule(q, at(-4, -5), at(-4, 2), 3.2, 1.7),
                      capsule(q, at(-4, 2), at(-1, 8), 1.7, .3))
        core = capsule(q, at(-4, -5, ENGRAVE + 1.8), at(-3, 0, ENGRAVE + 1.8), 1.2, .3)
        ember = union(sphere(q, at(7, -4), 1.7), capsule(q, at(7, -4), at(8, -1), 1.3, .3))
        leap = arrow(q, [(1, 5), (5, 6), (8, 3)], .75, 2.5)
        return [(union(flame, ember), 0), (union(core, leap), 1)]
    return [("ember", "#F28C52", .55), LIGHT], motif


def double_impact():
    """Mémoire vive : un impact et son écho, le même coup qui revient (chaque 10ᵉ impact rejoue les objets)."""
    def motif(q):
        cx, cy = -3.5, -3.0
        rays = [stroke(q, [(cx + 2.8 * dx, cy + 2.8 * dy), (cx + 6.5 * dx, cy + 6.5 * dy)], 1.05)
                for dx, dy in ((1, 0), (0, 1), (-1, 0), (0, -1))]
        sparks = [stroke(q, [(cx + 2.6 * dx, cy + 2.6 * dy), (cx + 4.2 * dx, cy + 4.2 * dy)], .8)
                  for dx, dy in ((.7, .7), (-.7, .7), (-.7, -.7), (.7, -.7))]
        blow = union(sphere(q, at(cx, cy), 2.1), *rays, *sparks)
        ex, ey = 4.6, 4.6
        # Écho : la même étoile, plus petite, reliée au coup par deux points de trajectoire.
        echo = union(sphere(q, at(ex, ey), 1.6), sphere(q, at(.4, .6), .8), sphere(q, at(2.4, 2.6), .8),
                     *(stroke(q, [(ex + 2.0 * dx, ey + 2.0 * dy), (ex + 4.4 * dx, ey + 4.4 * dy)], 1.0)
                       for dx, dy in ((1, 0), (0, 1), (-1, 0), (0, -1))))
        return [(blow, 0), (echo, 1)]
    return [LIGHT, ESSENCE], motif


def erasure_edge():
    """Lisière : sablier à la frontière effilochée de l'Effacement."""
    def motif(q):
        timer = union(stroke(q, [(-8, 7), (2, 7), (-8, -7), (2, -7), (-8, 7)], .95),
                      sphere(q, at(-3, -4), 1.6))
        boundary = union(stroke(q, [(6, 8), (6, 2), (8, 2), (8, -4)], 1.0),
                         sphere(q, at(7, -8), 1.0), sphere(q, at(10, -6), .65))
        return [(timer, 0), (boundary, 1)]
    return [LIGHT, ("erasure", "#A08BC8", .55)], motif


def dead_weight():
    """Poids mort : poids suspendu au-dessus d'une flèche vers le sol."""
    def motif(q):
        body = union(rounded_box(q, at(0, 2), (5.5, 3.4, .8), .7),
                     stroke(q, arc_points(0, 6, 2.7, 0, 180, 6), 1.0))
        down = arrow(q, [(0, -3), (0, -9)], 1.05, 3.5)
        return [(body, 0), (down, 1)]
    return [LIGHT, ESSENCE], motif


# Noms réservés à l'art : les cinq règles ne figurent pas encore dans les données.
# Leur futur ajout gameplay devra confirmer ces identifiants avant branchement.
PROPOSED = [
    PerkIcon("backlash", "combat", backlash),
    PerkIcon("ember_transfer", "combat", ember_transfer),
    PerkIcon("double_impact", "combat", double_impact),
    PerkIcon("erasure_edge", "survival", erasure_edge),
    PerkIcon("dead_weight", "combat", dead_weight),
]
ALL_ICONS = CATALOG + PROPOSED
NAMES = {
    "priority_targeting": "Convergence", "overflow": "Débordement", "carry_control": "Propagation",
    "overheal_reserve": "Prévoyance", "rally": "Reprise", "xp_trail": "Sillage",
    "salvage_xp": "Délestage", "carried_choice": "Seconde lecture", "familiar_loot": "Habitude",
    "backlash": "Contrecoup", "ember_transfer": "Braise", "double_impact": "Mémoire vive",
    "erasure_edge": "Lisière", "dead_weight": "Poids mort",
}
FAMILY_NAMES = {"combat": "Combat", "survival": "Survie", "collection": "Collecte", "rewards": "Récompense"}
