"""
L'Indicible découvert (plan 07, planche B5a) : trois pistes pour ce qu'on voit quand il se découvre, après chaque vague.
Pas encore branché à generate_props.py ; rendu par la planche seulement, rien n'est écrit dans assets/.

La nuit du 14 elle-même (lore §7) : au premier regard un monstre géant, au fond la nuit qu'on ne dit pas.
- « grappe » : les trente et un, une grappe de bras de noyés qui se dressent d'un remous et se tendent vers le haut ;
- « tete » : une tête colossale sortie de l'eau jusqu'aux yeux, cheveux étalés comme des algues, deux mains immenses
  agrippées au sol ; la lanterne en reflet dans les yeux ;
- « oeil » : un œil immense ouvert dans le sol inondé, cils de bras de noyé, iris de mer, la lanterne en reflet.

Un bras est un tentacule à main : bras et avant-bras effilés autour d'un coude, paume, doigts à deux phalanges et pouce
(ou un poing). Chaque bras est borné par une sphère : loin d'elle, sa distance n'est pas calculée, si bien que des
dizaines de bras restent rendables. Écume et paupières sont des unions adoucies, pour se fondre au lieu de perler.
"""
from __future__ import annotations

import numpy as np

from ..palette import make_emissive, make_material
from ..render import Part
from ..sdf import capsule, ellipsoid, sphere
from ._kit import M, PropModel, Weathering


def _union(*distances: np.ndarray) -> np.ndarray:
    result = distances[0]
    for d in distances[1:]:
        result = np.minimum(result, d)
    return result


def _smooth_union(a: np.ndarray, b: np.ndarray, k: float) -> np.ndarray:
    """Union adoucie : les volumes se fondent sur une largeur `k` au lieu de se toucher en billes."""
    h = np.clip(0.5 + 0.5 * (b - a) / k, 0.0, 1.0)
    return b * (1.0 - h) + a * h - k * h * (1.0 - h)


def _blend(distances, k: float) -> np.ndarray:
    result = None
    for d in distances:
        result = d if result is None else _smooth_union(result, d, k)
    return result


def _normalized(v) -> np.ndarray:
    v = np.asarray(v, dtype=np.float64)
    return v / np.linalg.norm(v)


class _Bounded:
    """Union de primitives bornée par une sphère : hors de la sphère, la distance à la sphère suffit."""

    def __init__(self, primitives: list, center: np.ndarray, radius: float, blend: float = 0.0):
        self.primitives = primitives
        self.center = center
        self.radius = radius
        self.blend = blend

    def __call__(self, p: np.ndarray) -> np.ndarray:
        bound = np.linalg.norm(p - self.center, axis=1) - self.radius
        near = bound < 0.5 * M
        if not near.any():
            return bound
        exact = bound.copy()
        q = p[near]
        values = [primitive(q) for primitive in self.primitives]
        exact[near] = _blend(values, self.blend) if self.blend > 0 else _union(*values)
        return exact


def _group(items: list[_Bounded]):
    def distance(p: np.ndarray) -> np.ndarray:
        return _union(*(item(p) for item in items))
    return distance


def _capsule(a, b, ra, rb=None):
    return lambda p: capsule(p, a, b, ra, rb)


def _sphere(c, r):
    return lambda p: sphere(p, c, r)


