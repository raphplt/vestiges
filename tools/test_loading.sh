#!/usr/bin/env bash
# Chargement récupérable (plan 26 Q4) : pannes injectées, scène quittée en route, fermeture pendant le chargement.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-loading.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
validation_prepare "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
for scenario in normal random-seed fault-catalogues fault-generation fault-decors leave-generation leave-decors quit; do
    expected=''
    # Une panne injectée est journalisée comme un vrai échec de chargement : seules ces lignes-là sont admises.
    if [[ $scenario == fault-* ]]; then
        # Les lignes « [n] … » sont la pile que Godot imprime sous cette erreur.
        expected='^ERROR: \[GameBootstrap\] Chargement en échec à l.étape |^System\.InvalidOperationException: panne injectée |^\s+\[[0-9]+\] '
    fi
    VALIDATION_EXPECTED_ERRORS="$expected" \
        validation_run "${VALIDATION_RUN_TIMEOUT:-180}" "$TEST_DIR/$scenario.log" "^\[LoadingRecoveryRegression\] RESULT failures=0 checks=[0-9]+ scenario=$scenario$" \
        "$GODOT" --headless --path . res://tools/tests/LoadingRecoveryRegression.tscn -- --scenario "$scenario"
    if [[ $scenario == leave-* ]]; then
        rg -q '^\[GameBootstrap\] Chargement abandonné à l.étape ' "$TEST_DIR/$scenario.log" \
            || { echo "Abandon non consigné : $scenario" >&2; exit 1; }
    fi
    rg '^\[LoadingRecoveryRegression\] (OK|RESULT)|^\[GameBootstrap\] Chargement' "$TEST_DIR/$scenario.log" || true
done
echo '✓ Chargement : pannes affichées avec retour au camp, scène quittée sans pause ni décors orphelins, fermeture propre.'
