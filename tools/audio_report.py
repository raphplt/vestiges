#!/usr/bin/env python3
"""Rapport d'un enregistrement de run (tools/record_run_audio.sh, plan 15 A0).

Lit audio.flac et la trace d'écoute (audio-events.csv, audio-states.csv, audio-sounds.csv), puis écrit :
- rapport.md : transitions musicales, découpage de chaque Résurgence (avant, annonce, début, crise, accalmie)
  avec niveau sonore, musique entendue, sons joués et voix coupées ;
- sonie.png : sonie court terme (EBU R128, fenêtre 3 s) sur toute la run, phases en couleur, événements marqués ;
- spectre-resurgence-N.png : spectrogramme de chaque Résurgence, de 30 s avant l'annonce à 30 s après la fin.

Ces mesures situent un moment et comparent deux enregistrements ; elles ne jugent pas le son. Seule l'écoute le fait.
Usage : python3 tools/audio_report.py <répertoire d'enregistrement>
"""
import csv
import math
import re
import subprocess
import sys
from collections import Counter
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

PHASE_COLORS = {
    "avant": (60, 90, 70),
    "annonce": (150, 120, 40),
    "début": (190, 60, 50),
    "crise": (130, 40, 40),
    "accalmie": (50, 80, 140),
}
FONT_PATHS = ("/usr/share/fonts/noto/NotoSans-Regular.ttf", "/usr/share/fonts/TTF/DejaVuSans.ttf",
              "/System/Library/Fonts/Supplemental/Arial.ttf")
BEFORE_SECONDS = 30.0
ONSET_SECONDS = 5.0
CALM_SECONDS = 30.0


def label_font():
    """Police accentuée si le système en a une ; la police par défaut de PIL ignore les accents."""
    for path in FONT_PATHS:
        if Path(path).exists():
            return ImageFont.truetype(path, 13)
    return ImageFont.load_default()


def read_csv(path):
    with open(path, newline="", encoding="utf-8") as handle:
        return list(csv.DictReader(handle))


def loudness_frames(audio):
    """Sonie momentanée (M, 400 ms) et court terme (S, 3 s) toutes les 100 ms, d'après le filtre ebur128 de ffmpeg."""
    result = subprocess.run(
        ["ffmpeg", "-hide_banner", "-nostats", "-v", "verbose", "-i", str(audio), "-af", "ebur128=framelog=verbose", "-f", "null", "-"],
        capture_output=True, text=True, check=True)
    pattern = re.compile(r"t:\s*([\d.]+)\s+TARGET:.*?M:\s*(-?[\d.]+|-inf)\s+S:\s*(-?[\d.]+|-inf)")
    frames = []
    for line in result.stderr.splitlines():
        match = pattern.search(line)
        if match:
            frames.append(tuple(float(v) if v != "-inf" else -120.0 for v in match.groups()))
    return frames


def mean_loudness(frames, start, end):
    """Moyenne énergétique de la sonie momentanée sur [start, end), en LUFS."""
    values = [m for t, m, _ in frames if start <= t < end and m > -70]
    if not values:
        return None
    return 10 * math.log10(sum(10 ** (v / 10) for v in values) / len(values))


def crisis_windows(events):
    """Une Résurgence par annonce : (numéro, annonce, début, fin), en secondes d'enregistrement."""
    crises = {}
    for row in events:
        kind, detail, at = row["kind"], row["detail"], float(row["audio_s"])
        if kind in ("crisis_warning", "crisis_started", "crisis_ended"):
            number = int(detail.split(":")[0])
            crises.setdefault(number, {})[kind] = at
    windows = []
    for number, marks in sorted(crises.items()):
        warning = marks.get("crisis_warning")
        started = marks.get("crisis_started")
        ended = marks.get("crisis_ended")
        if warning is None and started is not None:
            warning = started
        if warning is not None:
            windows.append((number, warning, started, ended))
    return windows


