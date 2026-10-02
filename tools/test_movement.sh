#!/usr/bin/env bash
# Vrai Player + entrées/physique ; --run-integration vérifie Main sans plafond d'images pendant son chargement.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-movement.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
validation_prepare "$TEST_DIR"
engine_args=(--headless --path . --fixed-fps 60)
integration=0
for argument in "$@"; do
    if [[ "$argument" == --run-integration ]]; then integration=1; fi
done
if [[ $integration -eq 0 ]]; then engine_args+=(--quit-after 14000); fi
validation_run "${VALIDATION_RUN_TIMEOUT:-180}" "$TEST_DIR/run.log" '^\[MovementRegression\] RESULT failures=0$' \
    "${GODOT_BIN:-godot-mono}" "${engine_args[@]}" res://tools/tests/MovementRegression.tscn -- "$@"
rg '^\[MovementRegression\]' "$TEST_DIR/run.log"
