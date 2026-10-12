#!/usr/bin/env bash
# Objets : statistiques d’armes, liens, soins et objets de dash.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-item-combat.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
validation_prepare "$TEST_DIR"
validation_run "${VALIDATION_RUN_TIMEOUT:-300}" "$TEST_DIR/item_combat.log" '^\[ItemCombatRegression\] PASS$' \
    "$GODOT" --headless --path . --fixed-fps 60 --quit-after 6000 res://tools/tests/ItemCombatRegression.tscn
rg '^\[ItemCombatRegression\] (RESULT|FAIL)' "$TEST_DIR/item_combat.log"
echo '✓ Objets : effets de combat vérifiés.'
