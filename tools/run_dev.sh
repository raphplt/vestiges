#!/usr/bin/env bash
# Profil de développement : contenu disponible, progression séparée dans user://dev/.
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet build --nologo
exec "${GODOT_BIN:-godot-mono}" --path . "$@" -- --dev
