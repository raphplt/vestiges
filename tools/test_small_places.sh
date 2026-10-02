#!/usr/bin/env bash
# Petits lieux (plan 22, lot C1) : placement, signe, usage unique, récompenses.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
GODOT="${GODOT_BIN:-godot-mono}"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-small_places.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
validation_prepare "$TEST_DIR"
validation_run "${VALIDATION_RUN_TIMEOUT:-180}" "$TEST_DIR/run.log" '^\[SmallPlacesRegression\] RESULT failures=0$' \
    "$GODOT" --headless --path . --fixed-fps 60 --quit-after 600 res://tools/tests/SmallPlacesRegression.tscn
rg '^\[SmallPlacesRegression\]' "$TEST_DIR/run.log"
