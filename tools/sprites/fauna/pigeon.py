"""Pigeon (plan 39 B2) : ville. Gris, ailes plus claires, cou à reflets verts, pattes roses ; s'envole en groupe."""
from __future__ import annotations

from ..palette import make_material
from ._bird import Plumage, bird_animations, bird_parts

FRAME_SIZE = (32, 28)
FRAME_PIVOT = (16.0, 23.0)
SCALE = 1.15

BODY, WING, HEAD, NECK, BEAK, LEGS = range(6)
MATERIALS = [
    make_material("body", "#8C8E98"),
    make_material("wing", "#B0B2B9"),
    make_material("head", "#5D6070"),
    make_material("neck", "#4E7A6C"),
    make_material("beak", "#C9A07C", contrast=0.6),
    make_material("legs", "#B86B6B", contrast=0.6),
]
PLUMAGE = Plumage(size=0.85, body=BODY, wing=WING, head=HEAD, beak=BEAK, legs=LEGS, neck=NECK)
ANIMATIONS = bird_animations()


def parts(state):
    return bird_parts(state, PLUMAGE)
