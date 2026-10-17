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


def hold_arm(animations: dict[str, list[Pose]], side: str, shoulder: float, elbow: float, arm_in: float = 0.0,
             follow: float = 0.2, actions: tuple[str, ...] = ("idle", "walk", "dash")) -> dict[str, list[Pose]]:
    """
    Bras qui tient son équipement (manche du marteau, rabat de la sacoche) au lieu de balancer dans le vide (plan 54) :
    pose fixe dont le balancier commun ne garde que `follow`, pour que le coude respire encore avec le pas.
    """
    held = dict(animations)
    for action in actions:
        held[action] = [replace(pose, **{
            f"shoulder_{side}": shoulder + follow * getattr(pose, f"shoulder_{side}"),
            f"elbow_{side}": elbow,
            f"arm_in_{side}": arm_in,
        }) for pose in animations[action]]
    return held


IDLE_FRAMES = 6


def living_idle(gait: Gait = Gait(), frames: int = IDLE_FRAMES) -> list[Pose]:
    """
    Idle des personnages jouables : un cycle de souffle lisible à leur taille (au moins un pixel).
    Inspiration : épaules et tête qui montent, bras qui s'ouvrent un peu ; balancement latéral du poids ;
    éléments souples (`drape`, `drape_lift`, `drape_sway`) en retard d'un quart de cycle sur le balancement.
    """
    stance = stance_pose(gait)
    # Un personnage lourd respire aussi fort mais se balance moins.
    sway = 0.055 * (1.0 - 0.4 * gait.heavy)
    poses = []
    for index in range(frames):
        phase = 2.0 * np.pi * index / frames
        inhale = (1.0 - np.cos(phase)) * 0.5
        poses.append(replace(
            stance,
            breath=2.7 * inhale,
            head_pitch=-0.06 * inhale,
            lean_side=sway * np.sin(phase),
            shrug_l=0.45 * inhale,
            shrug_r=0.45 * inhale,
            arm_out=gait.arm_out + 0.07 * inhale,
            elbow_l=0.25 + 0.14 * inhale,
            elbow_r=0.25 + 0.14 * inhale,
            drape=float(-np.cos(phase)),
            drape_lift=float(0.45 * np.sin(phase - 0.6)),
            drape_sway=float(np.sin(phase - 1.1)),
        ))
    return poses


def character_animations(gait: Gait = Gait()) -> dict[str, list[Pose]]:
    """Animations humanoïdes avec l'idle vivant des personnages ; les créatures gardent l'idle commun."""
    return {**humanoid_animations(gait), "idle": living_idle(gait)}


WALK_FRAMES = 8


