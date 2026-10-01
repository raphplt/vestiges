#!/usr/bin/env bash
# Plan 04 R7 : entrées, relances, pause et validation des choix.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/portable.sh
GODOT="${GODOT_BIN:-godot-mono}"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-choice-screen.XXXXXX")
trap 'rm -rf "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
dotnet build --nologo
"$GODOT" --headless --editor --import --path . >"$TEST_DIR/import.log" 2>&1
"$GODOT" --headless --path . --fixed-fps 60 --quit-after 600 res://tools/tests/ChoiceScreenRegression.tscn >"$TEST_DIR/run.log" 2>&1 || {
    cat "$TEST_DIR/run.log"
    exit 1
}
rg '\[ChoiceScreenRegression\]' "$TEST_DIR/run.log"
rg -q '\[ChoiceScreenRegression\] RESULT checks=[0-9]+ failures=0' "$TEST_DIR/run.log"
ERRORS=$(rg '^(ERROR|SCRIPT ERROR)|Unhandled exception|System\.[A-Za-z]+Exception' "$TEST_DIR/run.log" | rg -v 'steam_api|MixRate mismatch|ObjectDB instances were leaked|resources still in use at exit' || true)
if [[ -n "$ERRORS" ]]; then
    echo "$ERRORS"
    exit 1
fi
