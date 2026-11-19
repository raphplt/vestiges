#!/usr/bin/env bash
# Horde de la run 3D (plan 60, 60.2b) : pistage, errance, retrait, tir à portée qui ignore les étages ; sans rendu.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-crowd3d.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
validation_prepare "$TEST_DIR"
validation_run "${VALIDATION_RUN_TIMEOUT:-120}" "$TEST_DIR/run.log" '^\[Crowd3DRegression\] RESULT failures=0$' \
    "${GODOT_BIN:-godot-mono}" --headless --path . res://tools/tests/Crowd3DRegression.tscn -- "$@"
rg '^\[Crowd3DRegression\]' "$TEST_DIR/run.log"