def _arm(base, direction, length: float, radius: float, closed: bool, w: Weathering, face=(0.0, 0.0, 1.0),
         bend: float = 0.35, hand_scale: float = 1.35):
    """
    Bras dressé de `base` le long de `direction`, coudé de `bend` (radians) vers `face` puis redressé : chair et ongles,
    chacun borné. La paume regarde vers `face` (la caméra par défaut) autant que la direction le permet.
    """
    base = np.asarray(base, dtype=np.float64)
    d = _normalized(direction)
    side = np.cross(d, np.asarray(face, dtype=np.float64))
    if np.linalg.norm(side) < 1e-3:
        side = np.cross(d, (1.0, 0.0, 0.0))
    side = _normalized(side)
    front = _normalized(np.cross(side, d))
    # Coude : la première moitié penche d'un côté, la seconde revient ; le bras ondule comme un tentacule.
    twist = w.uniform(-1.0, 1.0)
    swing = _normalized(front * np.cos(twist) + side * np.sin(twist))
    elbow = base + _normalized(d * np.cos(bend) - swing * np.sin(bend)) * length * 0.5
    d2 = _normalized(d * np.cos(bend * 0.6) + swing * np.sin(bend * 0.6))
    wrist = elbow + d2 * length * 0.5
    hand_r = radius * hand_scale
    palm_len = 1.7 * hand_r
    palm_center = wrist + d2 * palm_len * 0.5
    knuckles = wrist + d2 * palm_len
    side2 = _normalized(np.cross(d2, front))
    front2 = _normalized(np.cross(side2, d2))
    rotation = np.stack([side2, d2, front2], axis=1)
    flesh = [
        _capsule(base, elbow, radius * 1.1, radius * 0.82),
        _capsule(elbow, wrist, radius * 0.82, radius * 0.5),
        _sphere(elbow, radius * 0.86),
        (lambda c, r: (lambda p: ellipsoid(p, c, r, rotation)))(palm_center, (hand_r * 0.95, palm_len * 0.58, hand_r * 0.36)),
    ]
    nails = []
    offsets = (-0.72, -0.24, 0.24, 0.72)
    if closed:
        for o in offsets:
            k = knuckles + side2 * o * hand_r * 0.95 + front2 * hand_r * 0.1
            mid = k + front2 * hand_r * 0.5 + d2 * hand_r * 0.15
            tip = mid - d2 * hand_r * 0.55 + front2 * hand_r * 0.1
            flesh += [_capsule(k, mid, hand_r * 0.27), _capsule(mid, tip, hand_r * 0.24)]
            nails.append(_sphere(tip + front2 * hand_r * 0.14, hand_r * 0.12))
        thumb_a = wrist + d2 * palm_len * 0.4 + side2 * hand_r * 0.95
        thumb_b = knuckles + front2 * hand_r * 0.7 + side2 * hand_r * 0.25 - d2 * hand_r * 0.3
        flesh.append(_capsule(thumb_a, thumb_b, hand_r * 0.26, hand_r * 0.22))
    else:
        lengths = (1.25, 1.5, 1.42, 1.1)
        for i, o in enumerate(offsets):
            spread = o * 0.38 + w.uniform(-0.08, 0.08)
            curl = w.uniform(0.15, 0.7)
            heading = _normalized(d2 + side2 * spread)
            k = knuckles + side2 * o * hand_r * 0.9
            mid = k + heading * hand_r * lengths[i] * 0.55
            tip = mid + _normalized(heading + front2 * curl) * hand_r * lengths[i] * 0.5
            flesh += [_capsule(k, mid, hand_r * 0.24, hand_r * 0.2), _capsule(mid, tip, hand_r * 0.2, hand_r * 0.15)]
            nails.append(_sphere(tip, hand_r * 0.13))
        thumb_a = wrist + d2 * palm_len * 0.3 + side2 * hand_r * 0.9
        thumb_b = thumb_a + _normalized(side2 * 0.9 + d2 * 0.6 + front2 * 0.4) * hand_r * 1.1
        flesh.append(_capsule(thumb_a, thumb_b, hand_r * 0.25, hand_r * 0.19))
    points = np.stack([base, elbow, wrist, knuckles])
    center = points.mean(axis=0)
    bound = float(np.max(np.linalg.norm(points - center, axis=1))) + hand_r * 2.4
    return _Bounded(flesh, center, bound, blend=radius * 0.25), _Bounded(nails, center, bound)


def _materials(*specs) -> list:
    return [make_emissive(name, color) if glow else make_material(name, color, contrast=contrast)
            for name, color, contrast, glow in specs]


def _foam(w: Weathering, count: int, center, spread, radius_range, k: float, flat: float = 0.3):
    """Écume fondue et aplatie : des plaques qui se mêlent à la surface de l'eau, pas des galets posés dessus."""
    center = np.asarray(center, dtype=np.float64)
    spread = np.asarray(spread, dtype=np.float64)
    bubbles = [(center + np.array([w.uniform(-1, 1), w.uniform(-1, 1), w.uniform(-1, 1)]) * spread,
                w.uniform(*radius_range)) for _ in range(count)]
    return lambda p: _blend([ellipsoid(p, c, (r, r * flat, r)) for c, r in bubbles], k)


