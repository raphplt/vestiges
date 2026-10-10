#!/usr/bin/env bash
# Quêtes de déblocage (plan 06 §9) dans des profils temporaires : catalogue, réserve de départ, sauvegarde (Q1), suivi en run (Q2).
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-quests.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
validation_prepare "$TEST_DIR"
# Chaque passage part d'un profil neuf : le suivi en run accomplit les 36 quêtes une à une.
for stage in unlocks tracking; do
    user_dir=$(vestiges_user_dir)
    [[ "$user_dir" == "$(abs_path "$TEST_DIR")"/* ]] || { echo "Profil non isolé : $user_dir" >&2; exit 1; }
    find "$user_dir" -mindepth 1 -delete
    args=()
    if [[ "$stage" == tracking ]]; then args+=(--tracking); fi
    validation_run "${VALIDATION_RUN_TIMEOUT:-300}" "$TEST_DIR/$stage.log" '^\[QuestRegression\] PASS$' \
        "$GODOT" --headless --path . --fixed-fps 60 --quit-after 6000 res://tools/tests/QuestRegression.tscn -- ${args[@]+"${args[@]}"}
    rg '^\[QuestRegression\] (RESULT|FAIL)' "$TEST_DIR/$stage.log"
done
echo '✓ Quêtes : catalogue, réserve de départ, sauvegarde, suivi de chaque quête et bandeau vérifiés.'
