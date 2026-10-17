#!/usr/bin/env bash
# Galerie des zones au sol (pixel_fx.gdshader) : matières, couloir et limite de portée sur trois sols, graine fixe.
# Usage : tools/capture_ground_zones.sh <répertoire> [poses=4] [copies du banc=0] ; sans écran : sous xvfb-run.
# Banc (3e argument > 0) : temps réel moyen par image avec les zones répétées, à comparer avant/après sur la même machine.
# --fixed-fps 60 : le temps du shader avance de 1/60 s par image, les poses capturées sont reproductibles.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/portable.sh
SCREEN_ARGS=$(godot_screen_args)
GODOT="${GODOT_BIN:-godot-mono}"
OUTPUT=$(abs_path "${1:?répertoire de sortie requis}")
mkdir -p "$OUTPUT"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-zones.XXXXXX")
trap 'rm -rf "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
dotnet build --nologo >/dev/null
"$GODOT" --headless --editor --import --path . >"$OUTPUT/import.log" 2>&1
run_timeout 300 "$GODOT" --path . --windowed $SCREEN_ARGS --resolution 1920x1080 --rendering-method gl_compatibility --audio-driver Dummy --disable-vsync --fixed-fps 60 \
    res://tools/tests/GroundZoneGallery.tscn -- --output "$OUTPUT" --shots "${2:-4}" --bench "${3:-0}" >"$OUTPUT/run.log" 2>&1 || { tail -20 "$OUTPUT/run.log"; exit 1; }
rg '\[GroundZoneGallery\] (RESULT|BENCH)' "$OUTPUT/run.log"
ERRORS=$(rg '^(ERROR|SCRIPT ERROR)|SHADER ERROR|Unhandled exception' "$OUTPUT/run.log" | rg -v 'steam_api|MixRate mismatch|ObjectDB instances were leaked|resources still in use at exit' || true)
if [[ -n "$ERRORS" ]]; then
    echo "$ERRORS" >&2
    exit 1
fi
