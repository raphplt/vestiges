#!/usr/bin/env bash
# Densité d'ennemis en spawn naturel sur la vraie Main rendue : ennemis visibles, temps sans ennemi, débits, niveaux.
# Usage : tools/measure_density.sh <répertoire résultats> [seeds...]   (SECONDS_PER_RUN=180 par défaut)
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
SCREEN_ARGS=$(godot_screen_args)
GODOT="${GODOT_BIN:-godot-mono}"
OUTPUT=$(abs_path "${1:?répertoire de résultats requis}")
shift
SEEDS=("$@")
if [[ ${#SEEDS[@]} -eq 0 ]]; then SEEDS=(221092026 777); fi
RUN_SECONDS="${SECONDS_PER_RUN:-180}"
if [[ ! "$RUN_SECONDS" =~ ^[1-9][0-9]*$ ]]; then echo "Durée positive requise." >&2; exit 1; fi
python3 - "$OUTPUT" "${SEEDS[@]}" <<'PYCHECK'
from pathlib import Path
import re,sys
root=Path(sys.argv[1]); seeds=sys.argv[2:]
if len(set(seeds))!=len(seeds) or any(not re.fullmatch(r'[0-9]+',seed) for seed in seeds):
    raise SystemExit('Seeds numériques distinctes requises.')
if root.exists() and any(root.iterdir()): raise SystemExit('Choisir un dossier de mesure neuf.')
root.mkdir(parents=True,exist_ok=True)
PYCHECK
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-density.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
validation_prepare "$OUTPUT"
failed=0
for seed in "${SEEDS[@]}"; do
    exit_code=0
    validation_run "${VALIDATION_RUN_TIMEOUT:-600}" "$OUTPUT/seed-$seed.log" "^\[RunObservation\] RESULT seed=$seed seconds=$RUN_SECONDS( .*)?$" \
        "$GODOT" --path . --windowed $SCREEN_ARGS --resolution 1280x720 --rendering-method gl_compatibility --audio-driver Dummy \
        res://tools/tests/RunObservation.tscn -- --dev --density --seconds "$RUN_SECONDS" --seed "$seed" --output "$OUTPUT" || exit_code=$?
    echo "$exit_code" > "$OUTPUT/seed-$seed.exit"
    if [[ $exit_code -ne 0 ]]; then failed=1; fi
done
python3 tools/summarize_run_measurements.py --flat "$OUTPUT" "$RUN_SECONDS" "${SEEDS[@]}" || failed=1
if [[ $failed -ne 0 ]]; then exit 1; fi
python3 tools/summarize_density.py "$OUTPUT"
