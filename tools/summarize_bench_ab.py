#!/usr/bin/env python3
"""Un A/B n'est comparable que si toutes les passes des deux versions sont valides."""
import json
import math
from pathlib import Path
import statistics
import sys


def main():
    root, passes = Path(sys.argv[1]), int(sys.argv[2])
    if passes < 1:
        raise ValueError("Nombre de passes positif requis")
    failed = False
    print("| Version | Résolution | FPS médian | p99 ms médian | Nœuds créés/s | Passes valides |")
    print("|---|---|---:|---:|---:|---:|")
    for side in ("base", "current"):
        for resolution in ("1280x720", "1920x1080"):
            rows = []
            for index in range(1, passes + 1):
                path = root / f"{side}-{index}" / f"{resolution}-1-baseline.json"
                try:
                    row = json.loads(path.read_text())
                    if row["valid"] is not True or any(not math.isfinite(row["frames"][key])
                            or row["frames"][key] <= 0 for key in ("fps", "p99_ms")):
                        raise ValueError("mesure invalide")
                    rows.append(row)
                except (OSError, ValueError, KeyError, TypeError) as error:
                    print(f"Passe invalide ({path}) : {error}", file=sys.stderr)
            if len(rows) != passes:
                failed = True
                print(f"| {side} | {resolution} | — | — | — | {len(rows)}/{passes} |")
                continue
            fps = statistics.median(row["frames"]["fps"] for row in rows)
            p99 = statistics.median(row["frames"]["p99_ms"] for row in rows)
            nodes = [row["nodes_added_per_second"] for row in rows if "nodes_added_per_second" in row]
            node_text = f"{statistics.median(nodes):.0f}" if nodes else "n/m"
            print(f"| {side} | {resolution} | {fps:.1f} | {p99:.1f} | {node_text} | {len(rows)}/{passes} |")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
