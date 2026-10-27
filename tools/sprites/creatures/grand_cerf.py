"""
Le Grand Cerf — gardien des bois de la colline (plan 57 G1), là où l'on montait le dimanche.
Ce que la ville a refusé de regarder dans ses bois : un cerf trop grand, dressé, la tête haute. Corps d'écorce
fendue, mousse et lichen qui pendent du cou, pattes de branches noueuses. Pas de visage : un crâne blanchi, les
orbites creuses où brûle la lumière violette du repaire. Une ramure de branches mortes, trop large, où reste accroché
un foulard rouge délavé, celui d'un promeneur.
Environ trois fois une créature : cadre de 176×160, pieds à (88, 146). Cinq actions : attente, marche, élan (la
charge, tête basse, ramure en avant), frappe (il se cabre et retombe des deux sabots) et mort (les genoux cèdent, il
s'effondre sur le flanc).
"""
from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, rotation_x, rotation_y, rotation_z, smin, sphere

FRAME_SIZE = (176, 160)
FRAME_PIVOT = (88.0, 146.0)
# Le gabarit est pensé à l'échelle d'un cerf, puis agrandi d'un bloc : la densité de pixels reste celle du bestiaire.
GROWTH = 1.3
# Il dépasse 180 unités de haut, ramure comprise : rayons plus longs, bornés à sa boîte.
RAY_RANGE = 400.0
BOUNDS = ((-92.0, -8.0, -92.0), (92.0, 210.0, 98.0))

BARK, BARK_DARK, HIDE, MOSS, WOOD, SKULL, SOCKET, HOOF, RAG, EYE = range(10)
MATERIALS = [
    make_material("bark", "#4E4038", contrast=1.15),
    make_material("bark_dark", "#2C2430", contrast=1.1),
    make_material("hide", "#665446", contrast=1.12),
    make_material("moss", "#7E8A64", contrast=1.0),
    make_material("wood", "#C8BCA2", contrast=0.95, specular=0.3),
    make_material("skull", "#E4D9C0", contrast=0.8, specular=0.55),
    make_material("socket", "#1C1424", contrast=0.5),
    make_material("hoof", "#2A2228"),
    make_material("rag", "#9A3E36", contrast=1.05),
    make_emissive("eye", "#C69CFF"),
]

# Hanches (x côté gauche positif, z avant), au repos.
FRONT_HIP_Z = 15.0
HIND_HIP_Z = -18.0
HIP_Y = 50.0
HIP_X = 7.5
# Le corps pivote autour des hanches arrière : c'est sur elles qu'il se cabre.
HINGE = np.array([0.0, HIP_Y, HIND_HIP_Z])


@dataclass(frozen=True)
class Stag:
    phase: float = 0.0
    stride: float = 0.0
    lift: float = 0.0
    bob: float = 0.0
    pitch: float = 0.0
    crouch: float = 0.0
    rear: float = 0.0
    head_low: float = 0.0
    roll: float = 0.0
    tuck: float = 0.0
    sway: float = 0.0
    front_lift: float = 0.0
    breath: float = 0.0


def _beam(points: list[tuple[float, float, float]], side: float) -> list[np.ndarray]:
    return [np.array([x * side, y, z]) for x, y, z in points]


# Ramure : un merrain par côté, de la tête vers le haut et l'extérieur, et ses andouillers. Points dans le repère de la
# tête (origine au sommet du crâne). Asymétrique : le côté gauche porte une branche cassée en plus.
MAIN_BEAM = [(2.5, 1.0, -1.0), (8.0, 9.0, -3.5), (15.0, 19.0, -6.0), (21.0, 30.0, -6.5), (23.0, 41.0, -4.0),
             (20.0, 50.0, 0.0)]
