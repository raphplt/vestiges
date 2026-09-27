#!/usr/bin/env bash
# Mesure de densité en vraie run (ennemis, niveaux, coffres vus), sans rendu et en temps de jeu accéléré.
# Compile et importe une fois, puis joue chaque seed en headless avec --fixed-fps 60 : une seconde de jeu
# ne dure plus une seconde d'horloge. Pour juger une image, utiliser tools/capture_run.sh.
# Usage : tools/measure_run.sh <répertoire> [secondes=180] ["seed seed …"]
# MEASURE_EXTRA_ARGS : arguments de RunObservation (ex. "--nomad") ; MEASURE_JOBS : seeds en parallèle (défaut 1).
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/portable.sh
export GODOT="${GODOT_BIN:-godot-mono}"
export OUTPUT=$(abs_path "${1:?répertoire de sortie requis}")
export RUN_SECONDS="${2:-180}"
SEEDS="${3:-221092026 1002 7 42 20260926}"
export MEASURE_EXTRA_ARGS="${MEASURE_EXTRA_ARGS:-}"
mkdir -p "$OUTPUT"

dotnet build --nologo -v q -clp:ErrorsOnly
"$GODOT" --headless --editor --import --path . >"$OUTPUT/import.log" 2>&1 || true

# Un profil Godot isolé par seed : les parties parallèles n'écrivent pas dans les mêmes sauvegardes.
run_seed() {
    local seed=$1 profile
    profile=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-measure.XXXXXX")
    (
        isolate_godot_profile "$profile"
        run_timeout 1800 "$GODOT" --headless --fixed-fps 60 --audio-driver Dummy --path . \
            res://tools/tests/RunObservation.tscn -- --dev --density --seconds "$RUN_SECONDS" \
            --seed "$seed" --output "$OUTPUT/seed-$seed" $MEASURE_EXTRA_ARGS >"$OUTPUT/seed-$seed.log" 2>&1
    ) || echo "[measure_run] seed $seed en échec : $OUTPUT/seed-$seed.log" >&2
    rm -rf "$profile"
}
export -f run_seed isolate_godot_profile run_timeout

started=$(date +%s)
echo $SEEDS | tr ' ' '\n' | xargs -P "${MEASURE_JOBS:-1}" -I{} bash -c 'run_seed {}'
grep -h '\[RunObservation\] RESULT' "$OUTPUT"/seed-*.log | sed 's/.*RESULT //' | tee "$OUTPUT/summary.txt"
echo "[measure_run] $(echo $SEEDS | wc -w | tr -d ' ') seeds × ${RUN_SECONDS} s de jeu en $(( $(date +%s) - started )) s"
