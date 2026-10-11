#!/usr/bin/env bash
# Parcours des menus, sur un profil neuf isolé.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-menu-navigation.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
validation_prepare "$TEST_DIR"
validation_run 120 "$TEST_DIR/run.log" '^\[MenuNavigationRegression\] RESULT checks=[0-9]+ failures=0$' \
    "${GODOT_BIN:-godot-mono}" --headless --path . --fixed-fps 60 res://tools/tests/MenuNavigationRegression.tscn
rg '^\[MenuNavigationRegression\]' "$TEST_DIR/run.log"