TINES = [
    # (indice du point de départ, bout de l'andouiller, rayon à la base)
    (1, (10.0, 12.0, 8.0), 1.7),
    (2, (13.0, 29.0, 4.0), 1.5),
    (3, (28.0, 38.0, -10.0), 1.4),
    (3, (17.0, 40.0, 2.0), 1.3),
    (4, (29.0, 49.0, -4.0), 1.1),
]
BROKEN_TINE = (2, (24.0, 22.0, -2.0), 1.4)
BEAM_RADII = [2.9, 2.6, 2.2, 1.8, 1.4, 0.9]


def parts(s: Stag) -> list[Part]:
    return [Part(lambda p, d=part.distance: d(p / GROWTH) * GROWTH, part.material, part.relief) for part in _parts(s)]


def _parts(s: Stag) -> list[Part]:
    body_rotation = rotation_z(s.roll) @ rotation_x(s.pitch - s.rear)
    lift = np.array([0.0, s.bob - s.crouch, 0.0])

    def at(local) -> np.ndarray:
        return HINGE + lift + body_rotation @ (np.asarray(local, dtype=np.float64) - HINGE)

    # Cou dressé ; l'élan l'abaisse à l'horizontale, ramure pointée devant.
    neck_base = at((0.0, 60.0, 24.0))
    neck_angle = 0.5 + 0.9 * s.head_low
    neck_rotation = body_rotation @ rotation_y(s.sway) @ rotation_x(neck_angle)
    head = neck_base + neck_rotation @ np.array([0.0, 25.0, 0.0])
    # Le crâne pend museau vers le bas ; tête basse, il regarde devant.
    head_rotation = neck_rotation @ rotation_x(0.95 - 0.4 * s.head_low)

    def on_head(local) -> np.ndarray:
        return head + head_rotation @ np.asarray(local, dtype=np.float64)

    chest = at((0.0, 55.0, 12.0))
    belly = at((0.0, 50.0 + s.breath, -2.0))
    rump = at((0.0, 55.0, -16.0))
    result = [
        # Poitrail haut et massif, croupe plus basse : d'un seul tenant avec le cou.
        Part(lambda p: smin(smin(smin(
            ellipsoid(p, chest, (14.5, 17.5, 16.5), body_rotation),
            ellipsoid(p, belly, (12.5, 13.0, 17.0), body_rotation), 4.0),
            ellipsoid(p, rump, (13.0, 14.0, 13.5), body_rotation), 4.0),
            capsule(p, neck_base, head + neck_rotation @ np.array([0.0, -4.0, 0.0]), 7.5, 4.2), 3.0), HIDE),
        # Écorce fendue en plaques sur l'épaule, le flanc et la cuisse.
        Part(lambda p: ellipsoid(p, at((8.5, 60.0, 14.0)), (5.5, 9.0, 8.0), body_rotation), BARK),
        Part(lambda p: ellipsoid(p, at((-8.5, 58.0, 13.0)), (5.0, 8.5, 7.5), body_rotation), BARK),
        Part(lambda p: ellipsoid(p, at((9.0, 52.0, -1.0)), (4.0, 7.0, 9.0), body_rotation), BARK_DARK),
        Part(lambda p: ellipsoid(p, at((-9.0, 54.0, -16.0)), (5.5, 9.5, 8.5), body_rotation), BARK),
        Part(lambda p: ellipsoid(p, at((8.5, 56.0, -17.0)), (5.0, 8.0, 7.5), body_rotation), BARK),
        # Échine de bois en crête, du garrot à la croupe.
        Part(lambda p: capsule(p, at((0.0, 69.0, 16.0)), at((0.0, 66.0, -18.0)), 2.6, 2.0), BARK_DARK),
        # Mousse et lichen qui pendent sous le cou et sur le garrot.
        Part(lambda p: ellipsoid(p, neck_base + neck_rotation @ np.array([0.0, 9.0, 6.0]), (4.0, 7.0, 3.0),
                                 neck_rotation), MOSS),
        Part(lambda p: ellipsoid(p, at((3.0, 70.0, 10.0)), (6.0, 3.0, 7.0), body_rotation), MOSS),
        Part(lambda p: ellipsoid(p, at((-2.0, 64.0, -24.0)), (4.0, 3.0, 4.5), body_rotation), MOSS),
        # Courte queue d'écorce.
        Part(lambda p: capsule(p, at((0.0, 62.0, -28.0)), at((0.0, 55.0, -33.0)), 2.6, 1.4), BARK_DARK),
        # Crâne blanchi, sans chair : le museau pend, les orbites creuses brûlent.
        Part(lambda p: smin(
            ellipsoid(p, on_head((0.0, 0.0, 2.5)), (7.2, 6.6, 7.6), head_rotation),
            ellipsoid(p, on_head((0.0, -1.8, 13.0)), (4.4, 3.8, 8.5), head_rotation), 2.5), SKULL),
        Part(lambda p: sphere(p, on_head((4.4, 2.0, 5.6)), 2.8), SOCKET),
        Part(lambda p: sphere(p, on_head((-4.4, 2.0, 5.6)), 2.8), SOCKET),
        Part(lambda p: ellipsoid(p, on_head((4.8, 2.0, 6.8)), (1.2, 1.8, 1.2), head_rotation), EYE),
        Part(lambda p: ellipsoid(p, on_head((-4.8, 2.0, 6.8)), (1.2, 1.8, 1.2), head_rotation), EYE),
        Part(lambda p: ellipsoid(p, on_head((0.0, -3.0, 20.0)), (2.6, 1.8, 1.6), head_rotation), SOCKET),
    ]

    # Ramure de branches mortes : le repère de tête la porte, l'élan la pointe devant.
    crown = on_head((0.0, 5.5, 0.0))
    antler_rotation = head_rotation @ rotation_x(-1.25 + 0.35 * s.head_low)
    for side in (1.0, -1.0):
        beam = [crown + antler_rotation @ point for point in _beam(MAIN_BEAM, side)]
        for i in range(len(beam) - 1):
            result.append(Part(lambda p, a=beam[i], b=beam[i + 1], ra=BEAM_RADII[i], rb=BEAM_RADII[i + 1]:
                               capsule(p, a, b, ra, rb), WOOD))
        tines = TINES + ([BROKEN_TINE] if side > 0 else [])
        for start, tip, radius in tines:
            end = crown + antler_rotation @ np.array([tip[0] * side, tip[1], tip[2]])
            result.append(Part(lambda p, a=beam[start], b=end, r=radius: capsule(p, a, b, r, 0.7), WOOD))
        if side > 0:
            # Foulard rouge délavé, resté accroché depuis un dimanche.
            knot = beam[2]
            result.append(Part(lambda p, c=knot: ellipsoid(p, c + np.array([0.0, -2.0, 0.0]), (2.4, 2.0, 2.4)), RAG))
            result.append(Part(lambda p, c=knot: capsule(p, c + np.array([0.5, -2.0, 0.5]),
                                                         c + np.array([2.0, -11.0, 2.0]), 1.6, 1.0), RAG))
        # Lichen en barbe sous le premier andouiller.
        result.append(Part(lambda p, c=beam[1]: ellipsoid(p, c + np.array([0.0, -3.0, 1.0]), (1.6, 3.4, 1.6)), MOSS))

    result += _legs(s, at, body_rotation)
    return result


