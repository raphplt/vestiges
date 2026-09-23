#!/usr/bin/env bash
# Capacités ennemies composées (Présage, bond) sur vrais Player/Enemy ; sauvegardes isolées.
set -euo pipefail
cd "$(dirname "$0")/.."
GODOT="${GODOT_BIN:-godot-mono}"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-abilities.XXXXXX")
trap 'rm -rf "$TEST_DIR"' EXIT
export XDG_DATA_HOME="$TEST_DIR/data"
export XDG_CONFIG_HOME="$TEST_DIR/config"
export XDG_CACHE_HOME="$TEST_DIR/cache"
dotnet build --nologo
"$GODOT" --headless --editor --import --path . >"$TEST_DIR/import.log" 2>&1
"$GODOT" --headless --path . --fixed-fps 60 --quit-after 6000 res://tools/tests/EnemyAbilityRegression.tscn >"$TEST_DIR/run.log" 2>&1 || {
    cat "$TEST_DIR/run.log"
    exit 1
}
rg '\[EnemyAbilityRegression\]' "$TEST_DIR/run.log"
rg -q '\[EnemyAbilityRegression\] RESULT failures=0' "$TEST_DIR/run.log"
ERRORS=$(rg '^(ERROR|SCRIPT ERROR)|Unhandled exception|System\.[A-Za-z]+Exception' "$TEST_DIR/run.log" | rg -v 'steam_api|MixRate mismatch|ObjectDB instances were leaked|resources still in use at exit' || true)
if [[ -n "$ERRORS" ]]; then
    echo "$ERRORS"
    exit 1
fi
