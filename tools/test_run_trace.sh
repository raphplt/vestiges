#!/usr/bin/env bash
# Tirages reproductibles (plan 26 Q8c-2) : même seed et même pas de temps, mêmes apparitions ; autre seed, autres apparitions.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-run-trace.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
validation_prepare "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
trace() {
    validation_run "${VALIDATION_RUN_TIMEOUT:-240}" "$TEST_DIR/$1.log" "^\[RunTraceRegression\] RESULT seed=$2 spawns=[0-9]+ hash=[0-9A-F]+$" \
        "$GODOT" --headless --fixed-fps 60 --path . res://tools/tests/RunTraceRegression.tscn -- --seed "$2"
    rg '^\[RunTraceRegression\]' "$TEST_DIR/$1.log" >&2
    rg -o 'hash=[0-9A-F]+' "$TEST_DIR/$1.log"
}
first=$(trace first 221092026)
second=$(trace second 221092026)
other=$(trace other 7)
[[ $first == "$second" ]] || { echo "Même seed, traces différentes : $first / $second" >&2; exit 1; }
[[ $first != "$other" ]] || { echo "Autre seed, même trace : $first" >&2; exit 1; }
echo "✓ Tirages : même seed, mêmes 10 apparitions ($first) ; autre seed, autres apparitions ($other)."
