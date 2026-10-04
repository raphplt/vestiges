#!/usr/bin/env bash
# Sauvegardes protégées et fin de run réglée une fois (plan 26 Q2a, Q2b), dans un profil temporaire : le profil personnel n'est ni lu ni modifié.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-saves.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
GODOT="${GODOT_BIN:-godot-mono}"
validation_prepare "$TEST_DIR"
# Les fichiers illisibles provoqués par le scénario sont signalés en erreur par les gestionnaires : seules
# ces erreurs-là sont admises.
VALIDATION_EXPECTED_ERRORS='^ERROR: \[(MetaSaveManager|RunHistoryManager|ScoreManager|RunSettlement)\] (Sauvegarde|Historique|Record|Acquis|Cannot save) ' \
    validation_run "${VALIDATION_RUN_TIMEOUT:-120}" "$TEST_DIR/saves.log" '^\[SaveFileRegression\] PASS$' \
    "$GODOT" --headless --path . --quit-after 600 res://tools/tests/SaveFileRegression.tscn
rg '^\[SaveFileRegression\] (RESULT|FAIL)' "$TEST_DIR/saves.log"
echo '✓ Sauvegardes : interruptions, fichiers tronqués, versions invalides et futures, échec d'"'"'écriture vérifiés.'