def segments_of(window, end_of_record):
    number, warning, started, ended = window
    segments = [("avant", warning - BEFORE_SECONDS, warning)]
    segments.append(("annonce", warning, started if started is not None else end_of_record))
    if started is not None:
        segments.append(("début", started, min(started + ONSET_SECONDS, ended or end_of_record)))
        segments.append(("crise", started, ended if ended is not None else end_of_record))
    if ended is not None:
        segments.append(("accalmie", ended, min(ended + CALM_SECONDS, end_of_record)))
    return [(name, max(0.0, a), b) for name, a, b in segments if b > a]


def sounds_between(events, start, end):
    played, stolen = Counter(), 0
    for row in events:
        at = float(row["audio_s"])
        if row["kind"] != "sound" or not start <= at < end:
            continue
        key, outcome = row["detail"].rsplit(":", 1)
        played[key] += 1
        if outcome == "VoiceStolen":
            stolen += 1
    return played, stolen


def music_between(events, states, start, end):
    """Musiques qui sonnent sur l'intervalle, dans l'ordre : celle en cours au début, puis chaque changement."""
    current = "-"
    for row in states:
        if float(row["audio_s"]) <= start:
            current = row["music"]
    for row in events:
        if row["kind"] == "music" and float(row["audio_s"]) <= start:
            current = row["detail"]
    heard = [current]
    for row in events:
        at = float(row["audio_s"])
        if row["kind"] == "music" and start < at < end:
            heard.append(f"{row['detail']} ({at - start:+.1f} s)")
    return heard


