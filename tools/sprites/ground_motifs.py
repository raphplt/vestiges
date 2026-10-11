"""
Motifs dessinés des sols (plan 37) : touffes, feuilles, mousse, cailloux, fissures, épis, flaques. Chaque famille se
décline dans la palette de la matière, quatre tons : 0 ombre portée, 1 ton moyen, 2 clair, 3 reflet ; lumière en haut
à gauche. Les motifs se sèment en plaques (plage du champ de fond) et laissent des zones calmes entre elles : un sol
se lit à ses motifs, pas à un bruit de pixels.
"""
from __future__ import annotations

from .ground import Stipple

Palette = tuple[str, str, str, str]


def calm(tones: tuple[str, str, str, str], amount: float = 0.55) -> tuple[str, str, str, str]:
    """Resserre les quatre tons de fond vers leur milieu : le fond reste calme, les motifs portent la matière."""
    rgb = [tuple(int(t.lstrip("#")[i:i + 2], 16) for i in (0, 2, 4)) for t in tones]
    middle = [(rgb[1][c] + rgb[2][c]) / 2 for c in range(3)]
    out = []
    for color in rgb:
        mixed = [round(color[c] + (middle[c] - color[c]) * amount) for c in range(3)]
        out.append("#" + "".join(f"{v:02x}" for v in mixed))
    return tuple(out)  # type: ignore[return-value]


def grass(palette: Palette, density: float = 1.0, low: float = 0.45) -> tuple[Stipple, ...]:
    """Touffes d'herbe, des brins isolés aux grosses touffes, plus denses sur les bosses du champ."""
    return (
        Stipple(("3.", "23", "02"), palette, 0.004 * density, (low - 0.15, 0.8)),
        Stipple((".3.", "323", "010"), palette, 0.007 * density, (low, 1.0)),
        Stipple(("3..3", "2.32", "2121", "0110"), palette, 0.007 * density, (low + 0.1, 1.0)),
        Stipple((".3..3.", "323.32", "121212", "010010"), palette, 0.004 * density, (low + 0.2, 1.0)),
        Stipple(("..3.3..", ".3232.3", "3212123", "2121212", ".01110."), palette, 0.0012 * density, (low + 0.25, 1.0)),
    )


def flowers(stem: str, petals: str, heart: str, density: float = 1.0) -> tuple[Stipple, ...]:
    """Petites fleurs sur leur tige, en touches rares."""
    return (
        Stipple((".1.", "121", ".3.", ".0."), ("#2a4a24", petals, heart, stem), 0.0012 * density, (0.55, 1.0)),
        Stipple(("1.1", ".3.", ".0."), ("#2a4a24", petals, petals, stem), 0.001 * density, (0.6, 1.0)),
    )


def litter(palette: Palette, density: float = 1.0, high: float = 0.38) -> tuple[Stipple, ...]:
    """Feuilles mortes à nervure et brindilles, en tas dans les creux."""
    return (
        Stipple((".32", "321", "10."), palette, 0.004 * density, (0.0, high)),
        Stipple(("23..", "2121", ".00."), palette, 0.003 * density, (0.0, high)),
        Stipple(("2.", "12", "0."), palette, 0.003 * density, (0.0, high + 0.12)),
        Stipple(("3...", ".2..", "..2.", "...1"), (palette[0], palette[1], palette[1], palette[2]), 0.0016 * density),
    )


def moss(palette: Palette, density: float = 1.0, low: float = 0.6) -> tuple[Stipple, ...]:
    """Coussins de mousse bombés, sur les bosses."""
    return (
        Stipple(("32", "21", "0."), palette, 0.004 * density, (low, 1.0)),
        Stipple((".33.", "3222", "2221", ".00."), palette, 0.005 * density, (low + 0.08, 1.0)),
    )


def stones(palette: Palette, density: float = 1.0, high: float = 0.55) -> tuple[Stipple, ...]:
    """Cailloux à facette claire et ombre portée, des graviers à la pierre isolée."""
    return (
        Stipple(("32", "10"), palette, 0.004 * density, (0.0, high)),
        Stipple((".33.", "3221", ".10."), palette, 0.0025 * density, (0.0, high - 0.05)),
        Stipple((".32.", "3221", "2211", ".00."), palette, 0.0008 * density, (0.0, high - 0.15)),
        Stipple(("..33..", ".32221", "322211", ".21110", "..000."), palette, 0.0004 * density, (0.0, high - 0.25)),
    )


