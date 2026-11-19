#!/usr/bin/env bash
# Bac à sable de la verticalité (plan 59), à jouer à la main : arène en étages, horde, bond sur Espace, et deux vues de la
# même simulation qu'on bascule avec Tab : caméra libre (par défaut ; souris pour tourner et incliner, molette pour le recul)
# et relief 2D isométrique.
# Rendu low poly (plan 60, lot 60.0b), la pile 3D de la run (scripts/World3D, plan 60 §29) : relief en facettes,
# modèles de assets/models, décors semés. Les sprites du plan 59 sont partis avec la run 2D (§254).
# Styles en jeu : F1 sol (falaises taillées ou droites), F2 corps (low, mid poly), F3 contours, F4 ombrage toon ;
# départ réglé dans data/world3d/render.json (start).
# Options après -- : --view chase|2d, --character <id> (facteur par défaut ; les autres sont exportés au premier
# lancement), --enemies N (35 par défaut), --seed S, --world-scale w.
# Espace : saut commun ; Maj ou clic droit : la capacité du personnage (dash, glissade, déphasage, saut flottant).
# Captures (bot qui fait le tour de l'arène, images des deux vues, puis fermeture) : --capture <dossier> [--seconds S].
# --biome <terre> (forest_reclaimed, urban_ruins, collapsed_quarry, wild_fields, swamp) : les décors de la terre
# (data/props), modèles de la bibliothèque build/models ; --lod-threshold <px> : erreur tolérée des niveaux de détail
# (0 : toujours le plus fin). La capture mesure alors images par seconde et triangles du tour du bot.
# Moteur de rendu : Forward+ par défaut (plan 60, §247), le jeu 2D restant en Compatibility ; options du moteur avant
# --, ex. --rendering-method gl_compatibility pour comparer.
# F5 à F7 (Forward+ seulement, plan 60 §26) : ombres de contact, brume volumétrique, lumières ; --effects none ou
# ssao,fog,lights : ceux allumés au lancement.
# Réglages : rendu, terre d'essai et caméra dans data/world3d/, joueur dans data/movement/player_3d.json, horde d'essai
# et arme dans data/sandbox/relief.json.
# Exemple : tools/relief_sandbox.sh -- --view 2d --enemies 80
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/portable.sh
dotnet build --nologo >/dev/null
# Le mid poly n'est pas versionné (plan 60, 60.0d) : on le génère au premier lancement.
if [[ ! -f assets/models/mid/characters/facteur.json ]]; then
    echo "[relief_sandbox] Modèles mid poly absents : génération (une minute environ)…"
    python3 tools/export_lowpoly.py --detail mid
fi
engine_args=()
user_args=()
while [[ $# -gt 0 && "$1" != -- ]]; do engine_args+=("$1"); shift; done
if [[ $# -gt 0 ]]; then shift; user_args=("$@"); fi
if [[ " ${engine_args[*]-} " != *" --rendering-method "* ]]; then
    engine_args+=(--rendering-method forward_plus)
fi
# Un autre personnage que le facteur : ses modèles sont exportés au premier lancement, non versionnés.
for ((i = 0; i < ${#user_args[@]}; i++)); do
    if [[ "${user_args[$i]}" == --character && -n "${user_args[$((i + 1))]-}" ]]; then
        character="${user_args[$((i + 1))]}"
        [[ -f assets/models/characters/$character.json ]] || python3 tools/export_lowpoly.py --character "$character"
        [[ -f assets/models/mid/characters/$character.json ]] || python3 tools/export_lowpoly.py --character "$character" --detail mid
    fi
done
# Les modèles convertis pour le moteur (plan 60 §19) : normales et niveaux de détail, non versionnés. Seuls les .glb
# plus récents que leur .scn repassent ; en cas d'échec, les .glb sont lus tels quels.
"${GODOT_BIN:-godot-mono}" --headless --path . res://tools/tests/ModelConvert.tscn -- --root "$(pwd)/assets/models" \
    >/dev/null 2>&1 || echo "[relief_sandbox] conversion des modèles en échec : les .glb sont lus tels quels"
# --biome <terre> : les décors de la terre viennent de la bibliothèque de l'export de masse (plan 60 §19, C4c).
if [[ " ${user_args[*]-} " == *" --biome "* ]]; then
    [[ -f build/models/index.json ]] || { echo "[relief_sandbox] bibliothèque absente : python3 tools/export_lowpoly.py --all --jobs 2"; exit 1; }
    "${GODOT_BIN:-godot-mono}" --headless --path . res://tools/tests/ModelConvert.tscn -- --root "$(pwd)/build/models" \
        >/dev/null 2>&1 || echo "[relief_sandbox] conversion de la bibliothèque en échec : les .glb sont lus tels quels"
fi
exec "${GODOT_BIN:-godot-mono}" --path . --windowed $(godot_screen_args) --resolution 1920x1080 \
    ${engine_args[@]+"${engine_args[@]}"} res://tools/tests/ReliefSandbox.tscn -- ${user_args[@]+"${user_args[@]}"}
