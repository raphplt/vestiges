#!/usr/bin/env python3
"""Rythme d'une configuration mesurée, minute par minute (plan 42 M0a).

Lit, pour chaque seed d'un dossier de tools/measure_run.sh, le relevé de densité (density-*.csv) et ceux de la sonde
de rythme (pace-*.csv, kills-*.csv, damage-sources-*.csv, decisions-*.csv). Donne, en moyenne sur les seeds et pour
chaque minute :
- le niveau en fin de minute et l'éloignement du départ (le plus grand de la minute, à vol d'oiseau) ;
- le temps pour tuer médian, du premier coup du joueur à la mort, d'une créature ordinaire et d'un élite ;
- le rapport dégâts infligés / PV des créatures apparues (sous 1, la marée gagne sur le joueur) ;
- les morts comptées (--mortal) et les PV les plus bas, en part des PV max ;
- la part du temps dans chaque phase d'oubli sous le joueur (Ancré, Fragile, Effiloché, Effacé, Néant) ;
- le plus long creux sans décision (choix, coffre, lieu, monument, micro-événement, Résurgence, élite à portée).
Puis la part des dégâts par arme et par nature sur toute la run.

Usage : python3 tools/summarize_pace.py <dossier de measure_run> [autre dossier…]
Un dossier = une configuration (personnage, profil, build) ; chacun a son tableau.
"""
import argparse
import csv
import glob
import os
import statistics
import sys
from collections import defaultdict

PHASES = ["Ancré", "Fragile", "Effiloché", "Effacé", "Néant"]
KINDS = {"DirectWeapon": "coup", "SecondaryWeapon": "secondaire", "DamageOverTime": "sur la durée", "Passive": "objets",
         "Unknown": "autre", "EnemyExplosion": "explosion"}


def number(value, digits=0):
    if value is None:
        return "–"
    return f"{value:,.{digits}f}".replace(",", " ").replace(".", ",")


def rows(path):
    with open(path) as handle:
        return list(csv.DictReader(handle))


def seed_files(folder):
    for density in sorted(glob.glob(f"{folder}/seed-*/density-*.csv")):
        directory = os.path.dirname(density)
        seed = os.path.basename(density)[len("density-"):-len(".csv")]
        pace = f"{directory}/pace-{seed}.csv"
        if os.path.exists(pace):
            yield seed, directory


def minute_table(folder):
    per_minute = defaultdict(list)
    damage = defaultdict(float)
    runs = 0
    for seed, directory in seed_files(folder):
        runs += 1
        density = {int(row["t"]): row for row in rows(f"{directory}/density-{seed}.csv")}
        pace = rows(f"{directory}/pace-{seed}.csv")
        kills = rows(f"{directory}/kills-{seed}.csv")
        for row in rows(f"{directory}/damage-sources-{seed}.csv"):
            damage[(row["weapon"], row["kind"])] += float(row["damage"])
        last_second = int(pace[-1]["t"]) if pace else 0

        def at(second):
            second = min(second, last_second)
            while second not in density and second > 0:
                second -= 1
            return density.get(second)

        # La run s'arrête une image avant sa durée : une minute couverte aux trois quarts compte.
        for minute in range((last_second + 15) // 60):
            start, end = minute * 60, (minute + 1) * 60
            seconds = [row for row in pace if start < int(row["t"]) <= end]
            if not seconds:
                continue
            last = seconds[-1]
            after = at(end)
            if after is None:
                continue
            before = (at(start) if start > 0 else None) or {key: "0" for key in after}
            spawned_hp = float(after["spawned_hp"]) - float(before["spawned_hp"])
            dealt = float(after["damage_dealt"]) - float(before["damage_dealt"])
            deaths_before = int(next((row["deaths"] for row in reversed(pace) if int(row["t"]) <= start), 0))
            ordinary = [float(k["ttk_s"]) for k in kills if start < float(k["t"]) <= end and k["elite"] == "0"]
            elite = [float(k["ttk_s"]) for k in kills if start < float(k["t"]) <= end and k["elite"] == "1"]
            phases = [0] * len(PHASES)
            for row in seconds:
                phases[int(row["phase"])] += 1
            per_minute[minute].append({
                "level": float(after["level"]),
                "distance": max(float(row["from_start_m"]) for row in seconds),
                "ttk": statistics.median(ordinary) if ordinary else None,
                "ttk_elite": statistics.median(elite) if elite else None,
                "ratio": dealt / spawned_hp if spawned_hp else None,
                "deaths": int(last["deaths"]) - deaths_before,
                "hp_low": min(float(row["hp"]) / float(row["max_hp"]) for row in seconds),
                "phases": [count / len(seconds) for count in phases],
                "gap": max(float(row["since_decision_s"]) for row in seconds),
            })
    return runs, per_minute, damage


def mean(values):
    values = [value for value in values if value is not None]
    return statistics.mean(values) if values else None


def print_folder(folder):
    runs, per_minute, damage = minute_table(folder)
    if not runs:
        print(f"{folder} : aucun relevé de rythme (pace-*.csv)", file=sys.stderr)
        return False
    print(f"## {os.path.basename(os.path.normpath(folder))} — {runs} runs\n")
    print("| Minute | Niveau | Éloignement (m) | Temps pour tuer (s) | Élite (s) | Dégâts / PV apparus | Morts | PV au plus bas "
          "| Ancré / Fragile / Effiloché / Effacé / Néant | Plus long creux (s) |")
    print("|---|---|---|---|---|---|---|---|---|---|")
    for minute in sorted(per_minute):
        samples = per_minute[minute]
        field = lambda key: mean(sample[key] for sample in samples)
        phases = " / ".join(f"{100 * mean(sample['phases'][i] for sample in samples):.0f}" for i in range(len(PHASES)))
        print(f"| {minute}–{minute + 1} | {number(field('level'), 1)} | {number(field('distance'))} | {number(field('ttk'), 2)} "
              f"| {number(field('ttk_elite'), 1)} | {number(field('ratio'), 2)} | {number(field('deaths'), 1)} "
              f"| {number(100 * field('hp_low'))} % | {phases} | {number(field('gap'))} |")
    total = sum(damage.values())
    if total > 0:
        print("\nDégâts infligés par source, toute la run :\n")
        print("| Arme | Nature | Part |")
        print("|---|---|---|")
        for (weapon, kind), amount in sorted(damage.items(), key=lambda item: -item[1]):
            if amount / total >= 0.005:
                print(f"| {weapon} | {KINDS.get(kind, kind)} | {number(100 * amount / total, 1)} % |")
    print()
    return True


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("folders", nargs="+")
    args = parser.parse_args()
    found = [print_folder(folder) for folder in args.folders]
    if not any(found):
        sys.exit("aucun relevé de rythme trouvé")


if __name__ == "__main__":
    main()
