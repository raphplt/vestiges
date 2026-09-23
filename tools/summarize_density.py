"""Synthèse des CSV de densité produits par RunObservation : médianes et trous par minute, toutes seeds confondues."""
import csv
import glob
import statistics
import sys


def main(directory: str) -> None:
    buckets: dict[int, list[dict]] = {}
    for path in sorted(glob.glob(f"{directory}/density-*.csv")):
        for row in csv.DictReader(open(path, encoding="utf-8")):
            row["_seed"] = path
            buckets.setdefault(int(row["t"]) // 60, []).append(row)
    print("| Minute | Visibles médiane | p25–p75 | < 5 visibles | À 600 px médiane | Dégâts reçus/min (sans esquive) |")
    print("|---|---:|---|---:|---:|---:|")
    for minute, rows in sorted(buckets.items()):
        visible = sorted(int(r["visible"]) for r in rows)
        near = [int(r["near600"]) for r in rows]
        sparse = 100 * sum(v < 5 for v in visible) / len(visible)
        damage = _damage_per_minute(rows)
        print(f"| {minute}–{minute + 1} | {statistics.median(visible):.1f} | "
              f"{visible[len(visible) // 4]}–{visible[3 * len(visible) // 4]} | {sparse:.0f} % | {statistics.median(near):.1f} | {damage} |")


def _damage_per_minute(rows: list[dict]) -> str:
    """Moyenne entre seeds de l'écart de dégâts cumulés sur la minute ; absent des anciens CSV."""
    if "hit_damage" not in rows[0]:
        return "–"
    by_seed: dict[str, list[float]] = {}
    for row in rows:
        by_seed.setdefault(row["_seed"], []).append(float(row["hit_damage"]))
    deltas = [values[-1] - values[0] for values in by_seed.values() if len(values) > 1]
    return f"{statistics.mean(deltas):.0f}" if deltas else "–"


if __name__ == "__main__":
    main(sys.argv[1])
