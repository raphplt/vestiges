#!/usr/bin/env python3
"""Prépare une audition des 24 armes, sans modifier la banque ni les assets du jeu.

Sources : choix CC0 déjà présents, crédités et vérifiés par empreinte. Trois variantes par arme,
avec écoute isolée et répétée au gain de jeu proposé. Exports et décisions restent hors dépôt.
Usage : python3 tools/prepare_weapon_audio.py ~/.local/share/vestiges-audio/2026-10-07/armes
"""
import argparse
import hashlib
import html
import json
import subprocess
import wave
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
RATE = 48000
# source, vitesse, poids : les couches évoquent la matière de l'objet, sans ajouter de longs sons de tir.
RECIPES = {
    "chipped_blade": ("Balayage de lame court", .16, [("blade_swing", 1.0, 1)]),
    "heavy_hammer": ("Masse creuse, ressort métallique", .28, [("heavy_swing", .72, 1), ("clock_strike", .8, .22)]),
    "makeshift_bow": ("Corde sèche", .28, [("bow_release", 1, 1)]),
    "sling": ("Déclic puis départ de bille", .14, [("throw_release", 1.1, 1), ("ui_click", 1.3, .28)]),
    "sharpened_pipe": ("Armature légère qui fend l'air", .22, [("blade_swing", 1.35, 1), ("crossbow_release", 1.4, .25)]),
    "crossbow": ("Percuteur court", .22, [("crossbow_release", 1.1, 1)]),
    "cleaver": ("Balayage large de tôle", .25, [("heavy_swing", 1.05, 1), ("blade_swing", .7, .35)]),
    "whip": ("Claquement souple", .32, [("whip_snap", 1, 1)]),
    "throwing_axes": ("Lancer et tintement de vaisselle", .26, [("throw_release", .8, 1), ("shard_strike", 1.2, .32)]),
    "nail_mace": ("Dents métalliques légères", .27, [("heavy_swing", 1.25, .7), ("chain_jump", 1.55, .4)]),
    "teachers_bell": ("Cloche courte, timbre ouvert conservé", .7, [("bell_pulse", 1, 1)]),
    "surgeons_scalpel": ("Petit trait de lame", .1, [("blade_swing", 1.8, 1)]),
    "lighthouse_shard": ("Impulsion de verre", .38, [("light_shot", .85, 1), ("shard_strike", .9, .2)]),
    "music_box": ("Lamelle pincée, une note au contact", .38, [("bell_pulse", 1.8, .35), ("ui_click", 1.4, .18)]),
    "chain_of_names": ("Trousseau qui s'entrechoque", .32, [("chain_jump", 1.2, 1)]),
    "compass_needle": ("Petit ressort et vibration d'aiguille", .25, [("needle_shot", .85, 1), ("ui_click", 1.6, .2)]),
    "photographers_flash": ("Obturateur mécanique en deux temps", .2, [("crossbow_release", 1.7, .7), ("ui_click", .8, .5)]),
    "essence_staff": ("Bois pincé et résonance douce", .32, [("bow_release", .65, .8), ("heavy_swing", 1.7, .18)]),
    "void_edge": ("Gommage sourd", .35, [("void_slash", .85, 1)]),
    "memory_lantern": ("Souffle bref de flamme", .45, [("lantern_fire", 1.15, 1)]),
    "echo_gauntlets": ("Souffle mat et cuir", .2, [("heavy_swing", .85, 1), ("whip_snap", .7, .15)]),
    "childs_drawing": ("Frottement sec et petit éclat", .24, [("void_slash", 1.5, .7), ("shard_strike", 1.8, .2)]),
    "last_broadcast": ("Contact radio et souffle court", .45, [("lantern_fire", 1.8, .7), ("ui_click", .6, .4)]),
    "clock_hand": ("Échappement d'horlogerie", .3, [("clock_strike", 1.2, 1)]),
}


def read_source(effect, credits, cache):
    if effect in cache:
        return cache[effect]
    credit = credits[effect]
    source = ROOT / credit["file"]
    if credit["license"] != "CC0-1.0":
        raise ValueError(f"Source non prévue par ce pipeline : {effect}")
    if hashlib.sha256(source.read_bytes()).hexdigest() != credit["sha256"]:
        raise ValueError(f"Source modifiée depuis les crédits : {effect}")
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", str(source), "-ac", "1", "-ar", str(RATE),
                          "-f", "f32le", "-"], check=True, capture_output=True).stdout
    samples = np.frombuffer(raw, dtype="<f4").astype(np.float64)
    active = np.flatnonzero(np.abs(samples) > max(np.max(np.abs(samples)) * .025, .0001))
    if not len(active):
        raise ValueError(f"Source silencieuse : {effect}")
    cache[effect] = samples[max(0, active[0] - 96):]
    return cache[effect]


