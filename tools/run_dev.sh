#!/usr/bin/env bash
# Profil de développement : contenu disponible, progression séparée dans user://dev/.
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet build --nologo
engine_args=()
user_args=()
while [[ $# -gt 0 && "$1" != -- ]]; do engine_args+=("$1"); shift; done
if [[ $# -gt 0 ]]; then shift; user_args=("$@"); fi
exec "${GODOT_BIN:-godot-mono}" --path . ${engine_args[@]+"${engine_args[@]}"} -- --dev ${user_args[@]+"${user_args[@]}"}
