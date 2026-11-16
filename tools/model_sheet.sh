#!/usr/bin/env bash
# Planche des modèles low poly (plan 60, lot 60.0a) : importe assets/models/**/*.glb, les montre de face, de trois quarts,
# de profil et de dos, chaque animation pose par pose, puis tous à leur vraie taille. Images dans <dossier>.
# Les modèles se régénèrent avec : python3 tools/export_lowpoly.py
# Bibliothèque de l'export de masse (plan 60 §17, lot C1), une planche par dizaine de modèles d'un catalogue :
#   tools/model_sheet.sh /tmp/planche --library build/models
# Exemple : tools/model_sheet.sh /tmp/planche
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/portable.sh
dir="${1:?usage: tools/model_sheet.sh <dossier>}"
mkdir -p "$dir"
dir="$(cd "$dir" && pwd)"
shift
extra=()
if [[ "${1:-}" == "--library" ]]; then
    extra=(--library "$(cd "${2:?--library <dossier>}" && pwd)")
fi
dotnet build --nologo >/dev/null
"${GODOT_BIN:-godot-mono}" --headless --path . --import >/dev/null 2>&1 || true
exec "${GODOT_BIN:-godot-mono}" --path . --windowed $(godot_screen_args) --resolution 1600x900 \
    res://tools/tests/ModelSheet.tscn -- --capture "$dir" "${extra[@]}"
