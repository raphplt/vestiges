#!/usr/bin/env bash
# Inventaire du joueur (plan 35) dans un profil temporaire : réglages, Écussons, objets et sac.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-inventory.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
validation_prepare "$TEST_DIR"
validation_run "${VALIDATION_RUN_TIMEOUT:-300}" "$TEST_DIR/inventory.log" '^\[InventoryRegression\] PASS$' \
    "$GODOT" --headless --path . --fixed-fps 60 --quit-after 6000 res://tools/tests/InventoryRegression.tscn
rg '^\[InventoryRegression\] (RESULT|FAIL)' "$TEST_DIR/inventory.log"
echo '✓ Inventaire : réglages, Écussons et objets vérifiés.'
