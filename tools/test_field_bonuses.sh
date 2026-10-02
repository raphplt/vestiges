#!/usr/bin/env bash
# Plan 24 C4 : bonus lâchés (limite, ramassage, effets, fin de vie).
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
OUTPUT=$(abs_path "${1:-/tmp/vestiges-field_bonuses-$(date +%Y%m%d-%H%M%S)}")
mkdir -p "$OUTPUT"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-field_bonuses-profile.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
validation_prepare "$OUTPUT"
validation_run "${VALIDATION_RUN_TIMEOUT:-180}" "$OUTPUT/run.log" '^\[FieldBonusRegression\] RESULT failures=0$' \
    "$GODOT" --headless --path . --fixed-fps 60 res://tools/tests/FieldBonusRegression.tscn --
rg '^\[FieldBonusRegression\]' "$OUTPUT/run.log"
