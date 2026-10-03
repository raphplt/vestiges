"""Animations humanoïdes communes, modulées par l'allure propre à chaque personnage."""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from .rig import Pose


@dataclass(frozen=True)
class Gait:
    """Allure : amplitude des foulées, posture, poids. Les valeurs de référence sont celles du Vagabond."""
    lean: float = 0.05
    arm_out: float = 0.24
    stride: float = 1.0
    arm_swing: float = 1.0
    bounce: float = 1.0
    heavy: float = 0.0


def stance_pose(gait: Gait = Gait()) -> Pose:
    return Pose(lean=gait.lean, arm_out=gait.arm_out, crouch=0.6 * gait.heavy, knee_l=0.1 + 0.1 * gait.heavy,
                knee_r=0.1 + 0.1 * gait.heavy)


def humanoid_animations(gait: Gait = Gait()) -> dict[str, list[Pose]]:
    stance = stance_pose(gait)
    contact = replace(stance, lean=gait.lean + 0.05, bob=-0.5 * gait.bounce,
                      hip_l=0.38 * gait.stride, knee_l=0.12, hip_r=-0.32 * gait.stride, knee_r=0.45,
                      shoulder_l=-0.45 * gait.arm_swing, shoulder_r=0.45 * gait.arm_swing, elbow_l=0.2, elbow_r=0.45)
    passing = replace(stance, lean=gait.lean + 0.05, bob=0.7 * gait.bounce, hip_l=-0.05, knee_l=0.08,
                      hip_r=0.3 * gait.stride, knee_r=1.0, shoulder_l=0.05, shoulder_r=-0.05)
    return {
        "idle": [
            stance,
            replace(stance, breath=0.35, bob=0.1),
            replace(stance, breath=0.7, bob=0.2, head_pitch=-0.04),
            replace(stance, breath=0.35, bob=0.1),
        ],
        "walk": [contact, passing, contact.mirrored(), passing.mirrored()],
        "dash": [
            replace(stance, crouch=2.2 + gait.heavy, lean=gait.lean + 0.3, hip_l=0.35, knee_l=0.7, hip_r=-0.25,
                    knee_r=0.6, shoulder_l=-0.7, shoulder_r=-0.7, elbow_l=0.5, elbow_r=0.5),
            replace(stance, lean=gait.lean + 0.37, bob=0.6, hip_l=0.55, knee_l=0.35, hip_r=-0.5, knee_r=0.45,
                    shoulder_l=-0.9, shoulder_r=-0.9, elbow_l=0.35, elbow_r=0.35, arm_out=gait.arm_out + 0.04),
            replace(stance, crouch=1.0, lean=gait.lean + 0.2, hip_l=0.3, knee_l=0.4, hip_r=-0.2, knee_r=0.3,
                    shoulder_l=-0.3, shoulder_r=0.2),
        ],
        "hurt": [
            replace(stance, lean=-0.3, head_pitch=-0.35, arm_out=gait.arm_out + 0.2, shoulder_l=-0.3, shoulder_r=-0.3,
                    knee_l=0.3, knee_r=0.3),
            replace(stance, lean=-0.12, crouch=1.2, head_pitch=-0.12, arm_out=gait.arm_out + 0.06, knee_l=0.4, knee_r=0.4),
        ],
        "death": [
            replace(stance, lean=-0.25, head_pitch=-0.3, arm_out=gait.arm_out + 0.16, knee_l=0.35, knee_r=0.35),
            replace(stance, crouch=5.0, lean=0.2, knee_l=1.1, knee_r=1.0, hip_l=0.4, hip_r=0.35,
                    shoulder_l=0.2, shoulder_r=0.2, head_pitch=0.3),
            replace(stance, crouch=9.0, lean=0.55, knee_l=1.9, knee_r=1.8, hip_l=0.8, hip_r=0.75,
                    shoulder_l=0.5, shoulder_r=0.4, elbow_l=0.2, elbow_r=0.2, head_pitch=0.45),
            replace(stance, crouch=11.0, lean=0.95, knee_l=2.3, knee_r=2.2, hip_l=1.1, hip_r=1.05,
                    shoulder_l=0.9, shoulder_r=0.8, elbow_l=0.1, elbow_r=0.1, head_pitch=0.6, arm_out=gait.arm_out + 0.06),
        ],
    }


IDLE_FRAMES = 6


def living_idle(gait: Gait = Gait(), frames: int = IDLE_FRAMES) -> list[Pose]:
    """
    Idle des personnages jouables : un cycle de souffle lisible à leur taille (au moins un pixel).
    Inspiration : épaules et tête qui montent, bras qui s'ouvrent un peu ; balancement latéral du poids ;
    éléments souples (`drape`) en retard d'un quart de cycle sur le balancement. Sinusoïdes : boucle sans à-coup.
    """
    stance = stance_pose(gait)
    # Un personnage lourd respire aussi fort mais se balance moins.
    sway = 0.05 * (1.0 - 0.4 * gait.heavy)
    poses = []
    for index in range(frames):
        phase = 2.0 * np.pi * index / frames
        inhale = (1.0 - np.cos(phase)) * 0.5
        poses.append(replace(stance, breath=2.6 * inhale, head_pitch=-0.05 * inhale, lean_side=sway * np.sin(phase),
                             arm_out=gait.arm_out + 0.07 * inhale, elbow_l=0.25 + 0.12 * inhale,
                             elbow_r=0.25 + 0.12 * inhale, drape=float(-np.cos(phase))))
    return poses


