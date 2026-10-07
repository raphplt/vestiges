#!/usr/bin/env bash
# Mixage : banque, priorités, voix simultanées, pause, fondus et retour au Hub.
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
validation_run 120 "$OUTPUT/bank.log" '\[AudioBankSmoke\] OK' \
    "$GODOT" --headless --path . --script res://tools/tests/AudioBankSmoke.gd
validation_run 120 "$OUTPUT/mix.log" '^\[AudioMixRegression\] RESULT failures=0$' \
    "$GODOT" --headless --path . --fixed-fps 60 res://tools/tests/AudioMixRegression.tscn
rg '^\[AudioMixRegression\]' "$OUTPUT/mix.log"
