"""
D'où viennent les dégâts reçus (plan 20 §5.4) : part de chaque créature dans la pression subie, rapportée
à sa présence autour du joueur.

Usage :
    python3 tools/damage_sources.py <dossier de mesure>...   # summary.txt de tools/measure_run.sh

Lit dans chaque ligne RESULT :
- hit_by : dégâts dirigés vers le bot (invincible, il n'esquive rien et n'a pas d'invulnérabilité) ;
- hit_gated : les mêmes, en ne gardant que les coups qui passeraient l'invulnérabilité après un coup ;
- near_by : secondes × créatures à moins de 600 px (exposition) ;
- kills_by : morts par espèce.
Le bot rend une pression « à joueur immobile » : il ne juge pas l'esquive, seulement le poids relatif des sources.
"""
from __future__ import annotations

import json
import sys
from collections import defaultdict
from pathlib import Path

FIELDS = ("hit_by", "hit_gated", "near_by", "kills_by")


def roles() -> dict[str, str]:
    """Rôle d'attaque lu dans les fiches : zone (omen_strike), tir (type ranged), boss, sinon mêlée."""
    found = {"void": "Néant"}
    for path in Path("data/enemies").glob("[a-z]*.json"):
        data = json.loads(path.read_text())
        if data.get("tier") == "boss":
            found[data["id"]] = "boss"
        elif "omen_strike" in data.get("abilities", {}):
            found[data["id"]] = "zone (Présage)"
        elif data.get("type") == "ranged":
            found[data["id"]] = "tir"
    return found


def parse(line: str) -> dict[str, dict[str, float]]:
    values: dict[str, dict[str, float]] = {}
    for token in line.split():
        key, _, raw = token.partition("=")
        if key not in FIELDS:
            continue
        values[key] = {}
        for entry in filter(None, raw.split(",")):
            name, _, amount = entry.rpartition(":")
            values[key][name] = float(amount)
    return values


def main() -> None:
    totals: dict[str, dict[str, float]] = {field: defaultdict(float) for field in FIELDS}
    runs = 0
    for folder in map(Path, sys.argv[1:]):
        for line in (folder / "summary.txt").read_text().splitlines():
            runs += 1
            for field, entries in parse(line).items():
                for name, amount in entries.items():
                    totals[field][name] += amount
    if not runs:
        sys.exit("aucune ligne RESULT")

    role_of = roles()
    sums = {field: sum(totals[field].values()) or 1 for field in FIELDS}
    names = sorted(set().union(*totals.values()), key=lambda n: -totals["hit_gated"].get(n, 0))
    print(f"{runs} run(s). Parts en % du total ; « rapport » = part des dégâts filtrés / part de l'exposition.\n")
    print("| Créature | Rôle | Dégâts bruts | Dégâts filtrés | Exposition | Morts | Rapport |")
    print("|---|---|---|---|---|---|---|")
    categories: dict[str, dict[str, float]] = defaultdict(lambda: defaultdict(float))
    for name in names:
        role = role_of.get(name, "mêlée")
        shares = {field: 100 * totals[field].get(name, 0) / sums[field] for field in FIELDS}
        for field, share in shares.items():
            categories[role][field] += share
        ratio = shares["hit_gated"] / shares["near_by"] if shares["near_by"] else float("nan")
        print(f"| {name} | {role} | {shares['hit_by']:.1f} | {shares['hit_gated']:.1f} | {shares['near_by']:.1f} "
              f"| {shares['kills_by']:.1f} | {ratio:.2f} |")

    print("\n| Rôle | Dégâts bruts | Dégâts filtrés | Exposition | Morts | Rapport |")
    print("|---|---|---|---|---|---|")
    for role, shares in sorted(categories.items(), key=lambda item: -item[1]["hit_gated"]):
        ratio = shares["hit_gated"] / shares["near_by"] if shares["near_by"] else float("nan")
        print(f"| {role} | {shares['hit_by']:.1f} | {shares['hit_gated']:.1f} | {shares['near_by']:.1f} "
              f"| {shares['kills_by']:.1f} | {ratio:.2f} |")


if __name__ == "__main__":
    main()
