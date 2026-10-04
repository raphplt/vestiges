#!/usr/bin/env bash
# File des classements Steam avec un faux service (plan 26 Q3), dans un profil temporaire. Ne prouve pas le service réel.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-steam-leaderboards.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
validation_prepare "$TEST_DIR"
validation_run "${VALIDATION_RUN_TIMEOUT:-120}" "$TEST_DIR/run.log" '^\[LeaderboardQueueRegression\] RESULT failures=0$' \
    "$GODOT" --headless --path . --quit-after 600 res://tools/tests/LeaderboardQueueRegression.tscn
rg '^\[LeaderboardQueueRegression\]' "$TEST_DIR/run.log"
