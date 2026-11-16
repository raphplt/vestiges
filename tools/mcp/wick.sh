#!/usr/bin/env bash
# Serveur MCP wick, lancé depuis .mcp.json. Le serveur est compilé hors dépôt :
#   git clone https://github.com/buildepicshit/Wick ~/.local/share/wick
#   MessagePack monté en 3.1.9 dans Directory.Packages.props, puis
#   dotnet build src/Wick.Server -c Release
set -euo pipefail
# shellcheck source=tools/mcp/env.sh
. "$(dirname "${BASH_SOURCE[0]}")/env.sh"

wick_dll="${WICK_HOME:-$HOME/.local/share/wick}/src/Wick.Server/bin/Release/net10.0/Wick.Server.dll"
if [ ! -f "$wick_dll" ]; then
    echo "wick : serveur absent ($wick_dll), voir l'en-tête de tools/mcp/wick.sh" >&2
    exit 1
fi

dotnet_bin="$(command -v dotnet || true)"
if [ -z "$dotnet_bin" ]; then
    echo "wick : dotnet introuvable" >&2
    exit 1
fi
# /usr/bin/dotnet n'est qu'un lien : DOTNET_ROOT doit viser le vrai dossier du SDK.
export DOTNET_ROOT="${DOTNET_ROOT:-$(cd "$(dirname "$(readlink -f "$dotnet_bin" 2>/dev/null || echo "$dotnet_bin")")" && pwd)}"

export WICK_GROUPS="core,runtime"
export WICK_GODOT_BIN="$GODOT_BIN"
export WICK_PROJECT_PATH="$PROJECT_ROOT"
exec "$dotnet_bin" "$wick_dll"
