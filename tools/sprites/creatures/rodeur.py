"""
Le Rôdeur — ce qui pousse là où un humain a été effacé (Bible §6.2).
Bipède voûté, bras trop longs et inégaux qui pendent devant lui : le gauche traîne des griffes d'os, le droit se
termine en poing de pierre. Chair cendrée de ce qui a été effacé, membres noircis, plaques minérales bleutées, mousse
sur la bosse, visage creux où trois yeux ne s'alignent pas.
Reprise du 11 octobre (plan 37 M4) : la chair brune se fondait dans les sols ; les bras suivaient la pente du torse
et faisaient de la créature un bâton penché de profil.
"""
from __future__ import annotations

from dataclasses import replace

import numpy as np

from ..palette import make_emissive, make_material
from ..poses import Gait, humanoid_animations
from ..render import Part
from ..rig import Pose, Proportions, build_skeleton
from ..sdf import capsule, ellipsoid, sphere

FRAME_SIZE = (40, 48)
FRAME_PIVOT = (20.0, 45.0)
DIMENSIONS = Proportions(ankle=2.5, shin=12.0, thigh=13.0, spine=17.0, neck=0.2, head_radius=4.4, shoulder_half=9.0,
                         hip_half=3.8, upper_arm=16.0, forearm=17.0)

HIDE, HIDE_DARK, STONE, MOSS, BONE, MAW, EYE = range(7)
MATERIALS = [
    make_material("hide", "#A89C8E"),
    make_material("hide_dark", "#463A4E"),
    make_material("stone", "#7E8899"),
    make_material("moss", "#76A040"),
    make_material("bone", "#E6DCC4", contrast=0.6),
    make_material("maw", "#1E1428", contrast=0.5),
    make_emissive("eye", "#7FFF00"),
]

# Avant-bras droit tronqué : l'asymétrie se lit à la longueur des bras autant qu'à leur terminaison.
RIGHT_FOREARM_SHARE = 0.62


def _along(a: np.ndarray, b: np.ndarray, share: float) -> np.ndarray:
    return a + (b - a) * share


