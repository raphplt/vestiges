#!/usr/bin/env bash
# Usage : tools/test_erasure_active.sh [dossier] [result.json de référence]
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
OUTPUT=$(abs_path "${1:-/tmp/vestiges-erasure_active-$(date +%Y%m%d-%H%M%S)}")
mkdir -p "$OUTPUT"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-erasure_active-profile.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
validation_prepare "$OUTPUT"
validation_run "${VALIDATION_RUN_TIMEOUT:-120}" "$OUTPUT/run.log" '^\[ErasureActiveRegression\] RESULT checks=[0-9]+ failures=0$' \
    "$GODOT" --headless --path . --fixed-fps 60 res://tools/tests/ErasureActiveRegression.tscn -- --output "$OUTPUT/result.json"
rg '^\[ErasureActiveRegression\]' "$OUTPUT/run.log"

if [[ -n "${2:-}" ]]; then
    python3 tools/compare_erasure_active.py "$2" "$OUTPUT/result.json" --output "$OUTPUT/comparison.json"
fi