# --- Piste A : la grappe des trente et un -------------------------------------------------------------------------

def grappe() -> PropModel:
    FLESH_A, FLESH_B, NAIL, WATER, FOAM = range(5)
    materials = _materials(("flesh_pale", "#7D8981", 0.85, False), ("flesh_drowned", "#5D6A66", 0.85, False),
                           ("nail", "#3F4743", 0.9, False), ("water", "#1F2B38", 0.7, False),
                           ("foam", "#8E9BA3", 0.6, False))

    def parts() -> list[Part]:
        w = Weathering(1411)
        arms_a, arms_b, nails = [], [], []
        # Trente et un bras : les plus longs au cœur, presque droits ; les autres s'ouvrent et retombent vers le bord.
        golden = np.pi * (3.0 - np.sqrt(5.0))
        for i in range(31):
            rho = np.sqrt((i + 0.5) / 31.0)
            angle = i * golden + w.uniform(-0.25, 0.25)
            x = np.cos(angle) * rho * 3.6 * M
            z = np.sin(angle) * rho * 2.5 * M
            outward = np.array([x, 0.0, z]) / max(1e-6, np.hypot(x, z))
            lean = 0.1 + 0.9 * rho ** 1.5 + w.uniform(-0.1, 0.2)
            direction = np.array([0.0, 1.0, 0.0]) + outward * lean + np.array([0.0, 0.0, 0.22]) \
                + np.array([w.uniform(-0.2, 0.2), 0.0, w.uniform(-0.2, 0.2)])
            length = (7.4 - 4.4 * rho + w.uniform(-0.7, 0.7)) * M
            radius = (0.36 - 0.1 * rho) * M
            flesh, nail = _arm((x, -0.8 * M, z), direction, length, radius, closed=w.uniform(0, 1) < 0.28, w=w,
                               bend=w.uniform(0.2, 0.6))
            (arms_a if i % 2 == 0 else arms_b).append(flesh)
            nails.append(nail)
        # Au cœur, des bras couchés s'enlacent : la grappe a une masse, pas seulement des tiges.
        for i in range(9):
            angle = i * 2.0 * np.pi / 9 + w.uniform(-0.3, 0.3)
            base = np.array([np.cos(angle) * 2.6 * M, -0.2 * M, np.sin(angle) * 1.8 * M])
            direction = np.array([-np.cos(angle + 0.6), 0.35 + w.uniform(0.0, 0.3), -np.sin(angle + 0.6)])
            flesh, nail = _arm(base, direction, 3.2 * M, 0.4 * M, closed=i % 3 == 0, w=w, bend=0.5)
            (arms_b if i % 2 == 0 else arms_a).append(flesh)
            nails.append(nail)
        swell = lambda p: _smooth_union(ellipsoid(p, (0, -0.35 * M, 0), (5.4 * M, 0.7 * M, 4.0 * M)),
                                        ellipsoid(p, (0, 0.0, 0), (3.2 * M, 0.9 * M, 2.4 * M)), 0.6 * M)
        rim = _foam(w, 40, (0, 0.15 * M, 0), (4.6 * M, 0.2 * M, 3.3 * M), (0.22 * M, 0.42 * M), 0.35 * M)
        # L'écume ne reste qu'en couronne autour des bras : le cœur est de l'eau noire.
        ring = lambda p: np.maximum(rim(p), (0.62 - np.hypot(p[:, 0] / (5.0 * M), p[:, 2] / (3.6 * M))) * M)
        return [Part(_group(arms_a), FLESH_A), Part(_group(arms_b), FLESH_B), Part(_group(nails), NAIL),
                Part(swell, WATER), Part(ring, FOAM)]

    bounds = ((-6.4 * M, -1.2 * M, -4.8 * M), (6.4 * M, 10.0 * M, 5.0 * M))
    return PropModel("indicible_form_grappe", parts, materials, 0.0, ray_range=440.0, canvas=(270, 260),
                     bounds=bounds, supersample=3)


# --- Piste B : la tête ---------------------------------------------------------------------------------------------