def soften(samples, cutoff):
    """Passe-bas doux hors ligne ; chaque variante garde une attaque et une fin fondues."""
    spectrum = np.fft.rfft(samples)
    frequencies = np.fft.rfftfreq(len(samples), 1 / RATE)
    return np.fft.irfft(spectrum / np.sqrt(1 + (frequencies / cutoff) ** 6), n=len(samples))


def candidate(weapon_id, duration, layers, variant, credits, cache):
    speed, length, cutoff = [(1, 1, 11000), (.88, 1.15, 9000), (1.08, .8, 6500)][variant]
    result = np.zeros(round(duration * length * RATE))
    for effect, pitch, weight in layers:
        source = read_source(effect, credits, cache)
        source = source / max(np.max(np.abs(source)), .00001)
        positions = np.arange(len(result)) * pitch * speed
        layer = np.interp(positions, np.arange(len(source)), source, left=0, right=0)
        result += layer * weight
    if weapon_id in ("music_box", "essence_staff"):
        t = np.arange(len(result)) / RATE
        frequency = 660 if weapon_id == "music_box" else 330
        # Lamelle originale : peu d'harmoniques, pour ne pas faire concurrence au thème musical.
        result += .25 * np.sin(2 * np.pi * frequency * speed * t) * np.exp(-t * 13)
        result += .06 * np.sin(2 * np.pi * frequency * 2.76 * speed * t) * np.exp(-t * 27)
    result = soften(result, cutoff)
    result -= result.mean()
    attack = min(144, len(result) // 8)
    release = min(round(.035 * RATE), len(result) // 3)
    result[:attack] *= np.linspace(0, 1, attack)
    result[-release:] *= np.linspace(1, 0, release)
    return normalize(result)


def normalize(result):
    """Même niveau actif pour comparer les timbres, en préservant une marge sur les transitoires."""
    active = result[np.abs(result) > np.max(np.abs(result)) * .03]
    rms = np.sqrt(np.mean(active ** 2))
    gain = min(10 ** (-23 / 20) / max(rms, 1e-6), 10 ** (-8 / 20) / max(np.max(np.abs(result)), 1e-6))
    return result * gain


def write_wav(path, samples):
    with wave.open(str(path), "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(RATE)
        output.writeframes(np.round(np.clip(samples, -1, 1) * 32767).astype("<i2").tobytes())


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    output = args.output.expanduser().resolve()
    if output.is_relative_to(ROOT):
        parser.error("Les auditions doivent rester hors dépôt.")
    output.mkdir(parents=True, exist_ok=True)
    weapons = json.loads((ROOT / "data/weapons/weapons.json").read_text())
    credits = {entry["effect_id"]: entry for entry in json.loads((ROOT / "assets/audio/CREDITS.json").read_text())}
    bank = json.loads((ROOT / "data/audio/sounds.json").read_text())
    if {w["id"] for w in weapons} != set(RECIPES):
        raise ValueError("Le catalogue a changé : réauditer les recettes.")
    cache, manifest, cards = {}, [], []
    for weapon_index, weapon in enumerate(weapons):
        weapon_id = weapon["id"]
        description, duration, layers = RECIPES[weapon_id]
        figures = []
        current = weapon.get("attack_audio")
        if current:
            name = f"{weapon_id}-actuel.wav"
            samples = read_source(bank[current]["effect_id"], credits, cache)
            write_wav(output / name, normalize(samples))
            figures.append(f'<div><b>Actuel</b><audio controls preload="none" src="{name}"></audio></div>')
        else:
            figures.append('<div><b>Actuellement muette</b></div>')
        for variant, label in enumerate(("A · court", "B · matière", "C · feutré")):
            samples = candidate(weapon_id, duration, layers, variant, credits, cache)
            name = f"{weapon_id}-{chr(65+variant)}.wav"
            write_wav(output / name, samples)
            # Cadence soutenue illustrative : le runtime limite à une voix et 180 ms entre départs.
            period = max(round(.28 * RATE), len(samples) + round(.025 * RATE))
            repeated = np.zeros(period * 7)
            for start in range(0, len(repeated), period):
                repeated[start:start + len(samples)] += samples * 10 ** (-14 / 20)
            repeated_name = name.replace('.wav', '-cadence.wav')
            write_wav(output / repeated_name, repeated)
            figures.append(f'<div><b>{label}</b><audio controls preload="none" src="{name}"></audio>'
                           f'<small>Répétition au gain proposé (−14 dB)</small><audio controls preload="none" src="{repeated_name}"></audio>'
                           f'<label><input type="radio" name="{weapon_id}" value="{chr(65+variant)}">Choisir</label></div>')
            manifest.append({"weapon_id": weapon_id, "candidate": chr(65+variant), "file": name,
                             "sha256": hashlib.sha256((output / name).read_bytes()).hexdigest(),
                             "duration_sec": len(samples) / RATE, "peak_dbfs": float(20*np.log10(np.max(np.abs(samples)))),
                             "bank_volume_db": -14, "integrated": False,
                             "processing": {"description": description, "variant": label, "layers": layers,
                                            "normalization": "RMS actif −23 dBFS, crête plafonnée à −8 dBFS"},
                             "sources": [credits[e] for e, _, _ in layers],
                             "synthesis": "Lamelle sinusoïdale originale" if weapon_id in ("music_box", "essence_staff") else None})
        cards.append(f'<section data-lot="{weapon_index // 6}" {"hidden" if weapon_index >= 6 else ""}><h2>{html.escape(weapon["name"])}</h2><p>{description}</p><div class="options">'
                     + ''.join(figures) + f'</div><label><input type="radio" name="{weapon_id}" value="aucun">Aucun</label> '
                     + f'<input class="note" name="{weapon_id}-note" placeholder="Remarque"></section>')
    (output / 'manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n')
    page = '''<!doctype html><html lang="fr"><meta charset="utf-8"><meta name="viewport" content="width=device-width">
<title>VESTIGES — Sons des armes</title><style>
body{background:#17191c;color:#eee;font:16px/1.5 system-ui;margin:30px auto;max-width:1200px;padding:0 20px}
h1{font-size:26px}h2{font-size:21px}section{padding:20px;margin:18px 0;background:#24272b;border-radius:10px}
.options{display:grid;grid-template-columns:repeat(auto-fit,minmax(235px,1fr));gap:18px}audio{width:100%;display:block;margin:10px 0}
small{display:block;color:#bbc1c9}.note{width:60%;padding:8px}button{padding:12px;cursor:pointer}
</style><h1>Une identité pour chaque arme</h1><p>Propositions à écouter, non intégrées. A : court ; B : matière ; C : feutré.
Les candidats et l'actuel sont ajustés à un niveau comparable pour choisir le timbre. La répétition applique le gain de jeu proposé ;
elle illustre une cadence soutenue, pas toutes les améliorations possibles. Les impacts ennemis restent séparés.</p>
<p><button onclick="showLot(0)">Armes 1–6</button> <button onclick="showLot(1)">Armes 7–12</button> <button onclick="showLot(2)">Armes 13–18</button> <button onclick="showLot(3)">Armes 19–24</button></p><button onclick="exportChoices()">Exporter mes choix</button>'''
    page += ''.join(cards)
    page += '''<script>
function showLot(n){document.querySelectorAll('section').forEach(s=>s.hidden=Number(s.dataset.lot)!==n)}
document.addEventListener('play',e=>{if(e.target.tagName==='AUDIO')document.querySelectorAll('audio').forEach(a=>{if(a!==e.target)a.pause()})},true);
function exportChoices(){let choices={};document.querySelectorAll('section').forEach(s=>{
let first=s.querySelector('input[type=radio]'),checked=s.querySelector('input:checked');
choices[first.name]={choice:checked?.value||'pending',note:s.querySelector('.note').value}});
let a=document.createElement('a');a.href=URL.createObjectURL(new Blob([JSON.stringify(choices,null,2)],{type:'application/json'}));
a.download='choix-armes-2026-10-07.json';a.click();setTimeout(()=>URL.revokeObjectURL(a.href),1000)}
</script></html>'''
    (output / 'index.html').write_text(page)
    print(f"72 candidats, 24 armes : {output / 'index.html'}")


if __name__ == '__main__':
    main()
