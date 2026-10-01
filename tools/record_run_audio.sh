#!/usr/bin/env bash
# Enregistrement audible d'une vraie run (plan 15, A0) : le Movie Maker de Godot écrit les images et le mixage
# du moteur à cadence fixe, sans passer par la sortie son du système ni enregistrer d'autre application.
# Bot nomade et invincible, profil neuf ; il visite les lieux et lit chaque écran de choix deux secondes.
# Produit run.mp4 (images + son), audio.flac, la trace d'écoute (audio-*.csv) et rapport.md (tools/audio_report.py).
# Usage : tools/record_run_audio.sh <répertoire> [secondes=360] [seed=221092026] [personnage=traqueur]
# RECORD_EXTRA_ARGS : arguments ajoutés au banc ; RECORD_KEEP_AVI=1 garde la capture brute (≈ 8 Mo/s).
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/portable.sh
SCREEN_ARGS=$(godot_screen_args)
GODOT="${GODOT_BIN:-godot-mono}"
OUTPUT=$(abs_path "${1:?répertoire de sortie requis}")
SECONDS_OF_PLAY="${2:-360}"
FPS=30
mkdir -p "$OUTPUT"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-record.XXXXXX")
trap 'rm -rf "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
dotnet build --nologo >/dev/null
"$GODOT" --headless --editor --import --path . >"$OUTPUT/import.log" 2>&1
# Le rendu à cadence fixe tourne plus lentement que le temps réel : prévoir large.
run_timeout 3600 "$GODOT" --path . --windowed $SCREEN_ARGS --resolution 1920x1080 --rendering-method gl_compatibility \
    --write-movie "$OUTPUT/raw.avi" --fixed-fps "$FPS" \
    res://tools/tests/RunObservation.tscn -- --density --nomad --visit --audio-trace --choice-delay 2 \
    --seconds "$SECONDS_OF_PLAY" --seed "${3:-221092026}" --character "${4:-traqueur}" --output "$OUTPUT" \
    ${RECORD_EXTRA_ARGS:-} >"$OUTPUT/run.log" 2>&1 || { tail -20 "$OUTPUT/run.log"; exit 1; }
grep -E '\[RunObservation\] display|\[AudioTrace\]' "$OUTPUT/run.log"
ffmpeg -hide_banner -loglevel error -y -i "$OUTPUT/raw.avi" -vf scale=1280:-2 -c:v libx264 -preset veryfast -crf 26 \
    -c:a aac -b:a 192k "$OUTPUT/run.mp4"
ffmpeg -hide_banner -loglevel error -y -i "$OUTPUT/raw.avi" -vn -c:a flac "$OUTPUT/audio.flac"
[[ "${RECORD_KEEP_AVI:-0}" == 1 ]] || rm -f "$OUTPUT/raw.avi"
python3 tools/audio_report.py "$OUTPUT"
