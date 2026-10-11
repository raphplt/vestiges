#!/usr/bin/env python3
"""Garde-fous du mix (plan 15) : mesure chaque son de la banque et vérifie les cibles de data/audio/mix_targets.json.

Usage : python3 tools/audio_bank_audit.py [--csv sortie.csv]
Code de sortie 1 si une règle est enfreinte : famille hors cible, son ponctuel trop fort, silence de tête, phase
opposée, fichier absent ou ducking incomplet. Les fichiers écrêtés à la source sont signalés sans échec.
FFmpeg seul requis (bibliothèque standard de Python) : la mesure ne juge pas le son, elle empêche le mix de dériver
quand un son est ajouté ou remplacé.
"""
import argparse
import array
import csv
import json
import math
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
RATE = 48000


def ffmpeg_lines(path, filters):
    result = subprocess.run(["ffmpeg", "-hide_banner", "-nostats", "-i", str(path), "-af", filters, "-f", "null", "-"],
                            capture_output=True, text=True)
    return result.stderr.splitlines()


def momentary_max(path):
    values = [float(line.split("=")[1]) for line in
              ffmpeg_lines(path, "apad=pad_dur=0.4,ebur128=metadata=1,ametadata=print:key=lavfi.r128.M")
              if "lavfi.r128.M=" in line]
    return max(values)


def integrated(path):
    lines = [line for line in ffmpeg_lines(path, "ebur128") if line.strip().startswith("I:")]
    return float(lines[-1].split()[1])


def samples(path):
    """Échantillons entrelacés en flottants et nombre de canaux."""
    probe = subprocess.run(["ffprobe", "-v", "error", "-show_entries", "stream=channels", "-of", "csv=p=0", str(path)],
                           capture_output=True, text=True, check=True)
    channels = int(probe.stdout.strip().splitlines()[0])
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", str(path), "-ar", str(RATE), "-f", "s16le", "-"],
                         capture_output=True, check=True).stdout
    data = array.array("h")
    data.frombytes(raw)
    if sys.byteorder == "big":
        data.byteswap()
    return data, channels


def shape(path):
    data, channels = samples(path)
    threshold = 32768 * 10 ** (-50 / 20)
    first = next((i for i, v in enumerate(data) if abs(v) > threshold), len(data))
    lead_ms = first // channels / RATE * 1000
    clipped = sum(1 for v in data if v >= 32766 or v <= -32767)
    correlation = None
    if channels == 2:
        left, right = data[0::2], data[1::2]
        sl = sum(v * v for v in left)
        sr = sum(v * v for v in right)
        if sl > 0 and sr > 0:
            correlation = sum(a * b for a, b in zip(left, right)) / math.sqrt(sl * sr)
    return lead_ms, clipped, correlation


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--csv", help="écrit toutes les mesures dans ce fichier")
    args = parser.parse_args()
    bank = json.loads((ROOT / "data/audio/sounds.json").read_text())
    targets = json.loads((ROOT / "data/audio/mix_targets.json").read_text())
    families = [dict(f, pattern=re.compile(f["keys"])) for f in targets["families"]]
    failures, warnings, rows = [], [], []

    for key, sound in bank.items():
        if "duck_music_db" in sound and "duck_sec" not in sound:
            failures.append(f"{key} : duck_music_db sans duck_sec")
        family = next((f for f in families if f["pattern"].search(key)), None)
        files = sound.get("variants") or [sound["path"]]
        for index, res_path in enumerate(files):
            path = ROOT / res_path.replace("res://", "")
            if not path.exists():
                failures.append(f"{key} : fichier absent {res_path}")
                continue
            music = family is not None and family.get("measure") == "integrated"
            level = integrated(path) if music else momentary_max(path)
            effective = level + sound.get("volume_db", 0) + (family or {}).get("call_offset_db", 0)
            row = {"key": key, "variant": index, "family": family["id"] if family else "", "level": round(level, 1),
                   "volume_db": sound.get("volume_db", 0), "effective": round(effective, 1)}
            if family:
                gap = effective - family["target"]
                if abs(gap) > family["tolerance"]:
                    failures.append(f"{key} : {effective:.1f} pour une cible {family['id']} de {family['target']} "
                                    f"(±{family['tolerance']}), corriger volume_db de {-gap:+.1f}")
            if not music:
                if effective > targets["max_effective"]:
                    failures.append(f"{key} : niveau effectif {effective:.1f} au-dessus du plafond {targets['max_effective']}")
                lead_ms, clipped, correlation = shape(path)
                row.update(lead_ms=round(lead_ms), clipped=clipped,
                           correlation=None if correlation is None else round(correlation, 3))
                if lead_ms > targets["lead_silence_ms"] and key not in targets["lead_exempt"]:
                    failures.append(f"{key} : {lead_ms:.0f} ms de silence de tête (le son part en retard)")
                if correlation is not None and correlation < targets["min_lr_correlation"]:
                    failures.append(f"{key} : corrélation gauche/droite {correlation:.2f}, le son s'annule en mono")
                if clipped:
                    warnings.append(f"{key} : {clipped} échantillons écrêtés dans le fichier source")
            rows.append(row)

    if args.csv:
        with open(args.csv, "w", newline="") as handle:
            writer = csv.DictWriter(handle, fieldnames=["key", "variant", "family", "level", "volume_db", "effective",
                                                        "lead_ms", "clipped", "correlation"])
            writer.writeheader()
            writer.writerows(rows)
    for line in warnings:
        print(f"[AudioBankAudit] avertissement : {line}")
    for line in failures:
        print(f"[AudioBankAudit] ÉCHEC : {line}")
    print(f"[AudioBankAudit] {len(rows)} fichiers mesurés, {len(failures)} échec(s), {len(warnings)} avertissement(s)")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
