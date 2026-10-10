#!/usr/bin/env python3
"""
Économie du Campement (plan 36, lot M0) : ce que coûte le Campement plein, ce que rapporte une run, et combien de
runs il faut pour tout acheter.

Usage :
    python3 tools/camp_economy.py                       # prix seuls, par rayon
    python3 tools/camp_economy.py <dossier de mesure>…  # plus les gains mesurés et le nombre de runs

Les prix sont lus dans data/progression/camp_upgrades.json, comme le jeu. Les gains viennent de la colonne score des
CSV écrits par tools/measure_run.sh (seed-*/density-*.csv) : une run rapporte score ÷ 10 Vestiges (ScoreManager). Le
bot de mesure ne meurt pas (--mortal le remet à fond) : une run de durée d lui rapporte ce qu'il a gagné à la minute d,
c'est-à-dire le gain d'un joueur qui serait mort à cette minute.

Archétypes de run (plan 36 §3) : débutant (meurt entre 5 et 10 min), moyenne (15 à 25 min), bonne (30 min, l'Indicible).
Deux façons d'acheter sont simulées : au moins cher d'abord (ce qui est à portée), et les emplacements d'armes d'abord
(on économise pour eux, puis le moins cher).
"""
from __future__ import annotations

import csv
import json
import statistics
import sys
from pathlib import Path

CAMP_JSON = Path("data/progression/camp_upgrades.json")
MINUTES = (5, 10, 15, 20, 25, 30)
ARCHETYPES = {"débutant": (5, 10), "moyenne": (15, 25), "bonne": (30, 30)}
TARGET_RUNS = (30, 50)


def load_camp() -> dict:
    return json.loads(CAMP_JSON.read_text())


def price_table(camp: dict) -> None:
    by_branch: dict[str, int] = {}
    print("Rang par rang (prix cumulés) :")
    for upgrade in camp["upgrades"]:
        prices = upgrade["prices"]
        total = sum(prices)
        by_branch[upgrade["branch"]] = by_branch.get(upgrade["branch"], 0) + total
        print(f"  {upgrade['id']:<20} {len(prices)} rang(s) {' / '.join(str(p) for p in prices):<34} = {total:>6}")
    print("Par rayon :")
    for branch, total in by_branch.items():
        print(f"  {branch:<12} {total:>6}")
    print(f"Campement plein : {sum(by_branch.values())} Vestiges")


def measured_vestiges(folders: list[Path]) -> dict[int, list[int]]:
    """Vestiges gagnés à chaque minute repère, une valeur par graine mesurée assez longtemps."""
    gains: dict[int, list[int]] = {minute: [] for minute in MINUTES}
    for folder in folders:
        for path in sorted(folder.glob("seed-*/density-*.csv")):
            with path.open() as handle:
                rows = list(csv.DictReader(handle))
            if not rows or "score" not in rows[0]:
                print(f"{path} : pas de colonne score (mesure antérieure au plan 36)", file=sys.stderr)
                continue
            by_second = {int(float(row["t"])): int(row["score"]) for row in rows}
            for minute in MINUTES:
                # La dernière ligne d'une mesure de d secondes porte t = d − 1.
                score = by_second.get(minute * 60, by_second.get(minute * 60 - 1))
                if score is not None:
                    gains[minute].append(score // 10)
    return gains


def archetype_gain(gains: dict[int, list[int]], low: int, high: int) -> float | None:
    values = [statistics.mean(gains[m]) for m in MINUTES if low <= m <= high and gains[m]]
    return statistics.mean(values) if values else None


def runs_to_buy(camp: dict, per_run: float, weapons_first: bool) -> tuple[int, list[tuple[int, str]]]:
    """Rend le nombre de runs pour tout acheter et la run où s'ouvre chaque emplacement d'arme."""
    ranks = [(price, upgrade["id"], rank + 1) for upgrade in camp["upgrades"] for rank, price in enumerate(upgrade["prices"])]
    # Un rang n'est achetable qu'après le précédent : à prix égal, l'ordre des rangs reste celui du fichier.
    ranks.sort(key=lambda entry: (not (weapons_first and entry[1] == "camp_weapon_slots"), entry[0], entry[2]))
    owned: dict[str, int] = {}
    wallet, runs, milestones = 0.0, 0, []
    pending = list(ranks)
    while pending:
        runs += 1
        wallet += per_run
        bought = True
        while bought:
            bought = False
            for entry in pending:
                price, upgrade_id, rank = entry
                saving = weapons_first and upgrade_id != "camp_weapon_slots" and any(e[1] == "camp_weapon_slots" for e in pending)
                if owned.get(upgrade_id, 0) == rank - 1 and price <= wallet and not saving:
                    wallet -= price
                    owned[upgrade_id] = rank
                    pending.remove(entry)
                    if upgrade_id == "camp_weapon_slots":
                        milestones.append((runs, f"{camp['base_weapon_slots'] + rank}e arme"))
                    bought = True
                    break
        if runs > 10_000:
            break
    return runs, milestones


def main() -> int:
    camp = load_camp()
    price_table(camp)
    folders = [Path(arg) for arg in sys.argv[1:]]
    if not folders:
        return 0
    gains = measured_vestiges(folders)
    print("\nVestiges gagnés par le bot à la minute (moyenne, min–max, graines) :")
    for minute in MINUTES:
        values = gains[minute]
        if values:
            print(f"  {minute:>2} min : {statistics.mean(values):>6.0f}  ({min(values)}–{max(values)}, {len(values)})")
    total = sum(sum(upgrade["prices"]) for upgrade in camp["upgrades"])
    print(f"\nRuns pour le Campement plein ({total} Vestiges ; cible : {TARGET_RUNS[0]} à {TARGET_RUNS[1]} runs moyennes) :")
    for name, (low, high) in ARCHETYPES.items():
        per_run = archetype_gain(gains, low, high)
        if per_run is None:
            continue
        for strategy, weapons_first in (("moins cher d'abord", False), ("armes d'abord", True)):
            runs, milestones = runs_to_buy(camp, per_run, weapons_first)
            slots = ", ".join(f"{label} après la run {run}" for run, label in milestones)
            print(f"  {name:<9} {per_run:>6.0f} par run, {strategy:<18} → {runs:>3} runs ; {slots}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
