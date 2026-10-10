#!/usr/bin/env bash
# Quêtes de déblocage (plan 06 §9) dans un profil temporaire : catalogue, réserve de départ, accomplissement et sauvegarde.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-quests.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
validation_prepare "$TEST_DIR"
validation_run "${VALIDATION_RUN_TIMEOUT:-180}" "$TEST_DIR/quests.log" '^\[QuestRegression\] PASS$' \
    "$GODOT" --headless --path . --fixed-fps 60 --quit-after 3000 res://tools/tests/QuestRegression.tscn
rg '^\[QuestRegression\] (RESULT|FAIL)' "$TEST_DIR/quests.log"
echo '✓ Quêtes : catalogue, réserve de départ, accomplissement et sauvegarde vérifiés.'
