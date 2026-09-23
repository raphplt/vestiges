"""Squelette humanoïde en cinématique directe. Repère modèle : x gauche du personnage, y haut, z avant."""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from .sdf import rotation_x, rotation_y, rotation_z


@dataclass(frozen=True)
class Proportions:
    ankle: float = 3.0
    shin: float = 12.0
    thigh: float = 13.0
    spine: float = 14.0
    neck: float = 2.5
    head_radius: float = 4.3
    shoulder_half: float = 6.6
    hip_half: float = 3.2
    upper_arm: float = 9.5
    forearm: float = 8.5


@dataclass(frozen=True)
class Pose:
    """Angles en radians. Balancements positifs = vers l'avant ; flexions positives = genou/coude pliés."""
    bob: float = 0.0
    crouch: float = 0.0
    breath: float = 0.0
    lean: float = 0.0
    lean_side: float = 0.0
    twist: float = 0.0
    head_pitch: float = 0.0
    hip_l: float = 0.0
    hip_r: float = 0.0
    knee_l: float = 0.1
    knee_r: float = 0.1
    shoulder_l: float = 0.0
    shoulder_r: float = 0.0
    elbow_l: float = 0.25
    elbow_r: float = 0.25
    arm_out: float = 0.12
    root_pitch: float = 0.0
    root_roll: float = 0.0

    def mirrored(self) -> "Pose":
        return replace(self, hip_l=self.hip_r, hip_r=self.hip_l, knee_l=self.knee_r, knee_r=self.knee_l,
                       shoulder_l=self.shoulder_r, shoulder_r=self.shoulder_l,
                       elbow_l=self.elbow_r, elbow_r=self.elbow_l, lean_side=-self.lean_side, twist=-self.twist)


DOWN = np.array([0.0, -1.0, 0.0])
UP = np.array([0.0, 1.0, 0.0])


def _swing(angle: float) -> np.ndarray:
    """Rotation qui porte un membre pendant vers l'avant (+z) pour un angle positif."""
    return rotation_x(-angle)


@dataclass(frozen=True)
class Skeleton:
    joints: dict[str, np.ndarray]
    torso: np.ndarray
    head: np.ndarray

    def point(self, name: str) -> np.ndarray:
        return self.joints[name]

    def on_torso(self, anchor: str, offset) -> np.ndarray:
        """Point attaché au torse : décalage exprimé dans le repère du torse."""
        return self.joints[anchor] + self.torso @ np.asarray(offset, dtype=np.float64)

    def on_head(self, offset) -> np.ndarray:
        return self.joints["head"] + self.head @ np.asarray(offset, dtype=np.float64)


def build_skeleton(pose: Pose, dims: Proportions = Proportions()) -> Skeleton:
    hip_height = dims.ankle + dims.shin + dims.thigh
    pelvis = np.array([0.0, hip_height - pose.crouch + pose.bob, 0.0])
    torso = rotation_y(pose.twist) @ rotation_x(pose.lean) @ rotation_z(pose.lean_side)
    chest = pelvis + torso @ (UP * (dims.spine + pose.breath))
    neck = chest + torso @ (UP * dims.neck)
    head_rotation = torso @ rotation_x(pose.head_pitch)
    head = neck + head_rotation @ (UP * (dims.head_radius + 0.6))

    joints: dict[str, np.ndarray] = {"pelvis": pelvis, "chest": chest, "neck": neck, "head": head}
    for side, sign in (("l", 1.0), ("r", -1.0)):
        shoulder = chest + torso @ np.array([sign * dims.shoulder_half, -1.6 + pose.breath * 0.5, 0.0])
        abduction = rotation_z(sign * pose.arm_out)
        swing = getattr(pose, f"shoulder_{side}")
        bend = getattr(pose, f"elbow_{side}")
        elbow = shoulder + torso @ abduction @ _swing(swing) @ (DOWN * dims.upper_arm)
        hand = elbow + torso @ abduction @ _swing(swing + bend) @ (DOWN * dims.forearm)

        hip = pelvis + rotation_y(pose.twist * 0.3) @ np.array([sign * dims.hip_half, 0.0, 0.0])
        thigh_angle = getattr(pose, f"hip_{side}")
        shin_angle = thigh_angle - getattr(pose, f"knee_{side}")
        knee = hip + _swing(thigh_angle) @ (DOWN * dims.thigh)
        ankle = knee + _swing(shin_angle) @ (DOWN * dims.shin)
        toe = ankle + _swing(shin_angle * 0.35) @ np.array([0.0, -1.2, 4.2])
        joints.update({f"shoulder_{side}": shoulder, f"elbow_{side}": elbow, f"hand_{side}": hand,
                       f"hip_{side}": hip, f"knee_{side}": knee, f"ankle_{side}": ankle, f"toe_{side}": toe})

    # Transformation racine au sol (chute, bascule) autour du point de contact.
    root = rotation_x(pose.root_pitch) @ rotation_z(pose.root_roll)
    if pose.root_pitch or pose.root_roll:
        joints = {name: root @ point for name, point in joints.items()}
        torso = root @ torso
        head_rotation = root @ head_rotation
    return Skeleton(joints, torso, head_rotation)
