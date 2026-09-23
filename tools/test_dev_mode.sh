#!/usr/bin/env bash
# Ne lit ni ne modifie le profil personnel : les processus partagent un XDG temporaire.
set -euo pipefail
cd "$(dirname "$0")/.."
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-dev-mode.XXXXXX")
trap 'rm -rf "$TEST_DIR"' EXIT
export XDG_DATA_HOME="$TEST_DIR/data"
export XDG_CONFIG_HOME="$TEST_DIR/config"
export XDG_CACHE_HOME="$TEST_DIR/cache"
GODOT="${GODOT_BIN:-godot-mono}"
dotnet build --nologo
"$GODOT" --headless --editor --import --path . >"$TEST_DIR/import.log" 2>&1
for stage in seed dev normal toggle preference normal_after_toggle; do
    args=()
    if [[ "$stage" == seed ]]; then args+=(--seed-profile); fi
    if [[ "$stage" == dev ]]; then args+=(--dev); fi
    if [[ "$stage" == toggle ]]; then args+=(--toggle-profile); fi
    if [[ "$stage" == preference ]]; then args+=(--verify-toggle-enabled); fi
    "$GODOT" --headless --path . --quit-after 600 res://tools/tests/DevelopmentModeRegression.tscn -- "${args[@]}" >"$TEST_DIR/$stage.log" 2>&1 || {
        cat "$TEST_DIR/$stage.log"
        exit 1
    }
    rg '\[DevelopmentModeRegression\]' "$TEST_DIR/$stage.log"
    rg -q '^\[DevelopmentModeRegression\] PASS$' "$TEST_DIR/$stage.log"
    errors=$(rg '^(ERROR|SCRIPT ERROR)|Unhandled exception|System\.[A-Za-z]+Exception' "$TEST_DIR/$stage.log" | rg -v 'steam_api|MixRate mismatch|ObjectDB instances were leaked|resources still in use at exit' || true)
    if [[ -n "$errors" ]]; then echo "$errors"; exit 1; fi
    if [[ "$stage" == seed ]]; then
        cp -a "$XDG_DATA_HOME/godot/app_userdata/Vestiges" "$TEST_DIR/normal-before"
    elif [[ "$stage" == dev || "$stage" == toggle ]]; then
        for file in meta_save.json run_history.json highscore.save analytics/aggregate.json; do
            cmp "$TEST_DIR/normal-before/$file" "$XDG_DATA_HOME/godot/app_userdata/Vestiges/$file"
        done
    fi
done
echo '✓ Profils normal/dev isolés ; contenu débloqué et retour normal vérifiés.'
