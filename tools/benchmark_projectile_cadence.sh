#!/usr/bin/env bash
# Banc de cadence à composition fixe, pas une mesure de FPS. Quatre cas, 90 s de jeu chacun.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/portable.sh
OUTPUT=$(abs_path "${1:?répertoire de sortie requis}")
GODOT="${GODOT_BIN:-godot-mono}"
mkdir -p "$OUTPUT"
PROFILE=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-cadence.XXXXXX")
trap 'rm -rf "$PROFILE"' EXIT
isolate_godot_profile "$PROFILE"
dotnet build --nologo
"$GODOT" --headless --editor --import --path . >"$OUTPUT/import.log" 2>&1
for aggression in 1 1.6; do
    for multiplier in 1 2.5; do
        log="$OUTPUT/cadence-$aggression-$multiplier.log"
        run_timeout 180 "$GODOT" --headless --path . --fixed-fps 60 --audio-driver Dummy --quit-after 6000 \
            res://tools/tests/ProjectileCadenceBenchmark.tscn -- \
            --howler-cooldown "$multiplier" --aggression "$aggression" >"$log" 2>&1
        rg '\[ProjectileCadenceBenchmark\] RESULT' "$log"
    done
done
