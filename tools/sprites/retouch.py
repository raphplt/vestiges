"""
Retouches manuelles des sprites procéduraux dans Aseprite.

Une retouche est un fichier `art/retouches/<chemin>.aseprite` accompagné de `<chemin>.json`, qui liste les PNG du jeu
qu'il produit (une cible par frame). Tant qu'une retouche existe, les générateurs ne réécrivent plus ses cibles :
le PNG vient de la retouche, exportée par `python3 tools/export_retouches.py`. Supprimer les deux fichiers rend le
sprite au générateur.

Les générateurs créent la source à la demande (`--editable`) : calques couleurs / lignes internes / contour, palette
du sprite suivie de la palette master de la charte. Une source existante n'est jamais écrasée.
"""
from __future__ import annotations

import json
from pathlib import Path
from typing import Sequence

from PIL import Image

from .aseprite import write_aseprite
from .render import LAYER_NAMES, flatten

RETOUCH_ROOT = Path("art/retouches")
MASTER_PALETTE = Path("assets/palettes/vestiges_master.gpl")
_locked: set[str] | None = None


def locked_targets() -> set[str]:
    """Chemins (relatifs au dépôt) des PNG produits par une retouche."""
    global _locked
    if _locked is None:
        _locked = set()
        for sidecar in RETOUCH_ROOT.rglob("*.json"):
            _locked.update(json.loads(sidecar.read_text())["targets"])
    return _locked


def is_locked(target: Path) -> bool:
    return target.as_posix() in locked_targets()


def save_unless_locked(image: Image.Image, target: Path, tool: str) -> bool:
    """Écrit le PNG sauf s'il vient d'une retouche ; renvoie True si le fichier a été écrit."""
    if is_locked(target):
        print(f"[{tool}] retouche conservée : {target}")
        return False
    target.parent.mkdir(parents=True, exist_ok=True)
    image.save(target)
    return True


def create_source(name: str, frames: Sequence[Sequence[Image.Image]], targets: Sequence[Path], tool: str,
                  duration_ms: int = 100) -> None:
    """Écrit `art/retouches/<name>.aseprite` et son compagnon, sauf si la retouche existe déjà."""
    source = RETOUCH_ROOT / f"{name}.aseprite"
    sidecar = source.with_suffix(".json")
    if source.exists() or sidecar.exists():
        print(f"[{tool}] retouche déjà présente, non écrasée : {source}")
        return
    write_aseprite(source, frames, LAYER_NAMES, _palette(frames), duration_ms)
    sidecar.write_text(json.dumps({"targets": [t.as_posix() for t in targets]}, indent=2) + "\n")
    locked_targets().update(t.as_posix() for t in targets)
    print(f"[{tool}] retouche créée : {source} ({len(frames)} frame(s)) — l'ouvrir dans Aseprite")


def _palette(frames: Sequence[Sequence[Image.Image]]) -> list[tuple[int, int, int]]:
    """Couleurs du sprite (du plus sombre au plus clair), puis couleurs master absentes ; 256 au plus."""
    colours: set[tuple[int, int, int]] = set()
    for layers in frames:
        for r, g, b, a in flatten(layers).getdata():
            if a:
                colours.add((r, g, b))
    ordered = sorted(colours, key=lambda c: 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2])
    for colour in _master_palette():
        if colour not in colours:
            ordered.append(colour)
    return ordered[:256]


def _master_palette() -> list[tuple[int, int, int]]:
    result = []
    for line in MASTER_PALETTE.read_text().splitlines():
        parts = line.split()
        if len(parts) >= 3 and all(p.isdigit() for p in parts[:3]):
            result.append((int(parts[0]), int(parts[1]), int(parts[2])))
    return result
