"""
Le Cracheur Pâli — une silhouette humaine délavée, presque effacée, qui crache ce qui lui reste (plan 07).
Maigre, voûté, peau blême comme un tirage passé au soleil ; sous la mâchoire pend une poche de bile rouille, lourde
et luisante. Membres grêles et inégaux, deux yeux vert-acide à des hauteurs différentes. Pour tirer, il se cambre,
la poche se contracte et la tête se projette vers l'avant.
Reprise du 11 octobre (plan 37 M4) : mannequin blême sans repère, sa poche se voyait à peine. Lambeaux d'une
chemise délavée sur le torse (il a été quelqu'un), poche plus grosse au reflet luisant, bras qui pendent.
Plan 54 S3 : côtes et poche qui accrochent la lumière ; il traîne la jambe droite (pas inégaux, épaule qui plonge du
côté boiteux), la poche ballotte au rythme des pas, et il se cambre plus loin en arrière avant de cracher.
"""
from __future__ import annotations

from dataclasses import replace

import numpy as np

from ..palette import make_emissive, make_material
from ..poses import Gait, humanoid_animations
from ..render import Part
from ..rig import Pose, Proportions, build_skeleton
from ..sdf import capsule, ellipsoid, sphere

FRAME_SIZE = (36, 48)
FRAME_PIVOT = (18.0, 45.0)
DIMENSIONS = Proportions(ankle=2.2, shin=13.0, thigh=12.5, spine=14.5, neck=2.4, head_radius=4.0, shoulder_half=5.6,
                         hip_half=3.0, upper_arm=10.5, forearm=11.0)

SKIN, SKIN_DARK, SAC, SAC_DARK, RIB, MAW, EYE, CLOTH, GLINT = range(9)
MATERIALS = [
    make_material("skin", "#B8B0A0", contrast=1.1),
    make_material("skin_dark", "#7E766A", contrast=1.1),
    make_material("sac", "#CC5533", contrast=1.1, specular=0.5),
    make_material("sac_dark", "#8A3A24", contrast=1.1),
    make_material("rib", "#D8D0C0", contrast=0.75, specular=0.5),
    make_material("maw", "#2B2230", contrast=0.5),
    make_emissive("eye", "#7FFF00"),
    make_material("cloth", "#6E84A0"),
    make_emissive("glint", "#FFB070"),
]


def parts(pose: Pose) -> list[Part]:
    s = build_skeleton(pose, DIMENSIONS)
    torso_center = (s.point("pelvis") + s.point("chest")) * 0.5
    # La poche gonfle au repos et se vide au tir (breath la porte).
    sac_size = 4.0 + 0.5 * pose.breath - 1.4 * max(0.0, pose.head_pitch)
    result = [
        Part(lambda p, c=torso_center: ellipsoid(p, c, (4.2, 7.6, 3.0), s.torso), SKIN),
        # Côtes qui saillent sur le flanc.
        Part(lambda p: ellipsoid(p, s.on_torso("chest", (2.8, -2.0, 1.6)), (1.4, 3.6, 1.2), s.torso), RIB),
        Part(lambda p: capsule(p, s.on_torso("pelvis", (0, 1.0, 0)), s.on_torso("pelvis", (0, -2.5, 0)), 3.2, 2.8), SKIN_DARK),
        Part(lambda p: sphere(p, s.point("head"), DIMENSIONS.head_radius), SKIN),
        # Mâchoire tombante et poche de bile sous le menton.
        Part(lambda p: ellipsoid(p, s.on_head((0, -2.4, 2.0)), (2.4, 1.4, 1.8), s.head), MAW),
        Part(lambda p, r=sac_size: ellipsoid(p, s.on_head((0.4, -4.2 - r * 0.4, 1.6)), (r * 0.9, r, r * 0.85), s.head), SAC),
        Part(lambda p, r=sac_size: ellipsoid(p, s.on_head((0.9, -4.6 - r * 0.5, 2.0)), (r * 0.5, r * 0.55, r * 0.45), s.head), SAC_DARK),
        # Reflet humide sur la poche.
        Part(lambda p, r=sac_size: sphere(p, s.on_head((-0.6, -3.6 - r * 0.2, 1.4 + r * 0.75)), 0.7), GLINT),
        Part(lambda p: sphere(p, s.on_head((1.6, 1.1, 3.3)), 1.15), EYE),
        Part(lambda p: sphere(p, s.on_head((-1.4, 0.3, 3.4)), 0.95), EYE),
        # Lambeaux de chemise délavée : col et épaules, déchirés au ventre.
        Part(lambda p: ellipsoid(p, s.on_torso("chest", (0.0, -3.0, 0.0)), (5.0, 5.6, 3.6), s.torso), CLOTH),
        Part(lambda p: capsule(p, s.on_torso("chest", (-2.6, -6.0, 3.0)), s.on_torso("chest", (-3.2, -9.5, 2.6)), 1.3, 0.6), CLOTH),
    ]
    for side, radius in (("l", 1.5), ("r", 1.2)):
        sleeve = s.point(f"shoulder_{side}") + (s.point(f"elbow_{side}") - s.point(f"shoulder_{side}")) * 0.45
        result += [
            Part(lambda p, side=side, e=sleeve, r=radius: capsule(p, s.point(f"shoulder_{side}"), e, r + 0.7, r + 0.5), CLOTH),
            Part(lambda p, side=side, r=radius: capsule(p, s.point(f"shoulder_{side}"), s.point(f"elbow_{side}"), r, r * 0.8), SKIN),
            Part(lambda p, side=side, r=radius: capsule(p, s.point(f"elbow_{side}"), s.point(f"hand_{side}"), r * 0.8, r * 0.6), SKIN_DARK),
            Part(lambda p, side=side: capsule(p, s.point(f"hip_{side}"), s.point(f"knee_{side}"), 1.7, 1.3), SKIN),
            Part(lambda p, side=side: capsule(p, s.point(f"knee_{side}"), s.point(f"ankle_{side}"), 1.3, 1.0), SKIN_DARK),
            Part(lambda p, side=side: capsule(p, s.point(f"ankle_{side}"), s.point(f"toe_{side}"), 1.2, 0.9), SKIN_DARK),
        ]
    return result


