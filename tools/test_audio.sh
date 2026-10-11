#!/usr/bin/env bash
# Mixage : niveaux de la banque, priorités, voix simultanées, pause, fondus et retour au Hub.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
OUTPUT=$(abs_path "${1:-/tmp/vestiges-audio-$(date +%Y%m%d-%H%M%S)}")
mkdir -p "$OUTPUT"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-audio-profile.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
validation_prepare "$OUTPUT"
# Garde-fous du mix : cibles par famille, plafond, silences de tête, phase (data/audio/mix_targets.json).
validation_run 300 "$OUTPUT/bank-audit.log" '^\[AudioBankAudit\] [0-9]+ fichiers mesurés, 0 échec' \
    python3 tools/audio_bank_audit.py --csv "$OUTPUT/bank-audit.csv"
validation_run 120 "$OUTPUT/bank.log" '\[AudioBankSmoke\] OK' \
    "$GODOT" --headless --path . --script res://tools/tests/AudioBankSmoke.gd
validation_run 120 "$OUTPUT/mix.log" '^\[AudioMixRegression\] RESULT failures=0$' \
    "$GODOT" --headless --path . --fixed-fps 60 res://tools/tests/AudioMixRegression.tscn
rg '^\[AudioMixRegression\]' "$OUTPUT/mix.log"
