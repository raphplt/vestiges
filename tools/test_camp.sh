#!/usr/bin/env bash
# Le Campement (plan 36) dans des profils temporaires : catalogue, achats, remboursements, normalisation, application,
# puis une vraie run en profil neuf (deux armes) et avec le Campement plein (quatre armes, toutes les stats).
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-camp.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
validation_prepare "$TEST_DIR"
for stage in catalog fresh full; do
    user_dir=$(vestiges_user_dir)
    [[ "$user_dir" == "$(abs_path "$TEST_DIR")"/* ]] || { echo "Profil non isolé : $user_dir" >&2; exit 1; }
    mkdir -p "$user_dir" && find "$user_dir" -mindepth 1 -delete
    # Une vraie run se charge sur des milliers d'images (génération en fil) : pas de --quit-after, le délai borne.
    godot_args=(--quit-after 6000) args=()
    if [[ "$stage" != catalog ]]; then godot_args=() args+=(--run "$stage"); fi
    validation_run "${VALIDATION_RUN_TIMEOUT:-300}" "$TEST_DIR/$stage.log" '^\[CampRegression\] PASS$' \
        "$GODOT" --headless --path . --fixed-fps 60 ${godot_args[@]+"${godot_args[@]}"} res://tools/tests/CampRegression.tscn \
        -- ${args[@]+"${args[@]}"}
    rg '^\[CampRegression\] (RESULT|FAIL)' "$TEST_DIR/$stage.log"
done
echo '✓ Campement : catalogue, achats, remboursements, normalisation et application en run vérifiés.'
