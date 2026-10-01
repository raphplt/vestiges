#!/usr/bin/env python3
"""Résume une ou plusieurs sorties measure_run --measure-projectiles en JSON."""

import argparse
import csv
import json
import math
import re
import statistics
from itertools import groupby
from pathlib import Path


def summarize(directory):
    results = []
    files = sorted(directory.glob("seed-*/projectiles-*.csv"))
    if not files:
        raise ValueError(f"Aucune mesure de projectiles dans {directory}")
    for path in files:
        seed = path.stem.removeprefix("projectiles-")
        with path.open() as handle:
            rows = list(csv.DictReader(handle))
        if not rows:
            raise ValueError(f"Mesure vide : {path}")
        with path.with_name(f"density-{seed}.csv").open() as handle:
            density = list(csv.DictReader(handle))
        log = directory.joinpath(f"seed-{seed}.log").read_text()
        result = next((line for line in log.splitlines() if "[RunObservation] RESULT " in line), None)
        if result is None:
            raise ValueError(f"Run incomplète : {seed}")
        counters = dict(re.findall(r"(\w+)=([^\s|]+)", result))
        phases = []
        for phase, group in groupby(rows, key=lambda row: row["phase"]):
            samples = list(group)
            visible = sorted(int(row["visible"]) for row in samples)
            total = sum(visible)
            phases.append({
                "phase": phase,
                "samples": len(samples),
                "from_s": float(samples[0]["t"]),
                "to_s": float(samples[-1]["t"]),
                "visible_mean": round(statistics.mean(visible), 3),
                "visible_p95": visible[math.ceil(len(visible) * 0.95) - 1],
                "visible_max": max(visible),
                "time_with_shots_pct": round(100 * sum(v > 0 for v in visible) / len(visible), 2),
                "visible_age_over_2_pct": round(100 * sum(int(row["visible_age_over_2"]) for row in samples) / total, 2) if total else 0,
                "visible_shooters_mean": round(statistics.mean(int(row["visible_shooters"]) for row in samples), 3),
                "visible_source_pct": {
                    source: round(100 * sum(int(row[source]) for row in samples) / total, 2) if total else 0
                    for source in ("spitter", "sentinel", "weaver", "howler", "other")
                },
            })
        results.append({
            "seed": seed,
            "samples": len(rows),
            "duration_s": float(rows[-1]["t"]),
            "enemy_visible_mean": round(statistics.mean(int(row["visible"]) for row in density), 2),
            "result": {key: counters[key] for key in (
                "view", "kills", "spawned", "hit_per_min_0_240", "hit_per_min_240_310", "hit_by", "hit_gated"
            )},
            "last_density_sample": density[-1],
            "phases": phases,
        })
    return {"directory": str(directory), "runs": results}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directories", type=Path, nargs="+")
    args = parser.parse_args()
    print(json.dumps([summarize(directory) for directory in args.directories], indent=2, ensure_ascii=False))