def character_animations(gait: Gait = Gait()) -> dict[str, list[Pose]]:
    """Animations humanoïdes avec l'idle vivant des personnages ; les créatures gardent l'idle commun."""
    return {**humanoid_animations(gait), "idle": living_idle(gait)}


WALK_FRAMES = 8


def playable_animations(gait: Gait = Gait()) -> dict[str, list[Pose]]:
    """
    Animations des personnages joués (plan 25 S3, DECISIONS §60) : plus d'images et plus de poids que l'allure commune.
    Marche en huit poses (rebond au passage, contre-balancier des épaules, transfert du poids, tissus en retard),
    dash en quatre (anticipation, étirement, groupé, réception), coup reçu en trois, mort en six.
    """
    stance = stance_pose(gait)
    heavy = gait.heavy
    walk = []
    for index in range(WALK_FRAMES):
        phase = 2.0 * np.pi * index / WALK_FRAMES
        swing = float(np.sin(phase))
        walk.append(replace(
            stance,
            lean=gait.lean + 0.08, bob=0.9 * gait.bounce * float(np.cos(2.0 * phase)),
            head_pitch=-0.05 * float(np.cos(2.0 * phase)), twist=0.14 * swing * gait.arm_swing,
            lean_side=0.045 * swing * (1.0 - 0.4 * heavy),
            hip_l=0.44 * gait.stride * swing, hip_r=-0.44 * gait.stride * swing,
            knee_l=0.1 + 1.0 * max(0.0, float(np.cos(phase))) ** 2, knee_r=0.1 + 1.0 * max(0.0, -float(np.cos(phase))) ** 2,
            shoulder_l=-0.55 * gait.arm_swing * swing, shoulder_r=0.55 * gait.arm_swing * swing,
            elbow_l=0.3 + 0.25 * max(0.0, -swing), elbow_r=0.3 + 0.25 * max(0.0, swing),
            drape=float(np.sin(phase - 1.1))))
    dash = [
        replace(stance, crouch=2.6 + heavy, lean=gait.lean - 0.06, hip_l=0.2, knee_l=0.95, hip_r=-0.1, knee_r=0.9,
                shoulder_l=-0.7, shoulder_r=-0.7, elbow_l=0.6, elbow_r=0.6, drape=-1.0),
        replace(stance, lean=gait.lean + 0.55, bob=0.9, hip_l=0.75, knee_l=0.25, hip_r=-0.75, knee_r=0.35,
                shoulder_l=-1.15, shoulder_r=-1.15, elbow_l=0.3, elbow_r=0.3, arm_out=gait.arm_out + 0.06, drape=1.0),
        replace(stance, lean=gait.lean + 0.42, crouch=0.8, hip_l=0.55, knee_l=0.95, hip_r=-0.2, knee_r=1.15,
                shoulder_l=-0.65, shoulder_r=-0.65, elbow_l=0.5, elbow_r=0.5, drape=1.0),
        replace(stance, crouch=1.8 + heavy, lean=gait.lean + 0.15, hip_l=0.3, knee_l=0.6, hip_r=-0.15, knee_r=0.55,
                shoulder_l=0.25, shoulder_r=-0.1, drape=0.2),
    ]
    hurt = [
        replace(stance, lean=-0.38, head_pitch=-0.42, arm_out=gait.arm_out + 0.28, shoulder_l=-0.5, shoulder_r=-0.5,
                elbow_l=0.8, elbow_r=0.8, knee_l=0.35, knee_r=0.35, drape=-1.0),
        replace(stance, lean=-0.22, crouch=1.3, head_pitch=-0.2, arm_out=gait.arm_out + 0.12, knee_l=0.45, knee_r=0.45,
                drape=-0.4),
        replace(stance, lean=-0.06, crouch=0.5, head_pitch=-0.05, knee_l=0.2, knee_r=0.2, drape=0.3),
    ]
    death = [
        replace(stance, lean=-0.32, head_pitch=-0.38, arm_out=gait.arm_out + 0.25, knee_l=0.35, knee_r=0.35, drape=-1.0),
        replace(stance, lean=-0.22, crouch=1.6, head_pitch=-0.3, lean_side=0.12, arm_out=gait.arm_out + 0.18,
                knee_l=0.55, knee_r=0.45, drape=-0.6),
        replace(stance, crouch=5.0, lean=0.18, lean_side=0.08, knee_l=1.15, knee_r=1.0, hip_l=0.4, hip_r=0.35,
                shoulder_l=0.2, shoulder_r=0.15, head_pitch=0.25, drape=0.2),
        replace(stance, crouch=8.0, lean=0.45, knee_l=1.75, knee_r=1.7, hip_l=0.75, hip_r=0.7,
                shoulder_l=0.45, shoulder_r=0.35, elbow_l=0.2, elbow_r=0.2, head_pitch=0.4, drape=0.6),
        replace(stance, crouch=10.5, lean=0.85, knee_l=2.2, knee_r=2.15, hip_l=1.05, hip_r=1.0,
                shoulder_l=0.85, shoulder_r=0.75, elbow_l=0.1, elbow_r=0.1, head_pitch=0.55, drape=0.9),
        replace(stance, crouch=11.5, lean=1.0, knee_l=2.35, knee_r=2.3, hip_l=1.15, hip_r=1.1,
                shoulder_l=1.0, shoulder_r=0.9, elbow_l=0.05, elbow_r=0.05, head_pitch=0.65, arm_out=gait.arm_out + 0.08,
                drape=0.4),
    ]
    return {"idle": living_idle(gait), "walk": walk, "dash": dash, "hurt": hurt, "death": death}