def _animations() -> dict[str, list[Pose]]:
    gait = Gait(lean=0.45, arm_out=0.25, stride=0.8, arm_swing=0.6, bounce=0.7, heavy=0.3)
    base = humanoid_animations(gait)
    # Bras ballants à la verticale devant le corps penché, au lieu de suivre la pente du dos.
    base = {name: [replace(pose, shoulder_l=pose.shoulder_l + gait.lean, shoulder_r=pose.shoulder_r + gait.lean)
                   for pose in poses] for name, poses in base.items()}
    stance = base["idle"][0]
    arms = gait.lean
    # Idle : la poche se gonfle et se vide, la tête dodeline.
    idle = [
        stance,
        replace(stance, breath=0.6, bob=0.15, head_pitch=-0.08, lean_side=-0.03),
        replace(stance, breath=1.1, bob=0.3, head_pitch=-0.14, shrug_l=0.3),
        replace(stance, breath=0.5, bob=0.1, head_pitch=0.04, lean_side=0.03),
    ]
    # Il traîne la jambe droite : grand pas franc à gauche, pas court et raide à droite, l'épaule plonge côté boiteux
    # et la poche ballotte avec un temps de retard (breath).
    walk = [
        replace(stance, bob=-0.5, crouch=0.4, lean_side=0.05, twist=0.12, hip_l=0.42, knee_l=0.1, hip_r=-0.3,
                knee_r=0.25, shoulder_l=arms - 0.35, shoulder_r=arms + 0.3, elbow_r=0.4, breath=0.2),
        replace(stance, bob=0.5, lean_side=0.02, hip_l=0.0, knee_l=0.08, hip_r=0.12, knee_r=0.55,
                shoulder_l=arms - 0.1, shoulder_r=arms + 0.1, breath=0.9, head_pitch=-0.06),
        replace(stance, bob=-0.9, crouch=1.0, lean_side=-0.09, twist=-0.08, shrug_r=-0.6, hip_l=-0.22,
                knee_l=0.5, hip_r=0.2, knee_r=0.05, shoulder_l=arms + 0.25, shoulder_r=arms - 0.15, breath=0.5,
                head_pitch=0.08),
        replace(stance, bob=0.2, lean_side=-0.03, hip_l=-0.05, knee_l=0.9, hip_r=-0.08, knee_r=0.1,
                shoulder_l=arms + 0.05, shoulder_r=arms, breath=0.1),
    ]
    # Tir, joué dès l'annonce : il se cambre loin en arrière en gonflant la poche, puis projette la tête et crache.
    attack = [
        replace(stance, lean=0.12, head_pitch=-0.5, breath=1.1, arm_out=0.45, shrug_l=0.5, shrug_r=0.5),
        replace(stance, lean=-0.05, head_pitch=-0.65, breath=1.3, arm_out=0.5, crouch=1.0, shrug_l=0.7, shrug_r=0.7),
        replace(stance, lean=0.8, head_pitch=0.7, crouch=1.4, shoulder_l=-0.7, shoulder_r=-0.6),
        replace(stance, lean=0.58, head_pitch=0.3, crouch=0.6),
    ]
    return {"idle": idle, "walk": walk, "attack": attack, "death": base["death"]}


ANIMATIONS = _animations()
