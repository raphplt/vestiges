#!/usr/bin/env bash
# Lot 6B : coût et contrat des impacts continus. Second argument facultatif : --baseline.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
OUTPUT=$(abs_path "${1:-/tmp/vestiges-cone-$(date +%Y%m%d-%H%M%S)}")
mkdir -p "$OUTPUT"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-cone-profile.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
validation_prepare "$OUTPUT"
validation_run "${VALIDATION_RUN_TIMEOUT:-180}" "$OUTPUT/run.log" '^\[ConeRegression\] RESULT failures=0$' \
    "$GODOT" --headless --path . --fixed-fps 60 res://tools/tests/ConeRegression.tscn -- --output "$OUTPUT/result.json" ${2:+"$2"}
rg '^\[ConeRegression\]' "$OUTPUT/run.log"
