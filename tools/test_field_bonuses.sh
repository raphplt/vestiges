#!/usr/bin/env bash
# Plan 24 C4 : bonus lâchés (limite, ramassage, effets, fin de vie).
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/portable.sh
OUTPUT=$(abs_path "${1:-/tmp/vestiges-field-bonus-$(date +%Y%m%d-%H%M%S)}")
mkdir -p "$OUTPUT"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-field-bonus-profile.XXXXXX")
trap 'rm -rf "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
dotnet build --nologo > "$OUTPUT/build.log"
"$GODOT" --headless --editor --import --path . > "$OUTPUT/import.log" 2>&1
run_timeout 180 "$GODOT" --headless --path . --fixed-fps 60 res://tools/tests/FieldBonusRegression.tscn -- > "$OUTPUT/run.log" 2>&1 || { cat "$OUTPUT/run.log"; exit 1; }
rg '\[FieldBonusRegression\]' "$OUTPUT/run.log"
rg -q '\[FieldBonusRegression\] RESULT failures=0' "$OUTPUT/run.log"
errors=$(rg '^(ERROR|SCRIPT ERROR)|Unhandled exception|System\.[A-Za-z]+Exception' "$OUTPUT/run.log" | rg -v 'steam_api|MixRate mismatch|ObjectDB instances were leaked|resources still in use at exit' || true)
if [[ -n "$errors" ]]; then echo "$errors"; exit 1; fi
