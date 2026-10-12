"""Corbeau (plan 39 B2) : ville et champs. Noir bleuté, bec sombre ; s'envole quand on approche."""
from __future__ import annotations

from ..palette import make_material
from ._bird import Plumage, bird_animations, bird_parts

FRAME_SIZE = (38, 34)
FRAME_PIVOT = (19.0, 28.0)
SCALE = 1.35

BODY, WING, BEAK, LEGS = range(4)
MATERIALS = [
    make_material("feathers", "#3D3D4E"),
    make_material("wing", "#2A2938"),
    make_material("beak", "#3C3835", contrast=0.6),
    make_material("legs", "#2A2626", contrast=0.6),
]
PLUMAGE = Plumage(size=1.0, body=BODY, wing=WING, head=BODY, beak=BEAK, legs=LEGS)
ANIMATIONS = bird_animations()


def parts(state):
    return bird_parts(state, PLUMAGE)
