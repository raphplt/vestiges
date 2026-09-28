#!/usr/bin/env bash
# Lot 6A : statuts à distance, dette temporelle, phases et dégâts du Néant.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/portable.sh
OUTPUT=$(abs_path "${1:-/tmp/vestiges-temporal-$(date +%Y%m%d-%H%M%S)}")
mkdir -p "$OUTPUT"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-temporal-profile.XXXXXX")
trap 'rm -rf "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
dotnet build --nologo > "$OUTPUT/build.log"
"$GODOT" --headless --editor --import --path . > "$OUTPUT/import.log" 2>&1
run_timeout 90 "$GODOT" --headless --path . --fixed-fps 60 res://tools/tests/TemporalRegression.tscn -- --output "$OUTPUT/result.json" > "$OUTPUT/run.log" 2>&1 || { cat "$OUTPUT/run.log"; exit 1; }
rg '\[TemporalRegression\]' "$OUTPUT/run.log"
rg -q '\[TemporalRegression\] RESULT failures=0' "$OUTPUT/run.log"
errors=$(rg '^(ERROR|SCRIPT ERROR)|Unhandled exception|System\.[A-Za-z]+Exception' "$OUTPUT/run.log" | rg -v 'steam_api|MixRate mismatch|ObjectDB instances were leaked|resources still in use at exit' || true)
if [[ -n "$errors" ]]; then echo "$errors"; exit 1; fi
