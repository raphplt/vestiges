#!/usr/bin/env bash
# Enchaîne plusieurs configurations de tools/measure_run.sh en une seule compilation et un seul import (plan 42).
# Usage : tools/measure_matrix.sh <répertoire neuf> <plan>
# Le plan est un fichier texte, une configuration par ligne, champs séparés par « | » :
#   nom | secondes | graines séparées par des espaces | profil de sauvegarde (dev ou normal) | arguments de RunObservation
# Lignes vides et lignes commençant par # ignorées. Chaque configuration écrit dans <répertoire>/<nom>, et la synthèse
# de rythme de toutes les configurations va dans <répertoire>/rythme.md (tools/summarize_pace.py).
# MEASURE_JOBS : graines en parallèle dans une configuration ; MATRIX_JOBS : configurations en parallèle (défaut 1).
# Godot n'occupe qu'un cœur par run, mais environ 1 Go de mémoire : MEASURE_JOBS × MATRIX_JOBS runs tournent ensemble.
# Si le terminal est bridé (scope de Zed), lancer hors quota : systemd-run --user --scope --quiet --collect tools/measure_matrix.sh …
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
OUTPUT=$(abs_path "${1:?répertoire de sortie requis}")
PLAN=$(abs_path "${2:?plan de mesure requis}")
if [[ -e "$OUTPUT" && -n "$(ls -A "$OUTPUT" 2>/dev/null)" ]]; then
    echo "Le dossier contient déjà des fichiers ; choisir un dossier neuf." >&2; exit 1
fi
mkdir -p "$OUTPUT"
cp "$PLAN" "$OUTPUT/plan.txt"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-matrix-import.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
validation_prepare "$OUTPUT/prepare"
# Les mesures suivantes reprennent cette compilation et cet import sans les refaire.
export VESTIGES_VALIDATION_PREPARED_ROOT
VESTIGES_VALIDATION_PREPARED_ROOT=$(pwd -P)

MATRIX_JOBS="${MATRIX_JOBS:-1}"
started=$(date +%s)
failed=0
folders=()
run_config() {
    local name=$1 seconds=$2 seeds=$3 profile=$4 args=$5
    echo "[measure_matrix] $name : $seconds s, graines $seeds, profil $profile, $args"
    MEASURE_PROFILE="$profile" MEASURE_EXTRA_ARGS="$args" tools/measure_run.sh "$OUTPUT/$name" "$seconds" "$seeds" \
        > "$OUTPUT/$name.log" 2>&1 || { echo "[measure_matrix] $name en échec : $OUTPUT/$name.log" >&2; return 1; }
}
while IFS='|' read -r name seconds seeds profile args; do
    name=$(echo "$name" | xargs)
    [[ -z "$name" || "$name" == \#* ]] && continue
    seconds=$(echo "$seconds" | xargs); seeds=$(echo "$seeds" | xargs); profile=$(echo "$profile" | xargs)
    args=$(echo "$args" | xargs)
    while (( $(jobs -rp | wc -l) >= MATRIX_JOBS )); do wait -n || failed=1; done
    # Entrée coupée : une mesure en arrière-plan ne doit pas lire les lignes du plan.
    run_config "$name" "$seconds" "$seeds" "$profile" "$args" < /dev/null &
    folders+=("$OUTPUT/$name")
done < "$PLAN"
while (( $(jobs -rp | wc -l) > 0 )); do wait -n || failed=1; done
python3 tools/summarize_pace.py "${folders[@]}" > "$OUTPUT/rythme.md" || failed=1
echo "[measure_matrix] ${#folders[@]} configurations en $(( $(date +%s) - started )) s ; synthèse : $OUTPUT/rythme.md"
exit "$failed"
