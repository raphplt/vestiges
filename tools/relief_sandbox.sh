#!/usr/bin/env bash
# Bac à sable de la verticalité (plan 59), à jouer à la main : arène en étages, horde, bond sur Espace, et trois vues de la
# même simulation qu'on bascule avec Tab (relief 2D isométrique, 3D de haut avec Q/E et molette, 3D en poursuite à la souris).
# Sol et décors d'un vrai biome, généré puis photographié au lancement (forêt par défaut) : --land-order <biome>
# (forest_reclaimed, wild_fields, collapsed_quarry, swamp, urban_ruins) ; --tiles garde des tuiles simples, sans run.
# Options après -- : --view 2d|3d|chase, --enemies N (35 par défaut), --seed S.
# Captures (bot qui fait le tour de l'arène, images des deux vues, puis fermeture) : --capture <dossier> [--seconds S].
# Réglages et arène : data/sandbox/relief.json.
# Exemple : tools/relief_sandbox.sh -- --land-order urban_ruins --view chase
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/portable.sh
dotnet build --nologo >/dev/null
engine_args=()
user_args=()
while [[ $# -gt 0 && "$1" != -- ]]; do engine_args+=("$1"); shift; done
if [[ $# -gt 0 ]]; then shift; user_args=("$@"); fi
if [[ " ${user_args[*]-} " != *" --land-order "* && " ${user_args[*]-} " != *" --tiles "* ]]; then
    user_args+=(--land-order forest_reclaimed)
fi
exec "${GODOT_BIN:-godot-mono}" --path . --windowed $(godot_screen_args) --resolution 1920x1080 \
    ${engine_args[@]+"${engine_args[@]}"} res://tools/tests/ReliefSandbox.tscn -- ${user_args[@]+"${user_args[@]}"}
