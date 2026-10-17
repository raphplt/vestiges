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
from ..sdf import arc_smear, capsule, ellipsoid, smax, smin, sphere, tapered_box

FRAME_SIZE = (40, 48)
FRAME_PIVOT = (20.0, 45.0)
DIMENSIONS = Proportions(ankle=2.5, shin=12.0, thigh=13.0, spine=17.0, neck=0.2, head_radius=4.4, shoulder_half=9.0,
                         hip_half=3.8, upper_arm=16.0, forearm=17.0)

HIDE, HIDE_DARK, STONE, MOSS, BONE, MAW, EYE = range(7)
MATERIALS = [
    make_material("hide", "#A89C8E", contrast=1.12),
    make_material("hide_dark", "#463A4E", contrast=1.12),
    make_material("stone", "#7E8899", contrast=1.18, specular=0.45),
    make_material("moss", "#76A040", contrast=1.12),
    make_material("bone", "#E6DCC4", contrast=0.75, specular=0.6),
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
        # Plaques minérales : épaule droite hypertrophiée, éclats nets le long du dos.
        Part(lambda p: ellipsoid(p, s.on_torso("chest", (-7.4, 0.8, -0.6)), (4.6, 4.0, 4.4), s.torso), STONE),
        Part(lambda p: capsule(p, s.on_torso("chest", (-4.0, 2.0, -6.0)), s.on_torso("chest", (-5.8, 6.8, -8.2)), 1.65, 0.65), STONE),
        Part(lambda p: capsule(p, s.on_torso("chest", (-1.0, -3.0, -7.0)), s.on_torso("chest", (-1.6, 0.8, -10.2)), 1.45, 0.55), STONE),
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 1.5, 0)), s.on_torso("pelvis", (0, -3.5, -0.5)), 5.2, 4.6), HIDE_DARK),
        # Tête en deux amas, noircie sous la chair cendrée : les yeux s'y détachent.
        Part(lambda p: sphere(p, s.point("head"), DIMENSIONS.head_radius), HIDE_DARK),
        Part(lambda p: sphere(p, s.on_head((2.2, 1.8, -0.8)), 2.9), HIDE_DARK),
        Part(lambda p: ellipsoid(p, s.on_head((0.2, -1.2, 3.2)), (3.0, 2.2, 1.6), s.head), MAW),
        Part(lambda p: ellipsoid(p, s.on_head((1.9, 1.2, 3.5)), (1.1, 1.7, 1.0), s.head), EYE),
        Part(lambda p: ellipsoid(p, s.on_head((-1.6, 0.0, 3.7)), (0.9, 1.4, 0.9), s.head), EYE),
        Part(lambda p: sphere(p, s.on_head((3.6, 2.6, 2.6)), 0.85), EYE),
        # Bras gauche : trop long, trois griffes d'os qui prolongent l'avant-bras.
        Part(lambda p: capsule(p, s.point("shoulder_l"), elbow_l, 3.0, 2.4), HIDE),
        Part(lambda p: capsule(p, elbow_l, hand_l, 2.4, 2.0), HIDE_DARK),
    ]
    spread = np.cross(claw_dir, [0.0, 1.0, 0.0])
    spread /= max(np.linalg.norm(spread), 1e-6)
    for offset in (-1.4, 0.0, 1.5):
        tip = hand_l + claw_dir * (8.0 - abs(offset) * 1.0) + spread * offset * 1.9
        # Les griffes effleurent le sol sans le traverser, sinon leur pointe est coupée au bas du cadre.
        if tip[1] < 0.8:
            tip = np.array([tip[0], 0.8, tip[2]])
        result.append(Part(lambda p, a=hand_l + spread * offset * 0.6, b=tip: capsule(p, a, b, 1.45, 0.55), BONE))

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
    gait = Gait(lean=0.62, arm_out=0.32, stride=0.65, arm_swing=0.5, bounce=0.55, heavy=0.8)
    base = humanoid_animations(gait)
    # Tête enfoncée sous la bosse, portée en avant : le regard vient d'en dessous. Les bras pendent devant le corps.
    base = {
        name: [
            replace(
                pose,
                head_pitch=pose.head_pitch + 0.35,
                shoulder_l=pose.shoulder_l + gait.lean,
                shoulder_r=pose.shoulder_r + gait.lean,
            )
            for pose in poses
        ]
        for name, poses in base.items()
    }
    stance = base["idle"][0]

    # Idle nerveux et asymétrique : souffle lourd, tressaillement de l'épaule griffue et tête qui scrute.
    idle = [
        stance,
        replace(stance, breath=0.7, bob=0.25, shrug_l=0.6, lean_side=-0.04, head_pitch=stance.head_pitch - 0.06),
        replace(stance, breath=1.2, bob=0.4, shrug_l=0.9, shrug_r=-0.3, twist=0.08, head_pitch=stance.head_pitch - 0.10),
        replace(stance, breath=0.35, bob=-0.15, shrug_l=0.2, lean_side=0.03, twist=-0.05),
    ]

    # Marche prédatrice boiteuse : le côté droit (poing de pierre) écrase le sol, le côté gauche (griffes) se projette.
    walk = [
        # 1. Appui lourd côté poing de pierre (droite devant).
        replace(
            stance,
            lean=0.66,
            bob=-0.85,
            crouch=1.0,
            lean_side=-0.09,
            twist=-0.18,
            hip_l=-0.36,
            knee_l=0.45,
            hip_r=0.44,
            knee_r=0.22,
            shoulder_l=gait.lean + 0.32,
            shoulder_r=gait.lean - 0.32,
            shrug_r=0.55,
        ),
        # 2. Redressement et passage griffu.
        replace(
            stance,
            lean=0.60,
            bob=0.55,
            lean_side=-0.02,
            twist=0.04,
            hip_l=0.24,
            knee_l=1.02,
            hip_r=-0.06,
            knee_r=0.10,
            shoulder_l=gait.lean + 0.05,
            shoulder_r=gait.lean - 0.05,
            shrug_l=0.5,
        ),
        # 3. Réception griffue côté gauche.
        replace(
            stance,
            lean=0.64,
            bob=-0.5,
            crouch=0.6,
            lean_side=0.08,
            twist=0.20,
            hip_l=0.45,
            knee_l=0.16,
            hip_r=-0.34,
            knee_r=0.48,
            shoulder_l=gait.lean - 0.35,
            shoulder_r=gait.lean + 0.30,
            shrug_l=0.4,
        ),
        # 4. Propulsion asymétrique (soulèvement du poing de pierre).
        replace(
            stance,
            lean=0.58,
            bob=0.7,
            lean_side=0.02,
            twist=-0.06,
            hip_l=-0.06,
            knee_l=0.10,
            hip_r=0.30,
            knee_r=0.92,
            shoulder_l=gait.lean - 0.08,
            shoulder_r=gait.lean + 0.12,
            shrug_r=0.65,
        ),
    ]

    # Attaque en 4 temps : 1. Anticipation recroquevillée -> 2. Frappe ample -> 3. Impact au sol -> 4. Récupération.
    attack = [
        replace(
            stance,
            twist=-0.50,
            lean=0.42,
            crouch=1.4,
            shoulder_l=-1.10,
            elbow_l=1.10,
            shrug_l=1.0,
            shoulder_r=0.40,
            arm_out=0.46,
        ),
        replace(
            stance,
            twist=0.35,
            lean=0.74,
            crouch=0.8,
            bob=0.4,
            shoulder_l=1.68,
            elbow_l=0.12,
            shoulder_r=-0.45,
            arm_out=0.32,
        ),
        replace(
            stance,
            twist=0.54,
            lean=0.78,
            crouch=2.0,
            bob=-0.4,
            shoulder_l=1.08,
            elbow_l=0.30,
            shoulder_r=-0.52,
            arm_out=0.35,
        ),
        replace(stance, twist=0.16, lean=0.60, crouch=1.0, shoulder_l=0.42, elbow_l=0.50),
    ]
    return {"idle": idle, "walk": walk, "attack": attack, "death": base["death"]}


ANIMATIONS = _animations()
