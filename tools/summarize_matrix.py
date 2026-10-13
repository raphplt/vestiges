#!/usr/bin/env python3
"""Synthèse d'une matrice de mesures (plan 42 M1/M2), une ligne par configuration de tools/measure_matrix.sh.

Pour chaque configuration (dossier <matrice>/<nom>/seed-*), en moyenne sur les graines avec l'étendue min–max :
niveau à 5, 10, 20 et 29 min, éliminations, coups fatals (bot --mortal, qui se relève), PV perdus par minute avant et
après 15 min, part du temps passé Effacé ou dans le Néant, plus long creux sans décision, arme qui a fait le plus de
dégâts. Sortie en Markdown, regroupée par bloc (préfixe du nom avant le premier tiret).

Usage : python3 tools/summarize_matrix.py <dossier de matrice> [--json sortie.json]
"""
import argparse
import csv
import glob
import json
import os
import statistics
from collections import defaultdict

MINUTES = (5, 10, 20, 29)


def read(path):
    with open(path) as handle:
        return list(csv.DictReader(handle))


def run_metrics(directory, seed):
    density = {int(r["t"]): r for r in read(f"{directory}/density-{seed}.csv")}
    pace = read(f"{directory}/pace-{seed}.csv")
    last = max(density)
    level = {m: int(density[min(m * 60, last)]["level"]) for m in MINUTES}
    trace = read(f"{directory}/trace-{seed}.csv")
    lost_early = sum(float(r["hp_lost"]) for r in trace if int(r["minute"]) < 15) / 15
    lost_late = sum(float(r["hp_lost"]) for r in trace if int(r["minute"]) >= 15) / max(1, (last + 1) / 60 - 15)
    late_phase = sum(1 for r in pace if int(r["phase"]) >= 3) / max(1, len(pace))
    damage = defaultdict(float)
    for r in read(f"{directory}/damage-sources-{seed}.csv"):
        damage[r["weapon"]] += float(r["damage"])
    total = sum(damage.values())
    top = max(damage.items(), key=lambda item: item[1]) if damage else ("-", 0.0)
    return {
        "seconds": last + 1,
        "level": level,
        "kills": int(density[last]["killed"]),
        "fatal": int(pace[-1]["deaths"]) if pace else 0,
        "lost_early": lost_early,
        "lost_late": lost_late,
        "late_phase": late_phase,
        "gap": max(float(r["since_decision_s"]) for r in pace) if pace else 0.0,
        "top_weapon": top[0],
        "top_share": top[1] / total if total else 0.0,
    }


def configuration(folder):
    runs = []
    for density in sorted(glob.glob(f"{folder}/seed-*/density-*.csv")):
        directory = os.path.dirname(density)
        seed = os.path.basename(density)[len("density-"):-len(".csv")]
        if os.path.exists(f"{directory}/pace-{seed}.csv"):
            runs.append(run_metrics(directory, seed))
    return runs


def span(values, digits=0):
    mean = statistics.mean(values)
    text = f"{mean:,.{digits}f}".replace(",", " ").replace(".", ",")
    if len(values) > 1 and min(values) != max(values):
        low = f"{min(values):,.{digits}f}".replace(",", " ").replace(".", ",")
        high = f"{max(values):,.{digits}f}".replace(",", " ").replace(".", ",")
        return f"{text} ({low}–{high})"
    return text


def summarize(runs):
    weapons = defaultdict(int)
    for run in runs:
        weapons[run["top_weapon"]] += 1
    return {
        "runs": len(runs),
        **{f"level_{m}": statistics.mean(run["level"][m] for run in runs) for m in MINUTES},
        "kills": statistics.mean(run["kills"] for run in runs),
        "fatal": statistics.mean(run["fatal"] for run in runs),
        "lost_early": statistics.mean(run["lost_early"] for run in runs),
        "lost_late": statistics.mean(run["lost_late"] for run in runs),
        "late_phase": statistics.mean(run["late_phase"] for run in runs),
        "gap": max(run["gap"] for run in runs),
        "top_weapon": max(weapons.items(), key=lambda item: item[1])[0],
        "top_share": statistics.mean(run["top_share"] for run in runs),
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("matrix")
    parser.add_argument("--json", help="écrit aussi les moyennes par configuration dans ce fichier")
    args = parser.parse_args()
    blocks = defaultdict(list)
    results = {}
    for folder in sorted(glob.glob(f"{args.matrix}/*/")):
        name = os.path.basename(folder.rstrip("/"))
        runs = configuration(folder)
        if not runs:
            continue
        results[name] = summarize(runs)
        blocks[name.split("-")[0]].append((name, runs))
    for block, configs in blocks.items():
        print(f"## Bloc {block}\n")
        print("| Configuration | Niv. 5 min | Niv. 10 min | Niv. 20 min | Niv. 29 min | Éliminations | Coups fatals "
              "| PV perdus/min avant 15 min | après 15 min | Temps Effacé ou Néant | Plus long creux (s) | Arme principale |")
        print("|---|---|---|---|---|---|---|---|---|---|---|---|")
        for name, runs in configs:
            levels = " | ".join(span([run["level"][m] for run in runs]) for m in MINUTES)
            summary = results[name]
            print(f"| {name.removeprefix(block + '-')} | {levels} | {span([r['kills'] for r in runs])} | {span([r['fatal'] for r in runs])} "
                  f"| {span([r['lost_early'] for r in runs])} | {span([r['lost_late'] for r in runs])} "
                  f"| {100 * summary['late_phase']:.0f} % | {summary['gap']:.0f} "
                  f"| {summary['top_weapon']} ({100 * summary['top_share']:.0f} %) |")
        print()
    if args.json:
        with open(args.json, "w") as handle:
            json.dump(results, handle, ensure_ascii=False, indent=1)


if __name__ == "__main__":
    main()