def _legs(s: Stag, at, body_rotation: np.ndarray) -> list[Part]:
    result = []
    # Pas croisé : avant gauche et arrière droit ensemble.
    for x, z, front, offset in ((HIP_X, FRONT_HIP_Z, True, 0.0), (-HIP_X, FRONT_HIP_Z, True, np.pi),
                                (HIP_X, HIND_HIP_Z, False, np.pi), (-HIP_X, HIND_HIP_Z, False, 0.0)):
        hip = at((x, HIP_Y - 2.0, z))
        side = np.sign(x)
        cycle = s.phase + offset
        foot = np.array([hip[0] + side * 1.5, max(np.cos(cycle), 0.0) * s.lift, hip[2] + np.sin(cycle) * s.stride])
        # Repliées sous le corps : cabré pour les pattes avant, mort pour toutes.
        curled = hip + body_rotation @ np.array([side * 1.5, -18.0, 10.0 if front else -6.0])
        fold = max(s.tuck, s.front_lift if front else 0.0)
        foot = foot + (curled - foot) * fold
        reach = foot - hip
        length = np.linalg.norm(reach)
        # Genou en avant pour les antérieures, jarret en arrière pour les postérieures, d'autant plus plié que la
        # patte est courte.
        bend = max(0.0, 46.0 - length) * 0.55 + 4.0
        knee = hip + reach * 0.5 + body_rotation @ np.array([0.0, 0.0, bend if front else -bend])
        hoof = foot + np.array([0.0, 0.0, 2.4])
        result += [
            Part(lambda p, a=hip, k=knee, f=front: capsule(p, a, k, 6.5 if f else 8.0, 3.4), BARK),
            Part(lambda p, k=knee, f=foot: capsule(p, k, f, 3.0, 2.3), BARK_DARK),
            Part(lambda p, k=knee: sphere(p, k, 3.0), BARK_DARK),
            Part(lambda p, f=foot, h=hoof: capsule(p, f + np.array([0.0, 1.0, 0.0]), h, 2.8, 2.2), HOOF),
        ]
    return result


