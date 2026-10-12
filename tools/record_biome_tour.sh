#!/usr/bin/env bash
# Tournage de la visite des biomes (plan 41 R5) : le joueur reste immobile au cœur des décors de chaque biome, créatures
# retirées, et le Movie Maker de Godot filme à cadence fixe. Sert à juger en mouvement le vent, les particules, les
# ombres de nuages et la météo, que les captures fixes ne montrent pas.
# Produit tour.mp4 (images seules) et, par biome, une rafale de quatre images à 0,2 s (tour-<biome>-<n>.png).
# Usage : tools/record_biome_tour.sh <répertoire> [secondes par biome=6] [seed=1002]
# TOUR_EXTRA_ARGS : arguments ajoutés au banc (par exemple --weather storm).
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/portable.sh
SCREEN_ARGS=$(godot_screen_args)
GODOT="${GODOT_BIN:-godot-mono}"
OUTPUT=$(abs_path "${1:?répertoire de sortie requis}")
SECONDS_PER_BIOME="${2:-6}"
FPS=30
mkdir -p "$OUTPUT"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-tour.XXXXXX")
trap 'rm -rf "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
dotnet build --nologo >/dev/null
"$GODOT" --headless --editor --import --path . >"$OUTPUT/import.log" 2>&1
run_timeout 1800 "$GODOT" --path . --windowed $SCREEN_ARGS --resolution 1920x1080 --rendering-method gl_compatibility \
    --write-movie "$OUTPUT/raw.avi" --fixed-fps "$FPS" --disable-vsync \
    res://tools/tests/RunObservation.tscn -- --capture-biome-tour "$SECONDS_PER_BIOME" --seed "${3:-1002}" \
    --output "$OUTPUT" ${TOUR_EXTRA_ARGS:-} >"$OUTPUT/run.log" 2>&1 || { tail -20 "$OUTPUT/run.log"; exit 1; }
grep -E '\[RunObservation\] (tour|RESULT)' "$OUTPUT/run.log"
ffmpeg -hide_banner -loglevel error -y -i "$OUTPUT/raw.avi" -an -vf scale=1280:-2 -c:v libx264 -preset veryfast -crf 24 "$OUTPUT/tour.mp4"
rm -f "$OUTPUT/raw.avi"
echo "[record_biome_tour] $OUTPUT/tour.mp4"
