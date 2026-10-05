#!/usr/bin/env bash
# Remapping sans pertes (plan 26 Q8b) : un changement, un vrai redémarrage, un ancien fichier, la réinitialisation.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-input-remap.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
validation_prepare "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
bindings="$(vestiges_user_dir)/input_bindings.cfg"
run_phase() {
    validation_run "${VALIDATION_RUN_TIMEOUT:-60}" "$TEST_DIR/$1.log" "^\[InputRemapRegression\] RESULT failures=0 checks=[0-9]+ phase=$1$" \
        "$GODOT" --headless --path . res://tools/tests/InputRemapRegression.tscn -- --phase "$1"
    rg '^\[InputRemapRegression\] (OK|RESULT)' "$TEST_DIR/$1.log"
}
# La lecture au démarrage ne doit jamais réécrire le fichier.
unchanged_by() {
    local before
    before=$(sha256sum "$bindings")
    run_phase "$1"
    [[ $(sha256sum "$bindings") == "$before" ]] || { echo "Fichier réécrit par la lecture : $1" >&2; exit 1; }
}
rm -f "$bindings"
run_phase remap
unchanged_by restart
run_phase legacy-write
unchanged_by legacy-check
run_phase reset
echo '✓ Remapping : un seul changement garde WASD, flèches, croix et stick au redémarrage ; ancien fichier repris ; reset complet.'
