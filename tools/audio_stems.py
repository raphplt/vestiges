#!/usr/bin/env python3
"""Page d'isolement d'une run enregistrée avec ses pistes séparées (plan 15, M2).

Usage : tools/audio_stems.py <dossier de tools/record_run_audio.sh lancé avec RECORD_EXTRA_ARGS=--audio-stems>

Remet chaque piste (musique, ambiance, effets, pas) au volume de son bus et à sa place dans l'enregistrement, l'encode
à côté de la vidéo, puis écrit index.html : la vidéo de la run, muette, et le mixage ou les pistes à couper ou
écouter seules, synchrones. Les mesures (niveau par seconde, plancher, planéité spectrale, spectrogrammes) situent
un bruit continu et la famille qui le porte ; elles ne jugent pas le son, l'oreille de Raphaël tranche.
NumPy, Pillow et FFmpeg requis.
"""

import csv
import html
import json
import subprocess
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

RATE = 24000
LABELS = {"mix": "Mixage complet", "music": "Musique", "ambiance": "Ambiance", "sfx": "Effets (sans les pas)", "steps": "Pas"}


def decode(path, filters=None):
    command = ["ffmpeg", "-v", "error", "-i", str(path)]
    if filters:
        command += ["-af", filters]
    command += ["-ac", "1", "-ar", str(RATE), "-f", "f32le", "-"]
    return np.frombuffer(subprocess.run(command, capture_output=True, check=True).stdout, np.float32)


def encode(source, target, filters=None):
    command = ["ffmpeg", "-v", "error", "-y", "-i", str(source)]
    if filters:
        command += ["-af", filters]
    subprocess.run(command + ["-c:a", "aac", "-b:a", "160k", str(target)], check=True)


def per_second(signal):
    """Niveau RMS (dBFS) et planéité spectrale (0 tonal, 1 bruit blanc) de chaque seconde."""
    levels, flatness = [], []
    window = np.hanning(RATE)
    for start in range(0, len(signal) - RATE + 1, RATE):
        frame = signal[start:start + RATE]
        rms = float(np.sqrt(np.mean(frame ** 2)))
        levels.append(20 * np.log10(rms + 1e-9))
        spectrum = np.abs(np.fft.rfft(frame * window))[40:] + 1e-12  # au-dessus de 40 Hz
        flatness.append(float(np.exp(np.mean(np.log(spectrum))) / np.mean(spectrum)))
    return np.array(levels), np.array(flatness)