def parts(pose: Pose) -> list[Part]:
    s = build_skeleton(pose, DIMENSIONS)
    torso_center = (s.point("pelvis") + s.point("chest")) * 0.5
    hand_l, elbow_l = s.point("hand_l"), s.point("elbow_l")
    claw_dir = (hand_l - elbow_l) / max(np.linalg.norm(hand_l - elbow_l), 1e-6)
    fist_r = _along(s.point("elbow_r"), s.point("hand_r"), RIGHT_FOREARM_SHARE)

    result = [
        Part(lambda p, c=torso_center: ellipsoid(p, c, (6.8, 10.0, 5.2), s.torso), HIDE),
        # Bosse dorsale qui écrase la tête vers l'avant, couverte de mousse.
        Part(lambda p: ellipsoid(p, s.on_torso("chest", (0.8, 2.5, -3.2)), (7.4, 6.8, 5.4), s.torso), HIDE),
        Part(lambda p: ellipsoid(p, s.on_torso("chest", (1.8, 6.6, -3.6)), (4.8, 2.8, 3.8), s.torso), MOSS),
        Part(lambda p: ellipsoid(p, s.on_torso("pelvis", (-2.5, 4.0, -3.6)), (2.6, 2.2, 1.8), s.torso), MOSS),
        # Plaques minérales : épaule droite hypertrophiée, éclats le long du dos.
        Part(lambda p: ellipsoid(p, s.on_torso("chest", (-7.4, 0.8, -0.6)), (4.6, 4.0, 4.4), s.torso), STONE),
        Part(lambda p: capsule(p, s.on_torso("chest", (-4.0, 2.0, -6.0)), s.on_torso("chest", (-5.5, 6.5, -8.0)), 1.6, 0.6), STONE),
        Part(lambda p: capsule(p, s.on_torso("chest", (-1.0, -3.0, -7.0)), s.on_torso("chest", (-1.5, 0.5, -10.0)), 1.4, 0.5), STONE),
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 1.5, 0)), s.on_torso("pelvis", (0, -3.5, -0.5)), 5.2, 4.6), HIDE_DARK),
        # Tête en deux amas, noircie sous la chair cendrée : les yeux s'y détachent, les traits n'arrivent pas à former
        # une expression.
        Part(lambda p: sphere(p, s.point("head"), DIMENSIONS.head_radius), HIDE_DARK),
        Part(lambda p: sphere(p, s.on_head((2.2, 1.8, -0.8)), 2.9), HIDE_DARK),
        Part(lambda p: ellipsoid(p, s.on_head((0.2, -1.2, 3.2)), (3.0, 2.2, 1.6), s.head), MAW),
        Part(lambda p: ellipsoid(p, s.on_head((1.9, 1.2, 3.5)), (1.1, 1.7, 1.0), s.head), EYE),
        Part(lambda p: ellipsoid(p, s.on_head((-1.6, 0.0, 3.7)), (0.9, 1.4, 0.9), s.head), EYE),
        Part(lambda p: sphere(p, s.on_head((3.6, 2.6, 2.6)), 0.8), EYE),
        # Bras gauche : trop long, trois griffes d'os qui prolongent l'avant-bras.
        Part(lambda p: capsule(p, s.point("shoulder_l"), elbow_l, 3.0, 2.4), HIDE),
        Part(lambda p: capsule(p, elbow_l, hand_l, 2.4, 1.9), HIDE_DARK),
    ]
    spread = np.cross(claw_dir, [0.0, 1.0, 0.0])
    spread /= max(np.linalg.norm(spread), 1e-6)
    for offset in (-1.4, 0.0, 1.5):
        tip = hand_l + claw_dir * (8.5 - abs(offset)) + spread * offset * 1.9
        result.append(Part(lambda p, a=hand_l + spread * offset * 0.6, b=tip: capsule(p, a, b, 1.5, 0.6), BONE))
    result += [
        # Bras droit : plus court et massif, poing de pierre.
        Part(lambda p: capsule(p, s.point("shoulder_r"), s.point("elbow_r"), 3.4, 2.8), HIDE),
        Part(lambda p: capsule(p, s.point("elbow_r"), fist_r, 2.8, 2.6), HIDE_DARK),
        Part(lambda p: sphere(p, fist_r, 4.4), STONE),
    ]
    for side in ("l", "r"):
        result += [
            Part(lambda p, side=side: capsule(p, s.point(f"hip_{side}"), s.point(f"knee_{side}"), 3.0, 2.6), HIDE),
            Part(lambda p, side=side: capsule(p, s.point(f"knee_{side}"), s.point(f"ankle_{side}"), 2.4, 2.0), HIDE_DARK),
            Part(lambda p, side=side: capsule(p, s.point(f"ankle_{side}"), s.point(f"toe_{side}"), 2.4, 1.8), HIDE_DARK),
        ]
    return result


def _animations() -> dict[str, list[Pose]]:
    gait = Gait(lean=0.62, arm_out=0.32, stride=0.6, arm_swing=0.45, bounce=0.5, heavy=0.8)
    base = humanoid_animations(gait)
    # Tête enfoncée sous la bosse, portée en avant : le regard vient d'en dessous. Les bras pendent à la verticale,
    # devant le corps penché, au lieu de suivre la pente du dos.
    base = {name: [replace(pose, head_pitch=pose.head_pitch + 0.35,
                           shoulder_l=pose.shoulder_l + gait.lean, shoulder_r=pose.shoulder_r + gait.lean)
                   for pose in poses] for name, poses in base.items()}
    stance = base["idle"][0]
    # Coup de revers du bras long : armé loin derrière, balayage traversant, poids emporté vers l'avant.
    attack = [
        replace(stance, twist=-0.4, lean=0.45, shoulder_l=-1.0, elbow_l=1.0, shoulder_r=0.3, arm_out=0.45),
        replace(stance, twist=0.3, lean=0.7, crouch=1.0, shoulder_l=1.6, elbow_l=0.15, shoulder_r=-0.4, arm_out=0.3),
        replace(stance, twist=0.5, lean=0.75, crouch=1.8, shoulder_l=1.0, elbow_l=0.3, shoulder_r=-0.5, arm_out=0.35),
        replace(stance, twist=0.15, lean=0.6, crouch=1.0, shoulder_l=0.4, elbow_l=0.5),
    ]
    return {"idle": base["idle"], "walk": base["walk"], "attack": attack, "death": base["death"]}


ANIMATIONS = _animations()
