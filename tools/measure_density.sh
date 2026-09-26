#!/usr/bin/env bash
# Densité d'ennemis en spawn naturel sur la vraie Main rendue : ennemis visibles, temps sans ennemi, débits, niveaux.
# Usage : tools/measure_density.sh <répertoire résultats> [seeds...]   (SECONDS_PER_RUN=180 par défaut)
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/portable.sh
SCREEN_ARGS=$(godot_screen_args)
GODOT="${GODOT_BIN:-godot-mono}"
OUTPUT=$(abs_path "${1:?répertoire de résultats requis}")
shift
SEEDS=("$@")
if [[ ${#SEEDS[@]} -eq 0 ]]; then SEEDS=(221092026 777); fi
mkdir -p "$OUTPUT"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-density.XXXXXX")
trap 'rm -rf "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
dotnet build --nologo >/dev/null
"$GODOT" --headless --editor --import --path . >"$OUTPUT/import.log" 2>&1
for seed in "${SEEDS[@]}"; do
    run_timeout 600 "$GODOT" --path . --windowed $SCREEN_ARGS --resolution 1280x720 --rendering-method gl_compatibility --audio-driver Dummy \
        res://tools/tests/RunObservation.tscn -- --dev --density --seconds "${SECONDS_PER_RUN:-180}" --seed "$seed" --output "$OUTPUT" \
        >"$OUTPUT/run-$seed.log" 2>&1 || { tail -20 "$OUTPUT/run-$seed.log"; exit 1; }
    rg '\[RunObservation\] RESULT' "$OUTPUT/run-$seed.log"
done
python3 tools/summarize_density.py "$OUTPUT"
