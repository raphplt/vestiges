#!/usr/bin/env python3
"""Chronologie des temps forts (plan 30) à partir d'une mesure `tools/measure_run.sh` lancée avec --timeline.

Usage : python3 tools/summarize_timeline.py <dossier de mesure> [--bin 30] [--svg planche.svg] [--title texte]
Lit, pour chaque graine, density-<seed>.csv, beats-<seed>.csv et timeline-<seed>.csv ; écrit sur la sortie standard
un tableau par tranche (moyenne des graines), la liste des temps forts par graine et les creux, chaque Résurgence et
micro-événement comparé à la minute d'avant et d'après, et, avec --svg,
une planche : foule proche par seconde, Résurgences, micro-événements, élites et Souverains, niveaux.
"""
import argparse
import csv
from pathlib import Path
from statistics import mean

# Ce qui change la situation à l'écran ; un niveau, un coffre ou une élite seule n'en font pas partie.
MAJOR = ("crisis_start", "event_start", "champion")
EVENT_COLORS = {
    "hunt": "#c0392b", "stampede": "#d68910", "fallen_relic": "#2e86c1", "vigil": "#7d3c98", "shard_rain": "#17a589",
}


def load(root):
    """Dossier de measure_run.sh (seed-<graine>/…) ou dossier à plat (CSV copiés dans un audit)."""
    runs = []
    folders = [(f, f.name.removeprefix("seed-")) for f in sorted(root.glob("seed-*")) if f.is_dir()]
    folders += [(root, f.stem.removeprefix("timeline-")) for f in sorted(root.glob("timeline-*.csv"))]
    for folder, seed in folders:
        try:
            density = list(csv.DictReader((folder / f"density-{seed}.csv").open()))
            beats = list(csv.DictReader((folder / f"beats-{seed}.csv").open()))
            events = list(csv.DictReader((folder / f"timeline-{seed}.csv").open()))
        except OSError:
            continue
        runs.append({"seed": seed, "density": density, "beats": beats, "events": events})
    return runs


def by_second(rows):
    return {int(float(row["t"])): row for row in rows}


def bins(run, size, seconds):
    density, beats = by_second(run["density"]), by_second(run["beats"])
    out = []
    for start in range(0, seconds, size):
        secs = [s for s in range(start + 1, start + size + 1) if s in density and s in beats]
        if not secs:
            continue
        first, last = density[max(min(secs) - 1, min(density))], density[max(secs)]
        span = max(secs) - min(secs) + 1
        events = [e for e in run["events"] if start <= float(e["t"]) < start + size]
        out.append({
            "start": start,
            "visible": mean(float(density[s]["visible"]) for s in secs),
            "near": mean(float(density[s]["near600"]) for s in secs),
            "close": mean(float(beats[s]["close300"]) for s in secs),
            "near_hp": mean(float(beats[s]["near_hp"]) for s in secs),
            "alive": mean(float(density[s]["alive"]) for s in secs),
            "spawned_s": (float(last["spawned"]) - float(first["spawned"])) / span,
            "killed_s": (float(last["killed"]) - float(first["killed"])) / span,
            "hit_min": (float(last["hit_damage"]) - float(first["hit_damage"])) * 60.0 / span,
            "essence": float(last["essence_gained"]),
            "level": int(last["level"]),
            "crisis": sum(beats[s]["crisis"] == "active" for s in secs) / len(secs),
            "warning": sum(beats[s]["crisis"] == "warning" for s in secs) / len(secs),
            "event": sum(beats[s]["event"] != "" for s in secs) / len(secs),
            "elites_near": mean(float(beats[s]["elites_near"]) for s in secs),
            "champions": max(int(beats[s]["champions"]) for s in secs),
            "elite_new": sum(e["kind"] == "elite" for e in events),
            "levels": sum(e["kind"] == "level" for e in events),
            "chests": sum(e["kind"] == "chest" for e in events),
        })
    return out