def _animations() -> dict[str, list[Stag]]:
    stand = Stag()
    # Pas lourd et lent : la tête suit d'un temps, la ramure tangue.
    beats = [
        dict(bob=-1.0, pitch=0.02, sway=-0.05),
        dict(bob=1.2, pitch=-0.01, sway=0.0),
        dict(bob=-1.0, pitch=0.02, sway=0.05),
        dict(bob=1.2, pitch=-0.01, sway=0.0),
    ]
    walk = [replace(stand, phase=i * np.pi / 2, stride=8.0, lift=6.0, **beat) for i, beat in enumerate(beats)]
    # Élan : galop tête basse, ramure en avant, le corps tendu et plus bas.
    gallop = [
        dict(bob=-2.0, crouch=3.0, pitch=0.06),
        dict(bob=3.0, crouch=1.0, pitch=-0.05),
        dict(bob=-1.0, crouch=3.5, pitch=0.08),
        dict(bob=2.0, crouch=1.5, pitch=-0.02),
    ]
    charge = [replace(stand, phase=i * np.pi / 2, stride=12.0, lift=9.0, head_low=1.0, **beat)
              for i, beat in enumerate(gallop)]
    return {
        # Il respire, la tête haute, et tourne lentement la ramure.
        "idle": [
            stand,
            replace(stand, breath=0.8, bob=0.4, sway=0.06),
            replace(stand, breath=1.4, bob=0.7, sway=0.1),
            replace(stand, breath=0.6, bob=0.3, sway=0.03),
        ],
        "walk": walk,
        "charge": charge,
        # Frappe : il se cabre, sabots levés, puis retombe de tout son poids, tête basse.
        "attack": [
            replace(stand, rear=0.3, front_lift=0.4, sway=-0.05),
            replace(stand, rear=0.62, front_lift=1.0, head_low=-0.15),
            replace(stand, rear=-0.05, crouch=5.0, head_low=0.6, pitch=0.08),
            replace(stand, crouch=2.0, head_low=0.3),
        ],
        # Les genoux cèdent, il s'agenouille, puis s'effondre sur le flanc.
        "death": [
            replace(stand, rear=0.15, head_low=-0.2, front_lift=0.3),
            replace(stand, pitch=0.25, crouch=12.0, tuck=0.5, head_low=0.5),
            replace(stand, roll=0.7, crouch=24.0, tuck=0.85, head_low=0.8),
            replace(stand, roll=1.35, crouch=34.0, tuck=1.0, head_low=0.9),
            replace(stand, roll=1.45, crouch=36.0, tuck=1.0, head_low=1.0),
        ],
    }


ANIMATIONS = _animations()
