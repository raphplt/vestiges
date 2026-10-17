#!/usr/bin/env bash
# Terres successives (plan 51, P1) : changement de terre en place sur la vraie scène Main, terres allumées.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
GODOT="${GODOT_BIN:-godot-mono}"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-lands.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
validation_prepare "$TEST_DIR"
validation_run "${VALIDATION_RUN_TIMEOUT:-300}" "$TEST_DIR/run.log" '^\[LandChangeRegression\] RESULT failures=0$' \
    "$GODOT" --headless --path . --fixed-fps 60 --audio-driver Dummy res://tools/tests/LandChangeRegression.tscn -- --dev --lands
rg '^\[LandChangeRegression\]' "$TEST_DIR/run.log"