def contrasts(runs, seconds):
    """Chaque Résurgence et micro-événement comparé à la minute d'avant et à celle d'après (moyenne des graines)."""
    found = {}
    for run in runs:
        density, beats = by_second(run["density"]), by_second(run["beats"])

        def level(a, b, key, source):
            values = [float(source[s][key]) for s in range(int(a) + 1, int(b) + 1) if s in source]
            return mean(values) if values else None

        def rate(a, b, key, factor=1.0):
            a, b = int(a), int(b)
            if a not in density or b not in density or b <= a:
                return None
            return (float(density[b][key]) - float(density[a][key])) * factor / (b - a)

        starts = {}
        for e in run["events"]:
            t = float(e["t"])
            if e["kind"] in ("crisis_start", "event_start"):
                starts[(e["kind"], e["id"])] = t
            elif e["kind"] in ("crisis_end", "event_end"):
                key = ("crisis_start" if e["kind"] == "crisis_end" else "event_start", e["id"])
                start = starts.pop(key, None)
                if start is None or t > seconds:
                    continue
                windows = ((max(0.0, start - 60), start), (start, t), (t, min(float(seconds), t + 60)))
                name = f"Résurgence {e['id']}" if e["kind"] == "crisis_end" else e["id"]
                found.setdefault(name, []).append({
                    "duration": t - start,
                    "near": [level(a, b, "near600", density) for a, b in windows],
                    "close": [level(a, b, "close300", beats) for a, b in windows],
                    "killed": [rate(a, b, "killed") for a, b in windows],
                    "hit": [rate(a, b, "hit_damage", 60.0) for a, b in windows],
                })

    def cell(items, key, digits):
        parts = []
        for index in range(3):
            values = [item[key][index] for item in items if item[key][index] is not None]
            parts.append(f"{mean(values):.{digits}f}" if values else "–")
        return " / ".join(parts)

    print("| Moment | n | Durée | Proches 600 px avant / pendant / après | Proches 300 px | Tuées/s | Dégâts reçus/min |")
    print("|---|---|---|---|---|---|---|")
    for name in sorted(found):
        items = found[name]
        print(f"| {name} | {len(items)} | {mean(i['duration'] for i in items):.0f} s | {cell(items, 'near', 0)} | "
              f"{cell(items, 'close', 0)} | {cell(items, 'killed', 1)} | {cell(items, 'hit', 0)} |")
    print()


def fmt_time(seconds):
    seconds = int(round(seconds))
    return f"{seconds // 60}:{seconds % 60:02d}"


def report(runs, size, seconds):
    per_run = [bins(run, size, seconds) for run in runs]
    print(f"## {len(runs)} graines : {', '.join(run['seed'] for run in runs)}\n")
    print("| Tranche | Proches 600 px | Proches 300 px | Visibles | En vie | Apparues/s | Tuées/s | Dégâts reçus/min | Niveau | Résurgence | Événement | Élites proches | Souverain |")
    print("|---|---|---|---|---|---|---|---|---|---|---|---|---|")
    for index in range(min(len(b) for b in per_run)):
        rows = [b[index] for b in per_run]
        m = lambda key: mean(row[key] for row in rows)
        print(f"| {fmt_time(rows[0]['start'])} | {m('near'):.0f} | {m('close'):.0f} | {m('visible'):.0f} | {m('alive'):.0f} | "
              f"{m('spawned_s'):.1f} | {m('killed_s'):.1f} | {m('hit_min'):.0f} | {m('level'):.0f} | {100 * m('crisis'):.0f} % | "
              f"{100 * m('event'):.0f} % | {m('elites_near'):.1f} | {sum(row['champions'] > 0 for row in rows)}/{len(rows)} |")
    print()
    for run in runs:
        print(f"### Graine {run['seed']}\n")
        beats = []
        open_events = {}
        for e in run["events"]:
            t, kind = float(e["t"]), e["kind"]
            if t > seconds:
                continue
            if kind == "crisis_warning":
                beats.append((t, f"annonce Résurgence {e['id']}"))
            elif kind == "crisis_start":
                beats.append((t, f"**Résurgence {e['id']}** ({e['detail']})"))
            elif kind == "crisis_end":
                beats.append((t, f"fin Résurgence {e['id']}"))
            elif kind == "event_start":
                open_events[e["id"]] = t
                beats.append((t, f"**{e['id']}**"))
            elif kind == "event_end":
                start = open_events.pop(e["id"], t)
                beats.append((t, f"fin {e['id']} ({'réussi' if e['detail'] == 'success' else 'échoué'}, {t - start:.0f} s)"))
            elif kind == "champion":
                beats.append((t, f"**Souverain** {e['id']} ({e['detail']})"))
            elif kind == "elite":
                beats.append((t, f"élite {e['id']} ({e['detail']})"))
        print("; ".join(f"{fmt_time(t)} {label}" for t, label in beats))
        majors = sorted(float(e["t"]) for e in run["events"] if e["kind"] in MAJOR and float(e["t"]) <= seconds)
        edges = [0.0] + majors + [float(seconds)]
        gaps = sorted(((b - a, a, b) for a, b in zip(edges, edges[1:])), reverse=True)[:3]
        levels = [float(e["t"]) for e in run["events"] if e["kind"] == "level"]
        elites = [float(e["t"]) for e in run["events"] if e["kind"] == "elite"]
        print(f"\nTemps forts (Résurgence, micro-événement, Souverain) : {sum(t <= 600 for t in majors)} en 10 min, "
              f"{len(majors)} en {seconds // 60} min. Élites apparues : {sum(t <= 600 for t in elites)} / {len(elites)}. "
              f"Niveaux : {sum(t <= 600 for t in levels)} / {len(levels)}. Plus longs creux : "
              + ", ".join(f"{fmt_time(a)}–{fmt_time(b)} ({g:.0f} s)" for g, a, b in gaps) + "\n")
    return per_run


