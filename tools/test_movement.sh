#!/usr/bin/env bash
# Vrai Player + entrées et physique Godot. Les sauvegardes du joueur restent isolées.
# --run-integration vérifie aussi le bootstrap de Main et le terrain généré.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/portable.sh
GODOT="${GODOT_BIN:-godot-mono}"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-movement.XXXXXX")
trap 'rm -rf "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
dotnet build --nologo
"$GODOT" --headless --editor --import --path . >"$TEST_DIR/import.log" 2>&1
status=0
"$GODOT" --headless --path . --fixed-fps 60 --quit-after 14000 res://tools/tests/MovementRegression.tscn -- "$@" >"$TEST_DIR/run.log" 2>&1 || status=$?
godot_exit_ok "$status" "$TEST_DIR/run.log" '\[MovementRegression\] RESULT failures=0' || {
    cat "$TEST_DIR/run.log"
    exit 1
}
cat "$TEST_DIR/run.log"
rg -q '\[MovementRegression\] RESULT failures=0' "$TEST_DIR/run.log"
# Les exceptions extérieures aux assertions doivent également faire échouer le banc.
ERRORS=$(rg '^(ERROR|SCRIPT ERROR)|Unhandled exception|System\.[A-Za-z]+Exception' "$TEST_DIR/run.log" | rg -v 'steam_api|MixRate mismatch|ObjectDB instances were leaked|resources still in use at exit' || true)
if [[ -n "$ERRORS" ]]; then
    echo "$ERRORS"
    exit 1
fi
