#!/usr/bin/env python3
"""
Audit des données du bestiaire (plan 07, lot A, étape 4 : « présence en JSON ne prouve pas apparition normale »).

Pour chaque fiche de data/enemies : rôle déclaré (type, rang, comportement, capacités), vitesse et gabarit, puis
tous les chemins d'apparition trouvés dans les données et le code :
  - pools d'exploration et de crise de chaque biome (exploration_enemy_pool / resurgence_enemy_pool),
    avec la part de la créature dans le pool (les doublons pondèrent) ;
  - gardes des points d'intérêt (data/pois) ;
  - micro-événements (data/events) ;
  - identifiant cité en dur dans scripts/ (hors score et effets).
Une créature sans aucun chemin est signalée « inatteignable ».

Usage : python3 tools/audit_bestiary.py            # tableau Markdown sur la sortie standard
"""
from __future__ import annotations

import json
import re
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
# Fichiers où un identifiant cité ne crée pas la créature (barème, sons, succès, bancs).
IGNORED_SCRIPTS = {"ScoreManager.cs", "AudioManager.cs", "SteamAchievements.cs", "Enemy.cs"}


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def enemies() -> dict[str, dict]:
    result = {}
    for path in sorted((ROOT / "data/enemies").glob("*.json")):
        if path.name.startswith("_"):
            continue
        data = load(path)
        result[data.get("id", path.stem)] = data
    return result


def biome_pools() -> dict[str, tuple[Counter, Counter]]:
    pools = {}
    for path in sorted((ROOT / "data/biomes").glob("*.json")):
        if path.name.startswith("_"):
            continue
        data = load(path)
        pools[data.get("name", data.get("id", path.stem))] = (Counter(data.get("exploration_enemy_pool", [])),
                                                              Counter(data.get("resurgence_enemy_pool", [])))
    return pools


def poi_guards() -> Counter:
    data = load(ROOT / "data/pois/pois.json")
    entries = data.get("pois", data) if isinstance(data, dict) else data
    if isinstance(entries, dict):
        entries = list(entries.values())
    guards = Counter()
    for poi in entries:
        for enemy in poi.get("enemy_guards", []):
            guards[enemy] += 1
    return guards


def event_mentions(ids: set[str]) -> Counter:
    mentions = Counter()
    for path in (ROOT / "data/events").glob("*.json"):
        text = path.read_text(encoding="utf-8")
        for enemy in ids:
            mentions[enemy] += len(re.findall(rf'"{re.escape(enemy)}"', text))
    return mentions


def script_mentions(ids: set[str]) -> dict[str, list[str]]:
    found: dict[str, list[str]] = {enemy: [] for enemy in ids}
    for path in (ROOT / "scripts").rglob("*.cs"):
        if path.name in IGNORED_SCRIPTS:
            continue
        text = path.read_text(encoding="utf-8")
        for enemy in ids:
            if f'"{enemy}"' in text:
                found[enemy].append(path.name)
    return found


def share(counter: Counter, enemy: str) -> str:
    total = sum(counter.values())
    return f"{round(100 * counter[enemy] / total)} %" if total and counter[enemy] else ""


def main() -> None:
    catalog = enemies()
    ids = set(catalog)
    pools = biome_pools()
    guards = poi_guards()
    events = event_mentions(ids)
    scripts = script_mentions(ids)

    print("| Créature | Type · rang · comportement | Vit. | PV | Capacités | Exploration (part du pool) | Crise et fin de run | Autres chemins |")
    print("|---|---|---|---|---|---|---|---|")
    unreachable = []
    for enemy_id, data in sorted(catalog.items()):
        stats = data.get("stats", {})
        abilities = ", ".join(sorted(data.get("abilities", {}).keys())) or "—"
        exploration = ", ".join(f"{biome} {share(day, enemy_id)}" for biome, (day, _) in pools.items() if day[enemy_id])
        crisis = ", ".join(f"{biome} {share(night, enemy_id)}" for biome, (_, night) in pools.items() if night[enemy_id])
        others = []
        if guards[enemy_id]:
            others.append(f"garde de {guards[enemy_id]} point(s) d'intérêt")
        if events[enemy_id]:
            others.append(f"micro-événements ({events[enemy_id]})")
        if scripts[enemy_id]:
            others.append("code : " + ", ".join(sorted(set(scripts[enemy_id]))))
        if not (exploration or crisis or others):
            unreachable.append(enemy_id)
        role = f"{data.get('type', '?')} · {data.get('tier', 'normal')} · {data.get('behavior', 'default')}"
        print(f"| {data.get('name', enemy_id)} (`{enemy_id}`) | {role} | {stats.get('speed', '?')} | {stats.get('hp', '?')} "
              f"| {abilities} | {exploration or '—'} | {crisis or '—'} | {'; '.join(others) or '—'} |")
    print()
    print("Inatteignables en jeu normal : " + (", ".join(f"`{e}`" for e in unreachable) if unreachable else "aucune") + ".")


if __name__ == "__main__":
    main()
