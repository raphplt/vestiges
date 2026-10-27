#!/usr/bin/env bash
# Visiter les terres une à une (étape 5 de doc/LE-JEU.md) : profil dev, terres dans l'ordre donné, invincible si demandé.
# La run se lance du Hub comme d'habitude ; sa première terre est la première de la liste, « Terre suivante » (F4) passe à
# la suivante, et la liste déroulante de F4 refait la terre en cours dans un autre biome.
#
#   tools/play_lands.sh                       les cinq terres, dans l'ordre ci-dessous
#   tools/play_lands.sh swamp,wild_fields     ces terres d'abord, puis le tirage habituel
#   tools/play_lands.sh --god swamp           invincible dès l'arrivée
#   tools/play_lands.sh --list                biomes disponibles
set -euo pipefail
cd "$(dirname "$0")/.."

order="forest_reclaimed,wild_fields,urban_ruins,swamp,collapsed_quarry"
extra=()
while [[ $# -gt 0 ]]; do
    case "$1" in
        --list)
            python3 -c '
import json
for biome in json.load(open("data/world/world_gen.json"))["available_biomes"]:
    print(biome)'
            exit 0 ;;
        --god) extra+=(--dev-command "god on"); shift ;;
        --) shift; extra+=("$@"); break ;;
        -*) echo "Option inconnue : $1 (voir l'en-tête de $0)" >&2; exit 2 ;;
        *) order="$1"; shift ;;
    esac
done

echo "[play_lands] terres : ${order//,/ → } ; F4 › Terres pour passer à la suivante ou changer de biome"
exec tools/run_dev.sh -- --land-order "$order" ${extra[@]+"${extra[@]}"}
