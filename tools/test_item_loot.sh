#!/usr/bin/env bash
# Objets : probabilités des sources, Chance, paiement unique et choix de coffre.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-item-loot.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
validation_prepare "$TEST_DIR"
validation_run "${VALIDATION_RUN_TIMEOUT:-300}" "$TEST_DIR/item_loot.log" '^\[ItemLootRegression\] PASS$' \
    "$GODOT" --headless --path . --fixed-fps 60 --quit-after 6000 res://tools/tests/ItemLootRegression.tscn
rg '^\[ItemLootRegression\] (RESULT|FAIL)' "$TEST_DIR/item_loot.log"
echo '✓ Objets : tirages et transactions vérifiés.'
