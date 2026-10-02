#!/usr/bin/env bash
# Cache et découverte cartographique ; sauvegardes isolées.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
GODOT="${GODOT_BIN:-godot-mono}"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-cartography.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
validation_prepare "$TEST_DIR"
validation_run "${VALIDATION_RUN_TIMEOUT:-180}" "$TEST_DIR/run.log" '^\[CartographyRegression\] RESULT checks=[0-9]+ failures=0$' \
    "$GODOT" --headless --path . --fixed-fps 60 --quit-after 6000 res://tools/tests/CartographyRegression.tscn
rg '^\[CartographyRegression\]' "$TEST_DIR/run.log"
