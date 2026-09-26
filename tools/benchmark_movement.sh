#!/usr/bin/env bash
# Main réelle en GL, sans cap FPS ; toutes les sauvegardes vont dans un profil temporaire.
# Usage : tools/benchmark_movement.sh [répertoire résultats]
# BENCH_SECONDS=20 BENCH_WARMUP=5 BENCH_REPEATS=3 BENCH_ENEMIES=120 GODOT_BIN=godot-mono
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/portable.sh
SCREEN_ARGS=$(godot_screen_args)
GODOT="${GODOT_BIN:-godot-mono}"
OUTPUT=$(abs_path "${1:-/tmp/vestiges-dense-$(date +%Y%m%d-%H%M%S)}")
mkdir -p "$OUTPUT"
if compgen -G "$OUTPUT/*-baseline.json" >/dev/null || compgen -G "$OUTPUT/*-dash.json" >/dev/null; then
    echo "Le dossier contient déjà des mesures ; choisir un nouveau dossier : $OUTPUT" >&2
    exit 1
fi
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-dense-profile.XXXXXX")
trap 'rm -rf "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
dotnet build --nologo
"$GODOT" --headless --editor --import --path . >"$OUTPUT/import.log" 2>&1
for resolution in 1280x720 1920x1080; do
    for ((repeat=1; repeat<=${BENCH_REPEATS:-3}; repeat++)); do
        # Alterner l'ordre limite le biais de chauffe/cache entre les deux variantes.
        modes=(baseline dash)
        if ((repeat % 2 == 0)); then modes=(dash baseline); fi
        for mode in "${modes[@]}"; do
            extra=()
            if [[ "$mode" == dash ]]; then extra+=(--dash); fi
            prefix="$OUTPUT/${resolution}-${repeat}-${mode}"
            echo "Benchmark $resolution répétition $repeat : $mode"
            run_timeout 180 "$GODOT" --path . --windowed $SCREEN_ARGS --resolution "$resolution" \
                --rendering-method gl_compatibility --disable-vsync --max-fps 0 --audio-driver Dummy \
                res://tools/tests/MovementDenseBenchmark.tscn -- --dev \
                --width "${resolution%x*}" --height "${resolution#*x}" --enemies "${BENCH_ENEMIES:-120}" ${BENCH_EXTRA_ARGS:-} \
                --seconds "${BENCH_SECONDS:-20}" --warmup "${BENCH_WARMUP:-5}" \
                --output "$prefix" ${extra[@]+"${extra[@]}"} >"$prefix.log" 2>&1 || { cat "$prefix.log"; exit 1; }
            rg -q '\[MovementDenseBenchmark\] RESULT valid=True' "$prefix.log"
            # Profil isolé sous macOS : Godot n'y crée pas son cache de shaders (sans effet sur la mesure).
            errors=$(rg '^(ERROR|SCRIPT ERROR)|Unhandled exception|System\.[A-Za-z]+Exception' "$prefix.log" | rg -v "steam_api|MixRate mismatch|ObjectDB instances were leaked|resources still in use at exit|Can't create shader cache folder" || true)
            if [[ -n "$errors" ]]; then echo "$errors"; exit 1; fi
        done
    done
done
python3 tools/summarize_movement_benchmark.py "$OUTPUT"
echo "Résultats et captures : $OUTPUT"
