#!/usr/bin/env bash
# L’Indicible : réglages contrôlés et combat scripté dans la scène de run, ses trois phases (plan 07 B3), dans un profil temporaire.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-indicible.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
validation_prepare "$TEST_DIR"
validation_run "${VALIDATION_RUN_TIMEOUT:-180}" "$TEST_DIR/run.log" '^\[IndicibleRegression\] RESULT failures=0$' \
    "$GODOT" --headless --path . --fixed-fps 60 --quit-after 6000 res://tools/tests/IndicibleRegression.tscn
rg '^\[IndicibleRegression\]' "$TEST_DIR/run.log"