def draw_timeline(frames, events, windows, end_of_record, path):
    width, height, margin = 1800, 420, 40
    image = Image.new("RGB", (width, height), (18, 18, 22))
    draw = ImageDraw.Draw(image)
    font = label_font()
    plot_w, plot_h = width - 2 * margin, height - 2 * margin

    def x_of(t):
        return margin + int(plot_w * t / max(end_of_record, 1.0))

    def y_of(lufs):
        lufs = max(-60.0, min(0.0, lufs))
        return margin + int(plot_h * (-lufs) / 60.0)

    for window in windows:
        for name, a, b in segments_of(window, end_of_record):
            if name == "début":
                continue
            draw.rectangle([x_of(a), margin, x_of(b), height - margin], fill=PHASE_COLORS[name])
    for lufs in range(0, -61, -10):
        draw.line([(margin, y_of(lufs)), (width - margin, y_of(lufs))], fill=(60, 60, 70))
        draw.text((4, y_of(lufs) - 6), f"{lufs}", fill=(170, 170, 170), font=font)
    for minute in range(0, int(end_of_record // 60) + 1):
        draw.text((x_of(minute * 60) - 6, height - margin + 6), f"{minute}:00", fill=(170, 170, 170), font=font)
    points = [(x_of(t), y_of(s)) for t, _, s in frames]
    if len(points) > 1:
        draw.line(points, fill=(235, 235, 235), width=1)
    marks = {"music": (120, 220, 255), "level_up": (120, 255, 140), "chest": (255, 210, 90)}
    for row in events:
        color = marks.get(row["kind"])
        if color:
            x = x_of(float(row["audio_s"]))
            draw.line([(x, margin), (x, margin + 14)], fill=color, width=2)
    draw.text((margin, 8), "Sonie court terme (LUFS, 3 s) — vert avant, jaune annonce, rouge crise, bleu accalmie ; "
              "repères : musique (cyan), niveau (vert), coffre (or)", fill=(220, 220, 220), font=font)
    image.save(path)


def draw_spectrum(audio, window, end_of_record, directory):
    number, warning, started, ended = window
    start = max(0.0, warning - BEFORE_SECONDS)
    stop = min(end_of_record, (ended if ended is not None else end_of_record) + CALM_SECONDS)
    path = directory / f"spectre-resurgence-{number}.png"
    subprocess.run(
        ["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-ss", f"{start:.2f}", "-t", f"{stop - start:.2f}",
         "-i", str(audio), "-lavfi", "showspectrumpic=s=1600x400:legend=0:scale=log:fscale=log:mode=combined",
         str(path)], check=True)
    image = Image.open(path).convert("RGB")
    draw = ImageDraw.Draw(image)
    font = label_font()
    for label, at in (("annonce", warning), ("début", started), ("fin", ended)):
        if at is None:
            continue
        x = int(image.width * (at - start) / (stop - start))
        draw.line([(x, 0), (x, image.height)], fill=(255, 255, 255), width=2)
        draw.text((x + 4, 4), label, fill=(255, 255, 255), font=font)
    image.save(path)
    return path.name


def main():
    directory = Path(sys.argv[1])
    audio = directory / "audio.flac"
    events = read_csv(directory / "audio-events.csv")
    states = read_csv(directory / "audio-states.csv")
    sounds = read_csv(directory / "audio-sounds.csv")
    frames = loudness_frames(audio)
    end_of_record = frames[-1][0] if frames else 0.0
    windows = crisis_windows(events)

    lines = [f"# Écoute instrumentée — {directory.name}", ""]
    start = next((float(r["audio_s"]) for r in events if r["kind"] == "trace" and r["detail"] == "start"), 0.0)
    lines.append(f"Enregistrement {end_of_record:.0f} s, run tracée à partir de {start:.1f} s. "
                 f"Sonie moyenne de la run : {mean_loudness(frames, start, end_of_record) or float('nan'):.1f} LUFS.")
    lines.append("")
    lines.append("Mesures de repérage : elles ne remplacent pas l'écoute.")
    lines.append("")

    lines.append("## Musique et signaux, dans l'ordre")
    lines.append("")
    lines.append("| Enregistrement | Jeu | Événement |")
    lines.append("|---|---|---|")
    for row in events:
        if row["kind"] in ("music", "phase", "crisis_warning", "crisis_started", "crisis_ended", "state"):
            lines.append(f"| {float(row['audio_s']):.1f} s | {float(row['game_s']):.1f} s | {row['kind']} {row['detail']} |")
    lines.append("")

    for window in windows:
        number = window[0]
        lines.append(f"## Résurgence {number}")
        lines.append("")
        lines.append("| Moment | Intervalle | Sonie | Musique | Sons joués | Voix coupées | Sons les plus fréquents |")
        lines.append("|---|---|---|---|---|---|---|")
        for name, a, b in segments_of(window, end_of_record):
            played, stolen = sounds_between(events, a, b)
            loudness = mean_loudness(frames, a, b)
            top = ", ".join(f"{k} ×{v}" for k, v in played.most_common(4))
            lines.append(f"| {name} | {a:.1f}–{b:.1f} s | {'—' if loudness is None else f'{loudness:.1f} LUFS'} | "
                         f"{' → '.join(music_between(events, states, a, b))} | {sum(played.values())} | {stolen} | {top} |")
        lines.append("")
        lines.append(f"![Spectre]({draw_spectrum(audio, window, end_of_record, directory)})")
        lines.append("")

    lines.append("## Sons sur toute la run")
    lines.append("")
    lines.append("| Clé | Joués | Limités | Voix coupées | Interface |")
    lines.append("|---|---|---|---|---|")
    for row in sorted(sounds, key=lambda r: -(int(r["played"]) + int(r["voice_stolen"]) + int(r["interface"]))):
        lines.append(f"| {row['key']} | {row['played']} | {row['throttled']} | {row['voice_stolen']} | {row['interface']} |")
    lines.append("")

    # Traces d'avant le plan 15 A1 : compteur d'ennemis que tenait l'AudioManager pour choisir la musique de combat.
    counter_rows = [r for r in states if int(r.get("audio_enemy_counter", -1)) >= 0]
    if counter_rows:
        drift = max(int(r["audio_enemy_counter"]) - int(r["alive"]) for r in counter_rows)
        lines.append(f"Compteur d'ennemis de l'AudioManager : écart maximal de {drift} au-dessus des créatures actives.")
        lines.append("")

    draw_timeline(frames, events, windows, end_of_record, directory / "sonie.png")
    lines.append("![Sonie](sonie.png)")
    (directory / "rapport.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"[audio_report] {directory / 'rapport.md'}")


if __name__ == "__main__":
    main()
