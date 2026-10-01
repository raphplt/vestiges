#!/usr/bin/env bash
# Usage : tools/test_erasure_active.sh [dossier] [result.json de référence]
# Lot 6C : cellules actives, trace exacte et réactivation de l’Effacement.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/portable.sh
OUTPUT=$(abs_path "${1:-/tmp/vestiges-erasure-active-$(date +%Y%m%d-%H%M%S)}")
mkdir -p "$OUTPUT"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-erasure-active-profile.XXXXXX")
trap 'rm -rf "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
dotnet build --nologo > "$OUTPUT/build.log"
"$GODOT" --headless --editor --import --path . > "$OUTPUT/import.log" 2>&1
run_timeout 120 "$GODOT" --headless --path . --fixed-fps 60 res://tools/tests/ErasureActiveRegression.tscn -- --output "$OUTPUT/result.json" > "$OUTPUT/run.log" 2>&1 || { cat "$OUTPUT/run.log"; exit 1; }
rg '\[ErasureActiveRegression\]' "$OUTPUT/run.log"
rg -q '\[ErasureActiveRegression\] RESULT checks=[0-9]+ failures=0' "$OUTPUT/run.log"
errors=$(rg '^(ERROR|SCRIPT ERROR)|Unhandled exception|System\.[A-Za-z]+Exception' "$OUTPUT/run.log" | rg -v 'steam_api|MixRate mismatch|ObjectDB instances were leaked|resources still in use at exit' || true)
if [[ -n "$errors" ]]; then echo "$errors"; exit 1; fi

if [[ -n "${2:-}" ]]; then
    python3 tools/compare_erasure_active.py "$2" "$OUTPUT/result.json" --output "$OUTPUT/comparison.json"
fi
