"""Rampes de couleurs pixel art : ombres froides, lumières chaudes, contours sel-out teintés (charte §4–5)."""
from __future__ import annotations

import colorsys
from dataclasses import dataclass

RampColor = tuple[int, int, int]


def hex_to_rgb(value: str) -> RampColor:
    value = value.lstrip("#")
    return int(value[0:2], 16), int(value[2:4], 16), int(value[4:6], 16)


def _shift(rgb: RampColor, lightness: float, saturation: float, hue_shift: float) -> RampColor:
    h, l, s = colorsys.rgb_to_hls(*(c / 255.0 for c in rgb))
    h = (h + hue_shift) % 1.0
    l = min(max(l * lightness, 0.0), 1.0)
    s = min(max(s * saturation, 0.0), 1.0)
    r, g, b = colorsys.hls_to_rgb(h, l, s)
    return round(r * 255), round(g * 255), round(b * 255)


def _toward(hue: float, target: float, amount: float) -> float:
    """Décalage de teinte signé vers une teinte cible (chemin le plus court)."""
    delta = (target - hue + 0.5) % 1.0 - 0.5
    return delta * amount


@dataclass(frozen=True)
class Material:
    """Quatre tons du plus sombre au plus clair, plus deux tons de contour."""
    name: str
    ramp: tuple[RampColor, RampColor, RampColor, RampColor]
    outline: RampColor
    inner_line: RampColor


def make_material(name: str, base_hex: str, contrast: float = 1.0) -> Material:
    base = hex_to_rgb(base_hex)
    hue = colorsys.rgb_to_hls(*(c / 255.0 for c in base))[0]
    # Ombres vers le violet-bleu (lumière ambiante froide de l'Effacement), lumières vers l'or (mémoire).
    cool = _toward(hue, 0.72, 0.18 * contrast)
    warm = _toward(hue, 0.12, 0.12 * contrast)
    ramp = (
        _shift(base, 1 - 0.42 * contrast, 0.9, cool),
        _shift(base, 1 - 0.2 * contrast, 0.95, cool * 0.5),
        base,
        _shift(base, 1 + 0.28 * contrast, 1.05, warm),
    )
    outline = _shift(base, 0.32, 0.85, cool * 1.3)
    inner = _shift(base, 0.5, 0.9, cool)
    return Material(name, ramp, outline, inner)
