#!/usr/bin/env bash
# Captures de l'écran d'accueil (profil isolé, mode dev tout débloqué).
# Usage : tools/capture_hub.sh <répertoire> [actions séparées par des virgules] [résolution=1920x1080]
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/portable.sh
GODOT="${GODOT_BIN:-godot-mono}"
OUTPUT=$(abs_path "${1:?répertoire de sortie requis}")
mkdir -p "$OUTPUT"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-hub.XXXXXX")
trap 'rm -rf "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
dotnet build --nologo >/dev/null
"$GODOT" --headless --editor --import --path . >"$OUTPUT/import.log" 2>&1
run_timeout 300 "$GODOT" --path . --windowed --resolution "${3:-1920x1080}" --rendering-method gl_compatibility --audio-driver Dummy \
    res://tools/tests/HubCapture.tscn -- ${HUB_DEV:---dev} --output "$OUTPUT" --actions "${2:-}" >"$OUTPUT/run.log" 2>&1 || { tail -20 "$OUTPUT/run.log"; exit 1; }
rg '\[HubCapture\]' "$OUTPUT/run.log"