def cracks(color: str, density: float = 1.0, high: float = 0.45) -> tuple[Stipple, ...]:
    """Fissures fines, dans les deux sens."""
    return (
        Stipple(("0...", ".0..", ".00.", "...0"), (color,), 0.0012 * density, (0.0, high)),
        Stipple(("...0", "..0.", "00..", "0..."), (color,), 0.0012 * density, (0.0, high)),
        Stipple(("0..", ".00", "...", "..."), (color,), 0.001 * density, (0.0, high)),
    )


def wheat(palette: Palette, density: float = 1.0) -> tuple[Stipple, ...]:
    """Épis debout : un rang serré de tiges claires à tête dorée, l'ombre au pied."""
    return (
        Stipple(("3", "2", "1", "0"), palette, 0.022 * density),
        Stipple((".3", "32", "21", "10", "0."), palette, 0.012 * density),
        Stipple(("3.3", "232", "121", "0.0"), palette, 0.008 * density, (0.4, 1.0)),
    )


def stubble(palette: Palette, density: float = 1.0) -> tuple[Stipple, ...]:
    """Chaume coupé : paires de tiges courtes, brins couchés."""
    return (
        Stipple(("3.3", "2.2", "0.0"), palette, 0.012 * density),
        Stipple(("32", "0."), palette, 0.012 * density),
        Stipple(("..3", ".2.", "1.."), palette, 0.004 * density),
    )


def mud(palette: Palette, glint: str, density: float = 1.0) -> tuple[Stipple, ...]:
    """Flaques luisantes et mottes, dans la vase."""
    return (
        Stipple((".33.", "0110", ".00."), (palette[0], palette[1], palette[2], glint), 0.003 * density, (0.0, 0.4)),
        Stipple(("3110", ".00."), (palette[0], palette[1], palette[2], glint), 0.004 * density, (0.0, 0.5)),
        Stipple(("32", "10"), palette, 0.004 * density, (0.55, 1.0)),
    )


def water(glint: str, deep: str, density: float = 1.0) -> tuple[Stipple, ...]:
    """Rides de l'eau : traits clairs couchés, un creux sombre dessous."""
    return (
        Stipple(("00.", ".11"), (glint, deep), 0.004 * density, (0.5, 1.0)),
        Stipple(("000",), (glint,), 0.003 * density, (0.55, 1.0)),
        Stipple((".00", "11."), (glint, deep), 0.003 * density, (0.4, 0.9)),
    )


def reeds(palette: Palette, density: float = 1.0) -> tuple[Stipple, ...]:
    """Joncs et roseaux en bouquets serrés."""
    return (
        Stipple(("3.3", "2.2", "2.1", "101", ".0."), palette, 0.003 * density, (0.6, 1.0)),
        Stipple(("3", "2", "1", "0"), palette, 0.004 * density, (0.55, 1.0)),
    )


def rubble(palette: Palette, density: float = 1.0) -> tuple[Stipple, ...]:
    """Éclats de béton et de brique, gravats isolés."""
    return (
        Stipple(("32", "10"), palette, 0.004 * density),
        Stipple(("332", "210"), palette, 0.0015 * density, (0.0, 0.5)),
        Stipple((".3", "21", "0."), palette, 0.003 * density),
    )


def weeds(palette: Palette, density: float = 1.0) -> tuple[Stipple, ...]:
    """Herbes folles qui percent le béton, le long des fissures."""
    return (
        Stipple(("3.3", ".2.", ".0."), palette, 0.002 * density, (0.0, 0.4)),
        Stipple(("3.", "23", "0."), palette, 0.002 * density, (0.0, 0.5)),
    )


def glints(color: str, deep: str, density: float = 1.0) -> tuple[Stipple, ...]:
    """Éclats de verre ou de cristal : un point vif et son ombre."""
    return (Stipple(("0", "1"), (color, deep), 0.0015 * density),)


def crystals(palette: Palette, density: float = 1.0) -> tuple[Stipple, ...]:
    """Pointes de cristal qui percent la roche, en grappes."""
    return (
        Stipple((".3", "32", "21", "10"), palette, 0.004 * density, (0.5, 1.0)),
        Stipple(("3.3", "323", "212", "101", ".0."), palette, 0.002 * density, (0.6, 1.0)),
        Stipple(("3", "1", "0"), palette, 0.006 * density, (0.4, 1.0)),
    )


def stains(palette: Palette, density: float = 1.0) -> tuple[Stipple, ...]:
    """Taches de rouille ou d'huile, sans relief."""
    return (
        Stipple(("12.", "211", ".1."), palette, 0.002 * density, (0.0, 0.4)),
        Stipple(("21",), palette, 0.003 * density),
    )