def tete() -> PropModel:
    SKIN, BROW, EYE_WHITE, LANTERN, HAIR, WATER, FOAM, FLESH, NAIL = range(9)
    materials = _materials(("skin", "#76817C", 0.8, False), ("socket", "#3E4644", 0.8, False),
                           ("eye_white", "#A9B2A8", 0.6, False),
                           ("lantern", "#F2B24A", 0, True), ("hair", "#26302D", 0.7, False),
                           ("water", "#1F2B38", 0.7, False), ("foam", "#5F7685", 0.55, False),
                           ("flesh", "#74807A", 0.85, False), ("nail", "#454C48", 0.9, False))
    # Échelle de la tête : elle sort de l'eau jusqu'au nez, plus large que trois maisons.
    k = 1.7 * M
    head_center = np.array([0.0, -0.7 * k, -0.6 * k])
    head_radii = (3.4 * k, 3.7 * k, 3.0 * k)

    def above_water(d: np.ndarray, p: np.ndarray) -> np.ndarray:
        """Rien sous la surface : la tête est coupée au ras de l'eau."""
        return np.maximum(d, -0.15 * M - p[:, 1])

    def parts() -> list[Part]:
        w = Weathering(5111)
        skull = lambda p: ellipsoid(p, head_center, head_radii)
        brow = lambda p: _blend([ellipsoid(p, (side * 1.3 * k, 0.98 * k, 2.0 * k), (1.35 * k, 0.4 * k, 0.6 * k))
                                 for side in (-1.0, 1.0)], 0.3 * k)
        nose = lambda p: ellipsoid(p, (0.0, -0.1 * k, 2.3 * k), (0.4 * k, 0.95 * k, 0.55 * k))
        face = lambda p: above_water(_smooth_union(_smooth_union(skull(p), brow(p), 0.35 * k), nose(p), 0.25 * k), p)
        # Des yeux de noyé, ouverts et vides, enfoncés sous l'arcade : pas d'iris, seulement la lanterne en reflet.
        eyes, sockets, glints = [], [], []
        for side in (-1.0, 1.0):
            c = np.array([side * 1.3 * k, 0.5 * k, 1.95 * k])
            eyes.append(lambda p, c=c: ellipsoid(p, c, (0.8 * k, 0.3 * k, 0.5 * k)))
            sockets.append(lambda p, c=c: ellipsoid(p, c + np.array([0, 0.05 * k, -0.05 * k]), (1.05 * k, 0.5 * k, 0.55 * k)))
            glints.append(_sphere(c + np.array([side * 0.25 * k, 0.05 * k, 0.48 * k]), 0.09 * k))
        eye_white = lambda p: _union(*(e(p) for e in eyes))
        socket = lambda p: np.maximum(_union(*(e(p) for e in sockets)), -eye_white(p))
        glint = lambda p: _union(*(g(p) for g in glints))
        # Cheveux : la calotte mouillée derrière le front, puis de longues mèches étalées sur l'eau, comme des algues.
        hair_top = lambda p: above_water(np.maximum(skull(p) - 0.1 * M, 2.2 * k - p[:, 1] - 0.55 * (p[:, 2] - head_center[2])), p)
        strands = []
        for i in range(46):
            angle = np.pi * (1.0 + i / 45.0) + w.uniform(-0.04, 0.04)
            start = head_center + np.array([np.cos(angle) * 2.9 * k, 0.0, np.sin(angle) * 2.4 * k])
            start[1] = w.uniform(0.7, 1.8) * k
            reach = w.uniform(3.0, 5.2) * k
            wave = w.uniform(0.25, 0.5) * (1.0 if i % 2 else -1.0)
            # Une mèche ondule en trois segments sur l'eau.
            points = [start]
            for j, f in enumerate((0.33, 0.66, 1.0)):
                a = angle + wave * np.sin(f * np.pi * 1.5)
                q = start + np.array([np.cos(a) * reach * f, 0.0, np.sin(a) * reach * f * 0.8])
                q[1] = (0.35 * k if j == 0 else 0.05 * M)
                points.append(q)
            radii = (0.2 * k, 0.17 * k, 0.12 * k, 0.05 * k)
            strands.append(_Bounded([_capsule(points[j], points[j + 1], radii[j], radii[j + 1]) for j in range(3)],
                                    (start + points[-1]) * 0.5, reach * 0.65 + 0.8 * k))
        # Mèches mouillées collées en travers du front, jusque sur un œil.
        for x0, x1, drop in ((-0.4, -1.9, 0.75), (0.3, 1.2, 0.55), (0.9, 2.3, 0.85), (-1.2, -0.6, 0.9)):
            a = np.array([x0 * k, 2.2 * k, 0.6 * k])
            b = np.array([(x0 + x1) * 0.5 * k, 1.35 * k, 1.75 * k])
            c = np.array([x1 * k, (1.35 - drop) * k, 2.2 * k])
            strands.append(_Bounded([_capsule(a, b, 0.17 * k, 0.14 * k), _capsule(b, c, 0.14 * k, 0.06 * k)],
                                    b, 2.0 * k))
        # Deux mains immenses agrippent le sol de part et d'autre, doigts plantés dans l'eau.
        hands, nails = [], []
        for side in (-1.0, 1.0):
            base = np.array([side * 5.4 * k, -0.6 * k, 0.9 * k])
            flesh, nail = _arm(base, (side * 0.3, 1.0, 0.5), 1.8 * k, 0.62 * k, closed=False, w=w, bend=0.8,
                               face=(0.0, 1.0, 0.0), hand_scale=1.5)
            hands.append(lambda p, f=flesh: above_water(f(p), p))
            nails.append(nail)
        flood = lambda p: ellipsoid(p, (0, -0.3 * M, 0.4 * k), (9.4 * k, 0.45 * M, 5.2 * k))
        ripple = lambda p: np.maximum(flood(p) - 0.04 * M, (0.82 - np.sin(np.hypot(p[:, 0] / 1.0, (p[:, 2] - 0.5 * k) * 1.35) / (0.55 * k))) * 0.4 * M)
        return [Part(face, SKIN), Part(socket, BROW), Part(hair_top, HAIR), Part(_group(strands), HAIR),
                Part(eye_white, EYE_WHITE), Part(glint, LANTERN), Part(_group(hands), FLESH), Part(_group(nails), NAIL),
                Part(flood, WATER), Part(ripple, FOAM, relief=False)]

    bounds = ((-10.0 * k, -1.0 * M, -6.4 * k), (10.0 * k, 3.4 * k, 6.0 * k))
    return PropModel("indicible_form_tete", parts, materials, 0.0, ray_range=700.0, canvas=(560, 330),
                     bounds=bounds, supersample=3)


