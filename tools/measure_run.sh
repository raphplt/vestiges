#!/usr/bin/env bash
# Mesure de densité en vraie run (ennemis, niveaux, coffres vus), sans rendu et en temps de jeu accéléré.
# Compile et importe une fois, puis joue chaque seed en headless avec --fixed-fps 60 : une seconde de jeu
# ne dure plus une seconde d'horloge. Pour juger une image, utiliser tools/capture_run.sh.
# Usage : tools/measure_run.sh <répertoire> [secondes=180] ["seed seed …"]
# MEASURE_EXTRA_ARGS : arguments de RunObservation (ex. "--nomad", "--nomad --visit" pour un bot qui ratisse les
# lieux vus) ; MEASURE_JOBS : seeds en parallèle (défaut 1). Le résumé donne aussi les lieux croisés et visités par
# minute, les micro-événements, l'Essence gagnée et dépensée (plan 22, lot C0).
# MEASURE_PROFILE=normal : profil neuf sans mode dev, armes et objets limités à la réserve de départ (plan 06 §9).
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
export GODOT="${GODOT_BIN:-godot-mono}"
export OUTPUT=$(abs_path "${1:?répertoire de sortie requis}")
export RUN_SECONDS="${2:-180}"
SEEDS="${3:-221092026 1002 7 42 20260926}"
export MEASURE_EXTRA_ARGS="${MEASURE_EXTRA_ARGS:-}"
export MEASURE_PROFILE="${MEASURE_PROFILE:-dev}"
JOBS="${MEASURE_JOBS:-1}"
if [[ ! "$RUN_SECONDS" =~ ^[1-9][0-9]*$ || ! "$JOBS" =~ ^[1-9][0-9]*$ ]]; then
    echo "Durée et nombre de jobs positifs requis." >&2; exit 1
fi
read -r -a seeds <<< "${SEEDS//$'\n'/ }"
python3 - "$OUTPUT" "${seeds[@]}" <<'PYCHECK'
from pathlib import Path
import re,sys
root=Path(sys.argv[1]); seeds=sys.argv[2:]
if not seeds or len(set(seeds))!=len(seeds) or any(not re.fullmatch(r'[0-9]+',seed) for seed in seeds):
    raise SystemExit('Seeds numériques distinctes requises.')
if root.exists() and any(root.iterdir()):
    raise SystemExit('Le dossier contient déjà des fichiers ; choisir un dossier neuf.')
root.mkdir(parents=True,exist_ok=True)
PYCHECK
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-measure-import.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
validation_prepare "$OUTPUT"

# Un profil isolé par seed ; le parent garde le verrou jusqu'à la fin de tous les jobs.
run_seed() {
    local seed=$1 profile exit_code=0
    profile=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-measure.XXXXXX") || return 1
    (
        source tools/lib/validation.sh
        isolate_godot_profile "$profile" || exit 1
        extra_args=()
        if [[ -n "$MEASURE_EXTRA_ARGS" ]]; then read -r -a extra_args <<< "$MEASURE_EXTRA_ARGS"; fi
        profile_args=(--dev)
        if [[ "$MEASURE_PROFILE" == normal ]]; then profile_args=(); fi
        validation_run "${VALIDATION_RUN_TIMEOUT:-$(( RUN_SECONDS > 900 ? RUN_SECONDS * 2 : 1800 ))}" \
            "$OUTPUT/seed-$seed.log" "^\[RunObservation\] RESULT seed=$seed seconds=$RUN_SECONDS( .*)?$" \
            "$GODOT" --headless --fixed-fps 60 --audio-driver Dummy --path . \
            res://tools/tests/RunObservation.tscn -- ${profile_args[@]+"${profile_args[@]}"} --density --seconds "$RUN_SECONDS" \
            --seed "$seed" --output "$OUTPUT/seed-$seed" ${extra_args[@]+"${extra_args[@]}"}
    ) || exit_code=$?
    echo "$exit_code" > "$OUTPUT/seed-$seed.exit"
    rm -rf "$profile"
    return "$exit_code"
}
export -f run_seed
started=$(date +%s)
failed=0
printf '%s\n' "${seeds[@]}" | xargs -P "$JOBS" -I{} bash -c 'run_seed "$1"' _ {} || failed=1
python3 tools/summarize_run_measurements.py "$OUTPUT" "$RUN_SECONDS" "${seeds[@]}" || failed=1
echo "[measure_run] ${#seeds[@]} seeds × ${RUN_SECONDS} s de jeu en $(( $(date +%s) - started )) s"
exit "$failed"
