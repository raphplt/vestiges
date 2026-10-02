#!/usr/bin/env bash
# Main réelle en GL, sans cap FPS ; toutes les sauvegardes vont dans un profil temporaire.
# Usage : tools/benchmark_movement.sh [répertoire résultats]
# BENCH_SECONDS=20 BENCH_WARMUP=5 BENCH_REPEATS=3 BENCH_ENEMIES=120 GODOT_BIN=godot-mono
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
SCREEN_ARGS=$(godot_screen_args)
GODOT="${GODOT_BIN:-godot-mono}"
REPEATS="${BENCH_REPEATS:-3}"
if [[ ! "$REPEATS" =~ ^[1-9][0-9]*$ ]]; then echo "Nombre de répétitions positif requis." >&2; exit 1; fi
OUTPUT=$(abs_path "${1:-/tmp/vestiges-dense-$(date +%Y%m%d-%H%M%S)}")
mkdir -p "$OUTPUT"
if compgen -G "$OUTPUT/*-baseline.json" >/dev/null || compgen -G "$OUTPUT/*-dash.json" >/dev/null; then
    echo "Le dossier contient déjà des mesures ; choisir un nouveau dossier : $OUTPUT" >&2
    exit 1
fi
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-dense-profile.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
validation_prepare "$OUTPUT"
for resolution in 1280x720 1920x1080; do
    for ((repeat=1; repeat<=REPEATS; repeat++)); do
        # Alterner l'ordre limite le biais de chauffe/cache entre les deux variantes.
        modes=(baseline dash)
        if ((repeat % 2 == 0)); then modes=(dash baseline); fi
        for mode in "${modes[@]}"; do
            extra=()
            if [[ "$mode" == dash ]]; then extra+=(--dash); fi
            prefix="$OUTPUT/${resolution}-${repeat}-${mode}"
            echo "Benchmark $resolution répétition $repeat : $mode"
            validation_run "${VALIDATION_RUN_TIMEOUT:-180}" "$prefix.log" '^\[MovementDenseBenchmark\] RESULT valid=True output=.+$' "$GODOT" --path . --windowed $SCREEN_ARGS --resolution "$resolution" \
                --rendering-method gl_compatibility --disable-vsync --max-fps 0 --audio-driver Dummy \
                res://tools/tests/MovementDenseBenchmark.tscn -- --dev \
                --width "${resolution%x*}" --height "${resolution#*x}" --enemies "${BENCH_ENEMIES:-120}" ${BENCH_EXTRA_ARGS:-} \
                --seconds "${BENCH_SECONDS:-20}" --warmup "${BENCH_WARMUP:-5}" \
                --output "$prefix" ${extra[@]+"${extra[@]}"}
            python3 - "$prefix.json" <<'PYCHECK'
import json,sys
with open(sys.argv[1]) as handle: row=json.load(handle)
if row.get('valid') is not True: raise SystemExit('Banc dense invalide : '+sys.argv[1])
PYCHECK
        done
    done
done
python3 tools/summarize_movement_benchmark.py "$OUTPUT"
echo "Résultats et captures : $OUTPUT"
