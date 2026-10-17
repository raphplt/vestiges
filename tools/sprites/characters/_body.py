"""Membres communs aux personnages humanoïdes : manches galbées, gants à revers, jambes et bottes structurées."""
from __future__ import annotations

from dataclasses import dataclass

import numpy as np

from ..render import Part
from ..rig import Skeleton
from ..sdf import capsule, smin, sphere, tapered_box


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
        sh = s.point(f"shoulder_{side}")
        el = s.point(f"elbow_{side}")
        ha = s.point(f"hand_{side}")
        parts += [
            Part(
                lambda p, sh=sh, el=el: capsule(p, sh, el, style.arm_radius, style.arm_radius * 0.9),
                style.sleeve,
                anchor=sh,
            ),
            Part(
                lambda p, el=el, ha=ha: capsule(p, el, ha, style.arm_radius * 0.9, style.arm_radius * 0.8),
                style.sleeve,
                anchor=el,
            ),
            Part(
                lambda p, ha=ha: sphere(p, ha, style.hand_radius),
                style.hand,
                anchor=ha,
            ),
        ]
        if not legs:
            continue
        hip = s.point(f"hip_{side}")
        knee = s.point(f"knee_{side}")
        ankle = s.point(f"ankle_{side}")
        toe = s.point(f"toe_{side}")
        boot_top = ankle_up(s, side, style.boot_height)
        parts += [
            Part(
                lambda p, hip=hip, knee=knee: capsule(p, hip, knee, style.leg_radius, style.leg_radius * 0.9),
                style.leg,
                anchor=hip,
            ),
            Part(
                lambda p, knee=knee, ankle=ankle: capsule(p, knee, ankle, style.leg_radius * 0.85, style.leg_radius * 0.8),
                style.leg,
                anchor=knee,
            ),
            Part(
                lambda p, bt=boot_top, ak=ankle: capsule(p, bt, ak, style.boot_radius, style.boot_radius),
                style.boot,
                anchor=ankle,
            ),
            Part(
                lambda p, ak=ankle, to=toe: capsule(p, ak, to, style.foot_radius, style.foot_radius * 0.85),
                style.boot,
                anchor=ankle,
            ),
        ]
    return parts
