#!/usr/bin/env python3
"""Génère uniquement le tir du harpon et deux ponctuations de vanne/air (plan 45 N3).

Usage : python3 tools/generate_scaphandriere_audio.py /tmp/vestiges-scaph-audition
Sources CC0 locales vérifiées par empreinte ; aucun téléchargement ni synthèse vocale.
Les WAV d'audition appliquent le gain du jeu. Les autres sons et crédits restent intacts.
"""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np

from audio_bank_audit import momentary_max
from prepare_weapon_audio import ROOT, RATE, RECIPES, candidate, write_wav

# Ponctuations matérielles du scaphandre, pas un enregistrement de respiration humaine.
AIR_RECIPES = {
    "air_consume": ("Vanne douce et souffle court", .24, [("dash_start", .65, .28), ("lantern_fire", 1.7, .65)]),
    "air_empty": ("Souffle de vanne qui se ferme", .30, [("lantern_fire", .85, .65), ("dash_start", 1.3, .12)]),
}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("audition", type=Path)
    args = parser.parse_args()
    audition = args.audition.expanduser().resolve()
    if audition.is_relative_to(ROOT):
        parser.error("L'audition reste hors dépôt.")
    audition.mkdir(parents=True, exist_ok=True)
    credits_path = ROOT / "assets/audio/CREDITS.json"
    bank_path = ROOT / "data/audio/sounds.json"
    credits = json.loads(credits_path.read_text())
    by_id = {entry["effect_id"]: entry for entry in credits}
    bank = json.loads(bank_path.read_text())
    cache, report = {}, []
    recipes = {"weapon_harpoon_rifle": RECIPES["harpoon_rifle"], **AIR_RECIPES}
    for effect, (description, duration, layers) in recipes.items():
        weapon = effect.startswith("weapon_")
        variant = 0 if weapon else 2
        samples = candidate("harpoon_rifle" if weapon else effect, duration, layers, variant, by_id, cache)
        relative = f"assets/audio/sfx/{'weapons/harpoon_rifle' if weapon else 'scaphandriere/' + effect}.wav"
        target = ROOT / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        write_wav(target, samples)
        gain = round((-40 if weapon else -43) - momentary_max(target), 1)
        key = f"sfx_{effect}"
        bank[key] = {"path": f"res://{relative}", "volume_db": gain,
                     "volume_variance_db": 1.0 if weapon else 0.0,
                     "min_interval_ms": 60 if weapon else 1500, "effect_id": effect,
                     "priority": 10 if weapon else 30, "max_voices": 3 if weapon else 1}
        sources = [by_id[source] for source, _, _ in layers]
        entry = {"effect_id": effect, "candidate_id": f"scaph_20261013_{effect}", "file": relative,
                 "sha256": hashlib.sha256(target.read_bytes()).hexdigest(),
                 "author": " ; ".join(dict.fromkeys(source["author"] for source in sources)),
                 "license": "CC0-1.0", "license_url": "https://creativecommons.org/publicdomain/zero/1.0/",
                 "source_url": " ; ".join(dict.fromkeys(source["source_url"] for source in sources)),
                 "attribution": " ; ".join(source["attribution"] for source in sources),
                 "processing": f"tools/generate_scaphandriere_audio.py : {description}, variante {'A' if weapon else 'C'}, fondus, mono 48 kHz ; audition humaine en run à confirmer.",
                 "recipe": {"duration_seconds": duration, "variant": variant, "layers": layers},
                 "sources": [{"effect_id": source["effect_id"], "sha256": source["sha256"]} for source in sources]}
        index = next((i for i, item in enumerate(credits) if item["effect_id"] == effect), None)
        if index is None:
            credits.append(entry)
        else:
            credits[index] = entry
        game_samples = samples * 10 ** (gain / 20)
        write_wav(audition / f"{effect}-gain-jeu.wav", game_samples)
        # Répétition représentative ; au moins le délai commun d'air, sans surcharge de voix.
        period = round((.5 if weapon else 1.6) * RATE)
        repeated = np.zeros(period * 5)
        for start in range(0, len(repeated), period):
            repeated[start:start + len(game_samples)] += game_samples
        write_wav(audition / f"{effect}-repetition.wav", repeated)
        report.append({"effect_id": effect, "duration_seconds": len(samples) / RATE,
                       "gain_db": gain, "effective_momentary": momentary_max(target) + gain,
                       "sha256": entry["sha256"]})
    credits_path.write_text(json.dumps(credits, ensure_ascii=False, indent=2) + "\n")
    bank_path.write_text(json.dumps(bank, ensure_ascii=False, indent=2) + "\n")
    (audition / "manifest.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n")
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
