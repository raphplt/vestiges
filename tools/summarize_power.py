#!/usr/bin/env python3
"""Puissance du joueur face aux ennemis, par tranche (3 min sur 15 par défaut ; plan 23 R0 et R5, plan 21 H).

Lit les CSV de densité écrits par tools/measure_run.sh (un dossier seed-*/density-*.csv par seed) et donne, en
moyenne sur les seeds : éliminations, dégâts reçus par le bot invincible (chaque coup compté, sans invulnérabilité),
PV moyen d'une créature apparue, dégâts infligés par seconde, temps pour tuer (PV moyen / dégâts par seconde),
rapport PV apparus / dégâts infligés, niveau en fin de tranche et Essence gagnée.

Usage : python3 tools/summarize_power.py [--minutes 25 --step 5] <dossier de measure_run> [autre dossier…]
Le temps pour tuer et les dégâts reçus donnent aussi leur étendue sur les seeds (min–max).
"""
import argparse
import csv
import glob
import statistics
import sys

def number(value, digits=0):
    return f"{value:,.{digits}f}".replace(",", " ").replace(".", ",")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--minutes", type=int, default=15)
    parser.add_argument("--step", type=int, default=3)
    parser.add_argument("folders", nargs="+")
    args = parser.parse_args()
    folders = args.folders
    edges = list(range(0, args.minutes * 60 + 1, args.step * 60))
    files = []
    for folder in folders:
        files += sorted(glob.glob(f"{folder}/seed-*/density-*.csv"))
    if not files:
        sys.exit("aucun CSV de densité trouvé")
    tranches = {}
    for path in files:
        rows = list(csv.DictReader(open(path)))
        by_second = {int(row["t"]): row for row in rows}
        last = max(by_second)

        def at(second):
            second = min(second, last)
            while second not in by_second and second > 0:
                second -= 1
            return by_second.get(second)

        for start, end in zip(edges, edges[1:]):
            if end > last + 1:
                break
            begin = at(start) if start > 0 else {key: "0" for key in rows[0]}
            finish = at(end)

            def delta(key):
                return float(finish[key]) - float(begin[key])

            spawned, hp, dealt = delta("spawned"), delta("spawned_hp"), delta("damage_dealt")
            tranches.setdefault((start, end), []).append({
                "kills": delta("killed"),
                "hit": delta("hit_damage"),
                "hp_per": hp / spawned if spawned else 0.0,
                "dps": dealt / (end - start),
                "ttk": (hp / spawned) / (dealt / (end - start)) if spawned and dealt else 0.0,
                "ratio": hp / dealt if dealt else 0.0,
                "level": float(finish["level"]),
                "essence": delta("essence_gained") if "essence_gained" in finish else float("nan"),
            })
    print(f"{len(files)} runs")
    print("| Tranche | Éliminations | Dégâts reçus | PV moyen d'une créature | Dégâts infligés /s | Temps pour tuer (s) "
          "| PV apparus / dégâts | Niveau en fin de tranche | Essence gagnée |")
    print("|---|---|---|---|---|---|---|---|---|")
    span = lambda runs, key, digits: f"{number(min(run[key] for run in runs), digits)}–{number(max(run[key] for run in runs), digits)}"
    for (start, end), runs in tranches.items():
        mean = lambda key: statistics.mean(run[key] for run in runs)
        print(f"| {start // 60}–{end // 60} min | {number(mean('kills'))} | {number(mean('hit'))} ({span(runs, 'hit', 0)}) | {number(mean('hp_per'), 1)} "
              f"| {number(mean('dps'), 1)} | {number(mean('ttk'), 2)} ({span(runs, 'ttk', 2)}) | {number(mean('ratio'), 2)} | {number(mean('level'), 1)} "
              f"| {number(mean('essence'))} |")


if __name__ == "__main__":
    main()
