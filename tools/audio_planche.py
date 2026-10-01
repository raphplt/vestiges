#!/usr/bin/env python3
"""Planche d'écoute (plan 15) : un extrait par enregistrement de tools/record_run_audio.sh, côte à côte.

Chaque extrait couvre la première Résurgence, de 30 s avant l'annonce à 30 s après la fin, images et son.
La planche reste un fichier local, hors dépôt : titre, déclenchement, lecteurs et choix.
Usage : python3 tools/audio_planche.py <planche.html> "<titre>" <enregistrement>[=<libellé>] ...
"""
import csv
import html
import subprocess
import sys
from pathlib import Path

MARGIN_SECONDS = 30.0


def cycle_window(directory):
    """Annonce, début et fin de la première Résurgence, en secondes d'enregistrement."""
    marks = {}
    with open(directory / "audio-events.csv", newline="", encoding="utf-8") as handle:
        for row in csv.DictReader(handle):
            if row["kind"] in ("crisis_warning", "crisis_started", "crisis_ended") and row["detail"].split(":")[0] == "1":
                marks.setdefault(row["kind"], float(row["audio_s"]))
    if "crisis_warning" not in marks:
        raise SystemExit(f"{directory} : aucune annonce de Résurgence dans la trace")
    return marks


def main():
    if len(sys.argv) < 4:
        raise SystemExit(__doc__)
    page = Path(sys.argv[1]).resolve()
    title = sys.argv[2]
    page.parent.mkdir(parents=True, exist_ok=True)
    rows = []
    for argument in sys.argv[3:]:
        path, _, label = argument.partition("=")
        directory = Path(path).resolve()
        label = label or directory.name
        marks = cycle_window(directory)
        start = max(0.0, marks["crisis_warning"] - MARGIN_SECONDS)
        stop = marks.get("crisis_ended", marks["crisis_warning"] + 90.0) + MARGIN_SECONDS
        clip = page.parent / f"{page.stem}-{directory.name}.mp4"
        subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-ss", f"{start:.2f}", "-t",
                        f"{stop - start:.2f}", "-i", str(directory / "run.mp4"), "-c:v", "libx264", "-preset", "veryfast",
                        "-crf", "26", "-c:a", "aac", "-b:a", "192k", str(clip)], check=True)
        cues = [f"annonce {marks['crisis_warning'] - start:.0f} s"]
        if "crisis_started" in marks:
            cues.append(f"début {marks['crisis_started'] - start:.0f} s")
        if "crisis_ended" in marks:
            cues.append(f"fin {marks['crisis_ended'] - start:.0f} s")
        rows.append((label, clip.name, " · ".join(cues)))

    items = "\n".join(
        f"""<section><h2>{html.escape(label)}</h2><p>{html.escape(cues)}</p>
<video controls preload="metadata" src="{html.escape(name)}"></video>
<label><input type="radio" name="choix" value="{html.escape(label)}"> Choisir</label></section>"""
        for label, name, cues in rows)
    page.write_text(f"""<!doctype html>
<html lang="fr"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>{html.escape(title)}</title>
<style>
:root {{ color-scheme: light dark; --fond: #f4f2ee; --texte: #222; --carte: #fff; }}
@media (prefers-color-scheme: dark) {{ :root {{ --fond: #18181b; --texte: #eee; --carte: #26262b; }} }}
body {{ margin: 0; padding: 16px; background: var(--fond); color: var(--texte); font: 15px/1.5 system-ui, sans-serif; }}
section {{ background: var(--carte); border-radius: 8px; padding: 12px 16px; margin: 0 0 16px; max-width: 980px; }}
video {{ width: 100%; border-radius: 4px; }}
h1 {{ font-size: 20px; }} h2 {{ font-size: 16px; margin: 0; }}
</style></head><body>
<h1>{html.escape(title)}</h1>
<p>Déclenchement : première Résurgence, de 30 s avant l'annonce à 30 s après la fin. Même seed, même bot, mixage
du moteur enregistré image par image. Écouter au même volume, de préférence au casque.</p>
{items}
</body></html>
""", encoding="utf-8")
    print(f"[audio_planche] {page}")


if __name__ == "__main__":
    main()
