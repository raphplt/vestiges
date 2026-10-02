#!/usr/bin/env bash
# Build strict, import terminé puis vrai Hub pendant 600 images par défaut.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
FRAMES="${1:-600}"
if [[ ! "$FRAMES" =~ ^[1-9][0-9]*$ ]]; then echo "Nombre d'images positif requis." >&2; exit 1; fi
LOG_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-smoke.XXXXXX")
# Le smoke garde ses preuves ; son profil est isolé comme celui des régressions.
isolate_godot_profile "$LOG_DIR/profile"
validation_prepare "$LOG_DIR"
validation_run "${VALIDATION_RUN_TIMEOUT:-120}" "$LOG_DIR/run.log" "^\[HubSmokeRegression\] RESULT failures=0 frames=$FRAMES$" \
    "${GODOT_BIN:-godot-mono}" --headless --path . --fixed-fps 60 res://tools/tests/HubSmokeRegression.tscn -- --frames "$FRAMES"
echo "✓ Smoke test OK (logs : $LOG_DIR)"
