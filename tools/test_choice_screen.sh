#!/usr/bin/env bash
# Plan 04 R7 : entrées, relances, pause et validation des choix.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
GODOT="${GODOT_BIN:-godot-mono}"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-choice_screen.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
validation_prepare "$TEST_DIR"
validation_run "${VALIDATION_RUN_TIMEOUT:-180}" "$TEST_DIR/run.log" '^\[ChoiceScreenRegression\] RESULT checks=[0-9]+ failures=0$' \
    "$GODOT" --headless --path . --fixed-fps 60 res://tools/tests/ChoiceScreenRegression.tscn
rg '^\[ChoiceScreenRegression\]' "$TEST_DIR/run.log"
