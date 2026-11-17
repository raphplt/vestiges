#!/usr/bin/env bash
# Convertit les modèles low poly (.glb) en scènes prêtes pour le moteur (plan 60 §19, lot C4a) : un .scn à côté de
# chaque .glb, normales lissées et niveaux de détail ; seuls les .glb plus récents que leur .scn repassent. Rapport :
# <dossier>/lods.json. Dossier par défaut : la bibliothèque build/models (tools/export_lowpoly.py --all).
#   tools/convert_models.sh                        # la bibliothèque
#   tools/convert_models.sh assets/models --force  # les modèles du bac à sable, tous
#   tools/convert_models.sh --probe props/forest/prop_tree_medium   # fenêtré : les niveaux de détail s'appliquent-ils ?
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/portable.sh
dotnet build --nologo >/dev/null
"${GODOT_BIN}" --headless --path . --import >/dev/null 2>&1 || true
if [[ "${1:-}" == "--probe" ]]; then
    exec "${GODOT_BIN}" --path . --windowed $(godot_screen_args) --resolution 1280x720 \
        res://tools/tests/ModelConvert.tscn -- --root "$(pwd)/build/models" --probe "${2:?--probe <modèle>}"
fi
root="build/models"
if [[ $# -gt 0 && "$1" != --* ]]; then
    root="$1"
    shift
fi
exec "${GODOT_BIN}" --headless --path . res://tools/tests/ModelConvert.tscn -- --root "$(cd "$root" && pwd)" "$@"
