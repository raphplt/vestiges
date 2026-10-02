#!/usr/bin/env bash
# Plan 15 A1 : intentions musicales (annonce, crise, accalmie, combat par présence, pause, mort, Hub, endgame).
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
OUTPUT=$(abs_path "${1:-/tmp/vestiges-music-$(date +%Y%m%d-%H%M%S)}")
mkdir -p "$OUTPUT"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-music-profile.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
validation_prepare "$OUTPUT"
validation_run "${VALIDATION_RUN_TIMEOUT:-180}" "$OUTPUT/run.log" '^\[MusicRegression\] RESULT failures=0$' \
    "$GODOT" --headless --path . --fixed-fps 60 res://tools/tests/MusicRegression.tscn --
rg '^\[MusicRegression\]' "$OUTPUT/run.log"
