#!/usr/bin/env bash
# Compare le banc de combat dense entre un commit de base et l'arbre de travail courant, en passes alternées.
# Usage : tools/bench_ab.sh <ref de base> <dossier de sortie neuf> [passes=2]
# BENCH_SECONDS (15 par défaut) et GODOT_BIN sont transmis au banc.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
BASE=${1:?ref de base requise (ex. HEAD, HEAD~1, un hash)}
OUTPUT=$(abs_path "${2:?dossier de sortie requis}")
PASSES=${3:-2}
if [[ ! "$PASSES" =~ ^[1-9][0-9]*$ ]]; then echo "Nombre de passes positif requis." >&2; exit 1; fi
if [[ -e "$OUTPUT" ]]; then
    echo "Le dossier existe déjà : $OUTPUT" >&2
    exit 1
fi
mkdir -p "$OUTPUT"

# Les FPS n'ont de sens que machine calme : on refuse de mesurer au-dessus d'une charge d'un quart des cœurs.
load=$(load_average)
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
cp tools/tests/MovementDenseBenchmark.cs tools/tests/MovementDenseBenchmark.cs.uid tools/tests/MovementDenseBenchmark.tscn "$WORKTREE/tools/tests/"
cp tools/benchmark_movement.sh "$WORKTREE/tools/"
mkdir -p "$WORKTREE/tools/lib"
cp tools/lib/portable.sh tools/lib/validation.sh "$WORKTREE/tools/lib/"
cp tools/check_validation_log.py tools/with_validation_lock.py "$WORKTREE/tools/"

export BENCH_REPEATS=1 BENCH_SECONDS="${BENCH_SECONDS:-15}"
failed=0
for ((pass = 1; pass <= PASSES; pass++)); do
    echo "Passe $pass/$PASSES : base ($BASE)"
    (cd "$WORKTREE" && tools/benchmark_movement.sh "$OUTPUT/base-$pass") >"$OUTPUT/base-$pass.log" 2>&1 || failed=1
    echo "Passe $pass/$PASSES : courant"
    tools/benchmark_movement.sh "$OUTPUT/current-$pass" >"$OUTPUT/current-$pass.log" 2>&1 || failed=1
done

python3 tools/summarize_bench_ab.py "$OUTPUT" "$PASSES" | tee "$OUTPUT/summary.txt" || failed=1
echo "Charge en fin de mesure : $(load_average)"
exit "$failed"