def svg(runs, seconds, path, title):
    width, left, right, lane, top = 1500, 90, 20, 150, 60
    plot = width - left - right
    height = top + lane * len(runs) + 50
    x = lambda t: left + plot * t / seconds
    peak = max(float(r["near600"]) for run in runs for r in run["density"] if float(r["t"]) <= seconds) or 1
    parts = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" font-family="sans-serif" font-size="12">',
             f'<rect width="{width}" height="{height}" fill="#fbfaf7"/>',
             f'<text x="{left}" y="24" font-size="16" font-weight="bold">{title}</text>',
             f'<text x="{left}" y="44" fill="#555">Courbe : créatures à moins de 600 px (échelle 0–{peak:.0f}). Bandeau gris : annonce ; rouge : Résurgence ; vert : accalmie. '
             'Barres : micro-événements. ▲ élite ; ★ Souverain ; | niveau ; ■ coffre ouvert.</text>']
    for minute in range(0, seconds // 60 + 1):
        parts.append(f'<line x1="{x(minute * 60):.1f}" y1="{top}" x2="{x(minute * 60):.1f}" y2="{top + lane * len(runs)}" stroke="#ddd"/>')
        parts.append(f'<text x="{x(minute * 60):.1f}" y="{top + lane * len(runs) + 16}" text-anchor="middle" fill="#555">{minute}</text>')
    parts.append(f'<text x="{left + plot / 2}" y="{top + lane * len(runs) + 36}" text-anchor="middle" fill="#555">minutes</text>')
    for index, run in enumerate(runs):
        y0 = top + index * lane
        base = y0 + lane - 40
        parts.append(f'<text x="8" y="{y0 + 20}" font-weight="bold">graine {run["seed"]}</text>')
        state = {"warning": None, "active": None, "calm": None}
        spans = []
        for e in run["events"]:
            t, kind = float(e["t"]), e["kind"]
            if kind == "crisis_warning":
                state["warning"] = t
            elif kind == "crisis_start":
                spans.append(("#d5d8dc", state["warning"] or t, t)); state["active"] = t
            elif kind == "crisis_end":
                spans.append(("#f5b7b1", state["active"] or t, t))
            elif kind == "calm_start":
                state["calm"] = t
            elif kind == "calm_end" and state["calm"] is not None:
                spans.append(("#d4efdf", state["calm"], t))
        for color, a, b in spans:
            if a <= seconds:
                parts.append(f'<rect x="{x(a):.1f}" y="{y0 + 4}" width="{max(1.0, x(min(b, seconds)) - x(a)):.1f}" height="{lane - 44}" fill="{color}"/>')
        open_events = {}
        for e in run["events"]:
            t = float(e["t"])
            if e["kind"] == "event_start":
                open_events[e["id"]] = t
            elif e["kind"] == "event_end" and e["id"] in open_events:
                a = open_events.pop(e["id"])
                color = EVENT_COLORS.get(e["id"], "#555")
                parts.append(f'<rect x="{x(a):.1f}" y="{base + 6}" width="{x(t) - x(a):.1f}" height="9" fill="{color}"/>')
                parts.append(f'<text x="{x(a):.1f}" y="{base + 28}" fill="{color}" font-size="11">{e["id"]}{"" if e["detail"] == "success" else " ✗"}</text>')
        points = " ".join(f'{x(float(r["t"])):.1f},{base - (lane - 50) * float(r["near600"]) / peak:.1f}'
                          for r in run["density"] if float(r["t"]) <= seconds)
        parts.append(f'<polyline points="{points}" fill="none" stroke="#1b2631" stroke-width="1.2"/>')
        parts.append(f'<line x1="{left}" y1="{base}" x2="{left + plot}" y2="{base}" stroke="#888"/>')
        for e in run["events"]:
            t = float(e["t"])
            if t > seconds:
                continue
            if e["kind"] == "elite":
                parts.append(f'<text x="{x(t):.1f}" y="{y0 + 16}" text-anchor="middle" fill="#b7950b">▲</text>')
            elif e["kind"] == "champion":
                parts.append(f'<text x="{x(t):.1f}" y="{y0 + 32}" text-anchor="middle" fill="#ca6f1e" font-size="16">★</text>')
            elif e["kind"] == "level":
                parts.append(f'<line x1="{x(t):.1f}" y1="{base}" x2="{x(t):.1f}" y2="{base + 5}" stroke="#2874a6"/>')
            elif e["kind"] == "chest":
                parts.append(f'<rect x="{x(t) - 3:.1f}" y="{base - 3}" width="6" height="6" fill="#7e5109"/>')
    parts.append("</svg>")
    Path(path).write_text("\n".join(parts) + "\n")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("directory", type=Path)
    parser.add_argument("--bin", type=int, default=30)
    parser.add_argument("--seconds", type=int, default=900)
    parser.add_argument("--svg")
    parser.add_argument("--title", default="Chronologie mesurée")
    args = parser.parse_args()
    runs = load(args.directory)
    if not runs:
        raise SystemExit("Aucune graine lisible (mesure lancée sans --timeline ?)")
    report(runs, args.bin, args.seconds)
    contrasts(runs, args.seconds)
    if args.svg:
        svg(runs, args.seconds, args.svg, args.title)


if __name__ == "__main__":
    main()
