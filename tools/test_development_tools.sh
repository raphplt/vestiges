#!/usr/bin/env bash
# Vraie Main : lancement normal, banc protégé et outils dev.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-development-tools.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
validation_prepare "$TEST_DIR"
# Le témoin normal n'est pas lancé depuis tools/, afin d'exercer le véritable profil normal.
cp tools/tests/DevelopmentToolsRegression.tscn "$TEST_DIR/NormalEntry.tscn"
for stage in normal test dev; do
    scene=res://tools/tests/DevelopmentToolsRegression.tscn
    args=()
    if [[ "$stage" == normal ]]; then scene="$TEST_DIR/NormalEntry.tscn"; args+=(--normal-entry); fi
    if [[ "$stage" == dev ]]; then args+=(--dev); fi
    validation_run "${VALIDATION_RUN_TIMEOUT:-180}" "$TEST_DIR/$stage.log" '^\[DevelopmentToolsRegression\] RESULT failures=0 checks=[0-9]+ provenance=(Normal|Test|Development)$' \
        "${GODOT_BIN:-godot-mono}" --headless --path . "$scene" -- ${args[@]+"${args[@]}"}
    rg '^\[DevelopmentToolsRegression\]' "$TEST_DIR/$stage.log"
    if [[ "$stage" == normal ]]; then
        cp -a "$(vestiges_user_dir)" "$TEST_DIR/normal-before"
    else
        for file in meta_save.json run_history.json highscore_kills.save analytics/aggregate.json; do
            if [[ -f "$TEST_DIR/normal-before/$file" ]]; then
                cmp "$TEST_DIR/normal-before/$file" "$(vestiges_user_dir)/$file"
            elif [[ -e "$(vestiges_user_dir)/$file" ]]; then
                echo "Fichier normal créé par un essai : $file" >&2; exit 1
            fi
        done
    fi
done
