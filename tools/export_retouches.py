"""
Exporte les retouches Aseprite (art/retouches/**/*.aseprite) vers les PNG du jeu listés par leur compagnon .json.

Usage :
    python3 tools/export_retouches.py            # toutes les retouches
    python3 tools/export_retouches.py stump      # seulement celles dont le chemin contient « stump »

Les calques visibles sont aplatis. La taille du sprite doit rester celle du PNG d'origine : le point au sol et
l'emprise des décors (props_manifest.json) en dépendent, comme l'ancrage des pieds des personnages. Godot
réimporte seul un PNG modifié ; lancer tools/smoke_test.sh pour les nouveaux fichiers.
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from tools.sprites.aseprite import read_aseprite  # noqa: E402
from tools.sprites.retouch import RETOUCH_ROOT  # noqa: E402


def export(filter_text: str | None) -> int:
    errors = 0
    for source in sorted(RETOUCH_ROOT.rglob("*.aseprite")):
        if filter_text and filter_text not in source.as_posix():
            continue
        sidecar = source.with_suffix(".json")
        if not sidecar.exists():
            print(f"[export_retouches] compagnon manquant, ignoré : {sidecar}")
            errors += 1
            continue
        targets = [Path(t) for t in json.loads(sidecar.read_text())["targets"]]
        frames = read_aseprite(source)
        if len(frames) != len(targets):
            print(f"[export_retouches] {source} : {len(frames)} frame(s) pour {len(targets)} cible(s), ignoré")
            errors += 1
            continue
        for frame, target in zip(frames, targets):
            if target.exists():
                with Image.open(target) as original:
                    if original.size != frame.size:
                        print(f"[export_retouches] {source} : taille {frame.size} ≠ {original.size} de {target}, ignoré")
                        errors += 1
                        continue
            target.parent.mkdir(parents=True, exist_ok=True)
            frame.save(target)
        print(f"[export_retouches] {source} → {len(targets)} PNG")
    return errors


if __name__ == "__main__":
    sys.exit(1 if export(sys.argv[1] if len(sys.argv) > 1 else None) else 0)