def spectrogram(signal, target, title):
    """Spectrogramme en fréquences logarithmiques (50 Hz–12 kHz), une colonne par demi-seconde."""
    size, hop, height = 4096, RATE // 2, 160
    columns = max(1, (len(signal) - size) // hop)
    freqs = np.fft.rfftfreq(size, 1 / RATE)
    edges = np.geomspace(50, 12000, height + 1)
    image = np.zeros((height, columns))
    window = np.hanning(size)
    # Une bande trop étroite pour contenir une case de fréquence (dans le grave) prend la case la plus proche.
    bins = [np.flatnonzero((freqs >= edges[row]) & (freqs < edges[row + 1])) for row in range(height)]
    bins = [b if b.size else np.array([np.argmin(np.abs(freqs - edges[row]))]) for row, b in enumerate(bins)]
    for col in range(columns):
        power = np.abs(np.fft.rfft(signal[col * hop:col * hop + size] * window)) ** 2
        for row in range(height):
            image[height - 1 - row, col] = 10 * np.log10(power[bins[row]].mean() + 1e-12)
    # 70 dB de dynamique sous la bande la plus forte de la piste.
    scaled = np.clip((image - image.max() + 70) / 70, 0, 1)
    rgb = np.stack([scaled ** 0.6, scaled ** 1.4, 0.35 + 0.4 * scaled], axis=-1)
    width = min(1600, max(800, columns * 4))
    picture = Image.fromarray((rgb * 255).astype(np.uint8)).resize((width, height * 2), Image.NEAREST)
    canvas = Image.new("RGB", (picture.width, picture.height + 22), (20, 20, 24))
    canvas.paste(picture, (0, 22))
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()
    draw.text((6, 5), f"{title} : 50 Hz en bas, 12 kHz en haut ; une colonne = 0,5 s", fill=(230, 230, 230), font=font)
    canvas.save(target)


def markers(folder):
    """Repères de la trace : signaux de Résurgence, niveaux, coffres, sur l'horloge de l'enregistrement."""
    path = folder / "audio-events.csv"
    if not path.exists():
        return []
    names = {"crisis_warning": "Annonce", "crisis_started": "Début de crise", "crisis_ended": "Fin de crise",
             "level_up": "Niveau", "chest": "Coffre", "music": "Musique"}
    found = []
    with path.open(encoding="utf-8") as file:
        for row in csv.DictReader(file):
            if row["kind"] in names:
                # « 1:20.0s » (Résurgence 1, compte à rebours) devient « Résurgence 1 » ; « chest_common:common », « common ».
                detail = row["detail"].split(":")[0] if row["kind"].startswith("crisis") else row["detail"].split(":")[-1]
                found.append((float(row["audio_s"]), f"{names[row['kind']]} {detail}"))
    return found


def frequent_sounds(folder, count=12):
    path = folder / "audio-sounds.csv"
    if not path.exists():
        return []
    with path.open(encoding="utf-8") as file:
        rows = [(row["key"], int(row["played"])) for row in csv.DictReader(file)]
    return sorted(rows, key=lambda item: -item[1])[:count]


def main():
    folder = Path(sys.argv[1]).resolve()
    manifest = json.loads((folder / "stems.json").read_text(encoding="utf-8"))
    delay_ms = int(round(manifest["start_audio_s"] * 1000))
    tracks = {"mix": decode(folder / "audio.flac")}
    encode(folder / "audio.flac", folder / "mix.m4a")
    for name, stem in manifest["stems"].items():
        filters = f"adelay={delay_ms}:all=1,volume={stem['bus_volume_db']}dB"
        encode(folder / stem["file"], folder / f"stem-{name}.m4a", filters)
        tracks[name] = decode(folder / stem["file"], filters)

    # Les mesures portent sur le temps de jeu enregistré, pas sur le chargement qui précède les pistes.
    begin = int(manifest["start_audio_s"]) + 1
    rows = []
    for name, signal in tracks.items():
        levels, flatness = per_second(signal)
        levels, flatness = levels[begin:], flatness[begin:]
        audible = levels > -70
        rows.append({
            "name": name,
            "floor": float(np.percentile(levels, 10)),
            "median": float(np.median(levels)),
            "peak": float(np.percentile(levels, 95)),
            "flat": float(np.median(flatness[audible])) if audible.any() else 0.0,
            "audible": float(audible.mean() * 100),
        })
        spectrogram(signal[begin * RATE:], folder / f"spectre-{name}.png", LABELS[name])
    (folder / "isolement.json").write_text(json.dumps(rows, indent=2, ensure_ascii=False), encoding="utf-8")
    write_page(folder, rows, markers(folder), frequent_sounds(folder))
    print(f"[audio_stems] {folder / 'index.html'}")
    for row in rows:
        print(f"[audio_stems] {row['name']}: plancher {row['floor']:.1f} dBFS, médiane {row['median']:.1f}, "
              f"95e centile {row['peak']:.1f}, planéité {row['flat']:.2f}, audible {row['audible']:.0f} % des secondes")


def write_page(folder, rows, marks, sounds):
    def fmt(seconds):
        return f"{int(seconds // 60)}:{int(seconds % 60):02d}"

    table = "\n".join(
        f"<tr><td>{LABELS[r['name']]}</td><td>{r['floor']:.1f}</td><td>{r['median']:.1f}</td><td>{r['peak']:.1f}</td>"
        f"<td>{r['flat']:.2f}</td><td>{r['audible']:.0f} %</td></tr>" for r in rows)
    buttons = "\n".join(
        f'<button class="mark" data-t="{t:.2f}">{fmt(t)} · {html.escape(label)}</button>'
        for t, label in marks if not label.startswith("Musique -"))
    sound_list = ", ".join(f"{html.escape(key)} ({count})" for key, count in sounds)
    stems = [r["name"] for r in rows if r["name"] != "mix"]
    toggles = "\n".join(
        f'<label><input type="checkbox" class="stem" data-stem="{s}" checked> {LABELS[s]}</label>'
        f'<button class="solo" data-stem="{s}">{LABELS[s]} seule</button>' for s in stems)
    audios = "\n".join(
        f'<audio id="a-{name}" src="{"mix.m4a" if name == "mix" else f"stem-{name}.m4a"}" preload="auto"></audio>'
        for name in ["mix"] + stems)
    images = "\n".join(f'<figure><img src="spectre-{r["name"]}.png" alt="Spectrogramme {LABELS[r["name"]]}"></figure>'
                       for r in rows)
    page = f"""<!doctype html>
<html lang="fr"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Isolement du frottement</title>
<style>
:root {{ color-scheme: dark; --bg:#141418; --fg:#e8e6e1; --muted:#9a978f; --accent:#d4a843; --line:#2c2c33; }}
body {{ background:var(--bg); color:var(--fg); font:15px/1.5 system-ui, sans-serif; margin:0 auto; max-width:1100px; padding:16px; }}
h1 {{ font-size:1.4rem; }} h2 {{ font-size:1.1rem; margin-top:2rem; }}
video {{ width:100%; background:#000; border-radius:6px; }}
.panel {{ display:flex; flex-wrap:wrap; gap:8px 16px; align-items:center; margin:12px 0; }}
button {{ background:#25252c; color:var(--fg); border:1px solid var(--line); border-radius:5px; padding:4px 10px; cursor:pointer; }}
button.on {{ border-color:var(--accent); color:var(--accent); }}
.marks {{ display:flex; flex-wrap:wrap; gap:6px; }}
table {{ border-collapse:collapse; width:100%; }} td, th {{ border-bottom:1px solid var(--line); padding:4px 8px; text-align:right; }}
td:first-child, th:first-child {{ text-align:left; }}
figure {{ margin:8px 0; }} img {{ max-width:100%; display:block; }}
p.note {{ color:var(--muted); }}
</style></head><body>
<h1>Isolement du frottement</h1>
<p>La vidéo est muette ; le son vient des pistes ci-dessous, synchrones. Écoute le <b>mixage complet</b>, puis passe en
<b>pistes séparées</b> et coupe-les une à une, ou écoute-en une seule : celle qui porte le frottement est celle dont
la coupure le fait disparaître. Note l'horodatage de la vidéo si le bruit n'est présent qu'à certains moments.</p>
<video id="video" src="run.mp4" controls muted playsinline></video>
<div class="panel">
<button id="mode-mix" class="on">Mixage complet</button>
<button id="mode-stems">Pistes séparées</button>
{toggles}
<button id="all">Toutes les pistes</button>
</div>
<div class="marks">{buttons}</div>
{audios}
<h2>Mesures par famille</h2>
<p class="note">Niveaux RMS par seconde, en dBFS, au volume du bus. Le <b>plancher</b> (10e centile) est le niveau
présent même dans les moments calmes : un bruit continu le relève. La <b>planéité</b> va de 0 (son tonal, musique)
à 1 (bruit blanc) : un frottement est un bruit large bande, donc plat. Ces chiffres situent un suspect ; ils ne
remplacent pas l'écoute.</p>
<table><tr><th>Piste</th><th>Plancher</th><th>Médiane</th><th>95e centile</th><th>Planéité</th><th>Audible</th></tr>
{table}</table>
<p class="note">Sons les plus joués de la run : {sound_list}.</p>
<h2>Spectrogrammes</h2>
<p class="note">Un bruit continu apparaît comme une bande horizontale diffuse, présente du début à la fin.</p>
{images}
<script>
const video = document.getElementById('video');
const mix = document.getElementById('a-mix');
const stems = [...document.querySelectorAll('audio')].filter(a => a !== mix);
let mode = 'mix';
function applyMutes() {{
  mix.muted = mode !== 'mix';
  document.querySelectorAll('input.stem').forEach(box => {{
    document.getElementById('a-' + box.dataset.stem).muted = mode !== 'stems' || !box.checked;
  }});
  document.getElementById('mode-mix').classList.toggle('on', mode === 'mix');
  document.getElementById('mode-stems').classList.toggle('on', mode === 'stems');
}}
function sync(force) {{
  for (const a of [mix, ...stems]) {{
    if (force || Math.abs(a.currentTime - video.currentTime) > 0.08) a.currentTime = video.currentTime;
    if (video.paused) a.pause(); else if (a.paused) a.play().catch(() => {{}});
  }}
}}
video.addEventListener('play', () => sync(true));
video.addEventListener('pause', () => sync(true));
video.addEventListener('seeked', () => sync(true));
setInterval(() => {{ if (!video.paused) sync(false); }}, 500);
document.getElementById('mode-mix').onclick = () => {{ mode = 'mix'; applyMutes(); }};
document.getElementById('mode-stems').onclick = () => {{ mode = 'stems'; applyMutes(); }};
document.getElementById('all').onclick = () => {{
  mode = 'stems'; document.querySelectorAll('input.stem').forEach(b => b.checked = true); applyMutes();
}};
document.querySelectorAll('input.stem').forEach(box => box.onchange = () => {{ mode = 'stems'; applyMutes(); }});
document.querySelectorAll('button.solo').forEach(button => button.onclick = () => {{
  mode = 'stems';
  document.querySelectorAll('input.stem').forEach(b => b.checked = b.dataset.stem === button.dataset.stem);
  applyMutes();
}});
document.querySelectorAll('button.mark').forEach(button => button.onclick = () => {{
  video.currentTime = Math.max(0, parseFloat(button.dataset.t) - 2); video.play();
}});
applyMutes();
</script>
</body></html>
"""
    (folder / "index.html").write_text(page, encoding="utf-8")


if __name__ == "__main__":
    main()
