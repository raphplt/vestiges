#!/usr/bin/env bash
# Effets des perks B2 : profil isolé, cas limites des fiches sur le vrai joueur et les résultats de combat.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
GODOT="${GODOT_BIN:-godot-mono}"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-perk_effects.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
validation_prepare "$TEST_DIR"
validation_run "${VALIDATION_RUN_TIMEOUT:-180}" "$TEST_DIR/run.log" '^\[PerkEffectsRegression\] RESULT failures=0$' \
    "$GODOT" --headless --path . --fixed-fps 60 --quit-after 600 res://tools/tests/PerkEffectsRegression.tscn
rg '^\[PerkEffectsRegression\]' "$TEST_DIR/run.log"
