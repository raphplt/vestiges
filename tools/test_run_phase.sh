#!/usr/bin/env bash
# Transitions globales de la run (plan 26 Q8d) : un seul propriétaire, late game irréversible, endgame conservé.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-run-phase.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
validation_prepare "$TEST_DIR"
validation_run "${VALIDATION_RUN_TIMEOUT:-60}" "$TEST_DIR/run_phase.log" '^\[RunPhaseRegression\] RESULT failures=0 checks=[0-9]+$' \
    "${GODOT_BIN:-godot-mono}" --headless --path . res://tools/tests/RunPhaseRegression.tscn
rg '^\[RunPhaseRegression\] (OK|RESULT)' "$TEST_DIR/run_phase.log"
echo '✓ Phases : quatrième Résurgence sous le seuil, late game gardé ; endgame conservé ; mort ; nouvelle run.'