def playable_animations(gait: Gait = Gait()) -> dict[str, list[Pose]]:
    """
    Animations des personnages joués (plan 54) : cycle de marche asymétrique en 8 poses clés
    (contact lourd, écrasement/transfert, propulsion étirée, suspension haute × 2 côtés),
    dash explosif en 4 poses, coup reçu en 3, mort en 6.
    """
    stance = stance_pose(gait)
    heavy = gait.heavy
    b = gait.bounce
    s = gait.stride
    a = gait.arm_swing
    lat = 1.0 - 0.4 * heavy

    # Demi-cycle gauche (4 poses clés franches) puis son miroir droit :
    # 1. Contact : talon plante devant, corps se tasse, sac/écharpe encore soulevés par la chute.
    p_contact = replace(
        stance,
        lean=gait.lean + 0.07,
        bob=-0.75 * b,
        crouch=0.5 + 0.5 * heavy,
        head_pitch=0.04,
        twist=0.16 * a,
        lean_side=0.045 * lat,
        hip_l=0.46 * s,
        knee_l=0.14,
        hip_r=-0.40 * s,
        knee_r=0.38,
        shoulder_l=-0.58 * a,
        shoulder_r=0.54 * a,
        elbow_l=0.26,
        elbow_r=0.55,
        shrug_l=-0.25,
        shrug_r=0.35,
        drape=0.45,
        drape_lift=0.70,
        drape_sway=0.55,
    )
    # 2. Écrasement / Transfert (recoil) : flexion maximale du genou d'appui, le sac tape vers le bas en retard.
    p_crush = replace(
        stance,
        lean=gait.lean + 0.10,
        bob=-1.15 * b,
        crouch=1.05 + 0.55 * heavy,
        head_pitch=0.06,
        twist=0.08 * a,
        lean_side=0.055 * lat,
        hip_l=0.24 * s,
        knee_l=0.40,
        hip_r=-0.12 * s,
        knee_r=1.12,
        shoulder_l=-0.30 * a,
        shoulder_r=0.32 * a,
        elbow_l=0.36,
        elbow_r=0.62,
        shrug_l=0.35,
        shrug_r=-0.15,
        drape=0.90,
        drape_lift=-0.60,
        drape_sway=0.85,
    )
    # 3. Propulsion (push-off) : détente vive de la jambe d'appui, genou arrière qui monte haut devant.
    p_push = replace(
        stance,
        lean=gait.lean + 0.06,
        bob=0.60 * b,
        crouch=0.15 * heavy,
        head_pitch=-0.04,
        twist=-0.06 * a,
        lean_side=0.02 * lat,
        hip_l=-0.16 * s,
        knee_l=0.08,
        hip_r=0.28 * s,
        knee_r=1.18,
        shoulder_l=0.18 * a,
        shoulder_r=-0.18 * a,
        elbow_l=0.42,
        elbow_r=0.30,
        shrug_l=0.15,
        shrug_r=0.30,
        drape=0.60,
        drape_lift=-0.75,
        drape_sway=0.30,
    )
    # 4. Suspension haute (apex) : sommet du rebond, jambe libre projetée vers l'avant avant l'impact suivant.
    p_apex = replace(
        stance,
        lean=gait.lean + 0.05,
        bob=1.05 * b,
        crouch=0.0,
        head_pitch=-0.05,
        twist=-0.15 * a,
        lean_side=-0.025 * lat,
        hip_l=-0.36 * s,
        knee_l=0.22,
        hip_r=0.48 * s,
        knee_r=0.20,
        shoulder_l=0.48 * a,
        shoulder_r=-0.46 * a,
        elbow_l=0.52,
        elbow_r=0.28,
        shrug_l=-0.15,
        shrug_r=0.35,
        drape=-0.35,
        drape_lift=0.30,
        drape_sway=-0.30,
    )
    half_walk = [p_contact, p_crush, p_push, p_apex]
    walk = half_walk + [p.mirrored() for p in half_walk]

    dash = [
        # 1. Compression franche d'anticipation.
        replace(
            stance,
            crouch=2.8 + heavy,
            lean=gait.lean - 0.08,
            hip_l=0.26,
            knee_l=1.00,
            hip_r=-0.14,
            knee_r=0.94,
            shoulder_l=-0.75,
            shoulder_r=-0.75,
            elbow_l=0.68,
            elbow_r=0.68,
            shrug_l=0.45,
            shrug_r=0.45,
            drape=-1.0,
            drape_lift=-0.55,
        ),
        # 2. Détente explosive : corps projeté vers l'avant, tissus fouettés à l'horizontale.
        replace(
            stance,
            lean=gait.lean + 0.58,
            bob=1.05,
            hip_l=0.78,
            knee_l=0.24,
            hip_r=-0.78,
            knee_r=0.34,
            shoulder_l=-1.18,
            shoulder_r=-1.18,
            elbow_l=0.28,
            elbow_r=0.28,
            arm_out=gait.arm_out + 0.07,
            drape=1.20,
            drape_lift=0.80,
            smear=1.0,
        ),
        # 3. Vol rasant groupé.
        replace(
            stance,
            lean=gait.lean + 0.44,
            bob=0.55,
            crouch=0.85,
            hip_l=0.56,
            knee_l=0.98,
            hip_r=-0.20,
            knee_r=1.18,
            shoulder_l=-0.68,
            shoulder_r=-0.68,
            elbow_l=0.52,
            elbow_r=0.52,
            drape=1.05,
            drape_lift=0.50,
            smear=0.5,
        ),
        # 4. Réception amortie : écrasement d'absorption, le sac retombe sur les épaules.
        replace(
            stance,
            crouch=2.0 + heavy,
            lean=gait.lean + 0.16,
            bob=-0.45,
            hip_l=0.32,
            knee_l=0.62,
            hip_r=-0.16,
            knee_r=0.58,
            shoulder_l=0.28,
            shoulder_r=-0.12,
            drape=0.25,
            drape_lift=0.80,
        ),
    ]
    hurt = [
        replace(
            stance,
            lean=-0.40,
            head_pitch=-0.44,
            arm_out=gait.arm_out + 0.28,
            shoulder_l=-0.52,
            shoulder_r=-0.52,
            elbow_l=0.82,
            elbow_r=0.82,
            knee_l=0.36,
            knee_r=0.36,
            drape=-1.0,
            drape_lift=0.75,
        ),
        replace(
            stance,
            lean=-0.22,
            crouch=1.4,
            head_pitch=-0.20,
            arm_out=gait.arm_out + 0.12,
            knee_l=0.46,
            knee_r=0.46,
            drape=-0.45,
            drape_lift=-0.45,
        ),
        replace(stance, lean=-0.06, crouch=0.5, head_pitch=-0.05, knee_l=0.20, knee_r=0.20, drape=0.3),
    ]
    death = [
        replace(stance, lean=-0.32, head_pitch=-0.38, arm_out=gait.arm_out + 0.25, knee_l=0.35, knee_r=0.35,
                drape=-1.0, drape_lift=0.65),
        replace(stance, lean=-0.22, crouch=1.6, head_pitch=-0.3, lean_side=0.12,
                arm_out=gait.arm_out + 0.18, knee_l=0.55, knee_r=0.45, drape=-0.6, drape_lift=0.25),
        replace(stance, crouch=5.0, lean=0.18, lean_side=0.08, knee_l=1.15, knee_r=1.0, hip_l=0.4, hip_r=0.35,
                shoulder_l=0.2, shoulder_r=0.15, head_pitch=0.25, drape=0.25, drape_lift=0.45),
        replace(stance, crouch=8.0, lean=0.45, knee_l=1.75, knee_r=1.7, hip_l=0.75, hip_r=0.7,
                shoulder_l=0.45, shoulder_r=0.35, elbow_l=0.2, elbow_r=0.2, head_pitch=0.4, drape=0.65, drape_lift=0.55),
        replace(stance, crouch=10.5, lean=0.85, knee_l=2.2, knee_r=2.15, hip_l=1.05, hip_r=1.0,
                shoulder_l=0.85, shoulder_r=0.75, elbow_l=0.1, elbow_r=0.1, head_pitch=0.55, drape=0.9, drape_lift=-0.45),
        replace(stance, crouch=11.5, lean=1.0, knee_l=2.35, knee_r=2.3, hip_l=1.15, hip_r=1.1,
                shoulder_l=1.0, shoulder_r=0.9, elbow_l=0.05, elbow_r=0.05, head_pitch=0.65, arm_out=gait.arm_out + 0.08,
                drape=0.4, drape_lift=-0.25),
    ]
    return {"idle": living_idle(gait), "walk": walk, "dash": dash, "hurt": hurt, "death": death}
