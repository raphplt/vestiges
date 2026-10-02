#!/usr/bin/env bash
# Ne lit ni ne modifie le profil personnel : les processus partagent un profil temporaire.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-dev-mode.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
validation_prepare "$TEST_DIR"
for stage in seed dev normal toggle preference normal_after_toggle; do
    args=()
    if [[ "$stage" == seed ]]; then args+=(--seed-profile); fi
    if [[ "$stage" == dev ]]; then args+=(--dev); fi
    if [[ "$stage" == toggle ]]; then args+=(--toggle-profile); fi
    if [[ "$stage" == preference ]]; then args+=(--verify-toggle-enabled); fi
    validation_run "${VALIDATION_RUN_TIMEOUT:-120}" "$TEST_DIR/$stage.log" '^\[DevelopmentModeRegression\] PASS$' \
        "$GODOT" --headless --path . --quit-after 600 res://tools/tests/DevelopmentModeRegression.tscn -- ${args[@]+"${args[@]}"}
    rg '^\[DevelopmentModeRegression\]' "$TEST_DIR/$stage.log"
    if [[ "$stage" == seed ]]; then
        cp -a "$(vestiges_user_dir)" "$TEST_DIR/normal-before"
    elif [[ "$stage" == dev || "$stage" == toggle ]]; then
        for file in meta_save.json run_history.json highscore_kills.save analytics/aggregate.json; do
            cmp "$TEST_DIR/normal-before/tests/$file" "$(vestiges_user_dir)/tests/$file"
        done
    fi
done
echo '✓ Profils normal/dev isolés ; contenu débloqué et retour normal vérifiés.'
