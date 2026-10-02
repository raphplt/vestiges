#!/usr/bin/env python3
"""Une mesure par seed demandée, issue d'un processus réussi et d'un CSV rempli."""
import argparse
import csv
import json
import math
from pathlib import Path
import re
import sys
from check_validation_log import check


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--flat', action='store_true', help='CSV à la racine, pour le banc rendu')
    parser.add_argument('directory', type=Path)
    parser.add_argument('seconds', type=int)
    parser.add_argument('seeds', nargs='+')
    args = parser.parse_args()
    root, seconds, seeds = args.directory, args.seconds, args.seeds
    results, lines = [], []
    for seed in seeds:
        log = root / f"seed-{seed}.log"
        try:
            if (root / f"seed-{seed}.exit").read_text().strip() != "0":
                raise ValueError("processus ou validation du journal en échec")
            check(log, rf"^\[RunObservation\] RESULT seed={seed} seconds={seconds}(?:\s.*)?$")
            csv_root = root if args.flat else root / f"seed-{seed}"
            with (csv_root / f"density-{seed}.csv").open() as handle:
                rows = list(csv.DictReader(handle))
            if not rows:
                raise ValueError("CSV vide")
            times = [float(row['t']) for row in rows]
            if any(not math.isfinite(value) for value in times) or times != sorted(set(times)):
                raise ValueError("Temps CSV invalides ou dupliqués")
            if times[0] < 0 or times[0] > 2 or times[-1] < max(1, seconds - 1):
                raise ValueError("CSV partiel : couverture temporelle insuffisante")
            line = next(line for line in log.read_text().splitlines() if line.startswith("[RunObservation] RESULT "))
            lines.append(line.removeprefix("[RunObservation] RESULT "))
            results.append({"seed": seed, "valid": True, "samples": len(rows)})
        except (OSError, ValueError, KeyError, csv.Error) as error:
            results.append({"seed": seed, "valid": False, "error": str(error)})
    (root / "summary.txt").write_text("\n".join(lines) + "\n")
    (root / "validation.json").write_text(json.dumps({"expected": len(seeds), "results": results}, indent=2) + "\n")
    for line in lines:
        print(line)
    invalid = [result for result in results if not result["valid"]]
    for result in invalid:
        print(f"Seed {result['seed']} invalide : {result['error']}", file=sys.stderr)
    return 1 if invalid or not seeds else 0


if __name__ == "__main__":
    sys.exit(main())
