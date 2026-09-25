#!/usr/bin/env bash
# Compare le banc de combat dense entre un commit de base et l'arbre de travail courant, en passes alternées.
# Usage : tools/bench_ab.sh <ref de base> <dossier de sortie neuf> [passes=2]
# BENCH_SECONDS (15 par défaut) et GODOT_BIN sont transmis au banc.
set -euo pipefail
cd "$(dirname "$0")/.."
BASE=${1:?ref de base requise (ex. HEAD, HEAD~1, un hash)}
OUTPUT=$(realpath -m "${2:?dossier de sortie requis}")
PASSES=${3:-2}
if [[ -e "$OUTPUT" ]]; then
    echo "Le dossier existe déjà : $OUTPUT" >&2
    exit 1
fi
mkdir -p "$OUTPUT"

# Les FPS n'ont de sens que machine calme : on refuse de mesurer au-dessus d'une charge d'un quart des cœurs.
load=$(cut -d' ' -f1 /proc/loadavg)
# getconf plutôt que nproc : nproc peut refléter une restriction cgroup du shell, pas la machine.
limit=$(( $(getconf _NPROCESSORS_ONLN) / 4 ))
if awk -v l="$load" -v m="$limit" 'BEGIN { exit !(l > m) }'; then
    echo "Charge trop élevée ($load > $limit) : mesure reportée. Voir ps -eo pcpu,comm --sort=-pcpu | head." >&2
    exit 2
fi

WORKTREE=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-bench-base.XXXXXX")
trap 'git worktree remove --force "$WORKTREE" >/dev/null 2>&1 || true; git worktree prune' EXIT
git worktree add -q --detach "$WORKTREE" "$BASE"
# Même instrument de mesure des deux côtés : la scène de banc courante remplace celle de la base.
cp tools/tests/MovementDenseBenchmark.cs "$WORKTREE/tools/tests/"

export BENCH_REPEATS=1 BENCH_SECONDS="${BENCH_SECONDS:-15}"
for ((pass = 1; pass <= PASSES; pass++)); do
    echo "Passe $pass/$PASSES : base ($BASE)"
    (cd "$WORKTREE" && tools/benchmark_movement.sh "$OUTPUT/base-$pass" >/dev/null 2>&1) || echo "  passe base $pass invalide" >&2
    echo "Passe $pass/$PASSES : courant"
    tools/benchmark_movement.sh "$OUTPUT/current-$pass" >/dev/null 2>&1 || echo "  passe courante $pass invalide" >&2
done

python3 - "$OUTPUT" <<'PY'
import json, statistics, sys
from pathlib import Path
root = Path(sys.argv[1])
print("| Version | Résolution | FPS médian | p99 ms médian | Nœuds créés/s | Passes valides |")
print("|---|---|---:|---:|---:|---:|")
for side in ("base", "current"):
    for resolution in ("1280x720", "1920x1080"):
        rows = [json.loads(p.read_text()) for p in sorted(root.glob(f"{side}-*/{resolution}-*-baseline.json"))]
        rows = [r for r in rows if r["valid"]]
        if not rows:
            print(f"| {side} | {resolution} | — | — | — | 0 |")
            continue
        fps = statistics.median(r["frames"]["fps"] for r in rows)
        p99 = statistics.median(r["frames"]["p99_ms"] for r in rows)
        nodes = [r["nodes_added_per_second"] for r in rows if "nodes_added_per_second" in r]
        node_text = f"{statistics.median(nodes):.0f}" if nodes else "n/m"
        print(f"| {side} | {resolution} | {fps:.1f} | {p99:.1f} | {node_text} | {len(rows)} |")
PY
echo "Charge en fin de mesure : $(cut -d' ' -f1-3 /proc/loadavg)"
