#!/usr/bin/env bash
# Captures plein écran d'une vraie run (joueur nomade piloté, invincible, spawn naturel) pour juger HUD et événements.
# Usage : tools/capture_run.sh <répertoire> [secondes=60] [intervalle=10] [résolution=1920x1080] [seed]
set -euo pipefail
cd "$(dirname "$0")/.."
GODOT="${GODOT_BIN:-godot-mono}"
OUTPUT=$(realpath -m "${1:?répertoire de sortie requis}")
mkdir -p "$OUTPUT"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-capture.XXXXXX")
trap 'rm -rf "$TEST_DIR"' EXIT
export XDG_DATA_HOME="$TEST_DIR/data" XDG_CONFIG_HOME="$TEST_DIR/config" XDG_CACHE_HOME="$TEST_DIR/cache"
dotnet build --nologo >/dev/null
"$GODOT" --headless --editor --import --path . >"$OUTPUT/import.log" 2>&1
timeout 900 "$GODOT" --path . --windowed --resolution "${4:-1920x1080}" --rendering-method gl_compatibility --audio-driver Dummy \
    res://tools/tests/RunObservation.tscn -- --dev --density --seconds "${2:-60}" --capture-every "${3:-10}" \
    --seed "${5:-221092026}" --output "$OUTPUT" ${CAPTURE_EXTRA_ARGS:-} >"$OUTPUT/run.log" 2>&1 || { tail -20 "$OUTPUT/run.log"; exit 1; }
rg '\[RunObservation\] RESULT' "$OUTPUT/run.log"
