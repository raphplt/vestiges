#!/usr/bin/env bash
# Lot 6A : statuts à distance, dette temporelle, phases et dégâts du Néant.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
OUTPUT=$(abs_path "${1:-/tmp/vestiges-temporal-$(date +%Y%m%d-%H%M%S)}")
mkdir -p "$OUTPUT"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-temporal-profile.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
validation_prepare "$OUTPUT"
validation_run "${VALIDATION_RUN_TIMEOUT:-90}" "$OUTPUT/run.log" '^\[TemporalRegression\] RESULT failures=0$' \
    "$GODOT" --headless --path . --fixed-fps 60 res://tools/tests/TemporalRegression.tscn -- --output "$OUTPUT/result.json"
rg '^\[TemporalRegression\]' "$OUTPUT/run.log"
