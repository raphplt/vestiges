#!/usr/bin/env bash
# Contrats des catalogues de réglages (plan 26 Q7) : fichiers du dépôt acceptés, fixtures négatives refusées.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-catalogs.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
validation_prepare "$TEST_DIR"
validation_run "${VALIDATION_RUN_TIMEOUT:-60}" "$TEST_DIR/catalogs.log" '^\[CatalogContractRegression\] RESULT failures=0 checks=[0-9]+$' \
    "${GODOT_BIN:-godot-mono}" --headless --path . res://tools/tests/CatalogContractRegression.tscn
rg '^\[CatalogContractRegression\] (OK|RESULT)' "$TEST_DIR/catalogs.log"
echo '✓ Catalogues : fichiers du dépôt acceptés ; champs absents, types, bornes, clés inconnues ou en double et références refusés.'
