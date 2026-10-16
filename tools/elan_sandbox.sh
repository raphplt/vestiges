#!/usr/bin/env bash
# Bac à sable de l'Élan (plan 52), à jouer à la main : vraie carte, foule entretenue, Élan allumé, coups fatals sans fin de partie.
# Options après -- : --enemies N (foule entretenue, 40 par défaut), --character <id> (traqueur par défaut).
# Exemple : tools/elan_sandbox.sh -- --enemies 80 --character facteur
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet build --nologo
engine_args=()
user_args=()
while [[ $# -gt 0 && "$1" != -- ]]; do engine_args+=("$1"); shift; done
if [[ $# -gt 0 ]]; then shift; user_args=("$@"); fi
exec "${GODOT_BIN:-godot-mono}" --path . "${engine_args[@]}" res://tools/tests/ElanSandbox.tscn -- --dev "${user_args[@]}"
