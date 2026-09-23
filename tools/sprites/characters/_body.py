"""Membres communs aux personnages humanoïdes : manches, mains, jambes et chaussures."""
from __future__ import annotations

from dataclasses import dataclass

import numpy as np

from ..render import Part
from ..rig import Skeleton
from ..sdf import capsule, sphere


@dataclass(frozen=True)
class LimbStyle:
    sleeve: int
    hand: int
    leg: int
    boot: int
    arm_radius: float = 2.0
    hand_radius: float = 1.7
    leg_radius: float = 2.4
    boot_radius: float = 2.3
    boot_height: float = 4.5
    foot_radius: float = 2.0


def ankle_up(s: Skeleton, side: str, length: float) -> np.ndarray:
    ankle = s.point(f"ankle_{side}")
    knee = s.point(f"knee_{side}")
    direction = (knee - ankle) / max(np.linalg.norm(knee - ankle), 1e-6)
    return ankle + direction * length


def limbs(s: Skeleton, style: LimbStyle, legs: bool = True) -> list[Part]:
    parts: list[Part] = []
    for side in ("l", "r"):
        parts += [
            Part(lambda p, side=side: capsule(p, s.point(f"shoulder_{side}"), s.point(f"elbow_{side}"),
                                              style.arm_radius, style.arm_radius * 0.9), style.sleeve),
            Part(lambda p, side=side: capsule(p, s.point(f"elbow_{side}"), s.point(f"hand_{side}"),
                                              style.arm_radius * 0.9, style.arm_radius * 0.8), style.sleeve),
            Part(lambda p, side=side: sphere(p, s.point(f"hand_{side}"), style.hand_radius), style.hand),
        ]
        if not legs:
            continue
        parts += [
            Part(lambda p, side=side: capsule(p, s.point(f"hip_{side}"), s.point(f"knee_{side}"),
                                              style.leg_radius, style.leg_radius * 0.9), style.leg),
            Part(lambda p, side=side: capsule(p, s.point(f"knee_{side}"), s.point(f"ankle_{side}"),
                                              style.leg_radius * 0.85, style.leg_radius * 0.8), style.leg),
            Part(lambda p, side=side: capsule(p, ankle_up(s, side, style.boot_height), s.point(f"ankle_{side}"),
                                              style.boot_radius, style.boot_radius), style.boot),
            Part(lambda p, side=side: capsule(p, s.point(f"ankle_{side}"), s.point(f"toe_{side}"),
                                              style.foot_radius, style.foot_radius * 0.85), style.boot),
        ]
    return parts
