#!/usr/bin/env python3
"""Survie d'un vrai joueur contre celle des profils du bot de mesure (plan 42 M0e).

Les parties jouées dans un build d'édition laissent une trace (PlayTraceRecorder) dans le profil Godot :
traces/<date>-<personnage>.csv et .txt, sous user:// ou user://dev/. Le bot écrit la même trace à chaque mesure
(seed-*/trace-<seed>.csv et .txt). Ce script les met côte à côte : PV perdus par minute, coups par minute, coups fatals et
dashs par minute, et les sources qui pèsent le plus. Un coup fatal compte même si le joueur s'en relève (ticket de
consigne ; le bot de mesure se relève toujours).

Usage : python3 tools/compare_play_traces.py [--player DOSSIER] [--character traqueur] <dossier de measure_run> […]
Sans --player, les traces sont cherchées dans le profil Godot de Vestiges (~/.local/share/godot/app_userdata/Vestiges).
"""
import argparse
import csv
import glob
import os
import statistics
from collections import defaultdict

USER_DIR = os.path.expanduser("~/.local/share/godot/app_userdata/Vestiges")


def number(value, digits=0):
    if value is None:
        return "–"
    return f"{value:,.{digits}f}".replace(",", " ").replace(".", ",")


def summary(path):
    with open(path) as handle:
        return dict(item.split("=", 1) for item in handle.read().split())


def load(csv_paths, character):
    """Runs : (résumé, PV perdus et coups par minute, PV perdus par source)."""
    runs = []
    for path in csv_paths:
        info = summary(path[:-4] + ".txt")
        if character and info.get("character") != character:
            continue
        per_minute = defaultdict(lambda: [0.0, 0])
        by_source = defaultdict(float)
        with open(path) as handle:
            for row in csv.DictReader(handle):
                per_minute[int(row["minute"])][0] += float(row["hp_lost"])
                per_minute[int(row["minute"])][1] += int(row["hits"])
                by_source[row["source"]] += float(row["hp_lost"])
        runs.append((info, per_minute, by_source))
    return runs


def describe(label, runs, minutes):
    if not runs:
        print(f"## {label} : aucune trace\n")
        return
    seconds = sum(float(info["seconds"]) for info, _, _ in runs)
    fatal = sum(int(info["fatal_hits"]) for info, _, _ in runs)
    dashes = sum(int(info["dashes"]) for info, _, _ in runs)
    print(f"## {label} — {len(runs)} runs, {number(seconds / 60, 1)} min de jeu\n")
    print(f"Coups fatals par 10 min : {number(10 * 60 * fatal / seconds, 1)} · dashs par minute : {number(60 * dashes / seconds, 1)}\n")
    print("| Minute | PV perdus | Coups encaissés | Runs encore en cours |")
    print("|---|---|---|---|")
    for minute in range(minutes):
        alive = [(per_minute.get(minute, [0.0, 0])) for info, per_minute, _ in runs if float(info["seconds"]) >= (minute + 1) * 60]
        if not alive:
            break
        print(f"| {minute}–{minute + 1} | {number(statistics.mean(v[0] for v in alive))} | {number(statistics.mean(v[1] for v in alive), 1)} | {len(alive)} |")
    total = defaultdict(float)
    for _, _, by_source in runs:
        for source, hp in by_source.items():
            total[source] += hp
    whole = sum(total.values())
    if whole > 0:
        top = sorted(total.items(), key=lambda item: -item[1])[:6]
        print("\nSources : " + ", ".join(f"{source} {number(100 * hp / whole)} %" for source, hp in top))
    print()


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--player", help="dossier des traces du joueur (défaut : profil Godot de Vestiges)")
    parser.add_argument("--character", help="ne garder que ce personnage")
    parser.add_argument("--minutes", type=int, default=30)
    parser.add_argument("measures", nargs="*", help="dossiers de tools/measure_run.sh")
    args = parser.parse_args()
    if args.player:
        player_paths = glob.glob(f"{args.player}/*.csv")
    else:
        player_paths = glob.glob(f"{USER_DIR}/traces/*.csv") + glob.glob(f"{USER_DIR}/dev/traces/*.csv")
    describe("Joueur", load(sorted(player_paths), args.character), args.minutes)
    for folder in args.measures:
        bot_paths = sorted(glob.glob(f"{folder}/seed-*/trace-*.csv"))
        describe(f"Bot — {os.path.basename(os.path.normpath(folder))}", load(bot_paths, args.character), args.minutes)


if __name__ == "__main__":
    main()
