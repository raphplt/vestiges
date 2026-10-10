#!/usr/bin/env bash
# Parties de boss : réglages, réserve commune, seuils, mort unique (plan 07 B1), dans un profil temporaire.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-boss-parts.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
validation_prepare "$TEST_DIR"
validation_run "${VALIDATION_RUN_TIMEOUT:-180}" "$TEST_DIR/run.log" '^\[BossPartsRegression\] RESULT failures=0$' \
    "$GODOT" --headless --path . --fixed-fps 60 --quit-after 3000 res://tools/tests/BossPartsRegression.tscn
rg '^\[BossPartsRegression\]' "$TEST_DIR/run.log"