# --- Piste C : l'œil -------------------------------------------------------------------------------------------------

def oeil() -> PropModel:
    WHITE, IRIS, PUPIL, LID, FLESH, NAIL, WATER, LANTERN, VEIN, RIPPLE = range(10)
    materials = _materials(("sclera", "#A4AC9F", 0.7, False), ("iris", "#2E6070", 0.85, False),
                           ("pupil", "#0C1118", 0.5, False), ("lid", "#5A545A", 0.8, False),
                           ("flesh", "#74807A", 0.85, False), ("nail", "#454C48", 0.9, False),
                           ("water", "#1F2B38", 0.7, False), ("lantern", "#F2B24A", 0, True),
                           ("vein", "#7A5650", 0.7, False), ("ripple", "#3D6079", 0.6, False))
    half_length = 6.0 * M
    half_depth = 2.5 * M

    def almond(x: np.ndarray) -> np.ndarray:
        """Demi-ouverture de l'œil (en z) le long de son grand axe : nulle aux coins, pleine au centre."""
        t = np.clip(np.abs(x) / half_length, 0.0, 1.0)
        return half_depth * (1.0 - t * t) ** 0.7

    def parts() -> list[Part]:
        w = Weathering(3411)
        inside = lambda p: np.abs(p[:, 2] - 0.1 * M) - almond(p[:, 0])
        ball = lambda p: ellipsoid(p, (0, -0.6 * M, 0.1 * M), (half_length, 1.3 * M, half_depth * 1.05))
        iris = lambda p: ellipsoid(p, (0.6 * M, 0.42 * M, 0.35 * M), (2.0 * M, 0.45 * M, 1.85 * M))
        pupil = lambda p: ellipsoid(p, (0.6 * M, 0.62 * M, 0.35 * M), (0.95 * M, 0.38 * M, 0.95 * M))
        glint = _sphere((0.05 * M, 0.95 * M, -0.15 * M), 0.3 * M)
        veins = []
        for _ in range(9):
            side = w.choice((-1.0, 1.0))
            a = np.array([side * w.uniform(4.2, 5.4) * M, 0.25 * M, w.uniform(-1.0, 1.0) * M])
            b = np.array([side * w.uniform(2.3, 2.9) * M, 0.5 * M, w.uniform(-0.9, 0.9) * M])
            veins.append(_capsule(a, b, 0.07 * M, 0.04 * M))
        vein = lambda p: np.maximum(_union(*(v(p) for v in veins)), inside(p))
        # Paupières : bourrelets de chair noyée le long de l'amande, la supérieure (au fond) plus épaisse.
        def lid(sign: float, height: float, thickness: float, lift: float):
            xs = np.linspace(-half_length * 1.08, half_length * 1.08, 15)
            pts = []
            for x in xs:
                open_z = float(almond(np.array([x]))[0])
                pts.append(np.array([x, lift + height * open_z / half_depth, sign * (open_z + thickness * 0.55) + 0.1 * M]))
            segments = [_capsule(pts[i], pts[i + 1], thickness * (0.55 + 0.45 * float(almond(np.array([pts[i][0]]))[0]) / half_depth))
                        for i in range(len(pts) - 1)]
            return lambda p: _blend([s(p) for s in segments], 0.3 * M)
        upper = lid(-1.0, 1.0 * M, 0.95 * M, 0.35 * M)
        lower = lid(1.0, 0.35 * M, 0.7 * M, 0.15 * M)
        # Cils : des bras de noyé qui sortent de la paupière supérieure en éventail inégal ; quelques-uns au bord du bas.
        lashes, nails = [], []
        for i, x in enumerate(np.linspace(-0.86, 0.86, 15) + np.array([w.uniform(-0.03, 0.03) for _ in range(15)])):
            open_z = float(almond(np.array([x * half_length]))[0])
            base = np.array([x * half_length, 0.9 * M + 0.8 * M * open_z / half_depth, -open_z - 0.6 * M])
            direction = np.array([x * 1.4 + w.uniform(-0.2, 0.2), 1.0, -0.25 + w.uniform(-0.25, 0.15)])
            length = (3.6 - 1.9 * abs(x) + w.uniform(-0.6, 0.6)) * M
            flesh, nail = _arm(base, direction, length, 0.3 * M, closed=w.uniform(0, 1) < 0.3, w=w,
                               bend=w.uniform(0.25, 0.75))
            lashes.append(flesh)
            nails.append(nail)
        for x in (-0.62, -0.2, 0.28, 0.66):
            open_z = float(almond(np.array([x * half_length]))[0])
            base = np.array([x * half_length, 0.2 * M, open_z + 0.9 * M])
            flesh, nail = _arm(base, (x * 0.9, 0.45, 1.0), 2.3 * M, 0.3 * M, closed=False, w=w, face=(0.0, 1.0, 0.0),
                               bend=0.25)
            lashes.append(flesh)
            nails.append(nail)
        flood = lambda p: ellipsoid(p, (0, -0.3 * M, 0.3 * M), (8.6 * M, 0.45 * M, 4.8 * M))
        ripples = _foam(w, 34, (0, 0.05 * M, 0.4 * M), (7.4 * M, 0.05 * M, 3.8 * M), (0.3 * M, 0.55 * M), 0.2 * M, flat=0.15)
        return [Part(lambda p: np.maximum(ball(p), inside(p)), WHITE), Part(vein, VEIN),
                Part(lambda p: np.maximum(iris(p), inside(p)), IRIS), Part(lambda p: np.maximum(pupil(p), inside(p)), PUPIL),
                Part(glint, LANTERN), Part(upper, LID), Part(lower, LID), Part(_group(lashes), FLESH),
                Part(_group(nails), NAIL), Part(flood, WATER), Part(ripples, RIPPLE)]

    bounds = ((-9.0 * M, -1.2 * M, -5.4 * M), (9.0 * M, 6.0 * M, 5.4 * M))
    return PropModel("indicible_form_oeil", parts, materials, 0.0, ray_range=440.0, canvas=(340, 210),
                     bounds=bounds, supersample=3)


def candidates() -> list[PropModel]:
    return [grappe(), tete(), oeil()]
